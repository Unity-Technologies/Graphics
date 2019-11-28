using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class AfterPostProcessPassData
        {
            public PostProcessParameters parameters;
            public RenderGraphMutableResource afterPostProcessBuffer;
            public RenderGraphMutableResource depthStencilBuffer;
            public RenderGraphResource opaqueAfterPostprocessRL;
            public RenderGraphResource transparentAfterPostprocessRL;
        }

        void RenderPostProcess(RenderGraph renderGraph, RenderGraphMutableResource depthBuffer, CullingResults cullResults, HDCamera hdCamera)
        {
            m_SSSColor = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, dimension: TextureXR.dimension, useDynamicScale: true, name: "SSSBuffer");

            // We render AfterPostProcess objects first into a separate buffer that will be composited in the final post process pass
            using (var builder = renderGraph.AddRenderPass<AfterPostProcessPassData>("AfterPostProcess", out var passData, CustomSamplerId.GBuffer.GetSampler()))
            {
                passData.parameters = PreparePostProcess(cullResults, hdCamera);
                passData.afterPostProcessBuffer = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, clearBuffer = true, clearColor = Color.black, name = "OffScreen AfterPostProcess" }), 0);
                if (passData.parameters.useDepthBuffer)
                    passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.opaqueAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.opaqueAfterPPDesc));
                passData.transparentAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.transparentAfterPPDesc));

                builder.SetRenderFunc(
                (AfterPostProcessPassData data, RenderGraphContext ctx) =>
                {
                    RenderAfterPostProcess( data.parameters
                                            , ctx.resources.GetRendererList(data.opaqueAfterPostprocessRL)
                                            , ctx.resources.GetRendererList(data.transparentAfterPostprocessRL)
                                            , ctx.renderContext, ctx.cmd);

                });
            }
        }
    }
}
