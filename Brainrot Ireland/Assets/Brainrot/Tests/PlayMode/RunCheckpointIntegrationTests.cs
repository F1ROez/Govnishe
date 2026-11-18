using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class RunCheckpointIntegrationTests
    {
        Scene previous,first,second;
        GameBalance tuning;System.IDisposable storage;string folder;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Запускайте PlayMode тесты из режима редактирования.");
            folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"AttemptTests-"+System.Guid.NewGuid());storage=SaveSystem.UseStorageDirectory(folder);
            previous=SceneManager.GetActiveScene();tuning=ScriptableObject.CreateInstance<GameBalance>();tuning.Waves.InitialDelay=600;
            first=SceneManager.CreateScene("Сохранение "+System.Guid.NewGuid());SceneManager.SetActiveScene(first);yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(first.IsValid()&&first.isLoaded)yield return SceneManager.UnloadSceneAsync(first);
            if(second.IsValid()&&second.isLoaded)yield return SceneManager.UnloadSceneAsync(second);
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            if(tuning!=null)Object.Destroy(tuning);storage.Dispose();if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);MobileInput.Clear();Time.timeScale=1;AudioListener.pause=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
        }
        GameManager NewGame(){var go=new GameObject("Игра");go.SetActive(false);var game=go.AddComponent<GameManager>();game.Balance=tuning;go.SetActive(true);return game;}
        [UnityTest]public IEnumerator LeavingAttemptKeepsPermanentPurchasesButNeverRestoresEnemiesOrHealth(){
            var game=NewGame();yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
            string oldId=game.CoinRunId;game.Player.TakeDamage(20,Vector3.right*20);game.Reward(0,7);
            Assert.IsTrue(MetaProgression.Award(oldId,900,100));Assert.IsTrue(MetaProgression.Buy(1));
            Assert.AreEqual(100,game.Player.Stats.MaxHealth,"Покупка применяется к следующей попытке.");
            game.Spawner.Spawn(EnemyKind.Normal,EnemySpawner.OceanSpawnPoint(.4f));
            Assert.IsFalse(game.SaveCheckpoint());Assert.IsFalse(RunPersistence.HasSavedRun);
            game.EndRun();Assert.IsFalse(game.HasRun);Assert.IsFalse(game.CanContinue);Assert.IsFalse(game.ResumeRun());Assert.AreEqual(0,game.Spawner.Active.Count);
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            yield return SceneManager.UnloadSceneAsync(first);Assert.IsNull(GameManager.Instance);
            second=SceneManager.CreateScene("Новая попытка "+System.Guid.NewGuid());SceneManager.SetActiveScene(second);
            var next=NewGame();yield return null;next.enabled=false;next.Player.enabled=false;
            Assert.IsFalse(next.RestoreSavedRun());Assert.IsFalse(next.HasRun);next.StartRun();
            Assert.AreNotEqual(oldId,next.CoinRunId);Assert.AreEqual(115,next.Player.Health);Assert.AreEqual(0,next.Kills);Assert.AreEqual(1,next.Wave);
            Assert.AreEqual(50,SaveSystem.Data.Coins);Assert.AreEqual(1,MetaProgression.Rank(1));
        }
    }
}
