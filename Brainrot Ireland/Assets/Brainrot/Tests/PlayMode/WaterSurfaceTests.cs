using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Brainrot.Tests
{
    public sealed class WaterSurfaceTests
    {
        Scene scene,previous;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Run world tests without an active game.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Water surface "+System.Guid.NewGuid());
            SceneManager.SetActiveScene(scene);WorldBuilder.Build();yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
        }
        [Test]public void TropicalWaterKeepsAuthoredPaletteInOneVisualOnlyPass(){
            var ocean=GameObject.Find("Endless ocean");Assert.IsNotNull(ocean);
            Assert.IsNull(ocean.GetComponent<Collider>(),"A visual ocean must not block gameplay or dashes.");
            var material=ocean.GetComponent<Renderer>().sharedMaterial;Assert.IsNotNull(material);
            Assert.AreEqual("Brainrot/TropicalWater",material.shader.name);Assert.AreEqual(1,material.passCount);
            var tuning=GameBalance.Current.Ocean;
            Assert.AreEqual(tuning.DeepColor,material.GetColor("_BaseColor"));Assert.AreEqual(tuning.ShallowColor,material.GetColor("_ShallowColor"));
            Assert.AreEqual(tuning.WaveSpeed,material.GetFloat("_WaveSpeed"),.001f);
            Assert.AreEqual(tuning.WaveScale,material.GetFloat("_WaveScale"),.001f);
            Assert.AreEqual(tuning.WaveStrength,material.GetFloat("_WaveStrength"),.001f);
            Assert.IsTrue(material.HasProperty("_FoamColor"));Assert.IsTrue(material.HasProperty("_CausticStrength"));
            Assert.IsTrue(material.HasProperty("_Shoreline"));Assert.Greater(material.GetVector("_Shoreline").w,1);
        }
        [UnityTest]public IEnumerator PalmAnimationDoesNotDirtyTransformsWhilePaused(){
            var crown=new GameObject("Stationary paused crown");var sway=crown.AddComponent<WorldPalmSway>();sway.Phase=1.7f;
            yield return null;float previousScale=Time.timeScale;var rotation=crown.transform.localRotation;
            try{
                Time.timeScale=0;yield return null;crown.transform.hasChanged=false;yield return null;
                Assert.AreEqual(rotation,crown.transform.localRotation);Assert.IsFalse(crown.transform.hasChanged);
            }finally{Time.timeScale=previousScale;Object.DestroyImmediate(crown);}
        }
    }
}
