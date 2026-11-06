using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Viwenta.Voxel.Tests
{
    public class ColumnShapeServiceTests
    {
        // Counts reproduced from the table in Docs/Design/ChunkStreaming.md section 2.
        [TestCase(5, 77, 45, 32, 32)]
        [TestCase(6, 109, 69, 40, 36)]
        [TestCase(7, 145, 109, 36, 40)]
        public void Get_ReturnsDocumentedCounts(int radius, int shape, int core, int inside, int outside)
        {
            ColumnShape s = ColumnShapeService.Get(radius);

            Assert.AreEqual(shape, s.Shape.Length, "Shape");
            Assert.AreEqual(core, s.Core.Length, "Core");
            Assert.AreEqual(inside, s.InsideBorder.Length, "InsideBorder");
            Assert.AreEqual(outside, s.OutsideBorder.Length, "OutsideBorder");
            Assert.AreEqual(shape + outside, s.DataRegion.Length, "DataRegion");
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void EveryCoreColumnHasAllFourNeighboursInShape(int radius)
        {
            ColumnShape s = ColumnShapeService.Get(radius);

            foreach (ColumnCoord c in s.Core.ToArray())
            {
                foreach (ColumnCoord n in PlusNeighbours(c))
                {
                    Assert.AreNotEqual(ColumnZone.Beyond, s.GetZone(n), $"core {c} neighbour {n} is outside the data region");
                    Assert.AreNotEqual(ColumnZone.OutsideBorder, s.GetZone(n), $"core {c} neighbour {n} is outside the shape");
                }
            }
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void ZonesAreDisjointAndMatchTheirLists(int radius)
        {
            ColumnShape s = ColumnShapeService.Get(radius);
            var seen = new HashSet<ColumnCoord>();

            void Check(ColumnCoord[] list, ColumnZone zone)
            {
                foreach (ColumnCoord c in list)
                {
                    Assert.IsTrue(seen.Add(c), $"{c} is listed twice");
                    Assert.AreEqual(zone, s.GetZone(c), $"{c}");
                }
            }

            Check(s.Core.ToArray(), ColumnZone.Core);
            Check(s.InsideBorder.ToArray(), ColumnZone.InsideBorder);
            Check(s.OutsideBorder.ToArray(), ColumnZone.OutsideBorder);
            Assert.AreEqual(s.DataRegion.Length, seen.Count);
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void AxisTipsAreNotInShape(int radius)
        {
            ColumnShape s = ColumnShapeService.Get(radius);

            foreach (ColumnCoord tip in new[]
            {
                new ColumnCoord(radius, 0), new ColumnCoord(-radius, 0),
                new ColumnCoord(0, radius), new ColumnCoord(0, -radius),
            })
            {
                Assert.IsFalse(ColumnShapeService.IsInShape(tip.X, tip.Z, radius), $"{tip} in IsInShape");
                Assert.AreEqual(ColumnZone.OutsideBorder, s.GetZone(tip), $"{tip} zone");
            }
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void OutsideBorderIsPlusAdjacentToInsideBorderAndNotDiagonalOnly(int radius)
        {
            ColumnShape s = ColumnShapeService.Get(radius);

            foreach (ColumnCoord c in s.OutsideBorder.ToArray())
            {
                bool touchesInside = false;
                foreach (ColumnCoord n in PlusNeighbours(c))
                {
                    touchesInside |= s.GetZone(n) == ColumnZone.InsideBorder;
                }

                Assert.IsTrue(touchesInside, $"{c} is not plus-adjacent to InsideBorder");
            }
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void ShapeIsSymmetric(int radius)
        {
            ColumnShape s = ColumnShapeService.Get(radius);

            foreach (ColumnCoord c in s.DataRegion.ToArray())
            {
                ColumnZone zone = s.GetZone(c);
                Assert.AreEqual(zone, s.GetZone(-c.X, c.Z));
                Assert.AreEqual(zone, s.GetZone(c.X, -c.Z));
                Assert.AreEqual(zone, s.GetZone(c.Z, c.X));
            }
        }

        [Test]
        public void GetZone_FarOutsideIsBeyond()
        {
            ColumnShape s = ColumnShapeService.Get(5);

            Assert.AreEqual(ColumnZone.Beyond, s.GetZone(100, 0));
            Assert.AreEqual(ColumnZone.Beyond, s.GetZone(0, -100));
            Assert.AreEqual(ColumnZone.Core, s.GetZone(0, 0));
        }

        [Test]
        public void Get_ReturnsCachedInstance()
        {
            Assert.AreSame(ColumnShapeService.Get(6), ColumnShapeService.Get(6));
        }

        [Test]
        public void Get_BuildsEveryAllowedRadius()
        {
            for (int r = VoxelConstants.MinShapeRadius; r <= VoxelConstants.MaxShapeRadius; r++)
            {
                Assert.Greater(ColumnShapeService.Get(r).DataRegion.Length, 0, $"radius {r}");
            }
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(65)]
        public void Get_RejectsRadiusOutsideAllowedRange(int radius)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ColumnShapeService.Get(radius));
            StringAssert.Contains("Radius must be between", ex.Message);
        }

        [Test]
        public void ComputeDiff_SameCenterAndRadius_IsEmpty()
        {
            ColumnShape s = ColumnShapeService.Get(5);
            var added = new List<ColumnCoord> { new ColumnCoord(9, 9) };
            var removed = new List<ColumnCoord> { new ColumnCoord(9, 9) };

            ColumnShapeService.ComputeDiff(s, new ColumnCoord(3, 4), s, new ColumnCoord(3, 4), added, removed);

            Assert.IsEmpty(added);
            Assert.IsEmpty(removed);
        }

        [Test]
        public void ComputeDiff_MovingOneColumn_MatchesSetDifference()
        {
            AssertDiffMatchesSets(5, new ColumnCoord(0, 0), 5, new ColumnCoord(1, 0));
            AssertDiffMatchesSets(7, new ColumnCoord(-3, 2), 7, new ColumnCoord(-3, 1));
        }

        [Test]
        public void ComputeDiff_RadiusChange_MatchesSetDifference()
        {
            AssertDiffMatchesSets(5, new ColumnCoord(2, 2), 7, new ColumnCoord(2, 2));
            AssertDiffMatchesSets(7, new ColumnCoord(2, 2), 5, new ColumnCoord(4, 1));
        }

        [Test]
        public void ComputeDiff_RejectsNullArguments()
        {
            ColumnShape s = ColumnShapeService.Get(5);
            var list = new List<ColumnCoord>();

            Assert.Throws<ArgumentNullException>(() => ColumnShapeService.ComputeDiff(null, default, s, default, list, list));
            Assert.Throws<ArgumentNullException>(() => ColumnShapeService.ComputeDiff(s, default, null, default, list, list));
            Assert.Throws<ArgumentNullException>(() => ColumnShapeService.ComputeDiff(s, default, s, default, null, list));
            Assert.Throws<ArgumentNullException>(() => ColumnShapeService.ComputeDiff(s, default, s, default, list, null));
        }

        [Test]
        public void VoxelConstants_AreConsistent()
        {
            Assert.AreEqual(16, VoxelConstants.ChunkSize);
            Assert.AreEqual(24, VoxelConstants.ColumnChunkCount);
            Assert.AreEqual(1 << VoxelConstants.ChunkSizeShift, VoxelConstants.ChunkSize);
            Assert.AreEqual(VoxelConstants.ChunkSize - 1, VoxelConstants.ChunkSizeMask);
            Assert.AreEqual(384, VoxelConstants.ColumnHeightBlocks);
            Assert.AreEqual(4096, VoxelConstants.VoxelsPerChunk);
            Assert.AreEqual(98304, VoxelConstants.VoxelsPerColumn);
        }

        private static void AssertDiffMatchesSets(int fromRadius, ColumnCoord fromCenter, int toRadius, ColumnCoord toCenter)
        {
            ColumnShape from = ColumnShapeService.Get(fromRadius);
            ColumnShape to = ColumnShapeService.Get(toRadius);
            HashSet<ColumnCoord> oldSet = Absolute(from, fromCenter);
            HashSet<ColumnCoord> newSet = Absolute(to, toCenter);
            var added = new List<ColumnCoord>();
            var removed = new List<ColumnCoord>();

            ColumnShapeService.ComputeDiff(from, fromCenter, to, toCenter, added, removed);

            var expectedAdded = new HashSet<ColumnCoord>(newSet);
            expectedAdded.ExceptWith(oldSet);
            var expectedRemoved = new HashSet<ColumnCoord>(oldSet);
            expectedRemoved.ExceptWith(newSet);

            Assert.AreEqual(expectedAdded.Count, added.Count, "added count");
            Assert.IsTrue(expectedAdded.SetEquals(added), "added set");
            Assert.AreEqual(expectedRemoved.Count, removed.Count, "removed count");
            Assert.IsTrue(expectedRemoved.SetEquals(removed), "removed set");
        }

        private static HashSet<ColumnCoord> Absolute(ColumnShape shape, ColumnCoord center)
        {
            var set = new HashSet<ColumnCoord>();
            foreach (ColumnCoord offset in shape.DataRegion.ToArray())
            {
                set.Add(center + offset);
            }

            return set;
        }

        private static IEnumerable<ColumnCoord> PlusNeighbours(ColumnCoord c)
        {
            yield return new ColumnCoord(c.X + 1, c.Z);
            yield return new ColumnCoord(c.X - 1, c.Z);
            yield return new ColumnCoord(c.X, c.Z + 1);
            yield return new ColumnCoord(c.X, c.Z - 1);
        }
    }
}
