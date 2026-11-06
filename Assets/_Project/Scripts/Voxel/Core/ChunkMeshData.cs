using System.Collections.Generic;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Mesh of one chunk as plain lists, free of UnityEngine so it can be built on a background thread.
    /// Positions and normals hold 3 floats per vertex, Uvs 3 (u, v, texture array layer), Colors one packed RGBA uint
    /// (see <see cref="BlockSkin.Tint"/>).
    /// </summary>
    public sealed class ChunkMeshData
    {
        public readonly List<float> Positions = new List<float>();
        public readonly List<float> Normals = new List<float>();
        public readonly List<float> Uvs = new List<float>();
        public readonly List<uint> Colors = new List<uint>();
        public readonly List<int> Indices = new List<int>();

        public int VertexCount => Colors.Count;
        public int TriangleCount => Indices.Count / 3;

        public void Clear()
        {
            Positions.Clear();
            Normals.Clear();
            Uvs.Clear();
            Colors.Clear();
            Indices.Clear();
        }
    }
}
