using System;
using System.IO;
using System.Collections.Generic;
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
            public ShippingFile[] shippingFiles;
        }
        [Serializable] sealed class ShippingFile { public string path,sha256; public long bytes; }
        /// <summary>Matches the packager: IL2CPP debug backups and symbols are not distributable game content.</summary>
        public static bool IsShippingPath(string relativePath)
        {
            string normalized=relativePath.Replace('\\','/');
            if(normalized=="alpha-build-report.json")return false;
            foreach(string part in normalized.Split('/')){
                string lower=part.Replace("_","").ToLowerInvariant();
                if(lower.Contains("donotship")||lower.Contains("dontship")||lower.Contains("backupthisfolder"))return false;
            }
            string extension=Path.GetExtension(normalized).ToLowerInvariant();
            return extension!=".pdb"&&extension!=".mdb";
        }
        static string Hash(string path)
        {
            using(var sha=SHA256.Create())using(var input=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(input)).Replace("-","").ToLowerInvariant();
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
            metadata.playerSha256=Hash(executable);
            var manifest=new List<ShippingFile>();
            string root=Path.GetFullPath(directory)+Path.DirectorySeparatorChar;
            var paths=Directory.GetFiles(directory,"*",SearchOption.AllDirectories);Array.Sort(paths,StringComparer.Ordinal);
            foreach(string path in paths){
                metadata.files++;metadata.totalBytes+=new FileInfo(path).Length;
                string relative=Path.GetFullPath(path).Substring(root.Length).Replace('\\','/');
                if(IsShippingPath(relative))manifest.Add(new ShippingFile{path=relative,bytes=new FileInfo(path).Length,sha256=Hash(path)});
            }
            metadata.shippingFiles=manifest.ToArray();
            File.WriteAllText(metadataPath,JsonUtility.ToJson(metadata,true));
            Debug.Log("BRAINROT_WINDOWS_BUILD_REPORT: "+Path.GetFullPath(metadataPath));
        }
    }
}
