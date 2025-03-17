using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Create a Scriptable Renderer Feature that either copies the depth texture to a render texture (RTHandle), or renders depth values to a render texture.
// The Scriptable Renderer Feature then outputs the render texture to the screen, using a material that renders an edge effect.
// For more information about creating scriptable renderer features, refer to https://docs.unity3d.com/Manual/urp/customizing-urp.html
public class DepthBlitFeature : ScriptableRendererFeature
{
    // Set the injection points for the render passes
    public RenderPassEvent evt_Depth = RenderPassEvent.AfterRenderingOpaques;
    public RenderPassEvent evt_Edge = RenderPassEvent.AfterRenderingOpaques;
    
    // Create a property for a Universal Renderer asset that sets the opaque layers to draw.
    public UniversalRendererData rendererDataAsset; 

    // Create a property for the shader that copies the depth texture.
    public Shader copyDepthShader;

    // Create a property for the material that renders the edge effect.
    public Material m_DepthEdgeMaterial;

    // Set the properties of the destination depth texture.
    private const string k_DepthRTName = "_MyDepthTexture";
    private FilterMode m_DepthRTFilterMode = FilterMode.Bilinear;
    private TextureWrapMode m_DepthRTWrapMode = TextureWrapMode.Clamp;

    // Create a class that keeps the reference to the depth texture in the frame data, so multiple passes in the render graph system can share the texture.
    public class TexRefData : ContextItem
    {
        public TextureHandle depthTextureHandle = TextureHandle.nullHandle;

        public override void Reset()
        {
            depthTextureHandle = TextureHandle.nullHandle;
        }
    }

    // Declare the render passes.
    // The script uses DepthOnlyPass for platforms that run OpenGL ES, which doesn't support copying from a depth texture.
    private DepthBlitCopyDepthPass m_CopyDepthPass;
    private DepthBlitDepthOnlyPass m_DepthOnlyPass; 
    private DepthBlitEdgePass m_DepthEdgePass;

    // Check if the platform supports copying from a depth texture.
    private bool CanCopyDepth(ref CameraData cameraData)
    {
        bool msaaEnabledForCamera = cameraData.cameraTargetDescriptor.msaaSamples > 1;
        bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
        bool supportsDepthTarget = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
        bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

        bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;

        // Avoid copying MSAA depth on GLES3 platforms to avoid invalid results
        if (IsGLESDevice() && msaaDepthResolve)
            return false;

        return supportsDepthCopy || msaaDepthResolve;
    }

    private bool IsGLESDevice()
    {
        return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
    }

    // Override the AddRenderPasses method to inject passes into the renderer. Unity calls AddRenderPasses once per camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Skip rendering if the camera is not a game camera.
        var cameraData = renderingData.cameraData;
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // Set up a RenderTextureDescriptor with the properties of the depth texture.
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
        
        // Create the DepthBlitCopyDepthPass or DepthBlitDepthOnlyPass render pass, and inject it into the renderer.
        RTHandle depthRTHandle;
        if (CanCopyDepth(ref cameraData))
        {
            if (m_CopyDepthPass == null)
                // Create a new instance of a render pass that copies the depth texture to the new render texture.
                // This render pass is a simplified version of CopyDepthPass in URP.
                m_CopyDepthPass = new DepthBlitCopyDepthPass(evt_Depth, copyDepthShader, 
                    desc, m_DepthRTFilterMode, m_DepthRTWrapMode, name: k_DepthRTName);

            renderer.EnqueuePass(m_CopyDepthPass);
            depthRTHandle = m_CopyDepthPass.depthRT;
        }
        else
        {
            if (m_DepthOnlyPass == null)
                // Create a new instance of a render pass that renders depth values to the new render texture.
                // This render pass is a simplified version of DepthOnlyPass in URP.
                m_DepthOnlyPass = new DepthBlitDepthOnlyPass(evt_Depth, RenderQueueRange.opaque, rendererDataAsset.opaqueLayerMask,  
                    desc, m_DepthRTFilterMode, m_DepthRTWrapMode, name: k_DepthRTName);

            renderer.EnqueuePass(m_DepthOnlyPass);
            depthRTHandle = m_DepthOnlyPass.depthRT;
        }

        // Pass the render texture to the edge effect render pass, and inject the render pass into the renderer.
        m_DepthEdgePass.SetRTHandle(ref depthRTHandle);
        renderer.EnqueuePass(m_DepthEdgePass);
    }

    public override void Create()
    {
        // Create a new instance of the render pass that renders the edge effect.
        m_DepthEdgePass = new DepthBlitEdgePass(m_DepthEdgeMaterial, evt_Edge);
    }

    // Free the resources the render passes use.
    protected override void Dispose(bool disposing)
    {
        m_CopyDepthPass?.Dispose();
        m_DepthOnlyPass?.Dispose();
        m_DepthEdgePass = null;
        m_CopyDepthPass = null;
        m_DepthOnlyPass = null;
    }
}
