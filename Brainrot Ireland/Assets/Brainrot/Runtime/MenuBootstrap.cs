using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace Brainrot {
 public sealed class MenuBootstrap:MonoBehaviour {
  Font font;Transform canvas;GameObject panel;Button continueButton,newButton;
  void Awake(){
   SaveSystem.Load();Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   var camera=new GameObject("Камера меню").AddComponent<Camera>();camera.transform.SetParent(transform);camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.35f,.56f);
   var root=new GameObject("Главное меню",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);var c=root.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=100;canvas=root.transform;
   var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
   font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
   var image=Resources.Load<Texture2D>("MenuBackground");if(image!=null){var bg=new GameObject("Фон",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));bg.transform.SetParent(canvas,false);bg.GetComponent<RawImage>().texture=image;bg.GetComponent<RawImage>().raycastTarget=false;var fitter=bg.GetComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fitter.aspectRatio=(float)image.width/image.height;}
   panel=Box(canvas,new Vector2(.62f,.06f),new Vector2(.96f,.94f),new Color(.015f,.055f,.1f,.85f));
   var events=new GameObject("Управление меню",typeof(EventSystem));events.transform.SetParent(transform,false);
#if ENABLE_INPUT_SYSTEM
   events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
   events.AddComponent<StandaloneInputModule>();
#endif
   Main();
  }
  void Start(){UIInputRouter.EnsureForScene(gameObject.scene);}
  void Update(){if(continueButton!=null)continueButton.interactable=!SceneFlow.Instance.Busy&&GameManager.Instance!=null&&GameManager.Instance.HasRun;if(newButton!=null)newButton.interactable=!SceneFlow.Instance.Busy;}
  void Clear(){foreach(Transform child in panel.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}continueButton=newButton=null;}
  void Main(){Clear();Text(panel.transform,"БРЕЙНРОТ\nИРЛАНДИЯ",42,.75f,.97f);continueButton=Button("ПРОДОЛЖИТЬ ИГРУ",.60f,()=>SceneFlow.Instance.Play(false));newButton=Button("НОВАЯ ИГРА",.47f,()=>SceneFlow.Instance.Play(true));Button("НАСТРОЙКИ",.34f,Settings);Button("РЕКОРДЫ",.21f,Records);Button("ВЫХОД",.08f,()=>Application.Quit());}
  void Records(){Clear();Text(panel.transform,"РЕКОРДЫ",32,.79f,.95f);var d=SaveSystem.Data;Text(panel.transform,$"Лучшее время\n{Mathf.FloorToInt(d.BestTime/60):00}:{Mathf.FloorToInt(d.BestTime%60):00}\n\nПобеждено врагов\n{d.BestKills}",25,.28f,.77f);Button("НАЗАД",.08f,Main);}
  void Settings(){
   Clear();Text(panel.transform,"НАСТРОЙКИ",32,.79f,.95f);
   Button("ГРОМКОСТЬ: "+Mathf.RoundToInt(SaveSystem.Data.Volume*100)+"%",.62f,()=>{SaveSystem.Data.Volume=SaveSystem.Data.Volume>=.99f?0:Mathf.Min(1,SaveSystem.Data.Volume+.1f);AudioListener.volume=SaveSystem.Data.Volume;SaveSystem.Save();Settings();});
   Button("ЧУВСТВИТЕЛЬНОСТЬ: "+SaveSystem.Data.Sensitivity.ToString("0.#"),.49f,()=>{SaveSystem.Data.Sensitivity=SaveSystem.Data.Sensitivity>=3?.5f:Mathf.Min(3,SaveSystem.Data.Sensitivity+.5f);SaveSystem.Save();Settings();});
   Button("ФИКСАЦИЯ КАМЕРЫ: "+(SaveSystem.Data.CameraLocked?"ВКЛ":"ВЫКЛ"),.36f,()=>{if(GameManager.Instance!=null&&GameManager.Instance.Player!=null)GameManager.Instance.Player.SetCameraLock(!GameManager.Instance.Player.CameraLocked);else{SaveSystem.Data.CameraLocked=!SaveSystem.Data.CameraLocked;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();}Settings();});
   Button("КАЧЕСТВО: "+(SaveSystem.Data.Quality+1),.23f,()=>{int levels=Mathf.Clamp(QualitySettings.names.Length,1,3);SaveSystem.Data.Quality=(SaveSystem.Data.Quality+1)%levels;QualitySettings.SetQualityLevel(SaveSystem.Data.Quality);SaveSystem.Save();Settings();});
   Button("НАЗАД",.08f,Main);
  }
  Button Button(string title,float y,UnityEngine.Events.UnityAction action){var go=Box(panel.transform,new Vector2(.07f,y),new Vector2(.93f,y+.10f),new Color(.12f,.75f,.77f));var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();button.onClick.AddListener(action);Text(go.transform,title,20,0,1).color=new Color(.02f,.08f,.12f);return button;}
  Text Text(Transform parent,string value,int size,float bottom,float top){var go=new GameObject("Надпись",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Rect(go,new Vector2(.03f,bottom),new Vector2(.97f,top));var label=go.GetComponent<Text>();label.font=font;label.text=value;label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(1,.89f,.56f);label.raycastTarget=false;return label;}
  static GameObject Box(Transform parent,Vector2 min,Vector2 max,Color color){var go=new GameObject("Панель",typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Rect(go,min,max);go.GetComponent<Image>().color=color;return go;}
  static void Rect(GameObject go,Vector2 min,Vector2 max){var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
 }
}
