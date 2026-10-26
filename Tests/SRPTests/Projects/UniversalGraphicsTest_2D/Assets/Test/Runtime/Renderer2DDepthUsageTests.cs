using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Covers Renderer2D.IsDepthUsageAllowed, which decides whether the 2D renderer uses a depth/stencil buffer at
// all. It combines three conditions: the renderer's own "Depth/Stencil Buffer" setting, a 3D render texture
// target (which cannot carry depth/stencil), and whether the back buffer has a depth/stencil buffer.
//
// The renderer setting is driven here by writing Renderer2DData's serialized field. The back buffer's state is
// not something the test can set - it is decided when the surface is created, from
// PlayerSettings.disableDepthAndStencilBuffers on the graphics APIs that act on it - so expectations are
// stated against Graphics.backBufferHasDepthStencil, which is what that condition contributes wherever the
// test happens to run. That keeps every case meaningful in the editor and in player builds alike.
[TestFixture]
class Renderer2DDepthUsageTests
{
    const string k_UseDepthStencilBufferField = "m_UseDepthStencilBuffer";
    const int k_Renderer2DIndex = 2;    // 2D Renderer. See the list of Renderers in CommonAssets/UniversalRPAsset.

    ContextContainer m_FrameData;
    UniversalCameraData m_CameraData;
    Renderer2DData m_RendererData;
    RenderTexture m_TargetTexture;
    bool m_PreviousUseDepthStencilBuffer;

    [SetUp]
    public void SetUp()
    {
        // Use the pipeline asset's 2D renderer data rather than ScriptableObject.CreateInstance: a fresh instance
        // has no light blend styles outside the editor (they are only built by editor-only code), and
        // Renderer2DData.OnEnable dereferences them when URP_COMPATIBILITY_MODE is defined.
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
        {
            Assert.Ignore("These tests only run when URP is the active render pipeline.");
            return;
        }

        Assert.Greater(urpAsset.rendererDataList.Length, k_Renderer2DIndex, "The render pipeline asset no longer has a renderer at the 2D renderer index. Update this test to match.");
        m_RendererData = urpAsset.rendererDataList[k_Renderer2DIndex] as Renderer2DData;
        Assert.NotNull(m_RendererData, $"Renderer {k_Renderer2DIndex} of the render pipeline asset is not a 2D renderer. Update this test to match.");
        m_PreviousUseDepthStencilBuffer = m_RendererData.useDepthStencilBuffer;

        m_FrameData = new ContextContainer();
        m_CameraData = m_FrameData.Create<UniversalCameraData>();
    }

    [TearDown]
    public void TearDown()
    {
        if (m_CameraData != null)
            m_CameraData.targetTexture = null;

        if (m_TargetTexture != null)
        {
            m_TargetTexture.Release();
            Object.DestroyImmediate(m_TargetTexture);
            m_TargetTexture = null;
        }

        // The renderer data belongs to the pipeline asset, so restore it rather than destroy it.
        if (m_RendererData != null)
            SetUseDepthStencilBuffer(m_RendererData, m_PreviousUseDepthStencilBuffer);

        m_FrameData?.Dispose();
    }

    [Test]
    public void IsDepthUsageAllowed_FollowsTheRendererDepthStencilBufferSetting([Values(false, true)] bool useDepthStencilBuffer)
    {
        SetUseDepthStencilBuffer(m_RendererData, useDepthStencilBuffer);

        bool expected = useDepthStencilBuffer && Graphics.backBufferHasDepthStencil;

        Assert.AreEqual(expected, Renderer2D.IsDepthUsageAllowed(m_FrameData, m_RendererData));
    }

    [Test]
    public void IsDepthUsageAllowed_FollowsTheBackBufferDepthStencil()
    {
        // With every other condition asking for depth, the back buffer decides. A build with
        // PlayerSettings.disableDepthAndStencilBuffers enabled on a graphics API that acts on it lands here
        // with no depth/stencil; everywhere else, including the editor, the back buffer keeps one.
        SetUseDepthStencilBuffer(m_RendererData, true);

        Assert.AreEqual(Graphics.backBufferHasDepthStencil, Renderer2D.IsDepthUsageAllowed(m_FrameData, m_RendererData));
    }

    [Test]
    public void IsDepthUsageAllowed_IsNotVetoedBy2DRenderTextureTargets()
    {
        // Only 3D render textures veto depth; a 2D one leaves the decision to the other conditions.
        SetUseDepthStencilBuffer(m_RendererData, true);
        m_CameraData.targetTexture = CreateTargetTexture(TextureDimension.Tex2D);

        Assert.AreEqual(Graphics.backBufferHasDepthStencil, Renderer2D.IsDepthUsageAllowed(m_FrameData, m_RendererData));
    }

    [Test]
    public void IsDepthUsageAllowed_IsNotAllowedFor3DRenderTextureTargets()
    {
        // 3D render textures cannot have a depth/stencil buffer, so this vetoes the decision on its own even
        // with everything else asking for depth.
        SetUseDepthStencilBuffer(m_RendererData, true);
        m_CameraData.targetTexture = CreateTargetTexture(TextureDimension.Tex3D);

        Assert.IsFalse(Renderer2D.IsDepthUsageAllowed(m_FrameData, m_RendererData));
    }

    RenderTexture CreateTargetTexture(TextureDimension dimension)
    {
        var descriptor = new RenderTextureDescriptor(4, 4)
        {
            dimension = dimension,
            volumeDepth = dimension == TextureDimension.Tex3D ? 4 : 1,
            graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            depthStencilFormat = GraphicsFormat.None,
            msaaSamples = 1
        };

        m_TargetTexture = new RenderTexture(descriptor);
        return m_TargetTexture;
    }

    // Renderer2DData.useDepthStencilBuffer is a read-only view of a serialized field, so the test has to write
    // the field. Kept in one place so a rename fails loudly instead of silently skipping the condition.
    internal static void SetUseDepthStencilBuffer(Renderer2DData rendererData, bool value)
    {
        var field = typeof(Renderer2DData).GetField(k_UseDepthStencilBufferField, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{nameof(Renderer2DData)}.{k_UseDepthStencilBufferField} no longer exists. Update this test to match.");

        field.SetValue(rendererData, value);
        Assert.AreEqual(value, rendererData.useDepthStencilBuffer, $"{nameof(Renderer2DData)}.{k_UseDepthStencilBufferField} is no longer what useDepthStencilBuffer returns. Update this test to match.");
    }
}
