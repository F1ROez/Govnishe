using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Brainrot.Editor {
 [InitializeOnLoad] public static class GameBuilder {
  const string Root="Assets/Brainrot/Generated"; public const string ScenePath="Assets/Scenes/BrainrotIreland.unity";
  static GameBuilder(){EditorApplication.delayCall+=ConfigureFirstImport;}
  static void ConfigureFirstImport(){if(EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool("BrainrotConfigured",false))return;SessionState.SetBool("BrainrotConfigured",true);Configure();}
  static void Folder(string path){if(!Directory.Exists(path))Directory.CreateDirectory(path);}
  public static GameBalance EnsureBalance(){
   Folder(Root+"/Resources");AssetDatabase.Refresh();
   const string path=Root+"/Resources/GameBalance.asset";
   var balance=AssetDatabase.LoadAssetAtPath<GameBalance>(path);
   if(balance==null){balance=ScriptableObject.CreateInstance<GameBalance>();AssetDatabase.CreateAsset(balance,path);AssetDatabase.SaveAssets();}
   return balance;
  }
  public static PlayerCharacterDefinition EnsureCharacter(){
   Folder(Root+"/Resources");AssetDatabase.Refresh();
   string weaponPath=Root+"/Resources/WoodenBat.asset";
   var weapon=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(weaponPath);
   if(weapon==null){weapon=ScriptableObject.CreateInstance<WeaponDefinition>();AssetDatabase.CreateAsset(weapon,weaponPath);}
   string path=Root+"/Resources/DefaultCharacter.asset";
   var character=AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinition>(path);
   if(character==null){character=ScriptableObject.CreateInstance<PlayerCharacterDefinition>();character.Weapon=weapon;AssetDatabase.CreateAsset(character,path);}
   AssetDatabase.SaveAssets();return character;
  }
  public static void Configure(){Folder(Root);AssetDatabase.Refresh();EnsureBalance();EnsureCharacter();
   string rendererPath=Root+"/ArenaRenderer.asset";var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
   if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,rendererPath);}
   if(renderer.postProcessData==null){renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");EditorUtility.SetDirty(renderer);}
   string pipelinePath=Root+"/ArenaURP.asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
   if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.name="Brainrot URP";AssetDatabase.CreateAsset(pipeline,pipelinePath);}
   pipeline.renderScale=1;pipeline.msaaSampleCount=2;pipeline.shadowDistance=58;pipeline.supportsHDR=true;
   pipeline.mainLightShadowmapResolution=2048;pipeline.shadowCascadeCount=2;pipeline.cascade2Split=.32f;
   // URP exposes these switches with internal setters; configure owned pipeline through serialization.
   var lighting=new SerializedObject(pipeline);
   foreach(string property in new[]{"m_MainLightShadowsSupported","m_SoftShadowsSupported","m_AnyShadowsSupported"}){var field=lighting.FindProperty(property);if(field!=null)field.boolValue=true;}
   var extra=lighting.FindProperty("m_AdditionalLightsRenderingMode");if(extra!=null)extra.intValue=(int)LightRenderingMode.PerPixel;
   lighting.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
   GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
   PlayerSettings.companyName="Brainrot Ireland";PlayerSettings.productName="Brainrot Ireland";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
   PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
   PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.brainrotireland.arena");
   // Preserve the existing input backend; GameInput and UI support Input System and Legacy.
   var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var included=graphics.FindProperty("m_AlwaysIncludedShaders");foreach(string shaderName in new[]{"Brainrot/TropicalWater","Brainrot/TropicalSky","Brainrot/ToonCharacter","Universal Render Pipeline/Lit","Universal Render Pipeline/Unlit","Universal Render Pipeline/Particles/Unlit"}){var shader=Shader.Find(shaderName);if(shader==null)continue;bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int i=included.arraySize;included.InsertArrayElementAtIndex(i);included.GetArrayElementAtIndex(i).objectReferenceValue=shader;}}graphics.ApplyModifiedPropertiesWithoutUndo();
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(SceneFlow.MenuPath,true),new EditorBuildSettingsScene(ScenePath,true)};EnsureAtmosphere();SahurModelImporter.BuildPrefab();EnemyModelImporter.GenerateAll();AssetDatabase.SaveAssets();
  }
  static void EnsureAtmosphere(){
   const string path=Root+"/Resources/IslandAtmosphere.asset";
   if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(path)!=null)return;
   var profile=WorldAtmosphere.CreateProfile(true);AssetDatabase.CreateAsset(profile,path);
   foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
   EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
  }
  [MenuItem("Tools/TungTung/Создать или обновить игру")]
  [MenuItem("Tools/TungTung/Build Game")] public static void BuildGame(){
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   Configure();Folder("Assets/Scenes");Folder(Root+"/Prefabs");Folder("Assets/Resources/Upgrades");AssetDatabase.Refresh();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var root=new GameObject("Brainrot Ireland");var manager=root.AddComponent<GameManager>();manager.Balance=EnsureBalance();manager.PlayerCharacter=EnsureCharacter();EditorSceneManager.SaveScene(scene,ScenePath);
   var template=new GameObject("Pooled Shark");template.AddComponent<EnemyController>();PrefabUtility.SaveAsPrefabAsset(template,Root+"/Prefabs/Shark.prefab");UnityEngine.Object.DestroyImmediate(template);
   var sandPath=Root+"/Sand.mat";if(AssetDatabase.LoadAssetAtPath<Material>(sandPath)==null){var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(1,.8f,.42f);AssetDatabase.CreateAsset(material,sandPath);}
   // Persist fallback definitions as editable ScriptableObjects without entering Play mode.
   var upgradesObject=new GameObject("Upgrade catalog");var system=upgradesObject.AddComponent<UpgradeSystem>();
   var ensure=typeof(UpgradeSystem).GetMethod("EnsureDefinitions",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);ensure.Invoke(system,null);
   var field=typeof(UpgradeSystem).GetField("definitions",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   foreach(var definition in (UpgradeDefinition[])field.GetValue(system)){string path="Assets/Resources/Upgrades/"+definition.Kind+".asset";if(AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path)!=null)continue;var copy=UnityEngine.Object.Instantiate(definition);copy.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(copy,path);}
   UnityEngine.Object.DestroyImmediate(upgradesObject);
   var menu=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Главное меню").AddComponent<MenuBootstrap>();EditorSceneManager.SaveScene(menu,SceneFlow.MenuPath);
   AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("Карта и игровые ресурсы созданы: "+ScenePath);
  }
  [MenuItem("Tools/TungTung/Установить фон меню")]
  public static void ImportMenuBackground(){
   string source=EditorUtility.OpenFilePanelWithFilters("Выберите приложенный фон меню (PNG или JPG)","",new[]{"Изображения","png,jpg,jpeg"});if(string.IsNullOrEmpty(source))return;
   Folder(Root+"/Resources");string extension=Path.GetExtension(source).ToLowerInvariant();string destination=Root+"/Resources/MenuBackground"+extension;
   foreach(string ext in new[]{".png",".jpg",".jpeg"}){string alternate=Root+"/Resources/MenuBackground"+ext;if(alternate!=destination&&File.Exists(alternate))AssetDatabase.DeleteAsset(alternate);}
   if(!string.Equals(Path.GetFullPath(source),Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase))File.Copy(source,destination,true);AssetDatabase.ImportAsset(destination);
   var importer=AssetImporter.GetAtPath(destination) as TextureImporter;if(importer!=null){importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.maxTextureSize=2048;importer.SaveAndReimport();}
   Debug.Log("Фон главного меню установлен.");
  }
  [MenuItem("Tools/TungTung/Собрать для Windows")]
  [MenuItem("Tools/TungTung/Build Windows")] public static void BuildWindows(){BuildGame();Build(BuildTarget.StandaloneWindows64,"Builds/Windows/BrainrotIreland.exe");}
  [MenuItem("Tools/TungTung/Собрать для Android")]
  [MenuItem("Tools/TungTung/Build Android")] public static void BuildAndroid(){BuildGame();PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;Build(BuildTarget.Android,"Builds/Android/BrainrotIreland.apk");}
  static void Build(BuildTarget target,string destination){Directory.CreateDirectory(Path.GetDirectoryName(destination));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{SceneFlow.MenuPath,ScenePath},target=target,locationPathName=destination,options=BuildOptions.None});if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);}
  public static void Validate(){BuildGame();if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null)throw new Exception("Scene missing");if(GraphicsSettings.defaultRenderPipeline==null)throw new Exception("URP missing");Debug.Log("BRAINROT_VALIDATION_OK");}
 }
}