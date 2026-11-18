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
        public bool IsOrcaladon;
        public OrcaladonVisual Orcaladon;
        EnemyController enemy;
        Quaternion[] legRest;
        bool movingKnown,animatorMoving;
        float speed,phase,biteRemaining,biteDuration=.32f,calibratedHeight;
        Vector3 restMotion;Quaternion restRotation=Quaternion.identity;
        static readonly int Moving=UnityEngine.Animator.StringToHash("Moving");
        void Awake(){enemy=GetComponentInParent<EnemyController>();CacheRest();}
        void Start(){CalibrateModel();}
        void CacheRest(){
            if(MotionRoot!=null){restMotion=MotionRoot.localPosition;restRotation=MotionRoot.localRotation;}
            if(LegBones==null)return;
            if(legRest==null||legRest.Length!=LegBones.Length)legRest=new Quaternion[LegBones.Length];
            for(int i=0;i<LegBones.Length;i++)if(LegBones[i]!=null)legRest[i]=LegBones[i].localRotation;
        }
        public bool CalibrateModel(){
            if(Normalization==null||ModelRoot==null)return false;
            // Old generated prefabs still store 3.4 m. The balance profile is authoritative,
            // so they shrink on first activation without requiring a manual prefab rebuild.
            if(IsOrcaladon)TargetHeight=Mathf.Max(.5f,GameBalance.Current.Orcaladon.ModelHeight);
            if(IsOrcaladon&&Orcaladon!=null)Orcaladon.ResetPose();
            if(Animator!=null&&Animator.runtimeAnimatorController!=null){Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.Rebind();Animator.Update(0);movingKnown=false;}
            bool any=false;Bounds bounds=new Bounds();
            foreach(var renderer in ModelRoot.GetComponentsInChildren<Renderer>(true)){
                if(renderer is TrailRenderer||renderer is LineRenderer)continue;
                // FBX skinned import bounds can include the original outstretched pose, and
                // rotating a world AABB back to actor space inflates it. Measure actual neutral
                // deformed vertices for Orcaladon, once during calibration, not every frame.
                if(IsOrcaladon&&renderer is SkinnedMeshRenderer skin&&skin.sharedMesh!=null){
                    var baked=new Mesh{name="Временное измерение Оркаладона"};
                    try{
                        skin.BakeMesh(baked);
                        Matrix4x4 matrix=Normalization.parent.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                        foreach(var vertex in baked.vertices){
                            Vector3 point=matrix.MultiplyPoint3x4(vertex);
                            if(!Finite(point))continue;
                            if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                        }
                    }finally{if(Application.isPlaying)Destroy(baked);else DestroyImmediate(baked);}
                    continue;
                }
                var world=renderer.bounds;
                for(int c=0;c<8;c++){
                    Vector3 point=Normalization.parent.InverseTransformPoint(world.center+Vector3.Scale(world.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));
                    if(float.IsNaN(point.x)||float.IsInfinity(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.y)||float.IsNaN(point.z)||float.IsInfinity(point.z))continue;
                    if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                }
            }
            if(!any||bounds.size.y<.00001f)return false;
            float factor=Mathf.Max(.2f,TargetHeight)/bounds.size.y;
            Normalization.localPosition=(Normalization.localPosition-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))*factor;
            Normalization.localScale*=factor;calibratedHeight=Mathf.Max(.2f,TargetHeight);CacheRest();return true;
        }
        /// <summary>Live height changes use the measured baseline; no meshes are baked during animation.</summary>
        public void ApplyConfiguredHeight(){
            if(!IsOrcaladon||Normalization==null||calibratedHeight<=0)return;
            float height=Mathf.Max(.5f,GameBalance.Current.Orcaladon.ModelHeight);
            if(Mathf.Approximately(height,calibratedHeight))return;
            float factor=height/calibratedHeight;
            Normalization.localPosition*=factor;Normalization.localScale*=factor;
            calibratedHeight=TargetHeight=height;
        }
        static bool Finite(Vector3 point)=>!float.IsNaN(point.x)&&!float.IsInfinity(point.x)&&!float.IsNaN(point.y)&&!float.IsInfinity(point.y)&&!float.IsNaN(point.z)&&!float.IsInfinity(point.z);
        public void Animate(float movement,bool attacking){speed=Mathf.Max(0,movement);ApplyMoving();}
        void ApplyMoving(){if(Animator==null||!Animator.isActiveAndEnabled||Animator.runtimeAnimatorController==null)return;bool moving=speed>.05f;if(!movingKnown||moving!=animatorMoving){Animator.SetBool(Moving,moving);animatorMoving=moving;movingKnown=true;}}
        public void PlayBite(float duration=.32f){if(IsOctopus||IsOrcaladon)return;biteDuration=Mathf.Max(.15f,duration);biteRemaining=biteDuration;}
        public void ResetBite(){biteRemaining=0;if(MotionRoot!=null){MotionRoot.localPosition=restMotion;MotionRoot.localRotation=restRotation;}}
        public void ResetVisual(){speed=phase=0;ResetBite();if(Orcaladon!=null)Orcaladon.ResetPose();if(legRest!=null)for(int i=0;i<legRest.Length;i++)if(LegBones[i]!=null)LegBones[i].localRotation=legRest[i];if(Animator!=null){if(!Animator.enabled)Animator.enabled=true;movingKnown=false;ApplyMoving();}}
        void LateUpdate(){
            ApplyConfiguredHeight();
            bool physical=enemy!=null&&(!enemy.Alive||(enemy.PhysicsBody!=null&&enemy.PhysicsBody.IsPhysical));
            if(Animator!=null&&Animator.enabled==physical){Animator.enabled=!physical;movingKnown=false;if(!physical)ApplyMoving();}
            if(physical)return;
            if(GameManager.Instance!=null&&!GameManager.Instance.IsPlaying)return;
            float dt=Time.deltaTime;if(dt<=0)return;
            if(IsOrcaladon){if(Orcaladon!=null)Orcaladon.Pose(dt,speed);return;}
            if(IsOctopus&&Animator==null){
                float gait=Mathf.Clamp01(speed/2f);phase=Mathf.Repeat(phase+dt*(gait>0?7.5f:1.7f),Mathf.PI*4);
                if(MotionRoot!=null){MotionRoot.localPosition=restMotion+Vector3.up*Mathf.Abs(Mathf.Sin(phase))*.065f*gait;MotionRoot.localRotation=restRotation*Quaternion.Euler(Mathf.Sin(phase*.5f)*2*gait,0,Mathf.Sin(phase)*(gait>0?7:1.5f));}
            }else if(!IsOctopus){
                float gait=Mathf.Clamp01(speed/2f);phase=Mathf.Repeat(phase+dt*Mathf.Lerp(0,9,gait),Mathf.PI*4);
                if(legRest!=null)for(int i=0;i<legRest.Length;i++)if(LegBones[i]!=null)LegBones[i].localRotation=legRest[i]*Quaternion.Euler(Mathf.Sin(phase+(i==1?Mathf.PI:i==2?Mathf.PI*.5f:0))*26*gait,0,0);
                float lunge=0;
                if(biteRemaining>0){biteRemaining=Mathf.Max(0,biteRemaining-dt);float t=1-biteRemaining/biteDuration;lunge=t<.38f?Mathf.SmoothStep(0,1,t/.38f):1-Mathf.SmoothStep(0,1,(t-.38f)/.62f);}
                if(MotionRoot!=null){MotionRoot.localPosition=restMotion+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.045f*gait)+Vector3.forward*(lunge*.48f);MotionRoot.localRotation=restRotation*Quaternion.Euler(lunge*9,0,Mathf.Sin(phase)*2.5f*gait);}
            }
        }
        void OnDisable(){ResetBite();}
    }
}
