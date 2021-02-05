using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRaytracingAmbientOcclusion
    {
        struct TraceAmbientOcclusionResult
        {
            public TextureHandle signalBuffer;
            public TextureHandle velocityBuffer;
        }

        public TextureHandle RenderRTAO(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle historyValidationBuffer,
            TextureHandle rayCountTexture, ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            TextureHandle result;

            if (m_RenderPipeline.GetRayTracingState())
            {
                // Evaluate the parameters
                AmbientOcclusionTraceParameters aoParameters = PrepareAmbientOcclusionTraceParameters(hdCamera, shaderVariablesRaytracing);

                // Trace the signal
                TraceAmbientOcclusionResult traceResult = TraceAO(renderGraph, aoParameters, depthBuffer, normalBuffer, rayCountTexture);

                // Denoise if required
                result = DenoiseAO(renderGraph, hdCamera, traceResult, depthBuffer, normalBuffer, motionVectors, historyValidationBuffer);

                // Compose the result to be done
                AmbientOcclusionComposeParameters aoComposeParameters = PrepareAmbientOcclusionComposeParameters(hdCamera, shaderVariablesRaytracing);
                result = ComposeAO(renderGraph, aoComposeParameters, result);
            }
            else
            {
                result = renderGraph.defaultResources.blackTextureXR;
            }
            return result;
        }

        class TraceRTAOPassData
        {
            public AmbientOcclusionTraceParameters parameters;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputTexture;
            public TextureHandle velocityBuffer;
        }

        TraceAmbientOcclusionResult TraceAO(RenderGraph renderGraph, in AmbientOcclusionTraceParameters parameters, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddRenderPass<TraceRTAOPassData>("Tracing the rays for RTAO", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingAmbientOcclusion)))
            {
                TraceAmbientOcclusionResult traceOutput;

                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);
                // Depending of if we will have to denoise (or not), we need to allocate the final format, or a bigger texture
                passData.outputTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8_UNorm, enableRandomWrite = true, name = "Ray Traced Ambient Occlusion" }));
                passData.velocityBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8_SNorm, enableRandomWrite = true, name = "Velocity Buffer" }));

                builder.SetRenderFunc(
                    (TraceRTAOPassData data, RenderGraphContext ctx) =>
                    {
                        // We need to fill the structure that holds the various resources
                        AmbientOcclusionTraceResources aotResources = new AmbientOcclusionTraceResources();
                        aotResources.depthStencilBuffer = data.depthBuffer;
                        aotResources.normalBuffer = data.normalBuffer;
                        aotResources.rayCountTexture = data.rayCountTexture;
                        aotResources.outputTexture = data.outputTexture;
                        aotResources.velocityBuffer = data.velocityBuffer;

                        TraceAO(ctx.cmd, data.parameters, aotResources);
                    });

                traceOutput.signalBuffer = passData.outputTexture;
                traceOutput.velocityBuffer = passData.velocityBuffer;
                return traceOutput;
            }
        }

        TextureHandle DenoiseAO(RenderGraph renderGraph, HDCamera hdCamera,
            TraceAmbientOcclusionResult traceAOResult,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationBuffer)
        {
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            if (aoSettings.denoise)
            {
                // Evaluate the history's validity
                float historyValidity = HDRenderPipeline.EvaluateHistoryValidity(hdCamera);

                // Run the temporal denoiser
                HDTemporalFilter temporalFilter = m_RenderPipeline.GetTemporalFilter();
                TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, true, historyValidity);
                TextureHandle historyBuffer = renderGraph.ImportTexture(RequestAmbientOcclusionHistoryTexture(hdCamera));
                TextureHandle denoisedRTAO = temporalFilter.Denoise(renderGraph, hdCamera, tfParameters, traceAOResult.signalBuffer, traceAOResult.velocityBuffer, historyBuffer, depthBuffer, normalBuffer, motionVectorBuffer, historyValidationBuffer);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser diffuseDenoiser = m_RenderPipeline.GetDiffuseDenoiser();
                DiffuseDenoiserParameters ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, true, aoSettings.denoiserRadius, false, false);
                traceAOResult.signalBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTAO, depthBuffer, normalBuffer, traceAOResult.signalBuffer);

                return traceAOResult.signalBuffer;
            }
            else
                return traceAOResult.signalBuffer;
        }

        class ComposeRTAOPassData
        {
            public AmbientOcclusionComposeParameters parameters;
            public TextureHandle outputTexture;
        }

        TextureHandle ComposeAO(RenderGraph renderGraph, in AmbientOcclusionComposeParameters parameters, TextureHandle aoTexture)
        {
            using (var builder = renderGraph.AddRenderPass<ComposeRTAOPassData>("Composing the result of RTAO", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingComposeAmbientOcclusion)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.outputTexture = builder.ReadWriteTexture(aoTexture);

                builder.SetRenderFunc(
                    (ComposeRTAOPassData data, RenderGraphContext ctx) =>
                    {
                        // We need to fill the structure that holds the various resources
                        ComposeAO(ctx.cmd, data.parameters, data.outputTexture);
                    });

                return passData.outputTexture;
            }
        }
    }
}
