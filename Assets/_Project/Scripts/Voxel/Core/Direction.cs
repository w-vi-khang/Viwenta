namespace Viwenta.Voxel
{
    public enum Direction : byte
    {
        PosX,
        NegX,
        PosY,
        NegY,
        PosZ,
        NegZ,
    }

    /// <summary>
    /// Local frame of a voxel face. A face-local point (u, w, h) maps to
    /// voxel-local position Origin + u*U + w*W + h*N, with u and w in 0..1 across the face and
    /// h along the outward normal (h &lt;= 0 means inside the voxel).
    /// Frames are chosen so that cross(W, U) == N: a triangle (a, b, c) faces outward in Unity when
    /// cross(b - a, c - a) points along N, so the canonical flat quad (0,0),(0,1),(1,1),(1,0) is front-facing.
    /// </summary>
    public readonly struct FaceFrame
    {
        public readonly int OriginX, OriginY, OriginZ;
        public readonly int UX, UY, UZ;
        public readonly int WX, WY, WZ;
        public readonly int NX, NY, NZ;

        FaceFrame(int ox, int oy, int oz, int ux, int uy, int uz, int wx, int wy, int wz, int nx, int ny, int nz)
        {
            OriginX = ox; OriginY = oy; OriginZ = oz;
            UX = ux; UY = uy; UZ = uz;
            WX = wx; WY = wy; WZ = wz;
            NX = nx; NY = ny; NZ = nz;
        }

        static readonly FaceFrame[] Frames =
        {
            new FaceFrame(1, 0, 0,  0, 0, 1,  0, 1, 0,   1, 0, 0), // PosX
            new FaceFrame(0, 0, 0,  0, 1, 0,  0, 0, 1,  -1, 0, 0), // NegX
            new FaceFrame(0, 1, 0,  1, 0, 0,  0, 0, 1,   0, 1, 0), // PosY
            new FaceFrame(0, 0, 0,  0, 0, 1,  1, 0, 0,   0, -1, 0), // NegY
            new FaceFrame(0, 0, 1,  0, 1, 0,  1, 0, 0,   0, 0, 1), // PosZ
            new FaceFrame(0, 0, 0,  1, 0, 0,  0, 1, 0,   0, 0, -1), // NegZ
        };

        public static FaceFrame Get(Direction direction) => Frames[(int)direction];
    }
}
