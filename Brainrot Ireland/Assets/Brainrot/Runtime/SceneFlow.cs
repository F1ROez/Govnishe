using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
namespace Brainrot {
 public sealed class SceneFlow:MonoBehaviour {
  public const string MenuPath="Assets/Scenes/MainMenu.unity",GamePath="Assets/Scenes/BrainrotIreland.unity";
  public static bool LoadingGame{get;private set;}
  static SceneFlow instance;readonly List<Behaviour> hidden=new List<Behaviour>();
  public bool Busy{get;private set;}
  public static SceneFlow Instance{get{if(instance==null){var go=new GameObject("Переходы сцен");instance=go.AddComponent<SceneFlow>();DontDestroyOnLoad(go);}return instance;}}
  public void ShowMenu(){if(!Busy)StartCoroutine(ReturnToMenu());}
  public void Play(bool fresh){if(!Busy)StartCoroutine(EnterGame(fresh));}
  void HideGame(Scene scene){
   if(!scene.IsValid()||!scene.isLoaded)return;
   foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Behaviour>(true))
    if(component.enabled&&(component is Camera||component is Canvas||component is AudioListener||component is EventSystem||component is BaseInputModule)){
     component.enabled=false;if(!hidden.Contains(component))hidden.Add(component);
    }
  }
  IEnumerator ReturnToMenu(){
   if(!Application.CanStreamedLevelBeLoaded(MenuPath)){Debug.LogError("Сцена меню не найдена. Выполните Tools → TungTung → Создать или обновить игру.");yield break;}
   Busy=true;
   try{
    var game=GameManager.Instance;if(game!=null)game.SuspendRun();HideGame(SceneManager.GetSceneByPath(GamePath));
    var menu=SceneManager.GetSceneByPath(MenuPath);if(!menu.isLoaded)yield return SceneManager.LoadSceneAsync(MenuPath,LoadSceneMode.Additive);
    menu=SceneManager.GetSceneByPath(MenuPath);if(menu.IsValid())SceneManager.SetActiveScene(menu);UIInputRouter.EnsureForScene(menu);
   }finally{Busy=false;}
  }
  IEnumerator EnterGame(bool fresh){
   if(!Application.CanStreamedLevelBeLoaded(GamePath)){Debug.LogError("Игровая сцена не найдена. Выполните Tools → TungTung → Создать или обновить игру.");yield break;}
   if(!fresh&&(GameManager.Instance==null||!GameManager.Instance.HasRun)&&!RunPersistence.HasSavedRun)yield break;
   Busy=true;
   try{
    var scene=SceneManager.GetSceneByPath(GamePath);
    if(!scene.isLoaded){LoadingGame=true;yield return SceneManager.LoadSceneAsync(GamePath,LoadSceneMode.Additive);LoadingGame=false;}
    scene=SceneManager.GetSceneByPath(GamePath);var menu=SceneManager.GetSceneByPath(MenuPath);
    if(!scene.IsValid()||GameManager.Instance==null){Debug.LogError("Не удалось открыть игровую сцену.");yield break;}
    SceneManager.SetActiveScene(scene);var game=GameManager.Instance;game.InitializeWorld();
    foreach(var b in hidden)if(b!=null)b.enabled=true;hidden.Clear();
    UIInputRouter.EnsureForScene(scene);
    if(fresh)game.StartRun();
    else if(!game.ResumeRun()){
     // A failed Continue never silently starts and overwrites a new run.
     game.Notify("Не удалось восстановить забег. Сохранение оставлено для проверки.",5);
     game.SuspendRun();HideGame(scene);
     if(menu.IsValid()&&menu.isLoaded){SceneManager.SetActiveScene(menu);UIInputRouter.EnsureForScene(menu);}
     yield break;
    }
    // Keep the old listener alive until the destination has created its own camera.
    if(menu.IsValid()&&menu.isLoaded){
     foreach(var root in menu.GetRootGameObjects()){
      foreach(var events in root.GetComponentsInChildren<EventSystem>())events.enabled=false;
      foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;
      foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.enabled=false;
     }
     yield return SceneManager.UnloadSceneAsync(menu);
    }
    UIInputRouter.EnsureForScene(scene);
   }finally{LoadingGame=false;Busy=false;}
  }
  void OnDestroy(){if(instance==this){instance=null;LoadingGame=false;}}
 }
}
