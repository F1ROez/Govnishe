using NUnit.Framework;
namespace Brainrot.Tests
{
    public sealed class RunEconomyBufferTests
    {
        [Test]public void ReusedPickupBuffersIgnoreStaleTailAndCreditEachTokenOnce(){
            var run=new RunEconomy();run.Begin("buffers");
            int[] ids={1,2,3,0},amounts={1,1,1,0};
            Assert.IsTrue(run.AwardBatch(ids,amounts,3));Assert.AreEqual(3,run.Balance);
            ids[0]=4;Assert.IsTrue(run.AwardBatch(ids,amounts,1));Assert.AreEqual(4,run.Balance);
            Assert.IsFalse(run.Award(4,1));Assert.AreEqual(4,run.Earned);
            Assert.IsTrue(run.Award(5,1));Assert.AreEqual(5,run.Balance);
        }
        [Test]public void InvalidBufferBatchIsAtomicAndCannotUseOutsidePrefix(){
            var run=new RunEconomy();run.Begin("invalid");
            int[] ids={11,12,11},amounts={1,1,1};
            Assert.IsFalse(run.AwardBatch(ids,amounts,4));Assert.IsFalse(run.AwardBatch(ids,amounts,-1));
            Assert.IsFalse(run.AwardBatch(ids,amounts,3));Assert.AreEqual(0,run.Balance);
            Assert.IsFalse(run.IsClaimed(11));Assert.IsTrue(run.AwardBatch(ids,amounts,2));
            Assert.AreEqual(2,run.Balance);Assert.IsFalse(run.Award(13,2));
            run.Complete();Assert.IsFalse(run.Award(14,1));Assert.IsFalse(run.AwardBatch(ids,amounts,1));
        }
    }
}
