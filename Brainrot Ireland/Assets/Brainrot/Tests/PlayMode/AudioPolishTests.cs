using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class AudioPolishTests
    {
        const BindingFlags Fields=BindingFlags.NonPublic|BindingFlags.Instance;
        float oldVolume,oldTimeScale;bool oldPause;AudioClip first,second;
        static T Read<T>(object target,string field)=>(T)target.GetType().GetField(field,Fields).GetValue(target);
        static void Write(object target,string field,object value)=>target.GetType().GetField(field,Fields).SetValue(target,value);
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Run these tests outside a running game.");
            oldVolume=AudioListener.volume;oldPause=AudioListener.pause;oldTimeScale=Time.timeScale;
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            if(GameAudio.Instance!=null)Object.Destroy(GameAudio.Instance.gameObject);
            yield return null;AudioListener.pause=false;AudioListener.volume=0;Time.timeScale=1;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            if(GameAudio.Instance!=null)Object.Destroy(GameAudio.Instance.gameObject);
            if(first!=null)Object.Destroy(first);if(second!=null)Object.Destroy(second);
            yield return null;AudioListener.pause=oldPause;AudioListener.volume=oldVolume;Time.timeScale=oldTimeScale;
        }
        [UnityTest]public IEnumerator NewCombatCuesArePrebuiltFiniteAndReleaseTheirClips(){
            GameAudio.Prewarm();var audio=GameAudio.Instance;var clips=new AudioClip[5];
            var names=new[]{"throwWhoosh","catchTap","dash","hurt","market"};
            for(int i=0;i<names.Length;i++){
                var clip=Read<AudioClip>(audio,names[i]);clips[i]=clip;Assert.IsNotNull(clip);
                var samples=new float[clip.samples];Assert.IsTrue(clip.GetData(samples,0));
                foreach(float sample in samples){Assert.IsFalse(float.IsNaN(sample)||float.IsInfinity(sample));Assert.LessOrEqual(Mathf.Abs(sample),.85f);}
                Assert.Less(Mathf.Abs(samples[0]),.001f);Assert.Less(Mathf.Abs(samples[samples.Length-1]),.003f,"One-shot endpoints must not click.");
            }
            Object.Destroy(audio.gameObject);yield return null;yield return null;
            foreach(var clip in clips)Assert.IsTrue(clip==null,"Synthesized clips must be freed with their owner.");
        }
        [UnityTest]public IEnumerator ShopFeedbackUsesDedicatedBoundedPersistentVoiceAtZeroTimeScale(){
            Time.timeScale=0;GameMusic.PlayPurchase(true);var music=GameMusic.Instance;
            var feedback=Read<AudioSource>(music,"feedbackSource");var button=Read<AudioSource>(music,"buttonSource");
            Assert.AreNotSame(feedback,button);Assert.AreSame(Read<AudioClip>(music,"purchaseClip"),feedback.clip);
            for(int i=0;i<200;i++){GameMusic.PlayButtonClick();GameMusic.PlayPurchase(true);}
            Assert.AreEqual(4,music.GetComponentsInChildren<AudioSource>(true).Length);
            yield return new WaitForSecondsRealtime(.10f);GameMusic.PlayPurchase(false);
            Assert.AreSame(Read<AudioClip>(music,"rejectClip"),feedback.clip);
        }
        [UnityTest]public IEnumerator BackgroundAudioPauseDoesNotAdvanceGameplayPlaylist(){
            GameMusic.SetMenu();var music=GameMusic.Instance;
            first=AudioClip.Create("test track A",22050,1,22050,false);second=AudioClip.Create("test track B",22050,1,22050,false);
            Write(music,"tracks",new[]{first,second});Write(music,"order",new MusicShuffleBag(2,14));GameMusic.SetGameplay();
            var selected=music.CurrentGameplayTrack;Assert.IsNotNull(selected);
            Read<AudioSource>(music,"gameplaySource").Stop();Write(music,"waitingForStart",false);
            AudioListener.pause=true;music.SendMessage("Update");
            Assert.AreSame(selected,music.CurrentGameplayTrack,"A paused source is not an ended song.");yield return null;
        }
    }
}
