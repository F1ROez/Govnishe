using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class SahurThrowPoseTests
    {
        GameObject actor;
        ImportedSahurVisual visual;
        Transform shoulder,forearm,hand,chest;
        float timeScale;
        static Transform Joint(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        [SetUp]public void Setup(){Assert.IsNull(GameManager.Instance);timeScale=Time.timeScale;Time.timeScale=1;}
        [UnityTearDown]public IEnumerator Cleanup(){if(actor!=null)Object.Destroy(actor);Time.timeScale=timeScale;yield return null;}
        void Build(){
            actor=new GameObject("Throw choreography test");actor.SetActive(false);actor.transform.SetPositionAndRotation(new Vector3(4,0,8),Quaternion.Euler(0,63,0));
            var model=Joint("Model",actor.transform,Vector3.zero);chest=Joint("spine.002",model,Vector3.up*1.15f);Joint("spine.006",chest,Vector3.up*.6f);
            shoulder=Joint("upper_arm.R",chest,new Vector3(.35f,.05f,0));forearm=Joint("forearm.R",shoulder,new Vector3(.18f,-.24f,0));hand=Joint("hand.R",forearm,new Vector3(.13f,-.2f,.08f));
            var left=Joint("upper_arm.L",chest,new Vector3(-.35f,.05f,0));left=Joint("forearm.L",left,new Vector3(-.18f,-.24f,0));Joint("hand.L",left,new Vector3(-.13f,-.2f,.08f));
            var bat=Joint("Bat",model,Vector3.zero);visual=actor.AddComponent<ImportedSahurVisual>();visual.ModelRoot=model;visual.Weapon=bat;visual.Grip=hand;visual.RightUpperArm=shoulder;visual.RightForearm=forearm;actor.SetActive(true);
        }
        [UnityTest]public IEnumerator ThrowWindsBehindShoulderThenReleasesForwardWithoutStretching(){
            Build();yield return null;
            var actorPosition=actor.transform.position;var actorRotation=actor.transform.rotation;var forePosition=forearm.localPosition;var handPosition=hand.localPosition;
            float upperLength=Vector3.Distance(shoulder.position,forearm.position),lowerLength=Vector3.Distance(forearm.position,hand.position);
            visual.PlayThrow(.7f,.32f);yield return new WaitForSeconds(.17f);
            Assert.IsTrue(visual.ThrowActive);float wind=Vector3.Dot(hand.position-shoulder.position,actor.transform.forward);
            Assert.Less(wind,0,"Throw needs a visible backward windup.");
            yield return new WaitForSeconds(.17f);
            float release=Vector3.Dot(hand.position-shoulder.position,actor.transform.forward);Assert.Greater(release,wind+.15f,"Release must extend forward, rather than repeat the horizontal melee arc.");
            Assert.That(Vector3.Distance(shoulder.position,forearm.position),Is.EqualTo(upperLength).Within(.0001f));Assert.That(Vector3.Distance(forearm.position,hand.position),Is.EqualTo(lowerLength).Within(.0001f));
            Assert.AreEqual(forePosition,forearm.localPosition);Assert.AreEqual(handPosition,hand.localPosition);Assert.AreEqual(Vector3.one,visual.Weapon.localScale);
            Assert.AreEqual(actorPosition,actor.transform.position);Assert.Less(Quaternion.Angle(actorRotation,actor.transform.rotation),.001f);
            yield return new WaitForSeconds(.45f);Assert.IsFalse(visual.ThrowActive);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator CancellingThrowRestoresPoseAndClearsAttackLock(){
            Build();yield return null;var rest=shoulder.localRotation;var torso=chest.localRotation;
            visual.PlayThrow(.7f,.32f);yield return new WaitForSeconds(.16f);Assert.Greater(Quaternion.Angle(rest,shoulder.localRotation),5);
            visual.ResetAttack();Assert.IsFalse(visual.ThrowActive);Assert.IsFalse(visual.AttackActive);Assert.Less(Quaternion.Angle(rest,shoulder.localRotation),.001f);Assert.Less(Quaternion.Angle(torso,chest.localRotation),.001f);
            visual.PlayAttack(.5f,.2f,false);Assert.IsTrue(visual.AttackActive);Assert.IsFalse(visual.ThrowActive);LogAssert.NoUnexpectedReceived();
        }
    }
}
