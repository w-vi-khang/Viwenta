using System;
using NUnit.Framework;

namespace Viwenta.Voxel.Tests
{
    public class ChunkMeshBuilderTests
    {
        const ushort Stone = 1;
        const ushort Glass = 2;

        BlockRegistry _registry;
        FaceShapeLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _registry = new BlockRegistry();
            _registry.Register(new BlockDefinition(Stone, "stone", true, new BlockSkin(7, 0x11223344u)));
            _registry.Register(new BlockDefinition(Glass, "glass", false, BlockSkin.Default));
            _library = new FaceShapeLibrary();
        }

        ChunkMeshData Build(ChunkNeighborhood n, int tier = 0)
        {
            var mesh = new ChunkMeshData();
            ChunkMeshBuilder.Build(n, _registry, _library, tier, mesh);
            return mesh;
        }

        static ChunkNeighborhood Single(int x, int y, int z, ushort id = Stone)
        {
            var chunk = new ChunkData();
            chunk.SetBlock(x, y, z, id);
            return new ChunkNeighborhood(chunk);
        }

        [Test]
        public void Build_EmptyChunk_ProducesNothing()
        {
            ChunkMeshData mesh = Build(new ChunkNeighborhood(new ChunkData()));

            Assert.AreEqual(0, mesh.VertexCount);
            Assert.AreEqual(0, mesh.TriangleCount);
        }

        [Test]
        public void Build_SingleBlock_ProducesSixFlatFaces()
        {
            ChunkMeshData mesh = Build(Single(5, 5, 5));

            Assert.AreEqual(24, mesh.VertexCount);
            Assert.AreEqual(12, mesh.TriangleCount);
        }

