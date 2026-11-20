using System.Collections.Generic;
using UnityEngine;
namespace Brainrot {
 public enum EnemyKind {Normal=0,Fast=1,Tank=2,MiniBoss=3,King=4,LiriliLarila=5,BrrBrrPatapim=6,CappuccinoAssassino=7,BombardiroCrocodilo=8,BlueberrinniOctopusini=9,Orcaladon=10}
 public sealed class EnemySpawner:MonoBehaviour {
  public readonly List<EnemyController> Active=new List<EnemyController>(64);
  public readonly List<EnemyController> Corpses=new List<EnemyController>(24);
  readonly Dictionary<EnemyKind,ObjectPool<EnemyController>> pools=new Dictionary<EnemyKind,ObjectPool<EnemyController>>();
  public WaveDirector Waves {get;}=new WaveDirector();
  BossSystem bosses;
  public BossSystem Bosses=>bosses!=null?bosses:(bosses=gameObject.AddComponent<BossSystem>());
  public EnemyController CurrentBoss=>bosses!=null?bosses.CurrentBoss:null;
  public bool IsBossWave=>BossSystem.IsBossWave(Waves.Number);
  readonly Queue<EnemyKind> introductions=new Queue<EnemyKind>(2);
  bool octopusIntroduced,orcaladonIntroduced;
  float spawnClock;int announcedWave;
  int budgetWave,plannedCount,spawnedCount,resolvedCount;
  public int WavePlanned=>plannedCount;
  public int WaveSpawned=>spawnedCount;
  public int WaveResolved=>resolvedCount;
  /// <summary>Includes enemies still due to arrive. During cleanup this equals the live count.</summary>
  public int WaveRemaining=>Mathf.Max(0,plannedCount-resolvedCount);
  public float WaveCompletion=>Completion(plannedCount,resolvedCount,Active.Count,Waves.Resting);
  public static float Completion(int planned,int resolved,int alive,bool resting){
   if(planned<=0)return resting&&alive==0?1:0;
   float value=Mathf.Clamp01(resolved/(float)planned);
   return alive>0||!resting?Mathf.Min(.99f,value):value;
  }
  /// <summary>Finite arrivals scheduled by the same slow/peak/slow envelope used during play.</summary>
  public static int PlannedPopulation(WaveTuning tuning,int wave){
   float duration=Mathf.Clamp(tuning.Duration,10,3600),at=Mathf.Max(0,tuning.InitialDelay);
   int count=0;float multiplier=WaveChallenges.ForWave(wave).SpawnIntervalMultiplier;
   while(at<duration&&count<256){count++;at+=Interval(tuning,WaveDirector.Envelope(at/duration))*multiplier;}
   // At least one foe makes even a custom very quiet wave meaningful; introductions consume this budget.
   return Mathf.Max(1,count);
  }
  void EnsureWaveBudget(){
   if(budgetWave==Waves.Number&&plannedCount>0)return;
   budgetWave=Waves.Number;
   if(IsBossWave){plannedCount=1;spawnedCount=Active.Count;resolvedCount=0;introductions.Clear();return;}
   int introductionsDue=(!octopusIntroduced&&budgetWave>=Mathf.Max(1,GameBalance.Current.Octopus.FirstWave)?1:0)+(!orcaladonIntroduced&&budgetWave>=Mathf.Max(1,GameBalance.Current.Orcaladon.FirstWave)?1:0);
   plannedCount=Mathf.Max(Active.Count+introductionsDue,PlannedPopulation(GameBalance.Current.Waves,budgetWave));
   spawnedCount=Active.Count;resolvedCount=0;
  }
  /// <summary>Every warned melee attack keeps a visible slot; distant waiting sharks may still approach.</summary>
  public bool CanBeginMeleeAttack(EnemyController candidate){
   int maximum=Mathf.Min(Mathf.Clamp(GameBalance.Current.Crowd.MaxMeleeWarnings,1,12),Mathf.Clamp(GameBalance.Current.Crowd.MaxSimultaneousMelee,1,8)),attacking=0;
   for(int i=0;i<Active.Count;i++){
    var enemy=Active[i];if(enemy==null||!enemy.Alive||enemy==candidate||enemy.Kind!=EnemyKind.Normal)continue;
    if(enemy.Action==EnemyAction.MeleeWindup||enemy.Action==EnemyAction.Biting){attacking++;if(attacking>=maximum)return false;}
   }
   return attacking<maximum;
  }
  readonly CrowdSeparation crowd=new CrowdSeparation();
  readonly EnemyController[] meleeWarnings=new EnemyController[12];
  readonly float[] meleeDistances=new float[12];
  int meleeWarningCount;
  public int LastCrowdNeighbourChecks=>crowd.LastNeighbourChecks;
  public bool IsMeleeWarningVisible(EnemyController enemy){
   int maximum=Mathf.Clamp(GameBalance.Current.Crowd.MaxMeleeWarnings,1,12);
   if(Active.Count<=maximum||enemy!=null&&enemy.Action==EnemyAction.MeleeWindup)return true;
   for(int i=0;i<meleeWarningCount;i++)if(meleeWarnings[i]==enemy)return true;
   return false;
  }
  void SelectMeleeWarnings(Vector3 player){
   meleeWarningCount=0;int maximum=Mathf.Clamp(GameBalance.Current.Crowd.MaxMeleeWarnings,1,12);
   for(int i=0;i<Active.Count;i++){
    var enemy=Active[i];if(enemy==null||!enemy.Alive||enemy.Kind!=EnemyKind.Normal||enemy.Action!=EnemyAction.MeleeWindup)continue;
    float distance=(enemy.transform.position-player).sqrMagnitude;int insert=meleeWarningCount;
    while(insert>0&&distance<meleeDistances[insert-1])insert--;
    if(insert>=maximum)continue;
    for(int j=Mathf.Min(meleeWarningCount,maximum-1);j>insert;j--){meleeWarnings[j]=meleeWarnings[j-1];meleeDistances[j]=meleeDistances[j-1];}
    meleeWarnings[insert]=enemy;meleeDistances[insert]=distance;meleeWarningCount=Mathf.Min(maximum,meleeWarningCount+1);
   }
  }
  float NextInterval()=>Interval(GameBalance.Current.Waves,Waves.Intensity)*WaveChallenges.ForWave(Waves.Number).SpawnIntervalMultiplier;
  Vector3 NextOceanPosition()=>OceanSpawnPoint(WaveChallenges.SpawnAngle(Waves.Number,Random.value));
  public static int MaxEnemies=>Mathf.Clamp(Application.isMobilePlatform?GameBalance.Current.Waves.MaxMobile:GameBalance.Current.Waves.MaxDesktop,1,100);
  public IslandZone CurrentZone=>IslandZone.Beach;
  public int CurrentAliveLimit {get{var t=GameBalance.Current.Waves;return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(t.EdgeAliveLimit,t.PeakAliveLimit,Waves.Intensity))+(Waves.Number-1)*Mathf.Max(0,t.ExtraAlivePerWave),1,MaxEnemies);}}
  ObjectPool<EnemyController> Pool(EnemyKind kind){
   if(!pools.TryGetValue(kind,out var pool)){pool=new ObjectPool<EnemyController>(()=>{var go=new GameObject(kind==EnemyKind.Orcaladon?"Оркаладон":kind==EnemyKind.BlueberrinniOctopusini?"Блуберрини Октопусини":"Тралалеро Тралала");go.transform.SetParent(transform,false);return go.AddComponent<EnemyController>();},MaxEnemies);pools.Add(kind,pool);}return pool;
  }
  public EnemyController Spawn(EnemyKind kind,Vector3 position,float difficulty=1){
   if(IsBossWave)return null;
   if((kind!=EnemyKind.Normal&&kind!=EnemyKind.BlueberrinniOctopusini&&kind!=EnemyKind.Orcaladon)||Active.Count>=CurrentAliveLimit)return null;
   EnsureWaveBudget();var enemy=Pool(kind).Get();position.y=0;enemy.Initialize(this,kind,1,position);Active.Add(enemy);spawnedCount++;plannedCount=Mathf.Max(plannedCount,spawnedCount);return enemy;
  }
  EnemyController SpawnBoss(){
   if(!IsBossWave||spawnedCount>0||Active.Count>0||Waves.Resting)return null;
   var system=Bosses;system.PrepareWave(Waves.Number);var bossKind=system.SelectedKind;
   var kind=bossKind==IslandBossKind.Octopus?EnemyKind.BlueberrinniOctopusini:bossKind==IslandBossKind.Orcaladon?EnemyKind.Orcaladon:EnemyKind.Normal;
   var player=GameManager.Instance!=null?GameManager.Instance.Player:null;
   var position=player!=null&&player.transform.position.sqrMagnitude>16?-player.transform.position:NextOceanPosition();
   position.y=0;position=position.sqrMagnitude>.001f?position.normalized*13:Vector3.forward*13;
   var enemy=Pool(kind).Get();enemy.Initialize(this,kind,1,position);enemy.ConfigureBoss(bossKind,Waves.Number);
   Active.Add(enemy);spawnedCount=plannedCount=1;system.Register(enemy);return enemy;
  }
  public static Vector3 OceanSpawnPoint(float angle){
   if(!Finite(angle))angle=0;float radius=Mathf.Max(28,IslandMap.CoastRadius(angle)+6);
   return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
  }
  public void Tick(float dt){
   var game=GameManager.Instance;if(game==null||!game.IsPlaying||dt<=0)return;
   EnsureWaveBudget();
   if(!IsBossWave)Waves.Advance(dt,Active.Count,spawnedCount<plannedCount||introductions.Count>0);
   EnsureWaveBudget();
   if(announcedWave!=Waves.Number){
    announcedWave=Waves.Number;Bosses.PrepareWave(announcedWave);spawnClock=Mathf.Max(0,GameBalance.Current.Waves.InitialDelay);if(announcedWave>=4&&!IsBossWave)game.Notify(WaveChallenges.ForWave(announcedWave).Title+"\n"+WaveChallenges.ForWave(announcedWave).Description,4);
    // An introduction is queued until it actually obtains a pool slot. Random spawns cannot overtake it.
    // With equal custom unlock waves both newcomers appear before ordinary random arrivals.
    int octopusWave=Mathf.Max(1,GameBalance.Current.Octopus.FirstWave),orcaWave=Mathf.Max(1,GameBalance.Current.Orcaladon.FirstWave);
    if(!IsBossWave){if(orcaWave<octopusWave){QueueOrcaladon();QueueOctopus();}else{QueueOctopus();QueueOrcaladon();}}
   }
   if(IsBossWave&&!Waves.Resting&&spawnedCount==0)SpawnBoss();
   if(!IsBossWave&&!Waves.Resting&&(spawnedCount<plannedCount||introductions.Count>0)){
    if(introductions.Count>0){
     var kind=introductions.Peek();var introduced=Spawn(kind,NextOceanPosition());
     if(introduced!=null){introductions.Dequeue();if(kind==EnemyKind.BlueberrinniOctopusini)octopusIntroduced=true;else orcaladonIntroduced=true;spawnClock=NextInterval();game.Notify(kind==EnemyKind.Orcaladon?"ОРКАЛАДОН — БЕРЕГИТЕСЬ РАЗГОНА И УДАРА ПО ЗЕМЛЕ!":"БЛУБЕРРИНИ ОКТОПУСИНИ — УКЛОНЯЙТЕСЬ ОТ ПРЫЖКА!",4);}
    }else{
     spawnClock-=dt;
     if(spawnClock<=0){
     spawnClock=NextInterval();
      if(Active.Count<CurrentAliveLimit)Spawn(RandomKind(Random.value,Waves.Number),NextOceanPosition());
     }
    }
   }
   SelectMeleeWarnings(game.Player.transform.position);
   for(int i=Active.Count-1;i>=0&&game.IsPlaying;i--)if(i<Active.Count)Active[i].Tick(dt);
   if(game.IsPlaying)crowd.Resolve(Active,game.Player.transform.position,.48f,dt,GameBalance.Current.Crowd);
  }
  void QueueOctopus(){if(!octopusIntroduced&&Waves.Number>=Mathf.Max(1,GameBalance.Current.Octopus.FirstWave)&&!introductions.Contains(EnemyKind.BlueberrinniOctopusini))introductions.Enqueue(EnemyKind.BlueberrinniOctopusini);}
  void QueueOrcaladon(){if(!orcaladonIntroduced&&Waves.Number>=Mathf.Max(1,GameBalance.Current.Orcaladon.FirstWave)&&!introductions.Contains(EnemyKind.Orcaladon))introductions.Enqueue(EnemyKind.Orcaladon);}
  public static EnemyKind RandomKind(float roll,int wave){
   var t=GameBalance.Current;float orca=wave>=Mathf.Max(1,t.Orcaladon.FirstWave)?Mathf.Clamp01(t.Orcaladon.SpawnChance):0;
   float octopus=wave>=Mathf.Max(1,t.Octopus.FirstWave)?Mathf.Min(1-orca,Mathf.Clamp01(t.Octopus.SpawnChance)):0;
   roll=Mathf.Clamp01(roll);return roll<orca?EnemyKind.Orcaladon:roll<orca+octopus?EnemyKind.BlueberrinniOctopusini:EnemyKind.Normal;
  }
  public static float Interval(WaveTuning tuning,float intensity)=>Mathf.Lerp(Mathf.Max(.15f,tuning.SlowInterval),Mathf.Max(.15f,tuning.PeakInterval),Mathf.Clamp01(intensity));
  public int CountInZone(IslandZone zone)=>zone==IslandZone.Beach?Active.Count:0;
  internal void Killed(EnemyController enemy){
   if(!Active.Remove(enemy))return;resolvedCount=Mathf.Min(spawnedCount,resolvedCount+1);
   if(bosses!=null&&bosses.CurrentBoss==enemy){bosses.Defeated(enemy);if(Active.Count==0)Waves.CompleteEncounter();}
   Corpses.Add(enemy);var t=GameBalance.Current.Bodies;
   int limit=Mathf.Clamp(Application.isMobilePlatform?t.MaxCorpsesMobile:t.MaxCorpsesPC,1,40);
   while(Corpses.Count>limit)ReleaseCorpse(Corpses[0]);
  }
  public void ReleaseCorpse(EnemyController enemy){if(enemy==null||!Corpses.Remove(enemy))return;Pool(enemy.Kind).Release(enemy);}
  internal void Despawn(EnemyController enemy){if(enemy==null)return;if(Active.Remove(enemy)){resolvedCount=Mathf.Min(spawnedCount,resolvedCount+1);Pool(enemy.Kind).Release(enemy);}else ReleaseCorpse(enemy);}
  public SpawnerRunSnapshot CaptureRunState(){
   int count=0;for(int i=0;i<Active.Count;i++)if(Active[i]!=null&&Active[i].Alive)count++;
   var saved=new EnemyRunSnapshot[count];int index=0;
   for(int i=0;i<Active.Count;i++)if(Active[i]!=null&&Active[i].Alive)saved[index++]=Active[i].CaptureRunState();
   EnsureWaveBudget();return new SpawnerRunSnapshot{BudgetWave=budgetWave,PlannedCount=plannedCount,SpawnedCount=spawnedCount,ResolvedCount=resolvedCount,SpawnClock=spawnClock,AnnouncedWave=announcedWave,OctopusIntroduced=octopusIntroduced,OrcaladonIntroduced=orcaladonIntroduced,Enemies=saved};
  }
  public void RestoreRunState(SpawnerRunSnapshot saved){
   ClearEnemies();if(bosses!=null)bosses.ResetRun();introductions.Clear();
   if(saved==null){budgetWave=plannedCount=spawnedCount=resolvedCount=0;announcedWave=0;spawnClock=0;octopusIntroduced=orcaladonIntroduced=false;return;}
   if(IsBossWave){
    // Legacy snapshots cannot resurrect an ordinary crowd on a new boss wave. Actual attempts are never saved.
    Bosses.PrepareWave(Waves.Number);budgetWave=Waves.Number;plannedCount=1;spawnedCount=resolvedCount=Waves.Resting?1:0;announcedWave=0;return;
   }
   announcedWave=Mathf.Clamp(saved.AnnouncedWave,0,Waves.Number);spawnClock=Finite(saved.SpawnClock)?Mathf.Clamp(saved.SpawnClock,0,600):0;
   octopusIntroduced=saved.OctopusIntroduced;orcaladonIntroduced=saved.OrcaladonIntroduced;
   // Restore all saved live enemies even if the current wave envelope has a lower spawn cap.
   // A restored enemy is grounded and cannot execute an old in-flight attack.
   if(saved.Enemies!=null)for(int i=0;i<saved.Enemies.Length&&Active.Count<100;i++){
    var state=saved.Enemies[i];if(state==null)continue;var kind=(EnemyKind)state.Kind;
    if(kind!=EnemyKind.Normal&&kind!=EnemyKind.BlueberrinniOctopusini&&kind!=EnemyKind.Orcaladon)continue;
    if(!Finite(state.Position.x)||!Finite(state.Position.z)||Mathf.Abs(state.Position.x)>100||Mathf.Abs(state.Position.z)>100||!Finite(state.Health)||state.Health<=0||!Finite(state.MaxHealth)||state.MaxHealth<=0)continue;
    var position=state.Position;position.y=0;var enemy=Pool(kind).Get();enemy.Initialize(this,kind,1,position);enemy.RestoreRunState(state);Active.Add(enemy);
    if(kind==EnemyKind.BlueberrinniOctopusini)octopusIntroduced=true;if(kind==EnemyKind.Orcaladon)orcaladonIntroduced=true;
   }
   budgetWave=Waves.Number;
   if(saved.BudgetWave==budgetWave&&saved.PlannedCount>0){
    plannedCount=Mathf.Max(Active.Count,Mathf.Clamp(saved.PlannedCount,1,10000));spawnedCount=Mathf.Clamp(saved.SpawnedCount,Active.Count,plannedCount);
    plannedCount=Mathf.Max(plannedCount,spawnedCount);resolvedCount=Mathf.Clamp(spawnedCount-Active.Count,0,spawnedCount);
   }else{
    // Additive v1 migration: old saves know the phase and live foes, but not historic arrivals.
    plannedCount=Waves.Resting?Mathf.Max(1,Active.Count):Mathf.Max(Active.Count,PlannedPopulation(GameBalance.Current.Waves,budgetWave));
    spawnedCount=Waves.Resting?plannedCount:Mathf.Clamp(Mathf.FloorToInt(plannedCount*Waves.Progress),Active.Count,plannedCount);
    resolvedCount=spawnedCount-Active.Count;
   }
   int octopusWave=Mathf.Max(1,GameBalance.Current.Octopus.FirstWave),orcaWave=Mathf.Max(1,GameBalance.Current.Orcaladon.FirstWave);
   if(orcaWave<octopusWave){QueueOrcaladon();QueueOctopus();}else{QueueOctopus();QueueOrcaladon();}
  }
  static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
  void ClearEnemies(){
   for(int i=Active.Count-1;i>=0;i--)if(Active[i]!=null)Pool(Active[i].Kind).Release(Active[i]);Active.Clear();
   for(int i=Corpses.Count-1;i>=0;i--)if(Corpses[i]!=null)Pool(Corpses[i].Kind).Release(Corpses[i]);Corpses.Clear();
   meleeWarningCount=0;
  }
  public void ResetRun(){
   ClearEnemies();if(bosses!=null)bosses.ResetRun();Waves.Reset();budgetWave=plannedCount=spawnedCount=resolvedCount=0;announcedWave=0;spawnClock=0;introductions.Clear();octopusIntroduced=orcaladonIntroduced=false;EnsureWaveBudget();
  }
 }
}
