using UnityEngine;
namespace Brainrot
{
    /// <summary>Visual-only bridge: gameplay moves the physics root; animation only poses this child hierarchy.</summary>
    public sealed class ImportedEnemyVisual : MonoBehaviour
    {
        public Transform MotionRoot,Normalization,ModelRoot;
        public Animator Animator;
        public Transform[] LegBones;
        public float TargetHeight=1.9f;
        public bool IsOctopus;
        EnemyController enemy;
        Quaternion[] legRest;
        float speed,phase,biteRemaining,biteDuration=.32f;
        Vector3 restMotion;
        static readonly int Moving=UnityEngine.Animator.StringToHash("Moving");
        void Awake(){enemy=GetComponentInParent<EnemyController>();CacheRest();}
        void Start(){CalibrateModel();}
        void CacheRest(){
            if(MotionRoot!=null)restMotion=MotionRoot.localPosition;
            if(LegBones==null)return;
            legRest=new Quaternion[LegBones.Length];for(int i=0;i<LegBones.Length;i++)if(LegBones[i]!=null)legRest[i]=LegBones[i].localRotation;
        }
        public bool CalibrateModel(){
            if(Normalization==null||ModelRoot==null)return false;
            if(Animator!=null&&Animator.runtimeAnimatorController!=null){Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.Rebind();Animator.Update(0);}
            bool any=false;Bounds bounds=new Bounds();
            foreach(var renderer in ModelRoot.GetComponentsInChildren<Renderer>(true)){
                if(renderer is TrailRenderer||renderer is LineRenderer)continue;
                var world=renderer.bounds;
                for(int c=0;c<8;c++){
                    Vector3 point=transform.InverseTransformPoint(world.center+Vector3.Scale(world.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));
                    if(float.IsNaN(point.x)||float.IsInfinity(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.y)||float.IsNaN(point.z)||float.IsInfinity(point.z))continue;
                    if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                }
            }
            if(!any||bounds.size.y<.00001f)return false;
            float factor=Mathf.Max(.2f,TargetHeight)/bounds.size.y;
            Normalization.localPosition=(Normalization.localPosition-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))*factor;
            Normalization.localScale*=factor;CacheRest();return true;
        }
        public void Animate(float movement,bool attacking){speed=Mathf.Max(0,movement);if(Animator!=null&&Animator.isActiveAndEnabled&&Animator.runtimeAnimatorController!=null)Animator.SetBool(Moving,speed>.05f);}
        public void PlayBite(float duration=.32f){if(IsOctopus)return;biteDuration=Mathf.Max(.15f,duration);biteRemaining=biteDuration;}
        public void ResetBite(){biteRemaining=0;if(MotionRoot!=null){MotionRoot.localPosition=restMotion;MotionRoot.localRotation=Quaternion.identity;}}
        public void ResetVisual(){speed=phase=0;ResetBite();if(legRest!=null)for(int i=0;i<legRest.Length;i++)if(LegBones[i]!=null)LegBones[i].localRotation=legRest[i];if(Animator!=null){Animator.enabled=true;if(Animator.runtimeAnimatorController!=null)Animator.SetBool(Moving,false);}}
        void LateUpdate(){
            bool physical=enemy!=null&&(!enemy.Alive||(enemy.PhysicsBody!=null&&enemy.PhysicsBody.IsPhysical));
            if(Animator!=null)Animator.enabled=!physical;
            if(physical)return;
            if(GameManager.Instance!=null&&!GameManager.Instance.IsPlaying)return;
            float dt=Time.deltaTime;if(dt<=0)return;
            if(IsOctopus&&Animator==null){
                float gait=Mathf.Clamp01(speed/2f);phase+=dt*(gait>0?7.5f:1.7f);
                if(MotionRoot!=null){MotionRoot.localPosition=restMotion+Vector3.up*Mathf.Abs(Mathf.Sin(phase))*.065f*gait;MotionRoot.localRotation=Quaternion.Euler(Mathf.Sin(phase*.5f)*2*gait,0,Mathf.Sin(phase)*(gait>0?7:1.5f));}
            }else if(!IsOctopus){
                float gait=Mathf.Clamp01(speed/2f);phase+=dt*Mathf.Lerp(0,9,gait);
                if(legRest!=null)for(int i=0;i<legRest.Length;i++)if(LegBones[i]!=null)LegBones[i].localRotation=legRest[i]*Quaternion.Euler(Mathf.Sin(phase+(i==1?Mathf.PI:i==2?Mathf.PI*.5f:0))*26*gait,0,0);
                float lunge=0;
                if(biteRemaining>0){biteRemaining=Mathf.Max(0,biteRemaining-dt);float t=1-biteRemaining/biteDuration;lunge=t<.38f?Mathf.SmoothStep(0,1,t/.38f):1-Mathf.SmoothStep(0,1,(t-.38f)/.62f);}
                if(MotionRoot!=null){MotionRoot.localPosition=restMotion+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.045f*gait)+Vector3.forward*(lunge*.48f);MotionRoot.localRotation=Quaternion.Euler(lunge*9,0,Mathf.Sin(phase)*2.5f*gait);}
            }
        }
        void OnDisable(){ResetBite();}
    }
}
