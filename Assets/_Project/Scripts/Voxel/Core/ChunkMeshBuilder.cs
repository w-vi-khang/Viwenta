using System;
using System.Collections.Generic;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Builds the mesh of one chunk by attaching prebuilt face shapes: for every exposed face it looks up a shape
    /// from the neighbours (<see cref="FaceVariantKey"/>) and copies it into the output, transformed to the face's place.
    /// No geometry is computed here.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        static readonly Direction[] Directions =
        {
            Direction.PosX, Direction.NegX, Direction.PosY, Direction.NegY, Direction.PosZ, Direction.NegZ,
        };

        /// <summary>Appends the chunk mesh to <paramref name="output"/>. <paramref name="tier"/> is the detail tier (0 = cheapest).</summary>
        public static void Build(ChunkNeighborhood blocks, BlockRegistry registry, FaceShapeLibrary library, int tier, ChunkMeshData output)
        {
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (library == null) throw new ArgumentNullException(nameof(library));
            if (output == null) throw new ArgumentNullException(nameof(output));

            ChunkData center = blocks.Center;
            if (center.IsEmpty)
                return;

            for (int y = 0; y < VoxelConstants.ChunkSize; y++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSize; z++)
                {
                    for (int x = 0; x < VoxelConstants.ChunkSize; x++)
                    {
                        ushort id = center.GetBlock(x, y, z);
                        if (id == BlockRegistry.AirId)
                            continue;

                        BlockDefinition block = registry.Get(id);
                        for (int d = 0; d < Directions.Length; d++)
                        {
                            Direction direction = Directions[d];
                            FaceFrame f = FaceFrame.Get(direction);
                            ushort neighborId = blocks.GetBlock(x + f.NX, y + f.NY, z + f.NZ);
                            if (neighborId == id || registry.Get(neighborId).IsOpaque)
                                continue;

                            FaceShape shape = block.FaceShapeOverride;
                            int symmetry = 0;
                            if (shape == null)
                            {
                                byte key = FaceVariantKey.Compute(blocks, x, y, z, direction);
                                shape = library.Resolve(key, tier, out symmetry);
                            }
                            AppendFace(output, shape, symmetry, direction, x, y, z, block.Skin, !block.IsOpaque);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copies <paramref name="shape"/> into <paramref name="mesh"/> on the face of voxel (vx, vy, vz) that looks along
        /// <paramref name="direction"/>, after applying <paramref name="symmetry"/> (mirror first, then rotation).
        /// Uvs come from the shape, or from the transformed (u, w) when the shape has none; the third component is the skin layer.
        /// A mirror reverses triangle order so faces keep facing outward.
        /// Triangles go to <see cref="ChunkMeshData.TransparentIndices"/> when <paramref name="transparent"/>, else to
        /// <see cref="ChunkMeshData.Indices"/>.
        /// </summary>
        public static void AppendFace(ChunkMeshData mesh, FaceShape shape, int symmetry, Direction direction, int vx, int vy, int vz, in BlockSkin skin,
            bool transparent = false)
        {
            FaceFrame f = FaceFrame.Get(direction);
            bool mirror = (symmetry & 4) != 0;
            int rotation = symmetry & 3;
            int baseVertex = mesh.VertexCount;

            for (int i = 0; i < shape.VertexCount; i++)
            {
                float u = shape.Positions[3 * i];
                float w = shape.Positions[3 * i + 1];
                float h = shape.Positions[3 * i + 2];
                float nu = shape.Normals[3 * i];
                float nw = shape.Normals[3 * i + 1];
                float nh = shape.Normals[3 * i + 2];

                if (mirror)
                {
                    u = 1f - u;
                    nu = -nu;
                }
                for (int r = 0; r < rotation; r++)
                {
                    float rotatedU = 1f - w;
                    w = u;
                    u = rotatedU;
                    float rotatedNu = -nw;
                    nw = nu;
                    nu = rotatedNu;
                }

                mesh.Positions.Add(vx + f.OriginX + u * f.UX + w * f.WX + h * f.NX);
                mesh.Positions.Add(vy + f.OriginY + u * f.UY + w * f.WY + h * f.NY);
                mesh.Positions.Add(vz + f.OriginZ + u * f.UZ + w * f.WZ + h * f.NZ);

                mesh.Normals.Add(nu * f.UX + nw * f.WX + nh * f.NX);
                mesh.Normals.Add(nu * f.UY + nw * f.WY + nh * f.NY);
                mesh.Normals.Add(nu * f.UZ + nw * f.WZ + nh * f.NZ);

                float tu = shape.Uvs != null ? shape.Uvs[2 * i] : u;
                float tv = shape.Uvs != null ? shape.Uvs[2 * i + 1] : w;
                mesh.Uvs.Add(tu);
                mesh.Uvs.Add(tv);
                mesh.Uvs.Add(skin.Layer);

                mesh.Colors.Add(skin.Tint);
            }

            List<int> indices = transparent ? mesh.TransparentIndices : mesh.Indices;
            for (int i = 0; i < shape.Indices.Length; i += 3)
            {
                indices.Add(baseVertex + shape.Indices[i]);
                indices.Add(baseVertex + shape.Indices[mirror ? i + 2 : i + 1]);
                indices.Add(baseVertex + shape.Indices[mirror ? i + 1 : i + 2]);
            }
        }
    }
}
