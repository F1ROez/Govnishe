using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class RetargetedAttackTests
    {
        GameObject actor;
        AnimationClip clip;
        ImportedSahurVisual bridge;
        Transform bone,weapon;
        float previousTimeScale;
        readonly Vector3 baseline=new Vector3(.25f,1,0);
        [SetUp]public void Setup(){Assert.IsNull(GameManager.Instance,"Запускайте из Edit mode.");previousTimeScale=Time.timeScale;Time.timeScale=1;}
        [UnityTearDown]public IEnumerator Cleanup(){if(actor!=null)Object.Destroy(actor);if(clip!=null)Object.Destroy(clip);Time.timeScale=previousTimeScale;yield return null;}
        void BuildActor(){
            actor=new GameObject("Synthetic retarget actor");actor.SetActive(false);actor.transform.position=new Vector3(5,0,-2);
            var model=new GameObject("Model").transform;model.SetParent(actor.transform,false);
            bone=new GameObject("Bone").transform;bone.SetParent(model,false);bone.localPosition=baseline;
            var grip=new GameObject("Grip").transform;grip.SetParent(bone,false);
            weapon=new GameObject("Bat").transform;weapon.SetParent(model,false);
            var tip=new GameObject("Tip").transform;tip.SetParent(weapon,false);tip.localPosition=Vector3.up*.88f;
            var trail=tip.gameObject.AddComponent<TrailRenderer>();trail.emitting=false;
            clip=new AnimationClip{name="Synthetic attack",legacy=true};
            clip.SetCurve("Bone",typeof(Transform),"localPosition.x",AnimationCurve.Linear(0,0,1,1));
            clip.SetCurve("Bat",typeof(Transform),"localPosition.x",AnimationCurve.Constant(0,1,1));
            bridge=actor.AddComponent<ImportedSahurVisual>();bridge.ModelRoot=model;bridge.Weapon=weapon;bridge.WeaponTip=tip;bridge.Grip=grip;bridge.Trail=trail;
            bridge.AttackClip=clip;bridge.AttackBones=new[]{bone};bridge.AttackContactNormalized=.4f;
            actor.SetActive(true);
        }
        [Test]public void RetimingKeepsSourceContactAtGameplayImpactTime(){
            Assert.That(ImportedSahurVisual.MapAttackTime(.09f,.32f,.09f,1.6f,.4f),Is.EqualTo(.64f).Within(.00001f));
            Assert.That(ImportedSahurVisual.MapAttackTime(0,.32f,.09f,1.6f,.4f),Is.EqualTo(0));
            Assert.That(ImportedSahurVisual.MapAttackTime(.32f,.32f,.09f,1.6f,.4f),Is.EqualTo(1.6f).Within(.00001f));
        }
        [UnityTest]public IEnumerator RetargetedPoseMovesOnlyVisualBonesAndCancelRestoresBaseline(){
            BuildActor();yield return null;
            Vector3 position=actor.transform.position,weaponReady=weapon.localPosition;
            bridge.PlayAttack(.8f,.25f);yield return new WaitForSeconds(.27f);
            Assert.IsTrue(bridge.UsesRetargetedAttack);Assert.IsTrue(bridge.AttackActive);
            Assert.That(bone.localPosition.x,Is.InRange(.3f,.7f));Assert.Greater(weapon.localPosition.x,.75f,"Биту должен ставить клип, а не прежняя процедурная дуга.");
            Assert.AreEqual(position,actor.transform.position);Assert.Greater(weapon.localScale.y,1);Assert.IsTrue(bridge.Trail.emitting);
            bridge.ResetAttack();Assert.IsFalse(bridge.AttackActive);Assert.That(Vector3.Distance(baseline,bone.localPosition),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(weaponReady,weapon.localPosition),Is.LessThan(.0001f));Assert.AreEqual(Vector3.one,weapon.localScale);Assert.IsFalse(bridge.Trail.emitting);Assert.AreEqual(0,bridge.Trail.positionCount);
        }
        [UnityTest]public IEnumerator CompletionAndDisableLeaveNoRetargetedPoseBehind(){
            BuildActor();yield return null;
            bridge.PlayAttack(.15f,.05f);yield return new WaitForSeconds(.22f);
            Assert.IsFalse(bridge.AttackActive);Assert.That(Vector3.Distance(baseline,bone.localPosition),Is.LessThan(.0001f));Assert.IsFalse(bridge.Trail.emitting);
            bridge.PlayAttack(.5f,.1f);yield return null;yield return null;bridge.enabled=false;
            Assert.IsFalse(bridge.AttackActive);Assert.That(Vector3.Distance(baseline,bone.localPosition),Is.LessThan(.0001f));Assert.IsFalse(bridge.Trail.emitting);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
