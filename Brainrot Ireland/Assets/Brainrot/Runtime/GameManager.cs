using UnityEngine;
namespace Brainrot {
 public enum GameState { Menu, Playing, Paused, Upgrade, Dead, Map }
 public sealed class GameManager : MonoBehaviour {
  public static GameManager Instance { get; private set; }
  public GameState State {get;private set;}=GameState.Menu;
  bool initialized,focused=true,backgrounded,quitting;
  float nextCheckpoint;
  const float AutosaveSeconds=15;
  public bool HasRun{get;private set;}
  public bool CanContinue=>HasRun||RunPersistence.HasSavedRun;
  public bool IsPlaying => State==GameState.Playing;
  [SerializeField] public GameBalance Balance;
  [SerializeField] public PlayerCharacterDefinition PlayerCharacter;
  public PlayerController Player; public EnemySpawner Spawner; public UpgradeSystem Upgrades;
  public string CoinRunId{get;private set;}="";
  public int RunCoins=>MetaProgression.RunEarned(CoinRunId);
  public CoinDrops Coins{get;private set;}
  public HeartDrops Hearts{get;private set;}
  public RunGoals Goals{get;private set;}
  public float Elapsed {get;private set;} public int Kills {get;private set;} public int Wave=>Spawner!=null?Spawner.Waves.Number:1; public IslandZone CurrentZone=>Player!=null?IslandMap.GetZone(Player.transform.position):IslandZone.Beach;
  public string Notification {get;private set;}=""; float noticeUntil; UIManager ui;
  void Awake(){ if(Instance!=null&&Instance!=this){Destroy(gameObject);return;} Instance=this;if(!SceneFlow.LoadingGame)InitializeWorld();}
  public void InitializeWorld(){if(initialized)return;initialized=true;Balance=Balance!=null?Balance:GameBalance.Current;Balance.EnsureExploration(); Time.timeScale=1; SaveSystem.Load();GameMusic.ApplyVolume(); Application.targetFrameRate=60;
   GameQuality.Apply(SaveSystem.Data.Quality);
   WorldBuilder.Build();GameAudio.Prewarm(); var go=new GameObject("Игрок");go.SetActive(false);Player=go.AddComponent<PlayerController>();Player.Character=PlayerCharacter!=null?PlayerCharacter:PlayerCharacterDefinition.Default;go.SetActive(true);
   Spawner=gameObject.AddComponent<EnemySpawner>(); Upgrades=gameObject.AddComponent<UpgradeSystem>();
   Hearts=gameObject.AddComponent<HeartDrops>();Coins=gameObject.AddComponent<CoinDrops>();Coins.Initialize(this);
   Goals=gameObject.AddComponent<RunGoals>();Goals.Initialize(this);
   ui=gameObject.AddComponent<UIManager>(); ui.Initialize(); SetState(GameState.Menu);
  }
  void Start(){if(initialized)UIInputRouter.EnsureForScene(gameObject.scene);}
  void Update(){
   if(!initialized)return;
   if(GameInput.Pause){if(State==GameState.Map)CloseMap();else if(IsPlaying)Pause();else if(State==GameState.Paused)Resume();}
   if(IsPlaying){
    Elapsed+=Time.deltaTime;Spawner.Tick(Time.deltaTime);Hearts.Tick(Time.deltaTime,Player);Coins.Tick(Time.deltaTime,Player);Goals.Tick(Time.deltaTime);
    if(Time.unscaledTime>=nextCheckpoint){SaveCheckpoint();nextCheckpoint=Time.unscaledTime+AutosaveSeconds;}
   }
   if(Time.unscaledTime>noticeUntil)Notification="";ui.Refresh();
  }
  void SetState(GameState value){
   if(value==GameState.Playing&&(!focused||backgrounded))value=GameState.Paused;
   if(value!=GameState.Playing){if(Player!=null)Player.CancelAttacks();GameAudio.StopGameplayEffects();}State=value;
   if(value==GameState.Playing)GameMusic.SetGameplay();else if(value==GameState.Menu)GameMusic.SetMenu();else GameMusic.SetPaused(true);
   if(!focused||backgrounded)GameMusic.SetPaused(true);
   if(value==GameState.Paused||value==GameState.Dead)UIInputRouter.EnsureForScene(gameObject.scene);
   Time.timeScale=IsPlaying?1:0;bool lockMouse=IsPlaying&&!Application.isMobilePlatform;Cursor.lockState=lockMouse?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!lockMouse;MobileInput.Clear();
  }
  public void StartRun(){
   string id=System.Guid.NewGuid().ToString("N");if(!MetaProgression.BeginRun(id)){Notify("Не удалось сохранить кошелёк. Попробуй ещё раз.",4);return;}CoinRunId=id;Coins.Clear();
   GameAudio.StopGameplayEffects();HitEffects.ClearAll();Hearts.Clear();RunPersistence.Clear();HasRun=true;Spawner.ResetRun();Elapsed=0;Kills=0;Notification="";Player.ResetRun();Upgrades.ResetRun();Goals.ResetRun();
   ui?.ResetRunPresentation();GameMusic.RestartGameplay();SetState(GameState.Playing);Notify("Акулы выходят из океана. Продержитесь как можно дольше.",5);SaveCheckpoint();nextCheckpoint=Time.unscaledTime+AutosaveSeconds;
  }
  public bool SaveCheckpoint(){
   if(!initialized||!HasRun||Player==null||Player.Health<=0||Spawner==null)return false;
   var snapshot=new RunSnapshot {Elapsed=Elapsed,Kills=Kills,ChallengeMaskEarnedThisRun=Goals!=null?Goals.EarnedThisRunMask:0,Player=Player.CaptureRunState(),Wave=Spawner.Waves.CaptureRunState(),Spawner=Spawner.CaptureRunState(),Hearts=Hearts.Capture(),CoinRunId=CoinRunId,Coins=Coins.Capture()};
   bool saved=RunPersistence.Save(snapshot);
   if(saved){SaveSystem.Data.BestTime=Mathf.Max(SaveSystem.Data.BestTime,Elapsed);SaveSystem.Data.BestKills=Mathf.Max(SaveSystem.Data.BestKills,Kills);SaveSystem.Data.BestWave=Mathf.Max(SaveSystem.Data.BestWave,Wave);SaveSystem.Save();}
   if(!saved)Notify("НЕ УДАЛОСЬ СОХРАНИТЬ ЗАБЕГ. ПРОВЕРЬ СВОБОДНОЕ МЕСТО НА ДИСКЕ.",5);
   return saved;
  }
  public bool RestoreSavedRun(){
   if(!RunPersistence.TryLoad(out var snapshot)||!RunSaveStore.IsValid(snapshot))return false;
   string runId=snapshot.CoinRunId;
   if(string.IsNullOrEmpty(runId)){runId=System.Guid.NewGuid().ToString("N");if(!MetaProgression.BeginRun(runId))return false;}
   else if(SaveSystem.Data.CoinRunId!=runId){Notify("Сохранение забега не соответствует кошельку.",4);return false;}
   CoinRunId=runId;Coins.Clear();
   // Transient hit/grab/tumble states are intentionally normalized; no invisible attack survives loading.
   GameAudio.StopGameplayEffects();HitEffects.ClearAll();Hearts.Clear();Player.CancelAttacks();MobileInput.Clear();Spawner.ResetRun();Upgrades.ResetRun();
   Player.RestoreRunState(snapshot.Player);Spawner.Waves.RestoreRunState(snapshot.Wave);Spawner.RestoreRunState(snapshot.Spawner);Hearts.Restore(snapshot.Hearts);Coins.Restore(snapshot.Coins);
   Elapsed=snapshot.Elapsed;Kills=snapshot.Kills;HasRun=true;Goals.ResetRun(true);Goals.RestoreRunProgress(snapshot.ChallengeMaskEarnedThisRun);Notification="";nextCheckpoint=Time.unscaledTime+AutosaveSeconds;
   ui?.ResetRunPresentation();GameMusic.RestartGameplay();SetState(GameState.Playing);Notify("ЗАБЕГ ВОССТАНОВЛЕН — ДЕРЖИ БИТУ КРЕПЧЕ!",3);return true;
  }
  public void OpenMap(){if(IsPlaying)SetState(GameState.Map);}
  public void CloseMap(){if(State==GameState.Map)SetState(GameState.Playing);}
  public void Pause(){if(IsPlaying){SetState(GameState.Paused);SaveCheckpoint();}}
  public void Resume(){if(State==GameState.Paused&&focused&&!backgrounded)SetState(GameState.Playing);}
  public void ShowUpgrades(){if(State!=GameState.Dead)SetState(GameState.Upgrade);}
  public void ResumeAfterUpgrade(){if(State==GameState.Upgrade)SetState(GameState.Playing);}
  public void MainMenu(){SceneFlow.Instance.ShowMenu();}
  public void SuspendRun(){SetState(GameState.Menu);SaveCheckpoint();}
  public bool ResumeRun(){if(HasRun){SetState(GameState.Playing);return true;}return RestoreSavedRun();}
  public bool TryCollectCoins(int[] ids,int[] amounts){
   if(!IsPlaying||!MetaProgression.AwardBatch(CoinRunId,ids,amounts))return false;GameAudio.PlayCoin();return true;
  }
  public bool TryCollectCoin(int id,int amount){
   if(!IsPlaying||!MetaProgression.Award(CoinRunId,id,amount))return false;
   GameAudio.PlayCoin();return true;
  }
  public void Reward(int xp,int kills){if(!IsPlaying)return;Kills+=kills;}
  public void Die(){if(!IsPlaying)return;HasRun=false;RunPersistence.Clear();SaveSystem.Record(Elapsed,Kills,Upgrades.Level,Wave);SetState(GameState.Dead);}
  public void Notify(string message,float seconds=3){Notification=message;noticeUntil=Time.unscaledTime+seconds;}
  public void ReportHit(bool critical,int count=1){ui.ShowHit(critical,count);}
  public void ReportMiss(){ui.ShowMiss();}
  public void Quit(){SaveCheckpoint();SaveSystem.Save();Application.Quit();}
  void OnApplicationFocus(bool hasFocus){focused=hasFocus;HandleVisibility();}
  void OnApplicationPause(bool paused){backgrounded=paused;HandleVisibility();}
  void HandleVisibility(){
   bool hidden=!focused||backgrounded;AudioListener.pause=hidden;
   if(hidden){if(IsPlaying)Pause();else SaveCheckpoint();Player?.CancelAttacks();MobileInput.Clear();GameMusic.SetPaused(true);}
   else if(initialized){if(State==GameState.Menu)GameMusic.SetMenu();else if(IsPlaying)GameMusic.SetGameplay();}
  }
  void OnApplicationQuit(){quitting=true;SaveCheckpoint();SaveSystem.Save();}
  void OnDestroy(){
   if(Instance!=this)return;
   if(!quitting)SaveCheckpoint();AudioListener.pause=false;Time.timeScale=1;Instance=null;
  }
 }
}
