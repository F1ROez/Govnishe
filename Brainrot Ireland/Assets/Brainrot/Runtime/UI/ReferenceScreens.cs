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
        static readonly Color Sea = new Color(.065f,.22f,.27f);
        static readonly Color Mint = new Color(.32f,.73f,.68f);
        static readonly Color Paper = new Color(.94f,.96f,.88f);
        public static Image Surface(Transform parent,string name,float x,float y,float w,float h,Color color){
            var image=CartoonUI.Image(name,parent,Vector2.zero,Vector2.one,color);Rect(image.rectTransform,x,y,w,h);return image;
        }
        static Text SeaText(Transform parent,string value,int size,float x,float y,float w,float h){
            var text=Text(parent,value,size,x,y,w,h);text.color=Paper;
            text.GetComponent<Outline>().effectColor=new Color(.025f,.10f,.13f,.65f);text.GetComponent<Outline>().effectDistance=new Vector2(.7f,-.7f);
            text.GetComponent<Shadow>().effectColor=Color.clear;return text;
        }
        static Transform Panel(Transform viewport,string title,float x,float y,float w,float h){
            Surface(viewport,"Мягкая тень",x,y+.012f,w,h,new Color(.01f,.06f,.09f,.35f));
            var rim=Surface(viewport,"Морская рамка",x,y,w,h,new Color(.30f,.53f,.53f));
            var body=Surface(rim.transform,"Матовая панель",.003f,.004f,.994f,.992f,Sea).transform;
            Surface(body,"Акцент",.075f,.035f,.12f,.008f,Mint);
            SeaText(body,title,32,.08f,.065f,.84f,.10f);
            Surface(body,"Разделитель",.08f,.19f,.84f,.002f,new Color(.5f,.76f,.74f,.25f));
            var leaf=Art(body,"LeafAccent",.86f,-.035f,.17f,.15f);leaf.preserveAspect=true;leaf.color=new Color(.65f,.83f,.72f);
            return body;
        }
        static Button SeaButton(Transform parent,string title,float x,float y,float w,float h,UnityAction action,int size=22,bool primary=false){
            var hit=Surface(parent,title,x,y,w,h,Color.clear);hit.raycastTarget=true;
            var face=Surface(hit.transform,"Кнопка",0,0,1,1,primary?new Color(.16f,.48f,.45f):new Color(.12f,.32f,.37f));
            Surface(face.transform,"Мягкий верхний край",.04f,.035f,.92f,.018f,new Color(.68f,.90f,.84f,.22f));
            SeaText(face.transform,title,size,.04f,.12f,.92f,.76f);
            var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.None;
            var motion=hit.gameObject.AddComponent<CartoonButtonMotion>();motion.VisualTarget=face.transform;motion.BaseColor=face.color;
            button.onClick.AddListener(GameMusic.PlayButtonClick);button.onClick.AddListener(action);CartoonUI.EnsureTouchTarget(button);return button;
        }
        public static void Pause(Transform viewport,UnityAction resume,UnityAction settings,UnityAction menu){
            var frame=Panel(viewport,"ПАУЗА",.3125f,.20f,.375f,.60f);
            var gm=GameManager.Instance;
            SeaText(frame,gm!=null?$"Волна {gm.Wave}  ·  Побеждено {gm.Kills}":"ОСТРОВ МОЖЕТ ПОДОЖДАТЬ",16,.08f,.22f,.84f,.06f);
            var first=SeaButton(frame,"ПРОДОЛЖИТЬ",.10f,.34f,.80f,.14f,resume,24,true);
            SeaButton(frame,"НАСТРОЙКИ",.10f,.515f,.80f,.14f,settings);
            SeaButton(frame,"ГЛАВНОЕ МЕНЮ",.10f,.69f,.80f,.14f,menu);
            SeaText(frame,"ESC — вернуться к игре",13,.1f,.88f,.8f,.05f);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        public static void Settings(Transform viewport,UnityAction back){
            var frame=Panel(viewport,"НАСТРОЙКИ",.275f,.105f,.45f,.79f);
            Slider(frame,"ГРОМКОСТЬ",.225f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;GameMusic.ApplyVolume();SaveSystem.Save();});
            Slider(frame,"ЧУВСТВИТЕЛЬНОСТЬ МЫШИ",.355f,.2f,3,SaveSystem.Data.Sensitivity,v=>{SaveSystem.Data.Sensitivity=v;SaveSystem.Save();});
            var player=GameManager.Instance!=null?GameManager.Instance.Player:null;bool locked=player!=null?player.CameraLocked:SaveSystem.Data.CameraLocked;
            Button camera=null,range=null,shake=null;
            camera=SeaButton(frame,"ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ"),.08f,.485f,.84f,.075f,()=>{
                locked=!locked;if(player!=null)player.SetCameraLock(locked);else{SaveSystem.Data.CameraLocked=locked;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();}
                camera.GetComponentInChildren<Text>().text="ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ");
            },18);
            range=SeaButton(frame,"КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ"),.08f,.575f,.84f,.075f,()=>{
                SaveSystem.Data.ShowAttackRange=!SaveSystem.Data.ShowAttackRange;SaveSystem.Save();range.GetComponentInChildren<Text>().text="КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ");
            },18);
            bool shaking=SaveSystem.Data.CameraShakeStrength>.01f;
            shake=SeaButton(frame,"ТРЯСКА КАМЕРЫ: "+(shaking?"ВКЛ":"ВЫКЛ"),.08f,.665f,.84f,.075f,()=>{
                shaking=!shaking;SaveSystem.Data.CameraShakeStrength=shaking?1:0;SaveSystem.Save();
                shake.GetComponentInChildren<Text>().text="ТРЯСКА КАМЕРЫ: "+(shaking?"ВКЛ":"ВЫКЛ");
            },18);
            SeaText(frame,"КАЧЕСТВО ГРАФИКИ",15,.08f,.758f,.84f,.035f);
            var buttons=new Button[3];string[] titles={"НИЗКОЕ","СРЕДНЕЕ","ВЫСОКОЕ"};
            for(int i=0;i<3;i++){
                int level=i;
                buttons[i]=SeaButton(frame,titles[i],.08f+i*.285f,.805f,.27f,.065f,()=>{
                    SaveSystem.Data.Quality=level;GameQuality.Apply(level);SaveSystem.Save();
                    for(int j=0;j<buttons.Length;j++)MarkQuality(buttons[j],j==level,titles[j]);
                },16);
                MarkQuality(buttons[i],SaveSystem.Data.Quality==i,titles[i]);
            }
            SeaButton(frame,"НАЗАД",.29f,.90f,.42f,.075f,back,20,true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(camera.gameObject);
        }
        static void MarkQuality(Button button,bool selected,string title){
            button.targetGraphic.color=selected?new Color(.16f,.48f,.45f):new Color(.12f,.32f,.37f);
            button.GetComponent<CartoonButtonMotion>().BaseColor=button.targetGraphic.color;
            button.GetComponentInChildren<Text>().text=(selected?"• ":"")+title;
        }
        static void Flourish(Transform parent,float x,float y,bool flip){
            var go=new GameObject("Морской завиток",typeof(RectTransform),typeof(WaveFlourish));go.transform.SetParent(parent,false);Rect((RectTransform)go.transform,x,y,.085f,.045f);go.GetComponent<WaveFlourish>().raycastTarget=false;if(flip)go.transform.localScale=new Vector3(-1,1,1);
        }
        static void Slider(Transform frame,string title,float top,float min,float max,float value,UnityAction<float> action){
            var label=SeaText(frame,title,17,.08f,top,.68f,.035f);label.alignment=TextAnchor.MiddleLeft;
            var number=SeaText(frame,"",16,.77f,top,.15f,.035f);
            bool percent=Mathf.Approximately(max,1);
            number.text=percent?Mathf.RoundToInt(value*100)+"%":value.ToString("0.0")+"×";
            var hit=Surface(frame,title+" — ползунок",.08f,top+.04f,.84f,.055f,Color.clear);hit.raycastTarget=true;
            var track=Surface(hit.transform,"Желоб",0,.36f,1,.28f,new Color(.035f,.13f,.18f));
            var fill=Surface(track.transform,"Заполнение",0,0,1,1,Mint);
            var handle=CartoonUI.Image("Ручка",hit.transform,new Vector2(0,.5f),new Vector2(0,.5f),Paper,CartoonUI.Disc);
            handle.rectTransform.sizeDelta=new Vector2(22,22);handle.raycastTarget=true;
            var slider=hit.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=value;
            slider.onValueChanged.AddListener(v=>{number.text=percent?Mathf.RoundToInt(v*100)+"%":v.ToString("0.0")+"×";action(v);});
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
