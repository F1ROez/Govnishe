using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class MotionVisualLifecycleTests
    {
        GameObject actor,effectsObject;
        [UnityTearDown]public IEnumerator Cleanup(){if(effectsObject!=null)Object.Destroy(effectsObject);if(actor!=null)Object.Destroy(actor);yield return null;}
        [Test]public void ReinitializingFootstepsDoesNotDuplicateTrailAndDisableStopsIt(){
            Assert.IsNull(GameManager.Instance);
            actor=new GameObject("Inactive player for motion effects");actor.SetActive(false);
            var player=actor.AddComponent<PlayerController>();
            effectsObject=new GameObject("Motion effects lifecycle");var effects=effectsObject.AddComponent<HeroMotionEffects>();
            effects.Initialize(player);effects.Initialize(player);
            var trails=actor.GetComponentsInChildren<TrailRenderer>(true);
            Assert.AreEqual(1,trails.Length,"Repeated setup must reuse the scene-owned dash ribbon.");
            Assert.AreEqual(1,effectsObject.GetComponentsInChildren<ParticleSystem>(true).Length);
            trails[0].emitting=true;effects.enabled=false;
            Assert.IsFalse(trails[0].emitting,"The trail belongs to the player hierarchy and must be stopped explicitly when its driver is disabled.");
            Assert.AreEqual(0,trails[0].positionCount);
        }
        [Test]public void DisablingVisualDuringThrowRestoresHeldBatRenderers(){
            actor=new GameObject("Held bat renderer lifecycle");
            var visual=CharacterVisual.Create(actor.transform,false,new Color(.7f,.4f,.15f));
            var renderers=visual.Weapon.GetComponentsInChildren<Renderer>(true);
            var before=new bool[renderers.Length];for(int i=0;i<renderers.Length;i++)before[i]=renderers[i].enabled;
            visual.SetWeaponThrown(true);
            foreach(var renderer in renderers)Assert.IsFalse(renderer.enabled);
            visual.enabled=false;
            for(int i=0;i<renderers.Length;i++)Assert.AreEqual(before[i],renderers[i].enabled,"A pooled/disabled visual must not return with an invisible held bat.");
        }
    }
}
