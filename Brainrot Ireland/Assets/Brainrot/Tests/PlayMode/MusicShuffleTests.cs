using System.Collections.Generic;
using NUnit.Framework;
namespace Brainrot.Tests
{
    public sealed class MusicShuffleTests
    {
        [Test]public void EveryCycleContainsAllSongsAndBoundariesNeverRepeat()
        {
            var bag=new MusicShuffleBag(6,2357);int previous=-1;
            for(int cycle=0;cycle<30;cycle++){
                var visited=new HashSet<int>();
                for(int i=0;i<6;i++){int next=bag.Next();Assert.That(next,Is.InRange(0,5));Assert.AreNotEqual(previous,next);Assert.IsTrue(visited.Add(next));previous=next;}
                Assert.AreEqual(6,visited.Count);
            }
        }
        [Test]public void EmptyLibraryAndSingleTrackRemainValid()
        {
            Assert.AreEqual(-1,new MusicShuffleBag(0).Next());var one=new MusicShuffleBag(1,1);for(int i=0;i<5;i++)Assert.AreEqual(0,one.Next());
        }
    }
}
