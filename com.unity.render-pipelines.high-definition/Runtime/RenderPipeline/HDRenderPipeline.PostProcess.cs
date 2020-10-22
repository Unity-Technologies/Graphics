using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class AfterPostProcessPassData
        {
            public PostProcessParameters    parameters;
            public TextureHandle            afterPostProcessBuffer;
            public TextureHandle            depthStencilBuffer;
            public RendererListHandle       opaqueAfterPostprocessRL;
            public RendererListHandle       transparentAfterPostprocessRL;
        }

        TextureHandle RenderPostProcess(    RenderGraph     renderGraph,
                                            PrepassOutput   prepassOutput,
                                            TextureHandle   inputColor,
                                            TextureHandle   backBuffer,
                                            CullingResults  cullResults,
                                            HDCamera        hdCamera)
        {
            PostProcessParameters parameters = PreparePostProcess(cullResults, hdCamera);

            TextureHandle afterPostProcessBuffer = renderGraph.defaultResources.blackTextureXR;
            TextureHandle dest = HDUtils.PostProcessIsFinalPass(parameters.hdCamera) ? backBuffer : renderGraph.CreateTexture(
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
                        passData.depthStencilBuffer = builder.UseDepthBuffer(prepassOutput.resolvedDepthBuffer, DepthAccess.ReadWrite);
                    passData.opaqueAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.opaqueAfterPPDesc));
                    passData.transparentAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.transparentAfterPPDesc));

                    builder.SetRenderFunc(
                    (AfterPostProcessPassData data, RenderGraphContext ctx) =>
                    {
                        RenderAfterPostProcess(data.parameters, data.opaqueAfterPostprocessRL, data.transparentAfterPostprocessRL, ctx.renderContext, ctx.cmd);
                    });

                    afterPostProcessBuffer = passData.afterPostProcessBuffer;
                }
            }

            var motionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors) ? prepassOutput.resolvedMotionVectorsBuffer : renderGraph.defaultResources.blackTextureXR;
            m_PostProcessSystem.Render(
                renderGraph,
                parameters.hdCamera,
                parameters.blueNoise,
                inputColor,
                afterPostProcessBuffer,
                prepassOutput.resolvedDepthBuffer,
                prepassOutput.depthPyramidTexture,
                prepassOutput.resolvedNormalBuffer,
                motionVectors,
                dest,
                parameters.flipYInPostProcess
            );

            return dest;
        }
    }
}
