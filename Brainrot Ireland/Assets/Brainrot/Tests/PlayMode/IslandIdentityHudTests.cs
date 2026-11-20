using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
namespace Brainrot.Tests
{
    public sealed class IslandIdentityHudTests
    {
        Scene previous,scene;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Island identity "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);}
        [Test]public void LogoIsASeparateWoodenCompositionAndWalletHasCoinArt(){
            var root=new GameObject("Identity",typeof(RectTransform));root.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
            ReferenceScreens.BuildLogo(root.transform);var value=ReferenceScreens.Wallet(root.transform,.7f,.04f,.2f,.07f);
            Assert.AreEqual("0",value.text);
            bool coin=false,wood=false,title=false;
            foreach(var image in root.GetComponentsInChildren<Image>()){coin|=image.sprite==TropicalUIArt.Get("IslandCoin");wood|=image.sprite==IslandThemeSurface.Wood;Assert.IsFalse(image.raycastTarget);}
            foreach(var text in root.GetComponentsInChildren<Text>())title|=text.text=="ТУН ТУН САХУРА";
            Assert.IsTrue(coin);Assert.IsTrue(wood);Assert.IsTrue(title);
        }
        [Test]public void HudHasThreeSquareIllustratedEmblemsAndIndependentKeycaps(){
            var root=new GameObject("HUD identity");var ui=root.AddComponent<UIManager>();ui.Initialize();
            var emblems=root.GetComponentsInChildren<AspectRatioFitter>(true);Assert.AreEqual(3,emblems.Length);
            foreach(var emblem in emblems){Assert.AreEqual(1,emblem.aspectRatio);Assert.AreEqual(AspectRatioFitter.AspectMode.FitInParent,emblem.aspectMode);}
            int bats=0,coins=0;bool hold=false,dash=false,throwKey=false;
            foreach(var image in root.GetComponentsInChildren<Image>(true)){if(image.name=="Иллюстрация биты")bats++;if(image.name=="Монета HUD")coins++;}
            foreach(var text in root.GetComponentsInChildren<Text>(true)){hold|=text.text=="ЛКМ — держать";dash|=text.text=="ПРОБЕЛ";throwKey|=text.text=="ПКМ";}
            Assert.AreEqual(2,bats);Assert.AreEqual(1,coins);Assert.IsTrue(hold&&dash&&throwKey);
            var boss=root.GetComponentInChildren<IslandBossHud>(true);Assert.IsNotNull(boss);Assert.IsFalse(boss.gameObject.activeSelf);boss.Present(null);Assert.IsFalse(boss.gameObject.activeSelf);
        }
    }
}
