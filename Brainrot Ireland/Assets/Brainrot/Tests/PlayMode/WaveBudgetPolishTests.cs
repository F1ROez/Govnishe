using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Brainrot.Tests
{
    /// <summary>Regression coverage for earned progress, deferred arrivals, checkpoint migration and attack slots.</summary>
    public sealed class WaveBudgetPolishTests
    {
        Scene previous,scene;
        GameManager game;
        GameBalance profile;
        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.IsNull(GameManager.Instance,"Запускайте тесты из Edit mode.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Бюджет волны "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Игра").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();
            profile.Waves.InitialDelay=0;profile.Waves.Duration=10;profile.Waves.SlowInterval=profile.Waves.PeakInterval=1;
            profile.Waves.Rest=0;profile.Player.CritChance=0;game.Balance=profile;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;game.Player.transform.position=Vector3.zero;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);yield return null;
        }
        [Test] public void CompletionIsEarnedAndCannotFinishWithAnActiveEnemy()
        {
            Assert.AreEqual(0,EnemySpawner.Completion(10,0,3,false));
            Assert.AreEqual(.5f,EnemySpawner.Completion(10,5,0,false),.0001f);
            Assert.Less(EnemySpawner.Completion(10,10,1,true),1);
            Assert.Less(EnemySpawner.Completion(10,10,0,false),1);
            Assert.AreEqual(1,EnemySpawner.Completion(10,10,0,true));
        }
        [Test] public void FinitePlanUsesTheSameEnvelopeAndNeverProducesUnboundedArrivals()
        {
            Assert.AreEqual(10,EnemySpawner.PlannedPopulation(profile.Waves,1));
            profile.Waves.Duration=3600;profile.Waves.SlowInterval=profile.Waves.PeakInterval=.15f;
            Assert.AreEqual(256,EnemySpawner.PlannedPopulation(profile.Waves,1));
            profile.Waves.InitialDelay=99999;Assert.AreEqual(1,EnemySpawner.PlannedPopulation(profile.Waves,1));
        }
        [UnityTest] public IEnumerator SaturatedSpawnSlotsDeferArrivalsAndCannotSkipTheClearGate()
        {
            profile.Waves.MaxDesktop=profile.Waves.MaxMobile=1;profile.Waves.EdgeAliveLimit=profile.Waves.PeakAliveLimit=1;
            game.Spawner.Tick(.01f);int planned=game.Spawner.WavePlanned;Assert.AreEqual(10,planned);
            Assert.AreEqual(1,game.Spawner.Active.Count);
            game.Spawner.Tick(11);Assert.AreEqual(1,game.Wave);Assert.IsFalse(game.Spawner.Waves.Resting);
            Assert.Less(game.Spawner.WaveCompletion,1);
            for(int i=0;i<planned+2&&game.Spawner.WaveSpawned<planned;i++){
                KillLiving();game.Spawner.Tick(1.01f);
            }
            Assert.AreEqual(planned,game.Spawner.WaveSpawned);Assert.Greater(game.Spawner.Active.Count,0);
            Assert.Less(game.Spawner.WaveCompletion,1);KillLiving();game.Spawner.Tick(.01f);
            Assert.IsTrue(game.Spawner.Waves.Resting);Assert.AreEqual(1,game.Spawner.WaveCompletion);Assert.AreEqual(0,game.Spawner.WaveRemaining);
            yield return null;
        }
        [UnityTest] public IEnumerator ResolvedProgressSurvivesRestoreAndRetreatCountsWithoutADeath()
        {
            var first=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*4);first.TakeDamage(9999,Vector3.zero);
            var octopus=game.Spawner.Spawn(EnemyKind.BlueberrinniOctopusini,Vector3.forward*28);
            typeof(OctopusAttack).GetProperty("Phase").SetValue(octopus.GetComponent<OctopusAttack>(),OctopusPhase.Retreat);
            octopus.Tick(.1f);Assert.AreEqual(2,game.Spawner.WaveResolved);Assert.AreEqual(0,game.Spawner.Active.Count);
            var saved=game.Spawner.CaptureRunState();float progress=game.Spawner.WaveCompletion;
            game.Spawner.RestoreRunState(saved);
            Assert.AreEqual(saved.PlannedCount,game.Spawner.WavePlanned);Assert.AreEqual(2,game.Spawner.WaveResolved);
            Assert.AreEqual(progress,game.Spawner.WaveCompletion,.0001f);yield return null;
        }
        [UnityTest] public IEnumerator LegacyRestCheckpointWithNoEnemiesMigratesToACompletedWave()
        {
            game.Spawner.Waves.RestoreRunState(new WaveRunSnapshot{Number=4,Time=10,Resting=true});
            game.Spawner.RestoreRunState(new SpawnerRunSnapshot{AnnouncedWave=4,OctopusIntroduced=true,OrcaladonIntroduced=true});
            Assert.Greater(game.Spawner.WavePlanned,0);Assert.AreEqual(game.Spawner.WavePlanned,game.Spawner.WaveResolved);
            Assert.AreEqual(1,game.Spawner.WaveCompletion);yield return null;
        }
        [UnityTest] public IEnumerator AttackSlotsKeepFollowingSharksFromAllBitingAtOnce()
        {
            profile.Crowd.MaxSimultaneousMelee=1;
            var first=game.Spawner.Spawn(EnemyKind.Normal,Vector3.back*1.6f);
            var second=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*1.6f);
            first.Tick(.61f);second.Tick(.61f);
            Assert.AreEqual(EnemyAction.MeleeWindup,first.Action);Assert.AreEqual(EnemyAction.Chasing,second.Action);
            Assert.IsTrue(game.Spawner.IsMeleeWarningVisible(first));Assert.IsFalse(game.Spawner.CanBeginMeleeAttack(second));
            first.TakeDamage(9999,Vector3.zero);second.Tick(.01f);Assert.AreEqual(EnemyAction.MeleeWindup,second.Action);
            yield return null;
        }
        [UnityTest] public IEnumerator LoweringAttackBudgetDoesNotHideAlreadyCommittedWarnings()
        {
            profile.Crowd.MaxSimultaneousMelee=3;profile.Crowd.MaxMeleeWarnings=3;
            var first=game.Spawner.Spawn(EnemyKind.Normal,Vector3.back*1.6f);
            var second=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*1.6f);
            var third=game.Spawner.Spawn(EnemyKind.Normal,Vector3.right*1.6f);
            first.Tick(.61f);second.Tick(.61f);third.Tick(.61f);
            Assert.AreEqual(EnemyAction.MeleeWindup,first.Action);Assert.AreEqual(EnemyAction.MeleeWindup,second.Action);Assert.AreEqual(EnemyAction.MeleeWindup,third.Action);
            profile.Crowd.MaxSimultaneousMelee=1;profile.Crowd.MaxMeleeWarnings=1;
            Assert.IsTrue(game.Spawner.IsMeleeWarningVisible(first));Assert.IsTrue(game.Spawner.IsMeleeWarningVisible(second));Assert.IsTrue(game.Spawner.IsMeleeWarningVisible(third));
            Assert.IsFalse(game.Spawner.CanBeginMeleeAttack(first),"An existing warning remains readable, but the lower budget cannot grant additional attacks.");
            yield return null;
        }
        void KillLiving(){for(int i=game.Spawner.Active.Count-1;i>=0;i--)game.Spawner.Active[i].TakeDamage(9999,Vector3.zero);}
    }
}
