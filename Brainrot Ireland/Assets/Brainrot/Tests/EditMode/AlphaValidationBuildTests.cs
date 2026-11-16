using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class AlphaValidationBuildTests
    {
        [Test] public void EnabledBuildScenesStartWithMenuThenArena()
        {
            var scenes=EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length,2);
            Assert.IsTrue(scenes[0].enabled);Assert.AreEqual(SceneFlow.MenuPath,scenes[0].path);
            Assert.IsTrue(scenes[1].enabled);Assert.AreEqual(Brainrot.Editor.GameBuilder.ScenePath,scenes[1].path);
            foreach(var scene in scenes)if(scene.enabled)Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path),scene.path);
        }
        [TestCase("ImportedSahur")]
        [TestCase("ImportedTralala")]
        [TestCase("ImportedOctopus")]
        [TestCase("ImportedOrcaladon")]
        public void GeneratedCharacterHasGeometryAndAssignedMaterials(string name)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Brainrot/Generated/Resources/"+name+".prefab");
            Assert.IsNotNull(prefab,"Run GameBuilder.Validate before this test.");
            bool geometry=false;
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)){
                Mesh mesh=null;
                if(renderer is SkinnedMeshRenderer skin)mesh=skin.sharedMesh;
                else{var filter=renderer.GetComponent<MeshFilter>();if(filter!=null)mesh=filter.sharedMesh;}
                if(mesh==null||mesh.vertexCount==0)continue;
                geometry=true;Assert.IsTrue(renderer.enabled,"Disabled model renderer in "+name);Assert.Greater(renderer.sharedMaterials.Length,0,"No material slots in "+name);
                foreach(var material in renderer.sharedMaterials){Assert.IsNotNull(material,"Missing material in "+name);Assert.IsNotNull(material.shader,"Missing shader in "+name);Assert.AreNotEqual("Hidden/InternalErrorShader",material.shader.name);}
            }
            Assert.IsTrue(geometry,"No vertex geometry in "+name);
        }
    }
}
