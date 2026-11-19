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
  public void Advance(float dt,int alive=0,bool pendingSpawns=false){
   dt=Mathf.Max(0,dt);var t=GameBalance.Current.Waves;
   if(WaitingForClear){if(alive>0)return;WaitingForClear=false;restTime=0;return;}
   if(Resting){if(alive>0){WaitingForClear=true;return;}Remaining=0;return;}
   float duration=Mathf.Max(10,t.Duration);time=Mathf.Min(duration,time+dt);Progress=Mathf.Clamp01(time/duration);Remaining=Mathf.Max(0,duration-time);
   if(time>=duration&&!pendingSpawns){Resting=true;WaitingForClear=alive>0;restTime=0;Remaining=0;}
  }
  public bool StartNextWave(){
   if(!Resting||WaitingForClear)return false;
   Number++;time=restTime=Progress=0;Resting=false;Remaining=Mathf.Max(10,GameBalance.Current.Waves.Duration);return true;
  }
  public WaveRunSnapshot CaptureRunState()=>new WaveRunSnapshot {Number=Number,Time=time,RestTime=restTime,Resting=Resting,WaitingForClear=WaitingForClear};
  public void RestoreRunState(WaveRunSnapshot state){
   var t=GameBalance.Current.Waves;float duration=Mathf.Max(10,t.Duration);
   Number=Mathf.Clamp(state.Number,1,1000000);time=Mathf.Clamp(state.Time,0,duration);restTime=Mathf.Clamp(state.RestTime,0,Mathf.Max(0,t.Rest));
   Resting=state.Resting;WaitingForClear=state.WaitingForClear&&Resting;Progress=Mathf.Clamp01(time/duration);
   Remaining=Resting?0:Mathf.Max(0,duration-time);
  }
  public string Stage=>WaitingForClear?"УНИЧТОЖЬТЕ ОСТАВШИХСЯ":Resting?"ПЕРЕДЫШКА":Progress<.25f?"НАЧАЛО":Progress>=1?"ПОДКРЕПЛЕНИЯ":Progress>.75f?"СПАД":"ПИК";
 }
}
