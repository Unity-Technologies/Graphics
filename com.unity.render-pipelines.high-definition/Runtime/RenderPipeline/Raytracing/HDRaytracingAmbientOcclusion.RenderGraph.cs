using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRaytracingAmbientOcclusion
    {
        public TextureHandle RenderRTAO(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle rayCountTexture, int frameCount, ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            TextureHandle result;

            if (m_RenderPipeline.GetRayTracingState())
            {
                // Trace the signal
                AmbientOcclusionTraceParameters aoParameters = PrepareAmbientOcclusionTraceParameters(hdCamera, shaderVariablesRaytracing);
                result = TraceAO(renderGraph, aoParameters, depthPyramid, normalBuffer, rayCountTexture);

                // Denoise if required
                result = DenoiseAO(renderGraph, hdCamera, result, depthPyramid, normalBuffer, motionVectors);

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
            public TextureHandle depthPyramid;
            public TextureHandle normalBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputTexture;
        }

        TextureHandle TraceAO(RenderGraph renderGraph, in AmbientOcclusionTraceParameters parameters, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddRenderPass<TraceRTAOPassData>("Tracing the rays for RTAO", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingAmbientOcclusion)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.rayCountTexture = builder.ReadTexture(builder.WriteTexture(rayCountTexture));
                // Depending of if we will have to denoise (or not), we need to allocate the final format, or a bigger texture
                passData.outputTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8_UNorm, enableRandomWrite = true, name = "Ray Traced Ambient Occlusion" }));

                builder.SetRenderFunc(
                (TraceRTAOPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    AmbientOcclusionTraceResources aotResources = new AmbientOcclusionTraceResources();
                    aotResources.depthStencilBuffer = data.depthPyramid;
                    aotResources.normalBuffer = data.normalBuffer;
                    aotResources.rayCountTexture = data.rayCountTexture;
                    aotResources.outputTexture = data.outputTexture;

                    TraceAO(ctx.cmd, data.parameters, aotResources);
                });

                return passData.outputTexture;
            }
        }

        TextureHandle DenoiseAO(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rayTracedAO, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer)
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
                TextureHandle denoisedRTAO = temporalFilter.Denoise(renderGraph, hdCamera, tfParameters, rayTracedAO, historyBuffer, depthPyramid, normalBuffer, motionVectorBuffer);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser diffuseDenoiser = m_RenderPipeline.GetDiffuseDenoiser();
                DiffuseDenoiserParameters ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, true, aoSettings.denoiserRadius, false, false);
                rayTracedAO = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTAO, depthPyramid, normalBuffer, rayTracedAO);

                return rayTracedAO;
            }
            else
                return rayTracedAO;
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
                passData.outputTexture = builder.ReadTexture(builder.WriteTexture(aoTexture));

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
