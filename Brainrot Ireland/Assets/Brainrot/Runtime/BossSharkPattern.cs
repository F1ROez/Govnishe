using UnityEngine;
namespace Brainrot
{
    public enum SharkBossAction { Approach, ChargeWindup, Charging, BiteWindup, Biting, StompWindup, Stomping, SpinWindup, Spinning, Recovery, Stunned }

    /// <summary>Committed attacks and deliberate counter windows; a new warning always gets a rendered frame.</summary>
    public sealed class BossSharkPattern:IBossPattern
    {
        readonly BossEncounter c;
        SharkBossAction state;
        float age,duration,wait,contactClock;
        int nextAttack,chargeSeries,chargesLeft,bite;
        bool hit,stompSent,doubleCharge;
        Vector3 direction;
        public SharkBossAction Action=>state;
        public int BiteNumber=>bite;
        public Vector3 CommittedDirection=>direction;
        public float StateDuration=>duration;
        public bool DirectionLocked=>state!=SharkBossAction.ChargeWindup||age>=Mathf.Max(0,duration-Mathf.Max(.12f,c.Settings.ChargeLockSeconds));
        public BossSharkPattern(BossEncounter encounter){c=encounter;Reset();}
        public void Reset(){
            state=SharkBossAction.Approach;age=duration=contactClock=0;wait=.8f;
            nextAttack=chargeSeries=chargesLeft=bite=0;hit=stompSent=doubleCharge=false;direction=Vector3.forward;
            c.HideWarnings();
        }
        public void Tick(float dt){
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt)||c.Player==null)return;
            age+=dt;
            Vector3 toward=Flat(c.Player.transform.position-c.Position);
            float distance=toward.magnitude;
            var aim=distance>.0001f?toward/distance:c.Enemy.transform.forward;
            switch(state){
                case SharkBossAction.Approach:
                    c.Face(aim,dt);c.Pose(BossPose.Run,Mathf.Repeat(age*.8f,1));
                    // The approach stays aggressive without moving during the committed hit windows.
                    if(distance>Mathf.Max(2,c.Radius+1.1f))c.Move(aim*Mathf.Min(c.Speed*dt,Mathf.Max(0,distance-c.Radius-1.1f)));
                    wait-=dt;if(wait>0)return;
                    int attack=nextAttack++%(c.PhaseTwo?4:3);
                    if(attack==0){doubleCharge=c.PhaseTwo&&(chargeSeries++%2==0);chargesLeft=doubleCharge?2:1;BeginCharge(aim);}
                    else if(attack==1){bite=0;BeginBite(aim);}
                    else if(attack==2)BeginStomp(aim);
                    else BeginSpin(aim);
                    return;
                case SharkBossAction.ChargeWindup:
                    if(!DirectionLocked)direction=aim;
                    c.Face(direction,dt);DrawCharge();c.Pose(BossPose.Windup,Progress);
                    if(age>=duration){Enter(SharkBossAction.Charging,Mathf.Max(.2f,c.Settings.ChargeSeconds));hit=false;}
                    return;
                case SharkBossAction.Charging:
                    DrawCharge();c.Pose(BossPose.Charge,Progress);
                    Vector3 previous=c.Position;
                    float step=Mathf.Min(dt,Mathf.Max(0,duration-(age-dt)));
                    c.ChargeMove(direction,Mathf.Max(2,c.Settings.ChargeSpeed)*step,out bool blocked);
                    float width=ChargeWidth;
                    if(!hit&&EnemyController.SweptHitXZ(previous,c.Position,c.Player.transform.position,width)){
                        hit=true;c.Player.TakeDamage(c.Damage,c.Player.transform.position-direction*2,8);
                    }
                    if(blocked){c.HideWarnings();Enter(SharkBossAction.Stunned,Mathf.Max(1,c.Settings.StunSeconds));c.Pose(BossPose.Stunned,0);return;}
                    if(age>=duration){
                        c.HideWarnings();chargesLeft--;
                        if(chargesLeft>0)BeginCharge(aim);
                        else Recover(Mathf.Max(doubleCharge?1.6f:1,c.Settings.Recovery));
                    }
                    return;
                case SharkBossAction.BiteWindup:
                    c.Face(direction,dt);DrawBite();c.Pose(BossPose.Windup,Progress);
                    if(age>=duration){Enter(SharkBossAction.Biting,.28f);hit=false;}
                    return;
                case SharkBossAction.Biting:
                    DrawBite();c.Pose(bite==0?BossPose.BiteLeft:bite==1?BossPose.BiteRight:BossPose.BiteHeavy,Progress);
                    if(!hit&&Progress>=.35f){
                        hit=true;c.ConeHit(c.Position,direction,BiteRange,BiteAngle,bite==2?1.2f:.75f,bite==2?8:3);
                    }
                    if(age>=duration){
                        c.HideWarnings();bite++;
                        if(bite<3)BeginBite(aim);else Recover(Mathf.Max(1,c.Settings.Recovery));
                    }
                    return;
                case SharkBossAction.StompWindup:
                    c.Pose(BossPose.Stomp,Progress*.5f);c.WarnCircle(0,c.Position,Mathf.Max(3,c.Settings.StompRadius),Progress);
                    if(age>=duration){Enter(SharkBossAction.Stomping,.38f);stompSent=false;}
                    return;
                case SharkBossAction.Stomping:
                    c.Pose(BossPose.Stomp,.5f+Progress*.5f);
                    if(!stompSent&&Progress>=.25f){
                        stompSent=true;c.HideWarnings();
                        c.Shockwave(c.Position,Mathf.Max(3,c.Settings.StompRadius),Mathf.Max(2,c.Settings.ShockwaveSpeed),1);
                    }
                    if(age>=duration)Recover(Mathf.Max(1.1f,c.Settings.Recovery));
                    return;
                case SharkBossAction.SpinWindup:
                    c.Face(direction,dt);c.Pose(BossPose.Windup,Progress);
                    c.WarnCircle(0,c.Position,SpinRadius,Progress);
                    if(age>=duration){Enter(SharkBossAction.Spinning,Mathf.Max(.8f,c.Settings.SpinSeconds));contactClock=0;}
                    return;
                case SharkBossAction.Spinning:
                    // The dangerous disc remains visible and moves no faster than an unupgraded hero.
                    c.Face(aim,dt);c.Move(aim*Mathf.Min(c.Speed*.55f*dt,Mathf.Max(0,distance-.3f)));
                    c.Pose(BossPose.Spin,Progress);c.WarnCircle(0,c.Position,SpinRadius,1);
                    contactClock-=dt;
                    if(contactClock<=0){contactClock=.65f;c.CircleHit(c.Position,SpinRadius,.65f,4);}
                    if(age>=duration)Recover(Mathf.Max(1.8f,c.Settings.Recovery));
                    return;
                case SharkBossAction.Stunned:
                    c.Pose(BossPose.Stunned,Progress);
                    if(age>=duration){state=SharkBossAction.Approach;age=0;wait=Mathf.Max(.4f,c.Settings.AttackInterval*.5f/Tempo);}
                    return;
                case SharkBossAction.Recovery:
                    c.Pose(BossPose.Recover,Progress);
                    if(age>=duration){state=SharkBossAction.Approach;age=0;wait=Mathf.Max(.25f,c.Settings.AttackInterval*(c.PhaseTwo?.75f:1)/Tempo);}
                    return;
            }
        }
        float Tempo=>c.Settings.TempoAt(c.Wave);
        float Progress=>duration>.00001f?Mathf.Clamp01(age/duration):1;
        float ChargeWidth=>c.Radius+.48f;
        float SpinRadius=>Mathf.Max(c.Radius+.7f,c.Settings.SpinRadius);
        float BiteRange=>Mathf.Max(c.Radius+1,Mathf.Max(2,c.Settings.BiteRange)*(bite==2?1.25f:1));
        float BiteAngle=>bite==2?125:100;
        void BeginCharge(Vector3 aim){
            direction=aim;Enter(SharkBossAction.ChargeWindup,Mathf.Max(.5f,c.Settings.ChargeWindup/Tempo));DrawCharge();
        }
        void DrawCharge(){
            float remaining=state==SharkBossAction.Charging?Mathf.Max(0,duration-age):Mathf.Max(.2f,c.Settings.ChargeSeconds);
            c.WarnLane(0,c.Position,direction,Mathf.Max(2,c.Settings.ChargeSpeed)*remaining,ChargeWidth,state==SharkBossAction.Charging?1:Progress);
        }
        void BeginBite(Vector3 aim){
            direction=Quaternion.AngleAxis(bite==0?-22:bite==1?22:0,Vector3.up)*aim;
            Enter(SharkBossAction.BiteWindup,Mathf.Max(bite==2?.65f:.3f,(bite==2?c.Settings.FinalBiteWindup:c.Settings.BiteWindup)/Tempo));DrawBite();
        }
        void DrawBite(){c.WarnCone(0,c.Position,direction,BiteRange,BiteAngle,state==SharkBossAction.Biting?1:Progress);}
        void BeginStomp(Vector3 aim){direction=aim;c.Face(aim,1);Enter(SharkBossAction.StompWindup,Mathf.Max(.7f,c.Settings.Windup/Tempo));c.WarnCircle(0,c.Position,Mathf.Max(3,c.Settings.StompRadius),0);}
        void BeginSpin(Vector3 aim){direction=aim;Enter(SharkBossAction.SpinWindup,Mathf.Max(.8f,c.Settings.Windup/Tempo));c.WarnCircle(0,c.Position,SpinRadius,0);}
        void Recover(float seconds){c.HideWarnings();Enter(SharkBossAction.Recovery,seconds);}
        void Enter(SharkBossAction next,float seconds){state=next;age=0;duration=Mathf.Max(.05f,seconds);}
        static Vector3 Flat(Vector3 value){value.y=0;return value;}
    }
}
