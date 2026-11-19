using System;
using System.IO;
using NUnit.Framework;
namespace Brainrot.Tests
{
    public sealed class MetaProgressionTests
    {
        [Test]public void ReceiptsAreAtomicAndAttemptLocal(){
            var economy=new RunEconomy();Assert.IsTrue(economy.Begin("first"));
            Assert.IsTrue(economy.AwardBatch(new[]{1,2,3},new[]{1,1,1}));Assert.AreEqual(3,economy.Balance);
            Assert.IsFalse(economy.AwardBatch(new[]{3,4},new[]{1,1}));Assert.IsFalse(economy.IsClaimed(4));Assert.AreEqual(3,economy.Balance);
            Assert.IsFalse(economy.AwardBatch(new[]{4},new[]{3}),"A displayed coin is exactly one unit.");
            economy.Complete();Assert.AreEqual(0,economy.Balance);Assert.AreEqual(3,economy.Earned);Assert.IsFalse(economy.AwardBatch(new[]{4},new[]{1}));
            Assert.IsTrue(economy.Begin("second"));Assert.AreEqual(0,economy.Earned);Assert.IsTrue(economy.AwardBatch(new[]{1},new[]{1}));Assert.AreEqual(1,economy.Balance);
        }
        [Test]public void PurchasesSpendCurrentBalanceAndResetOnANewAttempt(){
            var economy=new RunEconomy();economy.Begin("shop");economy.SetBalanceForTesting(10000);
            int total=0;for(int i=0;i<5;i++){int price=economy.Cost(0);total+=price;Assert.IsTrue(economy.Buy(0));Assert.AreEqual(i+1,economy.Rank(0));}
            Assert.AreEqual(10000-total,economy.Balance);Assert.AreEqual(0,economy.Cost(0));Assert.IsFalse(economy.Buy(0));
            economy.Complete();Assert.AreEqual(0,economy.Rank(0));Assert.IsFalse(economy.Buy(1));
            economy.Begin("next");Assert.AreEqual(0,economy.Rank(0));Assert.AreEqual(0,economy.Balance);Assert.AreEqual(30,economy.Cost(0));
        }
        [Test]public void CollectionAndPurchasingDoNotAccessDisk(){
            string directory=Path.Combine(Path.GetTempPath(),"brainrot-memory-economy-"+Guid.NewGuid().ToString("N"));
            try{using(SaveSystem.UseStorageDirectory(directory)){
                Directory.Delete(directory,true);File.WriteAllText(directory,"block any disk writes");
                var economy=new RunEconomy();economy.Begin("memory");economy.SetBalanceForTesting(100);
                Assert.IsTrue(economy.AwardBatch(new[]{1,2},new[]{1,1}));Assert.IsTrue(economy.Buy(0));Assert.AreEqual(72,economy.Balance);
            }}finally{if(File.Exists(directory))File.Delete(directory);else if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]public void LongAttemptIsNotLimitedByTheOldSavedReceiptBudget(){
            var economy=new RunEconomy();economy.Begin("long");var ids=new int[1000];var values=new int[1000];
            for(int batch=0;batch<20;batch++){
                for(int i=0;i<ids.Length;i++){ids[i]=batch*1000+i+1;values[i]=1;}
                Assert.IsTrue(economy.AwardBatch(ids,values));
            }
            Assert.AreEqual(20000,economy.Balance);Assert.AreEqual(20000,economy.Earned);
            Assert.IsTrue(economy.IsClaimed(16385));Assert.IsFalse(economy.AwardBatch(ids,values));
        }
        [Test]public void LegacyPermanentCurrencyIsDiscardedWithoutLosingRecordsOrPreferences(){
            string directory=Path.Combine(Path.GetTempPath(),"brainrot-migration-"+Guid.NewGuid().ToString("N"));
            try{using(SaveSystem.UseStorageDirectory(directory)){
                File.WriteAllText(Path.Combine(directory,"brainrot-save.json"),"{\"BestKills\":81,\"BestWave\":8,\"Volume\":0.35,\"Coins\":999,\"UpgradeRanks\":[5,5,5,5],\"CoinRunId\":\"old\",\"ClaimedCoinIds\":[1,2]}");
                SaveSystem.Load();Assert.AreEqual(81,SaveSystem.Data.BestKills);Assert.AreEqual(8,SaveSystem.Data.BestWave);Assert.AreEqual(.35f,SaveSystem.Data.Volume,.001f);
                Assert.AreEqual(0,SaveSystem.Data.Coins);Assert.IsEmpty(SaveSystem.Data.UpgradeRanks);Assert.IsEmpty(SaveSystem.Data.ClaimedCoinIds);Assert.AreEqual("",SaveSystem.Data.CoinRunId);
                Assert.IsTrue(SaveSystem.TrySave());SaveSystem.Load();Assert.AreEqual(0,SaveSystem.Data.Coins);Assert.IsEmpty(SaveSystem.Data.UpgradeRanks);
            }}finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
