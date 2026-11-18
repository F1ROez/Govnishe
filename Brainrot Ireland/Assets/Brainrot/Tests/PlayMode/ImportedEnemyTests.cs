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
        [UnityTest]public IEnumerator OrcaladonHasLargeSkinnedRigAndWalkResetPreservesTheActor(){
            var prefab=Resources.Load<GameObject>("ImportedOrcaladon");if(prefab==null)Assert.Ignore("Сначала создайте игру с моделью Оркаладона.");
            actor=Object.Instantiate(prefab,new Vector3(4,0,-3),Quaternion.Euler(0,35,0));yield return null;
            var bridge=actor.GetComponent<ImportedEnemyVisual>();Assert.IsNotNull(bridge);Assert.IsTrue(bridge.IsOrcaladon);Assert.That(bridge.TargetHeight,Is.EqualTo(3.4f).Within(.01f));
            CheckVisibleMesh(actor);var pose=actor.GetComponent<OrcaladonVisual>();Assert.IsNotNull(pose);
            foreach(var bone in new[]{pose.Body,pose.UpperLeft,pose.UpperRight,pose.ForeLeft,pose.ForeRight,pose.ThighLeft,pose.ThighRight,pose.ShinLeft,pose.ShinRight})Assert.IsNotNull(bone,"Не назначена кость нового рига.");
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();Assert.IsNotNull(skin);Assert.GreaterOrEqual(skin.bones.Length,9);
            Vector3 position=actor.transform.position;Quaternion rotation=actor.transform.rotation;var restArm=pose.UpperLeft.localRotation;var restPosition=bridge.MotionRoot.localPosition;
            bridge.Animate(2.2f,false);yield return new WaitForSeconds(.15f);
            Assert.Greater(Quaternion.Angle(restArm,pose.UpperLeft.localRotation),.1f);Assert.AreEqual(position,actor.transform.position);Assert.AreEqual(rotation,actor.transform.rotation);
            bridge.ResetVisual();Assert.That(Vector3.Distance(restPosition,bridge.MotionRoot.localPosition),Is.LessThan(.0001f));Assert.That(Quaternion.Angle(restArm,pose.UpperLeft.localRotation),Is.LessThan(.01f));
        }
        [UnityTest]public IEnumerator OrcaladonNeutralArmsAndBakedCalibrationStayStableWhenRotated(){
            var prefab=Resources.Load<GameObject>("ImportedOrcaladon");if(prefab==null)Assert.Ignore("Обновите графику и материалы перед тестом.");
            actor=Object.Instantiate(prefab,new Vector3(4,0,-3),Quaternion.Euler(0,53,0));actor.transform.localScale=Vector3.one*1.3f;yield return null;
            var bridge=actor.GetComponent<ImportedEnemyVisual>();var pose=actor.GetComponent<OrcaladonVisual>();pose.ResetPose();
            var relaxed=pose.UpperLeft.localRotation;pose.RestoreBindPose();
            Assert.That(Quaternion.Angle(relaxed,pose.UpperLeft.localRotation),Is.EqualTo(OrcaladonVisual.RelaxedArmRoll).Within(.1f));
            pose.ResetPose();pose.ResetPose();Assert.Less(Quaternion.Angle(relaxed,pose.UpperLeft.localRotation),.01f);
            Assert.Less(Vector3.Dot(pose.ForeLeft.position-pose.UpperLeft.position,actor.transform.up),-.05f,"Левая рука должна естественно опускаться от плеча.");
            Assert.Less(Vector3.Dot(pose.ForeRight.position-pose.UpperRight.position,actor.transform.up),-.05f,"Правая рука должна естественно опускаться от плеча.");
            Assert.IsTrue(bridge.CalibrateModel());var scale=bridge.Normalization.localScale;var offset=bridge.Normalization.localPosition;var position=actor.transform.position;
            Assert.IsTrue(bridge.CalibrateModel());Assert.Less(Vector3.Distance(scale,bridge.Normalization.localScale),.001f);Assert.Less(Vector3.Distance(offset,bridge.Normalization.localPosition),.001f);Assert.AreEqual(position,actor.transform.position);
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();
            try{
                skin.BakeMesh(baked);float low=float.PositiveInfinity,high=float.NegativeInfinity;
                var matrix=actor.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                foreach(var vertex in baked.vertices){float y=matrix.MultiplyPoint3x4(vertex).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
                Assert.That(high-low,Is.EqualTo(3.4f).Within(.015f));Assert.That(low,Is.EqualTo(0).Within(.015f));
            }finally{Object.Destroy(baked);}
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
