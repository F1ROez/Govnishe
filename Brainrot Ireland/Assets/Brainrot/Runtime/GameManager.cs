using UnityEngine;
namespace Brainrot {
 public enum GameState { Menu, Playing, Paused, Upgrade, Dead }
 public sealed class GameManager : MonoBehaviour {
  public static GameManager Instance { get; private set; }
  public GameState State {get;private set;}=GameState.Menu;
  public bool IsPlaying => State==GameState.Playing;
  [SerializeField] public GameBalance Balance;
  public PlayerController Player; public EnemySpawner Spawner; public UpgradeSystem Upgrades;
  public float Elapsed {get;private set;} public int Kills {get;private set;} public int Wave=>1+Mathf.FloorToInt(Elapsed/30);
  public string Notification {get;private set;}=""; float noticeUntil; UIManager ui; BossSystem bosses;
  void Awake(){ if(Instance!=null&&Instance!=this){Destroy(gameObject);return;} Instance=this; Balance=Balance!=null?Balance:GameBalance.Current; Time.timeScale=1; SaveSystem.Load();AudioListener.volume=SaveSystem.Data.Volume; Application.targetFrameRate=60;
   QualitySettings.SetQualityLevel(Mathf.Clamp(SaveSystem.Data.Quality,0,QualitySettings.names.Length-1));
   WorldBuilder.Build(); var go=new GameObject("Tung Tung Sahur"); Player=go.AddComponent<PlayerController>();
   Spawner=gameObject.AddComponent<EnemySpawner>(); Upgrades=gameObject.AddComponent<UpgradeSystem>(); bosses=gameObject.AddComponent<BossSystem>();
   ui=gameObject.AddComponent<UIManager>(); ui.Initialize(); SetState(GameState.Menu);
  }
  void Update(){ if(GameInput.Pause){if(IsPlaying)Pause();else if(State==GameState.Paused)Resume();}
   if(IsPlaying){Elapsed+=Time.deltaTime;Spawner.Tick(Time.deltaTime);bosses.Tick(Time.deltaTime);}
   if(Time.unscaledTime>noticeUntil) Notification=""; ui.Refresh();
  }
  void SetState(GameState value){ State=value;Time.timeScale=IsPlaying?1:0; bool lockMouse=IsPlaying&&!Application.isMobilePlatform;Cursor.lockState=lockMouse?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!lockMouse; MobileInput.Move=Vector2.zero;MobileInput.Look=Vector2.zero;MobileInput.Attack=false;MobileInput.Sprint=false;MobileInput.Dash=false; }
  public void StartRun(){Spawner.ResetRun();Elapsed=0;Kills=0;Notification="";Player.ResetRun();Upgrades.ResetRun();bosses.ResetRun();SetState(GameState.Playing);Notify("THE OCEAN IS GETTING LOUDER",4);}
  public void Pause(){if(IsPlaying)SetState(GameState.Paused);} public void Resume(){if(State==GameState.Paused)SetState(GameState.Playing);}
  public void ShowUpgrades(){if(State!=GameState.Dead)SetState(GameState.Upgrade);} public void ResumeAfterUpgrade(){if(State==GameState.Upgrade)SetState(GameState.Playing);}
  public void MainMenu(){Spawner.ResetRun();SetState(GameState.Menu);}
  public void Reward(int xp,int kills){if(!IsPlaying)return;Kills+=kills;Upgrades.AddExperience(xp);}
  public void Die(){if(!IsPlaying)return;SaveSystem.Record(Elapsed,Kills,Upgrades.Level);SetState(GameState.Dead);}
  public void Notify(string message,float seconds=3){Notification=message;noticeUntil=Time.unscaledTime+seconds;}
  public void ReportHit(bool critical,int count=1){ui.ShowHit(critical,count);}
  public void ReportMiss(){ui.ShowMiss();}
  public void Quit(){SaveSystem.Save();Application.Quit();}
  void OnApplicationPause(bool paused){if(paused&&IsPlaying)Pause();} void OnDestroy(){Time.timeScale=1;if(Instance==this)Instance=null;}
 }
}