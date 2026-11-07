using UnityEngine;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Meshes one fixed chunk (a small hill) from a block registry asset and shows it on this GameObject.
    /// Add it to an empty GameObject, assign a registry, and give the MeshRenderer two materials: opaque (submesh 0) then transparent (submesh 1).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class FixedChunkDemo : MonoBehaviour
    {
        [SerializeField] BlockRegistryAsset registryAsset;
        [Tooltip("Block ID of the top layer of the hill.")]
        [SerializeField] ushort surfaceBlockId = 3;
        [Tooltip("Block ID under the top layer.")]
        [SerializeField] ushort fillBlockId = 2;
        [Tooltip("Block ID of the transparent wall on the hill; 0 for none.")]
        [SerializeField] ushort glassBlockId = 4;
        [Tooltip("Detail tier passed to the mesh builder (0 = cheapest).")]
        [SerializeField, Min(0)] int tier;

        /// <summary>Builds the demo hill: a fill core, a surface top layer and a short transparent wall.</summary>
        public static ChunkData BuildHill(ushort surfaceId, ushort fillId, ushort glassId)
        {
            var chunk = new ChunkData();
            int[] heights = new int[VoxelConstants.ChunkSize * VoxelConstants.ChunkSize];
            for (int z = 0; z < VoxelConstants.ChunkSize; z++)
            {
                for (int x = 0; x < VoxelConstants.ChunkSize; x++)
                {
                    float dx = x - 7.5f, dz = z - 7.5f;
                    int height = Mathf.Clamp(8 - Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dz * dz) * 0.6f), 1, VoxelConstants.ChunkSize);
                    heights[z * VoxelConstants.ChunkSize + x] = height;
                    for (int y = 0; y < height; y++)
                        chunk.SetBlock(x, y, z, y == height - 1 ? surfaceId : fillId);
                }
            }

            if (glassId != 0)
            {
                const int wallZ = 2;
                for (int x = 2; x <= 5; x++)
                {
                    int height = heights[wallZ * VoxelConstants.ChunkSize + x];
                    for (int y = height; y < height + 2 && y < VoxelConstants.ChunkSize; y++)
                        chunk.SetBlock(x, y, wallZ, glassId);
                }
            }
            return chunk;
        }

        void Start()
        {
            if (registryAsset == null)
            {
                Debug.LogError("FixedChunkDemo needs a BlockRegistryAsset.", this);
                return;
            }

            BlockRegistry registry = registryAsset.Build();
            ChunkData chunk = BuildHill(surfaceBlockId, fillBlockId, glassBlockId);

            var data = new ChunkMeshData();
            ChunkMeshBuilder.Build(new ChunkNeighborhood(chunk), registry, new FaceShapeLibrary(), tier, data);

            var mesh = new Mesh { name = "FixedChunk" };
            ChunkMeshConverter.Apply(data, mesh);
            GetComponent<MeshFilter>().sharedMesh = mesh;
            if (GetComponent<MeshRenderer>().sharedMaterials.Length < 2)
                Debug.LogWarning("FixedChunkDemo: the MeshRenderer needs 2 materials (opaque, transparent); transparent faces will not be drawn.", this);
            var stats = new MeshStats();
            stats.Add(data);
            Debug.Log($"FixedChunkDemo: {stats} ({data.VertexCount * ChunkMeshConverter.VertexStrideBytes} vertex bytes).", this);
        }
    }
}
