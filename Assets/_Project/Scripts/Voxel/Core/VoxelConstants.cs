namespace Viwenta.Voxel
{
    /// <summary>
    /// Structural voxel constants. They define array layout and the save format, so changing one
    /// needs a save migration. Tunables (radius, budgets, weights) belong in config, not here.
    /// </summary>
    public static class VoxelConstants
    {
        /// <summary>Chunk edge length in voxels. Must stay a power of two (see <see cref="ChunkSizeShift"/>).</summary>
        public const int ChunkSize = 16;

        /// <summary>log2(<see cref="ChunkSize"/>): world-to-chunk conversion is a shift, local index is x | z&lt;&lt;shift | y&lt;&lt;(2*shift).</summary>
        public const int ChunkSizeShift = 4;

        /// <summary>Mask for the local coordinate inside a chunk.</summary>
        public const int ChunkSizeMask = ChunkSize - 1;

        /// <summary>Number of chunks stacked in one chunk column.</summary>
        public const int ColumnChunkCount = 24;

        public const int ColumnHeightBlocks = ColumnChunkCount * ChunkSize;
        public const int VoxelsPerChunk = ChunkSize * ChunkSize * ChunkSize;
        public const int VoxelsPerColumn = VoxelsPerChunk * ColumnChunkCount;

        /// <summary>Smallest and largest radius (in columns) <see cref="ColumnShapeService"/> accepts.</summary>
        public const int MinShapeRadius = 2;
        public const int MaxShapeRadius = 64;
    }
}
