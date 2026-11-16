using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace Brainrot
{
    /// <summary>Coordinates follow the supplied 16:9 compositions. Art and interactive rectangles share one viewport.</summary>
    public static class ReferenceScreens
    {
        public static RectTransform Viewport(Transform parent,string name){
            var go=new GameObject(name,typeof(RectTransform),typeof(ReferenceViewport));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1280,720);
            go.GetComponent<ReferenceViewport>().Fit();return rect;
        }
        public static void Rect(RectTransform rect,float x,float y,float w,float h){
            rect.anchorMin=new Vector2(x,1-y-h);rect.anchorMax=new Vector2(x+w,1-y);rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        public static Image Art(Transform parent,string key,float x,float y,float w,float h){
            var image=TropicalUIArt.Place(key,parent,Vector2.zero,Vector2.one,key);Rect(image.rectTransform,x,y,w,h);image.type=Image.Type.Simple;return image;
        }
        public static Text Text(Transform parent,string value,int size,float x,float y,float w,float h){
            var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Rect((RectTransform)go.transform,x,y,w,h);
            var label=go.GetComponent<Text>();label.text=value;label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            CartoonUI.Typography(label,true);label.horizontalOverflow=HorizontalWrapMode.Wrap;label.resizeTextMinSize=12;
            label.GetComponent<Outline>().effectDistance=new Vector2(2,-2);return label;
        }
        public static Button Hotspot(Transform parent,string title,float x,float y,float w,float h,UnityAction action,int size=28){
            var image=CartoonUI.Image(title,parent,Vector2.zero,Vector2.one,Color.white);Rect(image.rectTransform,x,y,w,h);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.clear;colors.highlightedColor=new Color(1,1,.7f,.12f);colors.pressedColor=new Color(.25f,.10f,0,.24f);colors.disabledColor=new Color(.08f,.08f,.08f,.42f);colors.fadeDuration=.08f;button.colors=colors;
            Text(image.transform,title,size,.025f,.12f,.95f,.76f);button.onClick.AddListener(GameMusic.PlayButtonClick);button.onClick.AddListener(action);CartoonUI.EnsureTouchTarget(button);return button;
        }
        public static void Pause(Transform viewport,UnityAction resume,UnityAction settings,UnityAction menu){
            var frame=Art(viewport,"ReferencePause",.215f,.165f,.57f,.76f);
            Text(frame.transform,"ПАУЗА",52,.27f,.15f,.46f,.12f);
            Hotspot(frame.transform,"ПРОДОЛЖИТЬ",.26f,.382f,.48f,.132f,resume,32);
            Hotspot(frame.transform,"НАСТРОЙКИ",.26f,.536f,.48f,.132f,settings,32);
            Hotspot(frame.transform,"ГЛАВНОЕ МЕНЮ",.26f,.690f,.48f,.132f,menu,29);
        }
        public static void Settings(Transform viewport,UnityAction back){
            var frame=Art(viewport,"ReferenceSettings",.21f,.145f,.58f,.80f).transform;
            Text(frame,"НАСТРОЙКИ",43,.24f,.12f,.52f,.12f);
            Slider(frame,"ГРОМКОСТЬ",.318f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;GameMusic.ApplyVolume();SaveSystem.Save();});
            Slider(frame,"ЧУВСТВИТЕЛЬНОСТЬ МЫШИ",.424f,.2f,3,SaveSystem.Data.Sensitivity,v=>{SaveSystem.Data.Sensitivity=v;SaveSystem.Save();});
            var player=GameManager.Instance!=null?GameManager.Instance.Player:null;bool locked=player!=null?player.CameraLocked:SaveSystem.Data.CameraLocked;
            Button camera=null,range=null;
            camera=Hotspot(frame,"ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ"),.20f,.552f,.60f,.087f,()=>{
                locked=!locked;if(player!=null)player.SetCameraLock(locked);else{SaveSystem.Data.CameraLocked=locked;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();}
                camera.GetComponentInChildren<Text>().text="ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ");
            },23);
            range=Hotspot(frame,"КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ"),.20f,.654f,.60f,.087f,()=>{
                SaveSystem.Data.ShowAttackRange=!SaveSystem.Data.ShowAttackRange;SaveSystem.Save();range.GetComponentInChildren<Text>().text="КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ");
            },23);
            Text(frame,"КАЧЕСТВО ГРАФИКИ",20,.23f,.75f,.54f,.045f);
            var buttons=new Button[3];string[] titles={"НИЗКОЕ","СРЕДНЕЕ","ВЫСОКОЕ"};
            for(int i=0;i<3;i++){
                int level=i;float left=.19f+i*.215f;
                var art=Art(frame,SaveSystem.Data.Quality==i?"BlueButton":"GoldButton",left,.799f,.20f,.06f);
                buttons[i]=Hotspot(art.transform,titles[i],0,0,1,1,()=>{
                    SaveSystem.Data.Quality=level;QualitySettings.SetQualityLevel(Mathf.Clamp(level,0,Mathf.Max(0,QualitySettings.names.Length-1)),true);SaveSystem.Save();
                    for(int j=0;j<buttons.Length;j++){var image=buttons[j].transform.parent.GetComponent<Image>();image.sprite=TropicalUIArt.Get(j==level?"BlueButton":"GoldButton");}
                },16);
            }
            Hotspot(frame,"НАЗАД",.323f,.867f,.357f,.09f,back,29);
        }
        static void Slider(Transform frame,string title,float top,float min,float max,float value,UnityAction<float> action){
            var label=Text(frame,title,21,.19f,top,.62f,.043f);label.alignment=TextAnchor.MiddleLeft;
            CartoonUI.GlossySlider(title,frame,new Vector2(.19f,1-top-.09f),new Vector2(.81f,1-top-.053f),min,max,value,action);
        }
    }
    public sealed class ReferenceViewport:MonoBehaviour
    {
        Vector2 previous;
        public void Fit(){var parent=transform.parent as RectTransform;if(parent==null)return;previous=parent.rect.size;float s=Mathf.Min(previous.x/1280,previous.y/720);transform.localScale=Vector3.one*Mathf.Max(.01f,s);}
        void LateUpdate(){var parent=transform.parent as RectTransform;if(parent!=null&&parent.rect.size!=previous)Fit();}
    }
}
