using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Viwenta.Voxel.EditorTools
{
    /// <summary>Packs the block face textures referenced by a registry into the library's Texture2DArray.</summary>
    public static class BlockTextureBaker
    {
        public static List<Texture2D> CollectSources(BlockRegistryAsset registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var seen = new HashSet<Texture2D>();
            var sources = new List<Texture2D>();
            foreach (BlockDefinitionAsset block in registry.Blocks)
            {
                if (block == null)
                    continue;
                foreach (Texture2D texture in block.ReferencedTextures())
                {
                    if (seen.Add(texture))
                        sources.Add(texture);
                }
            }

            sources.Sort((a, b) =>
            {
                int byPath = string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b));
                return byPath != 0 ? byPath : string.CompareOrdinal(a.name, b.name);
            });
            return sources;
        }

        public static Texture2DArray BuildArray(IReadOnlyList<Texture2D> sources, int faceSize, FilterMode filterMode)
        {
            bool powerOfTwo = faceSize > 0 && (faceSize & (faceSize - 1)) == 0;
            if (!powerOfTwo || faceSize < BlockTextureLibrary.MinFaceSize || faceSize > BlockTextureLibrary.MaxFaceSize)
                throw new ArgumentOutOfRangeException(nameof(faceSize),
                    $"Face size {faceSize} must be a power of two between {BlockTextureLibrary.MinFaceSize} and {BlockTextureLibrary.MaxFaceSize}.");
            if (sources == null || sources.Count == 0)
                throw new ArgumentException("No block textures to bake.", nameof(sources));

            int expectedMips = 1 + (int)Math.Round(Math.Log(faceSize, 2));
            for (int i = 0; i < sources.Count; i++)
            {
                Texture2D t = sources[i];
                if (t == null)
                    throw new ArgumentException($"Block texture {i} is missing.", nameof(sources));
                if (t.width != faceSize || t.height != faceSize)
                    throw new ArgumentException($"Block texture '{t.name}' is {t.width}x{t.height}, expected {faceSize}x{faceSize}.", nameof(sources));
                if (!t.isReadable)
                    throw new ArgumentException($"Block texture '{t.name}' is not readable; put it under {BlockTextureImportRules.Folder} so the import rules apply.", nameof(sources));
                if (t.mipmapCount != expectedMips)
                    throw new ArgumentException($"Block texture '{t.name}' has {t.mipmapCount} mip levels, expected {expectedMips}; enable Generate Mip Maps.", nameof(sources));
            }

            var array = new Texture2DArray(faceSize, faceSize, sources.Count, TextureFormat.RGBA32, true, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = filterMode,
            };
            for (int i = 0; i < sources.Count; i++)
            {
                for (int m = 0; m < expectedMips; m++)
                    array.SetPixels32(sources[i].GetPixels32(m), i, m);
            }
            array.Apply(false, false);
            return array;
        }

        public static void Bake(BlockRegistryAsset registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            BlockTextureLibrary library = registry.Textures;
            if (library == null)
                throw new InvalidOperationException($"Block registry '{registry.name}' has no block texture library.");

            List<Texture2D> sources = CollectSources(registry);
            Texture2DArray array = BuildArray(sources, library.FaceSize, library.FilterMode);
            array.name = $"{library.name}_Array";

            if (library.Array != null)
                AssetDatabase.RemoveObjectFromAsset(library.Array);
            AssetDatabase.AddObjectToAsset(array, library);
            library.SetBakedData(sources.ToArray(), array);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"Baked {sources.Count} block textures into '{library.name}'.", library);
        }

        [MenuItem("Viwenta/Voxel/Bake Block Textures")]
        static void BakeAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:BlockRegistryAsset");
            if (guids.Length == 0)
            {
                Debug.LogWarning("No BlockRegistryAsset found to bake.");
                return;
            }
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                try
                {
                    Bake(AssetDatabase.LoadAssetAtPath<BlockRegistryAsset>(path));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Baking block textures of '{path}' failed: {e.Message}");
                }
            }
        }
    }
}
