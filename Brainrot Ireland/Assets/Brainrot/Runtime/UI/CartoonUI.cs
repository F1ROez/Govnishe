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
        public static void Typography(Text text,bool heading=false){text.font=Font;text.fontStyle=heading?FontStyle.Bold:FontStyle.Normal;text.color=Cream;text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Min(text.fontSize,Mathf.Max(8,text.fontSize-4));text.resizeTextMaxSize=text.fontSize;text.verticalOverflow=VerticalWrapMode.Truncate;var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.19f,.095f,.035f,.95f);outline.effectDistance=new Vector2(1.5f,-1.5f);var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.09f,.07f,.04f,.6f);shadow.effectDistance=new Vector2(0,-3);}
        public static void StyleButton(Button button,bool primary=false){
            var image=button.GetComponent<Image>();image.sprite=TropicalUIArt.Get(primary?"GoldButton":"WoodButton");image.type=image.sprite.border.sqrMagnitude>.01f?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;image.color=Color.white;image.raycastTarget=true;image.pixelsPerUnitMultiplier=1.15f;
            var shadow=button.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-4);shadow.effectColor=new Color(.11f,.065f,.02f,.45f);
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.86f);colors.pressedColor=new Color(.82f,.76f,.66f);colors.disabledColor=new Color(.48f,.47f,.43f,.86f);colors.fadeDuration=.1f;button.colors=colors;
            button.onClick.AddListener(GameMusic.PlayButtonClick);
            button.gameObject.AddComponent<CartoonButtonMotion>();EnsureTouchTarget(button);
        }
        public static void MarkSelected(Button button){
            var image=button.GetComponent<Image>();image.sprite=TropicalUIArt.Get("BlueButton");image.color=Color.white;
            image.type=image.sprite.border.sqrMagnitude>.01f?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.86f,1,1);button.colors=colors;
        }
        /// <summary>A real UGUI slider: the complete track accepts clicks, with a generous round gold handle.</summary>
        public static Slider GlossySlider(string name,Transform parent,Vector2 min,Vector2 max,float minimum,float maximum,float value,UnityEngine.Events.UnityAction<float> changed){
            var outer=Image(name,parent,min,max,new Color(1,.67f,.025f));outer.raycastTarget=true;
            Image("Верхний блик рамки",outer.transform,new Vector2(.006f,.16f),new Vector2(.994f,.96f),new Color(1,.96f,.55f));
            var track=Image("Синий желоб",outer.transform,new Vector2(.008f,.16f),new Vector2(.992f,.84f),new Color(.015f,.11f,.25f));
            var fill=Image("Бирюзовая шкала",track.transform,Vector2.zero,Vector2.one,new Color(0,.79f,1));
            Image("Блик шкалы",fill.transform,new Vector2(.018f,.58f),new Vector2(.982f,.94f),new Color(.64f,1,1,.8f));
            var handleArea=new GameObject("Область ручки",typeof(RectTransform));handleArea.transform.SetParent(outer.transform,false);
            var area=(RectTransform)handleArea.transform;area.anchorMin=new Vector2(.015f,0);area.anchorMax=new Vector2(.985f,1);area.offsetMin=area.offsetMax=Vector2.zero;
            var handle=Image("Золотая ручка",area,new Vector2(0,.5f),new Vector2(0,.5f),new Color(.63f,.26f,.01f),Disc);
            handle.rectTransform.sizeDelta=new Vector2(34,34);handle.raycastTarget=true;
            Image("Ободок ручки",handle.transform,new Vector2(.055f,.07f),new Vector2(.945f,.96f),new Color(1,.9f,.35f),Disc);
            Image("Лицевая сторона ручки",handle.transform,new Vector2(.14f,.16f),new Vector2(.86f,.89f),new Color(1,.60f,0),Disc);
            Image("Солнечный блик",handle.transform,new Vector2(.24f,.57f),new Vector2(.76f,.82f),new Color(1,1,.68f,.75f));
            var slider=outer.gameObject.AddComponent<Slider>();slider.minValue=minimum;slider.maxValue=maximum;slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=value;
            var colors=slider.colors;colors.highlightedColor=new Color(1,1,.8f);colors.pressedColor=new Color(1,.88f,.45f);colors.fadeDuration=.08f;slider.colors=colors;
            slider.onValueChanged.AddListener(changed);return slider;
        }
        public static void EnsureTouchTarget(Button button){if(Application.isMobilePlatform&&button.GetComponent<CartoonMinimumTouchTarget>()==null)button.gameObject.AddComponent<CartoonMinimumTouchTarget>();}
    }
    /// <summary>Small desktop controls retain a physical finger target on mobile screens.</summary>
    public sealed class CartoonMinimumTouchTarget:MonoBehaviour
    {
        RectTransform rect;Canvas canvas;
        void Awake(){rect=(RectTransform)transform;canvas=GetComponentInParent<Canvas>();}
        void LateUpdate(){if(canvas==null)return;float minimum=52/Mathf.Max(.1f,canvas.scaleFactor);if(rect.rect.width<minimum)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,minimum);if(rect.rect.height<minimum)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,minimum);}
    }
    /// <summary>Unscaled spring motion belongs to the visual child; the pointer hitbox stays still.</summary>
    public sealed class CartoonButtonMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler,ISubmitHandler
    {
        public Transform VisualTarget;
        public Color BaseColor=Color.white;
        public Graphic HoverGlow;
        bool hover,pressed,selected,pointerHeld;
        Button button;Transform captured;RectTransform visualRect;Graphic face,glow;
        Vector3 restScale=Vector3.one,restPosition;
        Vector2 restAnchor;
        float scaleX=1,scaleY=1,velocityX,velocityY,lift,liftVelocity,submitRemaining,glowAmount,glowAlpha=1;
        void Awake(){button=GetComponent<Button>();}
        bool Interactive=>button!=null&&button.IsActive()&&button.IsInteractable();
        void Capture(){
            var visual=VisualTarget!=null?VisualTarget:transform;
            if(captured==visual)return;
            RestoreVisual();captured=visual;visualRect=visual as RectTransform;
            restScale=visual.localScale;restPosition=visual.localPosition;
            if(visualRect!=null)restAnchor=visualRect.anchoredPosition;
            scaleX=scaleY=1;velocityX=velocityY=lift=liftVelocity=0;
        }
        public void Tick(float delta){
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||button==null)return;
            Capture();bool active=Interactive;
            if(!active){hover=pressed=selected=pointerHeld=false;submitRemaining=0;}
            float dt=Mathf.Min(.1f,delta);
            bool down=active&&(pressed||submitRemaining>0),raised=active&&(hover||selected);
            submitRemaining=Mathf.Max(0,submitRemaining-dt);
            float targetX=down?.985f:raised?1.06f:1,targetY=down?.945f:raised?1.06f:1;
            float targetLift=down?-3.5f:raised?1.5f:0;
            // Substeps keep a responsive, lightly damped spring stable after a dropped frame.
            int count=Mathf.Max(1,Mathf.CeilToInt(dt*120));float step=dt/count;
            for(int i=0;i<count;i++){
                Spring(ref scaleX,ref velocityX,targetX,down?1000:480,down?48:32,step);
                Spring(ref scaleY,ref velocityY,targetY,down?1000:480,down?48:32,step);
                Spring(ref lift,ref liftVelocity,targetLift,down?1000:480,down?48:32,step);
            }
            captured.localScale=new Vector3(restScale.x*scaleX,restScale.y*scaleY,restScale.z);
            if(visualRect!=null)visualRect.anchoredPosition=restAnchor+Vector2.up*lift;
            else captured.localPosition=restPosition+Vector3.up*lift;
            if(face!=button.targetGraphic)face=button.targetGraphic;
            if(face!=null&&VisualTarget!=null)face.color=active?BaseColor:BaseColor*new Color(.64f,.64f,.64f,.90f);
            if(glow!=HoverGlow){glow=HoverGlow;if(glow!=null)glowAlpha=glow.color.a;}
            glowAmount=Mathf.MoveTowards(glowAmount,active&&(hover||selected)&&!down?1:0,dt*9);
            if(glow!=null){var color=glow.color;color.a=glowAlpha*glowAmount;glow.color=color;}
        }
        static void Spring(ref float value,ref float velocity,float target,float stiffness,float damping,float dt){
            velocity+=((target-value)*stiffness-velocity*damping)*dt;value+=velocity*dt;
            if(Mathf.Abs(value-target)<.00005f&&Mathf.Abs(velocity)<.0002f){value=target;velocity=0;}
        }
        void Update(){Tick(Time.unscaledDeltaTime);}
        public void OnPointerEnter(PointerEventData data){if(!Interactive)return;hover=true;pressed=pointerHeld;}
        public void OnPointerExit(PointerEventData data){hover=pressed=false;}
        public void OnPointerDown(PointerEventData data){
            if(!Interactive||(data!=null&&data.button!=PointerEventData.InputButton.Left))return;
            pointerHeld=pressed=true;selected=false;
        }
        public void OnPointerUp(PointerEventData data){
            if(data!=null&&data.button!=PointerEventData.InputButton.Left)return;
            bool release=pressed&&Interactive;pressed=pointerHeld=false;
            if(release){velocityY+=.35f;liftVelocity+=12;}
        }
        public void OnSelect(BaseEventData data){selected=!(data is PointerEventData);}
        public void OnDeselect(BaseEventData data){selected=pressed=pointerHeld=false;}
        public void OnSubmit(BaseEventData data){if(Interactive){selected=true;submitRemaining=.10f;}}
        void RestoreVisual(){
            if(captured==null)return;captured.localScale=restScale;
            if(visualRect!=null)visualRect.anchoredPosition=restAnchor;else captured.localPosition=restPosition;
        }
        void OnDisable(){
            hover=pressed=selected=pointerHeld=false;submitRemaining=glowAmount=0;
            scaleX=scaleY=1;velocityX=velocityY=lift=liftVelocity=0;RestoreVisual();
            if(glow!=null){var color=glow.color;color.a=0;glow.color=color;}
        }
    }
    /// <summary>Uses Image.fillAmount as its value while preserving nine-sliced rounded caps.</summary>
    public sealed class CartoonBarFill:MonoBehaviour
    {
        Image image;Graphic[] decoration;RectTransform rect;float left,right,shown=1;
        void Awake(){image=GetComponent<Image>();decoration=GetComponentsInChildren<Graphic>();rect=(RectTransform)transform;left=rect.anchorMin.x;right=rect.anchorMax.x;}
        void LateUpdate(){float target=Mathf.Clamp01(image.fillAmount);shown=Mathf.MoveTowards(shown,target,Time.unscaledDeltaTime*4);var maximum=rect.anchorMax;float x=Mathf.Lerp(left,right,shown);if(!Mathf.Approximately(maximum.x,x)){maximum.x=x;rect.anchorMax=maximum;}bool visible=shown>.001f;if(image.enabled!=visible)image.enabled=visible;foreach(var graphic in decoration)if(graphic!=image&&graphic.enabled!=visible)graphic.enabled=visible;}
    }

}
