using UnityEngine;
namespace Brainrot {
 public sealed class StaminaPool {
  public float Value{get;private set;}
  public float Maximum{get;private set;}=100;
  public bool Exhausted{get;private set;}
  float recovery;
  public void Reset(StaminaTuning t){Maximum=Mathf.Max(1,t.Maximum);Value=Maximum;Exhausted=false;recovery=0;}
  public bool Tick(bool sprintRequested,bool moving,bool dashing,float dt,StaminaTuning t){
   dt=Mathf.Max(0,dt);Maximum=Mathf.Max(1,t.Maximum);Value=Mathf.Min(Value,Maximum);
   if(Exhausted&&Value>=Mathf.Min(Maximum,Mathf.Max(1,t.RestartThreshold)))Exhausted=false;
   bool sprint=sprintRequested&&moving&&!dashing&&!Exhausted&&Value>0;
   if(sprint){Value=Mathf.Max(0,Value-Mathf.Max(.1f,t.DrainPerSecond)*dt);recovery=Mathf.Max(0,t.RestoreDelay);if(Value<=0){Exhausted=true;sprint=false;}}
   else {float ready=Mathf.Max(0,dt-recovery);recovery=Mathf.Max(0,recovery-dt);Value=Mathf.Min(Maximum,Value+Mathf.Max(.1f,t.RestorePerSecond)*ready);}
   return sprint;
  }
 }
}
