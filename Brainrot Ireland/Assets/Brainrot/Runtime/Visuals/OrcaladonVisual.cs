using UnityEngine;
namespace Brainrot
{
    /// <summary>Visual-only layered poses. Gait follows travelled distance; combat contact stays owned by the attack.</summary>
    public sealed class OrcaladonVisual : MonoBehaviour
    {
        public Transform ModelRoot,MotionRoot,Body,UpperLeft,UpperRight,ForeLeft,ForeRight,ThighLeft,ThighRight,ShinLeft,ShinRight;
        public bool NeutralArmsDown;
        Transform[] bones;Quaternion[] rest;Vector3[] restPositions;Vector3 motionPosition;Quaternion motionRotation;
        OrcaladonAttack attack;EnemyController enemy;
        float phase,breathPhase,upper,fore,lean,crouch,gait;
        Vector3 previousPosition;bool trackedPosition;
        void Awake(){Cache();}
        void Cache(){
            if(bones!=null)return;
            bones=new[]{Body,UpperLeft,UpperRight,ForeLeft,ForeRight,ThighLeft,ThighRight,ShinLeft,ShinRight};rest=new Quaternion[bones.Length];restPositions=new Vector3[bones.Length];
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null){rest[i]=bones[i].localRotation;restPositions[i]=bones[i].localPosition;}
            if(MotionRoot!=null){motionPosition=MotionRoot.localPosition;motionRotation=MotionRoot.localRotation;}
            enemy=GetComponentInParent<EnemyController>();
        }
        public const float RelaxedArmRoll=22;
        public void RestoreBindPose(){
            Cache();phase=breathPhase=upper=fore=lean=crouch=gait=0;trackedPosition=false;
            RestoreBones();
            if(MotionRoot!=null){MotionRoot.localPosition=motionPosition;MotionRoot.localRotation=motionRotation;}
        }
        void RestoreBones(){for(int i=0;i<bones.Length;i++)if(bones[i]!=null){bones[i].localRotation=rest[i];bones[i].localPosition=restPositions[i];}}
        public void ResetPose(){RestoreBindPose();if(!NeutralArmsDown){Rotate(1,0,0,-RelaxedArmRoll);Rotate(2,0,0,RelaxedArmRoll);}}
        public void Pose(float dt,float movement){
            Cache();if(attack==null&&enemy!=null)attack=GetComponentInParent<OrcaladonAttack>();
            Pose(dt,movement,attack!=null?attack.Phase:OrcaladonPhase.Approach,attack!=null?attack.PhaseProgress:0);
        }
        /// <summary>Deterministic visual sampling also used by pose previews; does not advance or execute combat.</summary>
        public void Pose(float dt,float movement,OrcaladonPhase state,float progress){
            Cache();if(ModelRoot==null||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            if(enemy!=null&&(!enemy.Alive||(enemy.PhysicsBody!=null&&enemy.PhysicsBody.IsPhysical)))return;
            progress=Mathf.Clamp01(progress);movement=Mathf.Max(0,movement);
            bool charging=state==OrcaladonPhase.Charging,walking=state==OrcaladonPhase.Approach||charging;
            float visualScale=Mathf.Max(.1f,transform.lossyScale.y*GameBalance.Current.Orcaladon.ModelHeight/OrcaladonTuning.DefaultModelHeight),travel=movement*dt;
            if(enemy!=null){
                // Stop cycling legs against a neighbour or during a turn in place. A newly visible
                // enemy cannot convert an entire offscreen journey into one exaggerated step.
                if(trackedPosition){var delta=transform.position-previousPosition;delta.y=0;travel=Mathf.Min(travel,delta.magnitude);}
                previousPosition=transform.position;trackedPosition=true;
            }
            float actualSpeed=travel/dt;
            float targetGait=walking?Mathf.Clamp01(actualSpeed/(charging?8:2.2f)):0;
            gait=Mathf.Lerp(gait,targetGait,1-Mathf.Exp(-14*dt));
            phase=Mathf.Repeat(phase+(walking?travel*(charging?1.3f:2.65f)/visualScale:0),Mathf.PI*2);
            breathPhase=Mathf.Repeat(breathPhase+dt*1.65f,Mathf.PI*2);
            float upperTarget=0,foreTarget=0,leanTarget=0,crouchTarget=0;
            switch(state){
                case OrcaladonPhase.ChargeAim:
                    float settle=Mathf.SmoothStep(0,1,progress);
                    upperTarget=Mathf.Lerp(15,25,settle);foreTarget=-28;leanTarget=Mathf.Lerp(5,12,settle);crouchTarget=.055f*settle;break;
                case OrcaladonPhase.Charging:upperTarget=34;foreTarget=-35;leanTarget=20;break;
                case OrcaladonPhase.SlamWindup:
                    float wind=Mathf.SmoothStep(0,1,progress/.6f);
                    float strike=Mathf.SmoothStep(0,1,(progress-.68f)/.32f);
                    upperTarget=Mathf.Lerp(-142*wind,12,strike);foreTarget=Mathf.Lerp(28*wind,25,strike);leanTarget=Mathf.Lerp(-9*wind,21,strike);crouchTarget=.18f*strike;break;
                case OrcaladonPhase.SlamImpact:upperTarget=12;foreTarget=25;leanTarget=21;crouchTarget=.18f;break;
            }
            float blend=1-Mathf.Exp(-12*dt);
            // Exact AI contact has priority over smoothing. The end of the windup is already
            // the accelerating downswing; no visual delay can make a damaging airborne slam.
            if(state==OrcaladonPhase.SlamImpact||(state==OrcaladonPhase.SlamWindup&&progress>=.68f))blend=1;
            upper=Mathf.Lerp(upper,upperTarget,blend);fore=Mathf.Lerp(fore,foreTarget,blend);lean=Mathf.Lerp(lean,leanTarget,blend);crouch=Mathf.Lerp(crouch,crouchTarget,blend);
            RestoreBones();
            float stride=Mathf.Sin(phase)*gait;
            float breath=Mathf.Sin(breathPhase)*(1-gait);
            bool resting=state==OrcaladonPhase.Approach||state==OrcaladonPhase.Recovery;
            Rotate(0,lean+(resting?breath*.65f:0),0,resting?breath*.6f:0);
            float armRoll=NeutralArmsDown?0:RelaxedArmRoll;
            if(!NeutralArmsDown&&state==OrcaladonPhase.SlamWindup)armRoll=Mathf.Lerp(RelaxedArmRoll,3,Mathf.SmoothStep(0,1,progress/.6f));
            else if(!NeutralArmsDown&&(charging||state==OrcaladonPhase.ChargeAim||state==OrcaladonPhase.SlamImpact))armRoll=12;
            Rotate(1,upper-stride*13,0,-armRoll);Rotate(2,upper+stride*13,0,armRoll);
            Rotate(3,fore-Mathf.Max(0,stride)*9,0,0);Rotate(4,fore-Mathf.Max(0,-stride)*9,0,0);
            Rotate(5,stride*23,0,0);Rotate(6,-stride*23,0,0);
            Rotate(7,Mathf.Max(0,-stride)*19,0,0);Rotate(8,Mathf.Max(0,stride)*19,0,0);
            if(Body!=null){
                // Torso compression/rebound belongs to the torso. Previously moving the whole
                // MotionRoot below its rest height also drove every planted foot into the sand.
                Vector3 left=ThighLeft!=null?ThighLeft.position:Vector3.zero,right=ThighRight!=null?ThighRight.position:Vector3.zero;
                float bob=Mathf.Abs(Mathf.Sin(phase))*.023f*gait;
                float breathing=resting?(breath+1)*.004f:0;
                Body.position+=transform.TransformVector(Vector3.up*(bob+breathing-crouch));
                if(ThighLeft!=null)ThighLeft.position=left;if(ThighRight!=null)ThighRight.position=right;
            }
            if(MotionRoot!=null){MotionRoot.localPosition=motionPosition;MotionRoot.localRotation=motionRotation;}
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
