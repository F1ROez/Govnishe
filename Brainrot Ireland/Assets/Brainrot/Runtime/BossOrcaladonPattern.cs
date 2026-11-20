using UnityEngine;
namespace Brainrot
{
    public enum BossOrcaStage
    {
        Approach, QuakeWindup, QuakeStrike, QuakeFinish,
        GrabWindup, GrabClose, Holding, Throwing,
        ChargeWindup, ChargeRun, ChargeBrake, ChargeRedirect,
        FistWindup, FistStrike, FistGap, Recovery, Stunned
    }

    /// <summary>A heavy, committed encounter. Each warning and strike occupies a separate tick,
    /// so even a long frame cannot choose and resolve an unshown attack together.</summary>
    public sealed class BossOrcaladonPattern : IBossPattern
    {
        readonly BossEncounter encounter;
        static readonly int[] SecondPhaseOrder={2,0,3,2,0,1};
        Vector3 committed,origin;
        float clock,duration=1,runDistance;
        int sequence,fist,chargeLeg;
        bool chargeHit;
        public BossOrcaStage Stage{get;private set;}
        public float Progress=>Mathf.Clamp01(clock/Mathf.Max(.001f,duration));
        public Vector3 CommittedDirection=>committed;
        public int CompletedAttacks{get;private set;}
        public static int AttackInSequence(int sequence,bool secondPhase)
        {
            sequence=Mathf.Max(0,sequence);
            return secondPhase?SecondPhaseOrder[sequence%SecondPhaseOrder.Length]:sequence%3;
        }
        BossFoeTuning T=>encounter.Settings;
        float Width=>Mathf.Max(encounter.Radius+.48f,Mathf.Max(.5f,T.ChargeWidth));
        float AreaRadius=>Mathf.Max(encounter.Radius+1,Mathf.Clamp(T.AreaRadius,3,14));
        float MeleeRange=>Mathf.Max(encounter.Radius+.75f,Mathf.Clamp(T.MeleeRange,2,9));
        float GrabRange=>Mathf.Max(encounter.Radius+.6f,Mathf.Clamp(T.GrabRange,1.5f,7));
        float Tempo=>Mathf.Clamp(T.TempoAt(encounter.Wave),1,1.25f);
        float RunSpeed=>Mathf.Clamp(T.ChargeSpeed,4,24);
        float TotalChargeDistance=>Mathf.Clamp(T.ChargeDistance,5,24);
        public BossOrcaladonPattern(BossEncounter owner){encounter=owner;Reset();}
        public void Reset()
        {
            sequence=fist=chargeLeg=CompletedAttacks=0;chargeHit=false;
            committed=Vector3.forward;origin=Vector3.zero;runDistance=0;
            Enter(BossOrcaStage.Approach,1.2f);encounter?.HideWarnings();
        }
        public void Tick(float dt)
        {
            if(encounter==null||encounter.Enemy==null||!encounter.Enemy.Alive||encounter.Player==null||encounter.Player.Health<=0||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            float used=Mathf.Min(dt,Mathf.Max(0,duration-clock));
            clock=Mathf.Min(duration,clock+dt);
            var delta=Flat(encounter.Player.transform.position-encounter.Position);
            var direction=delta.sqrMagnitude>.00001f?delta.normalized:encounter.Enemy.transform.forward;
            switch(Stage)
            {
                case BossOrcaStage.Approach:
                    int choice=AttackInSequence(sequence,encounter.PhaseTwo);
                    float desired=choice==2?Mathf.Min(12,TotalChargeDistance*.7f):choice==0?AreaRadius*.72f:choice==1?GrabRange*.86f:MeleeRange*.88f;
                    if(delta.magnitude>desired){
                        encounter.Move(direction*Mathf.Min(encounter.Speed*dt,delta.magnitude-desired));
                        encounter.Face(direction,dt);encounter.Pose(BossPose.Run,0);return;
                    }
                    encounter.Face(direction,dt);encounter.Pose(BossPose.Idle,0);
                    if(Progress<1)return;
                    sequence++;committed=direction;origin=encounter.Position;
                    encounter.Face(committed,1);
                    if(choice==0){Enter(BossOrcaStage.QuakeWindup,Mathf.Max(.8f,T.Windup/Tempo));DrawQuake(0);}
                    else if(choice==1){Enter(BossOrcaStage.GrabWindup,Mathf.Max(.75f,T.Windup*.85f/Tempo));DrawGrab(0);}
                    else if(choice==2){chargeLeg=0;chargeHit=false;Enter(BossOrcaStage.ChargeWindup,Mathf.Max(.9f,T.Windup/Tempo));DrawCharge(0,TotalChargeDistance*.55f);}
                    else BeginFist(0);
                    return;
                case BossOrcaStage.QuakeWindup:
                    DrawQuake(Progress);encounter.Pose(BossPose.Slam,Progress*.65f);
                    if(Progress>=1)Enter(BossOrcaStage.QuakeStrike,.20f);
                    return;
                case BossOrcaStage.QuakeStrike:
                    DrawQuake(1);encounter.Pose(BossPose.Slam,.65f+Progress*.35f);
                    if(Progress>=1){
                        encounter.HideWarnings();
                        // Independent visible annuli leave gaps; no full-disk invisible area hit.
                        float velocity=Mathf.Clamp(T.ShockwaveSpeed,4,16);
                        encounter.Shockwave(origin,AreaRadius,velocity,1,0);
                        encounter.Shockwave(origin,AreaRadius,velocity,1,.95f);
                        encounter.Shockwave(origin,AreaRadius,velocity,1,1.90f);
                        Enter(BossOrcaStage.QuakeFinish,2.15f);
                    }
                    return;
                case BossOrcaStage.QuakeFinish:
                    encounter.Pose(BossPose.Recover,Progress);
                    if(Progress>=1)Recover(Mathf.Max(1.6f,T.Recovery));
                    return;
                case BossOrcaStage.GrabWindup:
                    DrawGrab(Progress);encounter.Pose(BossPose.Grab,Progress*.65f);
                    if(Progress>=1)Enter(BossOrcaStage.GrabClose,.18f);
                    return;
                case BossOrcaStage.GrabClose:
                    DrawGrab(1);encounter.Pose(BossPose.Grab,.65f+Progress*.35f);
                    if(Progress>=1){
                        encounter.HideWarnings();
                        if(Flat(encounter.Player.transform.position-origin).sqrMagnitude<=GrabRange*GrabRange&&encounter.TryCapture(Mathf.Clamp(T.GrabSeconds,1,4),true,GrabRange))
                            Enter(BossOrcaStage.Holding,Mathf.Clamp(T.GrabSeconds,1,4)+1);
                        else Recover(Mathf.Max(1.4f,T.Recovery));
                    }
                    return;
                case BossOrcaStage.Holding:
                    encounter.Pose(encounter.CaptureProgress>.75f?BossPose.Throw:BossPose.Grab,encounter.CaptureProgress>.75f?Mathf.InverseLerp(.75f,1,encounter.CaptureProgress):1);
                    if(encounter.CaptureFinished||!encounter.Capturing){
                        if(encounter.CaptureEscaped)Stun(Mathf.Max(1.2f,T.StunSeconds));
                        else Enter(BossOrcaStage.Throwing,.30f);
                    }
                    return;
                case BossOrcaStage.Throwing:
                    encounter.Pose(BossPose.Throw,.85f+Progress*.15f);
                    if(Progress>=1)Recover(Mathf.Max(1.6f,T.Recovery));
                    return;
                case BossOrcaStage.ChargeWindup:
                    // Track only the early preparation; the final 45% is fully committed.
                    if(Progress<.55f){committed=direction;encounter.Face(committed,dt);}
                    DrawCharge(Progress,TotalChargeDistance*.55f);encounter.Pose(BossPose.Windup,Progress);
                    if(Progress>=1)BeginRun(TotalChargeDistance*.55f);
                    return;
                case BossOrcaStage.ChargeRun:
                    AdvanceCharge(RunSpeed*used);
                    encounter.Pose(BossPose.Charge,Progress);
                    if(Stage!=BossOrcaStage.ChargeRun)return;
                    if(Progress>=1){
                        if(chargeLeg==0)Enter(BossOrcaStage.ChargeBrake,.30f);
                        else Recover(Mathf.Max(1.6f,T.Recovery));
                    }
                    return;
                case BossOrcaStage.ChargeBrake:
                    AdvanceCharge(RunSpeed*.25f*(1-Progress)*used);
                    if(Stage!=BossOrcaStage.ChargeBrake)return;
                    encounter.Pose(BossPose.Recover,Progress*.5f);
                    if(Progress>=1){
                        // One visible steering correction, at rest, never a homing dash.
                        committed=Vector3.RotateTowards(committed,direction,Mathf.PI*.5f,0).normalized;
                        origin=encounter.Position;chargeLeg=1;
                        Enter(BossOrcaStage.ChargeRedirect,Mathf.Max(.7f,T.Windup*.7f/Tempo));
                        DrawCharge(0,TotalChargeDistance*.45f);
                    }
                    return;
                case BossOrcaStage.ChargeRedirect:
                    encounter.Face(committed,dt);DrawCharge(Progress,TotalChargeDistance*.45f);
                    encounter.Pose(BossPose.Windup,Progress);
                    if(Progress>=1)BeginRun(TotalChargeDistance*.45f);
                    return;
                case BossOrcaStage.FistWindup:
                    DrawFist(Progress);encounter.Pose(FistPose(),Progress*.65f);
                    if(Progress>=1)Enter(BossOrcaStage.FistStrike,fist==2?.22f:.16f);
                    return;
                case BossOrcaStage.FistStrike:
                    DrawFist(1);encounter.Pose(FistPose(),.65f+Progress*.35f);
                    if(Progress>=1){
                        float range=FistRange(),arc=fist==2?110:68;
                        encounter.ConeHit(origin,committed,range,arc,fist==2?1.15f:.7f,Mathf.Max(0,T.Knockback)*(fist==2?1:.5f));
                        encounter.HideWarnings();GameAudio.Impact(fist==2,false,1);
                        if(fist==2)Recover(Mathf.Max(2,T.Recovery));else Enter(BossOrcaStage.FistGap,.25f);
                    }
                    return;
                case BossOrcaStage.FistGap:
                    encounter.Pose(BossPose.Recover,Progress);
                    if(Progress>=1)BeginFist(fist+1);
                    return;
                case BossOrcaStage.Recovery:
                    encounter.Pose(BossPose.Recover,Progress);
                    if(Progress>=1){CompletedAttacks++;Enter(BossOrcaStage.Approach,Mathf.Max(.55f,T.AttackInterval/Tempo));}
                    return;
                case BossOrcaStage.Stunned:
                    encounter.Pose(BossPose.Stunned,Progress);
                    if(Progress>=1){CompletedAttacks++;Enter(BossOrcaStage.Approach,Mathf.Max(.65f,T.AttackInterval/Tempo));}
                    return;
            }
        }
        void Enter(BossOrcaStage stage,float seconds){Stage=stage;duration=Mathf.Max(.05f,seconds);clock=0;}
        void Recover(float seconds){encounter.HideWarnings();Enter(BossOrcaStage.Recovery,seconds);}
        void Stun(float seconds){encounter.HideWarnings();Enter(BossOrcaStage.Stunned,seconds);}
        void DrawQuake(float progress){encounter.WarnCircle(0,origin,AreaRadius,progress);}
        void DrawGrab(float progress){encounter.WarnCircle(0,origin,GrabRange,progress,BossWarning.Special);}
        void DrawCharge(float progress,float distance){encounter.WarnLane(0,encounter.Position,committed,distance,Width,progress);}
        void BeginRun(float distance){encounter.HideWarnings();runDistance=distance;Enter(BossOrcaStage.ChargeRun,distance/RunSpeed);}
        void AdvanceCharge(float distance)
        {
            if(distance<=0)return;
            var before=encounter.Position;encounter.ChargeMove(committed,Mathf.Min(runDistance,distance),out bool blocked);
            var after=encounter.Position;runDistance=Mathf.Max(0,runDistance-Flat(after-before).magnitude);
            if(!chargeHit&&EnemyController.SweptHitXZ(before,after,encounter.Player.transform.position,Width)){
                chargeHit=true;
                encounter.Hit(encounter.Player.transform.position-committed*2,1,Mathf.Max(0,T.Knockback));
            }
            if(blocked)Recover(Mathf.Max(1.5f,T.Recovery));
        }
        void BeginFist(int index)
        {
            fist=index;origin=encounter.Position;
            var aim=Flat(encounter.Player.transform.position-origin);if(aim.sqrMagnitude<.00001f)aim=encounter.Enemy.transform.forward;
            committed=(Quaternion.Euler(0,index==0?-18:index==1?18:0,0)*aim.normalized).normalized;
            encounter.Face(committed,1);
            Enter(BossOrcaStage.FistWindup,Mathf.Max(index==2?.8f:.4f,(index==2?T.FinalWindup:T.ComboWindup)/Tempo));
            DrawFist(0);
        }
        BossPose FistPose()=>fist==0?BossPose.RightPunch:fist==1?BossPose.LeftPunch:BossPose.DoublePunch;
        float FistRange()=>fist==2?Mathf.Min(11,MeleeRange*1.4f):MeleeRange;
        void DrawFist(float progress){encounter.WarnCone(0,origin,committed,FistRange(),fist==2?110:68,progress);}
        static Vector3 Flat(Vector3 value){value.y=0;return value;}
    }
}
