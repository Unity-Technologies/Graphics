#if ENABLE_AMD && ENABLE_AMD_MODULE
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.HighDefinition
{
    public class FSR2PassLifetimeTests
    {
        const int k_FramesToSettle = 10;

        RenderPipelineAsset m_PreviousQualityPipeline;
        HDRenderPipelineAsset m_FSR2Asset;
        GameObject m_CameraObject;
        RenderTexture m_Target;
        AMD.GraphicsDevice m_Device;
        AMD.GraphicsDevice.GraphicsDeviceDebugView m_DebugView;

        [SetUp]
        public void SetUp()
        {
            m_PreviousQualityPipeline = QualitySettings.renderPipeline;

            var activeAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (activeAsset == null)
                Assert.Ignore("HDRP is not the active render pipeline.");

            m_Device = AMD.GraphicsDevice.device;
            if (!HDDynamicResolutionPlatformCapabilities.FSR2Detected || m_Device == null || !m_Device.IsFeatureAvailable(AMD.GraphicsDeviceFeature.FSRUpscale2))
                Assert.Ignore("FSR2 is not available on this platform and graphics API.");

            m_DebugView = m_Device.CreateDebugView();

            m_FSR2Asset = Object.Instantiate(activeAsset);
            var settings = m_FSR2Asset.currentPlatformRenderPipelineSettings;
            settings.dynamicResolutionSettings.enabled = true;
            settings.dynamicResolutionSettings.advancedUpscalerNames = new List<string> { AdvancedUpscalers.FSR2.ToString() };
            settings.dynamicResolutionSettings.FSR2InjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.BeforePost;
            settings.dynamicResolutionSettings.FSR2UseOptimalSettings = false;
            m_FSR2Asset.currentPlatformRenderPipelineSettings = settings;
            QualitySettings.renderPipeline = m_FSR2Asset;

            m_Target = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            m_CameraObject = new GameObject(nameof(FSR2PassLifetimeTests));
            var camera = m_CameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.targetTexture = m_Target;
            m_CameraObject.AddComponent<HDAdditionalCameraData>().allowFidelityFX2SuperResolution = true;
        }

        [TearDown]
        public void TearDown()
        {
            QualitySettings.renderPipeline = m_PreviousQualityPipeline;

            if (m_CameraObject != null)
                Object.DestroyImmediate(m_CameraObject);

            if (m_Target != null)
                Object.DestroyImmediate(m_Target);

            if (m_FSR2Asset != null)
                Object.DestroyImmediate(m_FSR2Asset);

            m_Device?.DeleteDebugView(m_DebugView);
        }

        [UnityTest]
        public IEnumerator SwitchingPipelineAsset_DestroysFSR2Contexts()
        {
            var camera = m_CameraObject.GetComponent<Camera>();
            var cameraData = m_CameraObject.GetComponent<HDAdditionalCameraData>();

            camera.Render();
            var fsr2Pipeline = RenderPipelineManager.currentPipeline;
            for (int frame = 0; frame < k_FramesToSettle; ++frame)
                yield return null;

            uint baseline = LiveFeatureCount();

            cameraData.allowDynamicResolution = true;
            uint whileActive = baseline;
            for (int frame = 0; frame < k_FramesToSettle && whileActive <= baseline; ++frame)
            {
                camera.Render();
                yield return null;
                whileActive = LiveFeatureCount();
            }

            Assert.That(whileActive, Is.GreaterThan(baseline), "FSR2 created no context while the camera rendered with FSR2.");

            cameraData.allowDynamicResolution = false;
            QualitySettings.renderPipeline = m_PreviousQualityPipeline;
            camera.Render();

            Assert.That(fsr2Pipeline.disposed, Is.True, "Rendering with the previous pipeline asset did not dispose the FSR2 pipeline.");

            uint afterSwitch = whileActive;
            for (int frame = 0; frame < k_FramesToSettle && afterSwitch > baseline; ++frame)
            {
                yield return null;
                afterSwitch = LiveFeatureCount();
            }

            Assert.That(afterSwitch, Is.LessThanOrEqualTo(baseline), "FSR2 contexts outlived the pipeline that created them.");
        }

        uint LiveFeatureCount()
        {
            m_Device.UpdateDebugView(m_DebugView);
            return m_DebugView.upscaleFeatureInfoCount;
        }
    }
}
#endif
