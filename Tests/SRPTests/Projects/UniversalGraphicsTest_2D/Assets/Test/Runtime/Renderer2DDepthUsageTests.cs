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

    ContextContainer m_FrameData;
    UniversalCameraData m_CameraData;
    Renderer2DData m_RendererData;
    RenderTexture m_TargetTexture;

    [SetUp]
    public void SetUp()
    {
        m_FrameData = new ContextContainer();
        m_CameraData = m_FrameData.Create<UniversalCameraData>();
        m_RendererData = ScriptableObject.CreateInstance<Renderer2DData>();
    }

    [TearDown]
    public void TearDown()
    {
        m_CameraData.targetTexture = null;

        if (m_TargetTexture != null)
        {
            m_TargetTexture.Release();
            Object.DestroyImmediate(m_TargetTexture);
            m_TargetTexture = null;
        }

        if (m_RendererData != null)
            Object.DestroyImmediate(m_RendererData);

        m_FrameData.Dispose();
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
