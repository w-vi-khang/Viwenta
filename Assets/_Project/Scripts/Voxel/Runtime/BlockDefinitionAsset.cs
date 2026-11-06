using System;
using System.Collections.Generic;
using UnityEngine;

namespace Viwenta.Voxel
{
    /// <summary>Editor-authored block definition. Converted to a <see cref="BlockDefinition"/> when the registry is built.</summary>
    [CreateAssetMenu(menuName = "Viwenta/Voxel/Block Definition", fileName = "Block")]
    public sealed class BlockDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public sealed class FaceSkin
        {
            [Tooltip("Face texture under Assets/_Project/Art/Blocks/. Leave empty on Side/Bottom to reuse Top.")]
            public Texture2D texture;
            public Color32 tint = new Color32(255, 255, 255, 255);
        }

        [Tooltip("Stable ID stored in saves. Never reuse an ID after removing a block. 0 is air.")]
        [SerializeField] ushort id;
        [SerializeField] string blockName = "block";
        [Tooltip("Opaque blocks hide the faces of their neighbours.")]
        [SerializeField] bool isOpaque = true;

        [SerializeField] FaceSkin top = new FaceSkin();
        [SerializeField] FaceSkin side = new FaceSkin();
        [SerializeField] FaceSkin bottom = new FaceSkin();

        public ushort Id => id;

        /// <summary>The non-null textures of the top, side and bottom slots, in that order.</summary>
        public IEnumerable<Texture2D> ReferencedTextures()
        {
            if (top.texture != null) yield return top.texture;
            if (side.texture != null) yield return side.texture;
            if (bottom.texture != null) yield return bottom.texture;
        }

        public BlockDefinition ToDefinition(BlockTextureLibrary textures)
        {
            if (textures == null)
                throw new ArgumentNullException(nameof(textures));
            if (top.texture == null)
                throw new InvalidOperationException($"Block '{blockName}' (id {id}) has no top texture.");

            BlockSkin topSkin = Resolve(top, "top", textures);
            BlockSkin sideSkin = Resolve(side.texture == null ? top : side, "side", textures);
            BlockSkin bottomSkin = Resolve(bottom.texture == null ? top : bottom, "bottom", textures);
            return new BlockDefinition(id, blockName, isOpaque, topSkin, sideSkin, bottomSkin);
        }

        BlockSkin Resolve(FaceSkin slot, string slotName, BlockTextureLibrary textures)
        {
            if (!textures.TryGetLayer(slot.texture, out ushort layer))
                throw new InvalidOperationException(
                    $"Block '{blockName}' (id {id}): {slotName} texture '{slot.texture.name}' is not in block texture library '{textures.name}'. Run Viwenta > Voxel > Bake Block Textures.");
            Color32 tint = slot.tint;
            uint packedTint = (uint)(tint.r | (tint.g << 8) | (tint.b << 16) | (tint.a << 24));
            return new BlockSkin(layer, packedTint);
        }
    }
}
