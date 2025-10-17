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
