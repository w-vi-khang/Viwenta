using System;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Role of a column relative to a point of view. A higher value is a stronger zone, so when several
    /// points of view cover one column the zone to use is the maximum of their zones.
    /// </summary>
    public enum ColumnZone : byte
    {
        /// <summary>Not loaded.</summary>
        Beyond = 0,

        /// <summary>Data only, not drawn. Padding around <see cref="InsideBorder"/>.</summary>
        OutsideBorder = 1,

        /// <summary>Data only, not drawn. Supplies neighbour blocks so <see cref="Core"/> can decide visible faces.</summary>
        InsideBorder = 2,

        /// <summary>Data, meshed and drawn.</summary>
        Core = 3,
    }

    /// <summary>
    /// Immutable set of column offsets around a center for one radius. Obtain it from
    /// <see cref="ColumnShapeService.Get"/>; offsets are relative to the center.
    /// </summary>
    public sealed class ColumnShape
    {
        private readonly ColumnZone[] _zones;
        private readonly int _extent;
        private readonly int _gridSize;
        private readonly ColumnCoord[] _core;
        private readonly ColumnCoord[] _insideBorder;
        private readonly ColumnCoord[] _outsideBorder;
        private readonly ColumnCoord[] _shape;
        private readonly ColumnCoord[] _dataRegion;

        internal ColumnShape(
            int radius,
            ColumnZone[] zones,
            int extent,
            ColumnCoord[] core,
            ColumnCoord[] insideBorder,
            ColumnCoord[] outsideBorder)
        {
            Radius = radius;
            _zones = zones;
            _extent = extent;
            _gridSize = 2 * extent + 1;
            _core = core;
            _insideBorder = insideBorder;
            _outsideBorder = outsideBorder;

            _shape = Concat(core, insideBorder);
            _dataRegion = Concat(_shape, outsideBorder);
        }

        public int Radius { get; }

        /// <summary>Drawn columns.</summary>
        public ReadOnlySpan<ColumnCoord> Core => _core;

        /// <summary>Shape columns that are not drawn but feed <see cref="Core"/>.</summary>
        public ReadOnlySpan<ColumnCoord> InsideBorder => _insideBorder;

        /// <summary>Columns outside the shape that are 4-adjacent to <see cref="InsideBorder"/>.</summary>
        public ReadOnlySpan<ColumnCoord> OutsideBorder => _outsideBorder;

        /// <summary><see cref="Core"/> plus <see cref="InsideBorder"/>.</summary>
        public ReadOnlySpan<ColumnCoord> Shape => _shape;

        /// <summary>Every column that needs voxel data: <see cref="Shape"/> plus <see cref="OutsideBorder"/>.</summary>
        public ReadOnlySpan<ColumnCoord> DataRegion => _dataRegion;

        /// <summary>Zone of the column at offset (dx, dz) from the center. O(1).</summary>
        public ColumnZone GetZone(int dx, int dz)
        {
            if (dx < -_extent || dx > _extent || dz < -_extent || dz > _extent)
            {
                return ColumnZone.Beyond;
            }

            return _zones[(dz + _extent) * _gridSize + (dx + _extent)];
        }

        public ColumnZone GetZone(ColumnCoord offset) => GetZone(offset.X, offset.Z);

        private static ColumnCoord[] Concat(ColumnCoord[] a, ColumnCoord[] b)
        {
            var result = new ColumnCoord[a.Length + b.Length];
            Array.Copy(a, 0, result, 0, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }
    }
}
