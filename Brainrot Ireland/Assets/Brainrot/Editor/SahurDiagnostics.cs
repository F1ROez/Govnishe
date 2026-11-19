using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Read-only capture of the live rig. Does not rebind, reposition, or replace any assets.</summary>
    public static class SahurDiagnostics
    {
        [MenuItem("Tools/TungTung/Диагностика модели Сахура")]
        public static void Capture()
        {
            if(!Application.isPlaying){EditorUtility.DisplayDialog("Диагностика Сахура","Запустите игру через Play, начните забег и повторите эту команду.","Хорошо");return;}
            var report=new StringBuilder("SAHUR DIAGNOSTICS v1\n");
            report.AppendLine("Unity="+Application.unityVersion+" frame="+Time.frameCount+" timeScale="+Time.timeScale+" graphics="+SystemInfo.graphicsDeviceType);
            var game=GameManager.Instance;var player=game!=null?game.Player:null;
            if(player==null){EditorUtility.DisplayDialog("Диагностика Сахура","Игрок не найден. Начните забег и повторите команду.","Хорошо");return;}
            report.AppendLine("State="+game.State+" Player="+Pose(player.transform));
            if(player.Character!=null)report.AppendLine("Character="+player.Character.name+" VisualScale="+player.Character.VisualScale);
            var bridge=player.GetComponentInChildren<ImportedSahurVisual>(true);
            report.AppendLine("ImportedSahurVisual="+(bridge!=null?Pose(bridge.transform):"MISSING"));
            if(bridge!=null){
                var source=PrefabUtility.GetCorrespondingObjectFromSource(bridge.gameObject);
                report.AppendLine("Prefab source="+AssetDatabase.GetAssetPath(source));
                var field=typeof(ImportedSahurVisual).GetField("Normalization");
                report.AppendLine("Runtime has Normalization field="+(field!=null));
                foreach(var animator in bridge.GetComponentsInChildren<Animator>(true)){
                    report.AppendLine("ANIMATOR "+Pose(animator.transform)+" enabled="+animator.enabled+" initialized="+animator.isInitialized+" culling="+animator.cullingMode+" rootMotion="+animator.applyRootMotion);
                    report.AppendLine("Controller="+AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)+" avatar="+(animator.avatar!=null?animator.avatar.name+" valid="+animator.avatar.isValid:"NULL"));
                    if(animator.isInitialized&&animator.layerCount>0){var state=animator.GetCurrentAnimatorStateInfo(0);report.AppendLine("State hash="+state.fullPathHash+" time="+state.normalizedTime);foreach(var clip in animator.GetCurrentAnimatorClipInfo(0))report.AppendLine("Clip="+clip.clip.name+" weight="+clip.weight);}
                }
            }
            foreach(var renderer in player.GetComponentsInChildren<Renderer>(true)){
                try{DescribeRenderer(report,renderer,player.transform);}catch(Exception e){report.AppendLine("RENDERER DIAGNOSTIC ERROR "+e);}
            }
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                report.AppendLine("CAMERA "+Pose(camera.transform)+" enabled="+camera.enabled+" cullingMask="+camera.cullingMask+" near="+camera.nearClipPlane+" far="+camera.farClipPlane);
                foreach(var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                    report.AppendLine("  skin="+renderer.name+" layerVisible="+((camera.cullingMask&(1<<renderer.gameObject.layer))!=0)+" rendererBoundsInFrustum="+GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),renderer.bounds)+" boundsCenterViewport="+V(camera.WorldToViewportPoint(renderer.bounds.center)));
                }
            }
            report.AppendLine("HIERARCHY");foreach(var node in player.GetComponentsInChildren<Transform>(true))report.AppendLine(Pose(node));
            const string fbx="Assets/Brainrot/Art/Sahur/ZAHUR.fbx";var importer=AssetImporter.GetAtPath(fbx) as ModelImporter;
            if(importer!=null)report.AppendLine("FBX animationType="+importer.animationType+" globalScale="+importer.globalScale+" useFileScale="+importer.useFileScale+" fileScale="+importer.fileScale+" optimizeGameObjects="+importer.optimizeGameObjects);
            report.AppendLine("PREFAB CANDIDATES");foreach(string guid in AssetDatabase.FindAssets("ImportedSahur t:Prefab"))report.AppendLine(AssetDatabase.GUIDToAssetPath(guid));
            string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"SahurDiagnostics.txt");
            try{File.WriteAllText(path,report.ToString());Debug.Log("Отчёт о модели сохранён: "+path);EditorUtility.RevealInFinder(path);}
            catch(Exception e){Debug.LogError("Не удалось сохранить отчёт: "+e.Message);Debug.Log(report.ToString());}
        }
        static void DescribeRenderer(StringBuilder report,Renderer renderer,Transform player)
        {
            report.AppendLine("RENDERER "+Pose(renderer.transform)+" type="+renderer.GetType().Name+" enabled="+renderer.enabled+" forceOff="+renderer.forceRenderingOff+" visible="+renderer.isVisible+" shadow="+renderer.shadowCastingMode+" renderingLayers="+renderer.renderingLayerMask);
            report.AppendLine("World bounds center="+V(renderer.bounds.center)+" size="+V(renderer.bounds.size));
            foreach(var material in renderer.sharedMaterials){
                if(material==null){report.AppendLine("Material NULL");continue;}
                report.AppendLine("MATERIAL "+AssetDatabase.GetAssetPath(material)+" shader="+(material.shader!=null?material.shader.name+" supported="+material.shader.isSupported:"NULL")+" queue="+material.renderQueue+" passes="+material.passCount);
                foreach(string property in new[]{"_BaseColor","_Color"})if(material.HasProperty(property))report.AppendLine(property+"="+material.GetColor(property));
                foreach(string property in new[]{"_Surface","_AlphaClip","_Cutoff","_Cull","_ZWrite"})if(material.HasProperty(property))report.AppendLine(property+"="+material.GetFloat(property));
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})if(material.HasProperty(property)){var texture=material.GetTexture(property);report.AppendLine(property+"="+(texture!=null?AssetDatabase.GetAssetPath(texture)+" "+texture.dimension:"NULL"));}
            }
            var skin=renderer as SkinnedMeshRenderer;if(skin==null)return;
            report.AppendLine("SKIN rootBone="+(skin.rootBone!=null?Pose(skin.rootBone):"NULL")+" bones="+skin.bones.Length+" offscreen="+skin.updateWhenOffscreen+" localBounds="+skin.localBounds);
            report.AppendLine("Mesh="+AssetDatabase.GetAssetPath(skin.sharedMesh)+" vertices="+(skin.sharedMesh!=null?skin.sharedMesh.vertexCount:0));
            var baked=new Mesh();
            try{
                skin.BakeMesh(baked,false);bool any=false;Bounds local=new Bounds();
                foreach(var vertex in baked.vertices){var point=player.InverseTransformPoint(skin.transform.TransformPoint(vertex));if(!any){local=new Bounds(point,Vector3.zero);any=true;}else local.Encapsulate(point);}
                report.AppendLine("BAKED actor-local geometry any="+any+" center="+V(local.center)+" size="+V(local.size)+" min="+V(local.min)+" max="+V(local.max));
            }finally{UnityEngine.Object.DestroyImmediate(baked);}
        }
        static string V(Vector3 value)=>value.ToString("G9");
        static string Pose(Transform node)=>node.name+" parent="+(node.parent!=null?node.parent.name:"NULL")+" active="+node.gameObject.activeInHierarchy+" layer="+node.gameObject.layer+" world="+V(node.position)+" local="+V(node.localPosition)+" rotation="+V(node.localEulerAngles)+" scale="+V(node.localScale)+" lossy="+V(node.lossyScale);
    }
}
