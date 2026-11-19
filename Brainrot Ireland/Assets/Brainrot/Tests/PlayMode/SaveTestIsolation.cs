using System;
using System.IO;
using NUnit.Framework;

// All PlayMode fixtures may start/kill runs. Redirect persistence once for the complete test assembly.
[SetUpFixture]
public sealed class BrainrotSaveTestIsolation
{
    IDisposable scope;
    string directory;
    [OneTimeSetUp]public void IsolatePlayerStorage(){
        directory=Path.Combine(Path.GetTempPath(),"brainrot-playmode-"+Guid.NewGuid().ToString("N"));
        scope=Brainrot.SaveSystem.UseStorageDirectory(directory);
    }
    [OneTimeTearDown]public void RestorePlayerStorage(){
        scope?.Dispose();scope=null;
        if(!string.IsNullOrEmpty(directory)&&Directory.Exists(directory))Directory.Delete(directory,true);
    }
}
