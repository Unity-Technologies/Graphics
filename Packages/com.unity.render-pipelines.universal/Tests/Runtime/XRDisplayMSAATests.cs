using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    // XR counterpart of RealBackbufferMSAATests: URP keeps the XR eye textures single-sampled while no XR camera
    // renders MSAA directly into them (e.g. the camera goes through post-processing).
    [TestFixture]
    class XRDisplayMSAATests
    {
        const int k_AssetMsaaSampleCount = 4;
        // Downgrade hysteresis + one-frame apply lag (eye textures are reallocated at the next XR frame start) + margin.
        const int k_SettleFrames = UniversalRenderPipeline.realBackbufferMSAADowngradeDelayFrames + 1 + 3;
        // Upgrades skip the hysteresis: 1 frame to request + 1 frame for the reallocation + 1 frame of margin.
        const int k_UpgradeFrames = 3;

        RenderPipelineAsset m_PreviousQualityRenderPipelineAsset;
        UniversalRenderPipelineAsset m_Asset;
        UniversalRendererData m_RendererData;
        Camera m_Camera;
        bool m_Intermediate;

        readonly List<Camera> m_DisabledCameras = new();
        readonly ForceIntermediatePass m_ForceIntermediatePass = new();
        ProbePass m_ProbePass;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Captured before any early-out: NUnit runs OneTimeTearDown even when OneTimeSetUp ignores.
            m_PreviousQualityRenderPipelineAsset = QualitySettings.renderPipeline;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("These tests only run when URP is the active render pipeline.");
            if (UniversalRenderer.PlatformRequiresExplicitMsaaResolve())
                Assert.Ignore("Platform resolves MSAA explicitly (no multisampled eye texture); the display target MSAA request is always 1, so there is nothing to test here.");
            if (!XRSystem.displayActive)
                Assert.Ignore("No active XR display.");

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (!cam.enabled)
                    continue;
                cam.enabled = false;
                m_DisabledCameras.Add(cam);
            }

            m_RendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            m_RendererData.renderingMode = RenderingMode.Forward;
            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_Asset.m_RendererDataList[0] = m_RendererData;
            // Pin every setting that feeds RequiresIntermediateColorTexture, so only the force-intermediate pass
            // decides whether the camera renders directly.
            m_Asset.supportsHDR = false;
            m_Asset.supportsCameraOpaqueTexture = false;
            m_Asset.supportsCameraDepthTexture = false;
            m_Asset.renderScale = 1.0f;
            QualitySettings.renderPipeline = m_Asset;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            // Restoring the pipeline recreates it, which resets the XR display MSAA to the quality setting.
            QualitySettings.renderPipeline = m_PreviousQualityRenderPipelineAsset;

            foreach (var cam in m_DisabledCameras)
            {
                if (cam != null)
                    cam.enabled = true;
            }
            m_DisabledCameras.Clear();

            if (m_RendererData != null)
                Object.DestroyImmediate(m_RendererData);
            if (m_Asset != null)
                Object.DestroyImmediate(m_Asset);
        }

        [SetUp]
        public void SetUp()
        {
            m_Asset.msaaSampleCount = k_AssetMsaaSampleCount;
            m_Intermediate = false;
            m_ProbePass = new ProbePass();

            var go = new GameObject("MSAA Test XR Camera");
            m_Camera = go.AddComponent<Camera>();
            m_Camera.allowMSAA = true;
            m_Camera.allowHDR = false;
            m_Camera.allowDynamicResolution = false;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(0);
            data.renderPostProcessing = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Camera != null)
                Object.DestroyImmediate(m_Camera.gameObject);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != m_Camera)
                return;

            var renderer = camera.GetComponent<UniversalAdditionalCameraData>().scriptableRenderer;
            if (m_Intermediate)
                renderer.EnqueuePass(m_ForceIntermediatePass);
            renderer.EnqueuePass(m_ProbePass);
        }

        [UnityTest]
        public IEnumerator IntermediateToggles_DisplayFollows()
        {
            // Like toggling post-processing: the final pass becomes a full-screen blit, so the eye textures don't
            // need MSAA, and get it back when the camera renders directly again.
            yield return TickFrames(k_SettleFrames);
            AssertDisplayMSAA(k_AssetMsaaSampleCount, checkEyeTexture: false);
            // Where the provider doesn't multisample its eye textures, only the request can be checked.
            bool eyeTextureMsaaSupported = m_ProbePass.backbufferMsaaSamples == k_AssetMsaaSampleCount;

            m_Intermediate = true;
            yield return TickFrames(k_SettleFrames);
            AssertDisplayMSAA(1, eyeTextureMsaaSupported);

            m_Intermediate = false;
            yield return TickFrames(k_UpgradeFrames);
            AssertDisplayMSAA(k_AssetMsaaSampleCount, eyeTextureMsaaSupported);
        }

        [UnityTest]
        public IEnumerator AssetMSAAChangedAtRuntime_DisplayFollows()
        {
            // A runtime asset change must reach the eye textures without recreating the pipeline.
            const int k_ChangedSampleCount = 2;
            yield return TickFrames(k_SettleFrames);
            AssertDisplayMSAA(k_AssetMsaaSampleCount, checkEyeTexture: false);
            bool eyeTextureMsaaSupported = m_ProbePass.backbufferMsaaSamples == k_AssetMsaaSampleCount;

            m_Asset.msaaSampleCount = k_ChangedSampleCount;
            yield return TickFrames(k_UpgradeFrames);
            AssertDisplayMSAA(k_ChangedSampleCount, eyeTextureMsaaSupported);
        }

        [UnityTest]
        public IEnumerator TileOnlyDeferred_KeepsMSAA_FallsBackToForward()
        {
            // Tile-only Deferred doesn't support MSAA, but must still request it so the renderer falls back to Forward instead of silently dropping MSAA.
            m_RendererData.renderingMode = RenderingMode.Deferred;
            m_RendererData.tileOnlyMode = true;
            try
            {
                yield return TickFrames(k_SettleFrames);
                var renderer = m_Asset.GetRenderer(0) as UniversalRenderer;
                if (renderer == null || !renderer.useTileOnlyMode)
                    Assert.Ignore("Tile-only mode is not active on this platform.");

                AssertDisplayMSAA(k_AssetMsaaSampleCount, checkEyeTexture: false);
                Assert.AreEqual(RenderingMode.Forward, m_ProbePass.renderingModeActual, "Tile-only Deferred with MSAA should fall back to Forward.");
            }
            finally
            {
                m_RendererData.renderingMode = RenderingMode.Forward;
                m_RendererData.tileOnlyMode = false;
            }
        }

        void AssertDisplayMSAA(int expectedSamples, bool checkEyeTexture)
        {
            Assert.IsTrue(m_ProbePass.xrEnabled, "The test camera did not render as an XR camera.");
            Assert.AreEqual(!m_Intermediate, m_ProbePass.activeTargetIsBackbuffer,
                $"The test camera should render {(m_Intermediate ? "through an intermediate" : "directly to the eye textures")}.");
            Assert.AreEqual(expectedSamples, (int)XRSystem.GetDisplayMSAASamples(), "Unexpected XR display MSAA request.");
            if (checkEyeTexture)
                Assert.AreEqual(expectedSamples, m_ProbePass.backbufferMsaaSamples, "The eye texture URP rendered to has an unexpected MSAA sample count.");
        }

        static IEnumerator TickFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        class ForceIntermediatePass : ScriptableRenderPass
        {
            public ForceIntermediatePass()
            {
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
            }
        }

        // Records the last render of the test camera: whether it rendered as XR, whether directly to the eye
        // textures, and the MSAA sample count of the eye texture URP imported as the back buffer.
        class ProbePass : ScriptableRenderPass
        {
            public bool xrEnabled;
            public bool activeTargetIsBackbuffer;
            public int backbufferMsaaSamples;
            // Read while rendering: outside of it, the renderer can't see the XR display MSAA.
            public RenderingMode renderingModeActual;

            public ProbePass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();
                xrEnabled = cameraData.xr.enabled;
                activeTargetIsBackbuffer = resourceData.isActiveTargetBackBuffer;
                backbufferMsaaSamples = renderGraph.GetRenderTargetInfo(resourceData.backBufferColor).msaaSamples;
                if (cameraData.renderer is UniversalRenderer universalRenderer)
                    renderingModeActual = universalRenderer.renderingModeActual;
            }
        }
    }
}
