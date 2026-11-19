using NUnit.Framework;
using UnityEngine;

namespace Brainrot.Tests
{
    public sealed class RunGoalsTests
    {
        [Test]public void TutorialAcceptsActionsInAnyOrderAndWaitsForAllFour()
        {
            var progress=new TutorialProgress();
            progress.Observe(false,false,true,true);
            Assert.IsFalse(progress.Complete);StringAssert.Contains("WASD",progress.Hint(false));
            progress.Observe(true,false,false,false);
            StringAssert.Contains("ЛКМ",progress.Hint(false));
            progress.Observe(false,true,false,false);
            Assert.IsTrue(progress.Complete);Assert.AreEqual("",progress.Hint(false));
            progress.Reset();Assert.IsFalse(progress.Complete);
        }
        [Test]public void HeavyAttackHintChangesToReleaseOnlyAtFullCharge()
        {
            var progress=new TutorialProgress();progress.Observe(true,true,true,false);
            StringAssert.Contains("УДЕРЖИ",progress.Hint(false,true,false));
            StringAssert.Contains("ОТПУСТИ ЛКМ",progress.Hint(false,true,true));
            StringAssert.Contains("«УДАР»",progress.Hint(true,true,true));
        }
        [Test]public void RestoringPartOfWaveCannotAwardCleanWave()
        {
            var progress=new RunChallengeProgress();progress.Reset(3,0,true);
            var first=progress.Observe(4,10,0);
            Assert.AreEqual(0,(int)(first&RunChallenge.CleanWave));
            var second=progress.Observe(5,20,0);
            Assert.AreNotEqual(0,(int)(second&RunChallenge.CleanWave));
        }
        [Test]public void DamageInvalidatesOnlyCurrentWaveAndMilestonesHaveExactThresholds()
        {
            var progress=new RunChallengeProgress();progress.Reset(1,0,false);
            Assert.AreEqual(RunChallenge.None,progress.Observe(1,49,1));
            var next=progress.Observe(2,50,1);
            Assert.AreNotEqual(0,(int)(next&RunChallenge.FiftyDefeated));
            Assert.AreEqual(0,(int)(next&RunChallenge.CleanWave));
            var clean=progress.Observe(3,50,1);
            Assert.AreNotEqual(0,(int)(clean&RunChallenge.CleanWave));
            Assert.AreEqual(0,(int)(progress.Observe(4,50,1)&RunChallenge.WaveFive));
            Assert.AreNotEqual(0,(int)(progress.Observe(5,50,1)&RunChallenge.WaveFive));
            Assert.AreEqual(0,(int)(progress.Observe(9,50,1)&RunChallenge.WaveTen));
            Assert.AreNotEqual(0,(int)(progress.Observe(10,50,1)&RunChallenge.WaveTen));
        }
        [Test]public void WaveVariationsStartAfterIntroductionsAndStayBounded()
        {
            for(int wave=1;wave<=3;wave++){
                var basic=WaveChallenges.ForWave(wave);Assert.AreEqual(1,basic.SpawnIntervalMultiplier);Assert.AreEqual(0,basic.SpawnSectors);Assert.AreEqual("",basic.Title);
            }
            for(int wave=4;wave<=100;wave++){
                var plan=WaveChallenges.ForWave(wave);Assert.That(plan.SpawnIntervalMultiplier,Is.InRange(.9f,1.12f));Assert.IsNotEmpty(plan.Title);Assert.IsNotEmpty(plan.Description);
                var repeat=WaveChallenges.ForWave(wave+4);Assert.AreEqual(plan.Title,repeat.Title);Assert.AreEqual(plan.SpawnSectors,repeat.SpawnSectors);
            }
        }
        [Test]public void TwoShoreEventCreatesOppositeAnnouncedSectors()
        {
            float a=WaveChallenges.SpawnAngle(6,.25f),b=WaveChallenges.SpawnAngle(6,.75f);
            Assert.AreEqual(180,Mathf.Abs(Mathf.DeltaAngle(a*Mathf.Rad2Deg,b*Mathf.Rad2Deg)),.01f);
            for(int wave=1;wave<=40;wave++)foreach(float roll in new[]{0f,.5f,1f,float.NaN,float.PositiveInfinity}){
                float angle=WaveChallenges.SpawnAngle(wave,roll);Assert.IsFalse(float.IsNaN(angle)||float.IsInfinity(angle));Assert.That(angle,Is.InRange(0,Mathf.PI*2));
            }
        }
    }
}
