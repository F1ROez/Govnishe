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
        Text waveTitle,stageText,soundMute;Text stats, healthText, xpText, notification, aimHint, hitMarker, hitLabel,chargeLabel;
        float hitUntil;
        bool lastHitCritical;
        Image healthFill, xpFill,screenShade;
        Image heavyShade,dashShade,escapeFill;Text heavyTime,dashTime;GameObject capturePanel;
        GameState shownState = (GameState)(-1);
        int shownLevel = -1;
        readonly Color ink = CartoonUI.Ink;
        readonly Color gold = CartoonUI.Gold;
        readonly Color cyan = CartoonUI.Aqua;

        public void Initialize()
        {
            if (canvas != null) return;
            font = CartoonUI.Font;
            var pixel = new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
            solid = Sprite.Create(pixel,new Rect(0,0,1,1),new Vector2(.5f,.5f));
            var root = new GameObject("Brainrot UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = root.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280, 720); scale.matchWidthOrHeight = .5f;
            UIInputRouter.EnsureForScene(gameObject.scene);
            hud = Box("HUD", root.transform, Vector2.zero, Vector2.one, Color.clear);
            var plate=TropicalUIArt.Place("Vitals",hud.transform,new Vector2(.012f,.84f),new Vector2(.252f,.99f),"VitalsFrame").gameObject;
            TropicalUIArt.Place("Портрет Сахура",plate.transform,new Vector2(.025f,.10f),new Vector2(.25f,.90f),"Portrait",true);
            healthText=Label(plate.transform,"ЗДОРОВЬЕ",16,TextAnchor.MiddleLeft,new Vector2(.29f,.65f),new Vector2(.94f,.88f));
            healthFill=Bar(plate.transform,new Vector2(.29f,.48f),new Vector2(.94f,.65f),new Color(.9f,.14f,.15f));
            xpText=Label(plate.transform,"ВЫНОСЛИВОСТЬ",12,TextAnchor.MiddleLeft,new Vector2(.29f,.27f),new Vector2(.94f,.46f));
            xpFill=Bar(plate.transform,new Vector2(.29f,.13f),new Vector2(.94f,.28f),new Color(.23f,.83f,.91f));
            var wave=TropicalUIArt.Place("Деревянная табличка волны",hud.transform,new Vector2(.385f,.913f),new Vector2(.625f,.995f),"WavePlaque").gameObject;
            waveTitle=Label(wave.transform,"",25,TextAnchor.MiddleCenter,new Vector2(.09f,.13f),new Vector2(.91f,.87f));waveTitle.color=CartoonUI.Cream;
            var stage=TropicalUIArt.Place("Лента этапа",hud.transform,new Vector2(.405f,.868f),new Vector2(.605f,.918f),"StageRibbon").gameObject;
            stageText=Label(stage.transform,"",14,TextAnchor.MiddleCenter,new Vector2(.08f,.1f),new Vector2(.92f,.9f));
            var score=TropicalUIArt.Place("Время и победы",hud.transform,new Vector2(.400f,.826f),new Vector2(.610f,.869f),"StatsStrip").gameObject;
            stats=Label(score.transform,"",13,TextAnchor.MiddleCenter,new Vector2(.06f,.12f),new Vector2(.94f,.9f));stats.color=CartoonUI.Ink;stats.GetComponent<Outline>().effectColor=new Color(1,.95f,.8f,.8f);
            var soundButton=RoundButton("Звук",new Vector2(.893f,.91f),new Vector2(.935f,.982f),"SoundIcon",()=>GameMusic.ToggleMute());
            soundMute=Label(soundButton.transform,"×",19,TextAnchor.MiddleCenter,new Vector2(.55f,.01f),new Vector2(.99f,.46f));soundMute.color=new Color(1,.35f,.25f);
            RoundButton("Пауза",new Vector2(.945f,.91f),new Vector2(.987f,.982f),"PauseIcon",()=>GameManager.Instance.Pause());
            notification=Label(hud.transform,"",25,TextAnchor.MiddleCenter,new Vector2(.20f,.68f),new Vector2(.88f,.76f));notification.color=gold;
            var controls=TropicalUIArt.Place("Подсказки управления",hud.transform,new Vector2(.20f,.012f),new Vector2(.80f,.055f),"ControlsPanel").gameObject;
            var help=Label(controls.transform,"WASD · движение    ЛКМ · удар / удержать · сильный    Shift · бег    Пробел · рывок    V · камера",12,TextAnchor.MiddleCenter,new Vector2(.04f,.14f),new Vector2(.96f,.88f));help.color=Color.white;help.GetComponent<Outline>().effectColor=new Color(.035f,.13f,.28f,.95f);
            aimHint=Label(hud.transform,"",12,TextAnchor.LowerCenter,new Vector2(.32f,.063f),new Vector2(.76f,.09f));
            Label(hud.transform,"+",18,TextAnchor.MiddleCenter,new Vector2(.49f,.49f),new Vector2(.51f,.53f)).color=new Color(1,1,1,.5f);
            hitMarker=Label(hud.transform,"",36,TextAnchor.MiddleCenter,new Vector2(.47f,.46f),new Vector2(.53f,.56f));
            chargeLabel=Label(hud.transform,"",19,TextAnchor.MiddleCenter,new Vector2(.2f,.32f),new Vector2(.8f,.39f));chargeLabel.color=gold;
            hitLabel=Label(hud.transform,"",17,TextAnchor.MiddleCenter,new Vector2(.4f,.41f),new Vector2(.6f,.46f));

            BuildAbilityIcons();
            capturePanel=TropicalUIArt.Place("Захват осьминога",hud.transform,new Vector2(.25f,.17f),new Vector2(.75f,.29f),"VitalsFrame").gameObject;
            Label(capturePanel.transform,"БЫСТРО НАЖИМАЙ ЛКМ, ЧТОБЫ ВЫБРАТЬСЯ!",20,TextAnchor.MiddleCenter,new Vector2(0,.4f),Vector2.one);
            escapeFill=Bar(capturePanel.transform,new Vector2(.06f,.13f),new Vector2(.94f,.35f),cyan);
            overlay=Box("Screens",root.transform,Vector2.zero,Vector2.one,new Color(.025f,.025f,.025f,.45f));screenShade=overlay.GetComponent<Image>();
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
            screenShade.color=new Color(.025f,.025f,.025f,gm.State==GameState.Paused?.28f:.45f);
            if (gm.Player != null && gm.Upgrades != null)
            {
                healthText.text=$"ЗДОРОВЬЕ   {Mathf.CeilToInt(gm.Player.Health)} / {Mathf.CeilToInt(gm.Player.Stats.MaxHealth)}";
                healthFill.fillAmount=Mathf.Clamp01(gm.Player.Health/Mathf.Max(1,gm.Player.Stats.MaxHealth));
                xpText.text=$"ВЫНОСЛИВОСТЬ  {Mathf.CeilToInt(gm.Player.Stamina.Value)} / {Mathf.CeilToInt(gm.Player.Stamina.Maximum)}";
                xpFill.fillAmount=gm.Player.Stamina.Value/Mathf.Max(1,gm.Player.Stamina.Maximum);
            }
            xpText.gameObject.SetActive(true);xpFill.transform.parent.gameObject.SetActive(true);
            waveTitle.text=$"ВОЛНА {gm.Wave}";stageText.text=$"{gm.Spawner.Waves.Stage} · {Mathf.CeilToInt(gm.Spawner.Waves.Remaining)} с";stats.text=$"{TimeLabel(gm.Elapsed)}   |   Побеждено: {gm.Kills}";
            capturePanel.SetActive(gm.Player!=null&&gm.Player.IsCaptured);if(gm.Player!=null)escapeFill.fillAmount=gm.Player.EscapeProgress;
            notification.text=gm.Notification ?? "";soundMute.gameObject.SetActive(GameMusic.IsMuted);
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
                shownState=gm.State;shownLevel=level;
                MobileInput.Clear();
                if(gm.State!=GameState.Playing) BuildScreen();
            }
        }

        void BuildAbilityIcons(){
            var strong=AbilityCard("Сильный удар",new Vector2(.012f,.64f),new Vector2(.087f,.825f),"HeavyCard","HeavyIcon","СИЛЬНЫЙ\nУДАР","ЛКМ · удержать");
            heavyShade=CooldownShade(strong.transform);heavyTime=ReadyBadge(strong.transform);
            var dash=AbilityCard("Рывок",new Vector2(.012f,.435f),new Vector2(.087f,.62f),"DashCard","DashIcon","РЫВОК","ПРОБЕЛ");
            dashShade=CooldownShade(dash.transform);dashTime=ReadyBadge(dash.transform);
        }
        GameObject AbilityCard(string name,Vector2 min,Vector2 max,string frame,string icon,string title,string key){
            var card=TropicalUIArt.Place(name,hud.transform,min,max,frame).gameObject;
            TropicalUIArt.Place("Иконка",card.transform,new Vector2(.13f,.31f),new Vector2(.87f,.78f),icon,true);
            Label(card.transform,title,12,TextAnchor.MiddleCenter,new Vector2(.05f,.77f),new Vector2(.95f,.96f));
            Label(card.transform,key,9,TextAnchor.MiddleCenter,new Vector2(.04f,.045f),new Vector2(.96f,.15f));return card;
        }
        Text ReadyBadge(Transform parent){var badge=TropicalUIArt.Place("Готовность",parent,new Vector2(.08f,.13f),new Vector2(.92f,.32f),"ReadyBadge");return Label(badge.transform,"ГОТОВ",13,TextAnchor.MiddleCenter,new Vector2(.04f,.08f),new Vector2(.96f,.94f));}
        Image CooldownShade(Transform parent){var image=Box("Перезарядка",parent,new Vector2(.08f,.31f),new Vector2(.92f,.79f),new Color(.10f,.10f,.10f,.62f)).GetComponent<Image>();image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Radial360;image.fillOrigin=2;image.fillClockwise=true;image.raycastTarget=false;return image;}
        static void UpdateAbility(Image shade,Text label,float remaining,float duration){shade.fillAmount=Mathf.Clamp01(remaining/Mathf.Max(.1f,duration));label.text=remaining>0?Mathf.CeilToInt(remaining)+" с":"ГОТОВ";label.color=remaining>0?new Color(1,.9f,.6f):Color.white;label.transform.parent.GetComponent<Image>().color=remaining>0?new Color(.76f,.80f,.75f):Color.white;}
        Image RoundButton(string name,Vector2 min,Vector2 max,string icon,UnityEngine.Events.UnityAction action){
            if(Application.isMobilePlatform){float right=name=="Пауза" ? .987f : .895f;min=new Vector2(right-.075f,.86f);max=new Vector2(right,.99f);}
            var face=TropicalUIArt.Place(name,hud.transform,min,max,icon);face.preserveAspect=true;face.raycastTarget=true;var button=face.gameObject.AddComponent<Button>();button.targetGraphic=face;button.onClick.AddListener(GameMusic.PlayButtonClick);button.onClick.AddListener(action);face.gameObject.AddComponent<CartoonButtonMotion>();CartoonUI.EnsureTouchTarget(button);
            // Atlas sound/pause sprites already include their circular rim; avoid a second nested button.
            return face;
        }

        void ClearScreen()
        {
            foreach(Transform child in overlay.transform) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
            TropicalUIArt.Place("Резная рамка экрана",overlay.transform,new Vector2(.265f,.065f),new Vector2(.735f,.91f),"WoodPanel");
        }
        void BuildScreen()
        {
            ClearScreen();var gm=GameManager.Instance;
            if(gm.State==GameState.Menu)
            {
                Label(overlay.transform,"МАЛЕНЬКИЙ ОСТРОВ. БОЛЬШИЕ НЕПРИЯТНОСТИ.",19,TextAnchor.MiddleCenter,new Vector2(.1f,.82f),new Vector2(.9f,.9f)).color=cyan;
                Label(overlay.transform,"БРЕЙНРОТ\nИРЛАНДИЯ",56,TextAnchor.MiddleCenter,new Vector2(.1f,.56f),new Vector2(.9f,.83f)).color=gold;
                Label(overlay.transform,"Выживай среди акул. Держи биту крепче.",23,TextAnchor.MiddleCenter,new Vector2(.1f,.47f),new Vector2(.9f,.55f));
                MenuButton("ПРОДОЛЖИТЬ ИГРУ",.39f,()=>gm.ResumeRun());MenuButton("НОВАЯ ИГРА",.30f,()=>gm.StartRun());MenuButton("РЕКОРДЫ",.21f,ShowRecords);MenuButton("НАСТРОЙКИ",.12f,ShowSettings);MenuButton("ВЫХОД",.03f,()=>gm.Quit());
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
        void Title(string title) {Label(overlay.transform,title,34,TextAnchor.MiddleCenter,new Vector2(.06f,.75f),new Vector2(.94f,.9f)).color=gold;}
        void MenuButton(string title,float y,UnityEngine.Events.UnityAction action) {Button(overlay.transform,title,new Vector2(.37f,y),new Vector2(.63f,y+(Application.isMobilePlatform ? .075f : .065f)),action);}
        void ShowRecords()
        {
            ClearScreen();Title("ЛЕГЕНДЫ ОСТРОВА");var d=SaveSystem.Data;
            Label(overlay.transform,$"ВРЕМЯ ВЫЖИВАНИЯ    {TimeLabel(d.BestTime)}\n\nПОБЕЖДЕНО ВРАГОВ    {d.BestKills}",29,TextAnchor.MiddleCenter,new Vector2(.1f,.33f),new Vector2(.9f,.7f));MenuButton("НАЗАД",.16f,BuildScreen);
        }
        void ShowSettings()
        {
            ClearScreen();Title("НАСТРОЙКИ");
            SettingSlider("ГРОМКОСТЬ",.63f,0,1,SaveSystem.Data.Volume,v=>{SaveSystem.Data.Volume=v;GameMusic.ApplyVolume();SaveSystem.Save();});
            SettingSlider("ЧУВСТВИТЕЛЬНОСТЬ МЫШИ",.49f,.2f,3,SaveSystem.Data.Sensitivity,v=>{SaveSystem.Data.Sensitivity=v;SaveSystem.Save();});
            bool locked=GameManager.Instance.Player!=null?GameManager.Instance.Player.CameraLocked:SaveSystem.Data.CameraLocked;
            Button(overlay.transform,"ФИКСАЦИЯ КАМЕРЫ: "+(locked?"ВКЛ":"ВЫКЛ"),new Vector2(.29f,.385f),new Vector2(.71f,.435f),()=>{
                var player=GameManager.Instance.Player;
                if(player!=null)player.SetCameraLock(!player.CameraLocked);
                else {SaveSystem.Data.CameraLocked=!SaveSystem.Data.CameraLocked;SaveSystem.Save();}
                ShowSettings();
            });
            Button(overlay.transform,"КОНТУР УДАРА: "+(SaveSystem.Data.ShowAttackRange?"ВКЛ":"ВЫКЛ"),new Vector2(.29f,.30f),new Vector2(.71f,.35f),()=>{
                SaveSystem.Data.ShowAttackRange=!SaveSystem.Data.ShowAttackRange;SaveSystem.Save();ShowSettings();
            });
            Label(overlay.transform,"КАЧЕСТВО ГРАФИКИ",16,TextAnchor.MiddleCenter,new Vector2(.2f,.265f),new Vector2(.8f,.297f));
            Button(overlay.transform,"НИЗКОЕ",new Vector2(.29f,.205f),new Vector2(.42f,.255f),()=>Quality(0));
            Button(overlay.transform,"СРЕДНЕЕ",new Vector2(.435f,.205f),new Vector2(.565f,.255f),()=>Quality(1));
            Button(overlay.transform,"ВЫСОКОЕ",new Vector2(.58f,.205f),new Vector2(.71f,.255f),()=>Quality(2));
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
            Label(overlay.transform,title,22,TextAnchor.MiddleLeft,new Vector2(.29f,y+.04f),new Vector2(.71f,y+.1f));
            var root=Box(title,overlay.transform,new Vector2(.29f,y),new Vector2(.71f,y+.04f),new Color(.12f,.25f,.32f));
            root.GetComponent<Image>().raycastTarget=true;var slider=root.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;
            var fill=Box("Fill",root.transform,Vector2.zero,Vector2.one,cyan);slider.fillRect=fill.GetComponent<RectTransform>();
            var handle=Box("Handle",root.transform,Vector2.zero,new Vector2(.03f,1),gold);slider.handleRect=handle.GetComponent<RectTransform>();slider.targetGraphic=handle.GetComponent<Image>();handle.GetComponent<Image>().raycastTarget=true;slider.value=value;slider.onValueChanged.AddListener(action);
        }
        void BuildMobile(Transform root)
        {
            mobile=Box("Touch Controls",root,Vector2.zero,Vector2.one,Color.clear);
            var look=Box("Look",mobile.transform,new Vector2(.45f,.12f),new Vector2(1,.76f),new Color(0,0,0,.001f));look.GetComponent<Image>().raycastTarget=true;look.AddComponent<MobileLookArea>();
            var stick=Box("Move",mobile.transform,new Vector2(.035f,.12f),new Vector2(.23f,.4f),new Color(.1f,.3f,.4f,.48f));stick.GetComponent<Image>().raycastTarget=true;stick.AddComponent<MobileJoystick>();Label(stick.transform,"ДВИЖЕНИЕ",21,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one);
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
            var image=obj.GetComponent<Image>();image.color=color;image.sprite=(name=="Screens"||name=="HUD"||name=="Карта"||name=="Touch Controls")?solid:CartoonUI.Rounded;
            image.type=image.sprite==solid?Image.Type.Simple:Image.Type.Sliced;image.raycastTarget=name=="Screens"||name=="Карта";return obj;
        }
        Text Label(Transform parent,string text,int size,TextAnchor alignment,Vector2 min,Vector2 max)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var label=obj.GetComponent<Text>();label.font=font;label.text=text;label.fontSize=size;label.alignment=alignment;label.color=Color.white;label.raycastTarget=false;label.supportRichText=true;label.horizontalOverflow=HorizontalWrapMode.Wrap;CartoonUI.Typography(label,size>=22);return label;
        }
        Button Button(Transform parent,string title,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {
            var obj=Box(title,parent,min,max,cyan);var button=obj.AddComponent<Button>();button.targetGraphic=obj.GetComponent<Image>();CartoonUI.StyleButton(button,title.Contains("ПРОДОЛЖИТЬ")||title.Contains("НОВАЯ")||title.Contains("ЕЩЁ"));button.onClick.AddListener(action);
            var text=Label(obj.transform,title,18,TextAnchor.MiddleCenter,new Vector2(.04f,.04f),new Vector2(.96f,.96f));text.color=CartoonUI.Cream;text.fontStyle=FontStyle.Bold;return button;
        }
        Image Bar(Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var back=Box("Bar",parent,min,max,new Color(.06f,.17f,.45f));var fill=Box("Fill",back.transform,new Vector2(.006f,.10f),new Vector2(.994f,.90f),color).GetComponent<Image>();fill.type=Image.Type.Sliced;fill.raycastTarget=false;
            CartoonUI.Image("Блик полоски",fill.transform,new Vector2(.01f,.57f),new Vector2(.99f,.94f),new Color(1,1,1,.24f));
            fill.gameObject.AddComponent<CartoonBarFill>();return fill;
        }
        static string TimeLabel(float seconds) {int n=Mathf.Max(0,Mathf.FloorToInt(seconds));return $"{n/60:00}:{n%60:00}";}
    }
}
