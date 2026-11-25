using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // Covers the public UniversalRenderPipelineAsset.scalingMode: ScalingMode.None must leave the pipeline without an
    // active upscaler, and a game camera must then render at its native resolution with no upscaling step.
    class ScalingModeTests
    {
        // Records the camera data URP resolved for the test camera, so the tests can inspect it after rendering.
        class CaptureCameraDataFeature : ScriptableRendererFeature
        {
            public Camera targetCamera;
            public bool captured;
            public ImageScalingMode imageScalingMode;
            public float renderScale;
            public int targetWidth;
            public int targetHeight;

            public override void Create() { }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.camera != targetCamera)
                    return;

                captured = true;
                imageScalingMode = renderingData.cameraData.imageScalingMode;
                renderScale = renderingData.cameraData.renderScale;
                targetWidth = renderingData.cameraData.cameraTargetDescriptor.width;
                targetHeight = renderingData.cameraData.cameraTargetDescriptor.height;
            }
        }

        const int k_TargetSize = 64;

        UniversalRendererData m_RendererData;
        UniversalRenderPipelineAsset m_PipelineAsset;
        CaptureCameraDataFeature m_CaptureFeature;
        RenderPipelineAsset m_PreviousQualityPipeline;
        bool m_PipelineOverridden;
        GameObject m_CameraGO;
        Camera m_Camera;
        RenderTexture m_TargetTexture;

        [SetUp]
        public void SetUp()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("Requires URP to be the active render pipeline.");

            m_PreviousQualityPipeline = QualitySettings.renderPipeline;

            m_CaptureFeature = ScriptableObject.CreateInstance<CaptureCameraDataFeature>();
            m_RendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            m_RendererData.rendererFeatures.Add(m_CaptureFeature);

            // Create() populates the default upscaler priority list (STP, then Auto), so the upscaler selection
            // depends only on scalingMode.
            m_PipelineAsset = UniversalRenderPipelineAsset.Create(m_RendererData);

            m_TargetTexture = new RenderTexture(k_TargetSize, k_TargetSize, 24);
            m_CameraGO = new GameObject("ScalingModeTestCamera") { hideFlags = HideFlags.HideAndDontSave };
            m_Camera = m_CameraGO.AddComponent<Camera>();
            m_Camera.targetTexture = m_TargetTexture;
            m_CameraGO.AddComponent<UniversalAdditionalCameraData>();
            m_CaptureFeature.targetCamera = m_Camera;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore first: teardown also runs when setup throws part way through.
            if (m_PipelineOverridden)
            {
                QualitySettings.renderPipeline = m_PreviousQualityPipeline;
                m_PipelineOverridden = false;
            }

            if (m_CameraGO != null)
                Object.DestroyImmediate(m_CameraGO);
            if (m_TargetTexture != null)
            {
                m_TargetTexture.Release();
                Object.DestroyImmediate(m_TargetTexture);
            }
            if (m_PipelineAsset != null)
                Object.DestroyImmediate(m_PipelineAsset);
            if (m_RendererData != null)
                Object.DestroyImmediate(m_RendererData);
            if (m_CaptureFeature != null)
                Object.DestroyImmediate(m_CaptureFeature);
        }

        // The upscaler is resolved once when the pipeline is created, so scalingMode has to be set before the asset
        // becomes active. Rendering the camera creates the pipeline from the asset.
        UniversalRenderPipeline RenderWithScalingMode(ScalingMode scalingMode)
        {
            m_PipelineAsset.scalingMode = scalingMode;
            QualitySettings.renderPipeline = m_PipelineAsset;
            m_PipelineOverridden = true;

            m_Camera.Render();

            Assert.AreSame(m_PipelineAsset, GraphicsSettings.currentRenderPipeline, "The test asset is not the active pipeline asset.");
            var pipeline = RenderPipelineManager.currentPipeline as UniversalRenderPipeline;
            Assert.IsNotNull(pipeline, "URP was not created for the test asset.");
            Assert.IsTrue(m_CaptureFeature.captured, "The test camera was not rendered by URP.");
            return pipeline;
        }

        [Test]
        public void ScalingModeNone_HasNoActiveUpscaler()
        {
            var pipeline = RenderWithScalingMode(ScalingMode.None);

            Assert.AreEqual(ScalingMode.None, m_PipelineAsset.scalingMode);
            Assert.IsEmpty(pipeline.activeUpscalerName, "ScalingMode.None should leave the pipeline without an active upscaler.");
        }

        [Test]
        public void ScalingModeNone_CameraRendersAtNativeResolution()
        {
            RenderWithScalingMode(ScalingMode.None);

            Assert.AreEqual(ImageScalingMode.None, m_CaptureFeature.imageScalingMode, "ScalingMode.None should not upscale the camera.");
            Assert.AreEqual(1.0f, m_CaptureFeature.renderScale);
            Assert.AreEqual(m_Camera.pixelWidth, m_CaptureFeature.targetWidth);
            Assert.AreEqual(m_Camera.pixelHeight, m_CaptureFeature.targetHeight);
        }

        // Guards the tests above: with the same asset, Upscaling selects an upscaler, so an empty name under None is
        // caused by scalingMode and not by the device lacking upscaler support.
        [Test]
        public void ScalingModeUpscaling_HasActiveUpscaler()
        {
            var pipeline = RenderWithScalingMode(ScalingMode.Upscaling);

            Assert.IsNotEmpty(pipeline.activeUpscalerName, "ScalingMode.Upscaling should select an upscaler from the priority list.");
        }

        // The upscaler is resolved when the pipeline is created, so setting scalingMode on the already-active asset must
        // recreate the pipeline for the change to take effect. This covers the setter path that does not go through the
        // inspector.
        [Test]
        public void ScalingMode_SetAfterPipelineActive_RecreatesPipeline()
        {
            var upscalingPipeline = RenderWithScalingMode(ScalingMode.Upscaling);
            Assert.IsNotEmpty(upscalingPipeline.activeUpscalerName, "ScalingMode.Upscaling should select an upscaler from the priority list.");

            m_PipelineAsset.scalingMode = ScalingMode.None;
            m_Camera.Render();

            var pipeline = RenderPipelineManager.currentPipeline as UniversalRenderPipeline;
            Assert.IsNotNull(pipeline, "URP was not created for the test asset.");
            Assert.AreNotSame(upscalingPipeline, pipeline, "Setting scalingMode should recreate the current pipeline.");
            Assert.IsEmpty(pipeline.activeUpscalerName, "ScalingMode.None should leave the pipeline without an active upscaler after recreation.");
        }
    }
}
