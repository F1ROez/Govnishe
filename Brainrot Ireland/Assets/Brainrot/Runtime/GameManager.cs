using UnityEngine;
namespace Brainrot {
 public enum GameState { Menu, Playing, Paused, Upgrade, Dead, Map }
 public sealed class GameManager : MonoBehaviour {
  public static GameManager Instance { get; private set; }
  public GameState State {get;private set;}=GameState.Menu;
  bool initialized;
  public bool HasRun{get;private set;}
  public bool IsPlaying => State==GameState.Playing;
  [SerializeField] public GameBalance Balance;
  [SerializeField] public PlayerCharacterDefinition PlayerCharacter;
  public PlayerController Player; public EnemySpawner Spawner; public UpgradeSystem Upgrades;
  public float Elapsed {get;private set;} public int Kills {get;private set;} public int Wave=>Spawner!=null?Spawner.Waves.Number:1; public IslandZone CurrentZone=>Player!=null?IslandMap.GetZone(Player.transform.position):IslandZone.Beach;
  public string Notification {get;private set;}=""; float noticeUntil; UIManager ui;
  void Awake(){ if(Instance!=null&&Instance!=this){Destroy(gameObject);return;} Instance=this;if(!SceneFlow.LoadingGame)InitializeWorld();}
  public void InitializeWorld(){if(initialized)return;initialized=true;Balance=Balance!=null?Balance:GameBalance.Current;Balance.EnsureExploration(); Time.timeScale=1; SaveSystem.Load();AudioListener.volume=SaveSystem.Data.Volume; Application.targetFrameRate=60;
   QualitySettings.SetQualityLevel(Mathf.Clamp(SaveSystem.Data.Quality,0,QualitySettings.names.Length-1));
   WorldBuilder.Build(); var go=new GameObject("Игрок");go.SetActive(false);Player=go.AddComponent<PlayerController>();Player.Character=PlayerCharacter!=null?PlayerCharacter:PlayerCharacterDefinition.Default;go.SetActive(true);
   Spawner=gameObject.AddComponent<EnemySpawner>(); Upgrades=gameObject.AddComponent<UpgradeSystem>();
   ui=gameObject.AddComponent<UIManager>(); ui.Initialize(); SetState(GameState.Menu);
  }
  void Start(){if(initialized)UIInputRouter.EnsureForScene(gameObject.scene);}
  void Update(){ if(!initialized)return;if(GameInput.Pause){if(State==GameState.Map)CloseMap();else if(IsPlaying)Pause();else if(State==GameState.Paused)Resume();}
   if(IsPlaying){Elapsed+=Time.deltaTime;Spawner.Tick(Time.deltaTime);}
   if(Time.unscaledTime>noticeUntil) Notification=""; ui.Refresh();
  }
  void SetState(GameState value){ if(value!=GameState.Playing&&Player!=null)Player.CancelAttacks();State=value;if(value==GameState.Paused||value==GameState.Dead)UIInputRouter.EnsureForScene(gameObject.scene);Time.timeScale=IsPlaying?1:0; bool lockMouse=IsPlaying&&!Application.isMobilePlatform;Cursor.lockState=lockMouse?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!lockMouse; MobileInput.Move=Vector2.zero;MobileInput.Look=Vector2.zero;MobileInput.Attack=false;MobileInput.Sprint=false;MobileInput.Dash=false; }
  public void StartRun(){HasRun=true;Spawner.ResetRun();Elapsed=0;Kills=0;Notification="";Player.ResetRun();Upgrades.ResetRun();SetState(GameState.Playing);Notify("Акулы выходят из океана. Продержитесь как можно дольше.",5);}
  public void OpenMap(){if(IsPlaying)SetState(GameState.Map);}
  public void CloseMap(){if(State==GameState.Map)SetState(GameState.Playing);}
  public void Pause(){if(IsPlaying)SetState(GameState.Paused);} public void Resume(){if(State==GameState.Paused)SetState(GameState.Playing);}
  public void ShowUpgrades(){if(State!=GameState.Dead)SetState(GameState.Upgrade);} public void ResumeAfterUpgrade(){if(State==GameState.Upgrade)SetState(GameState.Playing);}
  public void MainMenu(){SceneFlow.Instance.ShowMenu();}
  public void SuspendRun(){SetState(GameState.Menu);}
  public void ResumeRun(){if(HasRun)SetState(GameState.Playing);}
  public void Reward(int xp,int kills){if(!IsPlaying)return;Kills+=kills;}
  public void Die(){if(!IsPlaying)return;HasRun=false;SaveSystem.Record(Elapsed,Kills,Upgrades.Level);SetState(GameState.Dead);}
  public void Notify(string message,float seconds=3){Notification=message;noticeUntil=Time.unscaledTime+seconds;}
  public void ReportHit(bool critical,int count=1){ui.ShowHit(critical,count);}
  public void ReportMiss(){ui.ShowMiss();}
  public void Quit(){SaveSystem.Save();Application.Quit();}
  void OnApplicationPause(bool paused){if(paused&&IsPlaying)Pause();} void OnDestroy(){Time.timeScale=1;if(Instance==this)Instance=null;}
 }
}