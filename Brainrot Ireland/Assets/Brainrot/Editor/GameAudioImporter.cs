using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Stream long owner-provided songs; keep the short button click immediately available.</summary>
    public static class GameAudioImporter
    {
        public const string Root="Assets/Brainrot/Art/Audio/Resources/Audio/";
        [MenuItem("Tools/TungTung/Обновить импорт музыки")]
        public static void ConfigureAll()
        {
            if(!AssetDatabase.IsValidFolder(Root.TrimEnd('/')))return;
            foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Root.TrimEnd('/')})){
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(importer!=null&&Configure(importer,path))importer.SaveAndReimport();
            }
        }
        internal static bool Configure(AudioImporter importer,string path)
        {
            bool click=path==Root+"Button.mp3";var settings=importer.defaultSampleSettings;
            var type=click?AudioClipLoadType.DecompressOnLoad:AudioClipLoadType.Streaming;
            var format=click?AudioCompressionFormat.PCM:AudioCompressionFormat.Vorbis;
            bool changed=settings.loadType!=type||settings.preloadAudioData!=click||settings.compressionFormat!=format||importer.forceToMono||importer.loadInBackground||!Mathf.Approximately(settings.quality,.75f)||settings.sampleRateSetting!=AudioSampleRateSetting.OptimizeSampleRate;
            if(!changed)return false;
            settings.loadType=type;settings.preloadAudioData=click;settings.compressionFormat=format;settings.quality=.75f;settings.sampleRateSetting=AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=false;
            return true;
        }
    }
    sealed class MusicAssetPostprocessor:AssetPostprocessor
    {
        void OnPreprocessAudio(){if(assetPath.StartsWith(GameAudioImporter.Root,System.StringComparison.Ordinal))GameAudioImporter.Configure((AudioImporter)assetImporter,assetPath);}
    }
}
