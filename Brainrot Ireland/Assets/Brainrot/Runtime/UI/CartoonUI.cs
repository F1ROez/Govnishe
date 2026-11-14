using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Brainrot
{
    /// <summary>Original runtime UI art. All textures are generated once and shared across scenes.</summary>
    public static class CartoonUI
    {
        public static readonly Color Ink=new Color(.24f,.12f,.045f);
        public static readonly Color Cream=new Color(1,.96f,.82f);
        public static readonly Color Panel=new Color(.12f,.43f,.88f,.96f);
        public static readonly Color Aqua=new Color(.08f,.94f,.91f);
        public static readonly Color Gold=new Color(1,.84f,.12f);
        public static readonly Color Coral=new Color(1,.23f,.42f);
        static Sprite rounded,disc,dash,heavy,heart,bolt;
        static Font font;
        public static Font Font {
            get {
                if(font==null){font=Resources.Load<Font>("IslandRounded");if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(!font.HasCharacter('Я')||!font.HasCharacter('ё')){var russian=UnityEngine.Font.CreateDynamicFontFromOSFont(new[]{"Arial","Segoe UI","DejaVu Sans"},20);if(russian!=null)font=russian;}}
                return font;
            }
        }
        public static Sprite Rounded=>rounded!=null?rounded:rounded=MakeRounded(false);
        public static Sprite Disc=>disc!=null?disc:disc=MakeRounded(true);
        public static Sprite Dash=>dash!=null?dash:dash=Icon(0);
        public static Sprite Heavy=>heavy!=null?heavy:heavy=Icon(1);
        public static Sprite Heart=>heart!=null?heart:heart=Icon(2);
        public static Sprite Bolt=>bolt!=null?bolt:bolt=Icon(3);
        static Sprite MakeRounded(bool circle){
            const int size=64;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.name=circle?"UI — круг":"UI — мягкий угол";texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];float radius=circle?31:13;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                var p=new Vector2(Mathf.Abs(x-31.5f),Mathf.Abs(y-31.5f));float distance=circle?p.magnitude-30.5f:new Vector2(Mathf.Max(0,p.x-(31-radius)),Mathf.Max(0,p.y-(31-radius))).magnitude-radius;
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(.5f-distance));
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,circle?Vector4.zero:new Vector4(15,15,15,15));
        }
        static float Line(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);return (p-a-d*t).magnitude;}
        static bool Polygon(Vector2 p,Vector2[] points){bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++){var a=points[i];var b=points[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}return inside;}
        static Sprite Icon(int kind){
            const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.name="UI — иконка "+kind;texture.wrapMode=TextureWrapMode.Clamp;var pixels=new Color[size*size];
            Vector2[] lightning={new Vector2(.59f,.94f),new Vector2(.23f,.48f),new Vector2(.46f,.48f),new Vector2(.38f,.08f),new Vector2(.81f,.61f),new Vector2(.55f,.61f)};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                Color total=Color.clear;
                for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++){
                    var p=new Vector2((x+(sx+.5f)/2)/size,(y+(sy+.5f)/2)/size);Color c=Color.clear;
                    if(kind==0){
                        bool outline=Line(p,new Vector2(.39f,.73f),new Vector2(.64f,.5f))<.092f||Line(p,new Vector2(.39f,.27f),new Vector2(.64f,.5f))<.092f||Line(p,new Vector2(.68f,.73f),new Vector2(.93f,.5f))<.092f||Line(p,new Vector2(.68f,.27f),new Vector2(.93f,.5f))<.092f;
                        bool face=Line(p,new Vector2(.39f,.74f),new Vector2(.64f,.51f))<.057f||Line(p,new Vector2(.39f,.28f),new Vector2(.64f,.51f))<.057f||Line(p,new Vector2(.68f,.74f),new Vector2(.93f,.51f))<.057f||Line(p,new Vector2(.68f,.28f),new Vector2(.93f,.51f))<.057f;
                        if(outline)c=Ink;if(face)c=Color.Lerp(new Color(.16f,.72f,.85f),new Color(.77f,1,.91f),p.y);
                        if(Line(p,new Vector2(.05f,.4f),new Vector2(.26f,.4f))<.021f||Line(p,new Vector2(.13f,.61f),new Vector2(.29f,.61f))<.021f)c=Cream;
                    }else if(kind==1){
                        float bat=Line(p,new Vector2(.43f,.43f),new Vector2(.7f,.77f));float handle=Line(p,new Vector2(.23f,.18f),new Vector2(.48f,.49f));
                        if(bat<.137f||handle<.066f)c=Ink;
                        if(bat<.108f)c=Color.Lerp(new Color(.94f,.47f,.17f),new Color(1,.89f,.46f),p.y);
                        if(handle<.039f)c=Cream;
                        if(Line(p,new Vector2(.49f,.58f),new Vector2(.66f,.79f))<.017f)c=new Color(1,.96f,.69f);
                        if(Line(p,new Vector2(.2f,.71f),new Vector2(.29f,.67f))<.024f||Line(p,new Vector2(.81f,.4f),new Vector2(.88f,.35f))<.024f||Line(p,new Vector2(.77f,.93f),new Vector2(.81f,.99f))<.024f)c=Cream;
                    }else if(kind==2){
                        var q=(p-new Vector2(.5f,.46f))*2.4f;float a=q.x*q.x+q.y*q.y- .55f;
                        if(a*a*a-q.x*q.x*q.y*q.y*q.y<0)c=Color.Lerp(Coral,new Color(1,.74f,.68f),p.y);
                    }else if(Polygon(p,lightning))c=Color.Lerp(new Color(1,.64f,.19f),new Color(1,.95f,.6f),p.y);
                    total+=c*.25f;
                }
                pixels[y*size+x]=total;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);
        }
        public static Image Image(string name,Transform parent,Vector2 min,Vector2 max,Color color,Sprite sprite=null){
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>();image.sprite=sprite!=null?sprite:Rounded;image.type=sprite==null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;image.color=color;image.raycastTarget=false;return image;
        }
        public static void Typography(Text text,bool heading=false){text.font=Font;text.fontStyle=heading?FontStyle.Bold:FontStyle.Normal;text.color=Cream;text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Max(11,text.fontSize-5);text.resizeTextMaxSize=text.fontSize;text.verticalOverflow=VerticalWrapMode.Truncate;var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.19f,.095f,.035f,.95f);outline.effectDistance=new Vector2(1.5f,-1.5f);var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.09f,.07f,.04f,.6f);shadow.effectDistance=new Vector2(0,-3);}
        public static void StyleButton(Button button,bool primary=false){
            var image=button.GetComponent<Image>();image.sprite=TropicalUIArt.Get(primary?"WoodButtonGold":"WoodButton");image.type=image.sprite.border.sqrMagnitude>.01f?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;image.color=Color.white;image.raycastTarget=true;
            var shadow=button.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-4);shadow.effectColor=new Color(.11f,.065f,.02f,.45f);
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.86f);colors.pressedColor=new Color(.82f,.76f,.66f);colors.disabledColor=new Color(.48f,.47f,.43f,.86f);colors.fadeDuration=.1f;button.colors=colors;
            button.onClick.AddListener(GameMusic.PlayButtonClick);
            button.gameObject.AddComponent<CartoonButtonMotion>();
        }
    }
    public sealed class CartoonButtonMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        bool hover,pressed;Button button;
        void Awake(){button=GetComponent<Button>();}
        void Update(){float target=button!=null&&button.interactable? (pressed ? .96f : hover ? 1.025f : 1) : 1;transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*target,1-Mathf.Exp(-22*Time.unscaledDeltaTime));}
        public void OnPointerEnter(PointerEventData data){hover=true;}
        public void OnPointerExit(PointerEventData data){hover=false;pressed=false;}
        public void OnPointerDown(PointerEventData data){pressed=true;}
        public void OnPointerUp(PointerEventData data){pressed=false;}
        void OnDisable(){hover=pressed=false;transform.localScale=Vector3.one;}
    }
    /// <summary>Uses Image.fillAmount as its value while preserving nine-sliced rounded caps.</summary>
    public sealed class CartoonBarFill:MonoBehaviour
    {
        Image image;Graphic[] decoration;RectTransform rect;float left,right,shown=1;
        void Awake(){image=GetComponent<Image>();decoration=GetComponentsInChildren<Graphic>();rect=(RectTransform)transform;left=rect.anchorMin.x;right=rect.anchorMax.x;}
        void LateUpdate(){float target=Mathf.Clamp01(image.fillAmount);shown=Mathf.MoveTowards(shown,target,Time.unscaledDeltaTime*4);var maximum=rect.anchorMax;maximum.x=Mathf.Lerp(left,right,shown);rect.anchorMax=maximum;image.enabled=shown>.001f;foreach(var graphic in decoration)if(graphic!=image)graphic.enabled=image.enabled;}
    }

}
