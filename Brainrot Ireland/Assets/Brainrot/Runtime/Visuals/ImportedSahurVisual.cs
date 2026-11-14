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
        public bool AttackActive => remaining > 0;
        float remaining, duration=.32f, reach=3.2f;
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
            ResetAttack();
        }
        void Start(){CalibrateModel();}
        /// <summary>Measure the evaluated skin, not renderer bounds or an editor-only sampled pose.</summary>
        public bool CalibrateModel()
        {
            if(ModelRoot==null)return false;
            if(Normalization==null&&ModelRoot.parent!=null&&ModelRoot.parent!=transform)Normalization=ModelRoot.parent;
            if(Normalization==null)return false;
            Normalization.localPosition=Vector3.zero;Normalization.localRotation=Quaternion.identity;Normalization.localScale=Vector3.one;
            var skins=ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var skin in skins){skin.enabled=true;skin.forceRenderingOff=false;skin.updateWhenOffscreen=true;}
            if(Animator!=null&&Animator.runtimeAnimatorController!=null){Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.Rebind();Animator.Update(0);}
            bool any=false;Bounds bounds=new Bounds();var baked=new Mesh();
            try{
                foreach(var skin in skins){
                    if(skin.sharedMesh==null)continue;
                    baked.Clear();skin.BakeMesh(baked,false);
                    foreach(var vertex in baked.vertices){
                        var point=transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                        if(float.IsNaN(point.x)||float.IsInfinity(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.y)||float.IsNaN(point.z)||float.IsInfinity(point.z))continue;
                        if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                    }
                }
            }finally{if(Application.isPlaying)Destroy(baked);else DestroyImmediate(baked);}
            if(!any||bounds.size.y<.0001f){Debug.LogError("Не удалось измерить скелетную сетку Сахура. Повторите создание модели.",this);return false;}
            float scale=1.9f/bounds.size.y;
            var hips=Find(ModelRoot,"spine");var center=hips!=null?transform.InverseTransformPoint(hips.position):bounds.center;
            Normalization.localScale=Vector3.one*scale;
            Normalization.localPosition=new Vector3(-center.x*scale,-bounds.min.y*scale,-center.z*scale);
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
        public void PlayAttack(float seconds){duration=Mathf.Max(.15f,seconds);remaining=duration;if(Trail!=null){Trail.emitting=false;Trail.Clear();}}
        void LateUpdate(){
            if(Weapon==null||Grip==null)return;
            if(GameManager.Instance!=null&&!GameManager.Instance.IsPlaying){ResetAttack();return;}
            bool swing=remaining>0;
            float yaw=0,progress=0;
            if(swing){
                remaining=Mathf.Max(0,remaining-Time.deltaTime);float elapsed=duration-remaining;float contact=Mathf.Min(.09f,duration*.35f);
                yaw=elapsed<contact?Mathf.Lerp(-95,0,Mathf.SmoothStep(0,1,elapsed/contact)):Mathf.Lerp(0,110,Mathf.SmoothStep(0,1,(elapsed-contact)/Mathf.Max(.01f,duration-contact)));
                progress=elapsed/duration;
                if(RightUpperArm!=null)RightUpperArm.localRotation=Quaternion.AngleAxis(-55,RightUpperArm.parent.InverseTransformDirection(transform.right))*upperRest;
                if(RightForearm!=null)RightForearm.localRotation=Quaternion.AngleAxis(-25,RightForearm.parent.InverseTransformDirection(transform.right))*foreRest;
            }
            // Bat is already a weighted part of the imported mesh. Reposition its existing bone, never create a second bat.
            Weapon.position=Grip.position+transform.forward*.045f;
            if(swing){
                Weapon.rotation=Quaternion.AngleAxis(yaw,transform.up)*Quaternion.LookRotation(transform.forward,transform.up)*Quaternion.Euler(90,0,0);
                float unit=Mathf.Max(.001f,Weapon.parent.TransformVector(Vector3.up*batLength).magnitude);
                Weapon.localScale=new Vector3(1,Mathf.Clamp((reach-.25f)/unit,.4f,5),1);
            }else{
                Weapon.localScale=Vector3.one;
                Weapon.rotation=Quaternion.LookRotation(transform.forward,transform.up)*Quaternion.Euler(8,0,-18);
            }
            if(Trail!=null)Trail.emitting=swing&&progress>.03f&&progress<.86f;
            if(swing&&remaining<=0)RestoreArms();
        }
        void RestoreArms(){if(RightUpperArm!=null)RightUpperArm.localRotation=upperRest;if(RightForearm!=null)RightForearm.localRotation=foreRest;}
        public void ResetAttack(){remaining=0;RestoreArms();if(Weapon!=null)Weapon.localScale=Vector3.one;if(Trail!=null){Trail.emitting=false;Trail.Clear();}}
        void OnDisable(){ResetAttack();}
    }
}
