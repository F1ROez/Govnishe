using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Keep pixel atlas coordinates stable: never resize non-power-of-two UI textures.</summary>
    public static class TropicalUIImporter
    {
        public const string Root="Assets/Brainrot/Art/UI/Resources/";
        static readonly string[] Files={"TropicalUIAtlas.png","TropicalMenuBackdrop.png","TropicalPanels.png","TropicalIcons.png","TropicalBoard.png","ReferenceMainMenu.png","ReferencePause.png","ReferenceSettings.png","ReferenceMenuClean.png","ReferencePauseClean.png","ReferenceSettingsClean.png","WaveTitleClean.png","AlphaPauseFrame.png","AlphaSettingsFrame.png","AlphaButtons.png"};
        internal static bool IsArt(string path){foreach(var file in Files)if(path==Root+file)return true;return false;}
        public static void ConfigureAll(){foreach(string file in Files){var importer=AssetImporter.GetAtPath(Root+file) as TextureImporter;if(importer!=null&&Configure(importer))importer.SaveAndReimport();}}
        internal static bool Configure(TextureImporter importer){
            bool atlas=!importer.assetPath.EndsWith("TropicalMenuBackdrop.png");
            bool changed=importer.textureType!=TextureImporterType.Default||importer.textureShape!=TextureImporterShape.Texture2D||importer.npotScale!=TextureImporterNPOTScale.None||importer.mipmapEnabled||!importer.sRGBTexture||importer.maxTextureSize!=4096||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.wrapMode!=TextureWrapMode.Clamp||importer.alphaIsTransparency!=atlas;
            if(!changed)return false;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.alphaIsTransparency=atlas;return true;
        }
    }
    sealed class TropicalUIAssetPostprocessor:AssetPostprocessor
    {
        void OnPreprocessTexture(){if(TropicalUIImporter.IsArt(assetPath))TropicalUIImporter.Configure((TextureImporter)assetImporter);}
    }
}
