using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

// This Renderer Feature enqueues either the CopyDepthPass or the DepthOnlyPass depending on the current platform support.
// CopyDepthPass copies the depth texture to an RTHandle. DepthOnlyPass renders depth values to an RTHandle.
// The Renderer Feature also enqueues the DepthBlitEdgePass which takes the RTHandle as input to create an effect to visualize depth, and output it to the screen.
public class DepthBlitFeature : ScriptableRendererFeature
{
    public RenderPassEvent evt_Depth = RenderPassEvent.AfterRenderingOpaques;
    public RenderPassEvent evt_Edge = RenderPassEvent.AfterRenderingOpaques;
    public UniversalRendererData rendererDataAsset; // The field for accessing opaqueLayerMask on the renderer asset
    
    public Shader copyDepthShader;
    private Material m_CopyDepthMaterial;
    
    public Material m_DepthEdgeMaterial;
    
    // The RTHandle for storing the depth texture
    private RTHandle m_DepthRTHandle;
    private const string k_DepthRTName = "_MyDepthTexture";
    
    // The passes for the effect
    private CopyDepthPass m_CopyDepthPass;
    private DepthOnlyPass m_DepthOnlyPass; // DepthOnlyPass is for platforms that run OpenGL ES, which does not support CopyDepth.
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
        return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer,  ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        if (CanCopyDepth(ref cameraData))
        {
            if (m_CopyDepthMaterial == null)
                m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(copyDepthShader);
            if (m_CopyDepthPass == null)
                m_CopyDepthPass = new CopyDepthPass(evt_Depth, m_CopyDepthMaterial);
            renderer.EnqueuePass(m_CopyDepthPass);
        }
        else
        {
            if (m_DepthOnlyPass == null)
                m_DepthOnlyPass = new DepthOnlyPass(evt_Depth, RenderQueueRange.opaque, rendererDataAsset.opaqueLayerMask);
            renderer.EnqueuePass(m_DepthOnlyPass);
        }

        renderer.EnqueuePass(m_DepthEdgePass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        // Create an RTHandle for storing the depth
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.graphicsFormat = GraphicsFormat.None;
        desc.msaaSamples = 1;
        RenderingUtils.ReAllocateIfNeeded(ref m_DepthRTHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_DepthRTName );
        
        // Setup source and destination RTHandles for the CopyDepthPass
        if (CanCopyDepth(ref cameraData))
            m_CopyDepthPass.Setup(renderer.cameraDepthTargetHandle, m_DepthRTHandle);
        else
            m_DepthOnlyPass.Setup(desc, m_DepthRTHandle);
        
        // Pass the RTHandle for the DepthEdge effect
        m_DepthEdgePass.SetRTHandle(ref m_DepthRTHandle, renderer.cameraColorTargetHandle);
    }

    public override void Create()
    {
        m_DepthEdgePass = new DepthBlitEdgePass(m_DepthEdgeMaterial, evt_Edge);
    }

    protected override void Dispose(bool disposing)
    {
        m_DepthRTHandle?.Release();
        CoreUtils.Destroy(m_CopyDepthMaterial);
        m_DepthEdgePass = null;
        m_CopyDepthPass = null;
        m_DepthOnlyPass = null;
    }
}