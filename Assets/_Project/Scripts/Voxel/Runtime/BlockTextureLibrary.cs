using System.Collections.Generic;
using UnityEngine;

namespace Viwenta.Voxel
{
    /// <summary>
    /// The block texture array and which source texture sits on which layer. Filled by the baker
    /// (Viwenta > Voxel > Bake Block Textures); layers are looked up when a registry is built, never stored in block data.
    /// </summary>
    [CreateAssetMenu(menuName = "Viwenta/Voxel/Block Texture Library", fileName = "BlockTextures")]
    public sealed class BlockTextureLibrary : ScriptableObject
    {
        public const int MinFaceSize = 4;
        public const int MaxFaceSize = 512;

        [Tooltip("Width and height in pixels of every block face texture. Power of two.")]
        [SerializeField] int faceSize = 32;
        [SerializeField] FilterMode filterMode = FilterMode.Point;
        [Tooltip("Written by the baker: layer i of the array is layers[i].")]
        [SerializeField] Texture2D[] layers = System.Array.Empty<Texture2D>();
        [Tooltip("Written by the baker; stored inside this asset.")]
        [SerializeField] Texture2DArray array;

        public int FaceSize => faceSize;
        public FilterMode FilterMode => filterMode;
        public Texture2DArray Array => array;
        public IReadOnlyList<Texture2D> Layers => layers;

        /// <summary>Layer of <paramref name="texture"/> in the baked array; false when it was not baked.</summary>
        public bool TryGetLayer(Texture2D texture, out ushort layer)
        {
            layer = 0;
            if (texture == null)
                return false;
            for (int i = 0; i < layers.Length; i++)
            {
                if (ReferenceEquals(layers[i], texture))
                {
                    layer = (ushort)i;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Called by the baker only. Replaces the layer list and the array reference.</summary>
        public void SetBakedData(Texture2D[] bakedLayers, Texture2DArray bakedArray)
        {
            if (bakedLayers == null)
                throw new System.ArgumentNullException(nameof(bakedLayers));
            if (bakedArray == null)
                throw new System.ArgumentNullException(nameof(bakedArray));
            layers = bakedLayers;
            array = bakedArray;
        }
    }
}
