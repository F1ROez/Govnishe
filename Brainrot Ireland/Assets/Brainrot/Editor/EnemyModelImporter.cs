using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot.Editor
{
    /// <summary>Original owner files are immutable. Generated rigs, optimized meshes and materials live in Generated.</summary>
    public static class EnemyModelImporter
    {
        const string Art="Assets/Brainrot/Art/Enemies/",Out="Assets/Brainrot/Generated/Enemies/";
        [MenuItem("Tools/TungTung/Обновить модели врагов")]
        public static void GenerateAll(){if(Application.isPlaying)return;Build(false);Build(true);AssetDatabase.SaveAssets();}
        static void Build(bool octopus){
            string name=octopus?"Octopus":"Tralala",source=Art+name+"/"+name+".fbx",output=Out+name+"/";
            if(!File.Exists(source)){Debug.LogWarning("Модель врага отсутствует: "+source);return;}
            Directory.CreateDirectory(output);Directory.CreateDirectory("Assets/Brainrot/Generated/Resources");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(source) as ModelImporter;if(importer==null)return;
            var animationType=octopus?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
            if(importer.animationType!=animationType||importer.optimizeGameObjects||importer.importAnimation!=octopus||!importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard){
                importer.animationType=animationType;importer.optimizeGameObjects=false;importer.importAnimation=octopus;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
            }
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(asset==null){Debug.LogWarning("Unity ещё импортирует модель врага: "+source);return;}
            var root=new GameObject(octopus?"Блюберринни Октопусини":"Тралалеро Тралала");
            try{
                var motion=new GameObject("Движение модели").transform;motion.SetParent(root.transform,false);
                var normalization=new GameObject("Нормализация").transform;normalization.SetParent(motion,false);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(normalization,false);
                foreach(var node in model.GetComponentsInChildren<Transform>(true)){node.gameObject.SetActive(true);node.gameObject.layer=0;}
                foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                var bridge=root.AddComponent<ImportedEnemyVisual>();bridge.ModelRoot=model.transform;bridge.MotionRoot=motion;bridge.Normalization=normalization;bridge.IsOctopus=octopus;bridge.TargetHeight=octopus?1.45f:1.95f;
                if(octopus){
                    var idle=Clip(source,"Idle",output);var walk=Clip(source,"Walk",output);
                    if(idle!=null)idle.SampleAnimation(model,0);
                    CombineOctopus(model,output);
                    var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();animator.applyRootMotion=false;animator.runtimeAnimatorController=Controller(output,idle,walk);animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;bridge.Animator=animator;
                    if(walk==null)Debug.LogWarning("Клип Walk осьминога не найден; используется Idle.");
                }else{
                    foreach(var animator in model.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(animator);
                    bridge.LegBones=RigShark(model,output);
                }
                ApplyMaterials(model,octopus,output);
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)){skin.enabled=true;skin.forceRenderingOff=false;skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;}
                if(!bridge.CalibrateModel())throw new InvalidOperationException("Не удалось измерить модель врага "+name);
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/Brainrot/Generated/Resources/Imported"+name+".prefab");
                Debug.Log("Модель врага обновлена: "+name+(octopus?" — оригинальные Idle/Walk, объединённая скелетная сетка.":" — созданный трёхногий риг и процедурная походка/укус."));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static AnimationClip Clip(string source,string token,string output){
            var original=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__")&&c.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0);if(original==null)return null;
            string path=output+token+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=UnityEngine.Object.Instantiate(original);clip.name=token;clip.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(clip,path);}else EditorUtility.CopySerialized(original,clip);
            clip.name=token;clip.hideFlags=HideFlags.None;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
        }
        static RuntimeAnimatorController Controller(string output,AnimationClip idle,AnimationClip walk){
            string path=output+"Octopus.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            if(!controller.parameters.Any(p=>p.name=="Moving"))controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            var a=machine.AddState("Ожидание");a.motion=idle;machine.defaultState=a;var b=machine.AddState("Ходьба");b.motion=walk!=null?walk:idle;
            var toWalk=a.AddTransition(b);toWalk.hasExitTime=false;toWalk.duration=.16f;toWalk.AddCondition(AnimatorConditionMode.If,0,"Moving");var toIdle=b.AddTransition(a);toIdle.hasExitTime=false;toIdle.duration=.16f;toIdle.AddCondition(AnimatorConditionMode.IfNot,0,"Moving");EditorUtility.SetDirty(controller);return controller;
        }
        static bool IsBone(Transform node){string n=node.name;return n=="root"||n=="head"||n=="body"||n.StartsWith("tentacle_")||n.StartsWith("helper_body_");}
        static void CombineOctopus(GameObject model,string output){
            var filters=model.GetComponentsInChildren<MeshFilter>(true);var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();var bones=new List<Transform>();var materials=new List<Material>();var triangles=new List<List<int>>();
            foreach(var filter in filters){
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;if(renderer==null||mesh==null)continue;
                Transform bone=filter.transform.parent;while(bone!=null&&bone!=model.transform&&!IsBone(bone))bone=bone.parent;if(bone==null)bone=model.transform;
                int boneIndex=bones.IndexOf(bone);if(boneIndex<0){boneIndex=bones.Count;bones.Add(bone);}
                Matrix4x4 matrix=model.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;Matrix4x4 normalMatrix=matrix.inverse.transpose;int offset=vertices.Count;var ns=mesh.normals;var tex=mesh.uv;var vs=mesh.vertices;
                for(int i=0;i<vs.Length;i++){vertices.Add(matrix.MultiplyPoint3x4(vs[i]));normals.Add(ns.Length==vs.Length?normalMatrix.MultiplyVector(ns[i]).normalized:Vector3.up);uv.Add(tex.Length==vs.Length?tex[i]:Vector2.zero);weights.Add(new BoneWeight{boneIndex0=boneIndex,weight0=1});}
                for(int sub=0;sub<mesh.subMeshCount;sub++){
                    var slots=renderer.sharedMaterials;var material=slots.Length>sub?slots[sub]:null;int target=materials.IndexOf(material);if(target<0){target=materials.Count;materials.Add(material);triangles.Add(new List<int>());}
                    foreach(int index in mesh.GetTriangles(sub))triangles[target].Add(index+offset);
                }
                var node=filter.transform;
                UnityEngine.Object.DestroyImmediate(renderer);UnityEngine.Object.DestroyImmediate(filter);
                // Original Walk/Idle animate bones only. Discard the 595 now-empty geometry leaves,
                // retaining every rig transform and the animation binding paths.
                if(node!=model.transform&&node.childCount==0&&!IsBone(node))UnityEngine.Object.DestroyImmediate(node.gameObject);
            }
            var combined=new Mesh{name="Octopus — объединённая сетка"};combined.indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16;combined.SetVertices(vertices);combined.SetNormals(normals);combined.SetUVs(0,uv);combined.boneWeights=weights.ToArray();combined.bindposes=bones.Select(b=>b.worldToLocalMatrix*model.transform.localToWorldMatrix).ToArray();combined.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)combined.SetTriangles(triangles[i],i);combined.RecalculateBounds();combined.RecalculateTangents();combined=SaveMesh(combined,output+"OctopusMesh.asset");
            var go=new GameObject("Объединённая модель осьминога");go.transform.SetParent(model.transform,false);var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=combined;skin.bones=bones.ToArray();skin.rootBone=model.transform;skin.localBounds=combined.bounds;skin.sharedMaterials=materials.ToArray();
        }
        static Transform[] RigShark(GameObject model,string output){
            var filter=model.GetComponentInChildren<MeshFilter>();if(filter==null||filter.sharedMesh==null)throw new InvalidOperationException("Сетка Тралалеро отсутствует.");
            var renderer=filter.GetComponent<MeshRenderer>();var original=filter.sharedMesh;var matrix=model.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var mesh=UnityEngine.Object.Instantiate(original);mesh.name="Тралалеро — скелетная сетка";mesh.vertices=original.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();if(original.normals.Length==original.vertexCount)mesh.normals=original.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();var bounds=mesh.bounds;
            var body=new GameObject("Тело акулы").transform;body.SetParent(model.transform,false);body.localPosition=bounds.center;
            var legs=new Transform[3];Vector3[] pivots={new Vector3(bounds.center.x-bounds.size.x*.27f,bounds.min.y+bounds.size.y*.4f,bounds.center.z+bounds.size.z*.16f),new Vector3(bounds.center.x+bounds.size.x*.27f,bounds.min.y+bounds.size.y*.4f,bounds.center.z+bounds.size.z*.16f),new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*.32f,bounds.center.z-bounds.size.z*.3f)};
            for(int i=0;i<3;i++){legs[i]=new GameObject("Нога "+(i+1)).transform;legs[i].SetParent(model.transform,false);legs[i].localPosition=pivots[i];}
            var bones=new[]{body,legs[0],legs[1],legs[2]};var weights=new BoneWeight[mesh.vertexCount];var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++){
                var v=vertices[i];float legWeight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(bounds.min.y+bounds.size.y*.28f,bounds.min.y+bounds.size.y*.48f,v.y));int index=0;float distance=float.PositiveInfinity;
                for(int k=0;k<3;k++){float d=new Vector2(v.x-pivots[k].x,v.z-pivots[k].z).sqrMagnitude;if(d<distance){distance=d;index=k;}}
                weights[i]=new BoneWeight{boneIndex0=0,weight0=1-legWeight,boneIndex1=index+1,weight1=legWeight};
            }
            mesh.boneWeights=weights;mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*model.transform.localToWorldMatrix).ToArray();mesh.RecalculateTangents();mesh=SaveMesh(mesh,output+"TralalaRig.asset");
            var materials=renderer.sharedMaterials;UnityEngine.Object.DestroyImmediate(renderer);UnityEngine.Object.DestroyImmediate(filter);
            var go=new GameObject("Скелетная модель акулы");go.transform.SetParent(model.transform,false);var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=bones;skin.rootBone=model.transform;skin.localBounds=mesh.bounds;skin.sharedMaterials=materials;return legs;
        }
        static Mesh SaveMesh(Mesh mesh,string path){var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        static void ApplyMaterials(GameObject model,bool octopus,string output){
            Texture2D texture=null;if(!octopus){string path=Art+"Tralala/shaded.png";var t=AssetImporter.GetAtPath(path) as TextureImporter;if(t!=null){if(SahurModelImporter.ConfigureTexture2D(t,false,true,2048))t.SaveAndReimport();texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);}}
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++){
                    string sourceName=slots[i]!=null?slots[i].name:"Default";string safe=new string(sourceName.Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_').ToArray());string path=output+safe+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=sourceName};AssetDatabase.CreateAsset(material,path);}
                    Color color=Color.white;if(octopus){string n=sourceName.ToLowerInvariant();color=n.Contains("black")?new Color(.018f,.035f,.065f):n.Contains("white")?new Color(.96f,.99f,1):n.Contains("sucker")?new Color(.97f,.52f,.44f):n.Contains("light")?new Color(.30f,.66f,.85f):n.Contains("gray")?new Color(.15f,.24f,.33f):new Color(.13f,.45f,.72f);}
                    material.SetColor("_BaseColor",color);material.SetFloat("_Surface",0);material.SetFloat("_AlphaClip",0);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",octopus?.30f:.36f);material.SetTexture("_BaseMap",texture);material.SetTexture("_MainTex",texture);material.renderQueue=2000;EditorUtility.SetDirty(material);slots[i]=material;
                }
                renderer.sharedMaterials=slots;
            }
        }
    }
    sealed class EnemyTexturePostprocessor:AssetPostprocessor
    {
        void OnPreprocessTexture(){if(assetPath=="Assets/Brainrot/Art/Enemies/Tralala/shaded.png")SahurModelImporter.ConfigureTexture2D((TextureImporter)assetImporter,false,true,2048);}
    }
}
