using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class PolishedPersistenceTests
    {
        string directory,path;
        RunSaveStore store;
        [SetUp]public void Setup(){directory=Path.Combine(Path.GetTempPath(),"brainrot-budget-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);path=Path.Combine(directory,"run.json");store=new RunSaveStore(path);}
        [TearDown]public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        static RunSnapshot Snapshot(float elapsed=12)=>new RunSnapshot{
            Elapsed=elapsed,Kills=15,ChallengeMaskEarnedThisRun=(int)RunChallenge.CleanWave,
            Player=new PlayerRunSnapshot{Health=75,Stamina=50},Wave=new WaveRunSnapshot{Number=3,Time=20},
            Spawner=new SpawnerRunSnapshot{BudgetWave=3,PlannedCount=40,SpawnedCount=15,ResolvedCount=15,AnnouncedWave=3}
        };
        [Test]public void WaveBudgetAndRunSpecificChallengeSummarySurviveDiskRoundTrip()
        {
            Assert.IsTrue(store.Save(Snapshot()));Assert.IsTrue(store.TryLoad(out var result));
            Assert.AreEqual(40,result.Spawner.PlannedCount);Assert.AreEqual(15,result.Spawner.SpawnedCount);
            Assert.AreEqual(15,result.Spawner.ResolvedCount);Assert.AreEqual(3,result.Spawner.BudgetWave);
            Assert.AreEqual((int)RunChallenge.CleanWave,result.ChallengeMaskEarnedThisRun);
        }
        [Test]public void RecoveryThenSavePreservesLastGoodBackupInsteadOfCopyingCorruption()
        {
            Assert.IsTrue(store.Save(Snapshot(12)));Assert.IsTrue(store.Save(Snapshot(24)));
            File.WriteAllText(path,"{broken");Assert.IsTrue(store.TryLoad(out var first));Assert.AreEqual(12,first.Elapsed);
            Assert.IsTrue(store.Save(Snapshot(36)));File.WriteAllText(path,"{broken again");
            Assert.IsTrue(store.TryLoad(out var recovered));Assert.AreEqual(12,recovered.Elapsed);
        }
        [Test]public void AdditiveBudgetFieldsKeepLegacyVersionOneSnapshotsValid()
        {
            var saved=Snapshot();saved.Spawner.BudgetWave=saved.Spawner.PlannedCount=saved.Spawner.SpawnedCount=saved.Spawner.ResolvedCount=0;
            Assert.IsTrue(RunSaveStore.IsValid(saved));Assert.IsTrue(store.Save(saved));Assert.IsTrue(store.TryLoad(out _));
        }
        [Test]public void ImpossibleBudgetNeverOverwritesExistingCheckpoint()
        {
            Assert.IsTrue(store.Save(Snapshot()));var invalid=Snapshot(90);invalid.Spawner.ResolvedCount=16;
            Assert.IsFalse(store.Save(invalid));invalid=Snapshot(90);invalid.Spawner.PlannedCount=10001;Assert.IsFalse(RunSaveStore.IsValid(invalid));
            invalid=Snapshot(90);invalid.Spawner.BudgetWave=4;Assert.IsFalse(RunSaveStore.IsValid(invalid));
            invalid=Snapshot(90);invalid.Spawner.ResolvedCount=14;Assert.IsFalse(RunSaveStore.IsValid(invalid),"Число живых противников должно совпадать с бюджетом.");
            invalid=Snapshot(90);invalid.Spawner.BudgetWave=2;Assert.IsFalse(RunSaveStore.IsValid(invalid),"Бюджет предыдущей волны не должен приниматься за текущий.");
            Assert.IsTrue(store.TryLoad(out var restored));Assert.AreEqual(12,restored.Elapsed);
        }
    }
}
