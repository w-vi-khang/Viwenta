using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Viwenta.Voxel.Tests
{
    public class ChunkMeshConverterTests
    {
        [TestCase(0f, 0x0000)]
        [TestCase(1f, 0x3C00)]
        [TestCase(0.5f, 0x3800)]
        [TestCase(16f, 0x4C00)]
        [TestCase(-1f, 0xBC00)]
        [TestCase(0.1f, 0x2E66)]
        [TestCase(70000f, 0x7C00)]
        public void ToHalf_MatchesKnownValues(float value, int expected)
        {
            Assert.AreEqual((ushort)expected, HalfConverter.ToHalf(value));
        }

        [Test]
        public void ToHalf_ChunkRange_RoundTripsWithinResolution()
        {
            for (float v = 0f; v <= 16f; v += 0.013f)
            {
                float back = FromHalf(HalfConverter.ToHalf(v));
                Assert.AreEqual(v, back, 1f / 64f, $"value {v}");
            }
        }

        static float FromHalf(ushort half)
        {
            int exponent = (half >> 10) & 0x1F;
            float mantissa = (half & 0x3FF) / 1024f;
            float magnitude = exponent == 0
                ? mantissa * Mathf.Pow(2f, -14f)
                : (1f + mantissa) * Mathf.Pow(2f, exponent - 15);
            return (half & 0x8000) != 0 ? -magnitude : magnitude;
        }

        static ChunkMeshData SingleBlockMesh()
        {
            var registry = new BlockRegistry();
            registry.Register(new BlockDefinition(1, "stone", true, new BlockSkin(3, 0xFF8040C0u)));
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, 1);
            var data = new ChunkMeshData();
            ChunkMeshBuilder.Build(new ChunkNeighborhood(chunk), registry, new FaceShapeLibrary(), 0, data);
            return data;
        }

        [Test]
        public void Apply_SmallMesh_UsesPackedLayoutAnd16BitIndices()
        {
            ChunkMeshData data = SingleBlockMesh();
            var mesh = new Mesh();

            ChunkMeshConverter.Apply(data, mesh);

            Assert.AreEqual(24, mesh.vertexCount);
            Assert.AreEqual(IndexFormat.UInt16, mesh.indexFormat);
            Assert.AreEqual(36u, mesh.GetIndexCount(0));
            Assert.AreEqual(ChunkMeshConverter.VertexStrideBytes, mesh.GetVertexBufferStride(0));
            Assert.AreEqual(VertexAttributeFormat.Float16, mesh.GetVertexAttributeFormat(VertexAttribute.Position));
            Assert.AreEqual(VertexAttributeFormat.SNorm8, mesh.GetVertexAttributeFormat(VertexAttribute.Normal));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Apply_KeepsPositionsNormalsLayerAndTint()
        {
            ChunkMeshData data = SingleBlockMesh();
            var mesh = new Mesh();

            ChunkMeshConverter.Apply(data, mesh);

            Vector3[] positions = mesh.vertices;
            Vector3[] normals = mesh.normals;
            var uvs = new System.Collections.Generic.List<Vector3>();
            mesh.GetUVs(0, uvs);
            Color32[] colors = mesh.colors32;
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Assert.AreEqual(data.Positions[3 * i], positions[i].x, 1f / 64f);
                Assert.AreEqual(data.Positions[3 * i + 1], positions[i].y, 1f / 64f);
                Assert.AreEqual(data.Positions[3 * i + 2], positions[i].z, 1f / 64f);
                Assert.AreEqual(data.Normals[3 * i], normals[i].x, 0.02f);
                Assert.AreEqual(3f, uvs[i].z, 1e-3f);
                Assert.AreEqual(new Color32(0xC0, 0x40, 0x80, 0xFF), colors[i]);
            }
            Object.DestroyImmediate(mesh);
        }

        static ChunkMeshData StoneAndGlassMesh()
        {
            var registry = new BlockRegistry();
            registry.Register(new BlockDefinition(1, "stone", true, BlockSkin.Default));
            registry.Register(new BlockDefinition(2, "glass", false, BlockSkin.Default));
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, 1);
            chunk.SetBlock(6, 5, 5, 2);
            var data = new ChunkMeshData();
            ChunkMeshBuilder.Build(new ChunkNeighborhood(chunk), registry, new FaceShapeLibrary(), 0, data);
            return data;
        }

        [Test]
        public void Apply_TransparentBlocks_GoToSecondSubMeshAfterOpaque()
        {
            ChunkMeshData data = StoneAndGlassMesh();
            var mesh = new Mesh();

            ChunkMeshConverter.Apply(data, mesh);

            Assert.AreEqual(2, mesh.subMeshCount);
            Assert.AreEqual(0u, mesh.GetIndexStart(ChunkMeshConverter.OpaqueSubMesh));
            Assert.AreEqual(36u, mesh.GetIndexCount(ChunkMeshConverter.OpaqueSubMesh));
            Assert.AreEqual(36u, mesh.GetIndexStart(ChunkMeshConverter.TransparentSubMesh));
            Assert.AreEqual(30u, mesh.GetIndexCount(ChunkMeshConverter.TransparentSubMesh));
            CollectionAssert.AreEqual(data.Indices, mesh.GetIndices(ChunkMeshConverter.OpaqueSubMesh));
            CollectionAssert.AreEqual(data.TransparentIndices, mesh.GetIndices(ChunkMeshConverter.TransparentSubMesh));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Apply_OnlyOpaqueBlocks_KeepsEmptyTransparentSubMesh()
        {
            var mesh = new Mesh();

            ChunkMeshConverter.Apply(SingleBlockMesh(), mesh);

            Assert.AreEqual(2, mesh.subMeshCount);
            Assert.AreEqual(0u, mesh.GetIndexCount(ChunkMeshConverter.TransparentSubMesh));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Apply_NullArguments_Throw()
        {
            var mesh = new Mesh();

            Assert.Throws<System.ArgumentNullException>(() => ChunkMeshConverter.Apply(null, mesh));
            Assert.Throws<System.ArgumentNullException>(() => ChunkMeshConverter.Apply(new ChunkMeshData(), null));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Apply_EmptyData_LeavesEmptyMesh()
        {
            var mesh = new Mesh();

            ChunkMeshConverter.Apply(new ChunkMeshData(), mesh);

            Assert.AreEqual(0, mesh.vertexCount);
            Object.DestroyImmediate(mesh);
        }

        [TestCase(65536, true)]
        [TestCase(65537, false)]
        public void Fits16BitIndices_SwitchesAtTheIndexLimit(int vertexCount, bool expected)
        {
            Assert.AreEqual(expected, ChunkMeshConverter.Fits16BitIndices(vertexCount));
        }

        [Test]
        public void Apply_MoreThan65536Vertices_Uses32BitIndices()
        {
            var data = new ChunkMeshData();
            for (int i = 0; i < 17000; i++)
                ChunkMeshBuilder.AppendFace(data, FaceShape.Flat, 0, Direction.PosY, i & 15, 0, 0, BlockSkin.Default);
            Assert.Greater(data.VertexCount, 65536);
            var mesh = new Mesh();

            ChunkMeshConverter.Apply(data, mesh);

            Assert.AreEqual(IndexFormat.UInt32, mesh.indexFormat);
            Assert.AreEqual((uint)data.Indices.Count, mesh.GetIndexCount(0));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Apply_MoreThan65536VerticesWithTransparentFaces_Splits32BitSubMeshes()
        {
            var data = new ChunkMeshData();
            for (int i = 0; i < 17000; i++)
                ChunkMeshBuilder.AppendFace(data, FaceShape.Flat, 0, Direction.PosY, i & 15, 0, 0, BlockSkin.Default, transparent: i >= 16990);
            var mesh = new Mesh();

            ChunkMeshConverter.Apply(data, mesh);

            Assert.AreEqual(IndexFormat.UInt32, mesh.indexFormat);
            Assert.AreEqual(60u, mesh.GetIndexCount(ChunkMeshConverter.TransparentSubMesh));
            CollectionAssert.AreEqual(data.TransparentIndices, mesh.GetIndices(ChunkMeshConverter.TransparentSubMesh));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void MeshStats_AddAndRemove_TrackTotalsAndIgnoreEmptyMeshes()
        {
            ChunkMeshData data = SingleBlockMesh();
            var stats = new MeshStats();

            stats.Add(data);
            stats.Add(data);
            stats.Add(new ChunkMeshData());
            Assert.AreEqual(2, stats.Meshes);
            Assert.AreEqual(48, stats.Vertices);
            Assert.AreEqual(24, stats.Triangles);

            stats.Remove(data);
            Assert.AreEqual(1, stats.Meshes);
            Assert.AreEqual(24, stats.Vertices);
        }
    }
}
