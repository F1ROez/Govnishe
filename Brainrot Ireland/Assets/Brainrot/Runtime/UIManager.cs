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
        GameObject hud, overlay, mobile, fullMap;
        bool mapExpanded;
        Text stats, healthText, xpText, notification, aimHint, hitMarker, hitLabel,chargeLabel;
        float hitUntil;
        bool lastHitCritical;
        Image healthFill, xpFill;
        Image heavyShade,dashShade;Text heavyTime,dashTime;
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
            if(!font.HasCharacter('Я')||!font.HasCharacter('ё')) {
                var russian=Font.CreateDynamicFontFromOSFont(new[]{"Arial","Segoe UI","DejaVu Sans"},18);
                if(russian!=null)font=russian;
            }
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
            healthText = Label(plate.transform,"ЗДОРОВЬЕ",23,TextAnchor.MiddleLeft,new Vector2(.06f,.62f),new Vector2(.94f,.97f));
            healthFill = Bar(plate.transform, new Vector2(.06f,.48f),new Vector2(.94f,.6f),new Color(1,.33f,.37f));
            xpText = Label(plate.transform,"УРОВЕНЬ 1",18,TextAnchor.MiddleLeft,new Vector2(.06f,.17f),new Vector2(.94f,.46f));
            xpFill = Bar(plate.transform,new Vector2(.06f,.08f),new Vector2(.94f,.16f),cyan);
            stats=Label(hud.transform,"",24,TextAnchor.UpperCenter,new Vector2(.35f,.84f),new Vector2(.8f,.96f));
            Button(hud.transform,"II",new Vector2(.92f,.935f),new Vector2(.975f,.99f),()=>GameManager.Instance.Pause());
            notification=Label(hud.transform,"",30,TextAnchor.MiddleCenter,new Vector2(.15f,.68f),new Vector2(.85f,.81f)); notification.color=gold;
            Label(hud.transform,"WASD — движение  •  Мышь — камера  •  ЛКМ — удар; удержать и отпустить — сильный  •  Shift — бег  •  Пробел — рывок  •  V — фиксация",16,TextAnchor.LowerCenter,new Vector2(.08f,.02f),new Vector2(.92f,.07f));
            aimHint=Label(hud.transform,"",16,TextAnchor.LowerCenter,new Vector2(.32f,.07f),new Vector2(.68f,.11f));
            Label(hud.transform,"+",18,TextAnchor.MiddleCenter,new Vector2(.49f,.49f),new Vector2(.51f,.53f)).color=new Color(1,1,1,.5f);
            hitMarker=Label(hud.transform,"",36,TextAnchor.MiddleCenter,new Vector2(.47f,.46f),new Vector2(.53f,.56f));
            chargeLabel=Label(hud.transform,"",19,TextAnchor.MiddleCenter,new Vector2(.2f,.32f),new Vector2(.8f,.39f));chargeLabel.color=gold;
            hitLabel=Label(hud.transform,"",17,TextAnchor.MiddleCenter,new Vector2(.4f,.41f),new Vector2(.6f,.46f));

            BuildAbilityIcons();
            overlay=Box("Screens",root.transform,Vector2.zero,Vector2.one,new Color(.015f,.055f,.1f,.91f));
            BuildMobile(root.transform);
            BuildFullMap(root.transform);
            Refresh();
        }

        public void Refresh()
        {
            if (canvas == null || GameManager.Instance == null) return;
            var gm=GameManager.Instance;
            hud.SetActive(gm.State==GameState.Playing || gm.State==GameState.Paused || gm.State==GameState.Upgrade || gm.State==GameState.Map);
            mobile.SetActive(Application.isMobilePlatform && gm.State==GameState.Playing);
            overlay.SetActive(gm.State!=GameState.Playing&&gm.State!=GameState.Map);
            if (gm.Player != null && gm.Upgrades != null)
            {
                healthText.text=$"ЗДОРОВЬЕ   {Mathf.CeilToInt(gm.Player.Health)} / {Mathf.CeilToInt(gm.Player.Stats.MaxHealth)}";
                healthFill.fillAmount=Mathf.Clamp01(gm.Player.Health/Mathf.Max(1,gm.Player.Stats.MaxHealth));
                xpText.text=$"УРОВЕНЬ {gm.Upgrades.Level}   ·   {gm.Upgrades.Experience} / {gm.Upgrades.RequiredExperience} ОПЫТА";
                xpFill.fillAmount=Mathf.Clamp01((float)gm.Upgrades.Experience/Mathf.Max(1,gm.Upgrades.RequiredExperience));
            }
            xpText.transform.parent.gameObject.SetActive(true);xpText.gameObject.SetActive(false);xpFill.transform.parent.gameObject.SetActive(false);
            stats.text=$"ВОЛНА {gm.Wave} · {gm.Spawner.Waves.Stage} · {Mathf.CeilToInt(gm.Spawner.Waves.Remaining)} с\n{TimeLabel(gm.Elapsed)}  |  Побеждено: {gm.Kills}";
            notification.text=gm.Notification ?? "";
            if(gm.Player!=null){UpdateAbility(heavyShade,heavyTime,gm.Player.Combat.HeavyCooldownRemaining,gm.Player.Combat.HeavyCooldownDuration);UpdateAbility(dashShade,dashTime,gm.Player.DashCooldownRemaining,gm.Player.DashCooldownDuration);}
            var combat=gm.Player!=null?gm.Player.Combat:null;
            chargeLabel.text=combat!=null&&combat.IsCharging&&!combat.HeavyReady?"СИЛЬНЫЙ УДАР ВОССТАНАВЛИВАЕТСЯ — ОБЫЧНАЯ АТАКА":combat!=null&&combat.IsCharging?(combat.ChargeProgress>=1?"СИЛЬНЫЙ УДАР ГОТОВ — ОТПУСТИ ЛКМ":"ЗАРЯД СИЛЬНОГО УДАРА: "+Mathf.RoundToInt(combat.ChargeProgress*100)+"%"):"";
            aimHint.text=gm.Player!=null && gm.Player.CameraLocked ? "ВЗГЛЯД ПО КАМЕРЕ  ·  V" : "СВОБОДНЫЙ ПОВОРОТ  ·  V";
            if(Time.unscaledTime>=hitUntil) {hitMarker.text="";hitLabel.text="";}
            else {
                float alpha=Mathf.Clamp01((hitUntil-Time.unscaledTime)/.12f);
                Color feedback=lastHitCritical?gold:Color.white;feedback.a=alpha;
                hitMarker.color=feedback;hitLabel.color=feedback;
            }
            
            mapExpanded=gm.State==GameState.Map;
            fullMap.SetActive(mapExpanded);
            int level=gm.Upgrades!=null?gm.Upgrades.Level:0;
            if(shownState!=gm.State || (gm.State==GameState.Upgrade && shownLevel!=level))
            {
                shownState=gm.State;shownLevel=level;page="";
                MobileInput.Clear();
                if(gm.State!=GameState.Playing) BuildScreen();
            }
        }

        void BuildAbilityIcons(){
            var strong=Box("Сильный удар",hud.transform,new Vector2(.025f,.60f),new Vector2(.13f,.76f),new Color(.03f,.10f,.16f,.9f));
            // Procedural bat silhouette, no external icon assets.
            var bat=Box("Бита",strong.transform,new Vector2(.41f,.32f),new Vector2(.59f,.84f),gold);bat.transform.localRotation=Quaternion.Euler(0,0,-32);
            var handle=Box("Рукоять",strong.transform,new Vector2(.45f,.22f),new Vector2(.53f,.47f),Color.white);handle.transform.localRotation=Quaternion.Euler(0,0,-32);
            heavyShade=CooldownShade(strong.transform);heavyTime=Label(strong.transform,"ГОТОВ",19,TextAnchor.MiddleCenter,new Vector2(0,.22f),new Vector2(1,.9f));
            Label(strong.transform,"СИЛЬНЫЙ",13,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(1,.23f));
            var dash=Box("Рывок",hud.transform,new Vector2(.025f,.41f),new Vector2(.13f,.57f),new Color(.03f,.10f,.16f,.9f));
            for(int j=0;j<2;j++){
                var a=Box("Стрелка",dash.transform,new Vector2(.25f+j*.22f,.50f),new Vector2(.48f+j*.22f,.57f),cyan);a.transform.localRotation=Quaternion.Euler(0,0,35);
                var b=Box("Стрелка",dash.transform,new Vector2(.25f+j*.22f,.38f),new Vector2(.48f+j*.22f,.45f),cyan);b.transform.localRotation=Quaternion.Euler(0,0,-35);
            }
            dashShade=CooldownShade(dash.transform);dashTime=Label(dash.transform,"ГОТОВ",19,TextAnchor.MiddleCenter,new Vector2(0,.22f),new Vector2(1,.9f));
            Label(dash.transform,"РЫВОК",13,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(1,.23f));
        }
        Image CooldownShade(Transform parent){var image=Box("Перезарядка",parent,Vector2.zero,Vector2.one,new Color(0,0,0,.72f)).GetComponent<Image>();image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Radial360;image.fillOrigin=2;image.fillClockwise=true;image.raycastTarget=false;return image;}
        static void UpdateAbility(Image shade,Text label,float remaining,float duration){shade.fillAmount=Mathf.Clamp01(remaining/Mathf.Max(.1f,duration));label.text=remaining>0?Mathf.CeilToInt(remaining).ToString():"ГОТОВ";label.color=remaining>0?Color.white:new Color(.55f,1,.65f);}

        void ClearScreen()
        {
            foreach(Transform child in overlay.transform) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
        }
        void BuildScreen()
        {
            ClearScreen();var gm=GameManager.Instance;
            if(gm.State==GameState.Menu)
            {
                Label(overlay.transform,"ПЯТЬ ЗОН. ОДИН ОСТРОВ. ТВОЯ ИСТОРИЯ.",19,TextAnchor.MiddleCenter,new Vector2(.1f,.82f),new Vector2(.9f,.9f)).color=cyan;
                Label(overlay.transform,"БРЕЙНРОТ\nИРЛАНДИЯ",56,TextAnchor.MiddleCenter,new Vector2(.1f,.56f),new Vector2(.9f,.83f)).color=gold;
                Label(overlay.transform,"Исследуй остров. Выбирай риск. Держи биту крепче.",23,TextAnchor.MiddleCenter,new Vector2(.1f,.47f),new Vector2(.9f,.55f));
                MenuButton("ИГРАТЬ",.36f,()=>gm.StartRun());MenuButton("РЕКОРДЫ",.26f,ShowRecords);MenuButton("НАСТРОЙКИ",.16f,ShowSettings);MenuButton("ВЫХОД",.06f,()=>gm.Quit());
            }
            else if(gm.State==GameState.Paused)
            {
                Title("ПАУЗА");MenuButton("ПРОДОЛЖИТЬ",.49f,()=>gm.Resume());MenuButton("НАСТРОЙКИ",.37f,ShowSettings);MenuButton("ГЛАВНОЕ МЕНЮ",.25f,()=>gm.MainMenu());
            }
            else if(gm.State==GameState.Dead)
            {
                Title("ЗАБЕГ ОКОНЧЕН");
                Label(overlay.transform,$"Выживание: {TimeLabel(gm.Elapsed)}     •     Побеждено: {gm.Kills}\n\nЛИЧНЫЙ РЕКОРД   {TimeLabel(SaveSystem.Data.BestTime)}   /   {SaveSystem.Data.BestKills} ПОБЕД",25,TextAnchor.MiddleCenter,new Vector2(.12f,.42f),new Vector2(.88f,.65f));
                MenuButton("ЕЩЁ ОДИН ЗАБЕГ",.25f,()=>gm.StartRun());MenuButton("ГЛАВНОЕ МЕНЮ",.13f,()=>gm.MainMenu());
            }
            else if(gm.State==GameState.Upgrade)
            {
                Title("НОВЫЙ УРОВЕНЬ!");Label(overlay.transform,"ВЫБЕРИ УЛУЧШЕНИЕ",23,TextAnchor.MiddleCenter,new Vector2(.1f,.66f),new Vector2(.9f,.74f)).color=cyan;
                var choices=gm.Upgrades.Choices;
                for(int i=0;i<choices.Count && i<3;i++)
                {
                    int index=i;var def=choices[i];float left=.055f+i*.31f;
                    var card=Box("Upgrade",overlay.transform,new Vector2(left,.23f),new Vector2(left+.28f,.61f),new Color(.08f,.2f,.27f,.98f));
                    Label(card.transform,UpgradeSystem.RarityTitle(def.Rarity),17,TextAnchor.MiddleCenter,new Vector2(.06f,.83f),new Vector2(.94f,.96f)).color=gold;
                    Label(card.transform,gm.Upgrades.DisplayTitle(def),25,TextAnchor.MiddleCenter,new Vector2(.06f,.57f),new Vector2(.94f,.84f));
                    Label(card.transform,gm.Upgrades.Describe(def),18,TextAnchor.MiddleCenter,new Vector2(.08f,.23f),new Vector2(.92f,.59f));
                    string take=gm.Upgrades.Rank(def)>=def.MaxRank?"ВОССТАНОВИТЬ ЗДОРОВЬЕ":$"ВЫБРАТЬ  ·  {gm.Upgrades.Rank(def)+1}/{def.MaxRank}";
                    Button(card.transform,take,new Vector2(.08f,.06f),new Vector2(.92f,.22f),()=>{gm.Upgrades.Choose(index);if(gm.State==GameState.Upgrade)BuildScreen();});
                }
            }
        }
        void Title(string title) {Label(overlay.transform,title,46,TextAnchor.MiddleCenter,new Vector2(.06f,.75f),new Vector2(.94f,.9f)).color=gold;}
        void MenuButton(string title,float y,UnityEngine.Events.UnityAction action) {Button(overlay.transform,title,new Vector2(.35f,y),new Vector2(.65f,y+.08f),action);}
        void ShowRecords()
        {
            page="records";ClearScreen();Title("ЛЕГЕНДЫ ОСТРОВА");var d=SaveSystem.Data;
            Label(overlay.transform,$"ВРЕМЯ ВЫЖИВАНИЯ    {TimeLabel(d.BestTime)}\n\nПОБЕЖДЕНО ВРАГОВ    {d.BestKills}",29,TextAnchor.MiddleCenter,new Vector2(.1f,.33f),new Vector2(.9f,.7f));MenuButton("НАЗАД",.16f,BuildScreen);
        }
        void ShowSettings()
        {
            page="settings";ClearScreen();Title("НАСТРОЙКИ");
            SettingSlider("ГРОМКОСТЬ",.63f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;AudioListener.volume=v;SaveSystem.Save();});
            SettingSlider("ЧУВСТВИТЕЛЬНОСТЬ МЫШИ",.49f,.2f,3,SaveSystem.Data.Sensitivity,v=>{SaveSystem.Data.Sensitivity=v;SaveSystem.Save();});
            bool locked=GameManager.Instance.Player!=null?GameManager.Instance.Player.CameraLocked:SaveSystem.Data.CameraLocked;
            Button(overlay.transform,"ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ"),new Vector2(.25f,.385f),new Vector2(.75f,.445f),()=>{
                var player=GameManager.Instance.Player;
                if(player!=null)player.SetCameraLock(!player.CameraLocked);
                else {SaveSystem.Data.CameraLocked=!SaveSystem.Data.CameraLocked;SaveSystem.Save();}
                ShowSettings();
            });
            Button(overlay.transform,"КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ"),new Vector2(.25f,.30f),new Vector2(.75f,.36f),()=>{
                SaveSystem.Data.ShowAttackRange=!SaveSystem.Data.ShowAttackRange;SaveSystem.Save();ShowSettings();
            });
            Label(overlay.transform,"КАЧЕСТВО ГРАФИКИ",16,TextAnchor.MiddleCenter,new Vector2(.2f,.265f),new Vector2(.8f,.297f));
            Button(overlay.transform,"НИЗКОЕ",new Vector2(.2f,.205f),new Vector2(.39f,.265f),()=>Quality(0));
            Button(overlay.transform,"СРЕДНЕЕ",new Vector2(.405f,.205f),new Vector2(.595f,.265f),()=>Quality(1));
            Button(overlay.transform,"ВЫСОКОЕ",new Vector2(.61f,.205f),new Vector2(.8f,.265f),()=>Quality(2));
            MenuButton("НАЗАД",.075f,BuildScreen);
        }

        public void ShowHit(bool critical,int count=1)
        {
            if(hitMarker==null)return;
            hitUntil=Time.unscaledTime+.3f;lastHitCritical=critical;
            hitMarker.text="×";hitLabel.text=(critical?"КРИТИЧЕСКИЙ УДАР":"ПОПАДАНИЕ")+(count>1?" ×"+count:"");
        }

        public void ShowMiss()
        {
            // A miss must not erase a confirmed hit from an immediately preceding echo.
            if(hitMarker==null || Time.unscaledTime<hitUntil)return;
            hitUntil=Time.unscaledTime+.13f;lastHitCritical=false;
            hitMarker.text="";hitLabel.text="ПРОМАХ";
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
            var stick=Box("Move",mobile.transform,new Vector2(.035f,.12f),new Vector2(.23f,.4f),new Color(.1f,.3f,.4f,.48f));stick.AddComponent<MobileJoystick>();Label(stick.transform,"ДВИЖЕНИЕ",21,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one);
            var attack=Button(mobile.transform,"УДАР",new Vector2(.82f,.11f),new Vector2(.97f,.29f),()=>{});attack.gameObject.AddComponent<MobileHoldButton>().Action=0;
            var sprint=Button(mobile.transform,"БЕГ",new Vector2(.27f,.12f),new Vector2(.38f,.23f),()=>{});sprint.gameObject.AddComponent<MobileHoldButton>().Action=1;
            Button(mobile.transform,"РЫВОК",new Vector2(.68f,.12f),new Vector2(.8f,.23f),()=>MobileInput.Dash=true);
            
        }
        static string ZoneHint(IslandZone zone)
        {
            switch(zone) {
                case IslandZone.Beach:return "Тралалеро Тралала · рекомендуется уровень 1–3";
                case IslandZone.Meadows:return "Лирили Ларила · рекомендуется уровень 2–5";
                case IslandZone.Forest:return "Брр Брр Патапим · рекомендуется уровень 4–8";
                case IslandZone.Ruins:return "Капучино Ассасино · рекомендуется уровень 7–12";
                default:return "Бомбардиро Крокодило · рекомендуется уровень 10+";
            }
        }
        void BuildMinimap(Transform root)
        {
            var plate=Box("Миникарта",hud.transform,new Vector2(.81f,.64f),new Vector2(.98f,.925f),new Color(.025f,.10f,.15f,.9f));
            var frame=Box("Рамка карты",plate.transform,new Vector2(.025f,.12f),new Vector2(.975f,.98f),Color.clear);
            var area=Box("Карта острова",frame.transform,Vector2.zero,Vector2.one,Color.clear);
            area.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            area.GetComponent<AspectRatioFitter>().aspectRatio=1;
            area.AddComponent<IslandMinimap>().Initialize(font,solid,false);
            Label(plate.transform,"КАРТА ОСТРОВА · M",13,TextAnchor.MiddleCenter,new Vector2(0,0),new Vector2(1,.13f)).color=gold;
        }
        void BuildFullMap(Transform root)
        {
            fullMap=Box("Карта",root,Vector2.zero,Vector2.one,new Color(.01f,.045f,.075f,.97f));
            Label(fullMap.transform,"КАРТА ОСТРОВА",35,TextAnchor.MiddleCenter,new Vector2(.1f,.87f),new Vector2(.9f,.97f)).color=gold;
            var frame=Box("Рамка большой карты",fullMap.transform,new Vector2(.23f,.17f),new Vector2(.77f,.86f),Color.clear);
            var area=Box("Большая карта",frame.transform,Vector2.zero,Vector2.one,Color.clear);
            var aspect=area.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            area.AddComponent<IslandMinimap>().Initialize(font,solid,true);
            Label(fullMap.transform,"Все зоны открыты. Чем дальше от пляжа, тем опаснее.\nБелая точка — ты; жёлтая линия — направление взгляда. Карта ставит игру на паузу.",19,TextAnchor.MiddleCenter,new Vector2(.05f,.08f),new Vector2(.95f,.17f));
            Button(fullMap.transform,"ЗАКРЫТЬ · M",new Vector2(.37f,.015f),new Vector2(.63f,.075f),ToggleMap);
            fullMap.SetActive(false);
        }
        void ToggleMap(){var gm=GameManager.Instance;if(gm.State==GameState.Map)gm.CloseMap();else if(gm.IsPlaying)gm.OpenMap();mapExpanded=gm.State==GameState.Map;if(fullMap!=null)fullMap.SetActive(mapExpanded);}
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
