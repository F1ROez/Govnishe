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
        Sprite solid;Texture2D solidTexture;
        Canvas canvas;
        GameObject hud, overlay, mobile, fullMap, controlHints,intermissionPanel,stallPrompt;
        Image nextWaveFill;Camera promptCamera;bool shownIntermission;
        bool mapExpanded;
        Text waveTitle,stageText,soundMute;Text stats, healthText, xpText, notification, aimHint, hitMarker, hitLabel,chargeLabel;
        Text tutorialLabel,waveProgressText;GameObject tutorialPanel,questBoard;
        HudProgressMeter waveMeter;HudWaveNotice waveNotice;readonly HudQuestCard[] questCards=new HudQuestCard[3];
        int shownWaveResolved=-1,shownWavePlanned=-1,shownAlive=-1,announcedWave=-1;bool announcedClear;
        string shownTutorial;
        float hitUntil;
        bool lastHitCritical,hitVisible;
        Image healthFill, xpFill,screenShade;
        Image escapeFill;GameObject capturePanel;
        AbilityDisplay heavyAbility,dashAbility;
        GameState shownState = (GameState)(-1);
        int shownLevel = -1;
        int shownCoins=-1,shownRunCoins=-1;float coinFlashUntil;Text coinLabel,coinRunLabel;
        int shownHealth=int.MinValue,shownMaxHealth=int.MinValue,shownStamina=int.MinValue,shownMaxStamina=int.MinValue;
        int shownWave=int.MinValue,shownRemaining=int.MinValue,shownKills=int.MinValue;
        int shownChargeMode=-1,shownChargePercent=-1,shownCameraMode=-1;
        string shownStage;
        readonly Color ink = CartoonUI.Ink;
        readonly Color gold = CartoonUI.Gold;
        readonly Color cyan = CartoonUI.Aqua;

        public void Initialize()
        {
            if (canvas != null) return;
            font = CartoonUI.Font;
            solidTexture = new Texture2D(1,1); solidTexture.SetPixel(0,0,Color.white); solidTexture.Apply();
            solid = Sprite.Create(solidTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f));
            var root = new GameObject("Brainrot UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = root.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280, 720); scale.matchWidthOrHeight = .5f;
            UIInputRouter.EnsureForScene(gameObject.scene);
            hud = Box("HUD", root.transform, Vector2.zero, Vector2.one, Color.clear);
            var plate=TropicalUIArt.Place("Здоровье и выносливость",hud.transform,new Vector2(.018f,.84f),new Vector2(.292f,.985f),"VitalsFrame").gameObject;
            healthFill=Bar(plate.transform,new Vector2(.265f,.55f),new Vector2(.952f,.87f),new Color(1,.105f,.16f));
            xpFill=Bar(plate.transform,new Vector2(.265f,.19f),new Vector2(.952f,.49f),new Color(0,.83f,1));
            healthText=Label(plate.transform,"ЗДОРОВЬЕ",16,TextAnchor.MiddleCenter,new Vector2(.28f,.55f),new Vector2(.94f,.87f));
            xpText=Label(plate.transform,"ВЫНОСЛИВОСТЬ",13,TextAnchor.MiddleCenter,new Vector2(.28f,.19f),new Vector2(.94f,.49f));
            TropicalUIArt.Place("Портрет Сахура",plate.transform,new Vector2(.012f,.01f),new Vector2(.275f,.99f),"Portrait",true);
            var wave=Box("Компактная волна",hud.transform,new Vector2(.407f,.925f),new Vector2(.593f,.985f),new Color(.025f,.19f,.23f,.94f));
            waveTitle=Label(wave.transform,"ВОЛНА 1",14,TextAnchor.MiddleCenter,new Vector2(.07f,.58f),new Vector2(.93f,.98f));
            stageText=Label(wave.transform,"",10,TextAnchor.MiddleRight,new Vector2(.52f,-.10f),new Vector2(.94f,.25f));stageText.gameObject.SetActive(false);
            stats=Label(wave.transform,"",9,TextAnchor.MiddleRight,new Vector2(.52f,-.50f),new Vector2(.94f,-.15f));
            var soundButton=RoundButton("Звук",new Vector2(.891f,.905f),new Vector2(.936f,.985f),"SoundIcon",()=>GameMusic.ToggleMute());
            soundMute=Label(soundButton.transform,"×",19,TextAnchor.MiddleCenter,new Vector2(.55f,.01f),new Vector2(.99f,.46f));soundMute.color=new Color(1,.35f,.25f);
            RoundButton("Пауза",new Vector2(.943f,.905f),new Vector2(.988f,.985f),"PauseIcon",()=>GameManager.Instance.Pause());
            waveMeter=QuestHUD.Meter(wave.transform,"Ход волны",new Vector2(.06f,.35f),new Vector2(.94f,.44f),new Color(.32f,.73f,.68f));
            waveMeter.GetComponent<Image>().color=new Color(.3f,.49f,.51f);
            waveProgressText=Label(wave.transform,"",9,TextAnchor.MiddleLeft,new Vector2(.06f,-.50f),new Vector2(.55f,-.15f));
            var wallet=TropicalUIArt.Place("Монеты",hud.transform,new Vector2(.805f,.91f),new Vector2(.881f,.975f),"IslandSecondary").gameObject;
            var coin=TropicalUIArt.Place("Золотая монета",wallet.transform,new Vector2(.10f,.18f),new Vector2(.39f,.82f),"IslandCoin",true);
            coinLabel=Label(wallet.transform,"",23,TextAnchor.MiddleCenter,new Vector2(.40f,.12f),new Vector2(.92f,.90f));coinLabel.color=ReferenceScreens.Cocoa;
            coinRunLabel=Label(wallet.transform,"",10,TextAnchor.MiddleCenter,new Vector2(0,-.35f),new Vector2(1,-.02f));coinRunLabel.gameObject.SetActive(false);

            var waveBanner=Box("Событие волны",hud.transform,new Vector2(.355f,.765f),new Vector2(.645f,.85f),Color.clear);
            waveNotice=waveBanner.AddComponent<HudWaveNotice>();waveNotice.Build();
            notification=Label(hud.transform,"",20,TextAnchor.MiddleCenter,new Vector2(.20f,.60f),new Vector2(.80f,.66f));notification.color=gold;
            BuildQuestBoard();BuildIntermissionHud();
            tutorialPanel=TropicalUIArt.Place("Короткое обучение",hud.transform,new Vector2(.24f,.12f),new Vector2(.80f,.195f),"StatsStrip").gameObject;
            tutorialLabel=Label(tutorialPanel.transform,"",14,TextAnchor.MiddleCenter,new Vector2(.04f,.15f),new Vector2(.79f,.85f));tutorialLabel.color=new Color(.06f,.18f,.25f);tutorialLabel.GetComponent<Outline>().effectColor=new Color(1,1,.85f,.8f);
            var skip=Button(tutorialPanel.transform,"СКРЫТЬ",new Vector2(.82f,.20f),new Vector2(.98f,.80f),()=>GameManager.Instance.Goals.SkipTutorial());skip.GetComponentInChildren<Text>().fontSize=11;
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

        public void ResetRunPresentation()
        {
            announcedWave=-1;announcedClear=false;
            waveMeter?.Set(0,true);waveNotice?.Hide();
            foreach(var card in questCards)if(card!=null)card.ResetPresentation();
            hitVisible=false;hitUntil=0;if(hitMarker!=null)hitMarker.text="";if(hitLabel!=null)hitLabel.text="";
            shownChargeMode=-1;shownChargePercent=-1;
        }

        public void Refresh()
        {
            if (canvas == null || GameManager.Instance == null) return;
            var gm=GameManager.Instance;
            bool hudVisible=gm.State==GameState.Playing || gm.State==GameState.Map;
            SetActive(hud,hudVisible);
            SetActive(mobile,Application.isMobilePlatform && gm.State==GameState.Playing);
            SetActive(overlay,gm.State!=GameState.Playing&&gm.State!=GameState.Map);
            if(shownState!=gm.State)screenShade.color=new Color(.025f,.025f,.025f,gm.State==GameState.Paused?.42f:.3f);
            if(hudVisible)RefreshHud(gm);
            mapExpanded=gm.State==GameState.Map;
            SetActive(fullMap,mapExpanded);
            int level=gm.Upgrades!=null?gm.Upgrades.Level:0;
            if(shownState!=gm.State || (gm.State==GameState.Upgrade && shownLevel!=level))
            {
                if(gm.State==GameState.Dead){announcedWave=-1;shownWaveResolved=-1;}
                shownState=gm.State;shownLevel=level;
                MobileInput.Clear();
                if(gm.State!=GameState.Playing) BuildScreen();
            }
        }
        static void SetActive(GameObject target,bool active){if(target.activeSelf!=active)target.SetActive(active);}
        void RefreshHud(GameManager gm)
        {
            var player=gm.Player;
            if(shownCoins!=gm.RunBalance||shownRunCoins!=gm.RunCoins){if(shownCoins>=0&&gm.RunBalance>shownCoins)coinFlashUntil=Time.unscaledTime+.3f;shownCoins=gm.RunBalance;shownRunCoins=gm.RunCoins;coinLabel.text=shownCoins.ToString();coinRunLabel.text="СОБРАНО "+shownRunCoins;}
            float coinPop=Mathf.Clamp01((coinFlashUntil-Time.unscaledTime)/.3f);var coinScale=Vector3.one*(1+Mathf.Sin(coinPop*Mathf.PI)*.12f);if(coinLabel.transform.localScale!=coinScale)coinLabel.transform.localScale=coinScale;coinLabel.color=Color.Lerp(CartoonUI.Cream,new Color(1,.82f,.25f),coinPop);

            if(player!=null){
                int health=Mathf.CeilToInt(player.Health),maximum=Mathf.CeilToInt(player.Stats.MaxHealth);
                if(health!=shownHealth||maximum!=shownMaxHealth){shownHealth=health;shownMaxHealth=maximum;healthText.text=$"ЗДОРОВЬЕ   {health} / {maximum}";}
                healthFill.fillAmount=Mathf.Clamp01(player.Health/Mathf.Max(1,player.Stats.MaxHealth));
                int stamina=Mathf.CeilToInt(player.Stamina.Value),staminaMaximum=Mathf.CeilToInt(player.Stamina.Maximum);
                if(stamina!=shownStamina||staminaMaximum!=shownMaxStamina){shownStamina=stamina;shownMaxStamina=staminaMaximum;xpText.text=$"ВЫНОСЛИВОСТЬ  {stamina} / {staminaMaximum}";}
                xpFill.fillAmount=player.Stamina.Value/Mathf.Max(1,player.Stamina.Maximum);
            }
            int wave=gm.Wave;
            if(wave!=shownWave){shownWave=wave;waveTitle.text=$"ВОЛНА {wave}";}
            string stage=gm.Spawner.Waves.Stage;int remaining=Mathf.CeilToInt(gm.Spawner.Waves.Remaining);
            int alive=gm.Spawner.Active.Count;
            bool clearing=gm.Spawner.Waves.WaitingForClear;
            if(stage!=shownStage||remaining!=shownRemaining||alive!=shownAlive){shownStage=stage;shownRemaining=remaining;shownAlive=alive;stageText.text=clearing?"ЗАЧИСТКА":gm.Spawner.Waves.Resting?"ЛАВКА ОТКРЫТА":stage=="ПОДКРЕПЛЕНИЯ"?"ПОДКРЕПЛЕНИЯ":$"{stage} · {remaining} с";}
            int resolved=gm.Spawner.WaveResolved,planned=gm.Spawner.WavePlanned;
            if(resolved!=shownWaveResolved||planned!=shownWavePlanned){shownWaveResolved=resolved;shownWavePlanned=planned;waveProgressText.text=planned>0?$"ПРОЙДЕНО {Mathf.Min(resolved,planned)} / {planned}":"ПОДГОТОВКА";}
            waveMeter.Set(gm.Spawner.WaveCompletion);
            if(gm.IsPlaying){
                if(announcedWave!=wave){announcedWave=wave;announcedClear=false;waveMeter.Set(gm.Spawner.WaveCompletion,true);waveNotice.Show(wave,false,gm.Goals!=null?gm.Goals.WaveEventText:"");GameAudio.PlayWave();}
                bool complete=gm.Spawner.Waves.Resting&&!clearing;
                if(complete&&!announcedClear){announcedClear=true;waveNotice.Show(wave,true,"");GameAudio.PlayWave(true);}
            }
            RefreshIntermission(gm);
            if(gm.Kills!=shownKills){shownKills=gm.Kills;stats.text=$"ПОБЕД: {gm.Kills}";}
            SetActive(capturePanel,player!=null&&player.IsCaptured);if(player!=null)escapeFill.fillAmount=player.EscapeProgress;
            notification.text=gm.Notification ?? "";SetActive(soundMute.gameObject,GameMusic.IsMuted);
            SetActive(controlHints,gm.IsPlaying&&!Application.isMobilePlatform);SetActive(notification.gameObject,gm.IsPlaying);
            string hint=gm.Goals!=null?gm.Goals.TutorialText:"";
            if(gm.Goals!=null)for(int i=0;i<questCards.Length;i++)questCards[i].Present(gm.Goals.GetCard(i));
            if(hint!=shownTutorial){shownTutorial=hint;tutorialLabel.text=hint;}
            bool guidance=gm.IsPlaying&&(player==null||!player.IsCaptured);
            SetActive(tutorialPanel,guidance&&!string.IsNullOrEmpty(hint));SetActive(questBoard,guidance&&!gm.IsIntermission);
            if(player!=null){heavyAbility.Update(Mathf.Max(player.Combat.HeavyCooldownRemaining,player.Combat.BoomerangRemaining),player.Combat.HeavyCooldownRemaining>0?player.Combat.HeavyCooldownDuration:Mathf.Max(.35f,GameBalance.Current.Boomerang.OutSeconds+GameBalance.Current.Boomerang.ReturnSeconds));dashAbility.Update(player.DashCooldownRemaining,player.DashCooldownDuration);}
            var combat=player!=null?player.Combat:null;
            int chargeMode=combat==null?0:combat.BoomerangActive?4:combat.HeavyCooldownNotice?1:!combat.IsCharging?0:combat.ChargeProgress>=1?3:2;
            int percent=chargeMode==2?Mathf.RoundToInt(combat.ChargeProgress*100):0;
            if(chargeMode!=shownChargeMode||percent!=shownChargePercent){
                shownChargeMode=chargeMode;shownChargePercent=percent;
                chargeLabel.text=chargeMode==4?"БИТА ВОЗВРАЩАЕТСЯ":chargeMode==1?"СИЛЬНЫЙ УДАР ПЕРЕЗАРЯЖАЕТСЯ":chargeMode==3?"СИЛЬНЫЙ УДАР ГОТОВ — ОТПУСТИ ЛКМ":chargeMode==2?"ЗАРЯД СИЛЬНОГО УДАРА: "+percent+"%":"";
            }
            int cameraMode=player!=null&&player.CameraLocked?1:0;
            if(cameraMode!=shownCameraMode){shownCameraMode=cameraMode;aimHint.text=cameraMode==1?"V · камера: фикс.":"V · камера: своб.";}
            if(hitVisible&&Time.unscaledTime>=hitUntil){hitVisible=false;hitMarker.text="";hitLabel.text="";}
            else if(hitVisible){
                float alpha=Mathf.Clamp01((hitUntil-Time.unscaledTime)/.12f);
                Color feedback=lastHitCritical?gold:Color.white;feedback.a=alpha;
                hitMarker.color=feedback;hitLabel.color=feedback;
            }
        }

        void BuildIntermissionHud(){
            intermissionPanel=Box("Между волнами",hud.transform,new Vector2(.305f,.715f),new Vector2(.695f,.842f),new Color(.025f,.20f,.24f,.94f));
            Label(intermissionPanel.transform,"ВОЛНА ПРОЙДЕНА",20,TextAnchor.MiddleCenter,new Vector2(.04f,.61f),new Vector2(.96f,.96f)).color=new Color(1,.91f,.57f);
            Label(intermissionPanel.transform,"Удерживай F — следующая волна",16,TextAnchor.MiddleCenter,new Vector2(.04f,.24f),new Vector2(.96f,.59f));
            var track=Box("Подготовка следующей волны",intermissionPanel.transform,new Vector2(.10f,.10f),new Vector2(.90f,.18f),new Color(.01f,.09f,.13f));
            nextWaveFill=Box("Удержание F",track.transform,Vector2.zero,Vector2.one,new Color(.10f,.90f,.81f)).GetComponent<Image>();
            nextWaveFill.type=Image.Type.Filled;nextWaveFill.fillMethod=Image.FillMethod.Horizontal;nextWaveFill.fillAmount=0;
            stallPrompt=Box("Подсказка лавки",hud.transform,Vector2.zero,Vector2.one,new Color(.025f,.16f,.19f,.94f));
            var rect=(RectTransform)stallPrompt.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(250,46);
            Label(stallPrompt.transform,"[E]  ОТКРЫТЬ ЛАВКУ",18,TextAnchor.MiddleCenter,new Vector2(.04f,.12f),new Vector2(.96f,.88f));
            intermissionPanel.SetActive(false);stallPrompt.SetActive(false);
        }
        void RefreshIntermission(GameManager game){
            bool active=game.IsIntermission;
            SetActive(intermissionPanel,active);
            if(active!=shownIntermission){shownIntermission=active;SetActive(waveNotice.gameObject,!active);}
            if(!active){SetActive(stallPrompt,false);nextWaveFill.fillAmount=0;return;}
            var shop=game.Stall;nextWaveFill.fillAmount=shop!=null?shop.StartHoldProgress:0;
            bool near=shop!=null&&shop.CanInteract;
            SetActive(stallPrompt,near);
            if(!near)return;
            if(promptCamera==null){
                promptCamera=Camera.main;
                if(promptCamera==null)foreach(var view in Camera.allCameras)if(view.name=="Arena Camera"){promptCamera=view;break;}
            }
            if(promptCamera==null){SetActive(stallPrompt,false);return;}
            Vector3 screen=promptCamera.WorldToScreenPoint(shop.WorldPosition);
            if(screen.z<=0||screen.x<0||screen.x>Screen.width||screen.y<0||screen.y>Screen.height){SetActive(stallPrompt,false);return;}
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hud.transform,screen,null,out var local)){
                var rect=(RectTransform)stallPrompt.transform;if(rect.anchoredPosition!=local)rect.anchoredPosition=local;
            }
        }

        void BuildQuestBoard(){
            questBoard=Box("Испытания",hud.transform,new Vector2(.805f,.50f),new Vector2(.985f,.86f),Color.clear);
            var heading=TropicalUIArt.Place("Заголовок испытаний",questBoard.transform,new Vector2(.07f,.885f),new Vector2(.93f,1),"StageRibbon");
            Label(heading.transform,"ИСПЫТАНИЯ",14,TextAnchor.MiddleCenter,new Vector2(.12f,.08f),new Vector2(.88f,.92f));
            for(int i=0;i<questCards.Length;i++){
                float top=.865f-i*.293f;
                var card=Box("Испытание "+(i+1),questBoard.transform,new Vector2(0,top-.265f),new Vector2(1,top),Color.clear);
                questCards[i]=card.AddComponent<HudQuestCard>();questCards[i].Build();
            }
        }
        void BuildAbilityIcons(){
            var strong=AbilityCard("Сильный удар",new Vector2(.022f,.65f),new Vector2(.082f,.78f),"HeavyCard","HeavyIcon","","ЛКМ · держать");
            heavyAbility=new AbilityDisplay(CooldownShade(strong.transform),ReadyBadge(strong.transform));
            var dash=AbilityCard("Рывок",new Vector2(.022f,.475f),new Vector2(.082f,.605f),"DashCard","DashIcon","","ПРОБЕЛ");
            dashAbility=new AbilityDisplay(CooldownShade(dash.transform),ReadyBadge(dash.transform));
            controlHints=Box("Клавиши под способностями",hud.transform,new Vector2(.022f,.345f),new Vector2(.16f,.445f),Color.clear);
            Label(controlHints.transform,"ЛКМ · удар | ПКМ · бумеранг",11,TextAnchor.MiddleLeft,new Vector2(0,.67f),new Vector2(1,1));
            Label(controlHints.transform,"Shift · бег",11,TextAnchor.MiddleLeft,new Vector2(0,.34f),new Vector2(1,.67f));
            aimHint=Label(controlHints.transform,"",11,TextAnchor.MiddleLeft,Vector2.zero,new Vector2(1,.34f));
        }

        GameObject AbilityCard(string name,Vector2 min,Vector2 max,string frame,string icon,string title,string key){
            var card=TropicalUIArt.Place(name,hud.transform,min,max,frame).gameObject;
            Label(card.transform,title,12,TextAnchor.MiddleCenter,new Vector2(.10f,.70f),new Vector2(.90f,.95f));
            if(!Application.isMobilePlatform){
                var hint=Box("Клавиша способности",card.transform,new Vector2(.03f,-.14f),new Vector2(.97f,.025f),new Color(.035f,.16f,.20f,.88f));
                Label(hint.transform,key,10,TextAnchor.MiddleCenter,new Vector2(.04f,.05f),new Vector2(.96f,.95f));
            }

            return card;
        }
        Text ReadyBadge(Transform parent){var badge=TropicalUIArt.Place("Готовность",parent,new Vector2(.10f,.075f),new Vector2(.90f,.255f),"ReadyBadge");return Label(badge.transform,"ГОТОВ",13,TextAnchor.MiddleCenter,new Vector2(.04f,.08f),new Vector2(.96f,.94f));}
        Image CooldownShade(Transform parent){var image=Box("Перезарядка",parent,new Vector2(.12f,.255f),new Vector2(.88f,.72f),new Color(.10f,.10f,.10f,.62f)).GetComponent<Image>();image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Radial360;image.fillOrigin=2;image.fillClockwise=true;image.raycastTarget=false;return image;}
        sealed class AbilityDisplay
        {
            readonly Image shade,badge;readonly Text label;
            int shownSeconds=int.MinValue;bool cooling;
            public AbilityDisplay(Image shade,Text label){this.shade=shade;this.label=label;badge=label.transform.parent.GetComponent<Image>();}
            public void Update(float remaining,float duration){
                shade.fillAmount=Mathf.Clamp01(remaining/Mathf.Max(.1f,duration));
                int seconds=remaining>0?Mathf.CeilToInt(remaining):0;
                bool active=remaining>0,first=shownSeconds==int.MinValue;
                if(seconds!=shownSeconds){shownSeconds=seconds;label.text=seconds>0?seconds+" с":"ГОТОВ";}
                if(first||active!=cooling){cooling=active;label.color=active?new Color(1,.9f,.6f):Color.white;badge.color=active?new Color(.76f,.80f,.75f):Color.white;}
            }
        }
        Image RoundButton(string name,Vector2 min,Vector2 max,string icon,UnityEngine.Events.UnityAction action){
            if(Application.isMobilePlatform){float right=name=="Пауза" ? .987f : .895f;min=new Vector2(right-.075f,.86f);max=new Vector2(right,.99f);}
            var hitbox=Box(name,hud.transform,min,max,Color.clear);hitbox.GetComponent<Image>().raycastTarget=true;
            var face=TropicalUIArt.Place(name+" — оформление",hitbox.transform,Vector2.zero,Vector2.one,icon);face.preserveAspect=true;var button=hitbox.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.None;button.onClick.AddListener(GameMusic.PlayButtonClick);button.onClick.AddListener(action);hitbox.AddComponent<CartoonButtonMotion>().VisualTarget=face.transform;CartoonUI.EnsureTouchTarget(button);
            // Atlas sound/pause sprites already include their circular rim; avoid a second nested button.
            return face;
        }

        void ClearScreen()
        {
            foreach(Transform child in overlay.transform) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
            TropicalUIArt.Place("Резная рамка экрана",overlay.transform,new Vector2(.25f,.065f),new Vector2(.75f,.795f),"WoodPanel");
        }
        void BuildScreen()
        {
            ClearOverlay();var gm=GameManager.Instance;screenShade.color=new Color(.025f,.04f,.065f,gm.State==GameState.Paused?.42f:.3f);
            if(gm.State==GameState.Menu)
            {
                var viewport=ReferenceScreens.Viewport(overlay.transform,"Главное меню 16:9");
                ReferenceScreens.MainMenu(viewport,()=>gm.StartRun(),ShowShop,ShowRecords,ShowSettings,()=>gm.Quit(),out _,out _,true);
            }
            else if(gm.State==GameState.Paused)
            {
                ReferenceScreens.Pause(ReferenceScreens.Viewport(overlay.transform,"Пауза 16:9"),()=>gm.Resume(),ShowSettings,()=>gm.MainMenu());
            }
            else if(gm.State==GameState.RunShop)
            {
                ShopScreen.ShowRun(ReferenceScreens.Viewport(overlay.transform,"Лавка 16:9"),()=>gm.CloseRunShop());
            }
            else if(gm.State==GameState.Dead)
            {
                var frame=ReferenceScreens.Panel(ReferenceScreens.Viewport(overlay.transform,"Результат забега"),"ЗАБЕГ ОКОНЧЕН",.28f,.10f,.44f,.81f);
                ReferenceScreens.SeaText(frame,$"ВОЛНА {gm.Wave}  ·  {TimeLabel(gm.Elapsed)}  ·  ПОБЕД {gm.Kills}",20,.08f,.23f,.84f,.07f);
                ReferenceScreens.SeaText(frame,$"СОБРАНО МОНЕТ: {gm.RunCoins}\nВ ЭТОМ ЗАБЕГЕ",25,.08f,.32f,.84f,.13f);
                ReferenceScreens.SeaText(frame,"ЛИЧНЫЙ РЕКОРД · ВОЛНА "+SaveSystem.Data.BestWave,16,.08f,.475f,.84f,.05f);
                ReferenceScreens.SeaText(frame,"Монеты и улучшения этой попытки завершены. Новый забег — новое начало.",18,.10f,.55f,.80f,.11f);
                var again=ReferenceScreens.SeaButton(frame,"ИГРАТЬ СНОВА",.10f,.70f,.80f,.11f,()=>gm.StartRun(),27,true);
                ReferenceScreens.SeaButton(frame,"ГЛАВНОЕ МЕНЮ",.10f,.84f,.80f,.09f,()=>gm.MainMenu(),23);
                if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(again.gameObject);
            }
            else if(gm.State==GameState.Upgrade)
            {
                ClearScreen();Title("НОВЫЙ УРОВЕНЬ!");Label(overlay.transform,"ВЫБЕРИ УЛУЧШЕНИЕ",23,TextAnchor.MiddleCenter,new Vector2(.1f,.66f),new Vector2(.9f,.74f)).color=cyan;
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
        void Title(string title) {
            var heading=TropicalUIArt.Place("Заголовок экрана",overlay.transform,new Vector2(.22f,title=="ПАУЗА"?.61f:.67f),new Vector2(.78f,title=="ПАУЗА"?.87f:.90f),"HeaderPlaque");
            Label(heading.transform,title,42,TextAnchor.MiddleCenter,new Vector2(.25f,.20f),new Vector2(.75f,.60f)).color=CartoonUI.Cream;
            TropicalUIArt.Place("Листва слева",overlay.transform,new Vector2(.225f,.035f),new Vector2(.34f,.19f),"LeafAccent",true);
            var leaf=TropicalUIArt.Place("Листва справа",overlay.transform,new Vector2(.66f,.035f),new Vector2(.775f,.19f),"LeafAccent",true);leaf.rectTransform.localScale=new Vector3(-1,1,1);
            TropicalUIArt.Place("Гибискус",overlay.transform,new Vector2(.455f,.025f),new Vector2(.545f,.14f),"FlowerAccent",true);
        }
        void MenuButton(string title,float y,UnityEngine.Events.UnityAction action) {Button(overlay.transform,title,new Vector2(.345f,y),new Vector2(.655f,y+.095f),action);}
        void ShowRecords()
        {
            ClearOverlay();ReferenceScreens.Records(ReferenceScreens.Viewport(overlay.transform,"Рекорды 16:9"),BuildScreen);
        }
        void ClearOverlay(){foreach(Transform child in overlay.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void ShowShop(){
            var gm=GameManager.Instance;if(gm==null||gm.State!=GameState.Menu)return;
            ClearOverlay();ShopScreen.Show(ReferenceScreens.Viewport(overlay.transform,"Магазин 16:9"),BuildScreen);
        }
        void ShowSettings()
        {
            ClearOverlay();screenShade.color=new Color(.025f,.04f,.065f,.42f);
            ReferenceScreens.Settings(ReferenceScreens.Viewport(overlay.transform,"Настройки 16:9"),BuildScreen);
        }

        public void ShowHit(bool critical,int count=1)
        {
            if(hitMarker==null)return;
            hitUntil=Time.unscaledTime+.3f;lastHitCritical=critical;hitVisible=true;
            hitMarker.text="×";hitLabel.text=(critical?"КРИТИЧЕСКИЙ УДАР":"ПОПАДАНИЕ")+(count>1?" ×"+count:"");
        }

        public void ShowMiss()
        {
            // A miss must not erase a confirmed hit from an immediately preceding echo.
            if(hitMarker==null || Time.unscaledTime<hitUntil)return;
            hitUntil=Time.unscaledTime+.13f;lastHitCritical=false;hitVisible=true;
            hitMarker.text="";hitLabel.text="ПРОМАХ";
        }

        void Quality(int quality) {SaveSystem.Data.Quality=quality;GameQuality.Apply(quality);SaveSystem.Save();ShowSettings();}
        void QualityButton(string title,float left,float right,int quality){
            var button=Button(overlay.transform,title,new Vector2(left,.175f),new Vector2(right,.23f),()=>Quality(quality));
            if(SaveSystem.Data.Quality==quality)CartoonUI.MarkSelected(button);
        }
        void SettingSlider(string title,float y,float min,float max,float value,UnityEngine.Events.UnityAction<float> action)
        {
            Label(overlay.transform,title,21,TextAnchor.MiddleLeft,new Vector2(.30f,y+.04f),new Vector2(.70f,y+.095f));
            CartoonUI.GlossySlider(title,overlay.transform,new Vector2(.30f,y),new Vector2(.70f,y+.042f),min,max,value,action);
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
            return ReferenceScreens.Hotspot(parent,title,min.x,1-max.y,max.x-min.x,max.y-min.y,action,21);
        }
        Image Bar(Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var rim=Box("Контур полоски",parent,min,max,new Color(.64f,.97f,1));
            var back=Box("Bar",rim.transform,new Vector2(.006f,.07f),new Vector2(.994f,.93f),new Color(.015f,.13f,.29f));
            var fill=Box("Fill",back.transform,new Vector2(.006f,.08f),new Vector2(.994f,.92f),color).GetComponent<Image>();fill.type=Image.Type.Sliced;fill.raycastTarget=false;
            CartoonUI.Image("Блик полоски",fill.transform,new Vector2(.018f,.61f),new Vector2(.982f,.91f),new Color(1,1,1,.36f));
            CartoonUI.Image("Отражение полоски",fill.transform,new Vector2(.025f,.11f),new Vector2(.975f,.19f),new Color(1,1,1,.18f));
            fill.gameObject.AddComponent<CartoonBarFill>();return fill;
        }
        void OnDestroy(){
            if(canvas!=null){if(Application.isPlaying)Destroy(canvas.gameObject);else DestroyImmediate(canvas.gameObject);canvas=null;}
            if(solid!=null){if(Application.isPlaying)Destroy(solid);else DestroyImmediate(solid);solid=null;}
            if(solidTexture!=null){if(Application.isPlaying)Destroy(solidTexture);else DestroyImmediate(solidTexture);solidTexture=null;}
        }
        static string TimeLabel(float seconds) {int n=Mathf.Max(0,Mathf.FloorToInt(seconds));return $"{n/60:00}:{n%60:00}";}
    }
}
