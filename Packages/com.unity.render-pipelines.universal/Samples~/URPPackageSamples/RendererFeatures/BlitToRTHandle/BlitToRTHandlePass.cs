using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This pass creates an RTHandle and blits the camera color to it after rendering transparent objects.
// The RTHandle is then set as a global texture, which is available to shaders in the scene. The RTHandle is preserved in all frames while the renderer feature is running to create a recursive rendering effect.
public class BlitToRTHandlePass : ScriptableRenderPass
{
    private class PassData
    {
    }
    
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("BlitToRTHandle_CopyColor");
    private RTHandle m_InputHandle;
    private RTHandle m_OutputHandle;
    private const string k_OutputName = "_CopyColorTexture";
    private static readonly int m_OutputId = Shader.PropertyToID(k_OutputName);
    private Material m_Material;

    public BlitToRTHandlePass(RenderPassEvent evt, Material mat)
    {
        renderPassEvent = evt;
        m_Material = mat;
    }

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Create the custom RTHandle
        var desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );
        
        // Make the output texture available for the shaders in the scene.
        // In this sample the output texture is used recursively by the subsequent frames, so it must stay in memory while the renderer feature is running.
        // A TextureHandle object is discarded after each frame, that's why we cannot bind it as a global texture using the RenderGraph API (builder.SetGlobalTextureAfterPass).
        // Instead, we bind the RTHandle as a global texture using the shader API, because the RTHandle is not managed by the render graph system.
        Shader.SetGlobalTexture(m_OutputId, m_OutputHandle);

        // Set camera color as a texture resource for this render graph instance
        TextureHandle source = resourceData.activeColorTexture;

        // Set RTHandle as a texture resource for this render graph instance
        TextureHandle destination = renderGraph.ImportTexture(m_OutputHandle);
        
        if (!source.IsValid() || !destination.IsValid())
            return;
        
        // Blit the input texture to the destination texture
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
        renderGraph.AddBlitPass(para, "BlitToRTHandle_CopyColor");

        // In this example the pass executes after rendering transparent objects, and the transparent objects are reading the destination texture.
        // The following code sets the TextureHandle as the camera color target to avoid visual artefacts.
        resourceData.cameraColor = destination;
    }

    public void Dispose()
    {
        m_InputHandle?.Release();
        m_OutputHandle?.Release();
    }
}
