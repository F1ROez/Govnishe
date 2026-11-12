using UnityEngine;
namespace Brainrot {
 [System.Serializable] public sealed class PlayerStats {
  public float Damage=24,AttackInterval=.6f,MaxHealth=100,MoveSpeed=6,Lifesteal=0,FireDamage=0,Shockwave=0,Range=3.4f,CritChance=.05f,Regen=0,DashCooldown=3;public int ExtraStrikes=0;
 }
 public sealed class PlayerController:MonoBehaviour {
  public PlayerStats Stats=new PlayerStats(); public float Health{get;private set;}
  CharacterController motor; CharacterVisual visual; CombatSystem combat; Camera cam; float yaw,pitch=38,dashUntil,nextDash,invulnerableUntil; Vector3 dashDirection; float trauma;
  void Awake(){motor=gameObject.AddComponent<CharacterController>();motor.height=2.5f;motor.center=Vector3.up*1.25f;motor.radius=.48f;
   visual=CharacterVisual.Create(transform,false,new Color(.52f,.25f,.09f));combat=gameObject.AddComponent<CombatSystem>();combat.Initialize(this,visual);
   var cameraGo=new GameObject("Arena Camera");cam=cameraGo.AddComponent<Camera>();cam.fieldOfView=62;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.40f,.78f,.89f);cam.nearClipPlane=.15f;cam.farClipPlane=500;cameraGo.AddComponent<AudioListener>(); ResetRun();
  }
  public void ResetRun(){Stats=new PlayerStats();Health=Stats.MaxHealth;motor.enabled=false;transform.position=Vector3.up*.15f;transform.rotation=Quaternion.identity;motor.enabled=true;yaw=0;pitch=38;dashUntil=nextDash=invulnerableUntil=0;combat?.ResetRun();}
  void Update(){var gm=GameManager.Instance;if(gm==null||!gm.IsPlaying)return;
   Vector2 look=Application.isMobilePlatform?MobileInput.Look:GameInput.Look;
   MobileInput.Look=Vector2.zero; yaw+=look.x*2.3f*SaveSystem.Data.Sensitivity;pitch=Mathf.Clamp(pitch-look.y*1.8f*SaveSystem.Data.Sensitivity,15,70);
   Vector2 input=Application.isMobilePlatform?MobileInput.Move:GameInput.Move;input=Vector2.ClampMagnitude(input,1);
   Vector3 move=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y); bool sprint=GameInput.Sprint||MobileInput.Sprint;
   if((GameInput.Dash||MobileInput.Dash)&&Time.time>=nextDash){dashDirection=move.sqrMagnitude>.01f?move:transform.forward;dashUntil=Time.time+.18f;nextDash=Time.time+Stats.DashCooldown;invulnerableUntil=dashUntil;HitEffects.Burst(transform.position+Vector3.up,Color.cyan);}
   MobileInput.Dash=false;Vector3 velocity=Time.time<dashUntil?dashDirection*23:move*Stats.MoveSpeed*(sprint?1.45f:1);
   motor.Move((velocity+Vector3.down*12)*Time.deltaTime);Vector3 pos=transform.position;Vector2 flat=Vector2.ClampMagnitude(new Vector2(pos.x,pos.z),21);if(flat.x!=pos.x||flat.y!=pos.z){motor.enabled=false;transform.position=new Vector3(flat.x,.15f,flat.y);motor.enabled=true;}
   if(move.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(move),Time.deltaTime*14);
   if(GameInput.Attack||MobileInput.Attack)combat.TryAttack();
   visual.Animate(move.magnitude*(sprint?1.5f:1),combat.IsAttacking);Heal(Stats.Regen*Time.deltaTime);
  }
  void LateUpdate(){if(cam==null)return;Quaternion rot=Quaternion.Euler(pitch,yaw,0);Vector3 target=transform.position+Vector3.up*1.6f;cam.transform.position=target-rot*Vector3.forward*10+Random.insideUnitSphere*trauma;cam.transform.LookAt(target);trauma=Mathf.MoveTowards(trauma,0,Time.unscaledDeltaTime);}
  public void Heal(float amount){Health=Mathf.Min(Stats.MaxHealth,Health+Mathf.Max(0,amount));}
  public void TakeDamage(float amount,Vector3 source){if(!GameManager.Instance.IsPlaying||Time.time<invulnerableUntil)return;Health=Mathf.Max(0,Health-amount);invulnerableUntil=Time.time+.5f;trauma=.22f;HitEffects.Burst(transform.position+Vector3.up,new Color(1,.2f,.1f));GameAudio.Play(130,.1f,.15f);if(Health<=0)GameManager.Instance.Die();}
 }
}