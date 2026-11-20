using UnityEngine;
namespace Brainrot
{
    public enum BossPose {Idle,Run,Windup,BiteLeft,BiteRight,BiteHeavy,Stomp,Spin,Jump,Tentacle,Spit,Grab,Throw,Charge,Slam,RightPunch,LeftPunch,DoublePunch,Recover,Stunned,Phase,Dead}
    public enum BossWarning {Area,Charge,Special}
    public interface IBossPattern {void Tick(float dt);void Reset();}
    /// <summary>One encounter owns patterns, hazards and capture receipts; ordinary enemy AI stays separate.</summary>
    public sealed class BossEncounter:MonoBehaviour
    {
        public EnemyController Enemy{get;private set;}
        public PlayerController Player=>GameManager.Instance!=null?GameManager.Instance.Player:null;
        public CharacterVisual Visual=>Enemy!=null?Enemy.Visual:null;
        public IslandBossKind Kind{get;private set;}
        public int Wave{get;private set;}
        public bool IsActive{get;private set;}
        public bool PhaseTwo{get;private set;}
        public string Title=>Kind==IslandBossKind.Shark?"ГИГАНТСКИЙ ТРАЛАЛЕРО":Kind==IslandBossKind.Octopus?"ГИГАНТСКИЙ ОКТОПУСИНИ":"ГИГАНТСКИЙ ОРКАЛАДОН";
        public float HealthFraction=>Enemy!=null&&Enemy.MaxHealth>0?Mathf.Clamp01(Enemy.Health/Enemy.MaxHealth):0;
        public BossFoeTuning Settings=>GameBalance.Current.IslandBosses.For(Kind);
        public float Damage=>Settings.DamageAt(Wave);
        public float Speed=>Mathf.Max(.1f,Settings.Speed);
        public float Radius=>Enemy!=null&&Enemy.BodyCollider!=null?Enemy.BodyCollider.radius:.8f;
        public Vector3 Position{get=>transform.position;set{value.y=0;transform.position=IslandMap.ClampToLand(value,Mathf.Max(2,Radius));}}
        public bool Capturing{get;private set;}
        public bool CaptureEscaped{get;private set;}
        public bool CaptureFinished{get;private set;}
        public float CaptureProgress=>Capturing?Mathf.Clamp01(captureClock/Mathf.Max(.1f,captureDuration)):1;
        public int ActiveHazards=>arena!=null?arena.ActiveHazards:0;
        IBossPattern pattern;BossArenaEffects arena;BossVisualDriver driver;
        float phaseClock,stunClock,captureClock,nextCapture;bool throwCapture,wasPaused;
        readonly RaycastHit[] hits=new RaycastHit[48];
        public void Initialize(EnemyController enemy,IslandBossKind kind,int wave){
            ResetEncounter();Enemy=enemy;Kind=kind;Wave=Mathf.Max(5,wave);PhaseTwo=false;IsActive=true;
            if(arena==null){arena=gameObject.GetComponent<BossArenaEffects>();if(arena==null)arena=gameObject.AddComponent<BossArenaEffects>();}arena.Initialize(this);
            if(driver==null){driver=GetComponent<BossVisualDriver>();if(driver==null)driver=gameObject.AddComponent<BossVisualDriver>();}driver.Initialize(enemy);
            pattern=kind==IslandBossKind.Shark?(IBossPattern)new BossSharkPattern(this):kind==IslandBossKind.Octopus?new BossOctopusPattern(this):(IBossPattern)new BossOrcaladonPattern(this);
            pattern.Reset();phaseClock=1.25f;wasPaused=false;Pose(BossPose.Phase,0);
            GameManager.Instance?.Notify(Title+"\nНАЙДИ ОКНО ДЛЯ КОНТРАТАКИ",3);
        }
        public void Tick(float dt){
            if(!IsActive||Enemy==null||!Enemy.Alive||Player==null||Player.Health<=0||GameManager.Instance==null||!GameManager.Instance.IsPlaying||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            if(wasPaused){wasPaused=false;phaseClock=Mathf.Max(phaseClock,.65f);}
            if(!PhaseTwo&&HealthFraction<=Mathf.Clamp(Settings.PhaseThreshold,.1f,.9f)){
                PhaseTwo=true;CancelAttack();phaseClock=1.15f;GameManager.Instance.Notify(Title+" · ФАЗА II",3);GameAudio.PlayChallenge();
                HitEffects.Burst(Position+Vector3.up,Color.cyan,16,Radius+1);return;
            }
            if(phaseClock>0){phaseClock=Mathf.Max(0,phaseClock-dt);Pose(BossPose.Phase,1-phaseClock/1.25f);return;}
            arena.Tick(dt);
            if(!IsActive||GameManager.Instance==null||!GameManager.Instance.IsPlaying)return;
            if(Capturing&&!Player.IsBossCapturedBy(this))FinishCapture(true,false);
            if(Capturing){
                captureClock+=dt;var anchor=Position+transform.forward*(Radius+.9f)+Vector3.up*(1.6f+Mathf.Sin(Mathf.Clamp01(captureClock/.6f)*Mathf.PI*.5f)*1.4f);
                Player.PlaceBossCapture(this,anchor);
                if(captureClock>=captureDuration)FinishCapture(false,throwCapture);
            }
            if(!IsActive||GameManager.Instance==null||!GameManager.Instance.IsPlaying||Player==null||Player.Health<=0)return;
            if(stunClock>0){stunClock=Mathf.Max(0,stunClock-dt);Pose(BossPose.Stunned,1-stunClock/Mathf.Max(.1f,Settings.StunSeconds));return;}
            pattern?.Tick(dt);
        }
        public void HitReaction(){driver?.NotifyHit();}
        public void Pose(BossPose pose,float progress,float strength=1){driver?.SetPose(pose,Mathf.Clamp01(progress),strength);}
        public void Face(Vector3 direction,float dt){direction.y=0;if(direction.sqrMagnitude>.0001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-7*Mathf.Max(0,dt)));}
        bool Environment(Collider collider)=>collider!=null&&!collider.isTrigger&&collider.bounds.max.y>.4f&&!collider.transform.IsChildOf(transform)&&collider.GetComponentInParent<EnemyController>()==null&&collider.GetComponentInParent<PlayerController>()==null;
        public bool ChargeMove(Vector3 direction,float distance,out bool blocked){
            direction.y=0;blocked=false;if(direction.sqrMagnitude<.0001f||distance<=0)return false;direction.Normalize();
            var origin=Position;float allowed=distance;
            int count=Physics.SphereCastNonAlloc(origin+Vector3.up*Mathf.Max(.7f,Radius*.8f),Mathf.Max(.4f,Radius*.8f),direction,hits,distance+.08f,~0,QueryTriggerInteraction.Ignore);
            if(count>=hits.Length){allowed=0;blocked=true;}
            else for(int i=0;i<count;i++)if(Environment(hits[i].collider)&&hits[i].distance<allowed+.08f){allowed=Mathf.Max(0,hits[i].distance-.08f);blocked=true;}
            var wanted=origin+direction*allowed;Position=wanted;
            // The coast also ends a rush safely; the actor never waits outside the walkable arena.
            if((Position-wanted).sqrMagnitude>.0001f)blocked=true;
            return (Position-origin).sqrMagnitude>.000001f;
        }
        public void Move(Vector3 displacement){
            displacement.y=0;float distance=displacement.magnitude;if(distance<.00001f)return;
            ChargeMove(displacement/distance,distance,out bool blocked);
            if(blocked){var tangent=Vector3.Cross(Vector3.up,displacement.normalized);ChargeMove(tangent,distance*.5f,out _);}
        }
        public void WarnCircle(int slot,Vector3 center,float radius,float progress,BossWarning color=BossWarning.Area)=>arena?.Circle(slot,center,Mathf.Max(.1f,radius),progress,color);
        public void WarnLane(int slot,Vector3 origin,Vector3 dir,float length,float halfWidth,float progress)=>arena?.Lane(slot,origin,dir,length,halfWidth,progress);
        public void WarnCone(int slot,Vector3 origin,Vector3 dir,float range,float degrees,float progress)=>arena?.Cone(slot,origin,dir,range,degrees,progress);
        public void HideWarnings()=>arena?.HideWarnings();
        public static bool InCircle(Vector3 point,Vector3 center,float radius){point.y=center.y=0;return (point-center).sqrMagnitude<=radius*radius;}
        public static bool InCone(Vector3 point,Vector3 center,Vector3 direction,float radius,float degrees){
            point.y=center.y=direction.y=0;var delta=point-center;return delta.sqrMagnitude<=radius*radius&&(delta.sqrMagnitude<.000001f||Vector3.Angle(direction,delta)<=Mathf.Clamp(degrees,1,360)*.5f);
        }
        public void CircleHit(Vector3 center,float radius,float multiplier=1,float knockback=0){if(Player!=null&&InCircle(Player.transform.position,center,radius))Hit(center,multiplier,knockback);}
        public void ConeHit(Vector3 origin,Vector3 direction,float range,float degrees,float multiplier=1,float knockback=0){if(Player!=null&&InCone(Player.transform.position,origin,direction,range,degrees))Hit(origin,multiplier,knockback);}
        public void Hit(Vector3 source,float multiplier=1,float knockback=0){
            if(!IsActive||Capturing||GameManager.Instance==null||!GameManager.Instance.IsPlaying||Player==null)return;
            Player.TakeDamage(Damage*Mathf.Max(0,multiplier),source,knockback);
        }
        public void Shockwave(Vector3 center,float radius,float speed,float damageMultiplier=1,float delay=0){
            arena?.Shockwave(center,radius,speed,damageMultiplier,delay);
            if(delay<=0){HitEffects.Burst(center+Vector3.up*.1f,new Color(.95f,.76f,.43f),18,0);GameAudio.Impact(true,false,1);if(Player!=null&&InCircle(Player.transform.position,center,14))Player.BossCameraImpulse(.09f);}
        }
        public void Ink(Vector3 center,float radius,float duration,float slowMultiplier=.55f)=>arena?.Ink(center,radius,duration,slowMultiplier);
        public void Tentacle(Vector3 center,float radius,float delay,float damageMultiplier=1)=>arena?.Tentacle(center,radius,delay,damageMultiplier);
        public bool TryCapture(float duration=2.5f,bool throwPlayer=false,float range=0){
            if(!IsActive||Player==null||Time.time<nextCapture||!InCircle(Player.transform.position,Position,Mathf.Max(.5f,range>0?range:Kind==IslandBossKind.Orcaladon?Settings.GrabRange:Settings.CaptureRange))||!Player.BeginBossCapture(this))return false;
            Capturing=true;CaptureEscaped=CaptureFinished=false;captureClock=0;captureDuration=Mathf.Max(.6f,duration);throwCapture=throwPlayer;nextCapture=Time.time+8;return true;
        }
        float captureDuration;
        public void EscapeCapture(){if(Capturing)FinishCapture(true,false);}
        void FinishCapture(bool escaped,bool throwPlayer){
            Capturing=false;CaptureEscaped=escaped;CaptureFinished=true;
            Player?.EndBossCapture(this,escaped);
            if(throwPlayer&&!escaped){Player?.TakeDamage(Damage,Position,Mathf.Max(8,Settings.Knockback));Pose(BossPose.Throw,1);}
            else if(!escaped)Player?.TakeDamage(Damage*.6f,Position,4);
        }
        public void CancelAttack(){
            if(Capturing){Capturing=false;Player?.EndBossCapture(this,true);}CaptureFinished=true;CaptureEscaped=false;
            pattern?.Reset();arena?.Clear();driver?.ResetPose();stunClock=0;
        }
        public void OnBossDeath(){
            if(!IsActive)return;CancelAttack();IsActive=false;
            GameManager.Instance?.Notify("ПОБЕДА! "+Title,4);GameAudio.PlayChallenge();HitEffects.Burst(Position+Vector3.up,new Color(1,.8f,.3f),24,Radius+2);
        }
        public void ResetEncounter(){CancelAttack();IsActive=false;PhaseTwo=false;phaseClock=stunClock=nextCapture=captureClock=0;pattern=null;Enemy=null;wasPaused=false;}
        public void Suspend(){if(!IsActive)return;CancelAttack();wasPaused=true;}
        void Update(){
            if(IsActive&&GameManager.Instance!=null&&!GameManager.Instance.IsPlaying&&!wasPaused){Suspend();}
        }
        void OnDisable(){ResetEncounter();}
    }
}
