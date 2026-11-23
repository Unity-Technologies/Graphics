using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

// Verifies what the 2D renderer actually puts in the frame's resources when depth usage is disallowed: the
// back buffer depth is not imported, except on OpenGL when the 2D passes draw straight into a back buffer that
// has one (there it stays bound with the back buffer color whether a pass asks for it or not, so it must be
// cleared, see UUM-159093), and the rest of the frame must still record without errors (the overlay UI pass in
// particular reads UniversalResourceData.backBufferDepth and has to cope with it being invalid).
//
// A back buffer with no depth/stencil is not something a test can arrange - it is decided when the surface is
// created - so the disallowed case is driven here through the renderer's own "Depth/Stencil Buffer" setting,
// which reaches the exact same branch in Renderer2D.CreateRenderGraphCameraRenderTargets. The back buffer's own
// contribution to the decision is covered by Renderer2DDepthUsageTests.
//
// All of this is render graph territory: this project also runs its graphics tests in Compatibility Mode, where
// the pipeline builds the frame through the legacy Setup/Execute path and none of these resources exist, so the
// fixture skips itself there.
[TestFixture]
class Renderer2DBackbufferDepthTests
{
    const int k_Renderer2DIndex = 2;    // 2D Renderer. See the list of Renderers in CommonAssets/UniversalRPAsset.

    readonly List<Camera> m_DisabledCameras = new List<Camera>();
    readonly BackbufferDepthProbePass m_ProbePass = new BackbufferDepthProbePass();

    GameObject m_CameraObj;
    Camera m_Camera;
    UniversalAdditionalCameraData m_CameraData;
    Renderer2DData m_RendererData;
    bool m_PreviousUseDepthStencilBuffer;

    [SetUp]
    public void SetUp()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
        {
            Assert.Ignore("These tests only run when URP is the active render pipeline.");
            return;
        }

        // Under XR the renderer imports the XR-owned render target rather than the back buffer, so there is
        // nothing to assert about the back buffer depth here.
        if (XRGraphicsAutomatedTests.enabled)
        {
            Assert.Ignore("XR is active; the 2D renderer does not import the back buffer depth in that case.");
            return;
        }

        // In Compatibility Mode the pipeline runs the legacy Setup/Execute path instead of
        // Renderer2D.CreateRenderGraphCameraRenderTargets, so the frame has no UniversalResourceData to probe and
        // nothing here to assert. useRenderGraph is the flag the pipeline actually dispatches on: it is cached when
        // the pipeline is created, so it can differ from RenderGraphSettings.enableRenderCompatibilityMode once
        // RenderGraphGraphicsAutomatedTests.enabled has been flipped without recreating the pipeline - which is what
        // the graphics test fixtures in this project do.
        if (!UniversalRenderPipeline.useRenderGraph)
        {
            Assert.Ignore("Compatibility Mode (Render Graph disabled) is active; the 2D renderer does not build the frame's render graph resources.");
            return;
        }

        Assert.Greater(urpAsset.rendererDataList.Length, k_Renderer2DIndex, "The render pipeline asset no longer has a renderer at the 2D renderer index. Update this test to match.");
        m_RendererData = urpAsset.rendererDataList[k_Renderer2DIndex] as Renderer2DData;
        Assert.NotNull(m_RendererData, $"Renderer {k_Renderer2DIndex} of the render pipeline asset is not a 2D renderer. Update this test to match.");
        m_PreviousUseDepthStencilBuffer = m_RendererData.useDepthStencilBuffer;

