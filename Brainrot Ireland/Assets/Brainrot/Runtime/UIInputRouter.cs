using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
namespace Brainrot {
 /// <summary>Scene-owned UI input must survive unloading the other scene and Time.timeScale == 0.</summary>
 public static class UIInputRouter {
  public static EventSystem EnsureForScene(Scene scene){
   if(!scene.IsValid()||!scene.isLoaded)return null;
   EventSystem target=null;
   foreach(var root in scene.GetRootGameObjects()){
    var candidates=root.GetComponentsInChildren<EventSystem>(true);
    foreach(var candidate in candidates)if(target==null&&candidate.gameObject.activeInHierarchy)target=candidate;
   }
   if(target==null){var go=new GameObject("Управление интерфейсом",typeof(EventSystem));SceneManager.MoveGameObjectToScene(go,scene);target=go.GetComponent<EventSystem>();}
   // Only one scene can own interactive screen UI at a time.
   foreach(var system in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(system!=target){
    foreach(var module in system.GetComponents<BaseInputModule>())module.enabled=false;
    system.enabled=false;
   }
#if ENABLE_INPUT_SYSTEM
   // Gameplay reads input in Update, and pause menus must still receive events at zero time scale.
   InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
   foreach(var legacy in target.GetComponents<StandaloneInputModule>())legacy.enabled=false;
   var input=target.GetComponent<InputSystemUIInputModule>();if(input==null)input=target.gameObject.AddComponent<InputSystemUIInputModule>();
   if(input.actionsAsset==null||input.point==null||input.point.action==null||input.leftClick==null||input.leftClick.action==null)input.AssignDefaultActions();
#else
   var input=target.GetComponent<StandaloneInputModule>();if(input==null)input=target.gameObject.AddComponent<StandaloneInputModule>();
#endif
   target.enabled=true;input.enabled=false;input.enabled=true;EventSystem.current=target;target.UpdateModules();input.ActivateModule();
   return target;
  }
 }
}
