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
        [Test]public void EachPhysicalPickupAddsOneAndWalletSurvivesDeath(){
            profile.Coins.MagnetRadius=.8f;profile.Coins.PickupRadius=.2f;
            game.Reward(0,1);game.Coins.Drop(EnemyKind.BlueberrinniOctopusini,game.Player.transform.position,game.Kills);
            for(int i=0;i<30;i++)game.Coins.Tick(.1f,game.Player);
            Assert.AreEqual(1,SaveSystem.Data.Coins);Assert.AreEqual(2,game.Coins.ActiveCount);
            profile.Coins.MagnetRadius=3;
            for(int i=0;i<30;i++)game.Coins.Tick(.1f,game.Player);
            Assert.AreEqual(3,SaveSystem.Data.Coins);Assert.AreEqual(0,game.Coins.ActiveCount);Assert.AreEqual(3,game.RunCoins);
            game.Die();SaveSystem.Load();Assert.AreEqual(3,SaveSystem.Data.Coins);
        }
        [Test]public void PoolOverflowNeverBanksUncollectedCurrency(){
            profile.Coins.OrcaladonValue=100;
            for(int i=1;i<=6;i++){game.Reward(0,1);game.Coins.Drop(EnemyKind.Orcaladon,new Vector3(15,0,0),game.Kills);}
            Assert.AreEqual(CoinDrops.Capacity,game.Coins.ActiveCount);Assert.AreEqual(0,SaveSystem.Data.Coins);
            foreach(var coin in game.Coins.Capture())Assert.AreEqual(1,coin.Amount);
            game.Coins.Clear();Assert.AreEqual(0,game.Coins.ActiveCount);Assert.AreEqual(0,SaveSystem.Data.Coins);
        }
        [Test]public void PermanentPurchaseAppliesOnEveryNewRunWithoutStacking(){
            var before=game.Player.Stats.Copy();
            SaveSystem.Data.Coins=100;Assert.IsTrue(SaveSystem.TrySave());Assert.IsTrue(MetaProgression.Buy(1));
            Assert.AreEqual(before.MaxHealth,game.Player.Stats.MaxHealth);
            game.StartRun();Assert.AreEqual(before.MaxHealth+15,game.Player.Stats.MaxHealth);Assert.AreEqual(50,SaveSystem.Data.Coins);
            game.StartRun();Assert.AreEqual(before.MaxHealth+15,game.Player.Stats.MaxHealth);Assert.AreEqual(50,SaveSystem.Data.Coins);
        }
        [Test]public void UnitReceiptIdsNeverOverlapAcrossKills(){
            var ids=new System.Collections.Generic.HashSet<int>();
            for(int kill=1;kill<=10;kill++)for(int index=0;index<100;index++)Assert.IsTrue(ids.Add(CoinDrops.RewardId(kill,index)));
            Assert.AreEqual(0,CoinDrops.RewardId(0,0));Assert.AreEqual(0,CoinDrops.RewardId(1,100));
        }
    }
}
