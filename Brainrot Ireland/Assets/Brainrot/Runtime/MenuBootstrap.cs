using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
            backdrop=ReferenceScreens.Art(viewport,"ReferenceMenuScene",0,0,1,1);
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
            backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();
            var title=ReferenceScreens.Text(root,"ТУН ТУН\nСАХУР",53,.055f,.10f,.39f,.22f);title.color=new Color(1,.96f,.80f);
            ReferenceScreens.Text(root,"Выдержи волну. Собери монеты.\nВернись сильнее.",23,.06f,.75f,.42f,.15f);
            var panel=ReferenceScreens.Panel(root,"ОСТРОВ САХУРА",.58f,.09f,.36f,.83f);
            ReferenceScreens.SeaText(panel,"МОНЕТЫ  "+SaveSystem.Data.Coins,22,.10f,.215f,.80f,.065f);
            continueButton=MainButton(panel,"ПРОДОЛЖИТЬ",.32f,()=>SceneFlow.Instance.Play(false),true);
            newButton=MainButton(panel,"НОВЫЙ ЗАБЕГ",.415f,()=>SceneFlow.Instance.Play(true));
            MainButton(panel,"МАГАЗИН",.51f,Shop);MainButton(panel,"РЕКОРДЫ",.605f,Records);
            MainButton(panel,"НАСТРОЙКИ",.70f,Settings);MainButton(panel,"ВЫХОД",.795f,()=>Application.Quit());
            continueButton.interactable=!SceneFlow.Instance.Busy&&((GameManager.Instance!=null&&GameManager.Instance.CanContinue)||RunPersistence.HasSavedRun);
            newButton.interactable=!SceneFlow.Instance.Busy;
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(continueButton.interactable?continueButton.gameObject:newButton.gameObject);
        }
        Button MainButton(Transform root,string title,float y,UnityEngine.Events.UnityAction action,bool primary=false){
            return ReferenceScreens.SeaButton(root,title,.10f,y,.80f,.078f,()=>{if(!SceneFlow.Instance.Busy)action();},21,primary);
        }
        void Shop(){
            backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();Shade(root);
            bool continuing=(GameManager.Instance!=null&&GameManager.Instance.CanContinue)||RunPersistence.HasSavedRun;
            ShopScreen.Show(root,Main,continuing);
        }
        void Settings(){backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();Shade(root);ReferenceScreens.Settings(root,Main);}
        void Records(){
            backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();Shade(root);
            var frame=ReferenceScreens.Panel(root,"РЕКОРДЫ",.29f,.12f,.42f,.77f);var data=SaveSystem.Data;
            ReferenceScreens.SeaText(frame,"ЛУЧШЕЕ ВРЕМЯ",19,.12f,.25f,.76f,.06f);
            ReferenceScreens.SeaText(frame,$"{Mathf.FloorToInt(data.BestTime/60):00}:{Mathf.FloorToInt(data.BestTime%60):00}",34,.12f,.32f,.76f,.09f);
            ReferenceScreens.SeaText(frame,"ПОБЕЖДЕНО: "+data.BestKills,21,.12f,.45f,.76f,.07f);
            ReferenceScreens.SeaText(frame,"ЛУЧШАЯ ВОЛНА: "+data.BestWave,21,.12f,.54f,.76f,.07f);
            Button replay=null;replay=ReferenceScreens.SeaButton(frame,"ПОВТОРИТЬ ОБУЧЕНИЕ",.12f,.67f,.76f,.09f,()=>{
                SaveSystem.Data.TutorialCompleted=false;SaveSystem.Save();if(GameManager.Instance!=null&&GameManager.Instance.Goals!=null)GameManager.Instance.Goals.ReplayTutorial();replay.GetComponentInChildren<Text>().text="ОБУЧЕНИЕ ВКЛЮЧЕНО";
            },18);
            var back=ReferenceScreens.SeaButton(frame,"НАЗАД",.26f,.84f,.48f,.09f,Main,22,true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(back.gameObject);
        }
        static void Shade(Transform parent){CartoonUI.Image("Затемнение",parent,Vector2.zero,Vector2.one,new Color(.025f,.06f,.09f,.30f));}
    }
}
