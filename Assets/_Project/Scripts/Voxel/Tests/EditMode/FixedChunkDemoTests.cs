using NUnit.Framework;

namespace Viwenta.Voxel.Tests
{
    public class FixedChunkDemoTests
    {
        const ushort Fill = 2;
        const ushort Surface = 3;
        const ushort Glass = 4;

        static BlockRegistry Registry()
        {
            var registry = new BlockRegistry();
            registry.Register(new BlockDefinition(Fill, "dirt", true, BlockSkin.Default));
            registry.Register(new BlockDefinition(Surface, "grass", true, BlockSkin.Default));
            registry.Register(new BlockDefinition(Glass, "glass", false, BlockSkin.Default));
            return registry;
        }

        static ChunkMeshData Mesh(ChunkData chunk)
        {
            var mesh = new ChunkMeshData();
            ChunkMeshBuilder.Build(new ChunkNeighborhood(chunk), Registry(), new FaceShapeLibrary(), 0, mesh);
            return mesh;
        }

        [Test]
        public void BuildHill_TopLayerIsSurfaceAndBelowIsFill()
        {
            ChunkData chunk = FixedChunkDemo.BuildHill(Surface, Fill, Glass);

            int top = VoxelConstants.ChunkSize - 1;
            while (top >= 0 && chunk.GetBlock(7, top, 7) == BlockRegistry.AirId)
                top--;

            Assert.GreaterOrEqual(top, 1);
            Assert.AreEqual(Surface, chunk.GetBlock(7, top, 7));
            Assert.AreEqual(Fill, chunk.GetBlock(7, top - 1, 7));
        }

        [Test]
        public void BuildHill_GlassWall_GoesToTransparentSubMesh()
        {
            ChunkMeshData withGlass = Mesh(FixedChunkDemo.BuildHill(Surface, Fill, Glass));
            Assert.Greater(withGlass.TransparentIndices.Count, 0);

            ChunkData noGlass = FixedChunkDemo.BuildHill(Surface, Fill, 0);
            Assert.AreEqual(0, Mesh(noGlass).TransparentIndices.Count);
            for (int z = 0; z < VoxelConstants.ChunkSize; z++)
                for (int y = 0; y < VoxelConstants.ChunkSize; y++)
                    for (int x = 0; x < VoxelConstants.ChunkSize; x++)
                        Assert.AreNotEqual(Glass, noGlass.GetBlock(x, y, z));
        }
    }
}
