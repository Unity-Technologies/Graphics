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

        RenderGraphMutableResource RenderPostProcess(   RenderGraph                 renderGraph,
                                                        RenderGraphResource         inputColor,
                                                        RenderGraphMutableResource  depthBuffer,
                                                        RenderGraphMutableResource  backBuffer,
                                                        CullingResults              cullResults,
                                                        HDCamera                    hdCamera)
        {
            PostProcessParameters parameters = PreparePostProcess(cullResults, hdCamera);

            RenderGraphResource afterPostProcessBuffer = renderGraph.ImportTexture(TextureXR.GetBlackTexture());
            RenderGraphMutableResource dest = HDUtils.PostProcessIsFinalPass(parameters.hdCamera) ? backBuffer : renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one, true, true) { colorFormat = GetColorBufferFormat(), name = "Intermediate Postprocess buffer" });

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
            {
                // We render AfterPostProcess objects first into a separate buffer that will be composited in the final post process pass
                using (var builder = renderGraph.AddRenderPass<AfterPostProcessPassData>("After Post-Process", out var passData, ProfilingSampler.Get(HDProfileId.AfterPostProcessing)))
                {
                    passData.parameters = parameters;
                    passData.afterPostProcessBuffer = builder.UseColorBuffer(renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, clearBuffer = true, clearColor = Color.black, name = "OffScreen AfterPostProcess" }), 0);
                    if (passData.parameters.useDepthBuffer)
                        passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                    passData.opaqueAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.opaqueAfterPPDesc));
                    passData.transparentAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.transparentAfterPPDesc));

                    builder.SetRenderFunc(
                    (AfterPostProcessPassData data, RenderGraphContext ctx) =>
                    {
                        RenderAfterPostProcess(data.parameters
                                                , ctx.resources.GetRendererList(data.opaqueAfterPostprocessRL)
                                                , ctx.resources.GetRendererList(data.transparentAfterPostprocessRL)
                                                , ctx.renderContext, ctx.cmd);

                    });

                    afterPostProcessBuffer = passData.afterPostProcessBuffer;
                }
            }

            m_PostProcessSystem.Render(
                renderGraph,
                parameters.hdCamera,
                parameters.blueNoise,
                inputColor,
                afterPostProcessBuffer,
                depthBuffer,
                dest,
                parameters.flipYInPostProcess
            );

            return dest;
        }
    }
}
