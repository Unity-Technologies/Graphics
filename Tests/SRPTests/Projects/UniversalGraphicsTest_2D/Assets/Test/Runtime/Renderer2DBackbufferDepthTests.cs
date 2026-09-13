using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

// Verifies what the 2D renderer actually puts in the frame's resources when depth usage is disallowed: the
// back buffer depth must not be imported, and the rest of the frame must still record without errors (the
// overlay UI pass in particular reads UniversalResourceData.backBufferDepth and has to cope with it being
// invalid).
//
// A back buffer with no depth/stencil is not something a test can arrange - it is decided when the surface is
// created - so the disallowed case is driven here through the renderer's own "Depth/Stencil Buffer" setting,
// which reaches the exact same branch in Renderer2D.CreateRenderGraphCameraRenderTargets. The back buffer's own
// contribution to the decision is covered by Renderer2DDepthUsageTests.
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

        Assert.Greater(urpAsset.rendererDataList.Length, k_Renderer2DIndex, "The render pipeline asset no longer has a renderer at the 2D renderer index. Update this test to match.");
        m_RendererData = urpAsset.rendererDataList[k_Renderer2DIndex] as Renderer2DData;
        Assert.NotNull(m_RendererData, $"Renderer {k_Renderer2DIndex} of the render pipeline asset is not a 2D renderer. Update this test to match.");
        m_PreviousUseDepthStencilBuffer = m_RendererData.useDepthStencilBuffer;

        // Frame isolation: only the test camera should render while the probe is installed.
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
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
    public IEnumerator BackbufferDepthIsNotImportedWhenDepthUsageIsDisallowed()
    {
        Renderer2DDepthUsageTests.SetUseDepthStencilBuffer(m_RendererData, false);

        yield return RecordFrame();

        Assert.IsFalse(m_ProbePass.backBufferDepthIsValid, "The 2D renderer imported the back buffer depth even though depth usage is disallowed.");
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
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            recorded = true;
            backBufferDepthIsValid = resourceData.backBufferDepth.IsValid();
        }
    }
}
