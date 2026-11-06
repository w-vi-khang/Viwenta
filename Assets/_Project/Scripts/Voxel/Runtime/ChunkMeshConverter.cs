using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Main-thread step that turns the engine-free <see cref="ChunkMeshData"/> into a Unity <see cref="Mesh"/>.
    /// The vertex is packed to 24 bytes: position and uv as half floats, normal as signed bytes, tint as bytes.
    /// Unity requires attributes in the order position, normal, colour, uv; the struct and layout below follow it.
    /// Chunk-local positions stay within 0..16, where a half float resolves 1/64 or finer.
    /// Every non-empty mesh has two submeshes sharing one index buffer: <see cref="OpaqueSubMesh"/> then
    /// <see cref="TransparentSubMesh"/> (possibly empty), so every chunk takes the same two materials.
    /// </summary>
    public static class ChunkMeshConverter
    {
        public const int VertexStrideBytes = 24;

        public const int OpaqueSubMesh = 0;
        public const int TransparentSubMesh = 1;

        /// <summary>Largest vertex count that still fits 16-bit indices.</summary>
        public const int MaxVerticesFor16BitIndices = ushort.MaxValue + 1;

        struct PackedVertex
        {
            public ushort PosX, PosY, PosZ, PosW;
            public sbyte NormalX, NormalY, NormalZ, NormalW;
            public Color32 Color;
            public ushort U, V, Layer, UvW;
        }

        static readonly VertexAttributeDescriptor[] Layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 4),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4),
        };

        /// <summary>True when <paramref name="vertexCount"/> vertices can be indexed with 16-bit indices.</summary>
        public static bool Fits16BitIndices(int vertexCount) => vertexCount <= MaxVerticesFor16BitIndices;

        /// <summary>
        /// Fills <paramref name="mesh"/> from <paramref name="data"/>; the mesh is cleared first.
        /// Uses 16-bit indices when the vertex count allows it, 32-bit otherwise. An empty mesh gets no submeshes.
        /// </summary>
        public static void Apply(ChunkMeshData data, Mesh mesh)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));

            mesh.Clear();
            int vertexCount = data.VertexCount;
            if (vertexCount == 0)
                return;

            var vertices = new PackedVertex[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                uint tint = data.Colors[i];
                vertices[i] = new PackedVertex
                {
                    PosX = HalfConverter.ToHalf(data.Positions[3 * i]),
                    PosY = HalfConverter.ToHalf(data.Positions[3 * i + 1]),
                    PosZ = HalfConverter.ToHalf(data.Positions[3 * i + 2]),
                    NormalX = ToSNorm8(data.Normals[3 * i]),
                    NormalY = ToSNorm8(data.Normals[3 * i + 1]),
                    NormalZ = ToSNorm8(data.Normals[3 * i + 2]),
                    U = HalfConverter.ToHalf(data.Uvs[3 * i]),
                    V = HalfConverter.ToHalf(data.Uvs[3 * i + 1]),
                    Layer = HalfConverter.ToHalf(data.Uvs[3 * i + 2]),
                    Color = new Color32((byte)tint, (byte)(tint >> 8), (byte)(tint >> 16), (byte)(tint >> 24)),
                };
            }

            int opaqueCount = data.Indices.Count;
            int indexCount = opaqueCount + data.TransparentIndices.Count;
            bool wide = !Fits16BitIndices(vertexCount);
            mesh.SetVertexBufferParams(vertexCount, Layout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);
            if (wide)
            {
                var all = new int[indexCount];
                data.Indices.CopyTo(all, 0);
                data.TransparentIndices.CopyTo(all, opaqueCount);
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
                mesh.SetIndexBufferData(all, 0, 0, indexCount);
            }
            else
            {
                var narrow = new ushort[indexCount];
                for (int i = 0; i < opaqueCount; i++)
                    narrow[i] = (ushort)data.Indices[i];
                for (int i = opaqueCount; i < indexCount; i++)
                    narrow[i] = (ushort)data.TransparentIndices[i - opaqueCount];
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
                mesh.SetIndexBufferData(narrow, 0, 0, indexCount);
            }
            mesh.subMeshCount = 2;
            mesh.SetSubMesh(OpaqueSubMesh, new SubMeshDescriptor(0, opaqueCount));
            mesh.SetSubMesh(TransparentSubMesh, new SubMeshDescriptor(opaqueCount, indexCount - opaqueCount));
            mesh.RecalculateBounds();
        }

        static sbyte ToSNorm8(float value) => (sbyte)Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * 127f);
    }
}
