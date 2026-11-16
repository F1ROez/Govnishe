using UnityEngine;
using UnityEngine.UI;
namespace Brainrot
{
    public sealed class MenuBootstrap:MonoBehaviour
    {
        RectTransform viewport;
        GameObject page;
        Button continueButton,newButton;
        Image backdrop;
        void Awake(){
            SaveSystem.Load();Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var camera=new GameObject("Камера меню",typeof(Camera),typeof(AudioListener));camera.transform.SetParent(transform,false);
            var view=camera.GetComponent<Camera>();view.cullingMask=0;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.015f,.15f,.25f);
            GameMusic.ApplyVolume();GameMusic.SetMenu();
            var root=new GameObject("Главное меню",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=100;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            viewport=ReferenceScreens.Viewport(root.transform,"Композиция 16:9");
            backdrop=ReferenceScreens.Art(viewport,"ReferenceMainMenu",0,0,1,1);
            UIInputRouter.EnsureForScene(gameObject.scene);Main();
        }
        void Start(){UIInputRouter.EnsureForScene(gameObject.scene);}
        void Update(){
            bool ready=!SceneFlow.Instance.Busy;
            if(continueButton!=null)continueButton.interactable=ready&&((GameManager.Instance!=null&&GameManager.Instance.CanContinue)||RunPersistence.HasSavedRun);
            if(newButton!=null)newButton.interactable=ready;
        }
        RectTransform Clear(){
            if(page!=null){page.SetActive(false);Destroy(page);}continueButton=newButton=null;
            page=new GameObject("Активный экран",typeof(RectTransform));page.transform.SetParent(viewport,false);var rect=(RectTransform)page.transform;ReferenceScreens.Rect(rect,0,0,1,1);return rect;
        }
        void Main(){
            backdrop.sprite=TropicalUIArt.Get("ReferenceMainMenu");var root=Clear();
            ReferenceScreens.Text(root,"ОСТРОВ",43,.085f,.051f,.25f,.074f);
            var name=ReferenceScreens.Text(root,"ТУН ТУН САХУРА",35,.055f,.127f,.30f,.079f);name.color=new Color(1,.86f,.12f);
            ReferenceScreens.Text(root,"Защищай остров от бесконечных волн!\nПродержись дольше и побей свой рекорд!",20,.12f,.889f,.39f,.073f);
            continueButton=MainButton(root,"ПРОДОЛЖИТЬ ИГРУ",.149f,()=>SceneFlow.Instance.Play(false));
            newButton=MainButton(root,"НОВАЯ ИГРА",.291f,()=>SceneFlow.Instance.Play(true));
            MainButton(root,"РЕКОРДЫ",.429f,Records);MainButton(root,"НАСТРОЙКИ",.568f,Settings);MainButton(root,"ВЫХОД",.707f,()=>Application.Quit());
        }
        Button MainButton(Transform root,string title,float y,UnityEngine.Events.UnityAction action){
            var b=ReferenceScreens.Hotspot(root,"",.621f,y,.31f,.12f,action);
            var face=b.targetGraphic.GetComponent<Image>();face.sprite=TropicalUIArt.Get(title=="ПРОДОЛЖИТЬ ИГРУ"?"GoldButton":"WoodButton");
            string icon=title=="ПРОДОЛЖИТЬ ИГРУ"?"MenuPlay":title=="НОВАЯ ИГРА"?"MenuNew":title=="РЕКОРДЫ"?"MenuRecords":title=="НАСТРОЙКИ"?"MenuSettings":"MenuExit";
            ReferenceScreens.Art(face.transform,icon,.09f,.18f,.16f,.64f).preserveAspect=true;
            var text=b.GetComponentInChildren<Text>();text.text=title;text.fontSize=title=="ПРОДОЛЖИТЬ ИГРУ"?24:29;text.resizeTextMaxSize=text.fontSize;ReferenceScreens.Rect((RectTransform)text.transform,.265f,.12f,.69f,.76f);return b;
        }
        void Settings(){backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();Shade(root);ReferenceScreens.Settings(root,Main);}
        void Records(){
            backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();Shade(root);
            var frame=ReferenceScreens.Art(root,"ReferenceSettings",.21f,.145f,.58f,.80f).transform;
            ReferenceScreens.Text(frame,"РЕКОРДЫ",43,.24f,.12f,.52f,.12f);var data=SaveSystem.Data;
            ReferenceScreens.Text(frame,"ЛУЧШЕЕ ВРЕМЯ",23,.20f,.33f,.60f,.06f);
            ReferenceScreens.Text(frame,$"{Mathf.FloorToInt(data.BestTime/60):00}:{Mathf.FloorToInt(data.BestTime%60):00}",38,.20f,.40f,.60f,.09f);
            ReferenceScreens.Text(frame,"ПОБЕЖДЕНО: "+data.BestKills,24,.20f,.51f,.60f,.07f);
            ReferenceScreens.Text(frame,"ЛУЧШАЯ ВОЛНА: "+data.BestWave,24,.20f,.60f,.60f,.07f);
            Button replay=null;replay=ReferenceScreens.Hotspot(frame,"ПОВТОРИТЬ ОБУЧЕНИЕ",.20f,.72f,.60f,.087f,()=>{
                SaveSystem.Data.TutorialCompleted=false;SaveSystem.Save();if(GameManager.Instance!=null&&GameManager.Instance.Goals!=null)GameManager.Instance.Goals.ReplayTutorial();replay.GetComponentInChildren<Text>().text="ОБУЧЕНИЕ ВКЛЮЧЕНО";
            },23);
            ReferenceScreens.Hotspot(frame,"НАЗАД",.323f,.867f,.357f,.09f,Main,29);
        }
        static void Shade(Transform parent){CartoonUI.Image("Затемнение",parent,Vector2.zero,Vector2.one,new Color(.025f,.06f,.09f,.30f));}
    }
}
