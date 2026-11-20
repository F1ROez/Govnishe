using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Original bat choreography in actor space. Joint-position IK avoids imported FBX bone-axis/scale assumptions.</summary>
    sealed class SahurCombatPose
    {
        readonly ImportedSahurVisual owner;
        readonly Transform rightUpper,rightFore,rightHand,leftUpper,leftFore,leftHand,chest,head;
        readonly Transform[] bones;
        readonly Vector3[] positions,scales;
        readonly Quaternion[] rotations;
        bool applied;
        public bool Valid=>rightUpper!=null&&rightFore!=null&&rightHand!=null&&owner.Weapon!=null;
        public SahurCombatPose(ImportedSahurVisual visual){
            owner=visual;rightUpper=visual.RightUpperArm;rightFore=visual.RightForearm;rightHand=visual.Grip;
            leftUpper=ImportedSahurVisual.Find(visual.ModelRoot,"upper_arm.L");leftFore=ImportedSahurVisual.Find(visual.ModelRoot,"forearm.L");leftHand=ImportedSahurVisual.Find(visual.ModelRoot,"hand.L");
            chest=ImportedSahurVisual.Find(visual.ModelRoot,"spine.002");head=ImportedSahurVisual.Find(visual.ModelRoot,"spine.006");
            var changed=new List<Transform>();
            foreach(var bone in new[]{chest,head,rightUpper,rightFore,rightHand,leftUpper,leftFore,leftHand,visual.Weapon})if(bone!=null&&!changed.Contains(bone))changed.Add(bone);
            bones=changed.ToArray();positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];scales=new Vector3[bones.Length];
        }
        void Capture(){
            for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;scales[i]=bones[i].localScale;}
            applied=true;
        }
        public void Restore(){
            if(!applied)return;
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];bones[i].localScale=scales[i];}
            applied=false;
        }
        public void Charge(float progress){
            if(!Valid)return;
            var tuning=GameBalance.Current.SahurAnimation;
            Pose(true,0,0,Mathf.SmoothStep(0,1,Mathf.Clamp01(progress)),tuning);
        }
        public void Swing(float elapsed,float duration,float contact,bool heavy,bool fromCharge){
            if(!Valid)return;
            float before=contact>.00001f?Mathf.Clamp01(elapsed/contact):1;
            float after=elapsed<=contact?0:Mathf.Clamp01((elapsed-contact)/Mathf.Max(.00001f,duration-contact));
            float inTime=Mathf.Min(.035f,Mathf.Max(.005f,contact*.6f));
            float weight=heavy&&fromCharge?1:Mathf.SmoothStep(0,1,elapsed/inTime);
            weight*=Mathf.SmoothStep(0,1,(duration-elapsed)/Mathf.Min(.075f,duration*.24f));
            // Accelerate into the contact, then carry momentum into follow-through rather than
            // easing to a dead stop exactly when damage is delivered.
            float drive=Mathf.Pow(before,heavy?2.2f:1.7f);
            float follow=1-Mathf.Pow(1-after,3);
            Pose(heavy,drive,follow,weight,GameBalance.Current.SahurAnimation);
        }
        /// <summary>Overhand release, separate from either bat swing. All changes are restored before Animator evaluates.</summary>
        public void Throw(float elapsed,float duration,float releaseDelay){
            if(!Valid)return;
            Capture();
            var actor=owner.transform;Vector3 up=actor.up,right=actor.right,forward=actor.forward;
            if(leftUpper!=null){var anatomical=Vector3.ProjectOnPlane(rightUpper.position-leftUpper.position,up);if(anatomical.sqrMagnitude>.000001f)right=anatomical.normalized;}
            float arm=Vector3.Distance(rightUpper.position,rightFore.position)+Vector3.Distance(rightFore.position,rightHand.position);
            if(arm<.00001f){Restore();return;}
            duration=Mathf.Max(.2f,duration);releaseDelay=Mathf.Clamp(releaseDelay,.05f,duration*.65f);
            float windEnd=releaseDelay*.55f;
            float wind=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/windEnd));
            float drive=Mathf.Pow(Mathf.Clamp01((elapsed-windEnd)/Mathf.Max(.01f,releaseDelay-windEnd)),1.7f);
            float follow=1-Mathf.Pow(1-Mathf.Clamp01((elapsed-releaseDelay)/Mathf.Max(.01f,duration-releaseDelay)),2);
            float weight=Mathf.SmoothStep(0,1,elapsed/.045f)*Mathf.SmoothStep(0,1,(duration-elapsed)/Mathf.Min(.13f,duration*.30f));
            Quaternion wrist=rightHand.rotation,leftWrist=leftHand!=null?leftHand.rotation:Quaternion.identity,headWorld=head!=null?head.rotation:Quaternion.identity;
            if(chest!=null){float twist=Mathf.Lerp(24*wind,-12,drive)+follow*5;float lean=Mathf.Lerp(-5*wind,10,drive)-follow*4;chest.rotation=Quaternion.AngleAxis(twist,up)*Quaternion.AngleAxis(lean,right)*chest.rotation;}
            if(head!=null)head.rotation=Quaternion.Slerp(head.rotation,headWorld,.75f);
            Vector3 shoulder=rightUpper.position;
            Vector3 windGoal=shoulder+right*(arm*.25f)+up*(arm*.28f)-forward*(arm*.50f);
            Vector3 releaseGoal=shoulder+forward*(arm*.88f)+right*(arm*.06f)+up*(arm*.13f);
            Vector3 followGoal=shoulder+forward*(arm*.62f)-up*(arm*.39f)-right*(arm*.10f);
            Vector3 goal=elapsed<windEnd?Vector3.Lerp(rightHand.position,windGoal,wind):elapsed<=releaseDelay?Vector3.Lerp(windGoal,releaseGoal,drive):Vector3.Lerp(releaseGoal,followGoal,follow);
            Vector3 axis=Vector3.Slerp((up-forward*.20f).normalized,(forward+up*.20f).normalized,drive);
            axis=Vector3.Slerp(axis,(forward-up*.45f).normalized,follow);
            SolveArm(rightUpper,rightFore,rightHand,goal,right+up*.4f-forward*.15f,Quaternion.FromToRotation(up,axis)*wrist);
            if(leftUpper!=null&&leftFore!=null&&leftHand!=null){
                var balance=leftUpper.position-right*(arm*.30f)-up*(arm*.43f)+forward*(arm*Mathf.Lerp(.20f,-.22f,drive));
                SolveArm(leftUpper,leftFore,leftHand,balance,-right-forward*.2f-up*.4f,leftWrist);
            }
            owner.Weapon.position=rightHand.position+forward*(arm*.035f);owner.Weapon.rotation=BatRotation(axis,forward,up);
            for(int i=0;i<bones.Length;i++){
                bones[i].localPosition=Vector3.Lerp(positions[i],bones[i].localPosition,weight);
                bones[i].localRotation=Quaternion.Slerp(rotations[i],bones[i].localRotation,weight);
            }
        }
        void Pose(bool twoHands,float before,float after,float weight,SahurAnimationTuning tuning){
            Capture();
            var actor=owner.transform;Vector3 up=actor.up,right=actor.right,forward=actor.forward;
            if(leftUpper!=null){var anatomicalRight=Vector3.ProjectOnPlane(rightUpper.position-leftUpper.position,up);if(anatomicalRight.sqrMagnitude>.00000001f)right=anatomicalRight.normalized;}
            float arm=Vector3.Distance(rightUpper.position,rightFore.position)+Vector3.Distance(rightFore.position,rightHand.position);
            if(arm<.00001f){Restore();return;}
            Quaternion rightWrist=rightHand.rotation,leftWrist=leftHand!=null?leftHand.rotation:Quaternion.identity;
            Quaternion headWorld=head!=null?head.rotation:Quaternion.identity;
            float twist=twoHands?Mathf.Lerp(16,-19,before)+after*7:Mathf.Lerp(tuning.TorsoTwistDegrees,0,before)-after*tuning.TorsoTwistDegrees*.8f;
            float lean=twoHands?Mathf.Lerp(-9,15,before)+after*3:Mathf.Lerp(-2,5,before);
            if(chest!=null)chest.rotation=Quaternion.AngleAxis(twist,up)*Quaternion.AngleAxis(lean,right)*chest.rotation;
            if(head!=null)head.rotation=Quaternion.Slerp(head.rotation,headWorld,.7f);
            Vector3 center=leftUpper!=null?(rightUpper.position+leftUpper.position)*.5f:rightUpper.position-right*arm*.4f;
            Vector3 wind,impact,follow,axis;
            float stretch=Mathf.Clamp(tuning.SwingStretch,.5f,1.5f);
            if(twoHands){
                wind=center+right*(arm*.18f)+up*(arm*.23f*Mathf.Clamp(tuning.ChargeLift,.3f,1.5f))+forward*(arm*.55f);
                impact=center+right*(arm*.12f)+forward*(arm*.62f*stretch)-up*(arm*.15f);
                follow=center+right*(arm*.06f)+forward*(arm*.48f*stretch)-up*(arm*.52f);
                float start=Mathf.Clamp(tuning.HeavyArcDegrees,80,240)*.62f;
                float end=-Mathf.Clamp(tuning.HeavyArcDegrees,80,240)*.38f;
                float pitch=after>0?Mathf.Lerp(8,end,after):Mathf.Lerp(start,8,before);
                float radians=pitch*Mathf.Deg2Rad;
                // Carry the raised barrel beside the head rather than through Sahur's unusually tall face.
                axis=(forward*Mathf.Cos(radians)+up*Mathf.Sin(radians)+right*((1-before)*.85f)).normalized;
            }
            else{
                wind=rightUpper.position+right*(arm*.23f)+up*(arm*.12f)-forward*(arm*.55f);
                impact=center+right*(arm*.12f)+forward*(arm*.7f*stretch)-up*(arm*.18f);
                follow=center-right*(arm*.36f)+forward*(arm*.27f*stretch)-up*(arm*.3f);
                float yaw=after>0?Mathf.Lerp(0,-tuning.NormalArcDegrees*.45f,after):Mathf.Lerp(tuning.NormalArcDegrees*.55f,0,before);
                float radians=yaw*Mathf.Deg2Rad;
                axis=(forward*Mathf.Cos(radians)+right*Mathf.Sin(radians)+up*Mathf.Lerp(.38f,.12f,before)).normalized;
            }
            Vector3 goal=after>0?Vector3.Lerp(impact,follow,after):Vector3.Lerp(wind,impact,before);
            Quaternion wristTurn=Quaternion.FromToRotation(up,axis);
            SolveArm(rightUpper,rightFore,rightHand,goal,right+forward*.12f-up*.35f,wristTurn*rightWrist);
            if(twoHands&&leftUpper!=null&&leftFore!=null&&leftHand!=null){
                Vector3 secondGrip=rightHand.position+axis*(arm*.13f);
                SolveArm(leftUpper,leftFore,leftHand,secondGrip,-right+forward*.08f-up*.35f,wristTurn*leftWrist);
            }
            owner.Weapon.position=rightHand.position+forward*(arm*.035f);
            owner.Weapon.rotation=BatRotation(axis,forward,up);
            for(int i=0;i<bones.Length;i++){
                bones[i].localPosition=Vector3.Lerp(positions[i],bones[i].localPosition,weight);
                bones[i].localRotation=Quaternion.Slerp(rotations[i],bones[i].localRotation,weight);
            }
        }
        static Quaternion BatRotation(Vector3 axis,Vector3 forward,Vector3 up){
            Vector3 front=Vector3.ProjectOnPlane(forward,axis);
            if(front.sqrMagnitude<.00001f)front=Vector3.ProjectOnPlane(up,axis);
            return Quaternion.LookRotation(front.normalized,axis);
        }
        static void SolveArm(Transform upper,Transform fore,Transform hand,Vector3 goal,Vector3 pole,Quaternion wrist){
            Vector3 a=upper.position,b=fore.position,c=hand.position;
            float first=Vector3.Distance(a,b),second=Vector3.Distance(b,c),epsilon=Mathf.Max(.000001f,(first+second)*.001f);
            if(first<epsilon||second<epsilon)return;
            Vector3 offset=goal-a;float distance=offset.magnitude;if(distance<epsilon)return;
            Vector3 direction=offset/distance;distance=Mathf.Clamp(distance,Mathf.Abs(first-second)+epsilon,first+second-epsilon);
            Vector3 bend=Vector3.ProjectOnPlane(pole,direction);
            if(bend.sqrMagnitude<epsilon*epsilon)bend=Vector3.ProjectOnPlane(b-a,direction);
            if(bend.sqrMagnitude<epsilon*epsilon)bend=Vector3.Cross(direction,Vector3.forward);
            if(bend.sqrMagnitude<epsilon*epsilon)bend=Vector3.Cross(direction,Vector3.right);
            float cosine=Mathf.Clamp((first*first+distance*distance-second*second)/(2*first*distance),-1,1);
            Vector3 elbow=a+direction*(first*cosine)+bend.normalized*(first*Mathf.Sqrt(Mathf.Max(0,1-cosine*cosine)));
            Vector3 reachableGoal=a+direction*distance;
            upper.rotation=Quaternion.FromToRotation(b-a,elbow-a)*upper.rotation;
            fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,reachableGoal-fore.position)*fore.rotation;hand.rotation=wrist;
        }
    }
}
