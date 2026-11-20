using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Brainrot.Tests
{
    /// <summary>Exercise actual live controls and persisted profile changes, not screenshots with fixed numbers.</summary>
    public sealed class IslandMenuTests
    {
        IDisposable storage;string directory;GameObject root;RectTransform viewport;
        Scene previous,scene;float oldVolume,oldTimeScale;bool oldMute,oldCursorVisible;CursorLockMode oldCursorLock;GameMusic originalMusic;
        readonly HashSet<int> existingFlows=new HashSet<int>();
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Run UI tests from Edit mode, without an active game.");
            oldVolume=AudioListener.volume;oldTimeScale=Time.timeScale;oldMute=GameMusic.IsMuted;oldCursorVisible=Cursor.visible;oldCursorLock=Cursor.lockState;originalMusic=GameMusic.Instance;
            if(oldMute)GameMusic.ToggleMute();
            existingFlows.Clear();foreach(var flow in Object.FindObjectsByType<SceneFlow>(FindObjectsSortMode.None))existingFlows.Add(flow.GetInstanceID());
            directory=Path.Combine(Path.GetTempPath(),"brainrot-menu-"+Guid.NewGuid().ToString("N"));storage=SaveSystem.UseStorageDirectory(directory);
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Island menu "+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(scene);
            root=new GameObject("Menu test bounds",typeof(RectTransform));root.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
            viewport=ReferenceScreens.Viewport(root.transform,"Reference");yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(root!=null)Object.DestroyImmediate(root);
            if(GameMusic.Instance!=null&&GameMusic.Instance!=originalMusic)Object.DestroyImmediate(GameMusic.Instance.gameObject);
            foreach(var flow in Object.FindObjectsByType<SceneFlow>(FindObjectsSortMode.None))if(!existingFlows.Contains(flow.GetInstanceID()))Object.DestroyImmediate(flow.gameObject);
            GameQuality.Release();if(GameMusic.IsMuted!=oldMute)GameMusic.ToggleMute();AudioListener.volume=oldVolume;Time.timeScale=oldTimeScale;Cursor.lockState=oldCursorLock;Cursor.visible=oldCursorVisible;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            storage?.Dispose();if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        Button FindButton(string label,Transform under=null){
            foreach(var button in (under!=null?under:root.transform).GetComponentsInChildren<Button>()){
                var text=button.GetComponentInChildren<Text>();if(text!=null&&text.text==label)return button;
            }
            Assert.Fail("No active button: "+label);return null;
        }
        Text FindText(string label){
            foreach(var text in root.GetComponentsInChildren<Text>())if(text.text==label)return text;
            Assert.Fail("No active live label: "+label);return null;
        }
        Slider FindSlider(string prefix){
            foreach(var slider in root.GetComponentsInChildren<Slider>())if(slider.name.StartsWith(prefix,StringComparison.Ordinal))return slider;
            Assert.Fail("No slider: "+prefix);return null;
        }
        [Test]public void SettingsSlidersChangeLiveValuesAndFlushToProfileOnRelease(){
            SaveSystem.Data.Volume=.42f;SaveSystem.Data.Sensitivity=.8f;Assert.IsTrue(SaveSystem.TrySave());
            ReferenceScreens.Settings(viewport,()=>{});
            Assert.AreEqual(2,root.GetComponentsInChildren<Slider>().Length);
            var volume=FindSlider("ГРОМКОСТЬ");var sensitivity=FindSlider("ЧУВСТВИТЕЛЬНОСТЬ");
            Assert.AreEqual(.42f,volume.value,.001f);Assert.AreEqual(.8f,sensitivity.value,.001f);
            volume.value=.68f;sensitivity.value=1.5f;
            Assert.AreEqual(.68f,SaveSystem.Data.Volume,.001f);Assert.AreEqual(.68f,AudioListener.volume,.001f);
            Assert.AreEqual(1.5f,SaveSystem.Data.Sensitivity,.001f);Assert.IsNotNull(FindText("68%"));Assert.IsNotNull(FindText("1,5×"));
            volume.GetComponent<IslandSliderPersistence>().OnPointerUp(null);sensitivity.GetComponent<IslandSliderPersistence>().OnEndDrag(null);
            SaveSystem.Load();Assert.AreEqual(.68f,SaveSystem.Data.Volume,.001f);Assert.AreEqual(1.5f,SaveSystem.Data.Sensitivity,.001f);
        }
        [Test]public void SettingsSwitchesQualityAndBackPerformTheirOwnActions(){
            SaveSystem.Data.CameraLocked=true;SaveSystem.Data.ShowAttackRange=true;SaveSystem.Data.CameraShakeStrength=1;
            int backs=0;ReferenceScreens.Settings(viewport,()=>backs++);
            FindButton("ФИКСАЦИЯ КАМЕРЫ").onClick.Invoke();FindButton("КОНТУР УДАРА").onClick.Invoke();FindButton("ТРЯСКА КАМЕРЫ").onClick.Invoke();
            Assert.IsNotNull(FindButton("ФИКСАЦИЯ КАМЕРЫ"));Assert.IsNotNull(FindButton("КОНТУР УДАРА"));Assert.IsNotNull(FindButton("ТРЯСКА КАМЕРЫ"));
            Assert.IsFalse(SaveSystem.Data.CameraLocked);Assert.IsTrue(SaveSystem.Data.HasCameraPreference);
            Assert.IsFalse(SaveSystem.Data.ShowAttackRange);Assert.AreEqual(0,SaveSystem.Data.CameraShakeStrength);
            FindButton("ВЫСОКОЕ").onClick.Invoke();Assert.AreEqual(2,SaveSystem.Data.Quality);
            Assert.AreEqual(ReferenceScreens.Accent,FindButton("ВЫСОКОЕ").targetGraphic.color);
            Assert.AreNotEqual(ReferenceScreens.Accent,FindButton("СРЕДНЕЕ").targetGraphic.color);
            FindButton("НИЗКОЕ").onClick.Invoke();SaveSystem.Load();Assert.AreEqual(0,SaveSystem.Data.Quality);
            Assert.IsFalse(SaveSystem.Data.CameraLocked);Assert.IsFalse(SaveSystem.Data.ShowAttackRange);Assert.AreEqual(0,SaveSystem.Data.CameraShakeStrength);
            FindButton("НАЗАД").onClick.Invoke();Assert.AreEqual(1,backs);
        }
        [Test]public void PauseUsesMinimalPaperPanelAndKeepsThreeIndependentActions(){
            int resumed=0,settings=0,ended=0;
            ReferenceScreens.Pause(viewport,()=>resumed++,()=>settings++,()=>ended++);
            bool paper=false,oldSand=false;
            foreach(var image in root.GetComponentsInChildren<Image>()){
                paper|=image.color==ReferenceScreens.Paper;
                oldSand|=image.sprite==TropicalUIArt.Get("IslandSandPanel");
            }
            Assert.IsTrue(paper);Assert.IsFalse(oldSand,"The active minimalist panel must not bring back the old sand board.");
            Assert.AreEqual(3,root.GetComponentsInChildren<Button>().Length);
            FindButton("ПРОДОЛЖИТЬ").onClick.Invoke();FindButton("НАСТРОЙКИ").onClick.Invoke();FindButton("ЗАВЕРШИТЬ ЗАБЕГ").onClick.Invoke();
            Assert.AreEqual(1,resumed);Assert.AreEqual(1,settings);Assert.AreEqual(1,ended);
        }
        [Test]public void MenuShopContainsCosmeticsOnlyAndNoRunUpgradePurchases(){
            int backs=0;ShopScreen.Show(viewport,()=>backs++);
            Assert.IsNotNull(FindText("СКИНЫ ПОЯВЯТСЯ ПОЗЖЕ"));
            foreach(var button in root.GetComponentsInChildren<Button>()){
                var text=button.GetComponentInChildren<Text>();Assert.IsNotNull(text);
                Assert.IsFalse(text.text.StartsWith("КУПИТЬ",StringComparison.Ordinal));
                Assert.AreNotEqual("УЛУЧШЕНИЯ",text.text);
            }
            foreach(var text in root.GetComponentsInChildren<Text>())foreach(var upgrade in MetaProgression.Catalog)Assert.AreNotEqual(upgrade.Title,text.text);
            Assert.AreEqual(1,root.GetComponentsInChildren<Button>().Length);
            FindButton("НАЗАД").onClick.Invoke();Assert.AreEqual(1,backs);
        }
        [Test]public void RunShopCannotExposePurchasesOutsideAnActiveStallContext(){
            ShopScreen.ShowRun(viewport,()=>{});
            Assert.IsNotNull(FindText("ЛАВКА ОТКРЫВАЕТСЯ МЕЖДУ ВОЛНАМИ"));
            foreach(var button in root.GetComponentsInChildren<Button>())Assert.IsFalse(button.GetComponentInChildren<Text>().text.StartsWith("КУПИТЬ",StringComparison.Ordinal));
        }
        [Test]public void RecordsUseSavedNumbersAndReplayTutorialChangesPreference(){
            SaveSystem.Data.BestTime=814;SaveSystem.Data.BestKills=1554;SaveSystem.Data.BestWave=11;SaveSystem.Data.TutorialCompleted=true;
            int backs=0;ReferenceScreens.Records(viewport,()=>backs++);
            Assert.IsNotNull(FindText("13:34"));Assert.IsNotNull(FindText("ПОБЕЖДЕНО: 1554"));Assert.IsNotNull(FindText("ЛУЧШАЯ ВОЛНА: 11"));
            FindButton("ПОВТОРИТЬ ОБУЧЕНИЕ").onClick.Invoke();Assert.IsNotNull(FindButton("ОБУЧЕНИЕ ВКЛЮЧЕНО"));SaveSystem.Load();Assert.IsFalse(SaveSystem.Data.TutorialCompleted);
            FindButton("НАЗАД").onClick.Invoke();Assert.AreEqual(1,backs);
        }
        [Test]public void ProgressResetRequiresConfirmationAndCancelPreservesRecords(){
            SaveSystem.Data.BestWave=9;SaveSystem.Data.Volume=.42f;
            Assert.IsTrue(SaveSystem.TrySave());int backs=0;ReferenceScreens.Settings(viewport,()=>backs++);
            FindButton("СБРОСИТЬ ПРОГРЕСС").onClick.Invoke();Assert.AreEqual(9,SaveSystem.Data.BestWave);
            FindButton("ОТМЕНА").onClick.Invoke();Assert.AreEqual(9,SaveSystem.Data.BestWave);
            FindButton("СБРОСИТЬ ПРОГРЕСС").onClick.Invoke();FindButton("ПОДТВЕРДИТЬ СБРОС").onClick.Invoke();
            Assert.AreEqual(0,SaveSystem.Data.Coins);Assert.AreEqual(0,MetaProgression.Rank(0));Assert.AreEqual(0,SaveSystem.Data.BestWave);
            Assert.AreEqual(.42f,SaveSystem.Data.Volume,.001f);Assert.AreEqual(1,backs);
            SaveSystem.Load();Assert.AreEqual(0,SaveSystem.Data.Coins);
        }
        [Test]public void SettingsButtonsStayInsidePanelAndToggleThumbSlidesToItsNewState(){
            ReferenceScreens.Settings(viewport,()=>{});
            foreach(var button in root.GetComponentsInChildren<Button>()){
                var rect=(RectTransform)button.transform;
                Assert.GreaterOrEqual(rect.anchorMin.y,0,button.name);Assert.LessOrEqual(rect.anchorMax.y,1,button.name);
            }
            var toggle=root.GetComponentInChildren<IslandToggleMotion>();
            var thumb=toggle.transform.GetChild(0) as RectTransform;float before=thumb.anchorMin.x;
            toggle.GetComponentInParent<Button>().onClick.Invoke();toggle.Tick(.05f);
            Assert.AreNotEqual(before,thumb.anchorMin.x);Assert.Greater(thumb.anchorMin.x,.06f);Assert.Less(thumb.anchorMin.x,.57f);
            toggle.Tick(1);Assert.IsFalse(toggle.enabled);Assert.AreEqual(before>.3f?.06f:.57f,thumb.anchorMin.x,.001f);
        }
        [Test]public void ResetConfirmationIsolatesSettingsAndRestoresKeyboardFocusOnCancel(){
            var events=UIInputRouter.EnsureForScene(scene);ReferenceScreens.Settings(viewport,()=>{});
            var reset=FindButton("СБРОСИТЬ ПРОГРЕСС");events.SetSelectedGameObject(reset.gameObject);
            reset.onClick.Invoke();Assert.IsFalse(reset.IsInteractable(),"Keyboard navigation must not reach settings behind a modal.");
            var cancel=FindButton("ОТМЕНА");Assert.IsTrue(cancel.IsInteractable());cancel.onClick.Invoke();
            Assert.IsTrue(reset.IsInteractable());Assert.AreSame(reset.gameObject,events.currentSelectedGameObject);
        }
        [Test]public void VitalsHaveReadableWidthAndDoNotOverlapTheWavePanel(){
            var uiObject=new GameObject("HUD regression");uiObject.transform.SetParent(root.transform,false);var ui=uiObject.AddComponent<UIManager>();ui.Initialize();
            RectTransform vitals=null,wave=null;
            foreach(var rect in uiObject.GetComponentsInChildren<RectTransform>(true)){
                if(rect.name=="Здоровье и выносливость")vitals=rect;
                if(rect.name=="Компактная волна")wave=rect;
            }
            Assert.IsNotNull(vitals);Assert.IsNotNull(wave);
            Assert.Greater(vitals.anchorMax.x-vitals.anchorMin.x,.23f,"Health and stamina labels need more than a thin sliver of the screen.");
            Assert.Less(vitals.anchorMax.x,wave.anchorMin.x);
        }
        [Test]public void MobileHoldIgnoresForeignFingerAndThrowTapKeepsOneFrameEdge(){
            MobileInput.Clear();var go=new GameObject("Touch regression");go.transform.SetParent(root.transform,false);var held=go.AddComponent<MobileHoldButton>();
            var first=new UnityEngine.EventSystems.PointerEventData(null){pointerId=10};var second=new UnityEngine.EventSystems.PointerEventData(null){pointerId=11};
            held.Action=0;held.OnPointerDown(first);held.OnPointerDown(second);held.OnPointerUp(second);Assert.IsTrue(MobileInput.Attack);
            held.OnPointerUp(first);Assert.IsFalse(MobileInput.Attack);
            held.Action=2;held.OnPointerDown(first);held.OnPointerUp(first);Assert.IsFalse(MobileInput.Throw);Assert.IsTrue(MobileInput.ThrowPressed,"A brief tap must survive until PlayerController consumes its edge.");
            MobileInput.Clear();Assert.IsFalse(MobileInput.ThrowPressed);Assert.IsFalse(MobileInput.Throw);
        }
        [Test]public void MainMenuOffersOnePlayActionWithoutSavedRunOrNewGameButtons(){
            new GameObject("Main menu").AddComponent<MenuBootstrap>();
            var labels=new List<string>();
            foreach(var menu in Object.FindObjectsByType<MenuBootstrap>(FindObjectsSortMode.None))foreach(var button in menu.GetComponentsInChildren<Button>())labels.Add(button.GetComponentInChildren<Text>().text);
            CollectionAssert.AreEquivalent(new[]{"ИГРАТЬ","СКИНЫ","РЕКОРДЫ","НАСТРОЙКИ","ВЫХОД"},labels);
            Assert.IsFalse(labels.Contains("МАГАЗИН"));
            foreach(var menu in Object.FindObjectsByType<MenuBootstrap>(FindObjectsSortMode.None))foreach(var text in menu.GetComponentsInChildren<Text>())Assert.IsFalse(text.text.Contains("МОНЕТ"),"The menu must not display an attempt wallet.");
            Assert.IsFalse(labels.Contains("ПРОДОЛЖИТЬ"));Assert.IsFalse(labels.Contains("НОВАЯ ИГРА"));
        }
    }
}
