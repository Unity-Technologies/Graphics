using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This Renderer Feature enqueues either the DepthBlitCopyDepthPass or the DepthBlitDepthOnlyPass depending on the current platform support.
// DepthBlitCopyPass is a simplified version of URP CopyDepthPass. The pass copies the depth texture to an RTHandle.
// DepthBlitDepthOnlyPass is a simplified version of the URP DepthOnlyPass. The pass renders depth values to an RTHandle.
// The Renderer Feature also enqueues the DepthBlitEdgePass which takes the RTHandle as input to create an effect to visualize depth, and output it to the screen.
public class DepthBlitFeature : ScriptableRendererFeature
{
    public RenderPassEvent evt_Depth = RenderPassEvent.AfterRenderingOpaques;
    public RenderPassEvent evt_Edge = RenderPassEvent.AfterRenderingOpaques;
    public UniversalRendererData rendererDataAsset; // The field for accessing opaqueLayerMask on the renderer asset

    public Shader copyDepthShader;

    public Material m_DepthEdgeMaterial;

    // The RTHandle for storing the depth texture
    private RTHandle m_DepthRTHandle;
    private const string k_DepthRTName = "_MyDepthTexture";

    // This class is for keeping the TextureHandle reference in the frame data so that it can be shared with multiple passes in the render graph system.
    public class TexRefData : ContextItem
    {
        public TextureHandle depthTextureHandle = TextureHandle.nullHandle;

        public override void Reset()
        {
            depthTextureHandle = TextureHandle.nullHandle;
        }
    }

    // The passes for the effect
    private DepthBlitCopyDepthPass m_CopyDepthPass;
    private DepthBlitDepthOnlyPass m_DepthOnlyPass; // DepthOnlyPass is for platforms that run OpenGL ES, which does not support CopyDepth.
    private DepthBlitEdgePass m_DepthEdgePass;

    // Check if the platform supports CopyDepthPass
    private bool CanCopyDepth(ref CameraData cameraData)
    {
        bool msaaEnabledForCamera = cameraData.cameraTargetDescriptor.msaaSamples > 1;
        bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
        bool supportsDepthTarget = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
        bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

        bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;

        // Avoid copying MSAA depth on GLES3 platform to avoid invalid results
        if (IsGLESDevice() && msaaDepthResolve)
            return false;

        return supportsDepthCopy || msaaDepthResolve;
    }

    private bool IsGLESDevice()
    {
        return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,  ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // Create an RTHandle for storing the depth
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        if (CanCopyDepth(ref cameraData))
        {
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
        }
        else
        {
            desc.graphicsFormat = GraphicsFormat.None;
            desc.msaaSamples = 1;
        }
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_DepthRTHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_DepthRTName );

        // Setup passes
        if (CanCopyDepth(ref cameraData))
        {
            if (m_CopyDepthPass == null)
                m_CopyDepthPass = new DepthBlitCopyDepthPass(evt_Depth, copyDepthShader, m_DepthRTHandle);

            renderer.EnqueuePass(m_CopyDepthPass);
        }
        else
        {
            if (m_DepthOnlyPass == null)
                m_DepthOnlyPass = new DepthBlitDepthOnlyPass(evt_Depth, RenderQueueRange.opaque, rendererDataAsset.opaqueLayerMask, m_DepthRTHandle);

            renderer.EnqueuePass(m_DepthOnlyPass);
        }

        // Pass the RTHandle for the DepthEdge effect
        m_DepthEdgePass.SetRTHandle(ref m_DepthRTHandle);
        renderer.EnqueuePass(m_DepthEdgePass);
    }

    public override void Create()
    {
        m_DepthEdgePass = new DepthBlitEdgePass(m_DepthEdgeMaterial, evt_Edge);
    }

    protected override void Dispose(bool disposing)
    {
        m_CopyDepthPass?.Dispose();
        m_DepthRTHandle?.Release();
        m_DepthEdgePass = null;
        m_CopyDepthPass = null;
        m_DepthOnlyPass = null;
    }
}
