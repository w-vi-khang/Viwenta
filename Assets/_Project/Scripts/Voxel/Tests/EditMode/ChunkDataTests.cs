using System;
using NUnit.Framework;

namespace Viwenta.Voxel.Tests
{
    public class ChunkDataTests
    {
        [Test]
        public void New_IsEmptyAndUniform()
        {
            var chunk = new ChunkData();

            Assert.IsTrue(chunk.IsEmpty);
            Assert.IsTrue(chunk.IsUniform);
            Assert.AreEqual(0, chunk.GetBlock(3, 4, 5));
            Assert.IsFalse(chunk.Dirty);
            Assert.IsFalse(chunk.Modified);
        }

        [Test]
        public void SetBlock_StoresValueAndMarksDirty()
        {
            var chunk = new ChunkData();

            chunk.SetBlock(1, 2, 3, 7);

            Assert.AreEqual(7, chunk.GetBlock(1, 2, 3));
            Assert.AreEqual(0, chunk.GetBlock(3, 2, 1));
            Assert.IsFalse(chunk.IsUniform);
            Assert.IsTrue(chunk.Dirty);
            Assert.IsTrue(chunk.Modified);
        }

        [Test]
        public void SetBlock_SameValueOnUniformChunk_KeepsItUniformAndClean()
        {
            var chunk = new ChunkData(5);

            chunk.SetBlock(0, 0, 0, 5);

            Assert.IsTrue(chunk.IsUniform);
            Assert.IsFalse(chunk.Dirty);
        }

        [Test]
        public void SetBlock_OnNonAirUniformChunk_KeepsOtherVoxels()
        {
            var chunk = new ChunkData(5);

            chunk.SetBlock(1, 1, 1, 9);

            Assert.AreEqual(9, chunk.GetBlock(1, 1, 1));
            Assert.AreEqual(5, chunk.GetBlock(2, 2, 2));
            Assert.AreEqual(5, chunk.GetBlock(15, 15, 15));
        }

        [Test]
        public void TryCompact_AfterRestoringAllVoxels_BecomesUniform()
        {
            var chunk = new ChunkData();
            chunk.SetBlock(4, 4, 4, 3);
            Assert.IsFalse(chunk.TryCompact());

            chunk.SetBlock(4, 4, 4, 0);

            Assert.IsTrue(chunk.TryCompact());
            Assert.IsTrue(chunk.IsEmpty);
        }

        [TestCase(-1, 0, 0)]
        [TestCase(0, 16, 0)]
        [TestCase(0, 0, 16)]
        public void GetBlock_OutsideChunk_Throws(int x, int y, int z)
        {
            var chunk = new ChunkData();

            Assert.Throws<ArgumentOutOfRangeException>(() => chunk.GetBlock(x, y, z));
        }

        [Test]
        public void Index_UsesDocumentedLayout()
        {
            Assert.AreEqual(1 | (2 << 4) | (3 << 8), ChunkData.Index(1, 3, 2));
        }

        [Test]
        public void Neighborhood_ReadsAcrossChunkEdgesAndTreatsMissingAsAir()
        {
            var center = new ChunkData();
            var east = new ChunkData();
            east.SetBlock(0, 5, 5, 4);
            var neighborhood = new ChunkNeighborhood(center);
            neighborhood.SetNeighbor(1, 0, 0, east);

            Assert.AreEqual(4, neighborhood.GetBlock(16, 5, 5));
            Assert.AreEqual(0, neighborhood.GetBlock(-1, 5, 5));
            Assert.AreEqual(0, neighborhood.GetBlock(5, 16, 5));
        }

        [Test]
        public void Neighborhood_MoreThanOneChunkAway_Throws()
        {
            var neighborhood = new ChunkNeighborhood(new ChunkData());

            Assert.Throws<ArgumentOutOfRangeException>(() => neighborhood.GetBlock(32, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => neighborhood.GetBlock(0, -17, 0));
        }
    }
}
