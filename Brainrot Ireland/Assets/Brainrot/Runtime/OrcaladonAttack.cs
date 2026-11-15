using UnityEngine;
namespace Brainrot
{
    public enum OrcaladonPhase { Approach, ChargeAim, Charging, SlamWindup, SlamImpact, Recovery }

    /// <summary>Two committed, telegraphed attacks. Their cooldowns and hit gates are independent.</summary>
    public sealed class OrcaladonAttack : MonoBehaviour
    {
        EnemyController enemy;
        CharacterVisual visual;
        EnemyTelegraph warning;
        float phaseClock,phaseDuration,chargeCooldown,slamCooldown;
        bool chargeHit;
        public OrcaladonPhase Phase {get;private set;}
        public float PhaseProgress=>phaseDuration>.00001f?Mathf.Clamp01(1-phaseClock/phaseDuration):0;
        public Vector3 CommittedDirection {get;private set;}=Vector3.forward;
        public float ChargeCooldownRemaining=>chargeCooldown;
        public float SlamCooldownRemaining=>slamCooldown;
        public bool IsAttacking=>Phase==OrcaladonPhase.ChargeAim||Phase==OrcaladonPhase.Charging||Phase==OrcaladonPhase.SlamWindup||Phase==OrcaladonPhase.SlamImpact;
        OrcaladonTuning Settings=>GameBalance.Current.Orcaladon;
        float SlamRadius=>Mathf.Max(.5f,Mathf.Max(Settings.SlamRange,Settings.CollisionRadius+.55f));

        public void Initialize(EnemyController controller,CharacterVisual characterVisual)
        {
            enemy=controller;visual=characterVisual;
            if(warning==null)warning=new EnemyTelegraph(transform);
            ResetAttack();CommittedDirection=transform.forward;
        }
        public void Tick(float dt)
        {
            var game=GameManager.Instance;
            if(enemy==null||!enemy.Alive||game==null||!game.IsPlaying||game.Player==null||game.Player.Health<=0||dt<=0)return;
            chargeCooldown=Mathf.Max(0,chargeCooldown-dt);slamCooldown=Mathf.Max(0,slamCooldown-dt);
            var tuning=Settings;var player=game.Player;
            Vector3 offset=Flat(player.transform.position-transform.position);
            float distance=offset.magnitude;
            Vector3 direction=distance>.0001f?offset/distance:transform.forward;
            switch(Phase)
            {
                case OrcaladonPhase.Approach:
                    if(distance<=SlamRadius&&slamCooldown<=0){BeginSlam(direction);return;}
                    float min=Mathf.Max(.5f,tuning.ChargeMinDistance),max=Mathf.Max(min,tuning.ChargeMaxDistance);
                    if(distance>=min&&distance<=max&&chargeCooldown<=0){BeginCharge(direction);return;}
                    float speed=Mathf.Max(.1f,GameBalance.Current.Enemy(EnemyKind.Orcaladon).Speed);
                    float movement=distance>Mathf.Max(.6f,tuning.CollisionRadius+.5f)?speed:0;
                    transform.position+=direction*(movement*dt);
                    Face(direction,dt);visual?.Animate(movement,false);
                    return;
                case OrcaladonPhase.ChargeAim:
                    visual?.Animate(0,true);phaseClock-=dt;
                    if(phaseClock<=0){warning.Hide();Enter(OrcaladonPhase.Charging,Mathf.Max(.1f,tuning.ChargeDuration));}
                    return;
                case OrcaladonPhase.Charging:
                    float step=Mathf.Min(dt,Mathf.Max(0,phaseClock));
                    Vector3 start=transform.position,end=start+CommittedDirection*(Mathf.Max(1,tuning.ChargeSpeed)*step);end.y=0;transform.position=end;
                    if(!chargeHit&&EnemyController.SweptHitXZ(start,end,player.transform.position,Mathf.Max(.1f,tuning.CollisionRadius)+.48f)){
                        chargeHit=true;
                        // Always push along the charge, including a large frame that ends beyond the player.
                        player.TakeDamage(Mathf.Max(0,tuning.ChargeDamage),player.transform.position-CommittedDirection*2,Mathf.Max(0,tuning.ChargeKnockback));
                    }
                    phaseClock-=dt;visual?.Animate(Mathf.Max(1,tuning.ChargeSpeed),true);
                    if(phaseClock<=0)EnterRecovery(Mathf.Max(.05f,tuning.Recovery));
                    return;
                case OrcaladonPhase.SlamWindup:
                    visual?.Animate(0,true);phaseClock-=dt;
                    if(phaseClock<=0){
                        float radius=SlamRadius;
                        Enter(OrcaladonPhase.SlamImpact,.18f);
                        warning.Circle(transform.position,radius);
                        if(Flat(player.transform.position-transform.position).sqrMagnitude<=radius*radius)
                            player.TakeDamage(Mathf.Max(0,tuning.SlamDamage),transform.position,Mathf.Max(0,tuning.SlamKnockback));
                        HitEffects.Burst(transform.position+Vector3.up*.12f,new Color(1,.85f,.42f),24,radius);
                        GameAudio.Play(95,.13f,.12f);
                    }
                    return;
                case OrcaladonPhase.SlamImpact:
                    visual?.Animate(0,true);phaseClock-=dt;
                    if(phaseClock<=0)EnterRecovery(Mathf.Max(.05f,tuning.Recovery));
                    return;
                case OrcaladonPhase.Recovery:
                    visual?.Animate(0,false);phaseClock-=dt;
                    if(phaseClock<=0)Enter(OrcaladonPhase.Approach,0);
                    return;
            }
        }
        void BeginCharge(Vector3 direction)
        {
            var t=Settings;CommittedDirection=direction;transform.rotation=Quaternion.LookRotation(direction);chargeHit=false;
            chargeCooldown=Mathf.Max(.1f,t.ChargeCooldown);
            Enter(OrcaladonPhase.ChargeAim,Mathf.Max(.1f,t.ChargeWindup));
            warning.ChargeStraight(transform.position,direction,Mathf.Max(1,t.ChargeSpeed)*Mathf.Max(.1f,t.ChargeDuration),Mathf.Max(.1f,t.CollisionRadius)+.48f);
            visual?.Animate(0,true);
        }
        void BeginSlam(Vector3 direction)
        {
            var t=Settings;CommittedDirection=direction;transform.rotation=Quaternion.LookRotation(direction);
            slamCooldown=Mathf.Max(.1f,t.SlamCooldown);
            Enter(OrcaladonPhase.SlamWindup,Mathf.Max(.15f,t.SlamWindup));warning.Circle(transform.position,SlamRadius);visual?.Animate(0,true);
        }
        void Enter(OrcaladonPhase phase,float duration){Phase=phase;phaseDuration=phaseClock=Mathf.Max(0,duration);}
        void EnterRecovery(float duration){warning?.Hide();Enter(OrcaladonPhase.Recovery,duration);}
        public void Interrupt()
        {
            // A heavy hit cancels the committed attack without granting a fresh ready attack.
            chargeHit=false;EnterRecovery(Mathf.Max(.02f,Settings.HitStagger));
        }
        public void ResetAttack(){warning?.Hide();chargeHit=false;chargeCooldown=slamCooldown=0;Enter(OrcaladonPhase.Approach,0);}
        void Face(Vector3 direction,float dt){if(direction.sqrMagnitude>.0001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),Mathf.Clamp01(dt*7));}
        static Vector3 Flat(Vector3 value){value.y=0;return value;}
        void OnDisable(){ResetAttack();}
    }
}
