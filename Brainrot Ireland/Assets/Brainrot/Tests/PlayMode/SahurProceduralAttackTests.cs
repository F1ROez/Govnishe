using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class SahurProceduralAttackTests
    {
        GameObject actor;
        ImportedSahurVisual bridge;
        AnimationClip staleClip;
        Transform leftUpper,rightUpper,fore,hand;
        float previousTimeScale;
        [SetUp]public void Setup(){Assert.IsNull(GameManager.Instance,"Запускайте тесты из Edit mode.");previousTimeScale=Time.timeScale;Time.timeScale=1;}
        [UnityTearDown]public IEnumerator Cleanup(){if(actor!=null)Object.Destroy(actor);if(staleClip!=null)Object.Destroy(staleClip);Time.timeScale=previousTimeScale;yield return null;}
        static Transform Joint(string name,Transform parent,Vector3 offset){var bone=new GameObject(name).transform;bone.SetParent(parent,false);bone.localPosition=offset;return bone;}
        void BuildRig(){
            actor=new GameObject("Original Sahur animation test");actor.SetActive(false);actor.transform.position=new Vector3(7,0,-5);actor.transform.rotation=Quaternion.Euler(0,73,0);
            var model=Joint("Model",actor.transform,Vector3.zero);var chest=Joint("spine.002",model,Vector3.up*1.15f);Joint("spine.006",chest,Vector3.up*.6f);
            rightUpper=Joint("upper_arm.R",chest,new Vector3(.35f,.05f,0));fore=Joint("forearm.R",rightUpper,new Vector3(.18f,-.24f,0));hand=Joint("hand.R",fore,new Vector3(.13f,-.2f,.08f));
            leftUpper=Joint("upper_arm.L",chest,new Vector3(-.35f,.05f,0));var leftFore=Joint("forearm.L",leftUpper,new Vector3(-.18f,-.24f,0));Joint("hand.L",leftFore,new Vector3(-.13f,-.2f,.08f));
            var bat=Joint("Bat",model,new Vector3(.5f,.8f,.1f));var tip=Joint("Tip",bat,Vector3.up*.88f);var trail=tip.gameObject.AddComponent<TrailRenderer>();trail.emitting=false;
            // Updating an old generated prefab must not silently resurrect the failed private animation.
            staleClip=new AnimationClip{name="Old private animation",legacy=true};staleClip.SetCurve("spine.002",typeof(Transform),"localPosition.x",AnimationCurve.Constant(0,1,100));
            bridge=actor.AddComponent<ImportedSahurVisual>();bridge.ModelRoot=model;bridge.Weapon=bat;bridge.WeaponTip=tip;bridge.Grip=hand;bridge.RightUpperArm=rightUpper;bridge.RightForearm=fore;bridge.Trail=trail;
            bridge.AttackClip=staleClip;bridge.AttackBones=new[]{chest};actor.SetActive(true);
        }
        [UnityTest]public IEnumerator OrdinarySwingUsesOneHandAndIgnoresStalePrivateClip(){
            BuildRig();yield return null;
            var left=leftUpper.localRotation;var right=rightUpper.localRotation;var position=actor.transform.position;var scale=actor.transform.localScale;
            bridge.PlayAttack(.8f,.25f,false);yield return new WaitForSeconds(.27f);
            Assert.IsTrue(bridge.UsesProceduralAttack);Assert.IsFalse(bridge.UsesRetargetedAttack);Assert.IsFalse(bridge.HeavyAttackActive);
            Assert.That(Quaternion.Angle(left,leftUpper.localRotation),Is.LessThan(.001f),"Левая рука не должна делать второй хват при обычном ударе.");
            Assert.Greater(Quaternion.Angle(right,rightUpper.localRotation),5);Assert.AreEqual(position,actor.transform.position);Assert.AreEqual(scale,actor.transform.localScale);Assert.IsTrue(bridge.Trail.emitting);
            bridge.enabled=false;
            Assert.IsFalse(bridge.AttackActive);Assert.That(Quaternion.Angle(right,rightUpper.localRotation),Is.LessThan(.001f));Assert.IsFalse(bridge.Trail.emitting);Assert.AreEqual(0,bridge.Trail.positionCount);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator HeldChargeRaisesBatAndStrongReleaseUsesBothHandsThenRestoresPose(){
            BuildRig();yield return null;
            var left=leftUpper.localRotation;var right=rightUpper.localRotation;var forePosition=fore.localPosition;var readyHeight=bridge.WeaponTip.position.y;
            bridge.SetChargePose(1,true);yield return null;yield return null;
            Assert.IsTrue(bridge.ChargePoseActive);Assert.IsFalse(bridge.AttackActive);Assert.IsFalse(bridge.Trail.emitting);
            Assert.Greater(bridge.WeaponTip.position.y,readyHeight+.05f,"При удержании бита должна заметно подниматься для замаха.");
            Assert.Greater(Quaternion.Angle(left,leftUpper.localRotation),5);
            bridge.PlayAttack(.8f,.25f,true);yield return new WaitForSeconds(.27f);
            Assert.IsFalse(bridge.ChargePoseActive);Assert.IsTrue(bridge.HeavyAttackActive);Assert.Greater(Quaternion.Angle(left,leftUpper.localRotation),5);
            Assert.IsTrue(bridge.Trail.emitting);Assert.AreEqual(forePosition,fore.localPosition,"IK должна поворачивать суставы без растягивания костей.");
            bridge.ResetAttack();Assert.IsFalse(bridge.AttackActive);Assert.IsFalse(bridge.HeavyAttackActive);Assert.IsFalse(bridge.ChargePoseActive);
            Assert.That(Quaternion.Angle(left,leftUpper.localRotation),Is.LessThan(.001f));Assert.That(Quaternion.Angle(right,rightUpper.localRotation),Is.LessThan(.001f));Assert.AreEqual(Vector3.one,bridge.Weapon.localScale);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator LocomotionLeanResetsAndBatLengthDoesNotPopDuringSwing(){
            BuildRig();yield return null;
            var chest=ImportedSahurVisual.Find(bridge.ModelRoot,"spine.002");var baseline=chest.localRotation;
            var position=actor.transform.position;var scale=bridge.Weapon.localScale;
            bridge.SetLocomotion(new Vector3(3,0,6),false);bridge.Animate(4,false);
            yield return null;yield return null;
            Assert.Greater(Quaternion.Angle(baseline,chest.localRotation),.1f);
            Assert.AreEqual(position,actor.transform.position,"Наклон визуала не перемещает физического героя.");
            bridge.SetAttackReach(6);bridge.PlayAttack(.6f,.2f,true);
            yield return new WaitForSeconds(.23f);
            Assert.AreEqual(scale,bridge.Weapon.localScale,"Сильный удар не должен растягивать биту.");
            bridge.ResetAttack();yield return null;
            Assert.Less(Quaternion.Angle(baseline,chest.localRotation),.001f,"Новый забег/отмена очищает наклон движения.");
        }
        [UnityTest]public IEnumerator CancellingChargeAndCompletingHeavyDoNotLeaveRaisedArms(){
            BuildRig();yield return null;
            var left=leftUpper.localRotation;var right=rightUpper.localRotation;
            bridge.SetChargePose(1,true);yield return null;yield return null;bridge.ResetAttack();
            Assert.IsFalse(bridge.ChargePoseActive);Assert.That(Quaternion.Angle(left,leftUpper.localRotation),Is.LessThan(.001f));Assert.That(Quaternion.Angle(right,rightUpper.localRotation),Is.LessThan(.001f));
            bridge.PlayAttack(.15f,.05f,true);yield return new WaitForSeconds(.23f);
            Assert.IsFalse(bridge.AttackActive);Assert.That(Quaternion.Angle(left,leftUpper.localRotation),Is.LessThan(.001f));Assert.That(Quaternion.Angle(right,rightUpper.localRotation),Is.LessThan(.001f));
            Assert.IsFalse(bridge.Trail.emitting);LogAssert.NoUnexpectedReceived();
        }
    }
}
