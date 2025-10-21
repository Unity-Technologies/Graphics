using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This pass takes the outputs from the CopyColor pass and the Tunnel pass as inputs and blits them to the screen with a Material that creates the distortion effect.
public class DistortTunnelPass_Distort : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DistortTunnelPass_Distort");
    private Material m_Material;

    // The RTHandles to be used as input and output in the Compatibility mode (non-RenderGraph path)
    private RTHandle m_DistortTunnelTexHandle;
    private RTHandle m_OutputHandle;

    public DistortTunnelPass_Distort(Material mat, RenderPassEvent evt)
    {
        renderPassEvent = evt;
        m_Material = mat;
    }

    public void SetRTHandles(ref RTHandle srcRT)
    {
        m_DistortTunnelTexHandle = srcRT;
    }

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        DistortTunnelRendererFeature.TexRefData texRefData = frameData.Get<DistortTunnelRendererFeature.TexRefData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;
        
        // Set the input and output textures for the AddBlitPass method.
        TextureHandle destination = resourceData.activeColorTexture;
        TextureHandle source = texRefData.distortTunnelTexHandle;

        if (!source.IsValid() || !destination.IsValid())
            return;
        
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_Material, 0);
        renderGraph.AddBlitPass(para, "DistortTunnelPass_Distort");

    }
}
