using System;
using System.IO;
using UnityEngine;

namespace Brainrot
{
    [Serializable]
    public sealed class SaveData
    {
        public int Coins;
        public int[] UpgradeRanks=Array.Empty<int>();
        public string CoinRunId="";
        public int CoinRunEarned;
        public int[] ClaimedCoinIds=Array.Empty<int>();
        public float BestTime;
        public int BestKills;
        public int BestLevel;
        public int BestWave;
        public bool TutorialCompleted;
        public int UnlockedChallengeMask;
        public float Volume = .7f;
        public float Sensitivity = 1f;
        public int Quality = 1;
        public bool CameraLocked = true;
        public bool HasCameraPreference;
        public bool ShowAttackRange = true;
        public float CameraShakeStrength = 1f;
    }

    public static class SaveSystem
    {
        public static SaveData Data { get; private set; } = new SaveData();
        static string storageDirectory;
        private static string SavePath => Path.Combine(storageDirectory??Application.persistentDataPath, "brainrot-save.json");
        /// <summary>Scoped storage sandbox for automated validation, without touching a player's real saves.</summary>
        public static IDisposable UseStorageDirectory(string directory){return new StorageScope(directory);}
        sealed class StorageScope:IDisposable {
            readonly string previousDirectory;readonly SaveData previousData;readonly IDisposable runScope;bool disposed;
            public StorageScope(string directory){
                if(string.IsNullOrEmpty(directory))throw new ArgumentException("Storage directory missing.",nameof(directory));
                directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);
                previousDirectory=storageDirectory;previousData=Data;
                runScope=RunPersistence.UsePath(Path.Combine(directory,"brainrot-run.json"));storageDirectory=directory;Load();
            }
            public void Dispose(){if(disposed)return;disposed=true;storageDirectory=previousDirectory;Data=previousData;runScope.Dispose();}
        }

        public static void Load()
        {
            Data = new SaveData();
            if (TryLoad(SavePath) || TryLoad(SavePath + ".bak")) Sanitize();
            RunPersistence.Refresh();
        }

        private static bool TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length>512*1024) return false;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) return false;
                var restored = JsonUtility.FromJson<SaveData>(json);
                if (restored == null) return false;
                // Old saves predate camera/range settings. Keep records and migrate defaults.
                if (!json.Contains("\"CameraLocked\"")) restored.CameraLocked = true;
                if (!json.Contains("\"ShowAttackRange\"")) restored.ShowAttackRange = true;
                if (!json.Contains("\"CameraShakeStrength\"")) restored.CameraShakeStrength = 1f;
                Data = restored;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Debug.LogWarning("Не удалось прочитать сохранение: " + exception.Message);
                return false;
            }
        }

        public static void Save(){TrySave();}

        /// <summary>Returns success so wallet transactions can roll back after a storage error.</summary>
        public static bool TrySave()
        {
            try
            {
                Sanitize();
                string path = SavePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(Data, true));
                if(bytes.Length>512*1024){Debug.LogWarning("Сохранение превышает допустимый размер.");return false;}
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){
                    stream.Write(bytes,0,bytes.Length);stream.Flush(true);
                }
                if (File.Exists(path))
                {
                    // A corrupt primary must not overwrite the last good backup after recovery.
                    string backup=IsReadableProfile(path)?path+".bak":null;
                    // Same-volume replacement keeps the previous file valid if writing is interrupted.
                    try { File.Replace(temporary, path, backup); }
                    catch (PlatformNotSupportedException)
                    {
                        if(backup!=null)File.Copy(path, backup, true);
                        File.Copy(temporary, path, true);
                        File.Delete(temporary);
                    }
                }
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Debug.LogWarning("Не удалось записать сохранение: " + exception.Message);
                return false;
            }
            finally
            {
                try{string temporary=SavePath+".tmp";if(File.Exists(temporary))File.Delete(temporary);}
                catch(Exception exception) when(exception is IOException||exception is UnauthorizedAccessException){}
            }
        }

        static bool IsReadableProfile(string path)
        {
            try{
                if(!File.Exists(path)||new FileInfo(path).Length>512*1024)return false;
                string json=File.ReadAllText(path);
                return !string.IsNullOrWhiteSpace(json)&&json.TrimStart().StartsWith("{")&&JsonUtility.FromJson<SaveData>(json)!=null;
            }
            catch(Exception exception) when(exception is IOException||exception is UnauthorizedAccessException||exception is ArgumentException){return false;}
        }

        public static void Record(float time,int kills,int level,int wave=0)
        {
            Data.BestTime = Mathf.Max(Data.BestTime, time);
            Data.BestKills = Mathf.Max(Data.BestKills, kills);
            Data.BestLevel = Mathf.Max(Data.BestLevel, level);
            Data.BestWave = Mathf.Max(Data.BestWave, wave);
            Save();
        }

        private static void Sanitize()
        {
            MetaProgression.Sanitize(Data);
            Data.BestTime = Finite(Data.BestTime) ? Mathf.Max(0, Data.BestTime) : 0;
            Data.BestKills = Mathf.Max(0, Data.BestKills);
            Data.BestLevel = Mathf.Max(0, Data.BestLevel);
            Data.BestWave = Mathf.Clamp(Data.BestWave,0,1000000);
            Data.UnlockedChallengeMask = Mathf.Max(0,Data.UnlockedChallengeMask);
            Data.Volume = Finite(Data.Volume) ? Mathf.Clamp01(Data.Volume) : .7f;
            Data.Sensitivity = Finite(Data.Sensitivity) ? Mathf.Clamp(Data.Sensitivity,.2f,3f) : 1f;
            Data.Quality = Mathf.Clamp(Data.Quality, 0, 2);
            Data.CameraShakeStrength=Finite(Data.CameraShakeStrength)?Mathf.Clamp01(Data.CameraShakeStrength):1f;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
