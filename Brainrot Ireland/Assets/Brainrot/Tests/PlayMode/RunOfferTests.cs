using System.Collections.Generic;
using NUnit.Framework;
namespace Brainrot.Tests
{
    public sealed class RunOfferTests
    {
        [Test]public void ReopeningAndBuyingKeepStockStableUntilNextWave(){
            var run=new RunEconomy();run.Begin("stock");run.SetBalanceForTesting(100000);
            run.EnsureOffers(1);var before=new List<int>(run.OfferIds);Assert.AreEqual(3,before.Count);Assert.AreEqual(3,new HashSet<int>(before).Count);
            int chosen=before[0],price=run.Cost(chosen);Assert.IsTrue(run.TryBuyOffer(chosen));Assert.AreEqual(100000-price,run.Balance);
            for(int id=0;id<MetaProgression.Catalog.Length;id++)if(!before.Contains(id))Assert.IsFalse(run.TryBuyOffer(id),"Unlisted upgrades cannot be bought through the run shop.");
            Assert.IsTrue(run.IsOfferSold(chosen));Assert.IsFalse(run.TryBuyOffer(chosen));
            run.EnsureOffers(1);CollectionAssert.AreEqual(before,run.OfferIds);Assert.IsTrue(run.IsOfferSold(chosen));
            run.EnsureOffers(2);Assert.AreEqual(2,run.OfferWave);Assert.IsFalse(run.IsOfferSold(chosen));
            run.EnsureOffers(1);Assert.AreEqual(2,run.OfferWave);
        }
        [Test]public void CappedItemsNeverAppearAndAnEmptyCatalogueIsHandled(){
            var run=new RunEconomy();run.Begin("caps");run.SetBalanceForTesting(100000);
            int last=MetaProgression.Catalog.Length-1;
            for(int id=0;id<last;id++)for(int rank=0;rank<MetaProgression.Catalog[id].MaxRank;rank++)Assert.IsTrue(run.Buy(id));
            run.EnsureOffers(1);CollectionAssert.AreEqual(new[]{last},run.OfferIds);
            for(int rank=0;rank<MetaProgression.Catalog[last].MaxRank;rank++)Assert.IsTrue(run.Buy(last));
            run.EnsureOffers(2);Assert.IsEmpty(run.OfferIds);
            run.Complete();Assert.IsEmpty(run.OfferIds);run.Begin("fresh");run.EnsureOffers(1);Assert.AreEqual(3,run.OfferIds.Count);
        }
        [Test]public void PiercingCostsFortyAndCannotStackOrPersistAcrossAttempts(){
            var run=new RunEconomy();run.Begin("pierce");run.SetBalanceForTesting(40);
            Assert.AreEqual(40,run.Cost(4));Assert.IsTrue(run.Buy(4));Assert.AreEqual(0,run.Balance);Assert.AreEqual(1,run.Rank(4));
            Assert.IsFalse(run.Buy(4));run.Complete();run.Begin("new");Assert.AreEqual(0,run.Rank(4));Assert.AreEqual(40,run.Cost(4));
        }
        [Test]public void SeededAttemptStockIsRepeatableAndVariesAcrossSeeds(){
            var a=new RunEconomy();var b=new RunEconomy();a.Begin("repeat");b.Begin("repeat");
            for(int wave=1;wave<=8;wave++){a.EnsureOffers(wave);b.EnsureOffers(wave);CollectionAssert.AreEqual(a.OfferIds,b.OfferIds);}
            var choices=new HashSet<int>();for(int seed=0;seed<16;seed++){a.Begin("seed"+seed);a.EnsureOffers(1);choices.Add(a.OfferIds[0]);}
            Assert.Greater(choices.Count,1);
        }
    }
}
