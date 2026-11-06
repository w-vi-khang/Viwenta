using System;

namespace Viwenta.Voxel
{
    /// <summary>
    /// A chunk plus its 26 neighbours, read through chunk-local coordinates that may spill one chunk
    /// outside the centre (-16..31). A missing neighbour reads as air.
    /// </summary>
    public sealed class ChunkNeighborhood
    {
        readonly ChunkData[] _chunks = new ChunkData[27];

        public ChunkNeighborhood(ChunkData center)
        {
            Center = center ?? throw new ArgumentNullException(nameof(center));
            _chunks[SlotOf(0, 0, 0)] = center;
        }

        public ChunkData Center { get; }

        /// <summary>Sets the neighbour at chunk offset (dx, dy, dz), each in -1..1 and not all zero.</summary>
        public void SetNeighbor(int dx, int dy, int dz, ChunkData chunk)
        {
            if (dx == 0 && dy == 0 && dz == 0)
                throw new ArgumentException("Offset (0, 0, 0) is the centre chunk.");
            _chunks[SlotOf(dx, dy, dz)] = chunk;
        }

        public ushort GetBlock(int x, int y, int z)
        {
            ChunkData chunk = _chunks[SlotOf(x >> VoxelConstants.ChunkSizeShift, y >> VoxelConstants.ChunkSizeShift, z >> VoxelConstants.ChunkSizeShift)];
            return chunk == null
                ? BlockRegistry.AirId
                : chunk.GetBlock(x & VoxelConstants.ChunkSizeMask, y & VoxelConstants.ChunkSizeMask, z & VoxelConstants.ChunkSizeMask);
        }

        static int SlotOf(int dx, int dy, int dz)
        {
            if ((uint)(dx + 1) > 2 || (uint)(dy + 1) > 2 || (uint)(dz + 1) > 2)
                throw new ArgumentOutOfRangeException(nameof(dx), $"Chunk offset ({dx}, {dy}, {dz}) is more than one chunk from the centre.");
            return (dx + 1) + (dz + 1) * 3 + (dy + 1) * 9;
        }
    }
}
