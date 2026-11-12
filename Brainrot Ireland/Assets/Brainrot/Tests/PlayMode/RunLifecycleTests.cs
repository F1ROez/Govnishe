using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests {
 public sealed class RunLifecycleTests {
  GameObject root; GameManager game;
  [UnitySetUp] public IEnumerator Setup(){root=new GameObject("Test arena");game=root.AddComponent<GameManager>();yield return null;game.StartRun();}
  [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;foreach(var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))if(go.transform.parent==null)Object.Destroy(go);yield return null;}
  [UnityTest] public IEnumerator KillRewardsExperienceAndReturnsEnemyToPool(){var enemy=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*2);enemy.TakeDamage(9999,Vector3.zero);Assert.AreEqual(1,game.Kills);Assert.AreEqual(9,game.Upgrades.Experience);Assert.IsFalse(enemy.gameObject.activeSelf);Assert.AreEqual(0,game.Spawner.Active.Count);yield return null;}
  [UnityTest] public IEnumerator QueuedLevelsPauseUntilEveryChoiceIsApplied(){game.Upgrades.AddExperience(60);Assert.AreEqual(GameState.Upgrade,game.State);Assert.AreEqual(0,Time.timeScale);int selections=0;while(game.State==GameState.Upgrade&&selections++<10){Assert.Greater(game.Upgrades.Choices.Count,0);game.Upgrades.Choose(0);}Assert.AreEqual(GameState.Playing,game.State);Assert.AreEqual(4,game.Upgrades.Level);Assert.AreEqual(3,selections);yield return null;}
  [UnityTest] public IEnumerator PauseFreezesRunAndRestartResetsProgress(){game.Spawner.Spawn(EnemyKind.Tank,Vector3.forward*20);game.Pause();float elapsed=game.Elapsed;yield return null;yield return null;Assert.AreEqual(elapsed,game.Elapsed);game.StartRun();Assert.AreEqual(0,game.Spawner.Active.Count);Assert.AreEqual(1,game.Upgrades.Level);Assert.AreEqual(100,game.Player.Health);Assert.AreEqual(GameState.Playing,game.State);}
 }
}