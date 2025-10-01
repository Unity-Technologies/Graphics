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
        const string k_PrefabPath = "Packages/com.unity.render-pipelines.universal/Tests/Editor/Tools/Converters/ReadonlyMaterialConverter/Cube.prefab";

        GameObject m_GO;
        MeshRenderer m_MeshRenderer;
        Material[] m_RollbackMaterials;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
                Assert.Ignore("Project without URP. Skipping test");

            var universalRenderer = urpAsset.scriptableRenderer as UniversalRenderer;
            if (universalRenderer == null)
                Assert.Ignore("Project without URP - Universal Renderer. Skipping test");
        }

        [SetUp]
        public void Setup()
        {
            m_GO = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath);
            m_MeshRenderer = m_GO.GetComponent<MeshRenderer>();
            Assert.AreEqual(ReadonlyMaterialMap.count, m_MeshRenderer.sharedMaterials.Length, "ReadonlyMaterialMap - Lengths are different");

            int i = 0;
            foreach (var key in ReadonlyMaterialMap.Keys)
            {
                Assert.AreEqual(key, m_MeshRenderer.sharedMaterials[i].name, "ReadonlyMaterialMap - Order has changed");
                ++i;
            }

            m_RollbackMaterials = new Material[ReadonlyMaterialMap.count];
            m_MeshRenderer.sharedMaterials.CopyTo(m_RollbackMaterials, 0);
        }

        [TearDown]
        public void Teardown()
        {
            if (m_MeshRenderer != null)
                m_MeshRenderer.sharedMaterials = m_RollbackMaterials;
        }

        private void CheckMaterials(Material[] actual)
        {
            int i = 0;
            foreach (var key in ReadonlyMaterialMap.Keys)
            {
                Assert.IsTrue(ReadonlyMaterialMap.TryGetMappingMaterial(m_RollbackMaterials[i], out var expected));
                CheckMaterials(expected, actual[i]);
                ++i;
            }
        }

        private void CheckMaterials(Material expected, Material actual)
        {
            Assert.AreEqual(expected, actual, "The material was not changed");
        }

        [Test]
        [Timeout(5 * 60 * 1000)]
        public void ReassignGameObjectMaterials_Succeeds_WhenMaterialCanBeSet()
        {
            var materialConverter = new ReadonlyMaterialConverter();
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(m_GO);

            var assetItem = new RenderPipelineConverterAssetItem(gid, k_PrefabPath);
            materialConverter.assets.Add(assetItem);

            Assert.IsNull(materialConverter.m_MaterialReferenceChanger, "MaterialReferenceChanger should be null before BeforeConvert");
            materialConverter.BeforeConvert();
            Assert.IsNotNull(materialConverter.m_MaterialReferenceChanger, "MaterialReferenceChanger should NOT be null after BeforeConvert");
            var status = materialConverter.Convert(assetItem, out var message);
            materialConverter.AfterConvert();
            Assert.IsNull(materialConverter.m_MaterialReferenceChanger, "MaterialReferenceChanger should be null after AfterConvert");

            Assert.AreEqual(Status.Success, status);
            Assert.IsTrue(string.IsNullOrEmpty(message), $"Message should be empty. Message: {message}");
            CheckMaterials(m_MeshRenderer.sharedMaterials);
        }
    }

}
