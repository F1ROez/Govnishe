using UnityEngine;
namespace Brainrot {
 public sealed class GameAudio:MonoBehaviour {
  static AudioClip tone;static AudioSource source;
  public static void Play(float frequency,float duration,float volume){if(source==null){var go=new GameObject("Synth Audio");source=go.AddComponent<AudioSource>();source.spatialBlend=0;float[] samples=new float[4410];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*220/44100)*(1-i/(float)samples.Length);tone=AudioClip.Create("Original synthesized hit",samples.Length,1,44100,false);tone.SetData(samples,0);}source.pitch=frequency/220;source.PlayOneShot(tone,volume);}
 }
}