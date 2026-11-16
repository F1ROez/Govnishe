using System;
using System.IO;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Validated Windows-alpha checkpoint. Availability is cached; UI never reads disk each frame.</summary>
    public static class RunPersistence
    {
        static RunSaveStore store;
        static string overridePath;
        static RunSnapshot checkpoint;
        static bool loaded;
        static RunSaveStore Store=>store??(store=new RunSaveStore(overridePath??Path.Combine(Application.persistentDataPath,"brainrot-run.json")));
        public static bool HasSavedRun {get{EnsureLoaded();return checkpoint!=null;}}
        static void EnsureLoaded(){if(loaded)return;loaded=true;Store.TryLoad(out checkpoint);}
        public static void Refresh(){loaded=false;checkpoint=null;EnsureLoaded();}
        public static bool TryLoad(out RunSnapshot snapshot){EnsureLoaded();snapshot=checkpoint;return snapshot!=null;}
        public static bool Save(RunSnapshot snapshot){
            if(!RunSaveStore.IsValid(snapshot))return false;
            if(!Store.Save(snapshot))return false;checkpoint=snapshot;loaded=true;return true;
        }
        public static void Clear(){Store.Clear();checkpoint=null;loaded=true;}
        internal static IDisposable UsePath(string path)=>new StoreScope(path);
        sealed class StoreScope:IDisposable {
            readonly RunSaveStore previousStore;readonly string previousPath;readonly RunSnapshot previousCheckpoint;readonly bool previousLoaded;bool disposed;
            public StoreScope(string path){previousStore=store;previousPath=overridePath;previousCheckpoint=checkpoint;previousLoaded=loaded;overridePath=path;store=null;checkpoint=null;loaded=false;}
            public void Dispose(){if(disposed)return;disposed=true;overridePath=previousPath;store=previousStore;checkpoint=previousCheckpoint;loaded=previousLoaded;}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){store=null;checkpoint=null;loaded=false;}
    }
    /// <summary>Path-injected store allows corruption/recovery tests without touching the player's own saves.</summary>
    public sealed class RunSaveStore
    {
        public const int MaximumBytes=256*1024;
        readonly string path;
        string Backup=>path+".bak";
        string Cleared=>path+".cleared";
        public RunSaveStore(string path){if(string.IsNullOrEmpty(path))throw new ArgumentException("Checkpoint path missing.",nameof(path));this.path=Path.GetFullPath(path);}
        public bool TryLoad(out RunSnapshot snapshot){
            snapshot=null;
            try{if(File.Exists(Cleared))return false;return Read(path,out snapshot)||Read(Backup,out snapshot);}
            catch(Exception e) when(StorageError(e)){Debug.LogWarning("Не удалось открыть сохранённый забег: "+e.Message);return false;}
        }
        static bool Read(string file,out RunSnapshot snapshot){
            snapshot=null;
            try{
                if(!File.Exists(file)||new FileInfo(file).Length>MaximumBytes)return false;
                string json=File.ReadAllText(file);
                if(!LooksLikeSnapshot(json))return false;
                var value=JsonUtility.FromJson<RunSnapshot>(json);
                if(!IsValid(value))return false;snapshot=value;return true;
            }catch(Exception e) when(StorageError(e)||e is ArgumentException){return false;}
        }
        public bool Save(RunSnapshot snapshot){
            if(!IsValid(snapshot))return false;
            string temporary=path+".tmp";
            try{
                string json=JsonUtility.ToJson(snapshot,false);
                byte[] bytes=System.Text.Encoding.UTF8.GetBytes(json);if(bytes.Length>MaximumBytes)return false;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path)){
                    // Never replace a known-good backup with a corrupt primary recovered during load.
                    bool primaryValid=Read(path,out _);
                    try{File.Replace(temporary,path,primaryValid?Backup:null);}
                    catch(PlatformNotSupportedException){if(primaryValid)File.Copy(path,Backup,true);File.Copy(temporary,path,true);File.Delete(temporary);}
                }else File.Move(temporary,path);
                if(File.Exists(Cleared))File.Delete(Cleared);
                return true;
            }catch(Exception e) when(StorageError(e)){Debug.LogWarning("Не удалось сохранить забег: "+e.Message);return false;}
            finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(Exception e) when(StorageError(e)){} }
        }
        public void Clear(){
            // A tombstone prevents recovery of a dead/abandoned run even if a backup cannot be deleted.
            try{Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(Cleared,"1");}
            catch(Exception e) when(StorageError(e)){Debug.LogWarning("Не удалось отметить завершённый забег: "+e.Message);}
            foreach(string file in new[]{path,Backup,path+".tmp"})try{if(File.Exists(file))File.Delete(file);}catch(Exception e) when(StorageError(e)){Debug.LogWarning("Не удалось удалить старый забег: "+e.Message);}
        }
        public static bool IsValid(RunSnapshot s){
            if(s==null||s.Version!=RunSnapshot.CurrentVersion||!Finite(s.Elapsed,0,31536000)||s.Kills<0||s.Player==null||s.Wave==null||s.Spawner==null)return false;
            var p=s.Player;
            if(!Position(p.Position,40)||!Finite(p.Health,.001f,1000000)||!Finite(p.Stamina,0,1000000)||!Angle(p.RotationYaw)||!Angle(p.CameraYaw)||!Angle(p.CameraPitch)||!Finite(p.DashCooldown,0,86400)||!Finite(p.HeavyCooldown,0,86400))return false;
            var w=s.Wave;
            if(w.Number<1||w.Number>1000000||!Finite(w.Time,0,86400)||!Finite(w.RestTime,0,86400)||w.WaitingForClear&&!w.Resting)return false;
            var spawn=s.Spawner;
            if(!Finite(spawn.SpawnClock,0,86400)||spawn.AnnouncedWave<0||spawn.AnnouncedWave>w.Number||spawn.Enemies==null||spawn.Enemies.Length>100)return false;
            bool legacyBudget=spawn.BudgetWave==0&&spawn.PlannedCount==0&&spawn.SpawnedCount==0&&spawn.ResolvedCount==0;
            if(!legacyBudget&&(spawn.BudgetWave!=w.Number||spawn.PlannedCount<1||spawn.PlannedCount>10000||spawn.SpawnedCount<0||spawn.SpawnedCount>spawn.PlannedCount||spawn.ResolvedCount<0||spawn.ResolvedCount>spawn.SpawnedCount||spawn.SpawnedCount-spawn.ResolvedCount!=spawn.Enemies.Length))return false;
            if(s.ChallengeMaskEarnedThisRun<0)return false;
            foreach(var enemy in spawn.Enemies){
                if(enemy==null||enemy.Kind!=0&&enemy.Kind!=9&&enemy.Kind!=10||!Position(enemy.Position,100)||!Angle(enemy.RotationYaw)||!Finite(enemy.MaxHealth,.001f,1000000)||!Finite(enemy.Health,.001f,enemy.MaxHealth))return false;
            }
            return true;
        }
        static bool Position(Vector3 p,float extent)=>Finite(p.x,-extent,extent)&&Finite(p.z,-extent,extent)&&Finite(p.y,-12,40);
        static bool Angle(float value)=>Finite(value,-10000000,10000000);
        static bool Finite(float value,float min,float max)=>!float.IsNaN(value)&&!float.IsInfinity(value)&&value>=min&&value<=max;
        static bool StorageError(Exception e)=>e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException;
        static bool LooksLikeSnapshot(string json){
            if(string.IsNullOrWhiteSpace(json)||json.Length>MaximumBytes||!json.TrimStart().StartsWith("{")||!json.Contains("\"Version\"")||!json.Contains("\"Player\"")||!json.Contains("\"Wave\"")||!json.Contains("\"Spawner\""))return false;
            int depth=0;bool quoted=false,escaped=false;
            foreach(char c in json){
                if(quoted){if(escaped)escaped=false;else if(c=='\\')escaped=true;else if(c=='\"')quoted=false;continue;}
                if(c=='\"'){quoted=true;continue;}
                if(c=='{'||c=='['){if(++depth>12)return false;}else if(c=='}'||c==']'){if(--depth<0)return false;}
            }
            return depth==0&&!quoted;
        }
    }
}
