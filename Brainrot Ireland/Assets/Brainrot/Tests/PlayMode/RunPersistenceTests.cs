using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class RunPersistenceTests
    {
        string directory,path;
        RunSaveStore store;
        [SetUp]public void Setup(){directory=Path.Combine(Path.GetTempPath(),"brainrot-run-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);path=Path.Combine(directory,"run.json");store=new RunSaveStore(path);}
        [TearDown]public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        static RunSnapshot Snapshot(float elapsed=12){
            return new RunSnapshot{
                Elapsed=elapsed,Kills=37,
                Player=new PlayerRunSnapshot{Health=64,Stamina=23,Position=new Vector3(2,0,-3),CameraYaw=22,CameraPitch=10,HeavyCooldown=8,DashCooldown=1.2f},
                Wave=new WaveRunSnapshot{Number=4,Time=17},
                Spawner=new SpawnerRunSnapshot{AnnouncedWave=4,SpawnClock=.8f,OctopusIntroduced=true,OrcaladonIntroduced=true,Enemies=new[]{
                    new EnemyRunSnapshot{Kind=0,Health=12,MaxHealth=70,Position=new Vector3(8,0,6),RotationYaw=180},
                    new EnemyRunSnapshot{Kind=10,Health=80,MaxHealth=100,Position=new Vector3(-20,0,2)}
                }}
            };
        }
        [Test]public void RoundTripPreservesWavePlayerCooldownsAndLivingEnemies(){
            var original=Snapshot();Assert.IsTrue(store.Save(original));Assert.IsTrue(store.TryLoad(out var restored));
            Assert.AreEqual(4,restored.Wave.Number);Assert.AreEqual(17,restored.Wave.Time);Assert.AreEqual(37,restored.Kills);
            Assert.AreEqual(original.Player.Position,restored.Player.Position);Assert.AreEqual(64,restored.Player.Health);Assert.AreEqual(8,restored.Player.HeavyCooldown);
            Assert.AreEqual(2,restored.Spawner.Enemies.Length);Assert.AreEqual(80,restored.Spawner.Enemies[1].Health);Assert.IsTrue(restored.Spawner.OctopusIntroduced);
        }
        [Test]public void CorruptPrimaryUsesLastGoodBackup(){
            Assert.IsTrue(store.Save(Snapshot(12)));Assert.IsTrue(store.Save(Snapshot(24)));File.WriteAllText(path,"{broken");
            Assert.IsTrue(store.TryLoad(out var restored));Assert.AreEqual(12,restored.Elapsed);
        }
        [Test]public void ClearedRunCannotBeResurrectedFromOldBackup(){
            var old=Snapshot();Assert.IsTrue(store.Save(old));store.Clear();
            File.WriteAllText(path+".bak",JsonUtility.ToJson(old));Assert.IsFalse(store.TryLoad(out _));
            Assert.IsTrue(store.Save(Snapshot(99)));Assert.IsTrue(store.TryLoad(out var current));Assert.AreEqual(99,current.Elapsed);
        }
        [Test]public void InvalidSnapshotNeverOverwritesGoodCheckpoint(){
            Assert.IsTrue(store.Save(Snapshot(12)));var invalid=Snapshot(24);invalid.Player.Health=float.NaN;
            Assert.IsFalse(store.Save(invalid));Assert.IsTrue(store.TryLoad(out var restored));Assert.AreEqual(12,restored.Elapsed);
        }
        [Test]public void UnsupportedVersionAndOversizeFilesAreRejected(){
            var unsupported=Snapshot();unsupported.Version=RunSnapshot.CurrentVersion+1;File.WriteAllText(path,JsonUtility.ToJson(unsupported));
            Assert.IsFalse(store.TryLoad(out _));File.WriteAllText(path,new string(' ',RunSaveStore.MaximumBytes+1));Assert.IsFalse(store.TryLoad(out _));
        }
        [Test]public void MissingVersionIsNotMistakenForCurrentVersion(){
            string json=JsonUtility.ToJson(Snapshot()).Replace("\"Version\":1,","");File.WriteAllText(path,json);
            Assert.IsFalse(store.TryLoad(out _));
        }
        [Test]public void MalformedAndUnboundedLiveEnemyDataIsRejected(){
            var snapshot=Snapshot();snapshot.Spawner.Enemies[0].Kind=999;Assert.IsFalse(RunSaveStore.IsValid(snapshot));
            snapshot=Snapshot();snapshot.Spawner.Enemies=new EnemyRunSnapshot[101];Assert.IsFalse(RunSaveStore.IsValid(snapshot));
            snapshot=Snapshot();snapshot.Spawner.Enemies[0].Health=1000;Assert.IsFalse(RunSaveStore.IsValid(snapshot));
            snapshot=Snapshot();snapshot.Player.Position=new Vector3(float.PositiveInfinity,0,0);Assert.IsFalse(RunSaveStore.IsValid(snapshot));
        }
        [Test]public void WaveCheckpointPreservesClearGateAndWaveNumber(){
            var waves=new WaveDirector();waves.RestoreRunState(new WaveRunSnapshot{Number=8,Time=60,RestTime=0,Resting=true,WaitingForClear=true});
            waves.Advance(100,3);Assert.AreEqual(8,waves.Number);Assert.IsTrue(waves.WaitingForClear);
            var captured=waves.CaptureRunState();Assert.IsTrue(captured.Resting);Assert.IsTrue(captured.WaitingForClear);
            waves.Advance(1,0);Assert.IsFalse(waves.WaitingForClear);Assert.AreEqual(8,waves.Number);
        }
    }
}
