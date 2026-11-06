using System;
using NUnit.Framework;

namespace Viwenta.Voxel.Tests
{
    public class BlockRegistryTests
    {
        static BlockDefinition Stone(ushort id = 1) => new BlockDefinition(id, "stone", true, BlockSkin.Default);

        [Test]
        public void New_ContainsAirAtZero()
        {
            var registry = new BlockRegistry();

            BlockDefinition air = registry.Get(BlockRegistry.AirId);

            Assert.AreEqual("air", air.Name);
            Assert.IsFalse(air.IsOpaque);
        }

        [Test]
        public void Register_ThenGet_ReturnsSameDefinition()
        {
            var registry = new BlockRegistry();
            BlockDefinition stone = Stone();

            registry.Register(stone);

            Assert.AreSame(stone, registry.Get(1));
        }

        [Test]
        public void Register_DuplicateId_Throws()
        {
            var registry = new BlockRegistry();
            registry.Register(Stone());

            Assert.Throws<ArgumentException>(() => registry.Register(new BlockDefinition(1, "dirt", true, BlockSkin.Default)));
        }

        [Test]
        public void Register_AirId_Throws()
        {
            var registry = new BlockRegistry();

            Assert.Throws<ArgumentException>(() => registry.Register(Stone(0)));
        }

        [Test]
        public void Get_Unregistered_Throws()
        {
            var registry = new BlockRegistry();

            Assert.Throws<InvalidOperationException>(() => registry.Get(42));
            Assert.IsFalse(registry.TryGet(42, out _));
        }

        [Test]
        public void Definition_EmptyName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BlockDefinition(1, " ", true, BlockSkin.Default));
        }
    }
}
