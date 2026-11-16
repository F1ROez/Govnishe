using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace Brainrot
{
    public sealed class MenuBootstrap : MonoBehaviour
    {
        Texture2D backdropTexture;
        Font font;
        Transform canvas;
        GameObject panel, logo, caption;
        Button continueButton, newButton;
        bool mainPage;

        void Awake()
        {
            SaveSystem.Load();GameMusic.ApplyVolume();GameMusic.SetMenu();Time.timeScale=0;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var camera=new GameObject("Камера меню").AddComponent<Camera>();camera.transform.SetParent(transform);
            camera.gameObject.AddComponent<AudioListener>();camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.66f,.94f);
            var root=new GameObject("Главное меню",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            var c=root.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=100;canvas=root.transform;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
            font=CartoonUI.Font;
            var image=Resources.Load<Texture2D>("MenuBackground")??Resources.Load<Texture2D>("TropicalMenuBackdrop");
            if(image!=null){
                var bg=new GameObject("Фон",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));bg.transform.SetParent(canvas,false);
                bg.GetComponent<RawImage>().texture=image;bg.GetComponent<RawImage>().raycastTarget=false;
                var fitter=bg.GetComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fitter.aspectRatio=(float)image.width/image.height;
            }else BuildBackdrop();
            logo=TropicalUIArt.Place("Логотип на дереве",canvas,new Vector2(.02f,.715f),new Vector2(.56f,.995f),"LogoPlaque").gameObject;
            var islandTitle=Text(logo.transform,"ОСТРОВ",36,.41f,.63f);islandTitle.color=Color.white;
            Rect(islandTitle.gameObject,new Vector2(.24f,.41f),new Vector2(.76f,.63f));
            var sahurTitle=Text(logo.transform,"ТУН ТУН САХУРА",32,.22f,.43f);sahurTitle.color=new Color(1,.88f,.19f);
            Rect(sahurTitle.gameObject,new Vector2(.24f,.22f),new Vector2(.76f,.43f));
            caption=TropicalUIArt.Place("Подпись острова",canvas,new Vector2(.045f,.025f),new Vector2(.58f,.15f),"WoodButton").gameObject;
            Text(caption.transform,"Защищай остров от бесконечных волн!\nПродержись дольше и побей свой рекорд!",19,.17f,.83f).color=CartoonUI.Cream;
            panel=Box(canvas,new Vector2(.60f,.12f),new Vector2(.975f,.835f),Color.clear);
            var events=new GameObject("Управление меню",typeof(EventSystem));events.transform.SetParent(transform,false);
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
            Main();
        }
        void Start(){UIInputRouter.EnsureForScene(gameObject.scene);}
        void Update()
        {
            bool ready=!SceneFlow.Instance.Busy;
            if(continueButton!=null){bool available=ready&&GameManager.Instance!=null&&GameManager.Instance.HasRun;if(continueButton.interactable!=available)continueButton.interactable=available;}
            if(newButton!=null&&newButton.interactable!=ready)newButton.interactable=ready;
        }
        void Clear()
        {
            mainPage=false;
            foreach(Transform child in panel.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            continueButton=newButton=null;
        }
        void Main()
        {
            Clear();mainPage=true;logo.SetActive(true);caption.SetActive(true);
            Rect(panel,new Vector2(.60f,.12f),new Vector2(.975f,.835f));panel.GetComponent<Image>().color=Color.clear;
            continueButton=Button("ПРОДОЛЖИТЬ ИГРУ",.81f,()=>SceneFlow.Instance.Play(false),"MenuPlay");
            newButton=Button("НОВАЯ ИГРА",.62f,()=>SceneFlow.Instance.Play(true),"MenuNew");
            Button("РЕКОРДЫ",.43f,Records,"MenuRecords");
            Button("НАСТРОЙКИ",.24f,Settings,"MenuSettings");
            Button("ВЫХОД",.05f,()=>Application.Quit(),"MenuExit");
        }
        void Records()
        {
            Clear();Frame("РЕКОРДЫ");var d=SaveSystem.Data;
            Text(panel.transform,"ЛУЧШЕЕ ВРЕМЯ",25,.66f,.76f);
            Text(panel.transform,$"{Mathf.FloorToInt(d.BestTime/60):00}:{Mathf.FloorToInt(d.BestTime%60):00}",48,.51f,.65f).color=CartoonUI.Gold;
            Text(panel.transform,"ПОБЕЖДЕНО ВРАГОВ",25,.39f,.49f);
            Text(panel.transform,d.BestKills.ToString(),43,.25f,.39f).color=CartoonUI.Gold;
            Button("НАЗАД",.065f,Main);
        }
        void Settings()
        {
            Clear();Frame("НАСТРОЙКИ");
            SettingSlider("ГРОМКОСТЬ",.745f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;GameMusic.ApplyVolume();SaveSystem.Save();});
            SettingSlider("ЧУВСТВИТЕЛЬНОСТЬ МЫШИ",.60f,.2f,3,SaveSystem.Data.Sensitivity,v=>{SaveSystem.Data.Sensitivity=v;SaveSystem.Save();});
            Button("ФИКСАЦИЯ КАМЕРЫ: "+(SaveSystem.Data.CameraLocked?"ВКЛ":"ВЫКЛ"),.46f,()=>{
                if(GameManager.Instance!=null&&GameManager.Instance.Player!=null)GameManager.Instance.Player.SetCameraLock(!GameManager.Instance.Player.CameraLocked);
                else{SaveSystem.Data.CameraLocked=!SaveSystem.Data.CameraLocked;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();}
                Settings();
            });
            Button("КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ"),.335f,()=>{SaveSystem.Data.ShowAttackRange=!SaveSystem.Data.ShowAttackRange;SaveSystem.Save();Settings();});
            Text(panel.transform,"КАЧЕСТВО ГРАФИКИ",19,.275f,.33f);
            QualityButton("НИЗКОЕ",.10f,.35f,0);QualityButton("СРЕДНЕЕ",.375f,.625f,1);QualityButton("ВЫСОКОЕ",.65f,.90f,2);
            Button("НАЗАД",.065f,Main);
        }
        void SettingSlider(string title,float y,float min,float max,float value,UnityEngine.Events.UnityAction<float> action)
        {
            var label=Text(panel.transform,title,21,y+.052f,y+.105f);label.alignment=TextAnchor.MiddleLeft;
            Rect(label.gameObject,new Vector2(.12f,y+.052f),new Vector2(.88f,y+.105f));
            CartoonUI.GlossySlider(title,panel.transform,new Vector2(.12f,y),new Vector2(.88f,y+.048f),min,max,value,action);
        }
        void QualityButton(string title,float left,float right,int quality)
        {
            var button=MakeButton(title,new Vector2(left,.19f),new Vector2(right,.27f),()=>{
                SaveSystem.Data.Quality=quality;QualitySettings.SetQualityLevel(Mathf.Min(quality,QualitySettings.names.Length-1),true);SaveSystem.Save();Settings();
            },true,null,18);
            if(SaveSystem.Data.Quality==quality)CartoonUI.MarkSelected(button);
        }
        Button Button(string title,float y,UnityEngine.Events.UnityAction action,string icon=null)
        {
            return MakeButton(title,new Vector2(mainPage?0:.11f,y),new Vector2(mainPage?1:.89f,y+(mainPage?.165f:.105f)),action,!mainPage||title.Contains("ПРОДОЛЖИТЬ"),icon,mainPage?25:22);
        }
        Button MakeButton(string title,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action,bool primary,string icon,int size)
        {
            var go=Box(panel.transform,min,max,Color.white);go.name=title;
            var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();CartoonUI.StyleButton(button,primary);button.onClick.AddListener(action);
            if(icon!=null)TropicalUIArt.Place("Значок "+title,go.transform,new Vector2(.065f,.20f),new Vector2(.22f,.80f),icon,true);
            var label=Text(go.transform,title,size,.18f,.82f);label.color=CartoonUI.Cream;label.fontStyle=FontStyle.Bold;
            Rect(label.gameObject,new Vector2(icon!=null?.235f:.06f,.18f),new Vector2(.94f,.82f));
            return button;
        }
        Text Text(Transform parent,string value,int size,float bottom,float top)
        {
            var go=new GameObject("Надпись",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Rect(go,new Vector2(.10f,bottom),new Vector2(.90f,top));
            var label=go.GetComponent<Text>();label.font=font;label.text=value;label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            CartoonUI.Typography(label,size>=20);return label;
        }
        static GameObject Box(Transform parent,Vector2 min,Vector2 max,Color color){return CartoonUI.Image("Панель",parent,min,max,color).gameObject;}
        void Frame(string title)
        {
            logo.SetActive(false);caption.SetActive(false);Rect(panel,new Vector2(.235f,.06f),new Vector2(.765f,.83f));
            var image=panel.GetComponent<Image>();image.sprite=TropicalUIArt.Get("WoodPanel");image.type=image.sprite.border.sqrMagnitude>.01f?Image.Type.Sliced:Image.Type.Simple;image.color=Color.white;
            var header=TropicalUIArt.Place("Заголовок экрана",panel.transform,new Vector2(-.06f,.875f),new Vector2(1.06f,1.155f),"HeaderPlaque");
            var heading=Text(header.transform,title,42,.20f,.60f);Rect(heading.gameObject,new Vector2(.25f,.20f),new Vector2(.75f,.60f));
            TropicalUIArt.Place("Листва слева",panel.transform,new Vector2(-.08f,-.035f),new Vector2(.18f,.16f),"LeafAccent",true);
            var leaf=TropicalUIArt.Place("Листва справа",panel.transform,new Vector2(.82f,-.035f),new Vector2(1.08f,.16f),"LeafAccent",true);leaf.rectTransform.localScale=new Vector3(-1,1,1);
            TropicalUIArt.Place("Гибискус",panel.transform,new Vector2(.43f,-.035f),new Vector2(.57f,.065f),"FlowerAccent",true);
        }
        void BuildBackdrop()
        {
            var texture=new Texture2D(4,128,TextureFormat.RGBA32,false);texture.name="Меню — тропический градиент";texture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<128;y++)for(int x=0;x<4;x++){float t=y/127f;texture.SetPixel(x,y,t<.4f?Color.Lerp(new Color(0,.52f,.78f),new Color(.03f,.85f,.93f),t/.4f):Color.Lerp(new Color(.65f,.91f,1),new Color(.02f,.49f,1),(t-.4f)/.6f));}
            texture.Apply(false,true);backdropTexture=texture;
            var bg=new GameObject("Авторский фон меню",typeof(RectTransform),typeof(RawImage));bg.transform.SetParent(canvas,false);Rect(bg,Vector2.zero,Vector2.one);bg.GetComponent<RawImage>().texture=texture;bg.GetComponent<RawImage>().raycastTarget=false;
            CartoonUI.Image("Солнце",canvas,new Vector2(.43f,.68f),new Vector2(.56f,.91f),new Color(1,.95f,.75f),CartoonUI.Disc).preserveAspect=true;
            for(int i=0;i<4;i++){float y=.39f-i*.066f;CartoonUI.Image("Лента прибоя",canvas,new Vector2(-.05f,y),new Vector2(1.05f,y+.009f),new Color(.84f,1,.95f,.28f));}
            CartoonUI.Image("Остров — тень",canvas,new Vector2(.07f,.19f),new Vector2(.55f,.33f),new Color(.08f,.38f,.43f,.28f),CartoonUI.Disc);
            CartoonUI.Image("Остров — песок",canvas,new Vector2(.095f,.22f),new Vector2(.53f,.37f),new Color(1,.85f,.48f),CartoonUI.Disc);
            for(int j=0;j<2;j++){
                float x=.22f+j*.17f;var trunk=CartoonUI.Image("Пальма — ствол",canvas,new Vector2(x,.31f),new Vector2(x+.014f,.59f-j*.08f),new Color(.76f,.43f,.13f));trunk.transform.localRotation=Quaternion.Euler(0,0,j==0?-8:9);
                for(int k=0;k<5;k++){var leaf=CartoonUI.Image("Пальма — лист",canvas,new Vector2(x-.07f,.55f-j*.08f),new Vector2(x+.09f,.585f-j*.08f),new Color(.37f,.8f,.06f));leaf.transform.localRotation=Quaternion.Euler(0,0,(k-2)*26);}
            }
        }
        void OnDestroy(){if(backdropTexture!=null)Destroy(backdropTexture);}
        static void Rect(GameObject go,Vector2 min,Vector2 max){var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
