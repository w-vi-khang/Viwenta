using System;

namespace Viwenta.Voxel
{
    /// <summary>Integer position of a chunk column on the horizontal plane (column units, not blocks).</summary>
    public readonly struct ColumnCoord : IEquatable<ColumnCoord>
    {
        public readonly int X;
        public readonly int Z;

        public ColumnCoord(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static ColumnCoord operator +(ColumnCoord a, ColumnCoord b) => new ColumnCoord(a.X + b.X, a.Z + b.Z);
        public static ColumnCoord operator -(ColumnCoord a, ColumnCoord b) => new ColumnCoord(a.X - b.X, a.Z - b.Z);
        public static bool operator ==(ColumnCoord a, ColumnCoord b) => a.X == b.X && a.Z == b.Z;
        public static bool operator !=(ColumnCoord a, ColumnCoord b) => !(a == b);

        public bool Equals(ColumnCoord other) => this == other;
        public override bool Equals(object obj) => obj is ColumnCoord other && this == other;
        public override int GetHashCode() => unchecked((X * 397) ^ Z);
        public override string ToString() => $"({X}, {Z})";
    }
}
