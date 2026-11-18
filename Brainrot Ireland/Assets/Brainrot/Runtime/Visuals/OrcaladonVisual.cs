using UnityEngine;
namespace Brainrot
{
    /// <summary>Visual-only poses for the owner's corrected Orcaladon rig; gameplay stays on the actor root.</summary>
    public sealed class OrcaladonVisual : MonoBehaviour
    {
        public Transform ModelRoot,MotionRoot,Body,UpperLeft,UpperRight,ForeLeft,ForeRight,ThighLeft,ThighRight,ShinLeft,ShinRight;
        Transform[] bones;Quaternion[] rest;Vector3[] restPositions;Vector3 motionPosition;Quaternion motionRotation;
        OrcaladonAttack attack;EnemyController enemy;
        float phase,upper,fore,lean,crouch;
        void Awake(){Cache();}
        void Cache(){
            if(bones!=null)return;
            bones=new[]{Body,UpperLeft,UpperRight,ForeLeft,ForeRight,ThighLeft,ThighRight,ShinLeft,ShinRight};rest=new Quaternion[bones.Length];restPositions=new Vector3[bones.Length];
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null){rest[i]=bones[i].localRotation;restPositions[i]=bones[i].localPosition;}
            if(MotionRoot!=null){motionPosition=MotionRoot.localPosition;motionRotation=MotionRoot.localRotation;}
            enemy=GetComponentInParent<EnemyController>();
        }
        // FBX inspection: shoulder-to-elbow vectors are nearly horizontal (about 11° down).
        // A mirrored actor-space roll lowers both arms without changing bind poses or stretching bones.
        public const float RelaxedArmRoll=22;
        public void RestoreBindPose(){
            Cache();phase=upper=fore=lean=crouch=0;
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null){bones[i].localRotation=rest[i];bones[i].localPosition=restPositions[i];}
            if(MotionRoot!=null){MotionRoot.localPosition=motionPosition;MotionRoot.localRotation=motionRotation;}
        }
        public void ResetPose(){
            RestoreBindPose();Rotate(1,0,0,-RelaxedArmRoll);Rotate(2,0,0,RelaxedArmRoll);
        }
        public void Pose(float dt,float movement){
            Cache();if(ModelRoot==null||dt<=0)return;
            if(enemy!=null&&(!enemy.Alive||(enemy.PhysicsBody!=null&&enemy.PhysicsBody.IsPhysical)))return;
            if(attack==null)attack=GetComponentInParent<OrcaladonAttack>();
            var state=attack!=null?attack.Phase:OrcaladonPhase.Approach;
            float progress=attack!=null?attack.PhaseProgress:0;
            float gait=Mathf.Clamp01(movement/2.2f),upperTarget=0,foreTarget=0,leanTarget=0,crouchTarget=0;
            bool charging=state==OrcaladonPhase.Charging;
            if(charging)gait=1;
            phase=Mathf.Repeat(phase+dt*(charging?17:Mathf.Lerp(1.5f,5.8f,gait)),Mathf.PI*4);
            switch(state){
                case OrcaladonPhase.ChargeAim:upperTarget=25;foreTarget=-28;leanTarget=12;gait=0;break;
                case OrcaladonPhase.Charging:upperTarget=34;foreTarget=-35;leanTarget=20;break;
                case OrcaladonPhase.SlamWindup:
                    float wind=Mathf.SmoothStep(0,1,progress/.6f);
                    float strike=Mathf.SmoothStep(0,1,(progress-.68f)/.32f);
                    upperTarget=Mathf.Lerp(-142*wind,12,strike);foreTarget=Mathf.Lerp(28*wind,25,strike);leanTarget=Mathf.Lerp(-9*wind,21,strike);crouchTarget=.32f*strike;gait=0;break;
                case OrcaladonPhase.SlamImpact:upperTarget=12;foreTarget=25;leanTarget=21;crouchTarget=.32f;gait=0;break;
                case OrcaladonPhase.Recovery:gait=0;break;
            }
            float blend=1-Mathf.Exp(-(state==OrcaladonPhase.SlamImpact?32:12)*dt);
            // AI applies damage on entry to SlamImpact: hands must already be down at that exact boundary.
            if(state==OrcaladonPhase.SlamImpact||(state==OrcaladonPhase.SlamWindup&&progress>=.68f))blend=1;
            upper=Mathf.Lerp(upper,upperTarget,blend);fore=Mathf.Lerp(fore,foreTarget,blend);lean=Mathf.Lerp(lean,leanTarget,blend);crouch=Mathf.Lerp(crouch,crouchTarget,blend);
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null){bones[i].localRotation=rest[i];bones[i].localPosition=restPositions[i];}
            float stride=Mathf.Sin(phase)*gait;
            Rotate(0,lean,0,Mathf.Sin(phase*.5f)*1.3f*(1-gait));
            float armRoll=RelaxedArmRoll;
            if(state==OrcaladonPhase.SlamWindup)armRoll=Mathf.Lerp(RelaxedArmRoll,3,Mathf.SmoothStep(0,1,progress/.6f));
            else if(state==OrcaladonPhase.Charging||state==OrcaladonPhase.ChargeAim||state==OrcaladonPhase.SlamImpact)armRoll=12;
            Rotate(1,upper-stride*13,0,-armRoll);Rotate(2,upper+stride*13,0,armRoll);
            Rotate(3,fore-Mathf.Max(0,stride)*9,0,0);Rotate(4,fore-Mathf.Max(0,-stride)*9,0,0);
            Rotate(5,stride*23,0,0);Rotate(6,-stride*23,0,0);
            Rotate(7,Mathf.Max(0,-stride)*19,0,0);Rotate(8,Mathf.Max(0,stride)*19,0,0);
            if(Body!=null&&crouch>0){
                // Compress the torso into the impact while keeping both legs planted.
                Vector3 left=ThighLeft!=null?ThighLeft.position:Vector3.zero,right=ThighRight!=null?ThighRight.position:Vector3.zero;
                Body.position-=transform.TransformVector(Vector3.up*crouch);
                if(ThighLeft!=null)ThighLeft.position=left;if(ThighRight!=null)ThighRight.position=right;
            }
            if(MotionRoot!=null){
                float impact=state==OrcaladonPhase.SlamImpact?Mathf.Sin(progress*Mathf.PI)*.11f:0;
                MotionRoot.localPosition=motionPosition+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.035f*gait-impact);
                MotionRoot.localRotation=motionRotation*Quaternion.Euler(0,0,stride*1.8f);
            }
        }
        void Rotate(int index,float pitch,float yaw,float roll){
            var bone=bones[index];if(bone==null||bone.parent==null)return;
            var parent=bone.parent;
            var rotation=Quaternion.AngleAxis(pitch,parent.InverseTransformDirection(transform.right))*Quaternion.AngleAxis(yaw,parent.InverseTransformDirection(transform.up))*Quaternion.AngleAxis(roll,parent.InverseTransformDirection(transform.forward));
            bone.localRotation=rotation*rest[index];
        }
        void OnDisable(){ResetPose();}
    }
}
