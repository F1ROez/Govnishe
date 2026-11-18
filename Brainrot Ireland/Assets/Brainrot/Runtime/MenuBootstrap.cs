using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Brainrot
{
    public sealed class MenuBootstrap:MonoBehaviour
    {
        RectTransform viewport;
        GameObject page;
        Button playButton;
        readonly System.Collections.Generic.List<Button> mainButtons=new System.Collections.Generic.List<Button>();
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
            foreach(var button in mainButtons)if(button!=null)button.interactable=ready;
        }
        RectTransform Clear(){
            if(page!=null){page.SetActive(false);Destroy(page);}playButton=null;mainButtons.Clear();
            page=new GameObject("Активный экран",typeof(RectTransform));page.transform.SetParent(viewport,false);var rect=(RectTransform)page.transform;ReferenceScreens.Rect(rect,0,0,1,1);return rect;
        }
        void Main(){
            backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();
            root.gameObject.AddComponent<MainMenuEntrance>();
            // The hero occupies the left middle of the painting. Keep titles above his head and actions over open sea.
            var logo=ReferenceScreens.Art(root,"WoodButton",.035f,.035f,.405f,.165f);logo.color=new Color(.86f,.77f,.62f);
            var island=ReferenceScreens.Text(logo.transform,"ОСТРОВ",19,.12f,.08f,.76f,.28f);island.color=new Color(1,.97f,.85f);
            ReferenceScreens.Text(logo.transform,"ТУН ТУН САХУРА",31,.09f,.35f,.82f,.47f);
            var leaf=ReferenceScreens.Art(root,"LeafAccent",.014f,.02f,.09f,.13f);leaf.preserveAspect=true;leaf.color=new Color(.83f,.93f,.70f);
            Wallet(root,.70f,.09f,.245f,.073f);
            // A compact dock, rather than a second full-screen modal, lets the island artwork breathe.
            ReferenceScreens.Surface(root,"Тень меню",.635f,.241f,.31f,.60f,new Color(.02f,.13f,.16f,.28f));
            var rim=ReferenceScreens.Surface(root,"Деревянный край меню",.635f,.23f,.31f,.60f,new Color(.43f,.29f,.15f)).transform;
            var dock=ReferenceScreens.Surface(rim,"Песочная карточка меню",.008f,.009f,.984f,.982f,new Color(.97f,.92f,.80f)).transform;
            ReferenceScreens.SeaText(dock,"ПРИКЛЮЧЕНИЕ ЖДЁТ",17,.08f,.055f,.84f,.065f);
            playButton=MainButton(dock,"ИГРАТЬ",.16f,.17f,()=>SceneFlow.Instance.Play(true),true);
            MainButton(dock,"МАГАЗИН",.385f,.105f,Shop);
            MainButton(dock,"РЕКОРДЫ",.515f,.105f,Records);
            MainButton(dock,"НАСТРОЙКИ",.645f,.105f,Settings);
            MainButton(dock,"ВЫХОД",.775f,.105f,()=>Application.Quit());
            ReferenceScreens.SeaText(dock,"Монеты и улучшения остаются навсегда",12,.07f,.912f,.86f,.05f);
            var slogan=ReferenceScreens.Surface(root,"Подпись острова",.055f,.867f,.48f,.09f,new Color(.025f,.22f,.27f,.78f)).transform;
            ReferenceScreens.Text(slogan,"ВЫДЕРЖИ ВОЛНУ. ВЕРНИСЬ СИЛЬНЕЕ.",19,.035f,.10f,.93f,.80f);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }
        void Wallet(Transform root,float x,float y,float w,float h){
            var frame=ReferenceScreens.Surface(root,"Кошелёк",x,y,w,h,new Color(.97f,.92f,.80f)).transform;
            var disc=CartoonUI.Image("Золотая монета",frame,new Vector2(.06f,.20f),new Vector2(.19f,.80f),new Color(1,.71f,.12f),CartoonUI.Disc);
            var mark=ReferenceScreens.SeaText(disc.transform,"I",17,0,0,1,1);mark.color=new Color(.53f,.32f,.07f);
            var amount=ReferenceScreens.SeaText(frame,SaveSystem.Data.Coins+" МОНЕТ",22,.24f,.12f,.71f,.76f);amount.alignment=TextAnchor.MiddleLeft;
        }
        Button MainButton(Transform root,string title,float y,float height,UnityEngine.Events.UnityAction action,bool primary=false){
            var button=ReferenceScreens.SeaButton(root,title,.08f,y,.84f,height,()=>{if(!SceneFlow.Instance.Busy)action();},primary?30:20,primary);
            mainButtons.Add(button);return button;
        }
        void Shop(){
            backdrop.sprite=TropicalUIArt.Get("ReferenceMenuScene");var root=Clear();Shade(root);ShopScreen.Show(root,Main);
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
    /// <summary>Unscaled, brief entrance; pauses never freeze menu presentation.</summary>
    public sealed class MainMenuEntrance:MonoBehaviour
    {
        CanvasGroup group;float age;Vector3 baseline;
        void Awake(){group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;baseline=transform.localPosition;}
        void Update(){
            age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/.28f),ease=1-(1-t)*(1-t)*(1-t);
            group.alpha=ease;transform.localPosition=baseline+Vector3.down*((1-ease)*10);
            if(t>=1){transform.localPosition=baseline;enabled=false;}
        }
    }
}
