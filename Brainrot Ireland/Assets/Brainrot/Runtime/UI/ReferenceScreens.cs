using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
            image.color=Color.clear;
            var button=image.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
            var visual=Art(image.transform,"GoldButton",0,0,1,1);visual.type=Image.Type.Simple;button.targetGraphic=visual;
            Text(visual.transform,title,size,.06f,.15f,.88f,.70f);
            image.gameObject.AddComponent<CartoonButtonMotion>().VisualTarget=visual.transform;
            button.onClick.AddListener(GameMusic.PlayButtonClick);button.onClick.AddListener(action);CartoonUI.EnsureTouchTarget(button);return button;
        }
        public static void Pause(Transform viewport,UnityAction resume,UnityAction settings,UnityAction menu){
            var frame=Art(viewport,"ReferencePause",.23f,.21f,.54f,.72f);
            Text(frame.transform,"ПАУЗА",62,.27f,.15f,.46f,.12f);
            var first=Hotspot(frame.transform,"ПРОДОЛЖИТЬ",.235f,.378f,.54f,.132f,resume,32);
            Hotspot(frame.transform,"НАСТРОЙКИ",.235f,.545f,.54f,.132f,settings,32);
            Hotspot(frame.transform,"ГЛАВНОЕ МЕНЮ",.235f,.706f,.54f,.132f,menu,29);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        public static void Settings(Transform viewport,UnityAction back){
            var frame=Art(viewport,"ReferenceSettings",.215f,.205f,.57f,.76f).transform;
            Text(frame,"НАСТРОЙКИ",43,.24f,.095f,.52f,.12f).color=CartoonUI.Gold;
            Slider(frame,"ГРОМКОСТЬ",.255f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;GameMusic.ApplyVolume();SaveSystem.Save();});
            Slider(frame,"ЧУВСТВИТЕЛЬНОСТЬ МЫШИ",.369f,.2f,3,SaveSystem.Data.Sensitivity,v=>{SaveSystem.Data.Sensitivity=v;SaveSystem.Save();});
            var player=GameManager.Instance!=null?GameManager.Instance.Player:null;bool locked=player!=null?player.CameraLocked:SaveSystem.Data.CameraLocked;
            Button camera=null,range=null;
            camera=Hotspot(frame,"ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ"),.18f,.485f,.66f,.087f,()=>{
                locked=!locked;if(player!=null)player.SetCameraLock(locked);else{SaveSystem.Data.CameraLocked=locked;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();}
                camera.GetComponentInChildren<Text>().text="ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ");
            },23);
            range=Hotspot(frame,"КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ"),.18f,.582f,.66f,.087f,()=>{
                SaveSystem.Data.ShowAttackRange=!SaveSystem.Data.ShowAttackRange;SaveSystem.Save();range.GetComponentInChildren<Text>().text="КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ");
            },23);
            Text(frame,"КАЧЕСТВО ГРАФИКИ",20,.29f,.682f,.42f,.045f);
            Flourish(frame,.21f,.682f,false);Flourish(frame,.72f,.682f,true);
            var buttons=new Button[3];string[] titles={"НИЗКОЕ","СРЕДНЕЕ","ВЫСОКОЕ"};
            for(int i=0;i<3;i++){
                int level=i;float left=.15f+i*.245f;
                buttons[i]=Hotspot(frame,titles[i],left,.735f,.225f,.08f,()=>{
                    SaveSystem.Data.Quality=level;GameQuality.Apply(level);SaveSystem.Save();
                    for(int j=0;j<buttons.Length;j++)buttons[j].targetGraphic.GetComponent<Image>().sprite=TropicalUIArt.Get(j==level?"BlueButton":"GoldButton");
                },16);
                buttons[i].targetGraphic.GetComponent<Image>().sprite=TropicalUIArt.Get(SaveSystem.Data.Quality==i?"BlueButton":"GoldButton");
            }
            Hotspot(frame,"НАЗАД",.30f,.855f,.405f,.10f,back,29);
            Button shake=null;bool shaking=SaveSystem.Data.CameraShakeStrength>.01f;
            shake=Hotspot(frame,"ТРЯСКА\n"+(shaking?"ВКЛ":"ВЫКЛ"),.745f,.865f,.15f,.075f,()=>{
                shaking=!shaking;SaveSystem.Data.CameraShakeStrength=shaking?1:0;SaveSystem.Save();
                shake.GetComponentInChildren<Text>().text="ТРЯСКА\n"+(shaking?"ВКЛ":"ВЫКЛ");
            },12);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(camera.gameObject);
        }
        static void Flourish(Transform parent,float x,float y,bool flip){
            var go=new GameObject("Морской завиток",typeof(RectTransform),typeof(WaveFlourish));go.transform.SetParent(parent,false);Rect((RectTransform)go.transform,x,y,.085f,.045f);go.GetComponent<WaveFlourish>().raycastTarget=false;if(flip)go.transform.localScale=new Vector3(-1,1,1);
        }
        static void Slider(Transform frame,string title,float top,float min,float max,float value,UnityAction<float> action){
            var label=Text(frame,title,21,.19f,top,.62f,.043f);label.alignment=TextAnchor.MiddleLeft;
            CartoonUI.GlossySlider(title,frame,new Vector2(.19f,1-top-.09f),new Vector2(.81f,1-top-.053f),min,max,value,action);
        }
    }
    /// <summary>Small canvas flourish, drawn as smooth ribbons without cropped atlas edges.</summary>
    public sealed class WaveFlourish:MaskableGraphic
    {
        static Vector2 Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t){float u=1-t;return a*(u*u*u)+b*(3*u*u*t)+c*(3*u*t*t)+d*(t*t*t);}
        Vector2 Point(int index){
            Vector2 p=index<=24?Bezier(new Vector2(.02f,.32f),new Vector2(.24f,.88f),new Vector2(.43f,.66f),new Vector2(.61f,.36f),index/24f):Bezier(new Vector2(.61f,.36f),new Vector2(.99f,.01f),new Vector2(1,.87f),new Vector2(.76f,.61f),(index-24)/24f);
            var rect=rectTransform.rect;return new Vector2(rect.xMin+p.x*rect.width,rect.yMin+p.y*rect.height);
        }
        protected override void OnPopulateMesh(VertexHelper mesh){
            mesh.Clear();float h=rectTransform.rect.height;
            Stroke(mesh,h*.28f,new Color(.025f,.28f,.45f),0);Stroke(mesh,h*.20f,new Color(.02f,.85f,1),0);Stroke(mesh,h*.055f,new Color(.75f,1,1),h*.045f);
        }
        void Stroke(VertexHelper mesh,float width,Color shade,float lift){
            for(int i=0;i<48;i++){
                var a=Point(i)+Vector2.up*lift;var b=Point(i+1)+Vector2.up*lift;var delta=b-a;if(delta.sqrMagnitude<.000001f)continue;var side=new Vector2(-delta.y,delta.x).normalized*width*.5f;int start=mesh.currentVertCount;
                mesh.AddVert(a-side,shade,Vector2.zero);mesh.AddVert(a+side,shade,Vector2.zero);mesh.AddVert(b+side,shade,Vector2.zero);mesh.AddVert(b-side,shade,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
    public sealed class ReferenceViewport:MonoBehaviour
    {
        Vector2 previous;
        public void Fit(){var parent=transform.parent as RectTransform;if(parent==null)return;previous=parent.rect.size;float s=Mathf.Min(previous.x/1280,previous.y/720);transform.localScale=Vector3.one*Mathf.Max(.01f,s);}
        void LateUpdate(){var parent=transform.parent as RectTransform;if(parent!=null&&parent.rect.size!=previous)Fit();}
    }
}
