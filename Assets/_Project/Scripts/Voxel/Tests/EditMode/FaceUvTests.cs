using NUnit.Framework;

namespace Viwenta.Voxel.Tests
{
    public class FaceUvTests
    {
        static void Expected(Direction d, float px, float py, float pz, out float u, out float v)
        {
            switch (d)
            {
                case Direction.PosX: u = pz; v = py; break;
                case Direction.NegX: u = 1f - pz; v = py; break;
                case Direction.PosZ: u = 1f - px; v = py; break;
                case Direction.NegZ: u = px; v = py; break;
                case Direction.PosY: u = px; v = pz; break;
                default: u = px; v = 1f - pz; break;
            }
        }

        [Test]
        public void AppendFace_FlatFaces_TextureUpIsWorldUpAndUGrowsToViewerRight()
        {
            foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
            {
                var mesh = new ChunkMeshData();
                ChunkMeshBuilder.AppendFace(mesh, FaceShape.Flat, 0, direction, 0, 0, 0, BlockSkin.Default);

                Assert.AreEqual(4, mesh.VertexCount, direction.ToString());
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    float px = mesh.Positions[3 * i], py = mesh.Positions[3 * i + 1], pz = mesh.Positions[3 * i + 2];
                    Expected(direction, px, py, pz, out float u, out float v);
                    Assert.AreEqual(u, mesh.Uvs[3 * i], 1e-5f, $"{direction} vertex {i} u");
                    Assert.AreEqual(v, mesh.Uvs[3 * i + 1], 1e-5f, $"{direction} vertex {i} v");
                    bool side = direction != Direction.PosY && direction != Direction.NegY;
                    if (side && py > 0.5f)
                        Assert.AreEqual(1f, mesh.Uvs[3 * i + 1], 1e-5f, $"{direction} vertex {i} top v");
                }
            }
        }
    }
}
