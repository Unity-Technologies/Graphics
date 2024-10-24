using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This pass performs a blit operation with a Material. The input texture is set by the Renderer Feature.
public class DepthBlitEdgePass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthBlitEdgePass");
    private RTHandle m_DepthHandle; // The RTHandle of the depth texture, set by the Renderer Feature, only used in the Compatibility mode (non-RenderGraph path)
    private Material m_Material;

    public DepthBlitEdgePass(Material mat, RenderPassEvent evt)
    {
        renderPassEvent = evt;
        m_Material = mat;
    }

    public void SetRTHandle(ref RTHandle depthHandle)
    {
        m_DepthHandle = depthHandle;
    }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

    // Unity calls the Execute method in the Compatibility mode
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_DepthHandle == null)
            return;

        RTHandle destination = cameraData.renderer.cameraColorTargetHandle;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blitter.BlitCameraTexture(cmd, m_DepthHandle, destination, m_Material, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

#pragma warning restore 618, 672

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        DepthBlitFeature.TexRefData texRefData = frameData.Get<DepthBlitFeature.TexRefData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;
        
        // Set the depthTextureHandle as a texture resource for this render graph instance
        TextureHandle source = texRefData.depthTextureHandle;

        // Set camera color as a texture resource for this render graph instance
        TextureHandle destination = resourceData.activeColorTexture;

        if (!source.IsValid() || !destination.IsValid())
            return;
        
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_Material, 0);
        renderGraph.AddBlitPass(para, "DepthBlitEdgePass");
    }
}
