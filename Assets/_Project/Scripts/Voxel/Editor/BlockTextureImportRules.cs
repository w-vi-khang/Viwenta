using System;
using UnityEditor;

namespace Viwenta.Voxel.EditorTools
{
    /// <summary>
    /// Applies the block texture mold (see Docs/Design/AssetTemplates.md) to every texture under Assets/_Project/Art/Blocks/,
    /// so the baker can read it.
    /// </summary>
    public sealed class BlockTextureImportRules : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/Blocks/";

        public static bool Applies(string assetPath)
        {
            return assetPath != null && assetPath.StartsWith(Folder, StringComparison.Ordinal);
        }

        void OnPreprocessTexture()
        {
            if (!Applies(assetPath))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.isReadable = true;
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            importer.filterMode = UnityEngine.FilterMode.Point;
        }
    }
}
