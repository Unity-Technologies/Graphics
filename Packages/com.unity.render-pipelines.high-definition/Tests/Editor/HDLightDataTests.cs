using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class HDLightDataTests
    {
        GameObject m_Root;

        [SetUp]
        public void SetUp()
        {
           if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
                Assert.Ignore("This is an HDRP Tests, and the current pipeline is not HDRP.");
                
            m_Root = new GameObject("TEST_HDLightDataTests");
            m_Root.AddComponent<Light>();
            m_Root.AddComponent<HDAdditionalLightData>();
            m_Root.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
                CoreUtils.Destroy(m_Root);
        }

        [Test]
        [TestCase(LightType.Tube)]
        [TestCase(LightType.Disc)]
        [TestCase(LightType.Point)]
        public void TestUpdateMesh(LightType type)
        {
            var light = m_Root.GetComponent<Light>();
            light.type = type;

            var additionalLightData = m_Root.GetComponent<HDAdditionalLightData>();
            additionalLightData.displayAreaLightEmissiveMesh = true;
            m_Root.GetComponent<HDAdditionalLightData>().UpdateAreaLightEmissiveMesh();

            if (type.IsArea())
            {
                Assert.IsTrue(GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineRuntimeAssets>(out var assets));
                string expectedPath = AssetDatabase.GetAssetPath(type == LightType.Tube ? assets.emissiveCylinderMesh : assets.emissiveQuadMesh);
                string meshPath = AssetDatabase.GetAssetPath(additionalLightData.m_EmissiveMeshFilter.sharedMesh);
                Assert.AreEqual(expectedPath, meshPath);
            }
            else
            {
                Assert.IsNull(additionalLightData.m_EmissiveMeshFilter);
            }
        }
    }
}
