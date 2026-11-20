using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Brainrot.Tests
{
    public sealed class WorldResourceLifecycleTests
    {
        Scene previous,scene;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Проверки окружения запускаются вне активного забега.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Ресурсы острова "+System.Guid.NewGuid());
            SceneManager.SetActiveScene(scene);yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest]public IEnumerator AtmosphereRestoresCameraWhenIslandIsReplacedOrDisabled(){
            var camera=new GameObject("Arena Camera").AddComponent<Camera>();
            var data=camera.GetUniversalAdditionalCameraData();
            data.requiresDepthTexture=false;data.renderPostProcessing=false;data.dithering=false;
            data.antialiasing=AntialiasingMode.None;data.antialiasingQuality=AntialiasingQuality.Low;
            WorldBuilder.Build();yield return null;yield return null;
            Assert.IsTrue(data.renderPostProcessing);
            // Replacement disables the former world immediately, then destroys it at frame end.
            WorldBuilder.Build();yield return null;yield return null;
            Assert.IsTrue(data.renderPostProcessing,"Старая атмосфера не должна откатывать настройки нового острова.");
            var world=GameObject.Find("Procedural Island");world.SetActive(false);
            Assert.IsFalse(data.renderPostProcessing);Assert.IsFalse(data.requiresDepthTexture);Assert.IsFalse(data.dithering);
            Assert.AreEqual(AntialiasingMode.None,data.antialiasing);Assert.AreEqual(AntialiasingQuality.Low,data.antialiasingQuality);
            world.SetActive(true);Assert.IsTrue(data.renderPostProcessing);
        }
        [UnityTest]public IEnumerator LeafPaletteIsSharedAndStallReleasesConstructionMeshes(){
            WorldBuilder.Build();yield return null;
            var world=GameObject.Find("Procedural Island");Texture full=null,young=null;
            foreach(var renderer in world.GetComponentsInChildren<MeshRenderer>()){
                var material=renderer.sharedMaterial;if(material==null)continue;
                if(material.name=="Glossy emerald palm leaves")full=material.GetTexture("_BaseMap");
                if(material.name=="Young lime leaves")young=material.GetTexture("_BaseMap");
            }
            Assert.IsNotNull(full);Assert.AreSame(full,young,"Одинаковая палитра листьев должна занимать одну текстуру.");
            var stall=new GameObject("Проверка лавки").AddComponent<IslandMarketStall>();stall.Build();
            yield return null; // Destruction of construction-only meshes is deferred.
            var owned=typeof(IslandMarketStall).GetField("meshes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(stall) as System.Collections.ICollection;
            Assert.IsNotNull(owned);Assert.AreEqual(stall.GetComponentsInChildren<MeshFilter>(true).Length,owned.Count);
            foreach(var filter in stall.GetComponentsInChildren<MeshFilter>(true))Assert.IsNotNull(filter.sharedMesh);
        }
    }
}
