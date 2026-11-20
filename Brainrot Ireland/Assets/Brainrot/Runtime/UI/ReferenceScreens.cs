using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace Brainrot
{
    /// <summary>Live minimalist controls on a consistent 16:9 artboard.</summary>
    public static class ReferenceScreens
    {
        public static readonly Color Cocoa=new Color(.20f,.12f,.065f);
        public static readonly Color Accent=new Color(.035f,.55f,.57f);
        public static readonly Color Paper=new Color(1,.965f,.865f);
        static readonly Color Cream=new Color(1,.98f,.90f);
        public static RectTransform Viewport(Transform parent,string name){
            var go=new GameObject(name,typeof(RectTransform),typeof(ReferenceViewport));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1280,720);
            go.GetComponent<ReferenceViewport>().Fit();return rect;
        }
        public static void Rect(RectTransform rect,float x,float y,float w,float h){
            rect.anchorMin=new Vector2(x,1-y-h);rect.anchorMax=new Vector2(x+w,1-y);rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        public static RectTransform Group(Transform parent,string name,float x,float y,float w,float h){
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;Rect(rect,x,y,w,h);return rect;
        }
        public static Image Art(Transform parent,string key,float x,float y,float w,float h){
            var image=TropicalUIArt.Place(key,parent,Vector2.zero,Vector2.one,key);Rect(image.rectTransform,x,y,w,h);image.type=Image.Type.Simple;return image;
        }
        public static Image SlicedArt(Transform parent,string key,float x,float y,float w,float h){
            Color color=key=="IslandSliderTrack"?new Color(.17f,.26f,.28f):key=="IslandSliderFill"||key=="IslandTeal"||key=="IslandPrimary"?Accent:new Color(.89f,.91f,.88f);
            return Surface(parent,key,x,y,w,h,color);
        }
        static void ConfigureSlice(Image image){
            if(image.sprite==null||image.sprite.border.sqrMagnitude<=0){image.type=Image.Type.Simple;return;}
            image.type=Image.Type.Sliced;
            // Keep a short pill's rounded end caps circular instead of stretching its rim sideways.
            float height=image.rectTransform.rect.height;
            if(height>0)image.pixelsPerUnitMultiplier=Mathf.Max(1.35f,(image.sprite.border.y+image.sprite.border.w)/height);
        }
        public static Text Text(Transform parent,string value,int size,float x,float y,float w,float h){
            var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Rect((RectTransform)go.transform,x,y,w,h);
            var label=go.GetComponent<Text>();label.text=value;label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            CartoonUI.Typography(label,false);label.color=Cream;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.resizeTextMinSize=12;
            label.GetComponent<Outline>().enabled=false;DropShadow(label).enabled=false;return label;
        }
        public static Button Hotspot(Transform parent,string title,float x,float y,float w,float h,UnityAction action,int size=28){
            return SeaButton(parent,title,x,y,w,h,action,size,true);
        }
        public static Image Surface(Transform parent,string name,float x,float y,float w,float h,Color color){
            var image=CartoonUI.Image(name,parent,Vector2.zero,Vector2.one,color);Rect(image.rectTransform,x,y,w,h);image.sprite=IslandThemeSurface.Round;image.type=Image.Type.Sliced;return image;
        }
        public static Text SeaText(Transform parent,string value,int size,float x,float y,float w,float h){
            var text=Text(parent,value,size,x,y,w,h);SetTextStyle(text,false);return text;
        }
        static Shadow DropShadow(Text text){
            // Outline derives from Shadow; GetComponent<Shadow>() selects the outline first.
            foreach(var effect in text.GetComponents<Shadow>())if(effect.GetType()==typeof(Shadow))return effect;
            return text.gameObject.AddComponent<Shadow>();
        }
        static void SetTextStyle(Text text,bool primary){
            text.color=primary?Cream:Cocoa;text.GetComponent<Outline>().enabled=false;DropShadow(text).enabled=false;
        }
        public static Transform Panel(Transform viewport,string title,float x,float y,float w,float h,int titleSize=42,float headerTop=-.17f){
            var body=Group(viewport,title+" — панель",x,y,w,h);
            body.gameObject.AddComponent<IslandPanelEntrance>();
            Surface(body,"Тень панели",0,.012f,1,1,new Color(.015f,.04f,.05f,.27f));
            Surface(body,"Морская кромка",0,0,1,1,new Color(.10f,.44f,.46f));
            var paper=Surface(body,"Панель",.004f,.004f,.992f,.992f,Paper);paper.sprite=IslandThemeSurface.Cream;
            Surface(body,"Тёплая линия заголовка",.07f,.105f,.86f,.004f,new Color(.64f,.39f,.16f,.35f));
            var heading=SeaText(body,title,Mathf.Min(titleSize,32),.07f,.018f,.86f,.09f);heading.fontStyle=FontStyle.Bold;heading.alignment=TextAnchor.MiddleLeft;
            return body;
        }
        public static Button SeaButton(Transform parent,string title,float x,float y,float w,float h,UnityAction action,int size=22,bool primary=false,IslandSymbol? symbol=null,bool waves=true){
            var hit=Surface(parent,title,x,y,w,h,Color.clear);hit.raycastTarget=true;
            var shadow=Surface(hit.transform,"Тень кнопки",0,.04f,1,1,new Color(.12f,.075f,.03f,.22f));
            var rim=Surface(hit.transform,"Песочный кант",0,0,1,1,primary?new Color(.89f,.66f,.28f):new Color(.72f,.60f,.39f));
            var face=Surface(rim.transform,"Поверхность кнопки",.004f,.025f,.992f,.95f,primary?Accent:new Color(.98f,.88f,.67f));
            var label=SeaText(face.transform,title,size,symbol.HasValue?.20f:.06f,.12f,symbol.HasValue?.60f:.88f,.76f);label.resizeTextMinSize=12;SetTextStyle(label,primary);
            if(symbol.HasValue)IslandMenuSymbol.Place(face.transform,symbol.Value,.065f,.28f,.09f,.44f,primary?Cream:Cocoa);
            var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.None;
            var motion=hit.gameObject.AddComponent<CartoonButtonMotion>();motion.VisualTarget=rim.transform;motion.BaseColor=face.color;
            button.onClick.AddListener(GameMusic.PlayButtonClick);button.onClick.AddListener(action);CartoonUI.EnsureTouchTarget(button);return button;
        }
        public static void SetButtonStyle(Button button,bool primary,bool waves=false){
            button.targetGraphic.color=primary?Accent:new Color(.98f,.88f,.67f);
            var motion=button.GetComponent<CartoonButtonMotion>();if(motion!=null)motion.BaseColor=button.targetGraphic.color;
            var label=button.GetComponentInChildren<Text>();if(label!=null)SetTextStyle(label,primary);
        }
        public static Text Wallet(Transform parent,float x,float y,float w,float h,bool prefix=false){
            var body=SlicedArt(parent,"IslandSecondary",x,y,w,h).transform;

            var icon=Art(body,"IslandCoin",.055f,.12f,.16f,.76f);icon.preserveAspect=true;
            int coins=GameManager.Instance!=null?GameManager.Instance.RunBalance:0;return SeaText(body,prefix?"МОНЕТЫ "+coins:coins.ToString(),24,.235f,.12f,.71f,.76f);
        }
        public static Button[] MainMenu(Transform root,UnityAction play,UnityAction shop,UnityAction records,UnityAction settings,UnityAction exit,out Text wallet,out Text notice,bool background=false){
            if(background)Art(root,"IslandMenuScene",0,0,1,1);
            Surface(root,"Затемнение фона",0,0,1,1,new Color(.015f,.06f,.08f,.34f));
            BuildLogo(root);
            Text(root,"Выдержи волну. Вернись сильнее.",21,.055f,.81f,.48f,.08f).alignment=TextAnchor.MiddleLeft;
            var dock=Surface(root,"Меню",.595f,.11f,.35f,.78f,Paper).transform;
            dock.GetComponent<Image>().sprite=IslandThemeSurface.Cream;
            Surface(dock,"Морской акцент",.07f,.112f,.86f,.004f,Accent);
            wallet=SeaText(dock,"ЛУЧШАЯ ВОЛНА · "+SaveSystem.Data.BestWave,16,.07f,.045f,.86f,.055f);
            var buttons=new[]{
                SeaButton(dock,"ИГРАТЬ",.065f,.14f,.87f,.18f,play,34,true,IslandSymbol.Play),
                SeaButton(dock,"СКИНЫ",.065f,.365f,.87f,.12f,shop,25,false,IslandSymbol.Shop),
                SeaButton(dock,"РЕКОРДЫ",.065f,.505f,.87f,.12f,records,25,false,IslandSymbol.Trophy),
                SeaButton(dock,"НАСТРОЙКИ",.065f,.645f,.87f,.12f,settings,25,false,IslandSymbol.Settings),
                SeaButton(dock,"ВЫХОД",.065f,.785f,.87f,.12f,exit,25,false,IslandSymbol.Exit)
            };
            notice=SeaText(dock,"",13,.07f,.927f,.86f,.05f);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);return buttons;
        }
        public static void BuildLogo(Transform root){
            var group=Group(root,"Логотип игры",.045f,.055f,.505f,.255f);
            Surface(group,"Тень логотипа",.01f,.045f,.98f,.94f,new Color(.035f,.08f,.06f,.36f));
            Surface(group,"Песочная окантовка",0,0,1,.94f,new Color(.94f,.70f,.30f));
            var timber=Surface(group,"Древесный знак",.009f,.016f,.982f,.91f,Color.white);timber.sprite=IslandThemeSurface.Wood;
            var top=Text(timber.transform,"ОСТРОВ",26,.08f,.055f,.84f,.25f);top.fontStyle=FontStyle.Bold;top.color=new Color(.75f,.96f,.91f);
            var title=Text(timber.transform,"ТУН ТУН САХУРА",42,.045f,.30f,.91f,.48f);title.fontStyle=FontStyle.Bold;title.color=new Color(1,.96f,.77f);
            var outline=title.GetComponent<Outline>();outline.enabled=true;outline.effectDistance=new Vector2(1.3f,-1.3f);outline.effectColor=new Color(.15f,.07f,.025f,.9f);
            Surface(timber.transform,"Бирюзовая волна знака",.20f,.83f,.60f,.025f,new Color(.12f,.78f,.79f));
            IslandMenuSymbol.Place(timber.transform,IslandSymbol.Play,.075f,.785f,.045f,.13f,new Color(.99f,.79f,.32f));
        }
        public static void Pause(Transform viewport,UnityAction resume,UnityAction settings,UnityAction menu){
            var frame=Panel(viewport,"ПАУЗА",.21f,.17f,.58f,.785f,58,-.205f);
            var gm=GameManager.Instance;
            var status=SlicedArt(frame,"IslandSecondary",.17f,.095f,.66f,.085f).transform;
            SeaText(status,gm!=null?$"Волна {gm.Wave} · Побеждено {gm.Kills}":"ОСТРОВ МОЖЕТ ПОДОЖДАТЬ",22,.035f,.12f,.93f,.76f);
            var first=SeaButton(frame,"ПРОДОЛЖИТЬ",.065f,.235f,.87f,.21f,resume,45,true,IslandSymbol.Play,false);
            SeaButton(frame,"НАСТРОЙКИ",.09f,.475f,.82f,.16f,settings,38,false,IslandSymbol.Settings);
            SeaButton(frame,"ЗАВЕРШИТЬ ЗАБЕГ",.09f,.665f,.82f,.16f,menu,32,false,IslandSymbol.Exit);
            SeaText(frame,"При выходе монеты и улучшения забега пропадут.",17,.09f,.865f,.82f,.065f);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        public static void Settings(Transform viewport,UnityAction back){
            var frame=Panel(viewport,"НАСТРОЙКИ",.2125f,.105f,.575f,.87f,43);
            Slider(frame,"ГРОМКОСТЬ",.115f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;GameMusic.ApplyVolume();});
            Slider(frame,"ЧУВСТВИТЕЛЬНОСТЬ МЫШИ",.245f,.2f,3,SaveSystem.Data.Sensitivity,v=>SaveSystem.Data.Sensitivity=v);
            var player=GameManager.Instance!=null?GameManager.Instance.Player:null;bool locked=player!=null?player.CameraLocked:SaveSystem.Data.CameraLocked;
            var camera=ToggleRow(frame,"ФИКСАЦИЯ КАМЕРЫ",IslandSymbol.Camera,.39f,locked,v=>{
                locked=v;if(player!=null)player.SetCameraLock(v);else{SaveSystem.Data.CameraLocked=v;SaveSystem.Data.HasCameraPreference=true;SaveSystem.Save();}
            });
            ToggleRow(frame,"КОНТУР УДАРА",IslandSymbol.Target,.49f,SaveSystem.Data.ShowAttackRange,v=>{SaveSystem.Data.ShowAttackRange=v;SaveSystem.Save();});
            ToggleRow(frame,"ТРЯСКА КАМЕРЫ",IslandSymbol.Shake,.59f,SaveSystem.Data.CameraShakeStrength>.01f,v=>{SaveSystem.Data.CameraShakeStrength=v?1:0;SaveSystem.Save();});
            SeaText(frame,"КАЧЕСТВО ГРАФИКИ",23,.20f,.705f,.60f,.045f);


            var buttons=new Button[3];string[] titles={"НИЗКОЕ","СРЕДНЕЕ","ВЫСОКОЕ"};
            for(int i=0;i<3;i++){
                int level=i;
                buttons[i]=SeaButton(frame,titles[i],.085f+i*.285f,.766f,.26f,.09f,()=>{
                    SaveSystem.Data.Quality=level;GameQuality.Apply(level);SaveSystem.Save();
                    for(int j=0;j<buttons.Length;j++)SetButtonStyle(buttons[j],j==level);
                },23,SaveSystem.Data.Quality==i,null,false);
            }
            SeaButton(frame,"СБРОСИТЬ ПРОГРЕСС",.065f,.93f,.24f,.047f,()=>ConfirmReset(viewport,back,frame),11);
            SeaButton(frame,"НАЗАД",.36f,.885f,.57f,.09f,back,30,true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(camera.gameObject);
        }
        static void ConfirmReset(Transform viewport,UnityAction back,Transform settings){
            var previousFocus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            var settingsGroup=settings.GetComponent<CanvasGroup>();if(settingsGroup==null)settingsGroup=settings.gameObject.AddComponent<CanvasGroup>();
            settingsGroup.interactable=false;settingsGroup.blocksRaycasts=false;
            var shade=Surface(viewport,"Подтверждение сброса",0,0,1,1,new Color(.015f,.06f,.09f,.72f));shade.raycastTarget=true;
            var dialog=Panel(shade.transform,"СБРОС ПРОГРЕССА",.27f,.30f,.46f,.45f,29);
            var message=SeaText(dialog,"Текущий забег, рекорды и обучение будут сброшены. Настройки сохранятся.",20,.10f,.19f,.80f,.24f);
            SeaButton(dialog,"ПОДТВЕРДИТЬ СБРОС",.08f,.49f,.84f,.18f,()=>{
                var game=GameManager.Instance;if(game!=null)game.EndRun();
                if(!SaveSystem.ResetProgress()){message.text="Не удалось сохранить сброс. Попробуй ещё раз.";return;}
                shade.gameObject.SetActive(false);Object.Destroy(shade.gameObject);
                if(game!=null)game.MainMenu();else back();
            },25,true);
            var cancel=SeaButton(dialog,"ОТМЕНА",.20f,.72f,.60f,.15f,()=>{settingsGroup.interactable=true;settingsGroup.blocksRaycasts=true;shade.gameObject.SetActive(false);Object.Destroy(shade.gameObject);if(EventSystem.current!=null&&previousFocus!=null&&previousFocus.activeInHierarchy)EventSystem.current.SetSelectedGameObject(previousFocus);},23);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(cancel.gameObject);
        }
        static Button ToggleRow(Transform frame,string title,IslandSymbol icon,float top,bool initial,UnityAction<bool> change){
            bool value=initial;
            var button=SeaButton(frame,title,.07f,top,.86f,.09f,()=>{},21,false,null,false);
            var face=button.targetGraphic.transform;var label=button.GetComponentInChildren<Text>();Rect(label.rectTransform,.15f,.13f,.58f,.74f);label.alignment=TextAnchor.MiddleLeft;
            IslandMenuSymbol.Place(face,icon,.045f,.22f,.065f,.56f,Cocoa);
            var state=Surface(face,"Переключатель",.81f,.24f,.13f,.52f,value?Accent:new Color(.60f,.67f,.65f));
            var knob=CartoonUI.Image("Ручка переключателя",state.transform,Vector2.zero,Vector2.one,Cream,CartoonUI.Disc);
            Rect(knob.rectTransform,value?.57f:.06f,.10f,.37f,.80f);
            var motion=state.gameObject.AddComponent<IslandToggleMotion>();motion.Initialize(knob.rectTransform,state,value);
            button.onClick.AddListener(()=>{value=!value;change(value);motion.Set(value);});return button;
        }
        static void Slider(Transform frame,string title,float top,float min,float max,float value,UnityAction<float> change){
            var label=SeaText(frame,title,24,.09f,top,.69f,.048f);label.alignment=TextAnchor.MiddleLeft;
            var badge=SlicedArt(frame,"IslandSecondary",.81f,top-.002f,.105f,.055f).transform;
            var number=SeaText(badge,"",21,.03f,.09f,.94f,.82f);
            bool percent=Mathf.Approximately(max,1);
            UnityAction<float> display=v=>number.text=percent?Mathf.RoundToInt(v*100)+"%":v.ToString("0.0",System.Globalization.CultureInfo.GetCultureInfo("ru-RU"))+"×";
            display(value);
            var hit=Surface(frame,title+" — ползунок",.085f,top+.048f,.83f,.067f,Color.clear);hit.raycastTarget=true;
            SlicedArt(hit.transform,"IslandSliderTrack",0,.19f,1,.62f);
            var fillArea=Group(hit.transform,"Область заполнения",.014f,.26f,.972f,.43f);
            var fill=SlicedArt(fillArea,"IslandSliderFill",0,0,1,1);
            var handleArea=Group(hit.transform,"Область ручки",.014f,0,.972f,1);
            var handle=Surface(handleArea,"Ручка ползунка",0,0,0,1,Cream);handle.sprite=CartoonUI.Disc;handle.rectTransform.anchorMin=new Vector2(0,.5f);handle.rectTransform.anchorMax=new Vector2(0,.5f);handle.rectTransform.sizeDelta=new Vector2(28,28);handle.rectTransform.anchoredPosition=Vector2.zero;handle.raycastTarget=false;
            var slider=hit.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.SetValueWithoutNotify(value);
            var persistence=hit.gameObject.AddComponent<IslandSliderPersistence>();
            slider.onValueChanged.AddListener(v=>{display(v);change(v);persistence.MarkDirty();});
            slider.navigation=new Navigation{mode=Navigation.Mode.Automatic};
        }
        public static void Records(Transform viewport,UnityAction back){
            var frame=Panel(viewport,"РЕКОРДЫ",.225f,.16f,.55f,.81f,46);
            var time=SlicedArt(frame,"IslandSecondary",.12f,.145f,.76f,.225f).transform;
            SeaText(time,"ЛУЧШЕЕ ВРЕМЯ",28,.12f,.12f,.76f,.24f);
            var data=SaveSystem.Data;
            SeaText(time,$"{Mathf.FloorToInt(data.BestTime/60):00}:{Mathf.FloorToInt(data.BestTime%60):00}",62,.20f,.31f,.60f,.61f);
            var kills=SlicedArt(frame,"IslandSecondary",.15f,.405f,.70f,.105f).transform;
            SeaText(kills,"ПОБЕЖДЕНО: "+data.BestKills,31,.035f,.14f,.93f,.72f);
            var wave=SlicedArt(frame,"IslandSecondary",.15f,.535f,.70f,.105f).transform;
            SeaText(wave,"ЛУЧШАЯ ВОЛНА: "+data.BestWave,31,.035f,.14f,.93f,.72f);
            Button replay=null;replay=SeaButton(frame,"ПОВТОРИТЬ ОБУЧЕНИЕ",.12f,.675f,.76f,.125f,()=>{
                SaveSystem.Data.TutorialCompleted=false;SaveSystem.Save();if(GameManager.Instance!=null&&GameManager.Instance.Goals!=null)GameManager.Instance.Goals.ReplayTutorial();replay.GetComponentInChildren<Text>().text="ОБУЧЕНИЕ ВКЛЮЧЕНО";
            },27);
            var backButton=SeaButton(frame,"НАЗАД",.19f,.83f,.62f,.12f,back,34,true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }
    }
    /// <summary>Short unscaled panel arrival; no coroutine, layout rebuild or allocation after construction.</summary>
    public sealed class IslandPanelEntrance:MonoBehaviour
    {
        CanvasGroup group;Vector3 baseline;float age;
        void Awake(){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();baseline=transform.localScale;group.alpha=0;transform.localScale=baseline*.98f;}
        void Update(){
            age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/.18f),ease=1-(1-t)*(1-t)*(1-t);
            group.alpha=ease;transform.localScale=baseline*Mathf.Lerp(.98f,1,ease);
            if(t>=1){group.alpha=1;transform.localScale=baseline;enabled=false;}
        }
    }
    /// <summary>A switch thumb follows its state with a quick, interruption-safe eased slide.</summary>
    public sealed class IslandToggleMotion:MonoBehaviour
    {
        RectTransform knob;Image track;float position,target;
        public void Initialize(RectTransform thumb,Image background,bool value){knob=thumb;track=background;position=target=value?1:0;Apply();enabled=false;}
        public void Set(bool value){target=value?1:0;enabled=true;}
        public void Tick(float dt){
            if(knob==null||track==null)return;position=Mathf.MoveTowards(position,target,Mathf.Max(0,dt)*7);Apply();
            if(Mathf.Approximately(position,target))enabled=false;
        }
        void Update(){Tick(Time.unscaledDeltaTime);}
        void Apply(){
            float ease=position*position*(3-2*position);ReferenceScreens.Rect(knob,Mathf.Lerp(.06f,.57f,ease),.10f,.37f,.80f);
            track.color=Color.Lerp(new Color(.60f,.67f,.65f),ReferenceScreens.Accent,ease);
        }
    }
    /// <summary>Write slider changes after a short idle interval or at drag/page end, rather than once per mouse pixel.</summary>
    public sealed class IslandSliderPersistence:MonoBehaviour,IPointerUpHandler,IEndDragHandler
    {
        bool dirty;float quiet;
        public void MarkDirty(){dirty=true;quiet=0;}
        void Update(){if(dirty){quiet+=Time.unscaledDeltaTime;if(quiet>=.35f)Flush();}}
        public void Flush(){if(!dirty)return;dirty=false;SaveSystem.Save();}
        public void OnPointerUp(PointerEventData data){Flush();}
        public void OnEndDrag(PointerEventData data){Flush();}
        void OnDisable(){Flush();}
        void OnApplicationPause(bool paused){if(paused)Flush();}
        void OnApplicationQuit(){Flush();}
    }
    /// <summary>Low-opacity rays behind the face, revealed only by pointer or keyboard focus.</summary>
    public sealed class IslandButtonGlow:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh){
            mesh.Clear();var rect=rectTransform.rect;var center=rect.center;
            const int count=48;
            for(int i=0;i<count;i++){
                float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
                var innerA=center+new Vector2(Mathf.Cos(a)*rect.width*.32f,Mathf.Sin(a)*rect.height*.28f);
                var innerB=center+new Vector2(Mathf.Cos(b)*rect.width*.32f,Mathf.Sin(b)*rect.height*.28f);
                var outerA=center+new Vector2(Mathf.Cos(a)*rect.width*.5f,Mathf.Sin(a)*rect.height*.5f);
                var outerB=center+new Vector2(Mathf.Cos(b)*rect.width*.5f,Mathf.Sin(b)*rect.height*.5f);
                Color clear=color;clear.a=0;int start=mesh.currentVertCount;
                mesh.AddVert(innerA,color,Vector2.zero);mesh.AddVert(innerB,color,Vector2.zero);mesh.AddVert(outerB,clear,Vector2.zero);mesh.AddVert(outerA,clear,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
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
