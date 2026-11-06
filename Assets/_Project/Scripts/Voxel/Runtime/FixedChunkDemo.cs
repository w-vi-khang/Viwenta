using UnityEngine;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Meshes one fixed chunk (a small hill) from a block registry asset and shows it on this GameObject.
    /// Add it to an empty GameObject, assign a registry with at least one opaque block and a material.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class FixedChunkDemo : MonoBehaviour
    {
        [SerializeField] BlockRegistryAsset registryAsset;
        [Tooltip("Block ID used for the hill.")]
        [SerializeField] ushort blockId = 1;
        [Tooltip("Detail tier passed to the mesh builder (0 = cheapest).")]
        [SerializeField, Min(0)] int tier;

        void Start()
        {
            if (registryAsset == null)
            {
                Debug.LogError("FixedChunkDemo needs a BlockRegistryAsset.", this);
                return;
            }

            BlockRegistry registry = registryAsset.Build();
            var chunk = new ChunkData();
            for (int z = 0; z < VoxelConstants.ChunkSize; z++)
            {
                for (int x = 0; x < VoxelConstants.ChunkSize; x++)
                {
                    float dx = x - 7.5f, dz = z - 7.5f;
                    int height = Mathf.Clamp(8 - Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dz * dz) * 0.6f), 1, VoxelConstants.ChunkSize);
                    for (int y = 0; y < height; y++)
                        chunk.SetBlock(x, y, z, blockId);
                }
            }

            var data = new ChunkMeshData();
            ChunkMeshBuilder.Build(new ChunkNeighborhood(chunk), registry, new FaceShapeLibrary(), tier, data);

            var mesh = new Mesh { name = "FixedChunk" };
            ChunkMeshConverter.Apply(data, mesh);
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var stats = new MeshStats();
            stats.Add(data);
            Debug.Log($"FixedChunkDemo: {stats} ({data.VertexCount * ChunkMeshConverter.VertexStrideBytes} vertex bytes).", this);
        }
    }
}
