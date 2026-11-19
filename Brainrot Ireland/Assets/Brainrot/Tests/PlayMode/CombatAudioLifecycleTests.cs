using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class CombatAudioLifecycleTests
    {
        [UnitySetUp]public IEnumerator Setup(){Assert.IsNull(GameManager.Instance,"Запускайте PlayMode проверки из Edit Mode.");if(GameAudio.Instance!=null)Object.Destroy(GameAudio.Instance.gameObject);AudioListener.pause=false;yield return null;}
        [UnityTearDown]public IEnumerator Cleanup(){if(GameAudio.Instance!=null)Object.Destroy(GameAudio.Instance.gameObject);AudioListener.pause=false;yield return null;}
        [UnityTest]public IEnumerator RepeatedImpactsUseBoundedSourcesAndPauseCancelsEveryEffect()
        {
            GameAudio.Swing(true);var audio=GameAudio.Instance;Assert.IsNotNull(audio);
            int sources=audio.GetComponentsInChildren<AudioSource>(true).Length;
            for(int i=0;i<200;i++)GameAudio.Impact(true,i%2==0,8);
            Assert.AreEqual(sources,audio.GetComponentsInChildren<AudioSource>(true).Length);Assert.LessOrEqual(sources,11);
            GameAudio.BeginCharge();GameAudio.SetCharge(1,true);GameAudio.StopGameplayEffects();
            Assert.IsFalse(audio.Charging);Assert.AreEqual(0,audio.ActiveVoices);
            yield return null;
        }
        [UnityTest]public IEnumerator CoolingHeavyAbilityCannotStartAChargeLoopAndMissHasNoImpact()
        {
            GameAudio.Swing(false);var audio=GameAudio.Instance;GameAudio.StopGameplayEffects();
            GameAudio.SetCharge(1,false);Assert.IsFalse(audio.Charging);
            GameAudio.Impact(true,true,0);Assert.AreEqual(0,audio.ActiveVoices);
            GameAudio.EndCharge();GameAudio.EndCharge();yield return null;
            Assert.IsFalse(audio.Charging);
        }
    }
}
