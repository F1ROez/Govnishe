using System;
using System.IO;
using UnityEngine;

namespace Brainrot
{
    [Serializable]
    public sealed class SaveData
    {
        public float BestTime;
        public int BestKills;
        public int BestLevel;
        public float Volume = .7f;
        public float Sensitivity = 1f;
        public int Quality = 1;
        public bool CameraLocked = true;
        public bool HasCameraPreference;
        public bool ShowAttackRange = true;
    }

    public static class SaveSystem
    {
        public static SaveData Data { get; private set; } = new SaveData();
        private static string SavePath => Path.Combine(Application.persistentDataPath, "brainrot-save.json");

        public static void Load()
        {
            Data = new SaveData();
            if (TryLoad(SavePath) || TryLoad(SavePath + ".bak")) Sanitize();
        }

        private static bool TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) return false;
                var restored = JsonUtility.FromJson<SaveData>(json);
                if (restored == null) return false;
                // Old saves predate camera/range settings. Keep records and migrate defaults.
                if (!json.Contains("\"CameraLocked\"")) restored.CameraLocked = true;
                if (!json.Contains("\"ShowAttackRange\"")) restored.ShowAttackRange = true;
                Data = restored;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Debug.LogWarning("Не удалось прочитать сохранение: " + exception.Message);
                return false;
            }
        }

        public static void Save()
        {
            try
            {
                Sanitize();
                Directory.CreateDirectory(Application.persistentDataPath);
                string path = SavePath;
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Data, true));
                if (File.Exists(path))
                {
                    // Same-volume replacement keeps the previous file valid if writing is interrupted.
                    try { File.Replace(temporary, path, path + ".bak"); }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(path, path + ".bak", true);
                        File.Copy(temporary, path, true);
                        File.Delete(temporary);
                    }
                }
                else File.Move(temporary, path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Debug.LogWarning("Не удалось записать сохранение: " + exception.Message);
            }
        }

        public static void Record(float time,int kills,int level)
        {
            Data.BestTime = Mathf.Max(Data.BestTime, time);
            Data.BestKills = Mathf.Max(Data.BestKills, kills);
            Data.BestLevel = Mathf.Max(Data.BestLevel, level);
            Save();
        }

        private static void Sanitize()
        {
            Data.BestTime = Finite(Data.BestTime) ? Mathf.Max(0, Data.BestTime) : 0;
            Data.BestKills = Mathf.Max(0, Data.BestKills);
            Data.BestLevel = Mathf.Max(0, Data.BestLevel);
            Data.Volume = Finite(Data.Volume) ? Mathf.Clamp01(Data.Volume) : .7f;
            Data.Sensitivity = Finite(Data.Sensitivity) ? Mathf.Clamp(Data.Sensitivity,.2f,3f) : 1f;
            Data.Quality = Mathf.Clamp(Data.Quality, 0, 2);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
