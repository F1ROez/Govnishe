using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace Brainrot {
 public sealed class SceneFlow:MonoBehaviour {
  public const string MenuPath="Assets/Scenes/MainMenu.unity",GamePath="Assets/Scenes/BrainrotIreland.unity";
  public static bool LoadingGame{get;private set;}
  static SceneFlow instance;readonly List<Behaviour> hidden=new List<Behaviour>();
  public bool Busy{get;private set;}
  public static SceneFlow Instance{get{if(instance==null){var go=new GameObject("Переходы сцен");instance=go.AddComponent<SceneFlow>();DontDestroyOnLoad(go);}return instance;}}
  public void ShowMenu(){if(!Busy)StartCoroutine(ReturnToMenu());}
  public void Play(bool fresh){if(!Busy)StartCoroutine(EnterGame(fresh));}
  IEnumerator ReturnToMenu(){
   if(!Application.CanStreamedLevelBeLoaded(MenuPath)){Debug.LogError("Сцена меню не найдена. Выполните Tools → TungTung → Создать или обновить игру.");yield break;}
   Busy=true;var game=GameManager.Instance;if(game!=null)game.SuspendRun();hidden.Clear();
   var scene=SceneManager.GetSceneByPath(GamePath);if(scene.IsValid()&&scene.isLoaded)foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Behaviour>(true))if(component.enabled&&(component is Camera||component is Canvas||component is AudioListener||component is EventSystem||component is BaseInputModule)){component.enabled=false;hidden.Add(component);}
   var menu=SceneManager.GetSceneByPath(MenuPath);if(!menu.isLoaded)yield return SceneManager.LoadSceneAsync(MenuPath,LoadSceneMode.Additive);
   menu=SceneManager.GetSceneByPath(MenuPath);if(menu.IsValid())SceneManager.SetActiveScene(menu);UIInputRouter.EnsureForScene(menu);Busy=false;
  }
  IEnumerator EnterGame(bool fresh){
   if(!Application.CanStreamedLevelBeLoaded(GamePath)){Debug.LogError("Игровая сцена не найдена. Выполните Tools → TungTung → Создать или обновить игру.");yield break;}
   Busy=true;var scene=SceneManager.GetSceneByPath(GamePath);if(!scene.isLoaded){LoadingGame=true;yield return SceneManager.LoadSceneAsync(GamePath,LoadSceneMode.Additive);LoadingGame=false;}
   scene=SceneManager.GetSceneByPath(GamePath);var menu=SceneManager.GetSceneByPath(MenuPath);
   if(menu.IsValid()&&menu.isLoaded)foreach(var root in menu.GetRootGameObjects()){foreach(var events in root.GetComponentsInChildren<EventSystem>())events.enabled=false;foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;}
   SceneManager.SetActiveScene(scene);GameManager.Instance.InitializeWorld();foreach(var b in hidden)if(b!=null)b.enabled=true;hidden.Clear();
   UIInputRouter.EnsureForScene(scene);
   if(fresh||!GameManager.Instance.HasRun)GameManager.Instance.StartRun();else GameManager.Instance.ResumeRun();
   if(menu.IsValid()&&menu.isLoaded)yield return SceneManager.UnloadSceneAsync(menu);UIInputRouter.EnsureForScene(scene);Busy=false;
  }
  void OnDestroy(){if(instance==this){instance=null;LoadingGame=false;}}
 }
}
