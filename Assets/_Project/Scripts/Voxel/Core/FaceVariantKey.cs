namespace Viwenta.Voxel
{
    /// <summary>
    /// The lookup key that picks a prebuilt face shape. It is derived from the neighbouring voxels, never chosen freely,
    /// so a face never disagrees with the faces next to it.
    /// <para>
    /// Bits 0-3 are the four edges of the face in the order +U, +W, -U, -W; an edge is bevelled when the voxel beside
    /// the edge and the voxel in front of that voxel are both empty (a convex edge).
    /// Bits 4-7 are the corners between edge i and edge i+1; a corner is rounded when both its edges are bevelled and the
    /// diagonal voxel and the one in front of it are empty too.
    /// </para>
    /// The eight symmetries of the square (4 rotations, optionally mirrored) map keys onto each other, so only one canonical
    /// key per orbit needs a prebuilt shape. Symmetry index = mirror*4 + rotation, applied mirror first.
    /// </summary>
    public static class FaceVariantKey
    {
        public const int KeyCount = 256;
        public const int SymmetryCount = 8;

        static readonly byte[] CanonicalOf = new byte[KeyCount];
        static readonly byte[] SymmetryOf = new byte[KeyCount];

        static FaceVariantKey()
        {
            for (int raw = 0; raw < KeyCount; raw++)
            {
                int canonical = raw;
                for (int s = 1; s < SymmetryCount; s++)
                {
                    int mapped = Transform((byte)raw, s);
                    if (mapped < canonical)
                        canonical = mapped;
                }

                int symmetry = -1;
                for (int s = 0; s < SymmetryCount; s++)
                {
                    if (Transform((byte)canonical, s) == raw)
                    {
                        symmetry = s;
                        break;
                    }
                }

                CanonicalOf[raw] = (byte)canonical;
                SymmetryOf[raw] = (byte)symmetry;
            }
        }

        /// <summary>The smallest key in the orbit of <paramref name="key"/>.</summary>
        public static byte Canonical(byte key) => CanonicalOf[key];

        /// <summary>The symmetry that turns the shape of <c>Canonical(key)</c> into the shape for <paramref name="key"/>.</summary>
        public static int SymmetryFor(byte key) => SymmetryOf[key];

        /// <summary>The key obtained by applying <paramref name="symmetry"/> (0..7) to <paramref name="key"/>.</summary>
        public static byte Transform(byte key, int symmetry)
        {
            bool mirror = (symmetry & 4) != 0;
            int rotation = symmetry & 3;
            int result = 0;
            for (int i = 0; i < 4; i++)
            {
                if ((key & (1 << i)) != 0)
                {
                    int j = mirror ? (6 - i) & 3 : i; // edge i -> 2 - i
                    result |= 1 << ((j + rotation) & 3);
                }
                if ((key & (1 << (4 + i))) != 0)
                {
                    int j = mirror ? (5 - i) & 3 : i; // corner i -> 1 - i
                    result |= 1 << (4 + ((j + rotation) & 3));
                }
            }
            return (byte)result;
        }

        /// <summary>Computes the key of the face of voxel (x, y, z) that looks along <paramref name="direction"/>.</summary>
        public static byte Compute(ChunkNeighborhood blocks, int x, int y, int z, Direction direction)
        {
            FaceFrame f = FaceFrame.Get(direction);
            int key = 0;
            for (int i = 0; i < 4; i++)
            {
                Edge(i, out int du, out int dw);
                int ox = du * f.UX + dw * f.WX;
                int oy = du * f.UY + dw * f.WY;
                int oz = du * f.UZ + dw * f.WZ;
                if (IsFree(blocks, x + ox, y + oy, z + oz, f))
                    key |= 1 << i;
            }

            int edges = key;
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) & 3;
                if ((edges & (1 << i)) == 0 || (edges & (1 << next)) == 0)
                    continue;
                Edge(i, out int du0, out int dw0);
                Edge(next, out int du1, out int dw1);
                int du = du0 + du1;
                int dw = dw0 + dw1;
                if (IsFree(blocks, x + du * f.UX + dw * f.WX, y + du * f.UY + dw * f.WY, z + du * f.UZ + dw * f.WZ, f))
                    key |= 1 << (4 + i);
            }
            return (byte)key;
        }

        static void Edge(int index, out int du, out int dw)
        {
            switch (index)
            {
                case 0: du = 1; dw = 0; break;
                case 1: du = 0; dw = 1; break;
                case 2: du = -1; dw = 0; break;
                default: du = 0; dw = -1; break;
            }
        }

        // The voxel at (x, y, z) and the one in front of it (along the face normal) are both empty.
        static bool IsFree(ChunkNeighborhood blocks, int x, int y, int z, FaceFrame f)
        {
            return blocks.GetBlock(x, y, z) == BlockRegistry.AirId
                && blocks.GetBlock(x + f.NX, y + f.NY, z + f.NZ) == BlockRegistry.AirId;
        }
    }
}
