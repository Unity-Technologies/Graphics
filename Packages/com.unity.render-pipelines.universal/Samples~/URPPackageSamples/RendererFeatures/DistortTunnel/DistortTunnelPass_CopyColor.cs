using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This pass performs a blit from a source texture to a destination texture set up by the RendererFeature.
public class DistortTunnelPass_CopyColor : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DistortTunnelPass_CopyColor");
    private RTHandle m_OutputHandle;
    private int m_Slice;
    private Material m_Material;

    public DistortTunnelPass_CopyColor(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }

    public void SetRTHandles(ref RTHandle dest, int slice)
    {
        m_OutputHandle = dest;
        m_Slice = slice;
        
        m_Material = Blitter.GetBlitMaterial(TextureDimension.Tex2DArray);
    }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

    // Unity calls the Configure method in the Compatibility mode (non-RenderGraph path)
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
    {
        ConfigureTarget(m_OutputHandle);
    }

    // Unity calls the Execute method in the Compatibility mode
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        RTHandle source = cameraData.renderer.cameraColorTargetHandle;
        
        Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            CoreUtils.SetRenderTarget(cmd, m_OutputHandle, depthSlice: m_Slice);
            Blitter.BlitTexture(cmd, source, viewportScale, m_Material, 0);
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
        DistortTunnelRendererFeature.TexRefData texRefData = frameData.GetOrCreate<DistortTunnelRendererFeature.TexRefData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;
        
        // Set camera color texture as a texture resource for this render graph instance
        TextureHandle source = resourceData.activeColorTexture;

        // Set the RTHandle as a texture resource for this render graph instance
        TextureHandle destination = renderGraph.ImportTexture(m_OutputHandle);
        texRefData.distortTunnelTexHandle = destination;

        if (!source.IsValid() || !destination.IsValid())
            return;

        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_Material, 0);
        para.destinationSlice = m_Slice;
        renderGraph.AddBlitPass(para, "DistortTunnelPass_CopyColor");
    }
}
