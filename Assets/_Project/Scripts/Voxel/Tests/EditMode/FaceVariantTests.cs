using System;
using NUnit.Framework;

namespace Viwenta.Voxel.Tests
{
    public class FaceVariantTests
    {
        static ChunkNeighborhood With(params (int x, int y, int z)[] blocks)
        {
            var chunk = new ChunkData();
            foreach (var b in blocks)
                chunk.SetBlock(b.x, b.y, b.z, 1);
            return new ChunkNeighborhood(chunk);
        }

        [Test]
        public void Compute_IsolatedBlock_BevelsEveryEdgeAndCorner()
        {
            ChunkNeighborhood n = With((5, 5, 5));

            foreach (Direction d in Enum.GetValues(typeof(Direction)))
                Assert.AreEqual(0xFF, FaceVariantKey.Compute(n, 5, 5, 5, d), d.ToString());
        }

        [Test]
        public void Compute_TopFaceInsideFlatFloor_HasNoBevel()
        {
            var chunk = new ChunkData();
            for (int x = 4; x <= 6; x++)
                for (int z = 4; z <= 6; z++)
                    chunk.SetBlock(x, 5, z, 1);

            Assert.AreEqual(0, FaceVariantKey.Compute(new ChunkNeighborhood(chunk), 5, 5, 5, Direction.PosY));
        }

        [Test]
        public void Compute_BlockWithNeighbourOnOneSide_LeavesThatEdgeAndItsCornersFlat()
        {
            // Neighbour at +X: on the top face (U = +X) edge 0 is shared, so edge 0 and corners 0 and 3 are not bevelled.
            ChunkNeighborhood n = With((5, 5, 5), (6, 5, 5));

            byte key = FaceVariantKey.Compute(n, 5, 5, 5, Direction.PosY);

            Assert.AreEqual(0b0000_1110 | 0b0110_0000, key); // edges 1, 2, 3; corners 1, 2
        }

        [Test]
        public void Compute_NeighbourAboveTheEdgeNeighbour_DoesNotBevelThatEdge()
        {
            // A voxel beside the block and another on top of it: the edge is a concave step, not convex.
            ChunkNeighborhood n = With((5, 5, 5), (6, 5, 5), (6, 6, 5));

            byte key = FaceVariantKey.Compute(n, 5, 5, 5, Direction.PosY);

            Assert.AreEqual(0, key & 1);
        }

        [Test]
        public void Compute_ReadsAcrossChunkBoundary()
        {
            var center = new ChunkData();
            center.SetBlock(15, 5, 5, 1);
            var east = new ChunkData();
            east.SetBlock(0, 5, 5, 1);
            var n = new ChunkNeighborhood(center);
            n.SetNeighbor(1, 0, 0, east);

            byte key = FaceVariantKey.Compute(n, 15, 5, 5, Direction.PosY);

            Assert.AreEqual(0, key & 1);
        }

        [Test]
        public void Symmetry_RoundTripsEveryKey()
        {
            for (int raw = 0; raw < FaceVariantKey.KeyCount; raw++)
            {
                byte canonical = FaceVariantKey.Canonical((byte)raw);
                int symmetry = FaceVariantKey.SymmetryFor((byte)raw);

                Assert.AreEqual(canonical, FaceVariantKey.Canonical(canonical), $"canonical idempotent for {raw}");
                Assert.AreEqual(raw, FaceVariantKey.Transform(canonical, symmetry), $"round trip for {raw}");
                Assert.LessOrEqual(canonical, raw);
            }
        }

        [Test]
        public void Symmetry_IdentityKeepsKey_AndRotationCyclesEdges()
        {
            Assert.AreEqual(0b0001, FaceVariantKey.Transform(0b0001, 0));
            Assert.AreEqual(0b0010, FaceVariantKey.Transform(0b0001, 1));
            Assert.AreEqual(0b0100, FaceVariantKey.Transform(0b0001, 4)); // mirror: edge 0 -> edge 2
            Assert.AreEqual(0b0010_0000, FaceVariantKey.Transform(0b0001_0000, 1)); // corner 0 -> corner 1
        }

        [Test]
        public void Library_UnknownKey_FallsBackToFlat()
        {
            var library = new FaceShapeLibrary();

            FaceShape shape = library.Resolve(0xFF, 2, out int symmetry);

            Assert.AreSame(FaceShape.Flat, shape);
            Assert.AreEqual(0, symmetry);
        }

        [Test]
        public void Library_HigherTierMissing_UsesLowerTier()
        {
            var library = new FaceShapeLibrary();
            FaceShape tier1 = TriangleShape();
            library.Add(0, 1, tier1);

            Assert.AreSame(tier1, library.Resolve(0, 3, out _));
            Assert.AreSame(FaceShape.Flat, library.Resolve(0, 0, out _));
        }

        [Test]
        public void Library_AddNonCanonicalKey_Throws()
        {
            var library = new FaceShapeLibrary();

            Assert.Throws<ArgumentException>(() => library.Add(0b0010, 0, FaceShape.Flat));
        }

        [Test]
        public void Library_AddDuplicate_Throws()
        {
            var library = new FaceShapeLibrary();
            library.Add(0, 0, FaceShape.Flat);

            Assert.Throws<ArgumentException>(() => library.Add(0, 0, FaceShape.Flat));
        }

        [Test]
        public void FaceShape_IndexOutOfRange_Throws()
        {
            Assert.Throws<ArgumentException>(() => new FaceShape(new float[3], new float[3], new[] { 0, 0, 1 }));
        }

        internal static FaceShape TriangleShape() => new FaceShape(
            new float[] { 0.1f, 0.1f, 0,  0.1f, 0.9f, -0.1f,  0.7f, 0.5f, 0 },
            new float[] { 0, 0, 1,  0, 0, 1,  0, 0, 1 },
            new[] { 0, 1, 2 });
    }
}
