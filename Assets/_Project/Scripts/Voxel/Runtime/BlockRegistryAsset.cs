using System.Collections.Generic;
using UnityEngine;

namespace Viwenta.Voxel
{
    /// <summary>The list of block definitions that make up a game's block table.</summary>
    [CreateAssetMenu(menuName = "Viwenta/Voxel/Block Registry", fileName = "BlockRegistry")]
    public sealed class BlockRegistryAsset : ScriptableObject
    {
        [SerializeField] BlockDefinitionAsset[] blocks = System.Array.Empty<BlockDefinitionAsset>();
        [Tooltip("Texture library the block textures are baked into.")]
        [SerializeField] BlockTextureLibrary textures;

        public BlockTextureLibrary Textures => textures;
        public IReadOnlyList<BlockDefinitionAsset> Blocks => blocks;

        /// <summary>Builds the engine-free registry. Throws when two blocks share an ID, a slot is empty, or a block texture is not baked.</summary>
        public BlockRegistry Build()
        {
            if (textures == null)
                throw new System.InvalidOperationException($"Block registry '{name}' has no block texture library.");
            var registry = new BlockRegistry();
            foreach (BlockDefinitionAsset block in blocks)
            {
                if (block == null)
                    throw new System.InvalidOperationException($"Block registry '{name}' has an empty slot.");
                registry.Register(block.ToDefinition(textures));
            }
            return registry;
        }
    }
}
