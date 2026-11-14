using UnityEditor;
using UnityEngine;
namespace Brainrot.Editor
{
    /// <summary>Keep pixel atlas coordinates stable: never resize non-power-of-two UI textures.</summary>
    public static class TropicalUIImporter
    {
        public const string Root="Assets/Brainrot/Art/UI/Resources/";
        public static void ConfigureAll(){foreach(string file in new[]{"TropicalUIAtlas.png","TropicalMenuBackdrop.png"}){var importer=AssetImporter.GetAtPath(Root+file) as TextureImporter;if(importer!=null&&Configure(importer))importer.SaveAndReimport();}}
        internal static bool Configure(TextureImporter importer){
            bool atlas=importer.assetPath.EndsWith("TropicalUIAtlas.png");
            bool changed=importer.textureType!=TextureImporterType.Default||importer.textureShape!=TextureImporterShape.Texture2D||importer.npotScale!=TextureImporterNPOTScale.None||importer.mipmapEnabled||!importer.sRGBTexture||importer.maxTextureSize!=4096||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.wrapMode!=TextureWrapMode.Clamp||importer.alphaIsTransparency!=atlas;
            if(!changed)return false;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.alphaIsTransparency=atlas;return true;
        }
    }
    sealed class TropicalUIAssetPostprocessor:AssetPostprocessor
    {
        void OnPreprocessTexture(){if(assetPath==TropicalUIImporter.Root+"TropicalUIAtlas.png"||assetPath==TropicalUIImporter.Root+"TropicalMenuBackdrop.png")TropicalUIImporter.Configure((TextureImporter)assetImporter);}
    }
}
