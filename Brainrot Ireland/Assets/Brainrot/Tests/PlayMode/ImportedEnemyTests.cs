using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class ImportedEnemyTests
    {
        GameObject actor;float previousTimeScale;
        [SetUp]public void Setup(){previousTimeScale=Time.timeScale;Time.timeScale=1;}
        [UnityTearDown]public IEnumerator Cleanup(){if(actor!=null)Object.Destroy(actor);Time.timeScale=previousTimeScale;yield return null;}
        [UnityTest]public IEnumerator ImportedSharkHasVisibleRigAndBiteDoesNotMovePhysicsRoot(){
            var prefab=Resources.Load<GameObject>("ImportedTralala");if(prefab==null)Assert.Ignore("Сначала выполните Создать или обновить игру.");
            actor=Object.Instantiate(prefab,new Vector3(2,0,3),Quaternion.identity);yield return null;
            var bridge=actor.GetComponent<ImportedEnemyVisual>();Assert.IsNotNull(bridge);Assert.IsFalse(bridge.IsOctopus);Assert.AreEqual(3,bridge.LegBones.Length);
            CheckVisibleMesh(actor);
            Vector3 rootPosition=actor.transform.position;bridge.Animate(3,false);bridge.PlayBite(.6f);yield return null;yield return null;
            Assert.Greater(bridge.MotionRoot.localPosition.z,0,"Укус должен двигать только визуальную модель вперёд.");Assert.AreEqual(rootPosition,actor.transform.position);
            bridge.ResetVisual();Assert.Less(bridge.MotionRoot.localPosition.sqrMagnitude,.0001f);
        }
        [UnityTest]public IEnumerator ToyOctopusHasEyesAndSwaysWithoutMovingPhysicsRoot(){
            var prefab=Resources.Load<GameObject>("ImportedOctopus");if(prefab==null)Assert.Ignore("Сначала выполните Создать или обновить игру.");
            actor=Object.Instantiate(prefab);yield return null;
            var bridge=actor.GetComponent<ImportedEnemyVisual>();Assert.IsNotNull(bridge);Assert.IsTrue(bridge.IsOctopus);CheckVisibleMesh(actor);
            Assert.IsNull(bridge.Animator,"Новая модель не требует рига или аниматора.");
            Assert.IsNotNull(actor.transform.Find("Движение модели/Большие мультяшные глаза"));
            Vector3 position=actor.transform.position;bridge.Animate(3,false);yield return null;yield return null;
            Assert.Greater(Quaternion.Angle(Quaternion.identity,bridge.MotionRoot.localRotation),.01f);Assert.AreEqual(position,actor.transform.position);
            bridge.ResetVisual();Assert.Less(Quaternion.Angle(Quaternion.identity,bridge.MotionRoot.localRotation),.01f);
        }
        static void CheckVisibleMesh(GameObject root){
            var model=root.GetComponent<ImportedEnemyVisual>().ModelRoot;
            var renderers=model.GetComponentsInChildren<Renderer>();Assert.AreEqual(1,renderers.Length);var skin=renderers[0];Assert.IsTrue(skin.enabled);
            Assert.That(skin.bounds.size.y,Is.InRange(.5f,4f),"Модель должна иметь видимую высоту, а не повторять ошибку масштаба Сахура.");
            Assert.That(skin.bounds.min.y,Is.InRange(-.4f,.4f));
            foreach(var material in skin.sharedMaterials){Assert.IsNotNull(material);Assert.AreEqual("Brainrot/ToonCharacter",material.shader.name);var texture=material.GetTexture("_BaseMap");if(texture!=null)Assert.IsInstanceOf<Texture2D>(texture);}
        }
    }
}
