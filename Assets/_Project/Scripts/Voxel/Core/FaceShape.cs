using System;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Prebuilt geometry of one voxel face in face-local space (see <see cref="FaceFrame"/>): positions are
    /// (u, w, h) triples, normals are (nu, nw, nh) triples. Triangles must face along +h in the Unity winding
    /// (cross(b - a, c - a) along +h). Uvs are optional; when null the builder derives them from (u, w).
    /// Plain arrays, no UnityEngine types, so shapes can be read on background threads.
    /// </summary>
    public sealed class FaceShape
    {
        public float[] Positions { get; }
        public float[] Normals { get; }
        public float[] Uvs { get; }
        public int[] Indices { get; }

        public int VertexCount => Positions.Length / 3;

        public FaceShape(float[] positions, float[] normals, int[] indices, float[] uvs = null)
        {
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            Normals = normals ?? throw new ArgumentNullException(nameof(normals));
            Indices = indices ?? throw new ArgumentNullException(nameof(indices));
            Uvs = uvs;

            if (positions.Length == 0 || positions.Length % 3 != 0)
                throw new ArgumentException("Positions must hold a non-zero multiple of 3 floats.", nameof(positions));
            if (normals.Length != positions.Length)
                throw new ArgumentException("Normals must have the same length as positions.", nameof(normals));
            if (uvs != null && uvs.Length != VertexCount * 2)
                throw new ArgumentException("Uvs must hold 2 floats per vertex.", nameof(uvs));
            if (indices.Length == 0 || indices.Length % 3 != 0)
                throw new ArgumentException("Indices must hold a non-zero multiple of 3 entries.", nameof(indices));
            for (int i = 0; i < indices.Length; i++)
            {
                if ((uint)indices[i] >= (uint)VertexCount)
                    throw new ArgumentException($"Index {indices[i]} at position {i} is outside the {VertexCount} vertices.", nameof(indices));
            }
        }

        /// <summary>The plain square face: 4 vertices, 2 triangles.</summary>
        public static FaceShape Flat { get; } = new FaceShape(
            new float[] { 0, 0, 0,  0, 1, 0,  1, 1, 0,  1, 0, 0 },
            new float[] { 0, 0, 1,  0, 0, 1,  0, 0, 1,  0, 0, 1 },
            new[] { 0, 1, 2, 0, 2, 3 });
    }
}
