using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Brainrot.Tests
{
    public sealed class UIRefreshTests
    {
        Scene previous,scene;
        GameManager game;
        GameBalance profile;
        UIManager ui;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Запускайте тесты из режима редактирования.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("HUD "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;game.Balance=profile;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
            ui=game.GetComponent<UIManager>();ui.Refresh();
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);MobileInput.Clear();Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
        }
        Text LabelStartingWith(string prefix){
            foreach(var text in ui.GetComponentsInChildren<Text>(true))if(text.text.StartsWith(prefix,System.StringComparison.Ordinal))return text;
            Assert.Fail("В HUD нет подписи: "+prefix);return null;
        }
        [UnityTest]public IEnumerator CachedHudStillUpdatesChangedVitalsWaveAndCooldown(){
            Text health=LabelStartingWith("ЗДОРОВЬЕ"),stamina=LabelStartingWith("ВЫНОСЛИВОСТЬ"),wave=LabelStartingWith("ВОЛНА ");
            Canvas.ForceUpdateCanvases();
            var glyphs=new TextGenerator();glyphs.Populate(wave.text,wave.GetGenerationSettings(wave.rectTransform.rect.size));
            Assert.Greater(glyphs.characterCountVisible,0,"Заголовок волны должен помещаться и создавать видимые символы.");
            game.Player.TakeDamage(20,game.Player.transform.position-Vector3.right);
            game.Player.Stamina.Tick(true,true,false,1,profile.Stamina);
            Assert.IsTrue(game.Player.Combat.ExecuteAttack(true));ui.Refresh();
            StringAssert.Contains("80 / 100",health.text);StringAssert.Contains("75 / 100",stamina.text);
            Assert.IsNotNull(LabelStartingWith("10 с"));
            game.Spawner.Waves.Advance(profile.Waves.Duration,0);ui.Refresh();Assert.IsNotNull(LabelStartingWith("ПЕРЕДЫШКА"));
            game.Spawner.Waves.Advance(profile.Waves.Rest,0);ui.Refresh();Assert.AreEqual("ВОЛНА 2",wave.text);
            yield return null;
        }
#if UNITY_EDITOR
        [UnityTest]public IEnumerator UnchangedHudDoesNotAllocateFormattedStringsOnEveryRefresh(){
            // Same-frame refreshes freeze the displayed inputs and isolate HUD formatting from world simulation.
            // This detects the former allocations even though Text itself ignored equal replacement strings.
            for(int i=0;i<8;i++)ui.Refresh();
            System.GC.GetAllocatedBytesForCurrentThread();
            long before=System.GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<128;i++)ui.Refresh();
            long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.Less(allocated,4096,"Стабильный HUD не должен создавать новые строки для каждого вызова Refresh.");
            yield return null;
        }
#endif
    }
}
