using System;
using System.Collections.Generic;

namespace Viwenta.Voxel
{
    /// <summary>
    /// Prebuilt face shapes keyed by (canonical <see cref="FaceVariantKey"/>, detail tier). Tier 0 is the cheapest;
    /// higher tiers carry more vertices (rounded bevels) and are chosen for chunks close to the viewer.
    /// A missing entry falls back to a lower tier, then to the flat square.
    /// </summary>
    public sealed class FaceShapeLibrary
    {
        readonly Dictionary<int, FaceShape> _shapes = new Dictionary<int, FaceShape>();

        public int Count => _shapes.Count;

        public void Add(byte canonicalKey, int tier, FaceShape shape)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            if (tier < 0)
                throw new ArgumentOutOfRangeException(nameof(tier), "Tier must not be negative.");
            if (FaceVariantKey.Canonical(canonicalKey) != canonicalKey)
                throw new ArgumentException($"Key {canonicalKey} is not canonical; use {FaceVariantKey.Canonical(canonicalKey)}.", nameof(canonicalKey));
            if (!_shapes.TryAdd(Pack(canonicalKey, tier), shape))
                throw new ArgumentException($"A shape for key {canonicalKey} at tier {tier} already exists.");
        }

        /// <summary>
        /// Finds the shape for <paramref name="rawKey"/>; <paramref name="symmetry"/> is the transform to apply to it.
        /// </summary>
        public FaceShape Resolve(byte rawKey, int tier, out int symmetry)
        {
            byte canonical = FaceVariantKey.Canonical(rawKey);
            for (int t = tier; t >= 0; t--)
            {
                if (_shapes.TryGetValue(Pack(canonical, t), out FaceShape shape))
                {
                    symmetry = FaceVariantKey.SymmetryFor(rawKey);
                    return shape;
                }
            }
            symmetry = 0;
            return FaceShape.Flat;
        }

        static int Pack(byte key, int tier) => key | (tier << 8);
    }
}