        // Frame isolation: only the test camera should render while the probe is installed.
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!camera.enabled)
                continue;

            camera.enabled = false;
            m_DisabledCameras.Add(camera);
        }

        m_CameraObj = new GameObject("Backbuffer Depth Test Camera");
        m_Camera = m_CameraObj.AddComponent<Camera>();
        m_CameraData = m_CameraObj.AddComponent<UniversalAdditionalCameraData>();

        // Render straight to the back buffer: no target texture, no stack and no post-processing, so the
        // renderer does not create intermediate targets.
        m_Camera.targetTexture = null;
        m_Camera.allowHDR = false;
        m_CameraData.SetRenderer(k_Renderer2DIndex);
        m_CameraData.renderType = CameraRenderType.Base;
        m_CameraData.renderPostProcessing = false;

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    [TearDown]
    public void TearDown()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        m_ProbePass.requiresIntermediateTexture = false;

        if (m_RendererData != null)
            Renderer2DDepthUsageTests.SetUseDepthStencilBuffer(m_RendererData, m_PreviousUseDepthStencilBuffer);

        if (m_CameraObj != null)
            Object.DestroyImmediate(m_CameraObj);

        foreach (var camera in m_DisabledCameras)
        {
            if (camera != null)
                camera.enabled = true;
        }
        m_DisabledCameras.Clear();
    }

    [UnityTest]
    public IEnumerator BackbufferDepthIsImportedWhenDepthUsageIsAllowed()
    {
        Renderer2DDepthUsageTests.SetUseDepthStencilBuffer(m_RendererData, true);

        yield return RecordFrame();

        // With the renderer setting on, the import follows the back buffer: there is nothing to import on a
        // build where PlayerSettings.disableDepthAndStencilBuffers left it without a depth/stencil buffer.
        Assert.AreEqual(Graphics.backBufferHasDepthStencil, m_ProbePass.backBufferDepthIsValid,
            "The imported back buffer depth did not match whether the back buffer has a depth/stencil buffer.");
    }

    [UnityTest]
    public IEnumerator BackbufferDepthIsImportedOnlyOnOpenGLWhenDepthUsageIsDisallowed()
    {
        Renderer2DDepthUsageTests.SetUseDepthStencilBuffer(m_RendererData, false);

        yield return RecordFrame();
        IgnoreIfNotRenderingStraightToTheBackbuffer();

        // On OpenGL the renderer setting must not drop the back buffer depth: left unimported it is never cleared,
        // and draws into the back buffer test against undefined contents (UUM-159093). Elsewhere it is not imported.
        Assert.AreEqual(BackbufferDepthStaysBound(), m_ProbePass.backBufferDepthIsValid,
            "The imported back buffer depth did not match whether the graphics API keeps it bound with the back buffer color.");
    }

    [UnityTest]
    public IEnumerator ActiveDepthIsBackbufferDepthOnlyOnOpenGLWhenDepthUsageIsDisallowed()
    {
        Renderer2DDepthUsageTests.SetUseDepthStencilBuffer(m_RendererData, false);

        yield return RecordFrame();
        IgnoreIfNotRenderingStraightToTheBackbuffer();

        // The 2D passes attach the active depth, which is what gets the back buffer depth cleared on first use
        Assert.AreEqual(BackbufferDepthStaysBound(), m_ProbePass.activeDepthIsBackBufferDepth,
            "The active depth did not match the back buffer depth when rendering straight to the back buffer.");
    }

    [UnityTest]
    public IEnumerator NoActiveDepthWithIntermediateColorWhenDepthUsageIsDisallowed()
    {
        Renderer2DDepthUsageTests.SetUseDepthStencilBuffer(m_RendererData, false);
        m_ProbePass.requiresIntermediateTexture = true;

        yield return RecordFrame();

        // The intermediate color target must not be paired with the back buffer depth, and the back buffer depth is
        // not imported at all since no 2D pass would attach it and get it cleared
        Assert.IsFalse(m_ProbePass.activeDepthIsValid, "An active depth was set for the intermediate color target even though depth usage is disallowed.");
        Assert.IsFalse(m_ProbePass.backBufferDepthIsValid, "The back buffer depth was imported with an intermediate color target even though depth usage is disallowed.");
    }

    [UnityTest]
    public IEnumerator RenderingSucceedsWhenDepthUsageIsDisallowed()
    {
        Renderer2DDepthUsageTests.SetUseDepthStencilBuffer(m_RendererData, false);

        // Passes that read the back buffer depth - the overlay UI pass above all - must skip themselves rather
        // than record against an invalid handle. Any error or exception they log fails this test.
        yield return RecordFrame();
        yield return null;
    }

    // OpenGL binds the default framebuffer's depth with its color, so the renderer imports it to get it cleared.
    static bool BackbufferDepthStaysBound()
    {
        var deviceType = SystemInfo.graphicsDeviceType;
        bool isOpenGL = deviceType == GraphicsDeviceType.OpenGLES3 || deviceType == GraphicsDeviceType.OpenGLCore;
        return isOpenGL && Graphics.backBufferHasDepthStencil;
    }

    // The OpenGL import only applies without an intermediate color target, which the setup cannot rule out everywhere
    // (e.g. a non-sRGB back buffer in Linear color space needs one for the sRGB conversion)
    void IgnoreIfNotRenderingStraightToTheBackbuffer()
    {
        if (BackbufferDepthStaysBound() && !m_ProbePass.activeColorIsBackBuffer)
            Assert.Ignore("The 2D renderer created an intermediate color target, so it does not render straight to the back buffer.");
    }

    // Renders frames until the probe pass has recorded one with the current settings.
    IEnumerator RecordFrame()
    {
        // The settings change lands on the next frame the renderer records, so drop the in-flight one first.
        yield return null;

        m_ProbePass.Reset();
        yield return null;

        Assert.IsTrue(m_ProbePass.recorded, "The probe pass never recorded; the 2D renderer did not render the test camera.");
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != m_Camera)
            return;

        m_CameraData.scriptableRenderer.EnqueuePass(m_ProbePass);
    }

    // Reads the frame's back buffer depth handle at record time; it adds no render pass of its own.
    class BackbufferDepthProbePass : ScriptableRenderPass
    {
        public bool recorded;
        public bool backBufferDepthIsValid;
        public bool activeDepthIsValid;
        public bool activeDepthIsBackBufferDepth;
        public bool activeColorIsBackBuffer;

        public BackbufferDepthProbePass()
        {
            // The 2D renderer maps any event up to BeforeRenderingTransparents onto
            // RenderPassEvent2D.BeforeRendering, which it records right after CreateResources - so the probe
            // sees the back buffer depth as CreateResources left it.
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public void Reset()
        {
            recorded = false;
            backBufferDepthIsValid = false;
            activeDepthIsValid = false;
            activeDepthIsBackBufferDepth = false;
            activeColorIsBackBuffer = false;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            recorded = true;
            backBufferDepthIsValid = resourceData.backBufferDepth.IsValid();
            activeDepthIsValid = resourceData.activeDepthTexture.IsValid();
            // TextureHandle has no equality operator on this branch, so check which target the active depth resolves to
            activeDepthIsBackBufferDepth = activeDepthIsValid && resourceData.activeDepthID == UniversalResourceData.ActiveID.BackBuffer;
            activeColorIsBackBuffer = resourceData.isActiveTargetBackBuffer;
        }
    }
}
