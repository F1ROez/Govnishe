using System;
using System.IO;
using NUnit.Framework;
namespace Brainrot.Tests
{
    public sealed class ProgressResetTests
    {
        [Test]public void ResetClearsProgressAndRetainsPreferencesAfterReload(){
            string folder=Path.Combine(Path.GetTempPath(),"brainrot-reset-"+Guid.NewGuid().ToString("N"));
            try{using(SaveSystem.UseStorageDirectory(folder)){
                SaveSystem.Data.Coins=800;SaveSystem.Data.UpgradeRanks=new[]{3,2,1,1};SaveSystem.Data.BestWave=8;SaveSystem.Data.BestKills=80;
                SaveSystem.Data.TutorialCompleted=true;SaveSystem.Data.Volume=.35f;SaveSystem.Data.Sensitivity=1.7f;
                Assert.IsTrue(SaveSystem.TrySave());Assert.IsTrue(SaveSystem.ResetProgress());SaveSystem.Load();
                Assert.AreEqual(0,SaveSystem.Data.Coins);Assert.AreEqual(0,MetaProgression.Rank(0));Assert.AreEqual(0,SaveSystem.Data.BestWave);
                Assert.IsFalse(SaveSystem.Data.TutorialCompleted);Assert.AreEqual(.35f,SaveSystem.Data.Volume,.001f);Assert.AreEqual(1.7f,SaveSystem.Data.Sensitivity,.001f);
                File.WriteAllText(Path.Combine(folder,"brainrot-save.json"),"broken");SaveSystem.Load();Assert.AreEqual(0,SaveSystem.Data.Coins);Assert.AreEqual(0,SaveSystem.Data.BestWave);
                Assert.IsFalse(SaveSystem.SetTestCoins(12345),"There is no persistent or future-attempt wallet outside a run.");SaveSystem.Load();Assert.AreEqual(0,SaveSystem.Data.Coins);
            }}finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
    }
}
