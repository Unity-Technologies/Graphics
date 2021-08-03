using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class DirGenRTGIPassData
        {
            public RTIndirectDiffuseDirGenParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle DirGenRTGI(RenderGraph renderGraph, in RTIndirectDiffuseDirGenParameters parameters, TextureHandle depthPyramid, TextureHandle normalBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DirGenRTGIPassData>("Generating the rays for RTGI", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "GI Ray Directions" }));

                builder.SetRenderFunc(
                (DirGenRTGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    RTIndirectDiffuseDirGenResources rtgiDirGenResources = new RTIndirectDiffuseDirGenResources();
                    rtgiDirGenResources.depthStencilBuffer = data.depthStencilBuffer;
                    rtgiDirGenResources.normalBuffer = data.normalBuffer;
                    rtgiDirGenResources.outputBuffer = data.outputBuffer;
                    RTIndirectDiffuseDirGen(ctx.cmd, data.parameters, rtgiDirGenResources);
                });

                return passData.outputBuffer;
            }
        }

        class UpscaleRTGIPassData
        {
            public RTIndirectDiffuseUpscaleParameters parameters;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle indirectDiffuseBuffer;
            public TextureHandle directionBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle UpscaleRTGI(RenderGraph renderGraph, in RTIndirectDiffuseUpscaleParameters parameters,
                                TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle indirectDiffuseBuffer, TextureHandle directionBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<UpscaleRTGIPassData>("Upscale the RTGI result", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseUpscale)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.indirectDiffuseBuffer = builder.ReadTexture(indirectDiffuseBuffer);
                passData.directionBuffer = builder.ReadTexture(directionBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Indirect Diffuse" }));

                builder.SetRenderFunc(
                (UpscaleRTGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    RTIndirectDiffuseUpscaleResources rtgiUpscaleResources = new RTIndirectDiffuseUpscaleResources();
                    rtgiUpscaleResources.depthStencilBuffer = data.depthBuffer;
                    rtgiUpscaleResources.normalBuffer = data.normalBuffer;
                    rtgiUpscaleResources.indirectDiffuseBuffer = data.indirectDiffuseBuffer;
                    rtgiUpscaleResources.directionBuffer = data.directionBuffer;
                    rtgiUpscaleResources.outputBuffer = data.outputBuffer;
                    RTIndirectDiffuseUpscale(ctx.cmd, data.parameters, rtgiUpscaleResources);
                });

                return passData.outputBuffer;
            }
        }

        class AdjustRTGIWeightPassData
        {
            public AdjustRTIDWeightParameters parameters;
            public TextureHandle depthPyramid;
            public TextureHandle stencilBuffer;
            public TextureHandle indirectDiffuseBuffer;
        }

        TextureHandle AdjustRTGIWeight(RenderGraph renderGraph, in AdjustRTIDWeightParameters parameters, TextureHandle indirectDiffuseBuffer, TextureHandle depthPyramid, TextureHandle stencilBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<AdjustRTGIWeightPassData>("Adjust the RTGI weight", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseAdjustWeight)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.indirectDiffuseBuffer = builder.ReadWriteTexture(indirectDiffuseBuffer);

                builder.SetRenderFunc(
                (AdjustRTGIWeightPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    AdjustRTIDWeight(ctx.cmd, data.parameters, data.indirectDiffuseBuffer, data.depthPyramid, data.stencilBuffer);
                });

                return passData.indirectDiffuseBuffer;
            }
        }

        static RTHandle RequestRayTracedIndirectDiffuseHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF,
                ReflectionHistoryBufferAllocatorFunction, 1);
        }

        TextureHandle RenderIndirectDiffusePerformance(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle rayCountTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            // Pointer to the final result
            TextureHandle rtgiResult;

            // Fetch all the settings
            GlobalIllumination settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            RTIndirectDiffuseDirGenParameters rtgiDirGenParameters = PrepareRTIndirectDiffuseDirGenParameters(hdCamera, settings);
            TextureHandle directionBuffer = DirGenRTGI(renderGraph, in rtgiDirGenParameters, depthPyramid, normalBuffer);

            DeferredLightingRTParameters deferredParamters = PrepareIndirectDiffuseDeferredLightingRTParameters(hdCamera);
            TextureHandle lightingBuffer = DeferredLightingRT(renderGraph, in deferredParamters, directionBuffer, depthPyramid, normalBuffer, skyTexture, rayCountTexture);

            RTIndirectDiffuseUpscaleParameters rtgiUpscaleParameters = PrepareRTIndirectDiffuseUpscaleParameters(hdCamera, settings);
            rtgiResult = UpscaleRTGI(renderGraph, in rtgiUpscaleParameters,
                                    depthPyramid, normalBuffer, lightingBuffer, directionBuffer);
            // Denoise if required
            rtgiResult = DenoiseRTGI(renderGraph, hdCamera, rtgiResult, depthPyramid, normalBuffer, motionVectors);

            // Adjust the weight
            AdjustRTIDWeightParameters artidParamters = PrepareAdjustRTIDWeightParametersParameters(hdCamera);
            rtgiResult = AdjustRTGIWeight(renderGraph, in artidParamters, rtgiResult, depthPyramid, stencilBuffer);

            return rtgiResult;
        }

        class TraceQualityRTGIPassData
        {
            public QualityRTIndirectDiffuseParameters parameters;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputBuffer;
        }

        TextureHandle QualityRTGI(RenderGraph renderGraph, in QualityRTIndirectDiffuseParameters parameters, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddRenderPass<TraceQualityRTGIPassData>("Quality RT Indirect Diffuse", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseEvaluation)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced Indirect Diffuse" }));

                builder.SetRenderFunc(
                (TraceQualityRTGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    QualityRTIndirectDiffuseResources rtgiQRenderingResources = new QualityRTIndirectDiffuseResources();
                    rtgiQRenderingResources.depthBuffer = data.depthBuffer;
                    rtgiQRenderingResources.normalBuffer = data.normalBuffer;
                    rtgiQRenderingResources.rayCountTexture = data.rayCountTexture;
                    rtgiQRenderingResources.outputBuffer = data.outputBuffer;
                    RenderQualityRayTracedIndirectDiffuse(ctx.cmd, data.parameters, rtgiQRenderingResources);
                });

                return passData.outputBuffer;
            }
        }

        TextureHandle RenderIndirectDiffuseQuality(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle rayCountTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            // Evaluate the signal
            QualityRTIndirectDiffuseParameters rtgiQRenderingParameters = PrepareQualityRTIndirectDiffuseParameters(hdCamera, settings);
            TextureHandle  rtgiResult = QualityRTGI(renderGraph, in rtgiQRenderingParameters, depthPyramid, normalBuffer, rayCountTexture);

            // Denoise if required
            rtgiResult = DenoiseRTGI(renderGraph, hdCamera, rtgiResult, depthPyramid, normalBuffer, motionVectors);

            // Adjust the weight
            AdjustRTIDWeightParameters artidParamters = PrepareAdjustRTIDWeightParametersParameters(hdCamera);
            rtgiResult = AdjustRTGIWeight(renderGraph, in artidParamters, rtgiResult, depthPyramid, stencilBuffer);

            return rtgiResult;
        }

        static RTHandle RequestIndirectDiffuseHistoryTextureHF(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF,
                IndirectDiffuseHistoryBufferAllocatorFunction, 1);
        }

        static RTHandle RequestIndirectDiffuseHistoryTextureLF(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF,
                IndirectDiffuseHistoryBufferAllocatorFunction, 1);
        }

        TextureHandle DenoiseRTGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rtGIBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer)
        {
            var giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            if (giSettings.denoise)
            {
                // Evaluate the history's validity
                float historyValidity0 = EvaluateIndirectDiffuseHistoryValidity0(hdCamera, giSettings.fullResolution, true);

                HDTemporalFilter temporalFilter = GetTemporalFilter();
                HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();

                // Run the temporal denoiser
                TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, false, historyValidity0);
                TextureHandle historyBufferHF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureHF(hdCamera));
                TextureHandle denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, tfParameters, rtGIBuffer, historyBufferHF, depthPyramid, normalBuffer, motionVectorBuffer);

                // Apply the diffuse denoiser
                DiffuseDenoiserParameters ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, false, giSettings.denoiserRadius, giSettings.halfResolutionDenoiser, giSettings.secondDenoiserPass);
                rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTGI, depthPyramid, normalBuffer, rtGIBuffer);

                // If the second pass is requested, do it otherwise blit
                if (giSettings.secondDenoiserPass)
                {
                    float historyValidity1 = EvaluateIndirectDiffuseHistoryValidity1(hdCamera, giSettings.fullResolution, true);

                    // Run the temporal denoiser
                    tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, false, historyValidity1);
                    TextureHandle historyBufferLF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureLF(hdCamera));
                    denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, tfParameters, rtGIBuffer, historyBufferLF, depthPyramid, normalBuffer, motionVectorBuffer);

                    // Apply the diffuse denoiser
                    ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, false, giSettings.denoiserRadius * 0.5f, giSettings.halfResolutionDenoiser, false);
                    rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTGI, depthPyramid, normalBuffer, rtGIBuffer);

                    // Propagate the history validity for the second buffer
                    PropagateIndirectDiffuseHistoryValidity1(hdCamera, giSettings.fullResolution, true);
                }

                // Propagate the history validity for the first buffer
                PropagateIndirectDiffuseHistoryValidity0(hdCamera, giSettings.fullResolution, true);

                return rtGIBuffer;
            }
            else
                return rtGIBuffer;
        }

        TextureHandle RenderRayTracedIndirectDiffuse(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, Texture skyTexture, TextureHandle rayCountTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            TextureHandle rtreflResult;
            bool qualityMode = false;

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                qualityMode = giSettings.mode.value == RayTracingMode.Quality;
            else
                qualityMode = m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality;


            if (qualityMode)
                rtreflResult = RenderIndirectDiffuseQuality(renderGraph, hdCamera,
                    depthPyramid, stencilBuffer, normalBuffer, motionVectors, rayCountTexture, skyTexture,
                    shaderVariablesRaytracing);
            else
                rtreflResult = RenderIndirectDiffusePerformance(renderGraph, hdCamera,
                    depthPyramid, stencilBuffer, normalBuffer, motionVectors, rayCountTexture, skyTexture,
                    shaderVariablesRaytracing);

            return rtreflResult;
        }
    }
}