        [Test]
        public void Build_TwoAdjacentOpaqueBlocks_HideTheSharedFaces()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, Stone);
            chunk.SetBlock(6, 5, 5, Stone);

            ChunkMeshData mesh = Build(new ChunkNeighborhood(chunk));

            Assert.AreEqual(10 * 4, mesh.VertexCount);
        }

        [Test]
        public void Build_FullUniformChunkWithoutNeighbours_ProducesOnlyTheOuterSurface()
        {
            ChunkMeshData mesh = Build(new ChunkNeighborhood(new ChunkData(Stone)));

            Assert.AreEqual(6 * 16 * 16 * 4, mesh.VertexCount);
        }

        [Test]
        public void Build_FullChunkSurroundedByFullChunks_ProducesNothing()
        {
            var n = new ChunkNeighborhood(new ChunkData(Stone));
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                        if (dx != 0 || dy != 0 || dz != 0)
                            n.SetNeighbor(dx, dy, dz, new ChunkData(Stone));

            Assert.AreEqual(0, Build(n).VertexCount);
        }

        [Test]
        public void Build_BlockOnChunkEdge_UsesNeighbourChunkToHideFace()
        {
            var center = new ChunkData();
            center.SetBlock(15, 5, 5, Stone);
            var east = new ChunkData();
            east.SetBlock(0, 5, 5, Stone);
            var n = new ChunkNeighborhood(center);
            n.SetNeighbor(1, 0, 0, east);

            Assert.AreEqual(5 * 4, Build(n).VertexCount);
        }

        [Test]
        public void Build_TransparentNeighbour_DoesNotHideFace()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, Stone);
            chunk.SetBlock(6, 5, 5, Glass);

            ChunkMeshData mesh = Build(new ChunkNeighborhood(chunk));

            // Stone shows all 6 faces; glass shows 5 (its face against the opaque stone is hidden).
            Assert.AreEqual(11 * 4, mesh.VertexCount);
        }

        [Test]
        public void Build_TransparentBlock_GoesToTransparentIndices()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, Stone);
            chunk.SetBlock(6, 5, 5, Glass);

            ChunkMeshData mesh = Build(new ChunkNeighborhood(chunk));

            Assert.AreEqual(6 * 6, mesh.Indices.Count);
            Assert.AreEqual(5 * 6, mesh.TransparentIndices.Count);
            Assert.AreEqual(11 * 2, mesh.TriangleCount);
            foreach (int index in mesh.TransparentIndices)
                Assert.GreaterOrEqual(index, 6 * 4, "transparent triangles must use glass vertices only");
        }

        [Test]
        public void Build_OnlyOpaqueBlocks_LeavesTransparentIndicesEmpty()
        {
            ChunkMeshData mesh = Build(Single(5, 5, 5));

            Assert.AreEqual(36, mesh.Indices.Count);
            Assert.AreEqual(0, mesh.TransparentIndices.Count);
        }

        [Test]
        public void AppendFace_Transparent_KeepsWindingOutward()
        {
            var mesh = new ChunkMeshData();
            ChunkMeshBuilder.AppendFace(mesh, FaceShape.Flat, 4, Direction.PosX, 0, 0, 0, BlockSkin.Default, transparent: true);

            Assert.AreEqual(0, mesh.Indices.Count);
            Assert.AreEqual(6, mesh.TransparentIndices.Count);
            mesh.Indices.AddRange(mesh.TransparentIndices);
            AssertWindingMatchesNormals(mesh, "transparent");
        }

        [Test]
        public void Clear_EmptiesBothIndexLists()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, Stone);
            chunk.SetBlock(6, 5, 5, Glass);
            ChunkMeshData mesh = Build(new ChunkNeighborhood(chunk));

            mesh.Clear();

            Assert.AreEqual(0, mesh.VertexCount);
            Assert.AreEqual(0, mesh.TriangleCount);
        }

        [Test]
        public void Build_UnregisteredBlock_ThrowsWithTheId()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(1, 1, 1, 99);

            var ex = Assert.Throws<InvalidOperationException>(() => Build(new ChunkNeighborhood(chunk)));
            StringAssert.Contains("99", ex.Message);
        }

        [Test]
        public void Build_AppliesSkinLayerAndTint_AndMapsFaceToWholeLayer()
        {
            ChunkMeshData mesh = Build(Single(5, 5, 5));

            Assert.AreEqual(mesh.VertexCount * 3, mesh.Uvs.Count);
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                Assert.That(mesh.Uvs[3 * i], Is.InRange(0f, 1f));
                Assert.That(mesh.Uvs[3 * i + 1], Is.InRange(0f, 1f));
                Assert.AreEqual(7f, mesh.Uvs[3 * i + 2]);
                Assert.AreEqual(0x11223344u, mesh.Colors[i]);
            }
        }

        [Test]
        public void Build_FlatFace_CoversTheFullUvSquare()
        {
            var mesh = new ChunkMeshData();

            ChunkMeshBuilder.AppendFace(mesh, FaceShape.Flat, 0, Direction.PosY, 0, 0, 0, BlockSkin.Default);

            var corners = new System.Collections.Generic.HashSet<(float, float)>();
            for (int i = 0; i < mesh.VertexCount; i++)
                corners.Add((mesh.Uvs[3 * i], mesh.Uvs[3 * i + 1]));
            CollectionAssert.AreEquivalent(new[] { (0f, 0f), (0f, 1f), (1f, 1f), (1f, 0f) }, corners);
        }

        [Test]
        public void Build_UsesBlockOverrideShape()
        {
            _registry.Register(new BlockDefinition(3, "odd", true, BlockSkin.Default, FaceVariantTests.TriangleShape()));

            ChunkMeshData mesh = Build(Single(5, 5, 5, 3));

            Assert.AreEqual(6 * 3, mesh.VertexCount);
            Assert.AreEqual(6, mesh.TriangleCount);
        }

        [Test]
        public void Build_UsesLibraryShapeForMatchingKeyAndTier()
        {
            _library.Add(0xFF, 1, FaceVariantTests.TriangleShape());

            Assert.AreEqual(24, Build(Single(5, 5, 5), 0).VertexCount);
            Assert.AreEqual(18, Build(Single(5, 5, 5), 1).VertexCount);
        }

        [Test]
        public void Build_FlatShapes_AllTrianglesFaceOutward()
        {
            ChunkMeshData mesh = Build(Single(5, 5, 5));

            AssertWindingMatchesNormals(mesh);
        }

        [Test]
        public void AppendFace_EveryDirectionAndSymmetry_KeepsWindingOutwardAndStaysOnVoxel()
        {
            FaceShape shape = FaceVariantTests.TriangleShape();

            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                FaceFrame f = FaceFrame.Get(direction);
                for (int symmetry = 0; symmetry < FaceVariantKey.SymmetryCount; symmetry++)
                {
                    var mesh = new ChunkMeshData();
                    ChunkMeshBuilder.AppendFace(mesh, shape, symmetry, direction, 0, 0, 0, BlockSkin.Default);

                    string label = $"{direction} symmetry {symmetry}";
                    AssertWindingMatchesNormals(mesh, label);
                    for (int i = 0; i < mesh.VertexCount; i++)
                    {
                        Assert.That(mesh.Positions[3 * i], Is.InRange(-0.001f, 1.001f), label);
                        Assert.That(mesh.Positions[3 * i + 1], Is.InRange(-0.001f, 1.001f), label);
                        Assert.That(mesh.Positions[3 * i + 2], Is.InRange(-0.001f, 1.001f), label);
                        float nx = mesh.Normals[3 * i], ny = mesh.Normals[3 * i + 1], nz = mesh.Normals[3 * i + 2];
                        Assert.AreEqual(f.NX, nx, 1e-5f, label);
                        Assert.AreEqual(f.NY, ny, 1e-5f, label);
                        Assert.AreEqual(f.NZ, nz, 1e-5f, label);
                    }
                }
            }
        }

        [Test]
        public void AppendFace_Rotation_MovesAsymmetricShapeConsistentlyWithKeyTransform()
        {
            // Shape sits near edge 0 (+U). After rotating by 1 the same vertex must sit near edge 1 (+W).
            var shape = new FaceShape(
                new float[] { 0.9f, 0.4f, 0,  0.9f, 0.6f, 0,  1.0f, 0.5f, 0 },
                new float[] { 0, 0, 1,  0, 0, 1,  0, 0, 1 },
                new[] { 0, 1, 2 });
            var mesh = new ChunkMeshData();

            ChunkMeshBuilder.AppendFace(mesh, shape, 1, Direction.PosY, 0, 0, 0, BlockSkin.Default);

            // PosY: U = +X, W = +Z. Vertex 2 was (u=1, w=0.5); rotated -> (u=0.5, w=1) -> x 0.5, z 1.
            Assert.AreEqual(0.5f, mesh.Positions[6], 1e-5f);
            Assert.AreEqual(1f, mesh.Positions[8], 1e-5f);
        }

        static void AssertWindingMatchesNormals(ChunkMeshData mesh, string label = "")
        {
            for (int t = 0; t < mesh.Indices.Count; t += 3)
            {
                int a = mesh.Indices[t], b = mesh.Indices[t + 1], c = mesh.Indices[t + 2];
                float abx = mesh.Positions[3 * b] - mesh.Positions[3 * a];
                float aby = mesh.Positions[3 * b + 1] - mesh.Positions[3 * a + 1];
                float abz = mesh.Positions[3 * b + 2] - mesh.Positions[3 * a + 2];
                float acx = mesh.Positions[3 * c] - mesh.Positions[3 * a];
                float acy = mesh.Positions[3 * c + 1] - mesh.Positions[3 * a + 1];
                float acz = mesh.Positions[3 * c + 2] - mesh.Positions[3 * a + 2];
                float cx = aby * acz - abz * acy;
                float cy = abz * acx - abx * acz;
                float cz = abx * acy - aby * acx;
                float dot = cx * mesh.Normals[3 * a] + cy * mesh.Normals[3 * a + 1] + cz * mesh.Normals[3 * a + 2];
                Assert.Greater(dot, 0f, $"triangle {t / 3} faces inward {label}");
            }
        }
    }
}
