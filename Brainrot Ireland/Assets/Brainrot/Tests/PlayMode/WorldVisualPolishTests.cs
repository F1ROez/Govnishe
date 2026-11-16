using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Brainrot.Tests
{
    public sealed class WorldVisualPolishTests
    {
        Scene scene,previous;
        [UnitySetUp]public IEnumerator Setup()
        {
            Assert.IsNull(GameManager.Instance,"Проверки окружения запускаются вне активного забега.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Берег и материалы "+System.Guid.NewGuid());
            SceneManager.SetActiveScene(scene);WorldBuilder.Build();yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
        }
        [Test]public void ShoreSlopeMeetsOceanWithoutChangingArenaCollider()
        {
            var slope=GameObject.Find("Пологий мокрый берег");Assert.IsNotNull(slope);
            Assert.IsNull(slope.GetComponent<Collider>(),"Визуальный склон не должен менять игровую поверхность.");
            var mesh=slope.GetComponent<MeshFilter>().sharedMesh;Assert.IsNotNull(mesh);
            const int ring=129;var vertices=mesh.vertices;Assert.AreEqual(ring*3,vertices.Length);
            for(int i=0;i<128;i++){
                var inner=vertices[i];var middle=vertices[ring+i];var outer=vertices[ring*2+i];
                Assert.That(inner.y,Is.EqualTo(-.109f).Within(.001f));Assert.Greater(middle.y,-.43f);Assert.Less(outer.y,-.43f);
                Assert.Greater(outer.sqrMagnitude,inner.sqrMagnitude);
            }
            var sand=GameObject.Find("Sand arena");Assert.IsNotNull(sand.GetComponent<MeshCollider>());
            Assert.Greater(sand.GetComponent<MeshFilter>().sharedMesh.normals[0].y,.9f);
        }
        [Test]public void SandUsesBoundedMipmappedTextureAndShellHasPaintableUvs()
        {
            var renderer=GameObject.Find("Sand arena").GetComponent<MeshRenderer>();
            var texture=renderer.sharedMaterial.GetTexture("_BaseMap") as Texture2D;
            Assert.IsNotNull(texture);Assert.LessOrEqual(texture.width,512);Assert.Greater(texture.mipmapCount,1);
            Assert.IsFalse(texture.isReadable,"CPU-копия процедурной текстуры после загрузки не нужна.");
            var shell=GameObject.Find("Ребристая ракушка").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(shell.vertexCount,shell.uv.Length);
            Assert.Greater(Vector2.Distance(shell.uv[0],shell.uv[shell.uv.Length-1]),1);
        }
        [UnityTest]public IEnumerator PalmSwayRestoresAuthoredRotationWhenDisabled()
        {
            var crown=new GameObject("Проверка исходного наклона кроны");var baseline=Quaternion.Euler(10,33,-8);
            crown.transform.localRotation=baseline;var sway=crown.AddComponent<WorldPalmSway>();sway.Phase=1.3f;
            yield return null;Assert.Greater(Quaternion.Angle(baseline,crown.transform.localRotation),.1f);
            sway.enabled=false;Assert.Less(Quaternion.Angle(baseline,crown.transform.localRotation),.001f);
            sway.enabled=true;yield return null;sway.enabled=false;
            Assert.Less(Quaternion.Angle(baseline,crown.transform.localRotation),.001f);
        }
        [Test]public void EnemyVisualResetKeepsAuthoredMotionRotation()
        {
            var root=new GameObject("Проверка исходного поворота модели");
            var motion=new GameObject("Движение").transform;motion.SetParent(root.transform,false);
            var baseline=Quaternion.Euler(4,22,-3);motion.localRotation=baseline;
            var normalization=new GameObject("Масштаб").transform;normalization.SetParent(motion,false);
            var model=GameObject.CreatePrimitive(PrimitiveType.Cube);model.transform.SetParent(normalization,false);
            var bridge=root.AddComponent<ImportedEnemyVisual>();bridge.MotionRoot=motion;bridge.Normalization=normalization;bridge.ModelRoot=model.transform;
            Assert.IsTrue(bridge.CalibrateModel());motion.localRotation=Quaternion.Euler(35,6,12);bridge.ResetBite();
            Assert.Less(Quaternion.Angle(baseline,motion.localRotation),.001f);
        }
        [Test]public void CharacterShaderKeepsColourAndControllableHighlights()
        {
            var shader=Shader.Find("Brainrot/ToonCharacter");Assert.IsNotNull(shader);
            var material=new Material(shader);
            try{
                Assert.IsTrue(material.HasProperty("_BaseColor"));Assert.IsTrue(material.HasProperty("_BaseMap"));
                Assert.IsTrue(material.HasProperty("_RimStrength"));Assert.IsTrue(material.HasProperty("_GlossStrength"));
                Assert.GreaterOrEqual(material.FindPass("ShadowCaster"),0);Assert.GreaterOrEqual(material.FindPass("DepthOnly"),0);
            }finally{Object.DestroyImmediate(material);}
        }
    }
}
