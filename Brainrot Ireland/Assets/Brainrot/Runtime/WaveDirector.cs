using UnityEngine;
namespace Brainrot {
 public sealed class WaveDirector {
  float time,restTime;
  public int Number{get;private set;}=1;
  public float Progress{get;private set;}
  public bool Resting{get;private set;}
  public bool WaitingForClear{get;private set;}
  public float Remaining{get;private set;}
  public float Intensity=>Resting?0:Envelope(Progress);
  public static float Envelope(float progress){float s=Mathf.Sin(Mathf.PI*Mathf.Clamp01(progress));return s*s;}
  public void Reset(){Number=1;time=restTime=Progress=0;Resting=WaitingForClear=false;Remaining=Mathf.Max(10,GameBalance.Current.Waves.Duration);}
  public void Advance(float dt,int alive=0){
   dt=Mathf.Max(0,dt);var t=GameBalance.Current.Waves;
   if(WaitingForClear){if(alive>0)return;WaitingForClear=false;restTime=0;return;}
   if(Resting){if(alive>0){WaitingForClear=true;return;}restTime+=dt;Remaining=Mathf.Max(0,t.Rest-restTime);if(restTime>=Mathf.Max(0,t.Rest)){Number++;time=0;Progress=0;Resting=false;Remaining=Mathf.Max(10,t.Duration);}return;}
   time+=dt;float duration=Mathf.Max(10,t.Duration);Progress=Mathf.Clamp01(time/duration);Remaining=Mathf.Max(0,duration-time);
   if(time>=duration){Resting=true;WaitingForClear=alive>0;restTime=0;Remaining=Mathf.Max(0,t.Rest);}
  }
  public string Stage=>WaitingForClear?"УНИЧТОЖЬТЕ ОСТАВШИХСЯ":Resting?"ПЕРЕДЫШКА":Progress<.25f?"НАЧАЛО":Progress>.75f?"СПАД":"ПИК";
 }
}
