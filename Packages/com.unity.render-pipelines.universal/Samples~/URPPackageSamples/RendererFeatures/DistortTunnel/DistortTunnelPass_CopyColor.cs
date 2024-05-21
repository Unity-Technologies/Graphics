using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This pass performs a blit from a source texture to a destination texture set up by the RendererFeature.
public class DistortTunnelPass_CopyColor : ScriptableRenderPass
{
    class PassData
    {
        public TextureHandle source;
        public Vector4 scaleBias;
    }

    private Vector4 m_ScaleBias = new Vector4(1f, 1f, 0f, 0f);
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DistortTunnelPass_CopyColor");
    private RTHandle m_OutputHandle;

    public DistortTunnelPass_CopyColor(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }

    public void SetRTHandles(ref RTHandle dest)
    {
        m_OutputHandle = dest;
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

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blitter.BlitCameraTexture(cmd, source, m_OutputHandle, 0);
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

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("DistortTunnelPass_CopyColor", out var passData))
        {
            // Set camera color as a texture resource for this render graph instance
            TextureHandle source = resourceData.activeColorTexture;

            // Set RTHandle as a texture resource for this render graph instance
            TextureHandle destination = renderGraph.ImportTexture(m_OutputHandle);
            texRefData.copyColorTexHandle = destination;

            if (!source.IsValid() || !destination.IsValid())
                return;

            passData.source = source;
            passData.scaleBias = m_ScaleBias;
            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, data.scaleBias, 0, true);
            });
        }
    }
}
