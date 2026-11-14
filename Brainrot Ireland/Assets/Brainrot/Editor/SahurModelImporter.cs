using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Owner FBX stays untouched; Unity-generated materials, clips and runtime prefab live under Generated.</summary>
    public static class SahurModelImporter
    {
        const string Source="Assets/Brainrot/Art/Sahur/";
        const string Output="Assets/Brainrot/Generated/Sahur/";
        const string PrefabPath="Assets/Brainrot/Generated/Resources/ImportedSahur.prefab";
        [MenuItem("Tools/TungTung/Обновить модель Сахура")]
        public static void BuildPrefab()
        {
            if(Application.isPlaying)return;
            string modelPath=Source+"ZAHUR.fbx";
            if(!File.Exists(modelPath)){Debug.LogWarning("Модель Сахура не найдена: "+modelPath);return;}
            Directory.CreateDirectory(Output);Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));AssetDatabase.Refresh();
            var importer=AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if(importer==null)return;
            bool reimport=importer.animationType!=ModelImporterAnimationType.Generic||importer.optimizeGameObjects||!importer.importAnimation||importer.materialImportMode!=ModelImporterMaterialImportMode.None;
            if(reimport){importer.animationType=ModelImporterAnimationType.Generic;importer.optimizeGameObjects=false;importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();}
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if(asset==null){Debug.LogWarning("Unity ещё импортирует FBX Сахура. Повторите создание игры после импорта.");return;}
            Texture2D albedo=Texture(Source+"Material_ALB.png",false,true);
            Texture2D normal=Texture(Source+"Material_NRM.png",true,false);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Output+"Sahur.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Сахур — материал владельца"};material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.42f);AssetDatabase.CreateAsset(material,Output+"Sahur.mat");}
            ClearNon2DMaps(material);
            if(albedo!=null){material.SetTexture("_BaseMap",albedo);material.SetTexture("_MainTex",albedo);}
            if(normal!=null){material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",.55f);material.EnableKeyword("_NORMALMAP");}
            var packed=Smoothness();if(packed!=null){material.SetTexture("_MetallicGlossMap",packed);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",1);material.EnableKeyword("_METALLICSPECGLOSSMAP");}
            EditorUtility.SetDirty(material);
            var root=new GameObject("Сахур — импортированная модель");
            try
            {
                var normalization=new GameObject("Нормализация масштаба").transform;normalization.SetParent(root.transform,false);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.transform.SetParent(normalization,false);
                foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();}
                AnimationClip idle=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().FirstOrDefault(c=>c.name.IndexOf("Idle",StringComparison.OrdinalIgnoreCase)>=0&&!c.name.StartsWith("__"));
                if(idle!=null)idle=LoopingIdle(idle);
                // The supplied Idle animates metarig scale from .229 to1. Sample before measuring and normalize OUTSIDE animated hierarchy.
                if(idle!=null)idle.SampleAnimation(model,0);
                Bounds bounds=Measure(model);float scale=1.9f/Mathf.Max(.01f,bounds.size.y);
                var hips=ImportedSahurVisual.Find(model.transform,"spine");Vector3 center=hips!=null?hips.position:bounds.center;
                normalization.localScale=Vector3.one*scale;normalization.localPosition=new Vector3(-center.x*scale,-bounds.min.y*scale,-center.z*scale);
                var bridge=root.AddComponent<ImportedSahurVisual>();bridge.ModelRoot=model.transform;
                bridge.Weapon=ImportedSahurVisual.Find(model.transform,"Bat");bridge.Grip=ImportedSahurVisual.Find(model.transform,"hand.R");bridge.RightUpperArm=ImportedSahurVisual.Find(model.transform,"upper_arm.R");bridge.RightForearm=ImportedSahurVisual.Find(model.transform,"forearm.R");
                if(bridge.Weapon!=null){
                    var end=ImportedSahurVisual.Find(bridge.Weapon,"Bat_end");float length=end!=null?Mathf.Max(.1f,end.localPosition.magnitude):.8877f;
                    var tip=new GameObject("Кончик импортированной биты").transform;tip.SetParent(bridge.Weapon,false);tip.localPosition=Vector3.up*length;bridge.WeaponTip=tip;
                    var trail=tip.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=TrailMaterial();trail.time=.18f;trail.minVertexDistance=.025f;trail.emitting=false;trail.widthCurve=AnimationCurve.Linear(0,.22f,1,0);
                    var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});trail.colorGradient=gradient;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows=false;bridge.Trail=trail;
                }
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){var b=skin.localBounds;b.Expand(4);skin.localBounds=b;}
                var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();animator.applyRootMotion=false;bridge.Animator=animator;
                animator.runtimeAnimatorController=Controller(model.transform,idle);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
                Debug.Log("Модель Сахура обновлена: оригинальная сетка/текстуры, Idle из FBX, созданные костные ходьба и бег, взмах импортированной битой.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        // Texture type (Default/NormalMap) and texture shape (2D/Cube) are independent settings.
        internal static bool ConfigureTexture2D(TextureImporter importer,bool normal,bool color,int size)
        {
            var kind=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            bool changed=importer.textureType!=kind||importer.textureShape!=TextureImporterShape.Texture2D||importer.sRGBTexture!=color||importer.maxTextureSize!=size;
            if(changed){importer.textureType=kind;importer.textureShape=TextureImporterShape.Texture2D;importer.sRGBTexture=color;importer.maxTextureSize=size;}
            return changed;
        }
        static Texture2D Texture(string path,bool normal,bool color)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)return null;
            if(ConfigureTexture2D(importer,normal,color,2048))importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)Debug.LogWarning("Не удалось загрузить 2D-текстуру Сахура: "+path);
            return texture;
        }
        static void ClearNon2DMaps(Material material)
        {
            foreach(string property in new[]{"_MainTex","_BaseMap","_BumpMap","_MetallicGlossMap"}){
                if(!material.HasProperty(property))continue;
                var texture=material.GetTexture(property);
                if(texture!=null&&texture.dimension!=UnityEngine.Rendering.TextureDimension.Tex2D)material.SetTexture(property,null);
            }
        }
        static Texture2D Smoothness()
        {
            string path=Output+"MetallicSmoothness.png";
            if(!File.Exists(path)){
                string source=Source+"Material_RGH.png";
                if(!File.Exists(source)){Debug.LogWarning("Карта шероховатости Сахура отсутствует. Используется обычная гладкость материала.");return null;}
                // Read the PNG directly: SaveAndReimport can leave LoadAssetAtPath null during an import refresh.
                // This also avoids changing the owner's source TextureImporter settings.
                Texture2D rough=null,packed=null;
                try{
                    rough=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                    if(!ImageConversion.LoadImage(rough,File.ReadAllBytes(source),false)){
                        Debug.LogWarning("Не удалось прочитать карту шероховатости Сахура. Используется обычная гладкость материала.");return null;
                    }
                    var pixels=rough.GetPixels32();
                    for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,0,0,(byte)(255-pixels[i].r));
                    packed=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true);
                    packed.SetPixels32(pixels);packed.Apply();
                    var png=packed.EncodeToPNG();
                    if(png==null||png.Length==0){Debug.LogWarning("Не удалось создать карту гладкости Сахура.");return null;}
                    File.WriteAllBytes(path,png);
                }
                catch(Exception exception){Debug.LogWarning("Карта гладкости Сахура пропущена: "+exception.Message);return null;}
                finally{
                    if(packed!=null)UnityEngine.Object.DestroyImmediate(packed);
                    if(rough!=null)UnityEngine.Object.DestroyImmediate(rough);
                }
            }
            // Register the generated file synchronously before asking Unity for its importer.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var output=AssetImporter.GetAtPath(path) as TextureImporter;
            if(output==null){Debug.LogWarning("Карта гладкости ещё недоступна в AssetDatabase. Модель создаётся с обычной гладкостью материала.");return null;}
            if(ConfigureTexture2D(output,false,false,1024))output.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Material TrailMaterial()
        {
            string path=Output+"BatTrail.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null){ClearNon2DMaps(material);EditorUtility.SetDirty(material);return material;}
            material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Белый шлейф",color=Color.white,renderQueue=3000};
            material.SetFloat("_Surface",1);material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");AssetDatabase.CreateAsset(material,path);return material;
        }
        static Bounds Measure(GameObject model)
        {
            bool any=false;Bounds bounds=new Bounds();
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
                var baked=new Mesh();skin.BakeMesh(baked);foreach(var vertex in baked.vertices){var world=skin.transform.TransformPoint(vertex);if(!any){bounds=new Bounds(world,Vector3.zero);any=true;}else bounds.Encapsulate(world);}UnityEngine.Object.DestroyImmediate(baked);
            }
            if(!any)foreach(var renderer in model.GetComponentsInChildren<Renderer>()){if(!any){bounds=renderer.bounds;any=true;}else bounds.Encapsulate(renderer.bounds);}
            return bounds;
        }
        static AnimationClip LoopingIdle(AnimationClip source){
            string path=Output+"OriginalIdle.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip!=null)return clip;
            clip=UnityEngine.Object.Instantiate(source);clip.name="Ожидание из FBX";clip.hideFlags=HideFlags.None;
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,path);return clip;
        }
        static RuntimeAnimatorController Controller(Transform model,AnimationClip idle)
        {
            string path=Output+"Sahur.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller!=null)return controller;
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var machine=controller.layers[0].stateMachine;
            foreach(var state in machine.states)machine.RemoveState(state.state);
            var idleState=machine.AddState("Ожидание");idleState.motion=idle!=null?idle:Gait(model,"Idle",2,0);machine.defaultState=idleState;
            var walk=machine.AddState("Ходьба");walk.motion=Gait(model,"Walk",.8f,28);
            var run=machine.AddState("Бег");run.motion=Gait(model,"Sprint",.55f,42);
            Transition(idleState,walk,AnimatorConditionMode.Greater,.1f);Transition(walk,idleState,AnimatorConditionMode.Less,.1f);
            Transition(walk,run,AnimatorConditionMode.Greater,3.5f);Transition(run,walk,AnimatorConditionMode.Less,3.5f);
            EditorUtility.SetDirty(controller);return controller;
        }
        static void Transition(AnimatorState from,AnimatorState to,AnimatorConditionMode mode,float value){var t=from.AddTransition(to);t.hasExitTime=false;t.duration=.12f;t.AddCondition(mode,value,"Speed");}
        static AnimationClip Gait(Transform model,string name,float length,float amplitude)
        {
            string path=Output+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip!=null)return clip;
            clip=new AnimationClip{name=name,frameRate=30};
            foreach(string boneName in new[]{"thigh.L","thigh.R","shin.L","shin.R","upper_arm.L","upper_arm.R"}){
                var bone=ImportedSahurVisual.Find(model,boneName);if(bone==null)continue;
                var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};Quaternion rest=bone.localRotation;
                Vector3 axis=bone.parent.InverseTransformDirection(model.right).normalized;
                for(int k=0;k<=16;k++){
                    float phase=k/16f*Mathf.PI*2+(boneName.EndsWith(".R")?Mathf.PI:0);float angle=Mathf.Sin(phase)*amplitude;
                    if(boneName.StartsWith("upper"))angle*=-.65f;if(boneName.StartsWith("shin"))angle=Mathf.Max(0,-angle)*.8f;
                    Quaternion q=Quaternion.AngleAxis(angle,axis)*rest;float time=length*k/16f;
                    curves[0].AddKey(time,q.x);curves[1].AddKey(time,q.y);curves[2].AddKey(time,q.z);curves[3].AddKey(time,q.w);
                }
                string relative=AnimationUtility.CalculateTransformPath(bone,model);string[] properties={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
                for(int k=0;k<4;k++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(relative,typeof(Transform),properties[k]),curves[k]);
            }
            // Hold the sampled Idle baseline scale in EVERY locomotion clip; supplied FBX animates metarig scale .229 ->1.
            foreach(var transform in model.GetComponentsInChildren<Transform>(true)){
                string relative=AnimationUtility.CalculateTransformPath(transform,model);Vector3 s=transform.localScale;
                foreach(var component in new[]{new {Name="m_LocalScale.x",Value=s.x},new {Name="m_LocalScale.y",Value=s.y},new {Name="m_LocalScale.z",Value=s.z}})
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(relative,typeof(Transform),component.Name),AnimationCurve.Constant(0,length,component.Value));
            }
            clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,path);return clip;
        }
    }
    sealed class SahurTexturePostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            bool albedo=assetPath=="Assets/Brainrot/Art/Sahur/Material_ALB.png";
            bool normal=assetPath=="Assets/Brainrot/Art/Sahur/Material_NRM.png";
            bool roughness=assetPath=="Assets/Brainrot/Art/Sahur/Material_RGH.png";
            bool packed=assetPath=="Assets/Brainrot/Generated/Sahur/MetallicSmoothness.png";
            if(!albedo&&!normal&&!roughness&&!packed)return;
            SahurModelImporter.ConfigureTexture2D((TextureImporter)assetImporter,normal,albedo,packed?1024:2048);
        }
    }

}
