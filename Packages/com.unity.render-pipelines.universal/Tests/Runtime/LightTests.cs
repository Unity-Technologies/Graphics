using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    class LightTests
    {
        GameObject m_LightObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_LightObject);
        }

        [Test]
        public void TestMainLightRenderingLayerMaskSyncWithUniversalLightAndShadowLayers()
        {
            m_LightObject = new GameObject("Light");
            var light = m_LightObject.AddComponent<Light>();
            var lightData = light.GetUniversalAdditionalLightData();

            lightData.renderingLayers = (1 << 1);
            lightData.shadowRenderingLayers = (1 << 2);

            lightData.customShadowLayers = false;
            Assert.AreEqual(light.renderingLayerMask, (int)lightData.renderingLayers);

            lightData.customShadowLayers = true;
            Assert.AreEqual(light.renderingLayerMask, (int)lightData.shadowRenderingLayers);

            lightData.customShadowLayers = false;
            lightData.renderingLayers = (1 << 3);
            lightData.shadowRenderingLayers = (1 << 4);
            Assert.AreEqual(light.renderingLayerMask, (int)lightData.renderingLayers);

            lightData.customShadowLayers = true;
            lightData.renderingLayers = (1 << 5);
            lightData.shadowRenderingLayers = (1 << 6);
            Assert.AreEqual(light.renderingLayerMask, (int)lightData.shadowRenderingLayers);
        }
    }
}
