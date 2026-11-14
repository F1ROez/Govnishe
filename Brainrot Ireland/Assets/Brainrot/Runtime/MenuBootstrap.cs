using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace Brainrot {
 public sealed class MenuBootstrap:MonoBehaviour {
  Texture2D backdropTexture;Font font;Transform canvas;GameObject panel;Button continueButton,newButton;
  void Awake(){
   SaveSystem.Load();GameMusic.ApplyVolume();GameMusic.SetMenu();Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   var camera=new GameObject("Камера меню").AddComponent<Camera>();camera.transform.SetParent(transform);camera.gameObject.AddComponent<AudioListener>();camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.24f,.64f,.73f);
   var root=new GameObject("Главное меню",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);var c=root.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=100;canvas=root.transform;
   var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
   font=CartoonUI.Font;
   var image=Resources.Load<Texture2D>("MenuBackground")??Resources.Load<Texture2D>("TropicalMenuBackdrop");if(image!=null){var bg=new GameObject("Фон",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));bg.transform.SetParent(canvas,false);bg.GetComponent<RawImage>().texture=image;bg.GetComponent<RawImage>().raycastTarget=false;var fitter=bg.GetComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fitter.aspectRatio=(float)image.width/image.height;}
   if(image==null)BuildBackdrop();
   var logo=TropicalUIArt.Place("Логотип на дереве",canvas,new Vector2(.025f,.78f),new Vector2(.375f,.985f),"LogoPlaque").gameObject;
   var topTitle=Text(logo.transform,"БРЕЙНРОТ",37,.48f,.92f);topTitle.color=Color.white;var titleRect=(RectTransform)topTitle.transform;titleRect.anchorMin=new Vector2(.13f,.48f);
   Text(logo.transform,"ИРЛАНДИЯ",35,.13f,.55f).color=new Color(1,.82f,.25f);
   var caption=TropicalUIArt.Place("Подпись острова",canvas,new Vector2(.035f,.045f),new Vector2(.43f,.135f),"WoodButton").gameObject;
   Text(caption.transform,"Маленький остров. Бесконечные волны.\nДержи биту крепче и побей свой рекорд!",16,.14f,.88f).color=CartoonUI.Cream;
   panel=Box(canvas,new Vector2(.70f,.16f),new Vector2(.96f,.87f),Color.clear);
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
  bool mainPage;
  void Clear(){mainPage=false;foreach(Transform child in panel.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}continueButton=newButton=null;}
  void Main(){Clear();mainPage=true;panel.GetComponent<Image>().color=Color.clear;continueButton=Button("ПРОДОЛЖИТЬ ИГРУ",.70f,()=>SceneFlow.Instance.Play(false));newButton=Button("НОВАЯ ИГРА",.55f,()=>SceneFlow.Instance.Play(true));Button("РЕКОРДЫ",.40f,Records);Button("НАСТРОЙКИ",.25f,Settings);Button("ВЫХОД",.10f,()=>Application.Quit());}
  void Records(){Clear();Frame();Text(panel.transform,"РЕКОРДЫ",27,.79f,.95f);var d=SaveSystem.Data;Text(panel.transform,$"Лучшее время\n{Mathf.FloorToInt(d.BestTime/60):00}:{Mathf.FloorToInt(d.BestTime%60):00}\n\nПобеждено врагов\n{d.BestKills}",21,.28f,.77f);Button("НАЗАД",.08f,Main);}
  void Settings(){
   Clear();Frame();Text(panel.transform,"НАСТРОЙКИ",27,.79f,.95f);
   Button("ГРОМКОСТЬ: "+Mathf.RoundToInt(SaveSystem.Data.Volume*100)+"%",.62f,()=>{SaveSystem.Data.Volume=SaveSystem.Data.Volume>=.99f?0:Mathf.Min(1,SaveSystem.Data.Volume+.1f);GameMusic.ApplyVolume();SaveSystem.Save();Settings();});
   Button("ЧУВСТВИТЕЛЬНОСТЬ: "+SaveSystem.Data.Sensitivity.ToString("0.#"),.49f,()=>{SaveSystem.Data.Sensitivity=SaveSystem.Data.Sensitivity>=3?.5f:Mathf.Min(3,SaveSystem.Data.Sensitivity+.5f);SaveSystem.Save();Settings();});
   Button("ФИКСАЦИЯ КАМЕРЫ: "+(SaveSystem.Data.CameraLocked?"ВКЛ":"ВЫКЛ"),.36f,()=>{if(GameManager.Instance!=null&&GameManager.Instance.Player!=null)GameManager.Instance.Player.SetCameraLock(!GameManager.Instance.Player.CameraLocked);else{SaveSystem.Data.CameraLocked=!SaveSystem.Data.CameraLocked;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();}Settings();});
   Button("КАЧЕСТВО: "+(SaveSystem.Data.Quality+1),.23f,()=>{int levels=Mathf.Clamp(QualitySettings.names.Length,1,3);SaveSystem.Data.Quality=(SaveSystem.Data.Quality+1)%levels;QualitySettings.SetQualityLevel(SaveSystem.Data.Quality);SaveSystem.Save();Settings();});
   Button("НАЗАД",.08f,Main);
  }
  Button Button(string title,float y,UnityEngine.Events.UnityAction action){var go=Box(panel.transform,new Vector2(.015f,y),new Vector2(.985f,y+(mainPage ? .12f : .095f)),CartoonUI.Aqua);go.name=title;var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();CartoonUI.StyleButton(button,title.Contains("ПРОДОЛЖИТЬ"));button.onClick.AddListener(action);var label=Text(go.transform,title,22,.12f,.88f);label.color=CartoonUI.Cream;label.fontStyle=FontStyle.Bold;return button;}
  Text Text(Transform parent,string value,int size,float bottom,float top){var go=new GameObject("Надпись",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Rect(go,new Vector2(.04f,bottom),new Vector2(.96f,top));var label=go.GetComponent<Text>();label.font=font;label.text=value;label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.color=CartoonUI.Gold;label.raycastTarget=false;CartoonUI.Typography(label,size>=20);return label;}
  static GameObject Box(Transform parent,Vector2 min,Vector2 max,Color color){return CartoonUI.Image("Панель",parent,min,max,color).gameObject;}
  void BuildBackdrop(){
   // Original abstract island postcard, used only until the owner's MenuBackground is imported.
   var texture=new Texture2D(4,128,TextureFormat.RGBA32,false);texture.name="Меню — тропический градиент";texture.wrapMode=TextureWrapMode.Clamp;
   for(int y=0;y<128;y++)for(int x=0;x<4;x++){float t=y/127f;texture.SetPixel(x,y,t<.40f?Color.Lerp(new Color(.10f,.55f,.65f),new Color(.31f,.78f,.79f),t/.4f):Color.Lerp(new Color(.68f,.88f,.84f),new Color(.27f,.65f,.80f),(t-.4f)/.6f));}
   texture.Apply(false,true);backdropTexture=texture;var bg=new GameObject("Авторский фон меню",typeof(RectTransform),typeof(RawImage));bg.transform.SetParent(canvas,false);Rect(bg,Vector2.zero,Vector2.one);bg.GetComponent<RawImage>().texture=texture;bg.GetComponent<RawImage>().raycastTarget=false;
   CartoonUI.Image("Солнце",canvas,new Vector2(.43f,.68f),new Vector2(.56f,.91f),new Color(1,.91f,.62f),CartoonUI.Disc).preserveAspect=true;
   for(int i=0;i<4;i++){float y=.39f-i*.066f;CartoonUI.Image("Лента прибоя",canvas,new Vector2(-.05f,y),new Vector2(1.05f,y+.009f),new Color(.84f,1,.95f,.18f));}
   CartoonUI.Image("Остров — тень",canvas,new Vector2(.07f,.19f),new Vector2(.55f,.33f),new Color(.08f,.38f,.43f,.28f),CartoonUI.Disc);
   CartoonUI.Image("Остров — песок",canvas,new Vector2(.095f,.22f),new Vector2(.53f,.37f),new Color(.98f,.84f,.54f),CartoonUI.Disc);
   CartoonUI.Image("Остров — трава",canvas,new Vector2(.17f,.265f),new Vector2(.46f,.38f),new Color(.47f,.73f,.43f),CartoonUI.Disc);
   for(int j=0;j<2;j++){
    float x=.22f+j*.17f;var trunk=CartoonUI.Image("Пальма — ствол",canvas,new Vector2(x,.31f),new Vector2(x+.014f,.59f-j*.08f),new Color(.59f,.38f,.23f));trunk.transform.localRotation=Quaternion.Euler(0,0,j==0?-8:9);
    for(int k=0;k<5;k++){var leaf=CartoonUI.Image("Пальма — лист",canvas,new Vector2(x-.07f,.55f-j*.08f),new Vector2(x+.09f,.585f-j*.08f),new Color(.29f,.62f,.39f));leaf.transform.localRotation=Quaternion.Euler(0,0,(k-2)*26);}
   }
  }
  void Frame(){var image=panel.GetComponent<Image>();image.sprite=TropicalUIArt.Get("WoodPanel");image.type=image.sprite.border.sqrMagnitude>.01f?Image.Type.Sliced:Image.Type.Simple;image.color=Color.white;}
  void OnDestroy(){if(backdropTexture!=null)Destroy(backdropTexture);}
  static void Rect(GameObject go,Vector2 min,Vector2 max){var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
 }
}
