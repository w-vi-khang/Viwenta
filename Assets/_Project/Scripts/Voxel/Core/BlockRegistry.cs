using System;

namespace Viwenta.Voxel
{
    /// <summary>Lookup table from block ID to <see cref="BlockDefinition"/>. ID 0 is always air.</summary>
    public sealed class BlockRegistry
    {
        public const ushort AirId = 0;

        readonly BlockDefinition[] _definitions = new BlockDefinition[ushort.MaxValue + 1];

        public BlockRegistry()
        {
            _definitions[AirId] = new BlockDefinition(AirId, "air", false, BlockSkin.Default);
        }

        public void Register(BlockDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition.Id == AirId)
                throw new ArgumentException("Block ID 0 is reserved for air.", nameof(definition));
            if (_definitions[definition.Id] != null)
                throw new ArgumentException($"Block ID {definition.Id} is already registered as '{_definitions[definition.Id].Name}'.", nameof(definition));
            _definitions[definition.Id] = definition;
        }

        public bool TryGet(ushort id, out BlockDefinition definition)
        {
            definition = _definitions[id];
            return definition != null;
        }

        public BlockDefinition Get(ushort id)
        {
            BlockDefinition definition = _definitions[id];
            if (definition == null)
                throw new InvalidOperationException($"Block ID {id} is not registered.");
            return definition;
        }
    }
}
