using System;

namespace Viwenta.Voxel
{
    /// <summary>
    /// How a block looks, separate from its face shapes: a layer of the block texture array and a tint.
    /// Shape meshes are shared by every block; the skin is applied when a face is attached.
    /// Each face maps the whole layer (u, w in 0..1), so layers never bleed into each other.
    /// </summary>
    public readonly struct BlockSkin
    {
        /// <summary>Index into the block texture array.</summary>
        public readonly ushort Layer;

        /// <summary>Vertex tint packed as R | G&lt;&lt;8 | B&lt;&lt;16 | A&lt;&lt;24.</summary>
        public readonly uint Tint;

        public BlockSkin(ushort layer, uint tint)
        {
            Layer = layer;
            Tint = tint;
        }

        /// <summary>Layer 0, white tint.</summary>
        public static BlockSkin Default => new BlockSkin(0, 0xFFFFFFFFu);
    }

    /// <summary>Immutable description of one block type. IDs are stable and never reused (saves store them).</summary>
    public sealed class BlockDefinition
    {
        public ushort Id { get; }
        public string Name { get; }

        /// <summary>An opaque block hides the faces of its neighbours.</summary>
        public bool IsOpaque { get; }

        public BlockSkin Skin { get; }

        /// <summary>When set, every exposed face of this block uses this shape instead of the shared variant library.</summary>
        public FaceShape FaceShapeOverride { get; }

        public BlockDefinition(ushort id, string name, bool isOpaque, BlockSkin skin, FaceShape faceShapeOverride = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Block name must not be empty.", nameof(name));
            Id = id;
            Name = name;
            IsOpaque = isOpaque;
            Skin = skin;
            FaceShapeOverride = faceShapeOverride;
        }
    }
}
