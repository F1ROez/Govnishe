using System.Collections.Generic;
using UnityEngine;
namespace Brainrot {
 public enum EnemyKind {Normal=0,Fast=1,Tank=2,MiniBoss=3,King=4,LiriliLarila=5,BrrBrrPatapim=6,CappuccinoAssassino=7,BombardiroCrocodilo=8}
 public sealed class EnemySpawner:MonoBehaviour {
  public readonly List<EnemyController> Active=new List<EnemyController>(64);
  public readonly List<EnemyController> Corpses=new List<EnemyController>(24);
  readonly Dictionary<EnemyKind,ObjectPool<EnemyController>> pools=new Dictionary<EnemyKind,ObjectPool<EnemyController>>();
  public WaveDirector Waves {get;}=new WaveDirector();
  float spawnClock;int announcedWave;
  public static int MaxEnemies=>Mathf.Clamp(Application.isMobilePlatform?GameBalance.Current.Waves.MaxMobile:GameBalance.Current.Waves.MaxDesktop,1,100);
  public IslandZone CurrentZone=>IslandZone.Beach;
  public int CurrentAliveLimit {get{var t=GameBalance.Current.Waves;return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(t.EdgeAliveLimit,t.PeakAliveLimit,Waves.Intensity))+(Waves.Number-1)*Mathf.Max(0,t.ExtraAlivePerWave),1,MaxEnemies);}}
  ObjectPool<EnemyController> Pool(EnemyKind kind){
   if(!pools.TryGetValue(kind,out var pool)){pool=new ObjectPool<EnemyController>(()=>{var go=new GameObject("Тралалеро Тралала");go.transform.SetParent(transform,false);return go.AddComponent<EnemyController>();},MaxEnemies);pools.Add(kind,pool);}return pool;
  }
  public EnemyController Spawn(EnemyKind kind,Vector3 position,float difficulty=1){
   if(kind!=EnemyKind.Normal||Active.Count>=CurrentAliveLimit)return null;
   var enemy=Pool(kind).Get();position.y=0;enemy.Initialize(this,kind,1,position);Active.Add(enemy);return enemy;
  }
  public void Tick(float dt){
   var game=GameManager.Instance;if(game==null||!game.IsPlaying||dt<=0)return;
   Waves.Advance(dt);
   if(announcedWave!=Waves.Number){announcedWave=Waves.Number;spawnClock=Mathf.Max(0,GameBalance.Current.Waves.InitialDelay);game.Notify("ВОЛНА "+announcedWave,3);}
   if(!Waves.Resting){
    spawnClock-=dt;
    if(spawnClock<=0){
     var t=GameBalance.Current.Waves;spawnClock=Interval(t,Waves.Intensity);
     if(Active.Count<CurrentAliveLimit){float a=Random.Range(0,Mathf.PI*2);Spawn(EnemyKind.Normal,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*28);}
    }
   }
   for(int i=Active.Count-1;i>=0&&game.IsPlaying;i--)if(i<Active.Count)Active[i].Tick(dt);
  }
  public static float Interval(WaveTuning tuning,float intensity)=>Mathf.Lerp(Mathf.Max(.15f,tuning.SlowInterval),Mathf.Max(.15f,tuning.PeakInterval),Mathf.Clamp01(intensity));
  public int CountInZone(IslandZone zone)=>zone==IslandZone.Beach?Active.Count:0;
  internal void Killed(EnemyController enemy){
   if(!Active.Remove(enemy))return;Corpses.Add(enemy);var t=GameBalance.Current.Bodies;
   int limit=Mathf.Clamp(Application.isMobilePlatform?t.MaxCorpsesMobile:t.MaxCorpsesPC,1,40);
   while(Corpses.Count>limit)ReleaseCorpse(Corpses[0]);
  }
  public void ReleaseCorpse(EnemyController enemy){if(enemy==null||!Corpses.Remove(enemy))return;Pool(enemy.Kind).Release(enemy);}
  internal void Despawn(EnemyController enemy){if(enemy==null)return;if(Active.Remove(enemy))Pool(enemy.Kind).Release(enemy);else ReleaseCorpse(enemy);}
  public void ResetRun(){
   for(int i=Active.Count-1;i>=0;i--)Pool(Active[i].Kind).Release(Active[i]);Active.Clear();
   for(int i=Corpses.Count-1;i>=0;i--)Pool(Corpses[i].Kind).Release(Corpses[i]);Corpses.Clear();
   Waves.Reset();announcedWave=0;spawnClock=0;
  }
 }
}
