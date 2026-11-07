using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Viwenta.Voxel.Tests
{
    public class VoxelShaderTests
    {
        const string ShaderName = "Viwenta/VoxelChunk";
        const string OpaquePath = "Assets/_Project/Materials/VoxelOpaque.mat";
        const string TransparentPath = "Assets/_Project/Materials/VoxelTransparent.mat";
        const string RegistryPath = "Assets/_Project/Settings/Blocks/DemoBlockRegistry.asset";
        const string ScenePath = "Assets/_Project/Scenes/VoxelDemo.unity";

        [Test]
        public void Shader_CompilesWithoutErrors()
        {
            Shader shader = Shader.Find(ShaderName);

            Assert.IsNotNull(shader);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
        }

        [Test]
        public void Shader_HasForwardShadowAndDepthPasses()
        {
            Shader shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader);

            // Unity reports some LightMode values (ShadowCaster) in upper case, so compare case-insensitively.
            var modes = new HashSet<string>();
            var lightMode = new ShaderTagId("LightMode");
            int count = shader.GetPassCountInSubshader(0);
            for (int i = 0; i < count; i++)
                modes.Add(shader.FindPassTagValue(0, i, lightMode).name.ToUpperInvariant());

            CollectionAssert.IsSupersetOf(modes, new[] { "UNIVERSALFORWARD", "SHADOWCASTER", "DEPTHONLY", "DEPTHNORMALS" });
        }

        [Test]
        public void Materials_OpaqueAndTransparentAreConfigured()
        {
            var opaque = AssetDatabase.LoadAssetAtPath<Material>(OpaquePath);
            var transparent = AssetDatabase.LoadAssetAtPath<Material>(TransparentPath);
            Assert.IsNotNull(opaque);
            Assert.IsNotNull(transparent);

            Assert.AreEqual(2000, opaque.renderQueue);
            Assert.AreEqual(1f, opaque.GetFloat("_ZWrite"));
            Assert.IsTrue(opaque.GetShaderPassEnabled("ShadowCaster"));

            Assert.AreEqual(3000, transparent.renderQueue);
            Assert.AreEqual(0f, transparent.GetFloat("_ZWrite"));
            Assert.AreEqual(5f, transparent.GetFloat("_SrcBlend"));
            Assert.AreEqual(10f, transparent.GetFloat("_DstBlend"));
            Assert.IsFalse(transparent.GetShaderPassEnabled("ShadowCaster"));
            Assert.AreEqual("Transparent", transparent.GetTag("RenderType", false));

            Assert.IsNotNull(opaque.GetTexture("_BlockTextures"));
            Assert.IsNotNull(transparent.GetTexture("_BlockTextures"));
        }

        [Test]
        public void DemoRegistry_GrassUsesDistinctTopSideBottomLayers()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BlockRegistryAsset>(RegistryPath);
            Assert.IsNotNull(asset);

            BlockRegistry registry = asset.Build();
            BlockDefinition grass = registry.Get(3);
            BlockDefinition dirt = registry.Get(2);

            Assert.AreNotEqual(grass.Top.Layer, grass.Side.Layer);
            Assert.AreEqual(dirt.Top.Layer, grass.Bottom.Layer);
            Assert.IsFalse(registry.Get(4).IsOpaque);
        }

        [Test]
        public void DemoScene_FixedChunkHasRegistryAndTwoMaterials()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                FixedChunkDemo demo = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    demo = root.GetComponentInChildren<FixedChunkDemo>();
                    if (demo != null)
                        break;
                }
                Assert.IsNotNull(demo, "VoxelDemo has no FixedChunkDemo");

                var registry = new SerializedObject(demo).FindProperty("registryAsset").objectReferenceValue;
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<BlockRegistryAsset>(RegistryPath), registry);
                Assert.AreEqual(2, demo.GetComponent<MeshRenderer>().sharedMaterials.Length);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
