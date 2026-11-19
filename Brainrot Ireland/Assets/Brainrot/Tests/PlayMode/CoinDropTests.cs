using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class CoinDropTests
    {
        IDisposable storage;string folder;GameManager game;GameBalance profile;Scene scene,previous;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);folder=Path.Combine(Path.GetTempPath(),"CoinTests-"+Guid.NewGuid());storage=SaveSystem.UseStorageDirectory(folder);
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Монеты "+Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();profile.Waves.InitialDelay=600;game.Balance=profile;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(GameMusic.Instance!=null)UnityEngine.Object.Destroy(GameMusic.Instance.gameObject);
            if(GameAudio.Instance!=null)UnityEngine.Object.Destroy(GameAudio.Instance.gameObject);
            UnityEngine.Object.Destroy(profile);storage.Dispose();if(Directory.Exists(folder))Directory.Delete(folder,true);Time.timeScale=1;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
        }
        [Test]public void EnemyRewardProducesSeparateUnitCoinsAndRepeatedDropDoesNotDuplicate(){
            game.Reward(0,1);game.Coins.Drop(EnemyKind.Normal,Vector3.zero,game.Kills);Assert.AreEqual(1,game.Coins.ActiveCount);
            game.Reward(0,1);game.Coins.Drop(EnemyKind.BlueberrinniOctopusini,Vector3.zero,game.Kills);Assert.AreEqual(4,game.Coins.ActiveCount);
            game.Reward(0,1);game.Coins.Drop(EnemyKind.Orcaladon,Vector3.zero,game.Kills);Assert.AreEqual(12,game.Coins.ActiveCount);
            game.Coins.Drop(EnemyKind.Orcaladon,Vector3.zero,game.Kills);Assert.AreEqual(12,game.Coins.ActiveCount);
            var ids=new System.Collections.Generic.HashSet<int>();
            foreach(var coin in game.Coins.Capture()){Assert.AreEqual(1,coin.Amount);Assert.IsTrue(ids.Add(coin.Id));}
            Assert.AreEqual(0,SaveSystem.Data.Coins,"Выпадение само по себе не пополняет кошелёк.");
        }
        [Test]public void EachPhysicalPickupAddsOneAndDeathDiscardsSpendableCoins(){
            profile.Coins.MagnetRadius=.8f;profile.Coins.PickupRadius=.2f;
            game.Reward(0,1);game.Coins.Drop(EnemyKind.BlueberrinniOctopusini,game.Player.transform.position,game.Kills);
            for(int i=0;i<30;i++)game.Coins.Tick(.1f,game.Player);
            Assert.AreEqual(1,game.RunBalance);Assert.AreEqual(2,game.Coins.ActiveCount);
            profile.Coins.MagnetRadius=3;
            for(int i=0;i<30;i++)game.Coins.Tick(.1f,game.Player);
            Assert.AreEqual(3,game.RunBalance);Assert.AreEqual(0,game.Coins.ActiveCount);Assert.AreEqual(3,game.RunCoins);
            game.Die();SaveSystem.Load();Assert.AreEqual(0,game.RunBalance);Assert.AreEqual(0,SaveSystem.Data.Coins);Assert.AreEqual(3,game.RunCoins);
        }
        [Test]public void PoolOverflowNeverBanksUncollectedCurrency(){
            profile.Coins.OrcaladonValue=100;
            for(int i=1;i<=6;i++){game.Reward(0,1);game.Coins.Drop(EnemyKind.Orcaladon,new Vector3(15,0,0),game.Kills);}
            Assert.AreEqual(CoinDrops.Capacity,game.Coins.ActiveCount);Assert.AreEqual(0,SaveSystem.Data.Coins);
            foreach(var coin in game.Coins.Capture())Assert.AreEqual(1,coin.Amount);
            game.Coins.Clear();Assert.AreEqual(0,game.Coins.ActiveCount);Assert.AreEqual(0,SaveSystem.Data.Coins);
        }
        [UnityTest]public IEnumerator ShopPurchaseAppliesImmediatelyButOnlyForThisAttempt(){
            var before=game.Player.Stats.Copy();
            Assert.IsTrue(SaveSystem.SetTestCoins(100));Assert.IsFalse(MetaProgression.Buy(1),"Purchases are unavailable during a live wave.");
            game.Spawner.Waves.RestoreRunState(new WaveRunSnapshot{Number=1,Time=profile.Waves.Duration,Resting=true});yield return null;game.Stall.Tick(1,false,false);
            var spot=game.Stall.WorldPosition;spot.y=0;var motor=game.Player.GetComponent<CharacterController>();motor.enabled=false;game.Player.transform.position=spot;motor.enabled=true;
            Assert.IsTrue(game.OpenRunShop());
            var shopBounds=new GameObject("Run shop test",typeof(RectTransform));shopBounds.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
            var viewport=ReferenceScreens.Viewport(shopBounds.transform,"Run shop");var shop=ShopScreen.ShowRun(viewport,()=>game.CloseRunShop());
            int id=game.Economy.OfferIds[0],price=game.Economy.Cost(id);
            UnityEngine.UI.Button buy=null;
            foreach(var button in shop.GetComponentsInChildren<UnityEngine.UI.Button>())if(button.name=="Купить улучшение "+id)buy=button;
            Assert.IsNotNull(buy);Assert.IsTrue(buy.interactable);buy.onClick.Invoke();
            Assert.AreEqual(100-price,game.RunBalance);Assert.AreEqual(1,MetaProgression.Rank(id));Assert.IsTrue(game.Economy.IsOfferSold(id));
            Assert.AreEqual(before.Damage+(id==0?5:0),game.Player.Stats.Damage,.001f);
            Assert.AreEqual(before.MaxHealth+(id==1?15:0),game.Player.Stats.MaxHealth,.001f);
            Assert.AreEqual(before.MoveSpeed+(id==2?.3f:0),game.Player.Stats.MoveSpeed,.001f);
            Assert.AreEqual(before.DashCooldown-(id==3?.12f:0),game.Player.Stats.DashCooldown,.001f);
            UnityEngine.Object.DestroyImmediate(shopBounds);
            SaveSystem.Load();Assert.AreEqual(0,SaveSystem.Data.Coins);Assert.AreEqual(1,MetaProgression.Rank(id));
            game.CloseRunShop();Assert.IsTrue(game.StartNextWave());Assert.AreEqual(1,MetaProgression.Rank(id));
            game.StartRun();Assert.AreEqual(before.MaxHealth,game.Player.Stats.MaxHealth);Assert.AreEqual(before.Damage,game.Player.Stats.Damage);Assert.AreEqual(0,game.RunBalance);Assert.AreEqual(0,MetaProgression.Rank(id));
        }
        [UnityTest]public IEnumerator IntermissionDoesNotFarmSurvivalTime(){
            game.Spawner.Waves.RestoreRunState(new WaveRunSnapshot{Number=1,Time=profile.Waves.Duration,Resting=true});
            float before=game.Elapsed;game.enabled=true;yield return new WaitForSeconds(.08f);
            Assert.AreEqual(before,game.Elapsed,"Standing at the stall must not increase a survival record.");
            Assert.IsTrue(game.StartNextWave());yield return new WaitForSeconds(.08f);Assert.Greater(game.Elapsed,before);
            game.enabled=false;
        }
        [Test]public void UnitReceiptIdsNeverOverlapAcrossKills(){
            var ids=new System.Collections.Generic.HashSet<int>();
            for(int kill=1;kill<=10;kill++)for(int index=0;index<100;index++)Assert.IsTrue(ids.Add(CoinDrops.RewardId(kill,index)));
            Assert.AreEqual(0,CoinDrops.RewardId(0,0));Assert.AreEqual(0,CoinDrops.RewardId(1,100));
        }
    }
}
