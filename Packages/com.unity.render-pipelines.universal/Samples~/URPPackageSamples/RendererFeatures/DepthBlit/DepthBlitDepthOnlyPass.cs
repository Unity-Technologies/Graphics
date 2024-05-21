using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This pass is a simplified version of the URP DepthOnlyPass. This pass renders depth to an RTHandle.
// Unlike the original URP DepthOnlyPass, this example does not use the _CameraDepthTexture texture, and demonstrates how to copy from the depth buffer to a custom RTHandle instead.
public class DepthBlitDepthOnlyPass : ScriptableRenderPass
{
    private const string k_PassName = "DepthBlitDepthOnlyPass";
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_PassName);
    private RTHandle m_DestRT; // The RTHandle for storing the depth texture, set by the Renderer Feature
    FilteringSettings m_FilteringSettings;
    private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthOnly");

    class PassData
    {
        public RendererListHandle rendererList;
    }

    public DepthBlitDepthOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, RTHandle destination)
    {
        renderPassEvent = evt;
        m_DestRT = destination;
        m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
    }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

    // Unity calls the Configure method in the Compatibility mode (non-RenderGraph path)
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        ConfigureTarget(m_DestRT);
    }

    // Unity calls the Execute method in the Compatibility mode
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Setup the RendererList for drawing objects with the shader tag "DepthOnly".
        var sortFlags = cameraData.defaultOpaqueSortFlags;
        var drawSettings = RenderingUtils.CreateDrawingSettings(k_ShaderTagId, ref renderingData, sortFlags);
        drawSettings.perObjectData = PerObjectData.None;
        RendererListParams param = new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);
        param.filteringSettings.batchLayerMask = uint.MaxValue;
        RendererList rendererList = context.CreateRendererList(ref param);

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            cmd.ClearRenderTarget(true,false, Color.black);
            cmd.DrawRendererList(rendererList);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

#pragma warning restore 618, 672

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        DepthBlitFeature.TexRefData texRefData = frameData.GetOrCreate<DepthBlitFeature.TexRefData>();

        // Set the texture resources for this render graph instance.
        TextureHandle dest = renderGraph.ImportTexture(m_DestRT);
        texRefData.depthTextureHandle = dest;

        if(!dest.IsValid())
            return;

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData, m_ProfilingSampler))
        {
            // Setup the RendererList for drawing objects with the shader tag "DepthOnly".
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            var drawSettings = RenderingUtils.CreateDrawingSettings(k_ShaderTagId, renderingData, cameraData, lightData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;
            RendererListParams param = new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);
            param.filteringSettings.batchLayerMask = uint.MaxValue;
            passData.rendererList = renderGraph.CreateRendererList(param);

            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachmentDepth(dest, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(true,false, Color.black);
                context.cmd.DrawRendererList(data.rendererList);
            });
        }
    }
}
