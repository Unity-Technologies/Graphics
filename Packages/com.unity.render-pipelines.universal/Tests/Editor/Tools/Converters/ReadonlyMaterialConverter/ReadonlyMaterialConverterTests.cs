using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

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
        [UnityPlatform(exclude = new[] { RuntimePlatform.OSXEditor })] // Timing out on macos: https://jira.unity3d.com/browse/UUM-131234
        public void ReassignGameObjectMaterials_Succeeds_WhenMaterialCanBeSet()
        {
            var materialConverter = new ReadonlyMaterialConverter();
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(m_GO);
            materialConverter.Add(gid.ToString(), k_PrefabPath);

            RunItemContext runItemContext = new RunItemContext(new ConverterItemInfo
            {
                descriptor = new ConverterItemDescriptor()
                {
                    name = m_GO.name
                },
                index = 0,
            });

            Assert.IsNull(materialConverter.m_MaterialReferenceChanger, "MaterialReferenceChanger should be null before OnPreRun");
            materialConverter.OnPreRun();
            Assert.IsNotNull(materialConverter.m_MaterialReferenceChanger, "MaterialReferenceChanger should NOT be null after OnPreRun");
            materialConverter.OnRun(ref runItemContext);
            materialConverter.OnPostRun();
            Assert.IsNull(materialConverter.m_MaterialReferenceChanger, "MaterialReferenceChanger should be null after OnPostRun");

            Assert.IsFalse(runItemContext.didFail, runItemContext.info);
            CheckMaterials(m_MeshRenderer.sharedMaterials);
        }
    }

}
