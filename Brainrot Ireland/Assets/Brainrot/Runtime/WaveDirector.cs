using UnityEngine;
namespace Brainrot {
 public sealed class WaveDirector {
  double elapsed;
  public int Number {get;private set;}=1;
  public float Progress {get;private set;}
  public bool Resting {get;private set;}
  public float Remaining {get;private set;}
  public float Intensity=>Resting?0:Envelope(Progress);
  public static float Envelope(float progress){float s=Mathf.Sin(Mathf.PI*Mathf.Clamp01(progress));return s*s;}
  public void Reset(){elapsed=0;Advance(0);}
  public void Advance(float dt){
   elapsed+=System.Math.Max(0,dt);var t=GameBalance.Current.Waves;
   double duration=System.Math.Max(10,t.Duration),rest=System.Math.Max(0,t.Rest),cycle=duration+rest;
   Number=(int)System.Math.Min(int.MaxValue,1+System.Math.Floor(elapsed/cycle));
   double local=elapsed%cycle;Resting=local>=duration;Progress=(float)System.Math.Min(1,local/duration);
   Remaining=(float)(Resting?cycle-local:duration-local);
  }
  public string Stage=>Resting?"ПЕРЕДЫШКА":Progress<.25f?"НАЧАЛО":Progress>.75f?"СПАД":"ПИК";
 }
}
