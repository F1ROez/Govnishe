using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class BoomerangTests
    {
        GameManager game;GameBalance balance;Scene scene,previous;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance);previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Boomerang "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Game").AddComponent<GameManager>();balance=ScriptableObject.CreateInstance<GameBalance>();balance.Normal.Health=1000;balance.Normal.Damage=0;balance.Waves.InitialDelay=600;game.Balance=balance;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;game.Player.Stats.CritChance=0;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(balance!=null)Object.Destroy(balance);yield return null;
        }
        [Test]public void SweptContactsDoNotSkipTargetsBetweenFrames(){
            Assert.IsTrue(BatBoomerang.SegmentHits(Vector3.zero,Vector3.forward*7,Vector3.forward*3,.5f));
            Assert.IsFalse(BatBoomerang.SegmentHits(Vector3.zero,Vector3.forward*7,Vector3.forward*3+Vector3.right,.5f));
        }
        [UnityTest]public IEnumerator AThrowHitsOncePerLegAndHasNoMeleeDoubleContact(){
            var enemy=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+game.Player.AimForward*3);float before=enemy.Health,damage=game.Player.Stats.Damage;
            Assert.IsTrue(game.Player.Combat.ExecuteAttack(false));Assert.IsTrue(game.Player.Combat.BoomerangActive);Assert.IsFalse(game.Player.Combat.ExecuteAttack(false));Assert.IsFalse(game.Player.Combat.ExecuteAttack(true));
            yield return new WaitForSeconds(balance.Boomerang.OutSeconds*.9f);
            Assert.AreEqual(before-damage,enemy.Health,.01f,"No hidden normal melee contact may accompany the outward projectile.");
            yield return new WaitForSeconds(balance.Boomerang.ReturnSeconds+.15f);
            Assert.AreEqual(before-2*damage,enemy.Health,.01f);Assert.IsFalse(game.Player.Combat.BoomerangActive);Assert.IsFalse(enemy.PhysicsBody.IsPhysical);
        }
        [UnityTest]public IEnumerator CancellationAndPauseRestoreTheHeldBatAndStopContacts(){
            var enemy=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+game.Player.AimForward*6);float health=enemy.Health;
            Assert.IsTrue(game.Player.Combat.ExecuteAttack(false));game.Pause();Assert.IsFalse(game.Player.Combat.BoomerangActive);
            yield return null;Assert.AreEqual(health,enemy.Health);
            game.Resume();game.Player.Combat.ResetRun();Assert.IsTrue(game.Player.Combat.ExecuteAttack(false));game.Player.ResetRun();
            Assert.IsFalse(game.Player.Combat.BoomerangActive);Assert.AreEqual(0,game.Player.Combat.BoomerangRemaining);
            yield return null;
        }
    }
}
