using UnityEngine;
namespace Brainrot {
 [System.Serializable] public sealed class PlayerStats {
  public float Damage=24,AttackInterval=.65f,MaxHealth=100,MoveSpeed=6,Lifesteal=0,FireDamage=0,Shockwave=0,Range=3.2f,CritChance=.05f,Regen=0,DashCooldown=3; public int ExtraStrikes=0;
  public static PlayerStats From(PlayerTuning tuning) {return new PlayerStats {Damage=Mathf.Max(1,tuning.Damage),AttackInterval=Mathf.Max(.12f,tuning.AttackInterval),MaxHealth=Mathf.Max(1,tuning.MaxHealth),MoveSpeed=Mathf.Max(.1f,tuning.MoveSpeed),Range=Mathf.Max(.5f,tuning.Range),CritChance=Mathf.Clamp01(tuning.CritChance),DashCooldown=Mathf.Max(.1f,tuning.DashCooldown)};}
 }
 public sealed class PlayerController:MonoBehaviour {
  [SerializeField] public PlayerCharacterDefinition Character;
  readonly System.Collections.Generic.List<IPlayerAbilityRuntime> abilities=new System.Collections.Generic.List<IPlayerAbilityRuntime>();
  public PlayerStats Stats=new PlayerStats(); public float Health{get;private set;}
  public int DamageTakenCount{get;private set;}
  public StaminaPool Stamina{get;}=new StaminaPool();
  OctopusAttack captor;bool captureHeld;
  public bool IsCaptured=>captor!=null;
  public float EscapeProgress{get;private set;}
  public bool IsCapturedBy(OctopusAttack source)=>captor==source;
  public bool BeginCapture(OctopusAttack source){if(IsCaptured||Health<=0)return false;captor=source;EscapeProgress=0;captureHeld=GameInput.Attack||MobileInput.Attack;CancelAttacks();return true;}
  public void EndCapture(OctopusAttack source){if(captor==source){captor=null;EscapeProgress=0;CancelAttacks();}}
  public void EscapeTap(){if(!IsCaptured)return;EscapeProgress=Mathf.Clamp01(EscapeProgress+1f/Mathf.Max(1,GameBalance.Current.Octopus.EscapeClicks));if(EscapeProgress>=.999f)captor.Escape();}
  public CombatSystem Combat=>combat;
  public Camera ViewCamera=>cam;
  public PlayerTuning ActiveTuning=>Character!=null&&Character.UseCustomStats&&Character.StartingStats!=null?Character.StartingStats:GameBalance.Current.Player;
  public bool CameraLocked => SaveSystem.Data.HasCameraPreference ? SaveSystem.Data.CameraLocked : ActiveTuning.CameraLocked;
  public Vector3 AimForward => Quaternion.Euler(0,yaw,0)*Vector3.forward;
  public Vector3 DashDirection {get;private set;}
  public float DashCooldownRemaining=>Mathf.Max(0,nextDash-Time.time);
  public float DashCooldownDuration=>Mathf.Max(.1f,Stats.DashCooldown);
  public bool IsDashing => Time.time<dashUntil;
  CharacterController motor; CharacterVisual visual; CombatSystem combat; Camera cam;CameraCollision cameraCollision;
  float yaw,pitch,dashUntil,nextDash,invulnerableUntil,trauma;
  float chargeCameraBlend;Vector3 externalVelocity;
  readonly CombatCameraFeedback combatCamera=new CombatCameraFeedback();
  const float BaseFieldOfView=62;
  public float CombatCameraImpact=>combatCamera.Impact;
  void Awake(){
   motor=gameObject.AddComponent<CharacterController>();motor.height=2.5f;motor.center=Vector3.up*1.25f;motor.radius=.48f;
   Character=Character!=null?Character:PlayerCharacterDefinition.Default;
   visual=CharacterVisual.CreatePlayer(transform,Character.VisualStyle,Character.BodyColor,Character.VisualScale);
   combat=gameObject.AddComponent<CombatSystem>();combat.Initialize(this,visual);
   var cameraGo=new GameObject("Arena Camera");cam=cameraGo.AddComponent<Camera>();cam.fieldOfView=62;cam.clearFlags=CameraClearFlags.Skybox;cam.backgroundColor=new Color(.40f,.78f,.89f);cam.nearClipPlane=.15f;cam.farClipPlane=500;cameraGo.AddComponent<AudioListener>();cameraCollision=new CameraCollision(transform);
   var outline=new GameObject("Bat reach").AddComponent<AttackRangeVisual>();outline.Initialize(this);combat.SetRangeVisual(outline);
   ResetRun();
  }
  public void ResetRun(){
   captor=null;EscapeProgress=0;captureHeld=false;DamageTakenCount=0;var settings=ActiveTuning;Stats=PlayerStats.From(settings);
   var weapon=Character!=null?Character.Weapon:null;if(weapon!=null){Stats.Damage*=Mathf.Max(.1f,weapon.DamageMultiplier);Stats.Range*=Mathf.Max(.1f,weapon.RangeMultiplier);Stats.AttackInterval*=Mathf.Max(.1f,weapon.AttackIntervalMultiplier);}
   Health=Stats.MaxHealth;Stamina.Reset(GameBalance.Current.Stamina);
   foreach(var ability in abilities)ability.Dispose();abilities.Clear();
   if(Character!=null&&Character.Abilities!=null)foreach(var definition in Character.Abilities)if(definition!=null){var runtime=definition.CreateRuntime(this);if(runtime!=null)abilities.Add(runtime);}
   motor.enabled=false;transform.position=IslandMap.StartPosition;transform.rotation=Quaternion.identity;motor.enabled=true;
   yaw=0;pitch=GameBalance.Current.Camera.ClampPitch(GameBalance.Current.Camera.InitialPitch);dashUntil=nextDash=invulnerableUntil=0;DashDirection=Vector3.zero;externalVelocity=Vector3.zero;chargeCameraBlend=0;trauma=0;combatCamera.Reset();if(cam!=null)cam.fieldOfView=BaseFieldOfView;
   combat?.ResetRun();visual?.ResetFeedback();cameraCollision?.Reset();
  }
  public PlayerRunSnapshot CaptureRunState(){
   return new PlayerRunSnapshot {Position=transform.position,RotationYaw=transform.eulerAngles.y,CameraYaw=Mathf.Repeat(yaw,360),CameraPitch=pitch,Health=Health,Stamina=Stamina.Value,DashCooldown=DashCooldownRemaining,HeavyCooldown=combat!=null?combat.HeavyCooldownRemaining:0};
  }
  public void RestoreRunState(PlayerRunSnapshot state){
   if(state==null)return;
   // A checkpoint stores stable actor state, not an in-flight hit, dash or octopus hold.
   ResetRun();Vector3 position=IslandMap.ClampToLand(state.Position);position.y=Mathf.Clamp(position.y,-.05f,1);
   motor.enabled=false;transform.SetPositionAndRotation(position,Quaternion.Euler(0,state.RotationYaw,0));motor.enabled=true;
   Health=Mathf.Clamp(state.Health,.001f,Stats.MaxHealth);Stamina.Restore(state.Stamina,GameBalance.Current.Stamina);
   yaw=Mathf.Repeat(state.CameraYaw,360);pitch=GameBalance.Current.Camera.ClampPitch(state.CameraPitch);
   nextDash=Time.time+Mathf.Clamp(state.DashCooldown,0,Mathf.Max(.1f,Stats.DashCooldown));dashUntil=Time.time;
   combat.RestoreHeavyCooldown(state.HeavyCooldown);invulnerableUntil=Time.time+1.25f;
   if(CameraLocked)transform.rotation=Quaternion.LookRotation(AimForward);
   Physics.SyncTransforms();cameraCollision?.Reset();
  }
  public void ToggleCameraLock(){SetCameraLock(!CameraLocked);}
  public void SetCameraLock(bool locked){SaveSystem.Data.CameraLocked=locked;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();if(locked)transform.rotation=Quaternion.LookRotation(AimForward);}
  // All callers provide a world-space movement vector; a stationary dash does not consume cooldown.
  public bool TryDash(Vector3 movement){
   movement.y=0;
   if(GameManager.Instance==null||!GameManager.Instance.IsPlaying||Time.time<nextDash||movement.sqrMagnitude<.001f)return false;
   var settings=ActiveTuning;DashDirection=movement.normalized;dashUntil=Time.time+Mathf.Max(.05f,settings.DashDuration);nextDash=Time.time+Mathf.Max(.1f,Stats.DashCooldown);invulnerableUntil=dashUntil;
   HitEffects.Burst(transform.position+Vector3.up,Color.cyan);return true;
  }
  void Update(){
   var gm=GameManager.Instance;if(gm==null||!gm.IsPlaying)return;
   if(IsCaptured){bool held=GameInput.Attack||MobileInput.Attack;if(held&&!captureHeld)EscapeTap();captureHeld=held;EscapeProgress=Mathf.Max(0,EscapeProgress-Time.deltaTime*.025f);}
   if(GameInput.CameraToggle)ToggleCameraLock();
   Vector2 look=Application.isMobilePlatform?MobileInput.Look:GameInput.Look;MobileInput.Look=Vector2.zero;
   yaw+=look.x*2.3f*SaveSystem.Data.Sensitivity;pitch=GameBalance.Current.Camera.ClampPitch(pitch-look.y*1.8f*SaveSystem.Data.Sensitivity);
   Vector2 input=Application.isMobilePlatform?MobileInput.Move:GameInput.Move;input=Vector2.ClampMagnitude(input,1);
   Vector3 move=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);bool sprint=GameInput.Sprint||MobileInput.Sprint;
   var settings=ActiveTuning;
   if(GameInput.Dash||MobileInput.Dash)TryDash(move);MobileInput.Dash=false;
   sprint=Stamina.Tick(sprint,move.sqrMagnitude>.01f,IsDashing,Time.deltaTime,GameBalance.Current.Stamina);
   Vector3 velocity=IsDashing?DashDirection*Mathf.Max(1,settings.DashSpeed):move*Stats.MoveSpeed*(sprint?Mathf.Max(1,settings.SprintMultiplier):1);
   // Integrate the decaying shove exactly: low frame rates must not enlarge boss knockback.
   float pushDecay=Mathf.Exp(-6*Time.deltaTime);Vector3 pushStep=externalVelocity*((1-pushDecay)/6);externalVelocity*=pushDecay;
   if(externalVelocity.sqrMagnitude<.001f)externalVelocity=Vector3.zero;
   Vector3 beforeMove=transform.position;
   motor.Move((velocity+Vector3.down*12)*Time.deltaTime+pushStep);
   Vector3 pos=transform.position;Vector3 bounded=IslandMap.ClampToLand(pos);
   if((bounded-pos).sqrMagnitude>.0001f){motor.enabled=false;transform.position=bounded;motor.enabled=true;}
   // Locked mode keeps aim relative to camera even when moving backwards or sideways.
   if(CameraLocked)transform.rotation=Quaternion.LookRotation(AimForward);
   else if(move.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(move),1-Mathf.Exp(-Mathf.Max(1,settings.TurnSpeed)*Time.deltaTime));
   if(!IsCaptured)combat.UpdateAttackInput(GameInput.Attack||MobileInput.Attack,GameInput.AttackPressed,GameInput.AttackReleased);
   Vector3 actualVelocity=(transform.position-beforeMove)/Mathf.Max(.0001f,Time.deltaTime);actualVelocity.y=0;
   visual.SetLocomotion(transform.InverseTransformDirection(actualVelocity),IsDashing);
   visual.Animate(actualVelocity.magnitude/Mathf.Max(.1f,Stats.MoveSpeed)*3,combat.IsAttacking);Heal(Stats.Regen*Time.deltaTime);foreach(var ability in abilities)ability.Tick(Time.deltaTime);
  }
  void LateUpdate(){
   float visualScale=Mathf.Max(.1f,Character!=null?Character.VisualScale:1)*Mathf.Max(.1f,GameBalance.Current.PlayerModelScale);
   if(visual!=null)visual.transform.localScale=Vector3.one*visualScale;
   if(cam==null)return;var settings=GameBalance.Current.Camera;var feedback=GameBalance.Current.Feedback;pitch=settings.ClampPitch(pitch);Quaternion rot=Quaternion.Euler(pitch,yaw,0);
   bool playing=GameManager.Instance!=null&&GameManager.Instance.IsPlaying&&Health>0;
   if(!playing){combatCamera.Reset();chargeCameraBlend=trauma=0;}
   float zoomTarget=combat!=null&&combat.IsCharging&&combat.HeavyReady?Mathf.SmoothStep(0,1,combat.ChargeProgress):0;
   combatCamera.Tick(playing?zoomTarget:0,playing?Time.deltaTime:0,feedback);
   cam.fieldOfView=combatCamera.FieldOfView(BaseFieldOfView,feedback,SaveSystem.Data.CameraShakeStrength);
   float zoomSpeed=zoomTarget>chargeCameraBlend?settings.ChargeZoomInSpeed:settings.ChargeZoomOutSpeed;
   chargeCameraBlend=Mathf.Lerp(chargeCameraBlend,zoomTarget,1-Mathf.Exp(-Mathf.Max(.1f,zoomSpeed)*Time.deltaTime));
   Vector3 target=transform.position+Vector3.up*(1.9f*visualScale+Mathf.Max(0,settings.HeightAboveHead));
   float minimumDistance=Mathf.Max(1.65f,visualScale*1.55f);
   float distance=Mathf.Max(minimumDistance,settings.Distance-Mathf.Max(0,settings.ChargeZoomDistance)*chargeCameraBlend);
   Vector3 shake=combatCamera.Offset(feedback,SaveSystem.Data.CameraShakeStrength)+new Vector3(Mathf.Sin(Time.unscaledTime*71),Mathf.Sin(Time.unscaledTime*83),Mathf.Sin(Time.unscaledTime*59))*trauma*Mathf.Clamp01(SaveSystem.Data.CameraShakeStrength);
   Vector3 desired=target+rot*Vector3.right*settings.ShoulderOffset-rot*Vector3.forward*distance+shake;
   if(settings.ObstacleCollision&&cameraCollision!=null){
    float nearHalfHeight=cam.nearClipPlane*Mathf.Tan(cam.fieldOfView*.5f*Mathf.Deg2Rad);
    float nearCorner=nearHalfHeight*Mathf.Sqrt(1+cam.aspect*cam.aspect);
    cam.transform.position=cameraCollision.Resolve(target,desired,Mathf.Max(settings.CollisionRadius,nearCorner),settings.CollisionPadding,settings.CollisionReturnSpeed,settings.CollisionMask,Time.unscaledDeltaTime);
   }else{cameraCollision?.Reset();cam.transform.position=desired;}
   cam.transform.rotation=rot;trauma=Mathf.MoveTowards(trauma,0,Time.unscaledDeltaTime);
  }
  public void Heal(float amount){Health=Mathf.Min(Stats.MaxHealth,Health+Mathf.Max(0,amount));}
  public void TakeDamage(float amount,Vector3 source,float knockbackSpeed=0){
   if(GameManager.Instance==null||!GameManager.Instance.IsPlaying||Time.time<invulnerableUntil||amount<=0)return;
   DamageTakenCount++;Health=Mathf.Max(0,Health-amount);foreach(var ability in abilities)ability.OnDamageTaken(amount);
   if(knockbackSpeed>0){Vector3 away=transform.position-source;away.y=0;if(away.sqrMagnitude<.001f)away=-transform.forward;externalVelocity=away.normalized*Mathf.Clamp(knockbackSpeed,0,40);}
   invulnerableUntil=Time.time+.5f;trauma=.18f;HitEffects.Burst(transform.position+Vector3.up,new Color(1,.2f,.1f));GameAudio.Play(130,.1f,.15f);if(Health<=0)GameManager.Instance.Die();
  }
  public void CancelAttacks(){combat?.CancelPendingSwings();ResetCombatCamera();}
  public void ResetCombatCamera(){chargeCameraBlend=trauma=0;combatCamera.Reset();if(cam!=null)cam.fieldOfView=BaseFieldOfView;}
  public void ConfirmBatHit(bool heavy,int hits){if(hits<=0)return;combatCamera.ConfirmHit(transform.forward,heavy,GameBalance.Current.Feedback);}
  public void NotifyAttack(){foreach(var ability in abilities)ability.OnAttack();}
  void OnDestroy(){cameraCollision?.Dispose();foreach(var ability in abilities)ability.Dispose();abilities.Clear();if(cam!=null)Destroy(cam.gameObject);}
 }
}
