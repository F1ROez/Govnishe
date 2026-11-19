using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Brainrot.Editor
{
    /// <summary>Lossless, Editor-only delivery of illustrated menu sources. No manual extraction or runtime decoding.</summary>
    [InitializeOnLoad]
    public static class IslandArtInstaller
    {
        const string PayloadRoot="Assets/Brainrot/Generated/Editor/MenuArtPayloads/";
        const string OutputRoot="Assets/Brainrot/Generated/Resources/";
        const int ChunkSize=32768;
        static readonly string[] Names={"IslandMenuAtlas","IslandControlsAtlas","IslandMenuBackdrop","IslandUpgradeIcons"};
        static bool installing;
        [Serializable] sealed class Manifest {public int version;public TextureEntry[] textures;}
        [Serializable] sealed class TextureEntry {public string name,file,sha256;public int length;public string[] chunks;}
        sealed class Pending {public string path;public byte[] bytes;}

        static IslandArtInstaller(){
            EditorApplication.delayCall+=InstallAfterImport;
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.ExitingEditMode&&!EnsureFiles())EditorApplication.isPlaying=false;
            };
        }
        static void InstallAfterImport(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=InstallAfterImport;return;}
            EnsureFiles();
        }
        /// <summary>Verify first, then materialize only the three owned PNGs. Existing matching outputs are left intact.</summary>
        public static bool EnsureFiles(){
            if(installing)return true;
            installing=true;
            try{
                string manifestPath=Full(PayloadRoot+"manifest.json");
                if(!File.Exists(manifestPath))throw new InvalidDataException("Не найден manifest.json оформления меню. Скопируйте папку Brainrot полностью.");
                var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
                if(manifest==null||manifest.version!=1||manifest.textures==null||manifest.textures.Length!=Names.Length)
                    throw new InvalidDataException("Неверный каталог оформления меню.");
                var known=new HashSet<string>(StringComparer.Ordinal);var pending=new List<Pending>();
                foreach(var entry in manifest.textures){
                    ValidateEntry(entry,known);
                    string path=OutputRoot+entry.file,full=Full(path);
                    RecoverInterruptedWrite(full);
                    if(File.Exists(full)&&new FileInfo(full).Length==entry.length&&Hash(File.ReadAllBytes(full))==entry.sha256)continue;
                    pending.Add(new Pending{path=path,bytes=Decode(entry)});
                }
                // All required payloads have passed length/hash checks before any new artwork is written.
                if(pending.Count==0)return true;
                Directory.CreateDirectory(Full(OutputRoot));
                AssetDatabase.StartAssetEditing();
                try{foreach(var item in pending)WriteVerified(Full(item.path),item.bytes);}
                finally{AssetDatabase.StopAssetEditing();}
                foreach(var item in pending)AssetDatabase.ImportAsset(item.path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                return true;
            }catch(Exception exception) when(exception is IOException||exception is UnauthorizedAccessException||exception is ArgumentException||exception is FormatException||exception is CryptographicException){
                Debug.LogError("Не удалось восстановить оформление меню: "+exception.Message);return false;
            }finally{installing=false;}
        }
        static string Full(string relative){return Path.GetFullPath(Path.Combine(Application.dataPath,"..",relative));}
        static void ValidateEntry(TextureEntry entry,HashSet<string> known){
            if(entry==null||Array.IndexOf(Names,entry.name)<0||!known.Add(entry.name)||entry.file!=entry.name+".png")
                throw new InvalidDataException("Неизвестный или повторный ресурс меню.");
            if(entry.length<24||entry.length>8*1024*1024||entry.sha256==null||entry.sha256.Length!=64||entry.chunks==null||entry.chunks.Length<1||entry.chunks.Length>128)
                throw new InvalidDataException("Неверные параметры ресурса "+entry.name);
            foreach(char c in entry.sha256)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))throw new InvalidDataException("Неверная контрольная сумма "+entry.name);
            for(int i=0;i<entry.chunks.Length;i++)if(entry.chunks[i]!=entry.name+"/"+i.ToString("000")+".b64")
                throw new InvalidDataException("Неверный порядок частей "+entry.name);
        }
        static byte[] Decode(TextureEntry entry){
            int length=((entry.length+2)/3)*4;var encoded=new StringBuilder(length);
            for(int i=0;i<entry.chunks.Length;i++){
                string path=Full(PayloadRoot+entry.chunks[i]);
                if(!File.Exists(path)||new FileInfo(path).Length>ChunkSize+1)throw new InvalidDataException("Отсутствует или повреждена часть "+entry.chunks[i]);
                string part=File.ReadAllText(path,Encoding.ASCII);
                if(part.EndsWith("\n",StringComparison.Ordinal))part=part.Substring(0,part.Length-1);
                if(part.Length<4||part.Length>ChunkSize||part.Length%4!=0||(i<entry.chunks.Length-1&&part.Length!=ChunkSize))
                    throw new InvalidDataException("Неверный размер части "+entry.chunks[i]);
                foreach(char c in part)if(!((c>='A'&&c<='Z')||(c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='+'||c=='/'||c=='='))
                    throw new InvalidDataException("Повреждён текст части "+entry.chunks[i]);
                encoded.Append(part);
            }
            if(encoded.Length!=length)throw new InvalidDataException("Неверная длина данных "+entry.name);
            byte[] bytes=Convert.FromBase64String(encoded.ToString());
            if(bytes.Length!=entry.length||Hash(bytes)!=entry.sha256)throw new InvalidDataException("Контрольная сумма не совпадает: "+entry.name);
            byte[] png={137,80,78,71,13,10,26,10};
            for(int i=0;i<png.Length;i++)if(bytes[i]!=png[i])throw new InvalidDataException("Неверный PNG: "+entry.name);
            if(bytes[12]!='I'||bytes[13]!='H'||bytes[14]!='D'||bytes[15]!='R')throw new InvalidDataException("Неверный заголовок PNG: "+entry.name);
            return bytes;
        }
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static void RecoverInterruptedWrite(string path){
            string backup=path+".install-backup";
            if(!File.Exists(path)&&File.Exists(backup))File.Move(backup,path);
        }
        static void WriteVerified(string path,byte[] bytes){
            string temp=path+".installing",backup=path+".install-backup";
            try{
                File.WriteAllBytes(temp,bytes);
                if(!File.Exists(path)){File.Move(temp,path);return;}
                try{File.Replace(temp,path,null);return;}
                catch(PlatformNotSupportedException){}
                catch(IOException){}
                if(File.Exists(backup))File.Delete(backup);
                File.Move(path,backup);
                try{File.Move(temp,path);}
                catch{if(!File.Exists(path)&&File.Exists(backup))File.Move(backup,path);throw;}
                if(File.Exists(backup))File.Delete(backup);
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
    sealed class IslandArtBuildCheck:IPreprocessBuildWithReport
    {
        public int callbackOrder=>-1000;
        public void OnPreprocessBuild(BuildReport report){
            if(!IslandArtInstaller.EnsureFiles())throw new BuildFailedException("Не удалось подготовить оформление меню. Проверьте полноту папки Brainrot.");
        }
    }
}
