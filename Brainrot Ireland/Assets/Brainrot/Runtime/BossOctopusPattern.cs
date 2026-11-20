using UnityEngine;
namespace Brainrot
{
    public enum BossOctopusPhase { Approach, Jump, Tentacles, Ink, GrabAim, Holding, Recovery }

    /// <summary>Spatial-control encounter. Every destination is committed before impact; no hidden contact damage.</summary>
    public sealed class BossOctopusPattern : IBossPattern
    {
        readonly BossEncounter encounter;
        readonly Vector3[] inkTargets=new Vector3[3];
        BossOctopusPhase phase;
        Vector3 start,target;
        float age,duration,nextTentacle;
        int sequence,tentacles;
        bool comboJump,stunned;
        public BossOctopusPhase Phase=>phase;
        public Vector3 CommittedLanding=>target;
        public int TentaclesScheduled=>tentacles;
        BossFoeTuning Settings=>encounter.Settings;
        float AreaRadius=>Mathf.Clamp(Settings.AreaRadius,1.5f,6);
        float HazardRadius=>Mathf.Clamp(Settings.HazardRadius,.7f,2.5f);
        float Progress=>duration>.00001f?Mathf.Clamp01(age/duration):1;

        public BossOctopusPattern(BossEncounter owner){encounter=owner;Reset();}
        public void Reset(){
            phase=BossOctopusPhase.Approach;age=0;duration=1.1f;sequence=tentacles=0;nextTentacle=0;
            target=start=Vector3.zero;comboJump=stunned=false;
        }
        public void Tick(float dt){
            if(encounter==null||encounter.Player==null||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            age+=dt;
            var offset=Flat(encounter.Player.transform.position-encounter.Position);
            float distance=offset.magnitude;
            var direction=distance>.00001f?offset/distance:Vector3.forward;
            switch(phase){
                case BossOctopusPhase.Approach:
                    encounter.Face(direction,dt);
                    if(distance>7){encounter.Move(direction*(encounter.Speed*dt));encounter.Pose(BossPose.Run,age);}
                    else encounter.Pose(BossPose.Idle,age);
                    if(age<duration)return;
                    int attack=sequence++%(encounter.PhaseTwo?4:3);
                    if(attack==1)BeginTentacles();
                    else if(attack==2)BeginInk();
                    else if(attack==3&&distance<=Mathf.Max(2,Settings.CaptureRange)+1)BeginGrab();
                    else BeginJump();
                    return; // A newly created warning must be shown in a rendered frame.
                case BossOctopusPhase.Jump:
                    // The actor remains grounded; the pose driver lifts/extends the body and tentacles.
                    // Locking both points means this jump cannot secretly home onto the player.
                    encounter.Position=Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,Progress));
                    encounter.WarnCircle(0,target,AreaRadius,Progress,BossWarning.Special);
                    encounter.Pose(BossPose.Jump,Progress);
                    if(age<duration)return;
                    encounter.Position=target;
                    encounter.CircleHit(target,AreaRadius,1.1f,8);
                    encounter.Shockwave(target,AreaRadius+3.5f,7,.65f,.18f);
                    Recover(Mathf.Max(1.1f,Settings.Recovery));return;
                case BossOctopusPhase.Tentacles:
                    encounter.Face(direction,dt);encounter.Pose(BossPose.Tentacle,Progress);
                    // Never schedule several fresh warnings in one oversized frame. Each appears
                    // sequentially and receives its own full warning duration in the hazard pool.
                    if(tentacles<3&&age>=nextTentacle){ScheduleTentacle();return;}
                    if(tentacles>=3&&age>=nextTentacle+.95f)Recover(Mathf.Max(1,Settings.Recovery));
                    return;
                case BossOctopusPhase.Ink:
                    encounter.Face(direction,dt);encounter.Pose(BossPose.Spit,Progress);
                    for(int i=0;i<inkTargets.Length;i++)encounter.WarnCircle(i,inkTargets[i],HazardRadius,Progress,BossWarning.Special);
                    if(age<duration)return;
                    for(int i=0;i<inkTargets.Length;i++)encounter.Ink(inkTargets[i],HazardRadius,Mathf.Clamp(Settings.HazardSeconds,2,9),.55f);
                    comboJump=encounter.PhaseTwo;
                    Recover(Mathf.Max(1,Settings.Recovery));return;
                case BossOctopusPhase.GrabAim:
                    // The warning stays on the actual capture radius throughout the windup.
                    encounter.Face(direction,dt);encounter.Pose(BossPose.Grab,Progress);
                    encounter.WarnCircle(0,encounter.Position,Mathf.Max(2,Settings.CaptureRange),Progress,BossWarning.Special);
                    if(age<duration)return;
                    encounter.HideWarnings();
                    if(encounter.TryCapture(Mathf.Max(1,Settings.CaptureSeconds),false,Mathf.Max(2,Settings.CaptureRange))){phase=BossOctopusPhase.Holding;age=0;}
                    else Recover(Mathf.Max(1.3f,Settings.Recovery));
                    return;
                case BossOctopusPhase.Holding:
                    encounter.Pose(BossPose.Grab,.7f);
                    if(encounter.Capturing)return;
                    stunned=encounter.CaptureEscaped;
                    Recover(stunned?Mathf.Max(1.5f,Settings.StunSeconds):Mathf.Max(1.2f,Settings.Recovery));return;
                case BossOctopusPhase.Recovery:
                    encounter.Pose(stunned?BossPose.Stunned:BossPose.Recover,Progress);
                    if(age<duration)return;
                    stunned=false;
                    if(comboJump){comboJump=false;BeginJump();return;}
                    Enter(BossOctopusPhase.Approach,Mathf.Max(.65f,Settings.AttackInterval/Settings.TempoAt(encounter.Wave)));return;
            }
        }
        void BeginJump(){
            encounter.HideWarnings();start=encounter.Position;start.y=0;
            target=LockLanding(encounter.Player.transform.position,AreaRadius);
            encounter.Face(Flat(target-start),1);
            Enter(BossOctopusPhase.Jump,Mathf.Max(1.1f,Settings.JumpWindup/Settings.TempoAt(encounter.Wave)));
            encounter.WarnCircle(0,target,AreaRadius,0,BossWarning.Special);encounter.Pose(BossPose.Jump,0);
        }
        void BeginTentacles(){
            encounter.HideWarnings();tentacles=0;Enter(BossOctopusPhase.Tentacles,2.8f);ScheduleTentacle();
        }
        void ScheduleTentacle(){
            Vector3 point=LockLanding(encounter.Player.transform.position,HazardRadius);
            encounter.Tentacle(point,HazardRadius,Mathf.Max(.7f,Settings.Windup),.85f);
            tentacles++;nextTentacle=age+.5f;
        }
        void BeginInk(){
            encounter.HideWarnings();FillInkTargets(encounter.Player.transform.position,encounter.Position,HazardRadius,inkTargets);
            Enter(BossOctopusPhase.Ink,Mathf.Max(.9f,Settings.Windup));
            for(int i=0;i<inkTargets.Length;i++)encounter.WarnCircle(i,inkTargets[i],HazardRadius,0,BossWarning.Special);
        }
        void BeginGrab(){
            encounter.HideWarnings();Enter(BossOctopusPhase.GrabAim,Mathf.Max(.95f,Settings.Windup));
            encounter.WarnCircle(0,encounter.Position,Mathf.Max(2,Settings.CaptureRange),0,BossWarning.Special);
        }
        void Recover(float seconds){encounter.HideWarnings();Enter(BossOctopusPhase.Recovery,seconds);}
        void Enter(BossOctopusPhase value,float seconds){phase=value;age=0;duration=Mathf.Max(.05f,seconds);}
        static Vector3 Flat(Vector3 value){value.y=0;return value;}
        public static Vector3 LockLanding(Vector3 requested,float radius){
            requested.y=0;return IslandMap.ClampToLand(requested,Mathf.Clamp(radius,.7f,6)+1);
        }
        /// <summary>Three separated ink destinations leave walkable gaps, even when the player hugs the shore.</summary>
        public static void FillInkTargets(Vector3 player,Vector3 boss,float radius,Vector3[] result){
            if(result==null||result.Length<3)return;
            radius=Mathf.Clamp(radius,.7f,2.5f);
            var forward=Flat(player-boss);forward=forward.sqrMagnitude>.00001f?forward.normalized:Vector3.forward;
            var side=Vector3.Cross(Vector3.up,forward);float spacing=radius*2.5f;
            // Inset the shared origin before spreading; clamping each point independently
            // against the coastline could otherwise collapse three puddles onto one position.
            var center=IslandMap.ClampToLand(Flat(player),spacing+radius+1);
            result[0]=center-side*spacing;result[1]=center+forward*spacing;result[2]=center+side*spacing;
        }
    }
}
