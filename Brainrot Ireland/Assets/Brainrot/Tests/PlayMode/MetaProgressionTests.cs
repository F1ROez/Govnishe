using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Brainrot.Tests
{
    public sealed class MetaProgressionTests
    {
        string directory;IDisposable storage;
        [SetUp]public void Setup(){directory=Path.Combine(Path.GetTempPath(),"brainrot-economy-"+Guid.NewGuid().ToString("N"));storage=SaveSystem.UseStorageDirectory(directory);}
        [TearDown]public void Cleanup(){storage.Dispose();if(Directory.Exists(directory))Directory.Delete(directory,true);else if(File.Exists(directory))File.Delete(directory);}
        [Test]public void ClaimSurvivesReloadAndOldCheckpointCannotDuplicateCoins(){
            Assert.IsTrue(MetaProgression.BeginRun("run-a"));Assert.IsTrue(MetaProgression.Award("run-a",1,3));
            Assert.AreEqual(3,SaveSystem.Data.Coins);SaveSystem.Load();
            Assert.IsTrue(MetaProgression.IsClaimed("run-a",1));Assert.IsFalse(MetaProgression.Award("run-a",1,3));
            Assert.AreEqual(3,SaveSystem.Data.Coins);Assert.AreEqual(3,MetaProgression.RunEarned("run-a"));
            Assert.IsTrue(MetaProgression.BeginRun("run-b"));Assert.IsFalse(MetaProgression.Award("run-a",2,5));
            Assert.IsTrue(MetaProgression.Award("run-b",1,1));Assert.AreEqual(4,SaveSystem.Data.Coins);Assert.AreEqual(1,MetaProgression.RunEarned("run-b"));
        }
        [Test]public void ClusterAwardIsAtomicAndRejectsARepeatedReceipt(){
            Assert.IsTrue(MetaProgression.BeginRun("batch"));
            Assert.IsTrue(MetaProgression.AwardBatch("batch",new[]{1,2},new[]{1,3}));Assert.AreEqual(4,SaveSystem.Data.Coins);
            Assert.IsFalse(MetaProgression.AwardBatch("batch",new[]{2,3},new[]{3,8}));Assert.AreEqual(4,SaveSystem.Data.Coins);Assert.IsFalse(MetaProgression.IsClaimed("batch",3));
        }
        [Test]public void PurchasePersistsAndAppliesOnlyWhenCallerCreatesNewStats(){
            SaveSystem.Data.Coins=1000;Assert.IsTrue(SaveSystem.TrySave());
            var current=new PlayerStats();float currentDamage=current.Damage;int cost=MetaProgression.Cost(0);
            Assert.IsTrue(MetaProgression.Buy(0));Assert.AreEqual(currentDamage,current.Damage);
            Assert.AreEqual(1000-cost,SaveSystem.Data.Coins);SaveSystem.Load();Assert.AreEqual(1,MetaProgression.Rank(0));
            var next=new PlayerStats();MetaProgression.Apply(next);Assert.AreEqual(currentDamage+5,next.Damage);
            SaveSystem.Data.Coins=0;Assert.IsFalse(MetaProgression.Buy(1));Assert.AreEqual(0,MetaProgression.Rank(1));
        }
        [Test]public void FailedWalletWriteRollsBackBothCoinsAndReceiptAndPurchase(){
            MetaProgression.BeginRun("test");SaveSystem.Data.Coins=1000;SaveSystem.TrySave();
            Directory.Delete(directory,true);File.WriteAllText(directory,"block writes");
            LogAssert.Expect(LogType.Warning,new Regex("Не удалось записать сохранение:"));
            Assert.IsFalse(MetaProgression.Award("test",4,5));Assert.AreEqual(1000,SaveSystem.Data.Coins);Assert.IsFalse(MetaProgression.IsClaimed("test",4));Assert.AreEqual(0,MetaProgression.RunEarned("test"));
            LogAssert.Expect(LogType.Warning,new Regex("Не удалось записать сохранение:"));
            Assert.IsFalse(MetaProgression.Buy(0));Assert.AreEqual(1000,SaveSystem.Data.Coins);Assert.AreEqual(0,MetaProgression.Rank(0));
        }
        [Test]public void LegacyProfileAndMalformedRanksAreMigratedWithoutLosingRecords(){
            File.WriteAllText(Path.Combine(directory,"brainrot-save.json"),"{\"BestKills\":81,\"Coins\":-1,\"UpgradeRanks\":[100,-3],\"CoinRunId\":\"x\",\"ClaimedCoinIds\":[1,1,-4,2]}");
            SaveSystem.Load();Assert.AreEqual(81,SaveSystem.Data.BestKills);Assert.AreEqual(0,SaveSystem.Data.Coins);
            Assert.AreEqual(5,MetaProgression.Rank(0));Assert.AreEqual(0,MetaProgression.Rank(1));Assert.AreEqual(2,SaveSystem.Data.ClaimedCoinIds.Length);
            Assert.AreEqual(MetaProgression.Catalog.Length,SaveSystem.Data.UpgradeRanks.Length);
        }
    }
}
