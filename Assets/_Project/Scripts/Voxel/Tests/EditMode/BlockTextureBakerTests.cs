using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Viwenta.Voxel.EditorTools;
using Object = UnityEngine.Object;

namespace Viwenta.Voxel.Tests
{
    public class BlockTextureBakerTests
    {
        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
                Object.DestroyImmediate(o);
            _created.Clear();
        }

        Texture2D MakeTexture(string textureName, int size, Color32 colour, bool mipChain = true)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, mipChain) { name = textureName };
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = colour;
            t.SetPixels32(pixels);
            t.Apply(mipChain);
            _created.Add(t);
            return t;
        }

        [Test]
        public void BuildArray_CopiesEveryLayerAndMip()
        {
            var red = new Color32(255, 0, 0, 255);
            var blue = new Color32(0, 0, 255, 255);
            Texture2D a = MakeTexture("a", 8, red);
            Texture2D b = MakeTexture("b", 8, blue);

            Texture2DArray array = BlockTextureBaker.BuildArray(new[] { a, b }, 8, FilterMode.Trilinear);
            _created.Add(array);

            Assert.AreEqual(2, array.depth);
            Assert.AreEqual(a.mipmapCount, array.mipmapCount);
            Assert.AreEqual(FilterMode.Trilinear, array.filterMode);
            int lastMip = array.mipmapCount - 1;
            Assert.AreEqual(red, array.GetPixels32(0, 0)[0]);
            Assert.AreEqual(red, array.GetPixels32(0, lastMip)[0]);
            Assert.AreEqual(blue, array.GetPixels32(1, 0)[0]);
            Assert.AreEqual(blue, array.GetPixels32(1, lastMip)[0]);
        }

        [Test]
        public void BuildArray_WrongSize_ThrowsWithTextureName()
        {
            Texture2D t = MakeTexture("too_big", 16, Color.white);

            var ex = Assert.Throws<ArgumentException>(() => BlockTextureBaker.BuildArray(new[] { t }, 8, FilterMode.Point));
            StringAssert.Contains("too_big", ex.Message);
            StringAssert.Contains("16x16", ex.Message);
        }

        [Test]
        public void BuildArray_Empty_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() => BlockTextureBaker.BuildArray(new Texture2D[0], 8, FilterMode.Point));
            StringAssert.Contains("No block textures to bake.", ex.Message);
        }

        [Test]
        public void BuildArray_NullEntry_Throws()
        {
            Texture2D t = MakeTexture("a", 8, Color.white);

            var ex = Assert.Throws<ArgumentException>(() => BlockTextureBaker.BuildArray(new Texture2D[] { t, null }, 8, FilterMode.Point));
            StringAssert.Contains("Block texture 1 is missing.", ex.Message);
        }

        [Test]
        public void BuildArray_FaceSizeNotPowerOfTwo_Throws()
        {
            Texture2D t = MakeTexture("a", 8, Color.white);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => BlockTextureBaker.BuildArray(new[] { t }, 24, FilterMode.Point));
            StringAssert.Contains("Face size 24 must be a power of two", ex.Message);
        }

        [Test]
        public void BuildArray_NoMipmaps_Throws()
        {
            Texture2D t = MakeTexture("flat", 8, Color.white, mipChain: false);

            var ex = Assert.Throws<ArgumentException>(() => BlockTextureBaker.BuildArray(new[] { t }, 8, FilterMode.Point));
            StringAssert.Contains("flat", ex.Message);
            StringAssert.Contains("1 mip levels, expected 4", ex.Message);
        }

        [Test]
        public void CollectSources_RemovesDuplicatesAndOrdersByName()
        {
            Texture2D a = MakeTexture("a", 4, Color.white);
            Texture2D b = MakeTexture("b", 4, Color.white);
            var registry = ScriptableObject.CreateInstance<BlockRegistryAsset>();
            _created.Add(registry);
            var blockOne = MakeBlock(1, b, a);
            var blockTwo = MakeBlock(2, b, null);
            var so = new SerializedObject(registry);
            SerializedProperty blocks = so.FindProperty("blocks");
            blocks.arraySize = 2;
            blocks.GetArrayElementAtIndex(0).objectReferenceValue = blockOne;
            blocks.GetArrayElementAtIndex(1).objectReferenceValue = blockTwo;
            so.ApplyModifiedPropertiesWithoutUndo();

            List<Texture2D> sources = BlockTextureBaker.CollectSources(registry);

            CollectionAssert.AreEqual(new[] { a, b }, sources);
            Assert.Throws<ArgumentNullException>(() => BlockTextureBaker.CollectSources(null));
        }

        BlockDefinitionAsset MakeBlock(ushort id, Texture2D top, Texture2D side)
        {
            var block = ScriptableObject.CreateInstance<BlockDefinitionAsset>();
            _created.Add(block);
            var so = new SerializedObject(block);
            so.FindProperty("id").intValue = id;
            so.FindProperty("top").FindPropertyRelative("texture").objectReferenceValue = top;
            so.FindProperty("side").FindPropertyRelative("texture").objectReferenceValue = side;
            so.ApplyModifiedPropertiesWithoutUndo();
            return block;
        }

        [Test]
        public void ImportRules_AppliesOnlyUnderBlocksFolder()
        {
            Assert.IsTrue(BlockTextureImportRules.Applies("Assets/_Project/Art/Blocks/blk_stone.png"));
            Assert.IsFalse(BlockTextureImportRules.Applies("Assets/_Project/Art/blk_stone.png"));
            Assert.IsFalse(BlockTextureImportRules.Applies("assets/_project/art/blocks/x.png"));
            Assert.IsFalse(BlockTextureImportRules.Applies(null));
        }
    }
}
