using UnityEngine;
namespace Brainrot
{
    /// <summary>Animation bridge for the owner's skinned Sahur mesh. Animator owns locomotion; the bat is posed after it.</summary>
    public sealed class ImportedSahurVisual : MonoBehaviour
    {
        public Transform ModelRoot;
        public Transform Normalization;
        public Animator Animator;
        public Transform Weapon;
        public Transform WeaponTip;
        public Transform Grip;
        public Transform RightUpperArm;
        public Transform RightForearm;
        public TrailRenderer Trail;
        public AnimationClip AttackClip;
        public Transform[] AttackBones;
        [Range(0,1)]public float AttackContactNormalized=.4f;
        // Old generated prefabs may still contain the private retargeted clip. The real Sahur rig always uses our own choreography.
        public bool UsesProceduralAttack=>RightUpperArm!=null&&RightForearm!=null&&Grip!=null&&Weapon!=null;
        public bool UsesRetargetedAttack=>!UsesProceduralAttack&&AttackClip!=null&&ModelRoot!=null&&AttackBones!=null&&AttackBones.Length>0;
        public bool AttackActive => remaining > 0;
        public bool ChargePoseActive=>charging;
        public bool HeavyAttackActive=>AttackActive&&heavyAttack;
        public bool ThrowActive=>AttackActive&&throwing;
        float remaining, duration=.32f,contactSeconds=.09f,attackStartedTime;
        float chargeProgress,impactPause,pausedAttackTime;
        bool charging,heavyAttack,fromCharge,throwing;
        SahurCombatPose procedural;
        Vector3 localVelocity,shownVelocity;
        bool dashing;
        float locomotionSpeed,dashLean,locomotionWeight=1,animatorSpeed=float.NaN;
        Transform body;
        Quaternion bodyBaseline;
        bool bodyPosed;
        public void SetLocomotion(Vector3 velocity,bool dash){localVelocity=velocity;dashing=dash;}
        void RestoreLocomotion(){if(bodyPosed&&body!=null)body.localRotation=bodyBaseline;bodyPosed=false;}
        void PoseLocomotion(){
            if(body==null)return;
            bodyBaseline=body.localRotation;bodyPosed=true;
            float dt=Mathf.Max(0,Time.deltaTime);
            shownVelocity=Vector3.Lerp(shownVelocity,localVelocity,1-Mathf.Exp(-14*dt));
            // Only the visual torso leans: actor/collider/camera pivot remain immediately responsive.
            float targetWeight=AttackActive||charging?.25f:1;
            locomotionWeight=Mathf.Lerp(locomotionWeight,targetWeight,1-Mathf.Exp(-24*dt));
            dashLean=Mathf.Lerp(dashLean,dashing?1:0,1-Mathf.Exp(-(dashing?22:16)*dt));
            float weight=locomotionWeight;
            var forward=transform.forward;var right=transform.right;
            float lean=Mathf.Clamp(shownVelocity.z*.7f,-5,7)+dashLean*5;
            float bank=Mathf.Clamp(-shownVelocity.x*.7f,-5,5);
            body.rotation=Quaternion.AngleAxis(lean*weight,right)*Quaternion.AngleAxis(bank*weight,forward)*body.rotation;
        }
        Transform[] cachedBones;
        Vector3[] basePositions;
        Quaternion[] baseRotations;
        Vector3 baseWeaponPosition,baseWeaponScale;
        Quaternion baseWeaponRotation;
        bool sampledPose;
        Quaternion upperRest, foreRest;
        static readonly int Speed = UnityEngine.Animator.StringToHash("Speed");
        void Awake(){
            if(ModelRoot==null)ModelRoot=transform;
            if(Animator==null)Animator=GetComponentInChildren<Animator>();
            if(Weapon==null)Weapon=Find(ModelRoot,"Bat");
            if(Grip==null)Grip=Find(ModelRoot,"hand.R");
            if(RightUpperArm==null)RightUpperArm=Find(ModelRoot,"upper_arm.R");
            if(RightForearm==null)RightForearm=Find(ModelRoot,"forearm.R");
            if(RightUpperArm!=null)upperRest=RightUpperArm.localRotation;
            if(RightForearm!=null)foreRest=RightForearm.localRotation;
            if(Animator!=null){Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
            body=Find(ModelRoot,"spine.002");
            if(UsesProceduralAttack)procedural=new SahurCombatPose(this);
            ResetAttack();
        }
        void Start(){CalibrateModel();}
        /// <summary>Fit renderer bounds after evaluating the live Animator, in the actor's local space.</summary>
        public bool CalibrateModel()
        {
            if(ModelRoot==null)return false;
            if(Normalization==null&&ModelRoot.parent!=null&&ModelRoot.parent!=transform)Normalization=ModelRoot.parent;
            if(Normalization==null)return false;
            var skins=ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var skin in skins){skin.enabled=true;skin.forceRenderingOff=false;skin.updateWhenOffscreen=true;}
            if(Animator!=null&&Animator.runtimeAnimatorController!=null){Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.Rebind();Animator.Update(0);animatorSpeed=float.NaN;}
            // The live Unity report exposed inconsistent BakeMesh scale with the FBX's 100x transforms.
            // Use the live WORLD bounds that Unity actually culls/renders. Skin bounds may follow rootBone,
            // so transforming localBounds through the mesh transform is not reliable for every imported rig.
            bool any=false;Bounds bounds=new Bounds();
            foreach(var skin in skins){
                if(skin.sharedMesh==null)continue;
                var world=skin.bounds;
                for(int corner=0;corner<8;corner++){
                    var vertex=world.center+Vector3.Scale(world.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    var point=transform.InverseTransformPoint(vertex);
                    if(float.IsNaN(point.x)||float.IsInfinity(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.y)||float.IsNaN(point.z)||float.IsInfinity(point.z))continue;
                    if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                }
            }
            if(!any||bounds.size.y<.0001f){Debug.LogError("Не удалось измерить скелетную сетку Сахура. Повторите создание модели.",this);return false;}
            float scale=1.9f/bounds.size.y;
            var hips=Find(ModelRoot,"spine");var center=hips!=null?transform.InverseTransformPoint(hips.position):bounds.center;
            var anchor=new Vector3(center.x,bounds.min.y,center.z);
            // Resize about the normalization origin, then move the measured anchor to the actor origin.
            Normalization.localPosition=(Normalization.localPosition-anchor)*scale;
            Normalization.localScale*=scale;
            if(RightUpperArm!=null)upperRest=RightUpperArm.localRotation;
            if(RightForearm!=null)foreRest=RightForearm.localRotation;
            return true;
        }
        public static Transform Find(Transform root,string name){
            if(root.name==name)return root;
            foreach(Transform child in root){var match=Find(child,name);if(match!=null)return match;}
            return null;
        }
        public void Animate(float movement,bool attacking){
            locomotionSpeed=Mathf.Lerp(locomotionSpeed,Mathf.Max(0,movement),1-Mathf.Exp(-18*Mathf.Max(0,Time.deltaTime)));
            if(locomotionSpeed<.005f)locomotionSpeed=0;
            SetAnimatorSpeed(locomotionSpeed);
        }
        void SetAnimatorSpeed(float value){
            if(Animator==null||!Animator.isActiveAndEnabled||Animator.runtimeAnimatorController==null)return;
            if(float.IsNaN(animatorSpeed)||Mathf.Abs(animatorSpeed-value)>.001f){Animator.SetFloat(Speed,value);animatorSpeed=value;}
        }
        public void SetAttackReach(float range){ /* Gameplay reach belongs to the sector, not to mesh stretching. */ }
        public void SetChargePose(float progress,bool active){charging=active&&remaining<=0;chargeProgress=Mathf.Clamp01(progress);if(!charging)procedural?.Restore();}
        public void PlayAttack(float seconds,float contactTime=.09f,bool heavy=false){
            bool charged=charging&&chargeProgress>.05f;
            ResetAttackState(false);heavyAttack=heavy;fromCharge=charged&&heavy;impactPause=pausedAttackTime=0;duration=Mathf.Max(.15f,seconds);contactSeconds=Mathf.Clamp(contactTime,0,duration*.95f);remaining=duration;attackStartedTime=Time.time;
            EnsurePoseCache();
        }
        public void PlayThrow(float seconds=.42f,float releaseDelay=.14f){
            ResetAttackState(false);throwing=true;duration=Mathf.Max(.2f,seconds);contactSeconds=Mathf.Clamp(releaseDelay,.05f,duration*.65f);remaining=duration;attackStartedTime=Time.time;
            EnsurePoseCache();
        }
        /// <summary>Keep the animation's strike pose aligned with CombatSystem's existing contact time.</summary>
        public static float MapAttackTime(float elapsed,float playbackDuration,float contactTime,float sourceDuration,float sourceContactNormalized){
            if(playbackDuration<=0||sourceDuration<=0)return 0;
            elapsed=Mathf.Clamp(elapsed,0,playbackDuration);contactTime=Mathf.Clamp(contactTime,0,playbackDuration*.95f);
            float contact=Mathf.Clamp01(sourceContactNormalized),phase;
            if(contactTime>.00001f&&elapsed<=contactTime)phase=contact*elapsed/contactTime;
            else phase=contact+(1-contact)*(elapsed-contactTime)/Mathf.Max(.00001f,playbackDuration-contactTime);
            return Mathf.Clamp01(phase)*sourceDuration;
        }
        void EnsurePoseCache(){
            if(AttackBones==cachedBones&&basePositions!=null&&basePositions.Length==(AttackBones!=null?AttackBones.Length:0))return;
            RestoreSampledPose();cachedBones=AttackBones;
            int count=AttackBones!=null?AttackBones.Length:0;basePositions=new Vector3[count];baseRotations=new Quaternion[count];
        }
        // Undo last frame's visual overrides before Animator evaluates this frame's locomotion.
        // Otherwise an unanimated bone (or an actor without Animator) could accumulate a blended pose.
        void Update(){RestoreSampledPose();procedural?.Restore();RestoreLocomotion();}
        void CapturePose(){
            EnsurePoseCache();
            for(int i=0;i<cachedBones.Length;i++)if(cachedBones[i]!=null){basePositions[i]=cachedBones[i].localPosition;baseRotations[i]=cachedBones[i].localRotation;}
            if(Weapon!=null){baseWeaponPosition=Weapon.localPosition;baseWeaponRotation=Weapon.localRotation;baseWeaponScale=Weapon.localScale;}
        }
        void RestoreSampledPose(){
            if(!sampledPose)return;
            if(cachedBones!=null)for(int i=0;i<cachedBones.Length;i++)if(cachedBones[i]!=null){cachedBones[i].localPosition=basePositions[i];cachedBones[i].localRotation=baseRotations[i];}
            if(Weapon!=null){Weapon.localPosition=baseWeaponPosition;Weapon.localRotation=baseWeaponRotation;Weapon.localScale=baseWeaponScale;}
            sampledPose=false;
        }
        void PoseReadyWeapon(){
            if(Weapon==null||Grip==null)return;
            Weapon.position=Grip.position+transform.forward*.045f;Weapon.localScale=Vector3.one;
            Weapon.rotation=Quaternion.LookRotation(transform.forward,transform.up)*Quaternion.Euler(8,0,-18);
        }
        void ApplyReach(){
            // The authored bat has a fixed length. Stretching it only during a swing caused a
            // visible pop at contact/recovery and changed the apparent position of the grip.
            if(Weapon!=null)Weapon.localScale=Vector3.one;
        }
        void SampleRetargetedPose(float elapsed){
            PoseReadyWeapon();CapturePose();
            AttackClip.SampleAnimation(ModelRoot.gameObject,MapAttackTime(elapsed,duration,contactSeconds,AttackClip.length,AttackContactNormalized));
            float inSeconds=Mathf.Min(.035f,Mathf.Min(duration*.12f,contactSeconds*.6f));
            float weight=inSeconds>.00001f?Mathf.SmoothStep(0,1,elapsed/inSeconds):1;
            weight*=Mathf.SmoothStep(0,1,(duration-elapsed)/Mathf.Min(.07f,duration*.23f));
            for(int i=0;i<cachedBones.Length;i++)if(cachedBones[i]!=null){
                cachedBones[i].localPosition=Vector3.Lerp(basePositions[i],cachedBones[i].localPosition,weight);
                cachedBones[i].localRotation=Quaternion.Slerp(baseRotations[i],cachedBones[i].localRotation,weight);
            }
            if(Weapon!=null){Weapon.localPosition=Vector3.Lerp(baseWeaponPosition,Weapon.localPosition,weight);Weapon.localRotation=Quaternion.Slerp(baseWeaponRotation,Weapon.localRotation,weight);}
            sampledPose=true;ApplyReach();
        }
        public void HoldImpact(float seconds){if(remaining>0)impactPause=Mathf.Max(impactPause,Mathf.Clamp(seconds,0,.08f));}
        void LateUpdate(){
            if(GameManager.Instance!=null&&!GameManager.Instance.IsPlaying){ResetAttack();return;}
            PoseLocomotion();
            bool swing=remaining>0;
            float held=Mathf.Min(Time.deltaTime,impactPause);impactPause=Mathf.Max(0,impactPause-held);pausedAttackTime+=held;
            if(swing)remaining=Mathf.Max(0,duration-(Time.time-attackStartedTime-pausedAttackTime));
            float elapsed=duration-remaining,progress=elapsed/duration;
            if(UsesProceduralAttack){
                if(procedural==null)procedural=new SahurCombatPose(this);
                PoseReadyWeapon();
                if(swing){if(throwing)procedural.Throw(elapsed,duration,contactSeconds);else procedural.Swing(elapsed,duration,contactSeconds,heavyAttack,fromCharge);ApplyReach();}
                else if(charging)procedural.Charge(chargeProgress);
                if(Trail!=null)Trail.emitting=swing&&!throwing&&impactPause<=0&&progress>.02f&&progress<.88f;
                if(swing&&remaining<=0){procedural.Restore();PoseReadyWeapon();if(Trail!=null)Trail.emitting=false;}
                return;
            }
            if(UsesRetargetedAttack&&!throwing){
                if(swing)SampleRetargetedPose(elapsed);else PoseReadyWeapon();
                if(Trail!=null)Trail.emitting=swing&&impactPause<=0&&progress>.03f&&progress<.86f;
                if(swing&&remaining<=0){RestoreSampledPose();PoseReadyWeapon();if(Trail!=null)Trail.emitting=false;}
                return;
            }
            if(Weapon==null||Grip==null)return;
            if(throwing&&swing){
                float drive=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.05f,contactSeconds)));
                float recovery=Mathf.SmoothStep(0,1,(duration-elapsed)/.12f);
                if(RightUpperArm!=null)RightUpperArm.localRotation=Quaternion.AngleAxis(Mathf.Lerp(28,-95,drive)*recovery,RightUpperArm.parent.InverseTransformDirection(transform.right))*upperRest;
                if(RightForearm!=null)RightForearm.localRotation=Quaternion.AngleAxis(Mathf.Lerp(-88,-15,drive)*recovery,RightForearm.parent.InverseTransformDirection(transform.right))*foreRest;
                PoseReadyWeapon();if(Trail!=null)Trail.emitting=false;if(remaining<=0)RestoreArms();return;
            }
            float yaw=0;
            if(swing){
                float contact=Mathf.Min(contactSeconds,duration*.65f);
                yaw=contact>.00001f&&elapsed<contact?Mathf.Lerp(-95,0,Mathf.SmoothStep(0,1,elapsed/contact)):Mathf.Lerp(0,110,Mathf.SmoothStep(0,1,(elapsed-contact)/Mathf.Max(.01f,duration-contact)));
                if(RightUpperArm!=null)RightUpperArm.localRotation=Quaternion.AngleAxis(-55,RightUpperArm.parent.InverseTransformDirection(transform.right))*upperRest;
                if(RightForearm!=null)RightForearm.localRotation=Quaternion.AngleAxis(-25,RightForearm.parent.InverseTransformDirection(transform.right))*foreRest;
            }
            Weapon.position=Grip.position+transform.forward*.045f;
            if(swing){Weapon.rotation=Quaternion.AngleAxis(yaw,transform.up)*Quaternion.LookRotation(transform.forward,transform.up)*Quaternion.Euler(90,0,0);ApplyReach();}
            else PoseReadyWeapon();
            if(Trail!=null)Trail.emitting=swing&&impactPause<=0&&progress>.03f&&progress<.86f;
            if(swing&&remaining<=0)RestoreArms();
        }
        void RestoreArms(){if(RightUpperArm!=null)RightUpperArm.localRotation=upperRest;if(RightForearm!=null)RightForearm.localRotation=foreRest;}
        public void ResetAttack(){ResetAttackState(true);}
        void ResetAttackState(bool resetMovement){if(resetMovement){shownVelocity=localVelocity=Vector3.zero;locomotionSpeed=dashLean=0;locomotionWeight=1;dashing=false;SetAnimatorSpeed(0);}
            bool retargeted=UsesRetargetedAttack;RestoreSampledPose();procedural?.Restore();RestoreLocomotion();remaining=impactPause=pausedAttackTime=0;charging=heavyAttack=fromCharge=throwing=false;chargeProgress=0;if(!retargeted&&!UsesProceduralAttack)RestoreArms();if(Weapon!=null)Weapon.localScale=Vector3.one;if(Trail!=null){Trail.emitting=false;Trail.Clear();}}
        void OnDisable(){ResetAttack();animatorSpeed=float.NaN;shownVelocity=localVelocity=Vector3.zero;locomotionSpeed=dashLean=0;locomotionWeight=1;dashing=false;}
    }
}
