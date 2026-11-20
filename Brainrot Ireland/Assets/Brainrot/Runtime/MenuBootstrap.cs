using UnityEngine;
using UnityEngine.UI;
namespace Brainrot
{
    public sealed class MenuBootstrap:MonoBehaviour
    {
        RectTransform viewport;GameObject page;Image backdrop;
        Text notice;bool shownReady;
        readonly System.Collections.Generic.List<Button> mainButtons=new System.Collections.Generic.List<Button>();
        void Awake(){
            SaveSystem.Load();Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var camera=new GameObject("Камера меню",typeof(Camera),typeof(AudioListener));camera.transform.SetParent(transform,false);
            var view=camera.GetComponent<Camera>();view.cullingMask=0;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.015f,.15f,.25f);
            GameMusic.ApplyVolume();GameMusic.SetMenu();
            var root=new GameObject("Главное меню",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=100;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            viewport=ReferenceScreens.Viewport(root.transform,"Композиция 16:9");
            backdrop=ReferenceScreens.Art(viewport,"IslandMenuScene",0,0,1,1);
            UIInputRouter.EnsureForScene(gameObject.scene);Main();
        }
        void Start(){UIInputRouter.EnsureForScene(gameObject.scene);}
        void Update(){
            bool ready=!SceneFlow.Instance.Busy;
            if(ready==shownReady)return;shownReady=ready;
            foreach(var button in mainButtons)if(button!=null)button.interactable=ready;
        }
        RectTransform Clear(){
            if(page!=null){page.SetActive(false);Destroy(page);}notice=null;mainButtons.Clear();
            page=new GameObject("Активный экран",typeof(RectTransform));page.transform.SetParent(viewport,false);var rect=(RectTransform)page.transform;ReferenceScreens.Rect(rect,0,0,1,1);
            page.AddComponent<MainMenuEntrance>();return rect;
        }
        void Main(){
            backdrop.sprite=TropicalUIArt.Get("IslandMenuScene");var root=Clear();
            var buttons=ReferenceScreens.MainMenu(root,Guard(()=>SceneFlow.Instance.Play(true)),Guard(Shop),Guard(Records),Guard(Settings),Guard(Exit),out _,out notice);
            mainButtons.AddRange(buttons);shownReady=!SceneFlow.Instance.Busy;foreach(var button in mainButtons)button.interactable=shownReady;
        }
        UnityEngine.Events.UnityAction Guard(UnityEngine.Events.UnityAction action){return ()=>{if(!SceneFlow.Instance.Busy)action();};}
        void Shop(){var root=Clear();ShopScreen.Show(root,Main);}
        void Settings(){var root=Clear();ReferenceScreens.Settings(root,Main);}
        void Records(){var root=Clear();ReferenceScreens.Records(root,Main);}
        void Exit(){
            if(Application.platform==RuntimePlatform.WebGLPlayer){notice.text="Чтобы выйти, закрой вкладку с игрой.";return;}
            if(Application.isEditor){notice.text="Выход работает в готовой сборке игры.";return;}
            Application.Quit();
        }
    }
    /// <summary>Unscaled entrance on navigation; scene transition buttons stay unavailable while a scene loads.</summary>
    public sealed class MainMenuEntrance:MonoBehaviour
    {
        CanvasGroup group;float age;Vector3 baseline;
        void Awake(){group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;baseline=transform.localPosition;}
        void Update(){
            age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/.23f),ease=1-(1-t)*(1-t)*(1-t);
            group.alpha=ease;transform.localPosition=baseline+Vector3.down*((1-ease)*9);
            if(t>=1){transform.localPosition=baseline;enabled=false;}
        }
    }
}
