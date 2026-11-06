using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Viwenta.Voxel.Tests
{
    public class BlockSkinTests
    {
        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
                Object.DestroyImmediate(o);
            _created.Clear();
        }

        Texture2D MakeTexture(string textureName)
        {
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, true) { name = textureName };
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(pixels);
            t.Apply(true);
            _created.Add(t);
            return t;
        }

        T MakeAsset<T>(string assetName) where T : ScriptableObject
        {
            var a = ScriptableObject.CreateInstance<T>();
            a.name = assetName;
            _created.Add(a);
            return a;
        }

        BlockTextureLibrary MakeLibrary(params Texture2D[] baked)
        {
            var library = MakeAsset<BlockTextureLibrary>("lib");
            var array = new Texture2DArray(4, 4, Math.Max(1, baked.Length), TextureFormat.RGBA32, true);
            _created.Add(array);
            library.SetBakedData(baked, array);
            return library;
        }

        BlockDefinitionAsset MakeBlock(string blockName, ushort id, Texture2D top, Texture2D side, Texture2D bottom, Color32? sideTint = null)
        {
            var block = MakeAsset<BlockDefinitionAsset>(blockName);
            var so = new SerializedObject(block);
            so.FindProperty("id").intValue = id;
            so.FindProperty("blockName").stringValue = blockName;
            SetSlot(so, "top", top, null);
            SetSlot(so, "side", side, sideTint);
            SetSlot(so, "bottom", bottom, null);
            so.ApplyModifiedPropertiesWithoutUndo();
            return block;
        }

        static void SetSlot(SerializedObject so, string slot, Texture2D texture, Color32? tint)
        {
            SerializedProperty p = so.FindProperty(slot);
            p.FindPropertyRelative("texture").objectReferenceValue = texture;
            if (tint.HasValue)
                p.FindPropertyRelative("tint").colorValue = tint.Value;
        }

        [Test]
        public void SkinFor_MapsUpToTopDownToBottomAndOthersToSide()
        {
            var top = new BlockSkin(1, 0xAu);
            var side = new BlockSkin(2, 0xBu);
            var bottom = new BlockSkin(3, 0xCu);
            var block = new BlockDefinition(1, "grass", true, top, side, bottom);

            Assert.AreEqual(top, block.SkinFor(Direction.PosY));
            Assert.AreEqual(bottom, block.SkinFor(Direction.NegY));
            Assert.AreEqual(side, block.SkinFor(Direction.PosX));
            Assert.AreEqual(side, block.SkinFor(Direction.NegX));
            Assert.AreEqual(side, block.SkinFor(Direction.PosZ));
            Assert.AreEqual(side, block.SkinFor(Direction.NegZ));
        }

        [Test]
        public void SingleSkinConstructor_UsesSkinOnEveryFace()
        {
            var skin = new BlockSkin(5, 0x01020304u);
            var block = new BlockDefinition(1, "stone", true, skin);

            Assert.AreEqual(skin, block.Top);
            Assert.AreEqual(skin, block.Side);
            Assert.AreEqual(skin, block.Bottom);
        }

        [Test]
        public void Build_GrassLikeBlock_UsesTopSideAndBottomLayers()
        {
            var registry = new BlockRegistry();
            registry.Register(new BlockDefinition(1, "grass", true,
                new BlockSkin(1, 0xFFFFFFFFu), new BlockSkin(2, 0xFFFFFFFFu), new BlockSkin(3, 0xFFFFFFFFu)));
            var chunk = new ChunkData();
            chunk.SetBlock(5, 5, 5, 1);
            var mesh = new ChunkMeshData();

            ChunkMeshBuilder.Build(new ChunkNeighborhood(chunk), registry, new FaceShapeLibrary(), 0, mesh);

            Assert.AreEqual(24, mesh.VertexCount);
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                float ny = mesh.Normals[3 * i + 1];
                float expected = ny > 0.5f ? 1f : ny < -0.5f ? 3f : 2f;
                Assert.AreEqual(expected, mesh.Uvs[3 * i + 2], $"vertex {i}");
            }
        }

        [Test]
        public void ToDefinition_ResolvesLayersFromLibraryAndFallsBackToTop()
        {
            Texture2D a = MakeTexture("a");
            Texture2D b = MakeTexture("b");
            BlockTextureLibrary library = MakeLibrary(a, b);
            BlockDefinitionAsset block = MakeBlock("grass", 4, b, null, a, new Color32(10, 20, 30, 40));

            BlockDefinition def = block.ToDefinition(library);

            Assert.AreEqual(1, def.Top.Layer);
            Assert.AreEqual(1, def.Side.Layer);
            Assert.AreEqual(0, def.Bottom.Layer);
            Assert.AreEqual(def.Top.Tint, def.Side.Tint);
            Assert.AreEqual(0xFFFFFFFFu, def.Top.Tint);
        }

        [Test]
        public void ToDefinition_NoTopTexture_ThrowsWithBlockName()
        {
            BlockDefinitionAsset block = MakeBlock("ghost", 9, null, null, null);

            var ex = Assert.Throws<InvalidOperationException>(() => block.ToDefinition(MakeLibrary(MakeTexture("a"))));
            StringAssert.Contains("ghost", ex.Message);
        }

        [Test]
        public void ToDefinition_TextureNotBaked_ThrowsWithSlotAndTextureName()
        {
            Texture2D a = MakeTexture("a");
            Texture2D missing = MakeTexture("missing_side");
            BlockDefinitionAsset block = MakeBlock("dirt", 3, a, missing, null);

            var ex = Assert.Throws<InvalidOperationException>(() => block.ToDefinition(MakeLibrary(a)));
            StringAssert.Contains("dirt", ex.Message);
            StringAssert.Contains("side", ex.Message);
            StringAssert.Contains("missing_side", ex.Message);
        }

        [Test]
        public void ToDefinition_NullLibrary_Throws()
        {
            BlockDefinitionAsset block = MakeBlock("dirt", 3, MakeTexture("a"), null, null);

            Assert.Throws<ArgumentNullException>(() => block.ToDefinition(null));
        }

        [Test]
        public void RegistryBuild_NoLibrary_ThrowsWithRegistryName()
        {
            var registry = MakeAsset<BlockRegistryAsset>("my_registry");

            var ex = Assert.Throws<InvalidOperationException>(() => registry.Build());
            StringAssert.Contains("my_registry", ex.Message);
        }
    }
}
