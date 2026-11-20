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
        [UnityTest]public IEnumerator NormalAttackDoesNotLaunchAndRightButtonIsSuppressedAcrossPause(){
            Assert.IsTrue(game.Player.Combat.ExecuteAttack(false));Assert.IsFalse(game.Player.Combat.BoomerangActive);
            yield return new WaitForSeconds(game.Player.Stats.AttackInterval+.05f);
            game.Player.Combat.UpdateBoomerangInput(false,false);game.Player.Combat.UpdateBoomerangInput(true,true);
            Assert.IsTrue(game.Player.Combat.BoomerangActive);game.Pause();Assert.IsFalse(game.Player.Combat.BoomerangActive);
            game.Resume();game.Player.Combat.UpdateBoomerangInput(true,true);Assert.IsFalse(game.Player.Combat.BoomerangActive);
        }
        [UnityTest]public IEnumerator PiercingUpgradeHitsOncePerLegAndHasNoMeleeDoubleContact(){
            game.Economy.SetBalanceForTesting(40);Assert.IsTrue(game.Economy.Buy(4));
            var enemy=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+game.Player.AimForward*3);float before=enemy.Health,damage=game.Player.Stats.Damage;
            Assert.IsTrue(game.Player.Combat.ExecuteBoomerang());Assert.IsTrue(game.Player.Combat.BoomerangActive);Assert.IsFalse(game.Player.Combat.ExecuteBoomerang());Assert.IsFalse(game.Player.Combat.ExecuteAttack(true));
            yield return new WaitForSeconds(.14f+balance.Boomerang.OutSeconds*.9f);
            Assert.AreEqual(before-damage,enemy.Health,.01f,"No hidden normal melee contact may accompany the outward projectile.");
            yield return new WaitForSeconds(balance.Boomerang.ReturnSeconds+.15f);
            Assert.AreEqual(before-2*damage,enemy.Health,.01f);Assert.IsFalse(game.Player.Combat.BoomerangActive);Assert.IsFalse(enemy.PhysicsBody.IsPhysical);
        }
        [UnityTest]public IEnumerator DefaultThrowBouncesAtNearestTargetAndDoesNotDamageBehindIt(){
            var direction=game.Player.AimForward;var origin=game.Player.transform.position;
            // Deliberately put the farther body last: active-list traversal must not choose it first.
            var nearest=game.Spawner.Spawn(EnemyKind.Normal,origin+direction*2.2f);
            var farther=game.Spawner.Spawn(EnemyKind.Normal,origin+direction*4.6f);
            float nearHealth=nearest.Health,farHealth=farther.Health;
            Assert.IsTrue(game.Player.Combat.ExecuteBoomerang());
            yield return new WaitForSeconds(.14f+balance.Boomerang.OutSeconds+balance.Boomerang.ReturnSeconds+.25f);
            Assert.AreEqual(nearHealth-game.Player.Stats.Damage,nearest.Health,.01f);
            Assert.AreEqual(farHealth,farther.Health,.01f,"The basic bat must not hit through the first body.");
            Assert.IsFalse(game.Player.Combat.BoomerangActive);
        }
        [UnityTest]public IEnumerator CooldownStartsWithAcceptedThrowAndSurvivesPauseUntilNewRun(){
            Assert.IsTrue(game.Player.Combat.ExecuteBoomerang());
            Assert.Greater(game.Player.Combat.BoomerangCooldownRemaining,2.8f);
            game.Pause();Assert.IsFalse(game.Player.Combat.BoomerangActive);
            float remaining=game.Player.Combat.BoomerangCooldownRemaining;
            yield return new WaitForSecondsRealtime(.05f);
            Assert.AreEqual(remaining,game.Player.Combat.BoomerangCooldownRemaining,.001f);
            game.Resume();Assert.IsFalse(game.Player.Combat.ExecuteBoomerang());
            game.Player.Combat.ResetRun();Assert.AreEqual(0,game.Player.Combat.BoomerangCooldownRemaining);
            Assert.IsTrue(game.Player.Combat.ExecuteBoomerang());
        }
        [UnityTest]public IEnumerator AReturnedBatAllowsMeleeWhileOnlyTheThrowIsCooling(){
            balance.Boomerang.CooldownSeconds=3;
            Assert.IsTrue(game.Player.Combat.ExecuteBoomerang());
            yield return new WaitForSeconds(.14f+balance.Boomerang.OutSeconds+balance.Boomerang.ReturnSeconds+.2f);
            Assert.IsFalse(game.Player.Combat.BoomerangActive);
            Assert.Greater(game.Player.Combat.BoomerangCooldownRemaining,0);
            Assert.IsFalse(game.Player.Combat.ExecuteBoomerang());
            Assert.IsTrue(game.Player.Combat.ExecuteAttack(false),"The throw cooldown must not disable ordinary melee after the catch.");
        }
        [Test]public void SweptEntryUsesTheBodyBoundary(){
            Assert.IsTrue(BatBoomerang.SegmentEntry(Vector3.zero,Vector3.forward*10,Vector3.forward*5,1,out float entry));
            Assert.AreEqual(.4f,entry,.0001f);
            Assert.IsTrue(BatBoomerang.SegmentEntry(Vector3.zero,Vector3.forward*10,Vector3.forward*2,1,out float near));
            Assert.Less(near,entry);
        }
        [UnityTest]public IEnumerator CancellationAndPauseRestoreTheHeldBatAndStopContacts(){
            var enemy=game.Spawner.Spawn(EnemyKind.Normal,game.Player.transform.position+game.Player.AimForward*6);float health=enemy.Health;
            Assert.IsTrue(game.Player.Combat.ExecuteBoomerang());game.Pause();Assert.IsFalse(game.Player.Combat.BoomerangActive);
            yield return null;Assert.AreEqual(health,enemy.Health);
            game.Resume();game.Player.Combat.ResetRun();Assert.IsTrue(game.Player.Combat.ExecuteBoomerang());game.Player.ResetRun();
            Assert.IsFalse(game.Player.Combat.BoomerangActive);Assert.AreEqual(0,game.Player.Combat.BoomerangRemaining);
            yield return null;
        }
    }
}
