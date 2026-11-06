using System;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Voxel data of one 16x16x16 chunk. A chunk that holds a single block ID stores no array.
    /// Local index is x | z&lt;&lt;4 | y&lt;&lt;8 (see <see cref="VoxelConstants"/>).
    /// </summary>
    public sealed class ChunkData
    {
        /// <summary>Layout version written into saves; bump with a migration when the layout changes.</summary>
        public const int CurrentFormatVersion = 1;

        ushort[] _voxels;
        ushort _uniformId;

        public ChunkData(ushort fillId = BlockRegistry.AirId)
        {
            _uniformId = fillId;
        }

        public bool IsUniform => _voxels == null;
        public bool IsEmpty => _voxels == null && _uniformId == BlockRegistry.AirId;

        /// <summary>Changed since it was last saved or meshed.</summary>
        public bool Dirty { get; set; }

        /// <summary>Edited after generation, so it must be saved rather than regenerated.</summary>
        public bool Modified { get; private set; }

        public int FormatVersion => CurrentFormatVersion;

        public static int Index(int x, int y, int z)
        {
            if ((uint)x >= VoxelConstants.ChunkSize || (uint)y >= VoxelConstants.ChunkSize || (uint)z >= VoxelConstants.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(x), $"Local voxel ({x}, {y}, {z}) is outside the chunk.");
            return x | (z << VoxelConstants.ChunkSizeShift) | (y << (2 * VoxelConstants.ChunkSizeShift));
        }

        public ushort GetBlock(int x, int y, int z)
        {
            int index = Index(x, y, z);
            return _voxels == null ? _uniformId : _voxels[index];
        }

        public void SetBlock(int x, int y, int z, ushort id)
        {
            int index = Index(x, y, z);
            if (_voxels == null)
            {
                if (id == _uniformId)
                    return;
                _voxels = new ushort[VoxelConstants.VoxelsPerChunk];
                if (_uniformId != 0)
                    Array.Fill(_voxels, _uniformId);
            }
            else if (_voxels[index] == id)
            {
                return;
            }

            _voxels[index] = id;
            Dirty = true;
            Modified = true;
        }

        /// <summary>Drops the array when every voxel holds the same ID. Returns true when the chunk is uniform afterwards.</summary>
        public bool TryCompact()
        {
            if (_voxels == null)
                return true;
            ushort first = _voxels[0];
            for (int i = 1; i < _voxels.Length; i++)
            {
                if (_voxels[i] != first)
                    return false;
            }
            _uniformId = first;
            _voxels = null;
            return true;
        }
    }
}
