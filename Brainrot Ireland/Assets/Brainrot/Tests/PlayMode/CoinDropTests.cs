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
        [Test]public void CoinRestorationDoesNotPayAgainAndWalletSurvivesDeath(){
            game.Reward(0,1);game.Coins.Drop(EnemyKind.BlueberrinniOctopusini,game.Player.transform.position,1);
            var saved=game.Coins.Capture();Assert.AreEqual(3,saved[0].Amount);
            for(int i=0;i<30;i++)game.Coins.Tick(.1f,game.Player);
            Assert.AreEqual(3,SaveSystem.Data.Coins);Assert.AreEqual(3,game.RunCoins);
            game.Coins.Restore(saved);Assert.AreEqual(0,game.Coins.ActiveCount);
            game.Die();SaveSystem.Load();Assert.AreEqual(3,SaveSystem.Data.Coins);
        }
        [Test]public void PurchasingWhileSuspendedDoesNotChangeContinuedRunStats(){
            var before=game.Player.Stats.Copy();game.SaveCheckpoint();
            Assert.IsTrue(MetaProgression.Award(game.CoinRunId,1,100));Assert.IsTrue(MetaProgression.Buy(1));
            Assert.IsTrue(game.RestoreSavedRun());Assert.AreEqual(before.MaxHealth,game.Player.Stats.MaxHealth);
            game.StartRun();Assert.AreEqual(before.MaxHealth+15,game.Player.Stats.MaxHealth);Assert.AreEqual(50,SaveSystem.Data.Coins);
        }
        [Test]public void CheckpointRejectsDuplicateCoinIdsAndInvalidStats(){
            var snapshot=new RunSnapshot{CoinRunId=game.CoinRunId,Kills=1,Player=game.Player.CaptureRunState(),Wave=new WaveRunSnapshot(),Spawner=new SpawnerRunSnapshot(),Coins=new[]{new CoinDropSnapshot{Id=1,Amount=1,Position=Vector3.zero}}};
            Assert.IsTrue(RunSaveStore.IsValid(snapshot));snapshot.Coins=new[]{snapshot.Coins[0],snapshot.Coins[0]};Assert.IsFalse(RunSaveStore.IsValid(snapshot));
            snapshot.Coins=Array.Empty<CoinDropSnapshot>();snapshot.Player.StartingStats.Damage=float.NaN;Assert.IsFalse(RunSaveStore.IsValid(snapshot));
        }
    }
}
