using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDTemporalFilter
    {
        class TemporalFilterPassData
        {
            public TemporalFilterParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;
            public TextureHandle velocityBuffer;
            public TextureHandle historyDepthTexture;
            public TextureHandle historyNormalTexture;
            public TextureHandle noisyBuffer;
            public TextureHandle validationBuffer;
            public TextureHandle historyBuffer;
            public TextureHandle outputBuffer;
        }

        public TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera, TemporalFilterParameters tfParameters, TextureHandle noisyBuffer, TextureHandle historyBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<TemporalFilterPassData>("TemporalDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.TemporalFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                passData.parameters = tfParameters;
                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);
                passData.velocityBuffer = renderGraph.defaultResources.blackTextureXR;
                passData.historyDepthTexture = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth));
                passData.historyNormalTexture = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal));
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.validationBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8_UNorm, enableRandomWrite = true, name = "ValidationTexture" });
                passData.historyBuffer = builder.ReadTexture(builder.WriteTexture(historyBuffer));
                passData.outputBuffer = builder.ReadTexture(builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporal Filter Output" })));

                builder.SetRenderFunc(
                (TemporalFilterPassData data, RenderGraphContext ctx) =>
                {
                    TemporalFilterResources tfResources = new TemporalFilterResources();
                    tfResources.depthStencilBuffer = data.depthStencilBuffer;
                    tfResources.normalBuffer = data.normalBuffer;
                    tfResources.velocityBuffer = data.velocityBuffer;
                    tfResources.motionVectorBuffer = data.motionVectorBuffer;
                    tfResources.historyDepthTexture = data.historyDepthTexture;
                    tfResources.historyNormalTexture = data.historyNormalTexture;
                    tfResources.noisyBuffer = data.noisyBuffer;
                    tfResources.validationBuffer = data.validationBuffer;
                    tfResources.historyBuffer = data.historyBuffer;
                    tfResources.outputBuffer = data.outputBuffer;
                    DenoiseBuffer(ctx.cmd, tfParameters, tfResources);
                });
                return passData.outputBuffer;
            }
        }
    }
}
