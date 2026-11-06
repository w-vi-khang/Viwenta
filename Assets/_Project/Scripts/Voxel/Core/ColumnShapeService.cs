using System;
using System.Collections.Generic;
using System.Threading;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Computes the disc of columns around a point. Pure and thread-safe: results depend only on the
    /// radius and are cached per radius as offsets from the center.
    /// </summary>
    public static class ColumnShapeService
    {
        private static readonly Lazy<ColumnShape>[] Cache = CreateCache();

        /// <summary>Shape for <paramref name="radius"/>. Offsets are relative to the center.</summary>
        public static ColumnShape Get(int radius)
        {
            if (radius < VoxelConstants.MinShapeRadius || radius > VoxelConstants.MaxShapeRadius)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius),
                    radius,
                    $"Radius must be between {VoxelConstants.MinShapeRadius} and {VoxelConstants.MaxShapeRadius}.");
            }

            return Cache[radius].Value;
        }

        /// <summary>
        /// Single definition of the shape: x^2 + z^2 &lt;= R^2 and |x^2 - z^2| != R^2.
        /// The second term removes the four single-column tips on the axes.
        /// </summary>
        public static bool IsInShape(int x, int z, int radius)
        {
            int xx = x * x;
            int zz = z * z;
            int rr = radius * radius;
            return xx + zz <= rr && Math.Abs(xx - zz) != rr;
        }

        /// <summary>
        /// Columns that need data when the point of view moves from (<paramref name="fromShape"/>,
        /// <paramref name="fromCenter"/>) to (<paramref name="toShape"/>, <paramref name="toCenter"/>).
        /// <paramref name="added"/> gets columns in the new data region but not the old one;
        /// <paramref name="removed"/> gets the reverse. Both lists are cleared first.
        /// A column whose zone merely changes (for example Border to Core) is in neither list.
        /// </summary>
        public static void ComputeDiff(
            ColumnShape fromShape,
            ColumnCoord fromCenter,
            ColumnShape toShape,
            ColumnCoord toCenter,
            List<ColumnCoord> added,
            List<ColumnCoord> removed)
        {
            if (fromShape == null) throw new ArgumentNullException(nameof(fromShape));
            if (toShape == null) throw new ArgumentNullException(nameof(toShape));
            if (added == null) throw new ArgumentNullException(nameof(added));
            if (removed == null) throw new ArgumentNullException(nameof(removed));

            added.Clear();
            removed.Clear();

            foreach (ColumnCoord offset in toShape.DataRegion)
            {
                ColumnCoord column = toCenter + offset;
                if (fromShape.GetZone(column.X - fromCenter.X, column.Z - fromCenter.Z) == ColumnZone.Beyond)
                {
                    added.Add(column);
                }
            }

            foreach (ColumnCoord offset in fromShape.DataRegion)
            {
                ColumnCoord column = fromCenter + offset;
                if (toShape.GetZone(column.X - toCenter.X, column.Z - toCenter.Z) == ColumnZone.Beyond)
                {
                    removed.Add(column);
                }
            }
        }

        private static Lazy<ColumnShape>[] CreateCache()
        {
            var cache = new Lazy<ColumnShape>[VoxelConstants.MaxShapeRadius + 1];
            for (int r = 0; r < cache.Length; r++)
            {
                int radius = r;
                cache[r] = new Lazy<ColumnShape>(() => Build(radius), LazyThreadSafetyMode.ExecutionAndPublication);
            }

            return cache;
        }

        private static ColumnShape Build(int radius)
        {
            int extent = radius + 1;
            int size = 2 * extent + 1;
            var zones = new ColumnZone[size * size];
            int borderThreshold = (radius - 1) * (radius - 1);

            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (!IsInShape(x, z, radius))
                    {
                        continue;
                    }

                    bool inner = x * x + z * z >= borderThreshold;
                    zones[Index(x, z, extent, size)] = inner ? ColumnZone.InsideBorder : ColumnZone.Core;
                }
            }

            // Adjacency is the plus shape only: diagonals are deliberately excluded.
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (zones[Index(x, z, extent, size)] != ColumnZone.InsideBorder)
                    {
                        continue;
                    }

                    MarkOutside(zones, x + 1, z, extent, size);
                    MarkOutside(zones, x - 1, z, extent, size);
                    MarkOutside(zones, x, z + 1, extent, size);
                    MarkOutside(zones, x, z - 1, extent, size);
                }
            }

            var core = new List<ColumnCoord>();
            var insideBorder = new List<ColumnCoord>();
            var outsideBorder = new List<ColumnCoord>();
            for (int z = -extent; z <= extent; z++)
            {
                for (int x = -extent; x <= extent; x++)
                {
                    switch (zones[Index(x, z, extent, size)])
                    {
                        case ColumnZone.Core: core.Add(new ColumnCoord(x, z)); break;
                        case ColumnZone.InsideBorder: insideBorder.Add(new ColumnCoord(x, z)); break;
                        case ColumnZone.OutsideBorder: outsideBorder.Add(new ColumnCoord(x, z)); break;
                    }
                }
            }

            return new ColumnShape(radius, zones, extent, core.ToArray(), insideBorder.ToArray(), outsideBorder.ToArray());
        }

        private static void MarkOutside(ColumnZone[] zones, int x, int z, int extent, int size)
        {
            int index = Index(x, z, extent, size);
            if (zones[index] == ColumnZone.Beyond)
            {
                zones[index] = ColumnZone.OutsideBorder;
            }
        }

        private static int Index(int x, int z, int extent, int size) => (z + extent) * size + (x + extent);
    }
}
