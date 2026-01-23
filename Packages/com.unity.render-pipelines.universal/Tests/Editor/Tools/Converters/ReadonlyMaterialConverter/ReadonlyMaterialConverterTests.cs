using NUnit.Framework;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class ReadonlyMaterialConverterTests
    {
        const string k_BasePrefabPath = "Assets/ReadonlyMaterialConverterTests/";

        GameObject m_GO;
        MeshRenderer m_MeshRenderer;
        string m_CurrentTestAssetPath;

        public static GameObject CreatePrefabWithMeshRenderer(Material[] materials, string assetPath)
        {
            var go = new GameObject("TestPrefab");

            try
            {
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = materials;

                CoreUtils.EnsureFolderTreeInAssetFilePath(assetPath);

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath, out bool success);
                if (!success || prefab == null)
                {
                    Debug.LogError("Failed to save prefab at: " + assetPath);
                    return null;
                }

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.SaveAssets();

                return prefab;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            return null;
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
                Assert.Ignore("Project without URP. Skipping test");
        }

        [TearDown]
        public void Teardown()
        {
            if (!string.IsNullOrEmpty(m_CurrentTestAssetPath))
            {
                // Delete the specific prefab file
                if (AssetDatabase.LoadAssetAtPath<GameObject>(m_CurrentTestAssetPath) != null)
                {
                    AssetDatabase.DeleteAsset(m_CurrentTestAssetPath);
                }
            }
            m_CurrentTestAssetPath = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up the entire test folder
            if (AssetDatabase.IsValidFolder(k_BasePrefabPath.TrimEnd('/')))
            {
                AssetDatabase.DeleteAsset(k_BasePrefabPath.TrimEnd('/'));
            }
        }

        private void SetupTestForConverter(ReadonlyMaterialConverter converter, string testName)
        {
            var materials = converter.mappings.GetBuiltInMaterials();
            Assume.That(materials.Length > 0, "There are no mapping materials");

            // Create unique asset path for this specific test
            m_CurrentTestAssetPath = k_BasePrefabPath + testName + ".prefab";

            m_GO = CreatePrefabWithMeshRenderer(materials, m_CurrentTestAssetPath);
            m_MeshRenderer = m_GO.GetComponent<MeshRenderer>();

            Assert.AreEqual(converter.mappings.count, m_MeshRenderer.sharedMaterials.Length,
                "ReadonlyMaterialMap - Lengths are different");

            int i = 0;
            foreach (var key in converter.mappings.Keys)
            {
                Assert.AreEqual(key, m_MeshRenderer.sharedMaterials[i].name,
                    "ReadonlyMaterialMap - Order has changed");
                ++i;
            }
        }

        private void CheckMaterialsAfterConversion(ReadonlyMaterialConverter converter, Material[] actual)
        {
            int i = 0;
            var materials = converter.mappings.GetBuiltInMaterials();
            foreach (var key in converter.mappings.Keys)
            {
                Assert.IsTrue(converter.mappings.TryGetMappingMaterial(materials[i], out var expected));
                Assert.AreEqual(expected, actual[i], "The material was not changed");
                ++i;
            }
        }

        private void RunConverterTest(ReadonlyMaterialConverter converter, string testName)
        {
            SetupTestForConverter(converter, testName);

            var gid = GlobalObjectId.GetGlobalObjectIdSlow(m_GO);
            var assetItem = new RenderPipelineConverterAssetItem(gid, AssetDatabase.GetAssetPath(m_GO));
            converter.assets.Add(assetItem);

            Assert.IsNull(converter.m_MaterialReferenceChanger,
                "MaterialReferenceChanger should be null before BeforeConvert");

            converter.BeforeConvert();

            Assert.IsNotNull(converter.m_MaterialReferenceChanger,
                "MaterialReferenceChanger should NOT be null after BeforeConvert");

            var status = converter.Convert(assetItem, out var message);

            converter.AfterConvert();

            Assert.IsNull(converter.m_MaterialReferenceChanger,
                "MaterialReferenceChanger should be null after AfterConvert");

            Assert.AreEqual(Status.Success, status);
            Assert.IsTrue(string.IsNullOrEmpty(message), $"Message should be empty. Message: {message}");

            CheckMaterialsAfterConversion(converter, m_MeshRenderer.sharedMaterials);
        }

        [Test]
        public void BuiltInToURP3D_ReassignMaterials_Succeeds()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset.scriptableRenderer is not UniversalRenderer)
                Assert.Ignore("Requires UniversalRenderer. Skipping test");

            var converter = new BuiltInToURP3DReadonlyMaterialConverter();
            RunConverterTest(converter, nameof(BuiltInToURP3D_ReassignMaterials_Succeeds));
        }

        [Test]
        public void URP3DToURP2D_ReassignMaterials_Succeeds()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset.scriptableRenderer is not Renderer2D)
                Assert.Ignore("Requires Renderer2D. Skipping test");

            var converter = new URP3DToURP2DReadonlyMaterialConverter();
            RunConverterTest(converter, nameof(URP3DToURP2D_ReassignMaterials_Succeeds));
        }

        [Test]
        public void BuiltInToURP2D_ReassignMaterials_Succeeds()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset.scriptableRenderer is not Renderer2D)
                Assert.Ignore("Requires Renderer2D. Skipping test");

            var converter = new BuiltInToURP2DReadonlyMaterialConverter();
            RunConverterTest(converter, nameof(BuiltInToURP2D_ReassignMaterials_Succeeds));
        }
    }
}
