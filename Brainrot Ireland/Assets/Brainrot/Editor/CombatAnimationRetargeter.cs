using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Bakes the owner's locally installed Attack2H01 onto Sahur without altering either source FBX.</summary>
    public static class CombatAnimationRetargeter
    {
        const string AttackName="HumanM@Attack2H01";
        const string InstalledSource="Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/"+AttackName+".fbx";
        const string Output="Assets/Brainrot/Generated/Combat/";
        const string SourceCopy=Output+"Source/AttackSource.fbx";
        const string ClipPath=Output+"SahurAttack2H01.anim";
        const float SampleRate=60;

        sealed class Pose
        {
            readonly Transform[] transforms;
            readonly Vector3[] positions,scales;
            readonly Quaternion[] rotations;
            public Pose(Transform root){
                transforms=root.GetComponentsInChildren<Transform>(true);
                positions=transforms.Select(t=>t.localPosition).ToArray();scales=transforms.Select(t=>t.localScale).ToArray();rotations=transforms.Select(t=>t.localRotation).ToArray();
            }
            public void Restore(){for(int i=0;i<transforms.Length;i++)if(transforms[i]!=null){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}}
        }
        sealed class Rig
        {
            readonly Dictionary<string,Transform> bones;
            readonly bool source;
            public Rig(Transform root,bool isSource){source=isSource;bones=new Dictionary<string,Transform>();foreach(var t in root.GetComponentsInChildren<Transform>(true))if(!bones.ContainsKey(t.name))bones.Add(t.name,t);}
            public Transform Bone(string name){if(!bones.TryGetValue(name,out var bone))throw new InvalidOperationException("В скелете отсутствует кость: "+name);return bone;}
            public Vector3 Position(string name)=>Bone(name).position;
            string Hips=>source?"B-hips":"spine";
            string Head=>source?"B-head":"spine.006";
            string Upper(string side)=>source?"B-upperArm."+side:"upper_arm."+side;
            string Fore(string side)=>source?"B-forearm."+side:"forearm."+side;
            string Hand(string side)=>source?"B-hand."+side:"hand."+side;
            public Vector3 Up=>(Position(Head)-Position(Hips)).normalized;
            public Vector3 Front=>Vector3.Cross(Position(Upper("L"))-Position(Upper("R")),Up).normalized;
            public Quaternion CharacterFrame=>Quaternion.LookRotation(Front,Up);
            public float TorsoLength=>Vector3.Distance(Position(Head),Position(Hips));
            public float ArmLength=>Vector3.Distance(Position(Upper("R")),Position(Fore("R")));
            // Joint directions are portable between these two rigs; their FBX bone axes and rest rotations are not.
            public Quaternion JointFrame(string bone,string child){
                Vector3 aim=child!=null?Position(child)-Position(bone):Up,reference=Front;
                string side=bone.EndsWith(".L",StringComparison.Ordinal)?"L":"R";
                if(bone.Contains("upperArm")||bone.Contains("upper_arm")||bone.Contains("forearm")){
                    reference=Vector3.Cross(Position(Fore(side))-Position(Upper(side)),Position(Hand(side))-Position(Fore(side)));
                    if(reference.sqrMagnitude<.00000001f)reference=Front;
                }
                else if(bone.Contains("hand.")){
                    string index=source?"B-indexFinger01.":"f_index.01.",pinky=source?"B-pinky01.":"f_pinky.01.";
                    reference=Vector3.Cross(Position(index+side)-Position(bone),Position(pinky+side)-Position(bone));
                }
                else if(bone.Contains("foot."))reference=Up;
                return DirectionFrame(aim,reference);
            }
        }
        sealed class Link
        {
            public string Source,Child;
            public Transform Target;
            public Quaternion RestOffset;
        }
        sealed class Curves
        {
            public readonly Transform Bone;
            readonly bool position;
            readonly AnimationCurve[] rotations={new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            readonly AnimationCurve[] positions={new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            Quaternion previous;bool any;
            public Curves(Transform bone,bool recordPosition){Bone=bone;position=recordPosition;}
            public void Record(float time){
                var q=Bone.localRotation;
                if(any&&Quaternion.Dot(previous,q)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);
                previous=q;any=true;
                for(int i=0;i<4;i++)rotations[i].AddKey(time,q[i]);
                if(position)for(int i=0;i<3;i++)positions[i].AddKey(time,Bone.localPosition[i]);
            }
            public void Write(AnimationClip clip,Transform root){
                string path=AnimationUtility.CalculateTransformPath(Bone,root);
                if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Клип удара не должен перемещать корень модели.");
                for(int i=0;i<4;i++)Set(clip,path,"m_LocalRotation."+"xyzw"[i],rotations[i]);
                if(position)for(int i=0;i<3;i++)Set(clip,path,"m_LocalPosition."+"xyz"[i],positions[i]);
            }
            static void Set(AnimationClip clip,string path,string property,AnimationCurve curve){
                for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),curve);
            }
        }

        public static AnimationClip Build(Transform targetModel,out Transform[] attackBones,out float contactNormalized)
        {
            attackBones=Array.Empty<Transform>();contactNormalized=.4f;
            string sourcePath=FindSource();
            if(sourcePath==null){Debug.LogWarning("HumanM@Attack2H01 не найден в Assets. Оставлен прежний удар. Импортируйте выбранную анимацию и снова создайте игру.");return null;}
            var pose=new Pose(targetModel);GameObject sourceObject=null;AnimationClip baked=null;
            try{
                AnimationClip sourceClip=ImportGenericCopy(sourcePath,out var sourceAsset);
                if(sourceClip==null||sourceAsset==null||sourceClip.length<=0)throw new InvalidOperationException("В исходном FBX нет непустой анимации.");
                sourceObject=UnityEngine.Object.Instantiate(sourceAsset);sourceObject.name="Временный скелет удара";sourceObject.hideFlags=HideFlags.HideAndDontSave;
                foreach(var renderer in sourceObject.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                foreach(var animator in sourceObject.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                var source=new Rig(sourceObject.transform,true);var target=new Rig(targetModel,false);
                Quaternion alignment=target.CharacterFrame*Quaternion.Inverse(source.CharacterFrame);
                Quaternion handRest=source.JointFrame("B-hand.R","B-middleFinger01.R");
                Vector3 up=targetModel.up,forward=targetModel.forward,sourceForward=Quaternion.Inverse(alignment)*forward;
                float ratio=target.TorsoLength/Mathf.Max(.00001f,source.TorsoLength),sourceArm=source.ArmLength;
                float ground=Mathf.Min(Vector3.Dot(target.Position("toe.L"),up),Vector3.Dot(target.Position("toe.R"),up));
                var links=CreateLinks(target,source);var hips=target.Bone("spine");var bat=target.Bone("Bat");
                var curves=links.Select(l=>new Curves(l.Target,l.Target==hips)).ToList();curves.Add(new Curves(bat,true));
                baked=new AnimationClip{name="Сахур — двуручный удар",frameRate=SampleRate,legacy=false};
                int samples=Mathf.Max(2,Mathf.CeilToInt(sourceClip.length*SampleRate));
                float peak=float.NegativeInfinity,minReach=float.PositiveInfinity,maxGripError=0;
                var reaches=new List<float>(samples+1);
                for(int i=0;i<=samples;i++){
                    float time=sourceClip.length*i/samples;
                    pose.Restore();sourceClip.SampleAnimation(sourceObject,time);
                    foreach(var link in links)link.Target.rotation=alignment*source.JointFrame(link.Source,link.Child)*link.RestOffset;
                    float foot=Mathf.Min(Vector3.Dot(target.Position("toe.L"),up),Vector3.Dot(target.Position("toe.R"),up));hips.position+=up*(ground-foot);
                    // FBX root scale can change on the first animation sample: normalize using the CURRENT arm length.
                    float currentArm=Mathf.Max(.00001f,source.ArmLength),unit=ratio*sourceArm/currentArm;
                    Vector3 rightHand=target.Position("hand.R");
                    Vector3 leftGoal=rightHand+alignment*(source.Position("B-hand.L")-source.Position("B-hand.R"))*unit;
                    SolveLeftGrip(target,leftGoal,up);
                    maxGripError=Mathf.Max(maxGripError,Vector3.Distance(target.Position("hand.L"),leftGoal)/Mathf.Max(.00001f,target.TorsoLength));
                    Vector3 axis=alignment*(source.JointFrame("B-hand.R","B-middleFinger01.R")*Quaternion.Inverse(handRest)*sourceForward);
                    bat.rotation=DirectionFrame(axis,forward);bat.position=rightHand+forward*(target.TorsoLength*.04f);
                    foreach(var curve in curves)curve.Record(time);
                    float reach=Vector3.Dot(source.Position("B-hand.R")-source.Position("B-hips"),sourceForward)/currentArm+3.5f*Vector3.Dot(axis,forward);
                    reaches.Add(reach);minReach=Mathf.Min(minReach,reach);peak=Mathf.Max(peak,reach);
                }
                if(peak-minReach<.15f)throw new InvalidOperationException("Исходный скелет не движется при выборке клипа. Проверьте импорт анимации.");
                foreach(var curve in curves)curve.Write(baked,targetModel);
                baked.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(baked);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(baked,settings);
                // The later recovery pose reaches farther than the actual strike in Attack2H01.
                // Use the first forward peak after the fastest forward swing, not the absolute furthest pose.
                int fastest=1;
                for(int i=2;i<reaches.Count;i++)if(reaches[i]-reaches[i-1]>reaches[fastest]-reaches[fastest-1])fastest=i;
                int strike=fastest;
                while(strike<samples&&reaches[strike+1]>reaches[strike])strike++;
                contactNormalized=Mathf.Clamp((float)strike/samples,.1f,.85f);
                var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                AnimationClip result;
                if(existing!=null){EditorUtility.CopySerialized(baked,existing);EditorUtility.SetDirty(existing);result=existing;}
                else{AssetDatabase.CreateAsset(baked,ClipPath);result=baked;baked=null;}
                attackBones=links.Select(l=>l.Target).ToArray();
                if(maxGripError>.15f)Debug.LogWarning("Удар перенесён, но левая рука не везде достаёт до хвата. Проверьте анимацию Сахура в Play.");
                Debug.Log("Attack2H01 перенесён на Сахура: "+attackBones.Length+" костей, "+(samples+1)+" поз. Исходный ассет не изменён; клип сохранён локально в Generated/Combat.");
                return result;
            }
            catch(Exception exception){Debug.LogWarning("Двуручный удар не создан: "+exception.Message+" Используется прежний взмах.");return null;}
            finally{pose.Restore();if(sourceObject!=null)UnityEngine.Object.DestroyImmediate(sourceObject);if(baked!=null)UnityEngine.Object.DestroyImmediate(baked);}
        }
        static string FindSource(){
            if(File.Exists(InstalledSource))return InstalledSource;
            return AssetDatabase.FindAssets(AttackName).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&!p.StartsWith(Output,StringComparison.Ordinal)&&Path.GetExtension(p).Equals(".fbx",StringComparison.OrdinalIgnoreCase)&&Path.GetFileNameWithoutExtension(p).Equals(AttackName,StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p,StringComparer.Ordinal).FirstOrDefault();
        }
        static AnimationClip ImportGenericCopy(string sourcePath,out GameObject asset){
            Directory.CreateDirectory(Output+"Source");
            byte[] bytes=File.ReadAllBytes(sourcePath);
            if(!File.Exists(SourceCopy)||!bytes.SequenceEqual(File.ReadAllBytes(SourceCopy)))File.WriteAllBytes(SourceCopy,bytes);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(SourceCopy,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(SourceCopy) as ModelImporter;
            if(importer==null)throw new InvalidOperationException("Unity ещё не зарегистрировал копию FBX.");
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.NoAvatar;
            importer.sourceAvatar=null;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.resampleCurves=true;
            importer.animationCompression=ModelImporterAnimationCompression.Off;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;
            var original=AssetImporter.GetAtPath(sourcePath) as ModelImporter;
            var clips=original!=null&&original.clipAnimations.Length>0?original.clipAnimations:importer.defaultClipAnimations;
            if(clips.Length>0){
                var selected=clips.FirstOrDefault(c=>c.name.IndexOf(AttackName,StringComparison.OrdinalIgnoreCase)>=0)??clips[0];
                selected.loopTime=false;selected.loopPose=false;selected.events=Array.Empty<AnimationEvent>();importer.clipAnimations=new[]{selected};
            }
            importer.SaveAndReimport();
            asset=AssetDatabase.LoadAssetAtPath<GameObject>(SourceCopy);
            return AssetDatabase.LoadAllAssetsAtPath(SourceCopy).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__",StringComparison.Ordinal)&&c.length>0).OrderByDescending(c=>c.length).FirstOrDefault();
        }
        static List<Link> CreateLinks(Rig target,Rig source){
            var links=new List<Link>();
            Add(links,target,source,"B-hips","B-spine","spine","spine.001");
            Add(links,target,source,"B-spine","B-chest","spine.001","spine.002");
            Add(links,target,source,"B-chest","B-neck","spine.002","spine.003");
            Add(links,target,source,"B-chest","B-neck","spine.003","spine.004");
            Add(links,target,source,"B-neck","B-head","spine.004","spine.005");
            Add(links,target,source,"B-neck","B-head","spine.005","spine.006");
            Add(links,target,source,"B-head",null,"spine.006",null);
            foreach(string side in new[]{"L","R"}){
                Add(links,target,source,"B-upperArm."+side,"B-forearm."+side,"upper_arm."+side,"forearm."+side);
                Add(links,target,source,"B-forearm."+side,"B-hand."+side,"forearm."+side,"hand."+side);
                Add(links,target,source,"B-hand."+side,"B-middleFinger01."+side,"hand."+side,"f_middle.01."+side);
                Add(links,target,source,"B-thigh."+side,"B-shin."+side,"thigh."+side,"shin."+side);
                Add(links,target,source,"B-shin."+side,"B-foot."+side,"shin."+side,"foot."+side);
                Add(links,target,source,"B-foot."+side,"B-toe."+side,"foot."+side,"toe."+side);
            }
            return links;
        }
        static void Add(List<Link> links,Rig target,Rig source,string sourceBone,string sourceChild,string targetBone,string targetChild){
            source.Bone(sourceBone);if(sourceChild!=null)source.Bone(sourceChild);
            var bone=target.Bone(targetBone);links.Add(new Link{Source=sourceBone,Child=sourceChild,Target=bone,RestOffset=Quaternion.Inverse(target.JointFrame(targetBone,targetChild))*bone.rotation});
        }
        internal static Quaternion DirectionFrame(Vector3 aim,Vector3 reference){
            if(aim.sqrMagnitude<.00000001f)throw new InvalidOperationException("Нулевая длина кости при переносе анимации.");
            aim.Normalize();var front=Vector3.ProjectOnPlane(reference,aim);
            if(front.sqrMagnitude<.00000001f)front=Vector3.ProjectOnPlane(Vector3.up,aim);
            if(front.sqrMagnitude<.00000001f)front=Vector3.ProjectOnPlane(Vector3.right,aim);
            return Quaternion.LookRotation(front.normalized,aim);
        }
        static void SolveLeftGrip(Rig rig,Vector3 goal,Vector3 up){
            var upper=rig.Bone("upper_arm.L");var fore=rig.Bone("forearm.L");var hand=rig.Bone("hand.L");
            Quaternion wrist=hand.rotation;Vector3 a=upper.position,b=fore.position,c=hand.position;
            float first=Vector3.Distance(a,b),second=Vector3.Distance(b,c),epsilon=Mathf.Max(.000001f,(first+second)*.0001f);
            if(first<epsilon||second<epsilon)return;
            var offset=goal-a;float distance=offset.magnitude;if(distance<epsilon)return;Vector3 direction=offset/distance;
            distance=Mathf.Clamp(distance,Mathf.Abs(first-second)+epsilon,first+second-epsilon);
            var bend=Vector3.ProjectOnPlane(b-a,direction);
            if(bend.sqrMagnitude<epsilon*epsilon)bend=Vector3.Cross(direction,up);
            if(bend.sqrMagnitude<epsilon*epsilon)bend=Vector3.Cross(direction,Vector3.right);
            float cosine=Mathf.Clamp((first*first+distance*distance-second*second)/(2*first*distance),-1,1);
            var elbow=a+direction*(first*cosine)+bend.normalized*(first*Mathf.Sqrt(Mathf.Max(0,1-cosine*cosine)));
            upper.rotation=Quaternion.FromToRotation(b-a,elbow-a)*upper.rotation;
            fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,goal-fore.position)*fore.rotation;hand.rotation=wrist;
        }
    }
}
