using UnityEngine;

namespace Viwenta.Voxel
{
    /// <summary>The list of block definitions that make up a game's block table.</summary>
    [CreateAssetMenu(menuName = "Viwenta/Voxel/Block Registry", fileName = "BlockRegistry")]
    public sealed class BlockRegistryAsset : ScriptableObject
    {
        [SerializeField] BlockDefinitionAsset[] blocks = System.Array.Empty<BlockDefinitionAsset>();

        /// <summary>Builds the engine-free registry. Throws when two blocks share an ID.</summary>
        public BlockRegistry Build()
        {
            var registry = new BlockRegistry();
            foreach (BlockDefinitionAsset block in blocks)
            {
                if (block == null)
                    throw new System.InvalidOperationException($"Block registry '{name}' has an empty slot.");
                registry.Register(block.ToDefinition());
            }
            return registry;
        }
    }
}
