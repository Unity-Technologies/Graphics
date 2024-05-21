using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This pass takes the outputs from the CopyColor pass and the Tunnel pass as inputs and blits them to the screen with a Material that creates the distortion effect.
public class DistortTunnelPass_Distort : ScriptableRenderPass
{
    class PassData
    {
        public Material material;
    }

    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DistortTunnelPass_Distort");
    private Material m_Material;

    // The RTHandles to be used as input and output in the Compatibility mode (non-RenderGraph path)
    private RTHandle m_CopyColorHandle;
    private RTHandle m_TunnelHandle;
    private RTHandle m_OutputHandle;

    public DistortTunnelPass_Distort(Material mat, RenderPassEvent evt)
    {
        renderPassEvent = evt;
        m_Material = mat;
    }

    public void SetRTHandles(ref RTHandle copyColorRT, ref RTHandle tunnelRT)
    {
        m_CopyColorHandle = copyColorRT;
        m_TunnelHandle = tunnelRT;

        if (m_Material == null)
            return;

        m_Material.SetTexture(m_CopyColorHandle.name,m_CopyColorHandle);
        m_Material.SetTexture(m_TunnelHandle.name,m_TunnelHandle);
    }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

    // Unity calls the OnCameraSetup method in the Compatibility mode (non-RenderGraph path)
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        m_OutputHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
        ConfigureTarget(m_OutputHandle);
    }

    // Unity calls the Execute method in the Compatibility mode
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blitter.BlitCameraTexture(cmd, m_OutputHandle, m_OutputHandle, m_Material, 0);
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
        DistortTunnelRendererFeature.TexRefData texRefData = frameData.Get<DistortTunnelRendererFeature.TexRefData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("DistortTunnelPass_Distort", out var passData))
        {
            // Set camera color as a texture resource for this render graph instance
            TextureHandle destination = resourceData.activeColorTexture;

            if (!texRefData.copyColorTexHandle.IsValid() || !texRefData.tunnelTexHandle.IsValid() || !destination.IsValid())
                return;

            passData.material = m_Material;
            builder.UseTexture(texRefData.copyColorTexHandle, AccessFlags.Read);
            builder.UseTexture(texRefData.tunnelTexHandle, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }
    }
}
