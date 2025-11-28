using NUnit.Framework;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class ReadonlyMaterialConverterTests
    {
        const string k_PrefabPath = "Assets/ReadonlyMaterialConverterTests/";

        GameObject m_GO;
        MeshRenderer m_MeshRenderer;
        BuiltInToURP3DReadonlyMaterialConverter m_Converter;

        public static GameObject CreatePrefabWithMeshRenderer(Material[] materials, string assetPath)
        {
            // Create a temporary GameObject
            var go = new GameObject("CreatePrefabWithMeshRenderer_GO");

            try
            {
                // Add components
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = materials;

                string localPath = assetPath + go.name + ".prefab";
                CoreUtils.EnsureFolderTreeInAssetFilePath(localPath);

                // Save as prefab
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, localPath, out bool success);
                if (!success || prefab == null)
                {
                    Debug.LogError("Failed to save prefab at: " + assetPath);
                    return null;
                }

                AssetDatabase.ImportAsset(localPath);
                AssetDatabase.SaveAssets();

                return prefab;
            }
            finally
            {
                // Cleanup temporary instance
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var hdAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (hdAsset == null)
                Assert.Ignore("Project without HDRP. Skipping test");
        }

        [SetUp]
        public void Setup()
        {
            m_Converter = new BuiltInToURP3DReadonlyMaterialConverter();

            var materials = m_Converter.mappings.GetBuiltInMaterials();
            Assume.That(materials.Length > 0, "There are no mapping materials");

            m_GO = CreatePrefabWithMeshRenderer(materials, k_PrefabPath);
            m_MeshRenderer = m_GO.GetComponent<MeshRenderer>();
            Assert.AreEqual(m_Converter.mappings.count, m_MeshRenderer.sharedMaterials.Length, "ReadonlyMaterialMap - Lengths are different");

            int i = 0;
            foreach (var key in m_Converter.mappings.Keys)
            {
                Assert.AreEqual(key, m_MeshRenderer.sharedMaterials[i].name, "ReadonlyMaterialMap - Order has changed");
                ++i;
            }
        }

        [TearDown]
        public void Teardown()
        {
            AssetDatabase.DeleteAsset(k_PrefabPath);
        }

        private void CheckMaterials(Material[] actual)
        {
            int i = 0;
            var materials = m_Converter.mappings.GetBuiltInMaterials();
            foreach (var key in m_Converter.mappings.Keys)
            {
                Assert.IsTrue(m_Converter.mappings.TryGetMappingMaterial(materials[i], out var expected));
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
            var materialConverter = new BuiltInToURP3DReadonlyMaterialConverter();
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(m_GO);

            var assetItem = new RenderPipelineConverterAssetItem(gid, AssetDatabase.GetAssetPath(m_GO));
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
