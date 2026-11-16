using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Batch entry point. GameBuilder throws if the actual Unity BuildReport is not successful.</summary>
    public static class AlphaValidationBuild
    {
        [Serializable] sealed class Metadata
        {
            public string startedUtc,completedUtc,unityVersion,sourceRevision,target,playerSha256;
            public bool development;
            public long playerBytes,totalBytes;
            public int files;
        }
        public static void BuildWindows()
        {
            const string directory="Builds/Windows";
            string metadataPath=Path.Combine(directory,"alpha-build-report.json");
            if(File.Exists(metadataPath))File.Delete(metadataPath);
            var started=DateTime.UtcNow;
            GameBuilder.BuildWindows();
            string executable=Path.Combine(directory,"BrainrotIreland.exe");
            if(!File.Exists(executable)||!File.Exists(Path.Combine(directory,"UnityPlayer.dll"))||!Directory.Exists(Path.Combine(directory,"BrainrotIreland_Data")))
                throw new InvalidOperationException("Windows build artifacts are incomplete.");
            var metadata=new Metadata{startedUtc=started.ToString("O"),completedUtc=DateTime.UtcNow.ToString("O"),unityVersion=Application.unityVersion,sourceRevision=Environment.GetEnvironmentVariable("BRAINROT_SOURCE_REVISION")??"unknown",target=EditorUserBuildSettings.activeBuildTarget.ToString(),playerBytes=new FileInfo(executable).Length};
            foreach(string argument in Environment.GetCommandLineArgs())if(argument=="--brainrot-development")metadata.development=true;
            using(var sha=SHA256.Create())using(var input=File.OpenRead(executable))metadata.playerSha256=BitConverter.ToString(sha.ComputeHash(input)).Replace("-","").ToLowerInvariant();
            foreach(string path in Directory.GetFiles(directory,"*",SearchOption.AllDirectories)){metadata.files++;metadata.totalBytes+=new FileInfo(path).Length;}
            File.WriteAllText(metadataPath,JsonUtility.ToJson(metadata,true));
            Debug.Log("BRAINROT_WINDOWS_BUILD_REPORT: "+Path.GetFullPath(metadataPath));
        }
    }
}
