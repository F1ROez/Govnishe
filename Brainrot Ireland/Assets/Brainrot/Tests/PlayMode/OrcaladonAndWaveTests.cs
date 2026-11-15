using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class OrcaladonAndWaveTests
    {
        Scene previous,scene;
        GameManager game;
        GameBalance profile;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Запускайте тесты из Edit mode.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Оркаладон "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();
            profile.Waves.InitialDelay=600;profile.Waves.Rest=0;profile.Player.CritChance=0;game.Balance=profile;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;game.Player.transform.position=Vector3.zero;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);yield return null;
        }
        EnemyController Orca(Vector3 position){var enemy=game.Spawner.Spawn(EnemyKind.Orcaladon,position);Assert.IsNotNull(enemy);return enemy;}
        void FinishWave(){game.Spawner.Waves.Advance(profile.Waves.Duration,0);game.Spawner.Waves.Advance(.01f,0);}

        [UnityTest]public IEnumerator ChargeLocksAimAndHitsOnlyOnceAcrossALargeFrame(){
            profile.Orcaladon.ChargeWindup=.2f;profile.Orcaladon.ChargeSpeed=20;profile.Orcaladon.ChargeDuration=1;
            profile.Orcaladon.SlamRange=2;profile.Orcaladon.ChargeMinDistance=3;profile.Orcaladon.ChargeMaxDistance=20;
            var enemy=Orca(Vector3.back*10);var attack=enemy.GetComponent<OrcaladonAttack>();
            enemy.Tick(.01f);Assert.AreEqual(OrcaladonPhase.ChargeAim,attack.Phase);Assert.AreEqual(100,game.Player.Health);
            var direction=attack.CommittedDirection;game.Player.transform.position=Vector3.right*6;
            enemy.Tick(.3f);Assert.AreEqual(OrcaladonPhase.Charging,attack.Phase);Assert.AreEqual(direction,attack.CommittedDirection);
            Assert.That(Vector3.Distance(enemy.transform.position,Vector3.back*10),Is.LessThan(.001f),"Подготовка должна останавливать врага.");
            game.Player.transform.position=Vector3.zero;enemy.Tick(.8f);
            Assert.AreEqual(70,game.Player.Health,"Рывок должен проверять весь пройденный отрезок.");
            yield return new WaitForSeconds(.55f);game.Player.transform.position=Vector3.forward*7;
            enemy.Tick(.1f);Assert.AreEqual(70,game.Player.Health,"Один разгон не наносит повторный урон.");
            Assert.Greater(attack.ChargeCooldownRemaining,0);Assert.AreEqual(0,attack.SlamCooldownRemaining);
            yield return null;
        }
        [UnityTest]public IEnumerator GroundSlamWarnsBeforeCircularDamageAndCanBeDodged(){
            profile.Orcaladon.SlamWindup=.5f;profile.Orcaladon.SlamRange=3.8f;
            var enemy=Orca(Vector3.zero);var attack=enemy.GetComponent<OrcaladonAttack>();game.Player.transform.position=Vector3.right*2;
            enemy.Tick(.01f);Assert.AreEqual(OrcaladonPhase.SlamWindup,attack.Phase);Assert.AreEqual(100,game.Player.Health);
            enemy.Tick(.25f);Assert.AreEqual(100,game.Player.Health);Assert.AreEqual(Vector3.zero,enemy.transform.position);
            game.Player.transform.position=Vector3.left*2;enemy.Tick(.3f);
            Assert.AreEqual(OrcaladonPhase.SlamImpact,attack.Phase);Assert.AreEqual(70,game.Player.Health,"Ближняя атака поражает круг, включая сторону за спиной.");
            yield return new WaitForSeconds(.55f);enemy.Tick(.1f);Assert.AreEqual(70,game.Player.Health,"Удар не повторяет урон после окончания неуязвимости игрока.");
            enemy.Tick(.3f);Assert.AreEqual(70,game.Player.Health);Assert.Greater(attack.SlamCooldownRemaining,0);Assert.AreEqual(0,attack.ChargeCooldownRemaining);
            enemy.TakeDamage(9999,Vector3.zero);game.Spawner.ReleaseCorpse(enemy);
            game.Player.Heal(999);
            var next=Orca(Vector3.zero);game.Player.transform.position=Vector3.right*2;next.Tick(.01f);
            game.Player.transform.position=Vector3.forward*7;next.Tick(.6f);Assert.AreEqual(100,game.Player.Health,"Из круга предупреждения можно выйти до удара.");
            yield return null;
        }
        [UnityTest]public IEnumerator HeavyHitOnlyNudgesLivingOrcaButDeathAndPoolingResetCorrectly(){
            var enemy=Orca(Vector3.zero);Assert.AreEqual(100,enemy.Health);var attack=enemy.GetComponent<OrcaladonAttack>();
            enemy.TakeDamage(10,Vector3.right*11);Assert.AreEqual(90,enemy.Health);Assert.IsFalse(enemy.PhysicsBody.IsPhysical);
            enemy.Tick(.1f);Assert.That(enemy.transform.position.x,Is.InRange(.01f,.8f));Assert.IsFalse(enemy.PhysicsBody.IsPhysical);
            enemy.TakeDamage(9999,Vector3.right*11);Assert.IsFalse(enemy.Alive);Assert.IsTrue(enemy.PhysicsBody.IsPhysical);Assert.Contains(enemy,game.Spawner.Corpses);
            game.Spawner.ReleaseCorpse(enemy);var position=EnemySpawner.OceanSpawnPoint(.9f);var reused=Orca(position);
            Assert.AreSame(enemy,reused);Assert.IsTrue(reused.Alive);Assert.AreEqual(100,reused.Health);Assert.IsFalse(reused.PhysicsBody.IsPhysical);
            Assert.AreEqual(OrcaladonPhase.Approach,attack.Phase);Assert.AreEqual(0,attack.ChargeCooldownRemaining);Assert.AreEqual(0,attack.SlamCooldownRemaining);
            Assert.That(Vector3.Distance(position,reused.transform.position),Is.LessThan(.001f));yield return null;
        }
        [UnityTest]public IEnumerator TinyCustomSlamRadiusStillReachesThePlayersBodySurface(){
            profile.Orcaladon.SlamRange=.5f;profile.Orcaladon.CollisionRadius=1.15f;
            var enemy=Orca(Vector3.zero);game.Player.transform.position=Vector3.right*1.68f;
            enemy.Tick(.01f);Assert.AreEqual(OrcaladonPhase.SlamWindup,enemy.GetComponent<OrcaladonAttack>().Phase);
            enemy.Tick(profile.Orcaladon.SlamWindup+.01f);Assert.AreEqual(70,game.Player.Health);
            yield return null;
        }
        [UnityTest]public IEnumerator NewcomersOpenTheirUnlockWaveEvenWithZeroRandomSpawnChance(){
            profile.Octopus.SpawnChance=profile.Orcaladon.SpawnChance=0;
            game.Spawner.Tick(.01f);Assert.AreEqual(0,game.Spawner.Active.Count);FinishWave();Assert.AreEqual(2,game.Spawner.Waves.Number);
            game.Spawner.Tick(.01f);Assert.AreEqual(1,game.Spawner.Active.Count);var octopus=game.Spawner.Active[0];Assert.AreEqual(EnemyKind.BlueberrinniOctopusini,octopus.Kind);
            Assert.IsFalse(IslandMap.IsWalkable(octopus.transform.position,0));octopus.TakeDamage(9999,Vector3.zero);
            FinishWave();Assert.AreEqual(3,game.Spawner.Waves.Number);game.Spawner.Tick(.01f);
            Assert.AreEqual(1,game.Spawner.Active.Count);Assert.AreEqual(EnemyKind.Orcaladon,game.Spawner.Active[0].Kind);
            Assert.IsFalse(IslandMap.IsWalkable(game.Spawner.Active[0].transform.position,0));yield return null;
        }
        [UnityTest]public IEnumerator FullSlotCannotLoseTheQueuedIntroductionAndLivingEnemyBlocksNextWave(){
            profile.Waves.MaxDesktop=profile.Waves.MaxMobile=1;profile.Waves.EdgeAliveLimit=profile.Waves.PeakAliveLimit=1;
            game.Spawner.Tick(.01f);FinishWave();var blocker=game.Spawner.Spawn(EnemyKind.Normal,EnemySpawner.OceanSpawnPoint(0));
            game.Spawner.Tick(.01f);Assert.AreEqual(EnemyKind.Normal,game.Spawner.Active[0].Kind);
            blocker.TakeDamage(9999,Vector3.zero);game.Spawner.Tick(.01f);Assert.AreEqual(EnemyKind.BlueberrinniOctopusini,game.Spawner.Active[0].Kind);
            game.Spawner.Waves.Advance(profile.Waves.Duration,game.Spawner.Active.Count);
            game.Spawner.Waves.Advance(99,game.Spawner.Active.Count);Assert.AreEqual(2,game.Spawner.Waves.Number);Assert.IsTrue(game.Spawner.Waves.WaitingForClear);
            yield return null;
        }
    }
}
