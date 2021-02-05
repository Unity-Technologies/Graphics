using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDTemporalFilter
    {
        class HistoryValidityPassData
        {
            public HistoryValidityParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;
            public TextureHandle historyDepthTexture;
            public TextureHandle historyNormalTexture;
            public TextureHandle validationBuffer;
        }

        public TextureHandle HistoryValidity(RenderGraph renderGraph, HDCamera hdCamera, HistoryValidityParameters parameters,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<HistoryValidityPassData>("History Validity Evaluation", out var passData, ProfilingSampler.Get(HDProfileId.HistoryValidity)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                passData.parameters = parameters;
                // Input Buffers
                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);

                // History buffers
                passData.historyDepthTexture = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth)));
                passData.historyNormalTexture = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal)));

                // Output buffers
                passData.validationBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8_UNorm, enableRandomWrite = true, name = "ValidationTexture" }));

                builder.SetRenderFunc(
                    (HistoryValidityPassData data, RenderGraphContext ctx) =>
                    {
                        HistoryValidityResources resources = new HistoryValidityResources();
                        resources.depthStencilBuffer = data.depthStencilBuffer;
                        resources.normalBuffer = data.normalBuffer;
                        resources.motionVectorBuffer = data.motionVectorBuffer;
                        resources.historyDepthTexture = data.historyDepthTexture;
                        resources.historyNormalTexture = data.historyNormalTexture;
                        resources.validationBuffer = data.validationBuffer;
                        ExecuteHistoryValidity(ctx.cmd, data.parameters, resources);
                    });
                return passData.validationBuffer;
            }
        }

        class TemporalFilterPassData
        {
            public TemporalFilterParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;
            public TextureHandle velocityBuffer;
            public TextureHandle noisyBuffer;
            public TextureHandle validationBuffer;
            public TextureHandle historyBuffer;
            public TextureHandle outputBuffer;
        }

        public TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera, TemporalFilterParameters tfParameters,
            TextureHandle noisyBuffer, TextureHandle velocityBuffer,
            TextureHandle historyBuffer,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<TemporalFilterPassData>("TemporalDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.TemporalFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                passData.parameters = tfParameters;

                // Prepass Buffers
                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);

                // Effect buffers
                passData.velocityBuffer = builder.ReadTexture(velocityBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.validationBuffer = builder.ReadTexture(historyValidationBuffer);

                // History buffer
                passData.historyBuffer = builder.ReadWriteTexture(historyBuffer);

                // Output buffers
                passData.outputBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporal Filter Output" }));

                builder.SetRenderFunc(
                    (TemporalFilterPassData data, RenderGraphContext ctx) =>
                    {
                        TemporalFilterResources tfResources = new TemporalFilterResources();
                        tfResources.depthStencilBuffer = data.depthStencilBuffer;
                        tfResources.normalBuffer = data.normalBuffer;
                        tfResources.velocityBuffer = data.velocityBuffer;
                        tfResources.motionVectorBuffer = data.motionVectorBuffer;
                        tfResources.noisyBuffer = data.noisyBuffer;
                        tfResources.validationBuffer = data.validationBuffer;
                        tfResources.historyBuffer = data.historyBuffer;
                        tfResources.outputBuffer = data.outputBuffer;
                        DenoiseBuffer(ctx.cmd, data.parameters, tfResources);
                    });
                return passData.outputBuffer;
            }
        }

        internal struct TemporalDenoiserArrayOutputData
        {
            public TextureHandle outputSignal;
            public TextureHandle outputSignalDistance;
        }

        class TemporalFilterArrayPassData
        {
            public TemporalFilterArrayParameters parameters;
            // Prepass buffers
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;

            // Effect Buffers
            public TextureHandle noisyBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle validationBuffer;
            public TextureHandle velocityBuffer;

            // History buffers
            public TextureHandle historyBuffer;
            public TextureHandle validationHistoryBuffer;
            public TextureHandle distanceHistorySignal;

            // Output buffers
            public TextureHandle outputBuffer;
            public TextureHandle outputDistanceSignal;
        }

        public TemporalDenoiserArrayOutputData DenoiseBuffer(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationBuffer,
            TextureHandle noisyBuffer, RTHandle historyBuffer,
            TextureHandle distanceBuffer, RTHandle distanceHistorySignal,
            TextureHandle velocityBuffer,
            RTHandle validationHistoryBuffer,
            int sliceIndex, Vector4 channelMask, Vector4 distanceChannelMask,
            bool distanceBased, bool singleChannel, float historyValidity)
        {
            TemporalDenoiserArrayOutputData resultData = new TemporalDenoiserArrayOutputData();
            using (var builder = renderGraph.AddRenderPass<TemporalFilterArrayPassData>("TemporalDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.TemporalFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                passData.parameters = PrepareTemporalFilterArrayParameters(hdCamera, distanceBased, singleChannel, historyValidity, sliceIndex, channelMask, distanceChannelMask);

                // Input buffers
                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);

                passData.velocityBuffer = builder.ReadTexture(velocityBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.distanceBuffer = distanceBased ? builder.ReadTexture(distanceBuffer) : renderGraph.defaultResources.blackTextureXR;
                passData.validationBuffer = builder.ReadTexture(historyValidationBuffer);

                // History buffers
                passData.historyBuffer = builder.ReadWriteTexture(renderGraph.ImportTexture(historyBuffer));
                passData.validationHistoryBuffer = builder.ReadWriteTexture(renderGraph.ImportTexture(validationHistoryBuffer));
                passData.distanceHistorySignal = distanceBased ? builder.ReadWriteTexture(renderGraph.ImportTexture(distanceHistorySignal)) : renderGraph.defaultResources.blackTextureXR;

                // Output textures
                passData.outputBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporal Filter Output" }));
                passData.outputDistanceSignal = distanceBased ? builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporal Filter Distance output" })) : new TextureHandle();

                builder.SetRenderFunc(
                    (TemporalFilterArrayPassData data, RenderGraphContext ctx) =>
                    {
                        TemporalFilterArrayResources resources = new TemporalFilterArrayResources();
                        resources.depthStencilBuffer = data.depthStencilBuffer;
                        resources.normalBuffer = data.normalBuffer;
                        resources.motionVectorBuffer = data.motionVectorBuffer;
                        resources.velocityBuffer = data.velocityBuffer;
                        resources.noisyBuffer = data.noisyBuffer;
                        resources.distanceBuffer = data.distanceBuffer;
                        resources.validationBuffer = data.validationBuffer;
                        resources.historyBuffer = data.historyBuffer;
                        resources.validationHistoryBuffer = data.validationHistoryBuffer;
                        resources.distanceHistorySignal = data.distanceHistorySignal;
                        resources.outputBuffer = data.outputBuffer;
                        resources.outputDistanceSignal = data.outputDistanceSignal;
                        ExecuteTemporalFilterArray(ctx.cmd, data.parameters, resources);
                    });
                resultData.outputSignal = passData.outputBuffer;
                resultData.outputSignalDistance = passData.outputDistanceSignal;
            }
            return resultData;
        }
    }
}
