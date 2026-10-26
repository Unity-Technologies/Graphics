using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class LightShadowLayersMultiEditTests
    {
        const int k_LightCount = 3;

        Light[] m_Lights;
        UniversalRenderPipelineSerializedLight m_SerializedLight;

        [SetUp]
        public void SetUp()
        {
            m_Lights = new Light[k_LightCount];
            for (int i = 0; i < k_LightCount; ++i)
            {
                m_Lights[i] = new GameObject("Light " + i).AddComponent<Light>();
                var additionalData = m_Lights[i].GetUniversalAdditionalLightData();
                additionalData.renderingLayers = 1u << i;
                additionalData.shadowRenderingLayers = 1u << i;
                additionalData.customShadowLayers = true;
            }

            var serializedObject = new SerializedObject(m_Lights);
            m_SerializedLight = new UniversalRenderPipelineSerializedLight(serializedObject, new LightEditor.Settings(serializedObject));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var light in m_Lights)
            {
                Undo.ClearUndo(light.GetUniversalAdditionalLightData());
                Undo.ClearUndo(light);
                Object.DestroyImmediate(light.gameObject);
            }
        }

        [Test]
        public void ChangingShadowLayers_UpdatesRenderingLayerMaskOfAllLights()
        {
            const uint shadowLayers = 1u << 5;
            m_SerializedLight.shadowRenderingLayers.uintValue = shadowLayers;

            UniversalRenderPipelineLightUI.SyncLightAndShadowLayers(m_SerializedLight);

            foreach (var light in m_Lights)
            {
                Assert.AreEqual((int)shadowLayers, light.renderingLayerMask, light.name);
            }
        }

        [Test]
        public void ChangingLightLayersWithoutCustomShadowLayers_UpdatesRenderingLayerMaskOfAllLights()
        {
            m_SerializedLight.customShadowLayers.boolValue = false;
            UniversalRenderPipelineLightUI.SyncLightAndShadowLayers(m_SerializedLight);

            const uint lightLayers = 1u << 4;
            m_SerializedLight.renderingLayers.uintValue = lightLayers;
            UniversalRenderPipelineLightUI.SyncLightAndShadowLayers(m_SerializedLight);

            foreach (var light in m_Lights)
            {
                Assert.AreEqual((int)lightLayers, light.renderingLayerMask, light.name);
            }
        }

        [Test]
        public void DisablingCustomShadowLayers_RestoresEachLightsOwnRenderingLayers()
        {
            m_SerializedLight.shadowRenderingLayers.uintValue = 1u << 5;
            UniversalRenderPipelineLightUI.SyncLightAndShadowLayers(m_SerializedLight);

            m_SerializedLight.customShadowLayers.boolValue = false;
            UniversalRenderPipelineLightUI.SyncLightAndShadowLayers(m_SerializedLight);

            for (int i = 0; i < k_LightCount; ++i)
            {
                Assert.AreEqual(1 << i, m_Lights[i].renderingLayerMask, m_Lights[i].name);
            }
        }

        [Test]
        public void UndoingShadowLayersChange_RestoresRenderingLayerMaskOfAllLights()
        {
            Undo.IncrementCurrentGroup();
            m_SerializedLight.shadowRenderingLayers.uintValue = 1u << 5;
            UniversalRenderPipelineLightUI.SyncLightAndShadowLayers(m_SerializedLight);

            Undo.PerformUndo();

            for (int i = 0; i < k_LightCount; ++i)
            {
                var additionalData = m_Lights[i].GetUniversalAdditionalLightData();
                Assert.AreEqual(1u << i, (uint)additionalData.shadowRenderingLayers, m_Lights[i].name);
                Assert.AreEqual(1 << i, m_Lights[i].renderingLayerMask, m_Lights[i].name);
            }
        }
    }
}
