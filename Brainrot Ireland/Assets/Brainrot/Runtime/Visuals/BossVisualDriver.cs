using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Boss-only bone poses. The actor/collider never moves here; generated skins are reused.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class BossVisualDriver:MonoBehaviour
    {
        struct Rest {public Transform Bone;public Vector3 Position,Scale;public Quaternion Rotation;}
        readonly List<Rest> rest=new List<Rest>(32);
        readonly List<Transform> tentacles=new List<Transform>(16);
        readonly List<Vector3> tentacleAxes=new List<Vector3>(16);
        readonly List<Mesh> ownedMeshes=new List<Mesh>(1);
        readonly List<SkinnedMeshRenderer> ownedSkins=new List<SkinnedMeshRenderer>(1);
        readonly List<MeshRenderer> originalRenderers=new List<MeshRenderer>(1);
        readonly List<bool> originalVisible=new List<bool>(1);
        EnemyController enemy;ImportedEnemyVisual imported;CharacterVisual character;OrcaladonVisual orca;
        Transform motion,body,head,upperLeft,upperRight,foreLeft,foreRight,thighLeft,thighRight,shinLeft,shinRight;
        Transform[] sharkLegs;
        Animator animator;bool animatorWasEnabled,controlling,generated,initialized;
        float flinch;
        BossPose pose;float progress,strength=1,phase,movePhase;Vector3 lastPosition;bool tracked;
        public bool HasArticulatedRig=>rest.Count>1;
        public int DrivenBoneCount=>rest.Count;
        public void Initialize(EnemyController controller){
            if(controller==null)return;
            if(initialized){ResetPose();enemy=controller;TakeControl();return;}
            enemy=controller;character=GetComponentInChildren<CharacterVisual>(true);
            imported=GetComponentInChildren<ImportedEnemyVisual>(true);orca=GetComponentInChildren<OrcaladonVisual>(true);
            if(imported!=null){
                imported.CalibrateModel();motion=imported.MotionRoot;animator=imported.Animator;
                if(orca!=null){
                    orca.ResetPose();body=orca.Body;upperLeft=orca.UpperLeft;upperRight=orca.UpperRight;foreLeft=orca.ForeLeft;foreRight=orca.ForeRight;
                    thighLeft=orca.ThighLeft;thighRight=orca.ThighRight;shinLeft=orca.ShinLeft;shinRight=orca.ShinRight;
                }else if(imported.IsOctopus){
                    FindOctopusBones(imported.ModelRoot);
                    if(tentacles.Count==0)BuildToyTentacles(imported.ModelRoot);
                }else{
                    sharkLegs=imported.LegBones;body=Find(imported.ModelRoot,"Тело акулы");head=Find(imported.ModelRoot,"head");
                }
            }else if(character!=null){
                motion=Find(character.transform,"Тело");body=motion;
                thighLeft=Find(character.transform,"Left leg");thighRight=Find(character.transform,"Right leg");
                upperLeft=Find(character.transform,"Left arm");upperRight=Find(character.transform,"Bat arm");
                FindOctopusBones(character.transform);
            }
            Remember(motion);Remember(body);Remember(head);Remember(upperLeft);Remember(upperRight);Remember(foreLeft);Remember(foreRight);
            Remember(thighLeft);Remember(thighRight);Remember(shinLeft);Remember(shinRight);
            if(sharkLegs!=null)foreach(var bone in sharkLegs)Remember(bone);
            foreach(var bone in tentacles){
                Remember(bone);var radial=enemy.transform.InverseTransformDirection(bone.position-(body!=null?body.position:enemy.transform.position));radial.y=0;
                tentacleAxes.Add(radial.sqrMagnitude>.00001f?Vector3.Cross(Vector3.up,radial.normalized):Vector3.right);
            }
            if(character!=null&&ownedSkins.Count>0)character.RefreshFeedbackRenderers();
            initialized=true;TakeControl();
        }
        static Transform Find(Transform root,string name){
            if(root==null)return null;
            foreach(var node in root.GetComponentsInChildren<Transform>(true))if(node.name==name)return node;
            return null;
        }
        void FindOctopusBones(Transform root){
            if(root==null)return;
            foreach(var node in root.GetComponentsInChildren<Transform>(true)){
                string name=node.name.ToLowerInvariant();
                if(name=="body"||name=="octopustoybody")body=node;
                else if(name=="head")head=node;
                else if(name.StartsWith("tentacle_")||name.StartsWith("щупальце"))tentacles.Add(node);
            }
        }
        void Remember(Transform bone){
            if(bone==null||bone==transform||bone==enemy.transform)return;
            foreach(var value in rest)if(value.Bone==bone)return;
            rest.Add(new Rest{Bone=bone,Position=bone.localPosition,Rotation=bone.localRotation,Scale=bone.localScale});
        }
        void TakeControl(){
            if(controlling)return;controlling=true;tracked=false;phase=movePhase=0;pose=BossPose.Idle;progress=0;
            if(animator!=null){animatorWasEnabled=animator.enabled;animator.enabled=false;}
            for(int i=0;i<originalRenderers.Count;i++)if(originalRenderers[i]!=null)originalRenderers[i].enabled=false;
            for(int i=0;i<ownedSkins.Count;i++)if(ownedSkins[i]!=null)ownedSkins[i].enabled=originalVisible[i];
            enabled=true;
        }
        void RestoreBones(){
            foreach(var value in rest)if(value.Bone!=null){value.Bone.localPosition=value.Position;value.Bone.localRotation=value.Rotation;value.Bone.localScale=value.Scale;}
        }
        public void ResetPose(){
            RestoreBones();flinch=0;phase=movePhase=0;tracked=false;pose=BossPose.Idle;progress=0;
            if(!controlling)return;controlling=false;
            if(animator!=null)animator.enabled=animatorWasEnabled;
            for(int i=0;i<originalRenderers.Count;i++)if(originalRenderers[i]!=null)originalRenderers[i].enabled=originalVisible[i];
            foreach(var skin in ownedSkins)if(skin!=null)skin.enabled=false;
        }
        public void SetPose(BossPose value,float normalized,float amplitude=1){
            if(!initialized||enemy==null)return;
            TakeControl();pose=value;progress=Mathf.Clamp01(normalized);strength=Mathf.Clamp(amplitude,0,2);
        }
        public void NotifyHit(){flinch=.10f;}
        void LateUpdate(){
            if(!initialized||!controlling||enemy==null)return;
            if(!enemy.IsBoss||!enemy.Alive||(enemy.PhysicsBody!=null&&enemy.PhysicsBody.IsPhysical)){ResetPose();return;}
            var game=GameManager.Instance;if(game!=null&&!game.IsPlaying)return;
            float dt=Mathf.Max(0,Time.deltaTime);phase=Mathf.Repeat(phase+dt*2,Mathf.PI*2);
            var delta=enemy.transform.position-lastPosition;delta.y=0;
            if(tracked)movePhase=Mathf.Repeat(movePhase+Mathf.Min(delta.magnitude,2)*1.9f,Mathf.PI*2);
            lastPosition=enemy.transform.position;tracked=true;
            Sample(pose,progress,strength);
            if(flinch>0){Rotate(body!=null?body:motion,-2.5f*Mathf.Sin(flinch/.10f*Mathf.PI),0,0);flinch=Mathf.Max(0,flinch-dt);}
        }
        /// <summary>Deterministic sampling for previews/tests; never changes combat clocks or actor transforms.</summary>
        public void Sample(BossPose value,float normalized,float amplitude=1){
            if(!initialized)return;RestoreBones();float p=Mathf.Clamp01(normalized),power=Mathf.Clamp(amplitude,0,2);
            float wind=Mathf.SmoothStep(0,1,p/.65f),strike=Mathf.SmoothStep(0,1,(p-.65f)/.35f);
            float walk=value==BossPose.Run||value==BossPose.Charge?1:0;
            float stride=Mathf.Sin(movePhase)*walk;float lean=0,turn=0,arms=0,bend=0,lift=0;
            switch(value){
                case BossPose.Idle:lean=Mathf.Sin(phase)*.65f;break;
                case BossPose.Run:lean=5;arms=15;break;
                case BossPose.Charge:lean=18;arms=32;bend=-28;break;
                case BossPose.Windup:lean=-8*wind;arms=-135*wind;bend=20*wind;break;
                case BossPose.Slam:case BossPose.DoublePunch:
                    lean=Mathf.Lerp(-10*wind,25,strike);arms=Mathf.Lerp(-142*wind,18,strike);bend=Mathf.Lerp(20*wind,30,strike);break;
                case BossPose.RightPunch:case BossPose.LeftPunch:
                    lean=8*strike;turn=(value==BossPose.RightPunch?1:-1)*Mathf.Lerp(-24*wind,24,strike);
                    arms=0;bend=-18;break;
                case BossPose.BiteLeft:case BossPose.BiteRight:case BossPose.BiteHeavy:
                    turn=(value==BossPose.BiteLeft?-1:value==BossPose.BiteRight?1:0)*Mathf.Sin(p*Mathf.PI)*24;
                    lean=Mathf.Lerp(-7*wind,12,strike);break;
                case BossPose.Stomp:
                    lean=-8*wind+15*strike;arms=-25*wind;Rotate(thighRight,-85*wind*(1-strike),0,0);Rotate(shinRight,55*wind*(1-strike),0,0);break;
                case BossPose.Spin:lean=10;turn=p*720;arms=-65;bend=32;break;
                case BossPose.Jump:lift=Mathf.Sin(p*Mathf.PI)*Mathf.Clamp(GameBalance.Current.IslandBosses.For(IslandBossKind.Octopus).JumpHeight,1,8);lean=-12*Mathf.Sin(p*Mathf.PI);arms=-95*Mathf.Sin(p*Mathf.PI);break;
                case BossPose.Tentacle:arms=-45*wind;lean=-6*wind;break;
                case BossPose.Spit:lean=-10*wind+20*strike;bend=-35;break;
                case BossPose.Grab:arms=Mathf.Lerp(-35,-78,wind);bend=-20*wind;lean=12*wind;break;
                case BossPose.Throw:arms=Mathf.Lerp(-95,25,strike);bend=Mathf.Lerp(-60,20,strike);lean=20*strike;break;
                case BossPose.Recover:lean=18*(1-Mathf.SmoothStep(0,1,p));arms=20*(1-p);bend=22*(1-p);break;
                case BossPose.Stunned:lean=16;turn=Mathf.Sin(phase*3)*7;arms=20;break;
                case BossPose.Phase:lean=-10*Mathf.Sin(p*Mathf.PI);arms=-90*Mathf.Sin(p*Mathf.PI);bend=25;break;
                case BossPose.Dead:lean=22;arms=22;break;
            }
            // Preserve planted hip positions when the torso leans; never sink the legs into sand.
            var plantedLeft=thighLeft!=null?thighLeft.position:Vector3.zero;var plantedRight=thighRight!=null?thighRight.position:Vector3.zero;
            Rotate(body,lean*power,turn*power,Mathf.Sin(phase)*.5f*(value==BossPose.Stunned?8:1));
            if(thighLeft!=null)thighLeft.position=plantedLeft;if(thighRight!=null)thighRight.position=plantedRight;
            if(body==null)Rotate(motion,lean*power,turn*power,0);
            if(motion!=null&&lift>0)motion.localPosition+=Vector3.up*(lift/Mathf.Max(.01f,motion.parent!=null?motion.parent.lossyScale.y:1));
            Rotate(upperLeft,arms-stride*16,0,-(orca!=null&&!orca.NeutralArmsDown?22:5));
            Rotate(upperRight,arms+stride*16,0,orca!=null&&!orca.NeutralArmsDown?22:5);
            Rotate(foreLeft,bend-Mathf.Max(0,stride)*8,0,0);Rotate(foreRight,bend-Mathf.Max(0,-stride)*8,0,0);
            if(value==BossPose.LeftPunch){Rotate(upperLeft,Mathf.Lerp(30*wind,-95,strike),0,-12);Rotate(foreLeft,Mathf.Lerp(-65*wind,0,strike),0,0);}
            if(value==BossPose.RightPunch){Rotate(upperRight,Mathf.Lerp(30*wind,-95,strike),0,12);Rotate(foreRight,Mathf.Lerp(-65*wind,0,strike),0,0);}
            if(value!=BossPose.Stomp){Rotate(thighLeft,stride*24,0,0);Rotate(thighRight,-stride*24,0,0);Rotate(shinLeft,Mathf.Max(0,-stride)*20,0,0);Rotate(shinRight,Mathf.Max(0,stride)*20,0,0);}
            if(sharkLegs!=null)for(int i=0;i<sharkLegs.Length;i++){
                float gait=Mathf.Sin(movePhase+(i==1?Mathf.PI:i==2?Mathf.PI*.5f:0))*28*walk;
                if(value==BossPose.Stomp&&i==0)gait-=80*wind*(1-strike);
                if(value==BossPose.BiteLeft||value==BossPose.BiteRight||value==BossPose.BiteHeavy)gait-=14*strike;
                if(value==BossPose.Spin)gait=25+Mathf.Sin(phase*6+i)*15;
                Rotate(sharkLegs[i],gait,0,0);
            }
            float curl=value==BossPose.Grab?65*wind:value==BossPose.Jump?40*Mathf.Sin(p*Mathf.PI):value==BossPose.Tentacle?45*wind:value==BossPose.Spit?22*wind:8;
            for(int i=0;i<tentacles.Count;i++){
                float reach=value==BossPose.Grab?Mathf.Lerp(10,-35,wind):0;
                float ripple=Mathf.Sin(phase*2+i*.72f)*5;
                var bone=tentacles[i];if(bone==null||bone.parent==null)continue;
                var axis=bone.parent.InverseTransformDirection(enemy.transform.TransformDirection(tentacleAxes[i]));
                bone.localRotation=Quaternion.AngleAxis((curl+ripple)*power,axis)*bone.localRotation;
                Rotate(bone,0,reach,Mathf.Sin(phase+i*.65f)*3);
            }
            if(head!=null)Rotate(head,-lean*.45f,turn*.22f,0);
        }
        void Rotate(Transform bone,float pitch,float yaw,float roll){
            if(bone==null||bone.parent==null||enemy==null)return;
            var parent=bone.parent;var actor=enemy.transform;
            bone.localRotation=Quaternion.AngleAxis(pitch,parent.InverseTransformDirection(actor.right))*Quaternion.AngleAxis(yaw,parent.InverseTransformDirection(actor.up))*Quaternion.AngleAxis(roll,parent.InverseTransformDirection(actor.forward))*bone.localRotation;
        }
        void BuildToyTentacles(Transform model){
            if(generated||model==null)return;generated=true;
            var filter=model.GetComponentInChildren<MeshFilter>(true);if(filter==null||filter.sharedMesh==null||!filter.sharedMesh.isReadable)return;
            var original=filter.GetComponent<MeshRenderer>();if(original==null)return;
            var mesh=Instantiate(filter.sharedMesh);mesh.name="Щупальца босса — локальный лёгкий риг";
            var matrix=model.worldToLocalMatrix*filter.transform.localToWorldMatrix;var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);mesh.vertices=vertices;
            var normals=mesh.normals;if(normals.Length==vertices.Length){var inverse=matrix.inverse.transpose;for(int i=0;i<normals.Length;i++)normals[i]=inverse.MultiplyVector(normals[i]).normalized;mesh.normals=normals;}
            mesh.RecalculateBounds();var bounds=mesh.bounds;float height=Mathf.Max(.01f,bounds.size.y),radius=Mathf.Max(.01f,Mathf.Min(bounds.extents.x,bounds.extents.z));
            var bones=new Transform[17];body=new GameObject("OctopusToyBody").transform;body.SetParent(model,false);body.localPosition=bounds.center;bones[0]=body;
            for(int arm=0;arm<8;arm++){
                float a=arm*Mathf.PI*.25f;var ray=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                var baseBone=new GameObject("Щупальце босса "+arm).transform;baseBone.SetParent(model,false);baseBone.localPosition=new Vector3(bounds.center.x,bounds.min.y+height*.19f,bounds.center.z)+ray*radius*.26f;
                var tip=new GameObject("Кончик щупальца "+arm).transform;tip.SetParent(baseBone,false);tip.localPosition=ray*radius*.38f;
                bones[arm*2+1]=baseBone;bones[arm*2+2]=tip;tentacles.Add(baseBone);tentacles.Add(tip);
            }
            var weights=new BoneWeight[vertices.Length];
            for(int i=0;i<vertices.Length;i++){
                var v=vertices[i];float x=v.x-bounds.center.x,z=v.z-bounds.center.z;float radial=Mathf.Sqrt(x*x+z*z)/radius;
                float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(bounds.min.y+height*.20f,bounds.min.y+height*.52f,v.y)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.65f,radial));
                float angle=Mathf.Repeat(Mathf.Atan2(x,z),Mathf.PI*2);int arm=Mathf.RoundToInt(angle/(Mathf.PI*.25f))%8;float tipWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.54f,.95f,radial));
                weights[i]=new BoneWeight{boneIndex0=0,weight0=1-weight,boneIndex1=arm*2+1,weight1=weight*(1-tipWeight),boneIndex2=arm*2+2,weight2=weight*tipWeight};
            }
            var bind=new Matrix4x4[bones.Length];for(int i=0;i<bind.Length;i++)bind[i]=bones[i].worldToLocalMatrix*model.localToWorldMatrix;
            mesh.boneWeights=weights;mesh.bindposes=bind;ownedMeshes.Add(mesh);
            var go=new GameObject("Скелетные щупальца босса");go.transform.SetParent(model,false);var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=bones;skin.rootBone=model;
            skin.sharedMaterials=original.sharedMaterials;skin.shadowCastingMode=original.shadowCastingMode;skin.receiveShadows=original.receiveShadows;skin.localBounds=new Bounds(bounds.center,bounds.size*2);skin.updateWhenOffscreen=true;
            originalRenderers.Add(original);originalVisible.Add(original.enabled);ownedSkins.Add(skin);skin.enabled=false;
        }
        void OnDisable(){ResetPose();}
        void OnDestroy(){foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);}
    }
}
