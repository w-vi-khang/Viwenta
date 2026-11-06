namespace Viwenta.Voxel
{
    /// <summary>
    /// Running totals of what the voxel renderer hands to the GPU, for profiling against a budget.
    /// Add each chunk mesh when it is built and subtract it when it is released.
    /// </summary>
    public struct MeshStats
    {
        public int Meshes;
        public long Vertices;
        public long Triangles;

        public void Add(ChunkMeshData mesh)
        {
            if (mesh.VertexCount == 0)
                return;
            Meshes++;
            Vertices += mesh.VertexCount;
            Triangles += mesh.TriangleCount;
        }

        public void Remove(ChunkMeshData mesh)
        {
            if (mesh.VertexCount == 0)
                return;
            Meshes--;
            Vertices -= mesh.VertexCount;
            Triangles -= mesh.TriangleCount;
        }

        public override string ToString() => $"{Meshes} meshes, {Vertices} vertices, {Triangles} triangles";
    }
}
