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
        GameBalance tuning;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Запускайте PlayMode тесты из режима редактирования.");
            previous=SceneManager.GetActiveScene();tuning=ScriptableObject.CreateInstance<GameBalance>();tuning.Waves.InitialDelay=600;
            first=SceneManager.CreateScene("Сохранение "+System.Guid.NewGuid());SceneManager.SetActiveScene(first);yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(first.IsValid()&&first.isLoaded)yield return SceneManager.UnloadSceneAsync(first);
            if(second.IsValid()&&second.isLoaded)yield return SceneManager.UnloadSceneAsync(second);
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);
            if(tuning!=null)Object.Destroy(tuning);MobileInput.Clear();Time.timeScale=1;AudioListener.pause=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return null;
        }
        GameManager NewGame(){var go=new GameObject("Игра");go.SetActive(false);var game=go.AddComponent<GameManager>();game.Balance=tuning;go.SetActive(true);return game;}
        [UnityTest]public IEnumerator FreshWorldCanContinueDiskCheckpointAndDeathClearsIt(){
            var game=NewGame();yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
            game.Player.TakeDamage(20,Vector3.right*20);game.Reward(0,7);
            var enemy=game.Spawner.Spawn(EnemyKind.Normal,EnemySpawner.OceanSpawnPoint(.4f));Assert.IsNotNull(enemy);
            enemy.TakeDamage(5,Vector3.zero);float enemyHealth=enemy.Health;
            Assert.IsTrue(game.SaveCheckpoint());Assert.IsTrue(RunPersistence.HasSavedRun);
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            yield return SceneManager.UnloadSceneAsync(first);RunPersistence.Refresh();Assert.IsNull(GameManager.Instance);
            second=SceneManager.CreateScene("Продолжение "+System.Guid.NewGuid());SceneManager.SetActiveScene(second);
            var restored=NewGame();yield return null;restored.enabled=false;restored.Player.enabled=false;
            Assert.IsFalse(restored.HasRun);Assert.IsTrue(restored.CanContinue);Assert.IsTrue(restored.ResumeRun());
            Assert.AreEqual(80,restored.Player.Health,.01f);Assert.AreEqual(7,restored.Kills);Assert.AreEqual(1,restored.Spawner.Active.Count);
            Assert.AreEqual(enemyHealth,restored.Spawner.Active[0].Health,.01f);Assert.AreEqual(GameState.Playing,restored.State);
            restored.Die();RunPersistence.Refresh();Assert.IsFalse(RunPersistence.HasSavedRun);Assert.IsFalse(restored.CanContinue);
        }
    }
}
