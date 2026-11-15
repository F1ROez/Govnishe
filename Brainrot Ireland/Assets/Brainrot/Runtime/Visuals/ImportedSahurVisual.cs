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
        float remaining, duration=.32f, reach=3.2f,contactSeconds=.09f,attackStartedTime;
        float chargeProgress;
        bool charging,heavyAttack,fromCharge;
        SahurCombatPose procedural;
        Transform[] cachedBones;
        Vector3[] basePositions;
        Quaternion[] baseRotations;
        Vector3 baseWeaponPosition,baseWeaponScale;
        Quaternion baseWeaponRotation;
        bool sampledPose;
        float batLength=.88f;
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
            if(WeaponTip!=null)batLength=Mathf.Max(.01f,WeaponTip.localPosition.magnitude);
            if(Animator!=null){Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
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
            if(Animator!=null&&Animator.runtimeAnimatorController!=null){Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.Rebind();Animator.Update(0);}
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
        public void Animate(float movement,bool attacking){if(Animator!=null&&Animator.runtimeAnimatorController!=null)Animator.SetFloat(Speed,movement);}
        public void SetAttackReach(float range){reach=Mathf.Max(.5f,range);}
        public void SetChargePose(float progress,bool active){charging=active&&remaining<=0;chargeProgress=Mathf.Clamp01(progress);if(!charging)procedural?.Restore();}
        public void PlayAttack(float seconds,float contactTime=.09f,bool heavy=false){
            bool charged=charging&&chargeProgress>.05f;
            ResetAttack();heavyAttack=heavy;fromCharge=charged&&heavy;duration=Mathf.Max(.15f,seconds);contactSeconds=Mathf.Clamp(contactTime,0,duration*.95f);remaining=duration;attackStartedTime=Time.time;
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
        void Update(){RestoreSampledPose();procedural?.Restore();}
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
            if(Weapon==null)return;
            var parent=Weapon.parent!=null?Weapon.parent:transform;
            float unit=Mathf.Max(.001f,parent.TransformVector(Vector3.up*batLength).magnitude);
            Weapon.localScale=new Vector3(1,Mathf.Clamp((reach-.25f)/unit,.4f,5),1);
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
        void LateUpdate(){
            if(GameManager.Instance!=null&&!GameManager.Instance.IsPlaying){ResetAttack();return;}
            bool swing=remaining>0;
            if(swing)remaining=Mathf.Max(0,duration-(Time.time-attackStartedTime));
            float elapsed=duration-remaining,progress=elapsed/duration;
            if(UsesProceduralAttack){
                if(procedural==null)procedural=new SahurCombatPose(this);
                PoseReadyWeapon();
                if(swing){procedural.Swing(elapsed,duration,contactSeconds,heavyAttack,fromCharge);ApplyReach();}
                else if(charging)procedural.Charge(chargeProgress);
                if(Trail!=null)Trail.emitting=swing&&progress>.02f&&progress<.88f;
                if(swing&&remaining<=0){procedural.Restore();PoseReadyWeapon();if(Trail!=null)Trail.emitting=false;}
                return;
            }
            if(UsesRetargetedAttack){
                if(swing)SampleRetargetedPose(elapsed);else PoseReadyWeapon();
                if(Trail!=null)Trail.emitting=swing&&progress>.03f&&progress<.86f;
                if(swing&&remaining<=0){RestoreSampledPose();PoseReadyWeapon();if(Trail!=null)Trail.emitting=false;}
                return;
            }
            if(Weapon==null||Grip==null)return;
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
            if(Trail!=null)Trail.emitting=swing&&progress>.03f&&progress<.86f;
            if(swing&&remaining<=0)RestoreArms();
        }
        void RestoreArms(){if(RightUpperArm!=null)RightUpperArm.localRotation=upperRest;if(RightForearm!=null)RightForearm.localRotation=foreRest;}
        public void ResetAttack(){bool retargeted=UsesRetargetedAttack;RestoreSampledPose();procedural?.Restore();remaining=0;charging=heavyAttack=fromCharge=false;chargeProgress=0;if(!retargeted&&!UsesProceduralAttack)RestoreArms();if(Weapon!=null)Weapon.localScale=Vector3.one;if(Trail!=null){Trail.emitting=false;Trail.Clear();}}
        void OnDisable(){ResetAttack();}
    }
}
