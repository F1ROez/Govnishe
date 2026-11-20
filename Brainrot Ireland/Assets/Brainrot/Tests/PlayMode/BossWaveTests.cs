using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class BossWaveTests
    {
        Scene previous,scene;GameManager game;GameBalance profile;
        [UnitySetUp]public IEnumerator Setup(){
            Assert.IsNull(GameManager.Instance,"Run outside a live game.");
            previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Boss waves "+System.Guid.NewGuid());SceneManager.SetActiveScene(scene);
            game=new GameObject("Game").AddComponent<GameManager>();profile=ScriptableObject.CreateInstance<GameBalance>();game.Balance=profile;
            profile.Waves.InitialDelay=600;profile.Player.CritChance=0;
            yield return null;game.StartRun();game.enabled=false;game.Player.enabled=false;
        }
        [UnityTearDown]public IEnumerator Cleanup(){
            if(GameMusic.Instance!=null)Object.Destroy(GameMusic.Instance.gameObject);Time.timeScale=1;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(profile!=null)Object.Destroy(profile);yield return null;
        }
        void BeginWave(int wave){game.Spawner.ResetRun();game.Spawner.Waves.RestoreRunState(new WaveRunSnapshot{Number=wave});game.Spawner.Tick(.01f);}
        [Test]public void ScheduleKeepsFirstMeetingsAndIndependentNonRepeatingBags(){
            var schedule=new BossWaveSchedule(291);var mirror=new BossWaveSchedule(291);
            Assert.AreEqual(IslandBossKind.Shark,schedule.ForWave(5));Assert.AreEqual(IslandBossKind.Octopus,schedule.ForWave(10));Assert.AreEqual(IslandBossKind.Orcaladon,schedule.ForWave(15));
            var last=IslandBossKind.Orcaladon;
            for(int start=20;start<500;start+=15){
                var bag=new HashSet<IslandBossKind>();
                for(int wave=start;wave<start+15;wave+=5){var kind=schedule.ForWave(wave);Assert.AreEqual(kind,mirror.ForWave(wave));Assert.AreNotEqual(last,kind);bag.Add(kind);last=kind;}
                Assert.AreEqual(3,bag.Count);
            }
            Assert.AreEqual(schedule.ForWave(200),mirror.ForWave(200),"Inspecting another wave must not consume the schedule.");
            Assert.IsFalse(BossSystem.IsBossWave(0));Assert.IsFalse(BossSystem.IsBossWave(4));Assert.IsFalse(BossSystem.IsBossWave(6));Assert.IsTrue(BossSystem.IsBossWave(1000000));
        }
        [Test]public void BossStatGrowthIsModerateAndBounded(){
            var tuning=profile.IslandBosses.Shark;
            Assert.AreEqual(tuning.Health,tuning.HealthAt(5),.001f);Assert.Greater(tuning.HealthAt(20),tuning.HealthAt(5));
            Assert.LessOrEqual(tuning.HealthAt(1000000),tuning.Health*tuning.ScalingCap+.01f);
            Assert.LessOrEqual(tuning.DamageAt(1000000),tuning.Damage*1.65f+.001f);Assert.LessOrEqual(tuning.TempoAt(1000000),1.25f);
        }
        [UnityTest]public IEnumerator EachFirstBossIsAloneAndVictoryOpensManualIntermission(){
            int[] waves={5,10,15};EnemyKind[] kinds={EnemyKind.Normal,EnemyKind.BlueberrinniOctopusini,EnemyKind.Orcaladon};
            for(int i=0;i<waves.Length;i++){
                BeginWave(waves[i]);var boss=game.Spawner.CurrentBoss;
                Assert.IsNotNull(boss);Assert.IsTrue(boss.IsBoss);Assert.AreEqual(kinds[i],boss.Kind);Assert.AreEqual(1,game.Spawner.Active.Count);Assert.AreEqual(1,game.Spawner.WavePlanned);
                Assert.IsNull(game.Spawner.Spawn(EnemyKind.Normal,Vector3.right*6),"Ordinary mass spawns are forbidden on boss waves.");
                game.Spawner.Tick(30);Assert.IsFalse(game.Spawner.Waves.Resting,"A timer cannot complete a living boss.");Assert.AreEqual(1,game.Spawner.Active.Count);
                boss.TakeDamage(boss.Health+1,Vector3.zero);Assert.IsNull(game.Spawner.CurrentBoss);Assert.AreEqual(0,game.Spawner.Active.Count);Assert.IsTrue(game.Spawner.Waves.Resting);Assert.IsFalse(game.Spawner.Waves.WaitingForClear);Assert.AreEqual(1,game.Spawner.WaveCompletion);
                game.Spawner.Tick(30);Assert.AreEqual(waves[i],game.Wave,"The next wave still needs the existing hold-F gate.");Assert.AreEqual(0,game.Spawner.Active.Count);
                Assert.IsTrue(game.Spawner.Waves.StartNextWave());Assert.AreEqual(waves[i]+1,game.Wave);Assert.IsFalse(game.Spawner.IsBossWave);
            }
            yield return null;
        }
        [UnityTest]public IEnumerator ResetAndPoolingRemoveBossScaleAndHeavyHitsCannotStunlockLivingBoss(){
            BeginWave(5);var boss=game.Spawner.CurrentBoss;float health=boss.Health;
            boss.TakeDamage(1,Vector3.right*12);Assert.AreEqual(health-1,boss.Health,.001f);Assert.IsFalse(boss.PhysicsBody.IsPhysical);
            Assert.Greater(boss.BodyCollider.radius,1);game.StartRun();game.enabled=false;game.Player.enabled=false;
            Assert.AreEqual(1,game.Wave);Assert.IsNull(game.Spawner.CurrentBoss);Assert.IsFalse(boss.IsBoss);
            var ordinary=game.Spawner.Spawn(EnemyKind.Normal,Vector3.forward*5);Assert.IsNotNull(ordinary);Assert.IsFalse(ordinary.IsBoss);Assert.AreEqual(.55f,ordinary.BodyCollider.radius,.001f);Assert.AreEqual(profile.Normal.Health,ordinary.MaxHealth,.001f);
            yield return null;
        }
        [UnityTest]public IEnumerator BossPhaseThresholdsRemainSpeciesSpecific(){
            int[] waves={5,10,15};
            foreach(int wave in waves){
                BeginWave(wave);var boss=game.Spawner.CurrentBoss;var encounter=boss.BossEncounter;
                float threshold=profile.IslandBosses.For(encounter.Kind).PhaseThreshold;
                boss.TakeDamage(boss.MaxHealth*(1-threshold)-1,Vector3.zero);boss.Tick(.01f);Assert.IsFalse(encounter.PhaseTwo);
                boss.TakeDamage(2,Vector3.zero);boss.Tick(.01f);Assert.IsTrue(encounter.PhaseTwo);
            }
            yield return null;
        }
    }
}
