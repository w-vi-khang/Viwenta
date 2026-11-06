using UnityEngine;

namespace Viwenta.Voxel
{
    /// <summary>Editor-authored block definition. Converted to a <see cref="BlockDefinition"/> when the registry is built.</summary>
    [CreateAssetMenu(menuName = "Viwenta/Voxel/Block Definition", fileName = "Block")]
    public sealed class BlockDefinitionAsset : ScriptableObject
    {
        [Tooltip("Stable ID stored in saves. Never reuse an ID after removing a block. 0 is air.")]
        [SerializeField] ushort id;
        [SerializeField] string blockName = "block";
        [Tooltip("Opaque blocks hide the faces of their neighbours.")]
        [SerializeField] bool isOpaque = true;
        [Tooltip("Layer of the block texture array this block's faces use.")]
        [SerializeField] ushort textureLayer;
        [SerializeField] Color32 tint = new Color32(255, 255, 255, 255);

        public ushort Id => id;

        public BlockDefinition ToDefinition()
        {
            uint packedTint = (uint)(tint.r | (tint.g << 8) | (tint.b << 16) | (tint.a << 24));
            return new BlockDefinition(id, blockName, isOpaque, new BlockSkin(textureLayer, packedTint));
        }
    }
}
