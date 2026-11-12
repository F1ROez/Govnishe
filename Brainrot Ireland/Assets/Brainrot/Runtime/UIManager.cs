using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Brainrot
{
    public sealed class UIManager : MonoBehaviour
    {
        Font font;
        Sprite solid;
        Canvas canvas;
        GameObject hud, overlay, mobile, boss;
        Text stats, healthText, xpText, notification, bossText;
        Image healthFill, xpFill, bossFill;
        GameState shownState = (GameState)(-1);
        int shownLevel = -1;
        string page = "";
        readonly Color ink = new Color(.07f, .14f, .2f);
        readonly Color gold = new Color(1f, .8f, .26f);
        readonly Color cyan = new Color(.17f, .84f, .85f);

        public void Initialize()
        {
            if (canvas != null) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var pixel = new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
            solid = Sprite.Create(pixel,new Rect(0,0,1,1),new Vector2(.5f,.5f));
            var root = new GameObject("Brainrot UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = root.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280, 720); scale.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null) {
                var events=new GameObject("Event System",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }
            hud = Box("HUD", root.transform, Vector2.zero, Vector2.one, Color.clear);
            var plate = Box("Vitals", hud.transform, new Vector2(.025f,.79f),new Vector2(.34f,.965f),new Color(.03f,.1f,.16f,.85f));
            healthText = Label(plate.transform,"HP",23,TextAnchor.MiddleLeft,new Vector2(.06f,.62f),new Vector2(.94f,.97f));
            healthFill = Bar(plate.transform, new Vector2(.06f,.48f),new Vector2(.94f,.6f),new Color(1,.33f,.37f));
            xpText = Label(plate.transform,"LEVEL 1",18,TextAnchor.MiddleLeft,new Vector2(.06f,.17f),new Vector2(.94f,.46f));
            xpFill = Bar(plate.transform,new Vector2(.06f,.08f),new Vector2(.94f,.16f),cyan);
            stats=Label(hud.transform,"",24,TextAnchor.UpperCenter,new Vector2(.35f,.84f),new Vector2(.8f,.96f));
            Button(hud.transform,"II",new Vector2(.91f,.88f),new Vector2(.975f,.97f),()=>GameManager.Instance.Pause());
            notification=Label(hud.transform,"",30,TextAnchor.MiddleCenter,new Vector2(.15f,.68f),new Vector2(.85f,.81f)); notification.color=gold;
            Label(hud.transform,"WASD  move   •   Mouse  look   •   LMB  swing   •   Shift  sprint   •   Space  dash",17,TextAnchor.LowerCenter,new Vector2(.1f,.02f),new Vector2(.9f,.07f));
            boss=Box("Boss",hud.transform,new Vector2(.28f,.73f),new Vector2(.72f,.82f),new Color(.03f,.08f,.13f,.88f));
            bossText=Label(boss.transform,"KING TRALALA",20,TextAnchor.MiddleCenter,new Vector2(0,.35f),Vector2.one);
            bossFill=Bar(boss.transform,new Vector2(.04f,.12f),new Vector2(.96f,.32f),new Color(.85f,.25f,.9f));
            overlay=Box("Screens",root.transform,Vector2.zero,Vector2.one,new Color(.015f,.055f,.1f,.91f));
            BuildMobile(root.transform);
            Refresh();
        }

        public void Refresh()
        {
            if (canvas == null || GameManager.Instance == null) return;
            var gm=GameManager.Instance;
            hud.SetActive(gm.State==GameState.Playing || gm.State==GameState.Paused || gm.State==GameState.Upgrade);
            mobile.SetActive(Application.isMobilePlatform && gm.State==GameState.Playing);
            overlay.SetActive(gm.State!=GameState.Playing);
            if (gm.Player != null && gm.Upgrades != null)
            {
                healthText.text=$"HEALTH   {Mathf.CeilToInt(gm.Player.Health)} / {Mathf.CeilToInt(gm.Player.Stats.MaxHealth)}";
                healthFill.fillAmount=Mathf.Clamp01(gm.Player.Health/Mathf.Max(1,gm.Player.Stats.MaxHealth));
                xpText.text=$"LEVEL {gm.Upgrades.Level}   ·   {gm.Upgrades.Experience} / {gm.Upgrades.RequiredExperience} XP";
                xpFill.fillAmount=Mathf.Clamp01((float)gm.Upgrades.Experience/Mathf.Max(1,gm.Upgrades.RequiredExperience));
            }
            stats.text=$"{TimeLabel(gm.Elapsed)}   |   {gm.Kills} KILLS\n<size=18>WAVE {gm.Wave}</size>";
            notification.text=gm.Notification ?? "";
            EnemyController king=null;
            if(gm.Spawner != null) foreach(var enemy in gm.Spawner.Active) if(enemy != null && enemy.Alive && enemy.IsBoss) {king=enemy;break;}
            boss.SetActive(king!=null);
            if(king!=null) {bossFill.fillAmount=king.Health/Mathf.Max(1,king.MaxHealth);bossText.text=$"{(king.Kind==EnemyKind.King?"KING TRALALA":"MEGA TRALALA")}   {Mathf.CeilToInt(king.Health)}";}
            int level=gm.Upgrades!=null?gm.Upgrades.Level:0;
            if(shownState!=gm.State || (gm.State==GameState.Upgrade && shownLevel!=level))
            {
                shownState=gm.State;shownLevel=level;page="";
                MobileInput.Clear();
                if(gm.State!=GameState.Playing) BuildScreen();
            }
        }

        void ClearScreen()
        {
            foreach(Transform child in overlay.transform) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
        }
        void BuildScreen()
        {
            ClearScreen();var gm=GameManager.Instance;
            if(gm.State==GameState.Menu)
            {
                Label(overlay.transform,"AN ENDLESS ISLAND SURVIVAL",19,TextAnchor.MiddleCenter,new Vector2(.1f,.82f),new Vector2(.9f,.9f)).color=cyan;
                Label(overlay.transform,"BRAINROT\nIRELAND",64,TextAnchor.MiddleCenter,new Vector2(.1f,.56f),new Vector2(.9f,.83f)).color=gold;
                Label(overlay.transform,"One wooden bat. An ocean full of sharks.",23,TextAnchor.MiddleCenter,new Vector2(.1f,.47f),new Vector2(.9f,.55f));
                MenuButton("PLAY",.36f,()=>gm.StartRun());MenuButton("RECORDS",.26f,ShowRecords);MenuButton("SETTINGS",.16f,ShowSettings);MenuButton("QUIT",.06f,()=>gm.Quit());
            }
            else if(gm.State==GameState.Paused)
            {
                Title("TAKE A BREATHER");MenuButton("RESUME",.49f,()=>gm.Resume());MenuButton("SETTINGS",.37f,ShowSettings);MenuButton("MAIN MENU",.25f,()=>gm.MainMenu());
            }
            else if(gm.State==GameState.Dead)
            {
                Title("THE OCEAN WON");
                Label(overlay.transform,$"SURVIVED  {TimeLabel(gm.Elapsed)}     •     KILLS  {gm.Kills}     •     LEVEL  {gm.Upgrades.Level}\n\nPERSONAL BEST   {TimeLabel(SaveSystem.Data.BestTime)}   /   {SaveSystem.Data.BestKills} KILLS",25,TextAnchor.MiddleCenter,new Vector2(.12f,.42f),new Vector2(.88f,.65f));
                MenuButton("ONE MORE RUN",.25f,()=>gm.StartRun());MenuButton("MAIN MENU",.13f,()=>gm.MainMenu());
            }
            else if(gm.State==GameState.Upgrade)
            {
                Title("LEVEL UP!");Label(overlay.transform,"CHOOSE YOUR NEXT BAD IDEA",23,TextAnchor.MiddleCenter,new Vector2(.1f,.66f),new Vector2(.9f,.74f)).color=cyan;
                var choices=gm.Upgrades.Choices;
                for(int i=0;i<choices.Count && i<3;i++)
                {
                    int index=i;var def=choices[i];float left=.055f+i*.31f;
                    var card=Box("Upgrade",overlay.transform,new Vector2(left,.23f),new Vector2(left+.28f,.61f),new Color(.08f,.2f,.27f,.98f));
                    Label(card.transform,def.Rarity.ToString().ToUpperInvariant(),17,TextAnchor.MiddleCenter,new Vector2(.06f,.83f),new Vector2(.94f,.96f)).color=gold;
                    Label(card.transform,def.Title,28,TextAnchor.MiddleCenter,new Vector2(.06f,.57f),new Vector2(.94f,.84f));
                    Label(card.transform,def.Description,19,TextAnchor.MiddleCenter,new Vector2(.08f,.23f),new Vector2(.92f,.59f));
                    Button(card.transform,$"TAKE  ·  {gm.Upgrades.Rank(def)+1}/{def.MaxRank}",new Vector2(.08f,.06f),new Vector2(.92f,.22f),()=>{gm.Upgrades.Choose(index);if(gm.State==GameState.Upgrade)BuildScreen();});
                }
            }
        }
        void Title(string title) {Label(overlay.transform,title,46,TextAnchor.MiddleCenter,new Vector2(.06f,.75f),new Vector2(.94f,.9f)).color=gold;}
        void MenuButton(string title,float y,UnityEngine.Events.UnityAction action) {Button(overlay.transform,title,new Vector2(.35f,y),new Vector2(.65f,y+.08f),action);}
        void ShowRecords()
        {
            page="records";ClearScreen();Title("ISLAND LEGENDS");var d=SaveSystem.Data;
            Label(overlay.transform,$"LONGEST SURVIVAL    {TimeLabel(d.BestTime)}\n\nMOST SHARKS DEFEATED    {d.BestKills}\n\nHIGHEST LEVEL    {d.BestLevel}",29,TextAnchor.MiddleCenter,new Vector2(.1f,.33f),new Vector2(.9f,.7f));MenuButton("BACK",.16f,BuildScreen);
        }
        void ShowSettings()
        {
            page="settings";ClearScreen();Title("SETTINGS");
            SettingSlider("VOLUME",.62f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;AudioListener.volume=v;SaveSystem.Save();});
            SettingSlider("MOUSE SENSITIVITY",.47f,.2f,3,SaveSystem.Data.Sensitivity,v=>{SaveSystem.Data.Sensitivity=v;SaveSystem.Save();});
            Button(overlay.transform,"QUALITY: LOW",new Vector2(.2f,.29f),new Vector2(.39f,.37f),()=>Quality(0));
            Button(overlay.transform,"MEDIUM",new Vector2(.405f,.29f),new Vector2(.595f,.37f),()=>Quality(1));
            Button(overlay.transform,"HIGH",new Vector2(.61f,.29f),new Vector2(.8f,.37f),()=>Quality(2));
            MenuButton("BACK",.13f,BuildScreen);
        }
        void Quality(int quality) {SaveSystem.Data.Quality=quality;QualitySettings.SetQualityLevel(Mathf.Min(quality,QualitySettings.names.Length-1),true);SaveSystem.Save();}
        void SettingSlider(string title,float y,float min,float max,float value,UnityEngine.Events.UnityAction<float> action)
        {
            Label(overlay.transform,title,22,TextAnchor.MiddleLeft,new Vector2(.25f,y+.04f),new Vector2(.75f,y+.1f));
            var root=Box(title,overlay.transform,new Vector2(.25f,y),new Vector2(.75f,y+.04f),new Color(.12f,.25f,.32f));
            var slider=root.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;
            var fill=Box("Fill",root.transform,Vector2.zero,Vector2.one,cyan);slider.fillRect=fill.GetComponent<RectTransform>();
            var handle=Box("Handle",root.transform,Vector2.zero,new Vector2(.03f,1),gold);slider.handleRect=handle.GetComponent<RectTransform>();slider.targetGraphic=handle.GetComponent<Image>();slider.value=value;slider.onValueChanged.AddListener(action);
        }
        void BuildMobile(Transform root)
        {
            mobile=Box("Touch Controls",root,Vector2.zero,Vector2.one,Color.clear);
            var look=Box("Look",mobile.transform,new Vector2(.45f,.12f),new Vector2(1,.76f),new Color(0,0,0,.001f));look.AddComponent<MobileLookArea>();
            var stick=Box("Move",mobile.transform,new Vector2(.035f,.12f),new Vector2(.23f,.4f),new Color(.1f,.3f,.4f,.48f));stick.AddComponent<MobileJoystick>();Label(stick.transform,"MOVE",21,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one);
            var attack=Button(mobile.transform,"SWING",new Vector2(.82f,.11f),new Vector2(.97f,.29f),()=>{});attack.gameObject.AddComponent<MobileHoldButton>().Action=0;
            var sprint=Button(mobile.transform,"RUN",new Vector2(.27f,.12f),new Vector2(.38f,.23f),()=>{});sprint.gameObject.AddComponent<MobileHoldButton>().Action=1;
            Button(mobile.transform,"DASH",new Vector2(.68f,.12f),new Vector2(.8f,.23f),()=>MobileInput.Dash=true);
        }
        GameObject Box(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=obj.GetComponent<Image>();image.color=color;image.sprite=solid;image.raycastTarget=color.a>0;return obj;
        }
        Text Label(Transform parent,string text,int size,TextAnchor alignment,Vector2 min,Vector2 max)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var label=obj.GetComponent<Text>();label.font=font;label.text=text;label.fontSize=size;label.alignment=alignment;label.color=Color.white;label.raycastTarget=false;label.supportRichText=true;label.horizontalOverflow=HorizontalWrapMode.Wrap;return label;
        }
        Button Button(Transform parent,string title,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {
            var obj=Box(title,parent,min,max,cyan);var button=obj.AddComponent<Button>();button.targetGraphic=obj.GetComponent<Image>();var colors=button.colors;colors.highlightedColor=new Color(1,.91f,.55f);colors.pressedColor=new Color(.6f,.76f,.77f);button.colors=colors;button.onClick.AddListener(action);
            Label(obj.transform,title,22,TextAnchor.MiddleCenter,new Vector2(.03f,0),new Vector2(.97f,1)).color=ink;return button;
        }
        Image Bar(Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var back=Box("Bar",parent,min,max,new Color(.015f,.055f,.085f));var fill=Box("Fill",back.transform,Vector2.zero,Vector2.one,color).GetComponent<Image>();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.raycastTarget=false;return fill;
        }
        static string TimeLabel(float seconds) {int n=Mathf.Max(0,Mathf.FloorToInt(seconds));return $"{n/60:00}:{n%60:00}";}
    }
}
