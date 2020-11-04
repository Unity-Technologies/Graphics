using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class DirGenRTRPassData
        {
            public RTReflectionDirGenParameters parameters;
            public TextureHandle depthBuffer;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle clearCoatMaskTexture;
            public TextureHandle outputBuffer;
        }

        TextureHandle DirGenRTR(RenderGraph renderGraph, in RTReflectionDirGenParameters parameters, TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle clearCoatTexture)
        {
            using (var builder = renderGraph.AddRenderPass<DirGenRTRPassData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.clearCoatMaskTexture = builder.ReadTexture(clearCoatTexture);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Directions" }));

                builder.SetRenderFunc(
                (DirGenRTRPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    RTReflectionDirGenResources rtrDirGenResources = new RTReflectionDirGenResources();
                    rtrDirGenResources.depthBuffer = data.depthBuffer;
                    rtrDirGenResources.stencilBuffer = data.stencilBuffer;
                    rtrDirGenResources.normalBuffer = data.normalBuffer;
                    rtrDirGenResources.clearCoatMaskTexture = data.clearCoatMaskTexture;
                    rtrDirGenResources.outputBuffer = data.outputBuffer;
                    RTReflectionDirectionGeneration(ctx.cmd, data.parameters, rtrDirGenResources);
                });

                return passData.outputBuffer;
            }
        }

        class UpscaleRTRPassData
        {
            public RTReflectionUpscaleParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle clearCoatMaskTexture;
            public TextureHandle lightingTexture;
            public TextureHandle hitPointTexture;
            public TextureHandle outputTexture;
        }

        TextureHandle UpscaleRTR(RenderGraph renderGraph, in RTReflectionUpscaleParameters parameters,
                                TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle clearCoatTexture, TextureHandle lightingTexture, TextureHandle hitPointTexture)
        {
            using (var builder = renderGraph.AddRenderPass<UpscaleRTRPassData>("Upscale the RTR result", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseUpscale)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.clearCoatMaskTexture = builder.ReadTexture(clearCoatTexture);
                passData.lightingTexture = builder.ReadTexture(lightingTexture);
                passData.hitPointTexture = builder.ReadTexture(hitPointTexture);
                passData.outputTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Reflections" }));

                builder.SetRenderFunc(
                (UpscaleRTRPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    RTReflectionUpscaleResources rtrUpscaleResources = new RTReflectionUpscaleResources();
                    rtrUpscaleResources.depthStencilBuffer = data.depthStencilBuffer;
                    rtrUpscaleResources.normalBuffer = data.normalBuffer;
                    rtrUpscaleResources.clearCoatMaskTexture = data.clearCoatMaskTexture;
                    rtrUpscaleResources.lightingTexture = data.lightingTexture;
                    rtrUpscaleResources.hitPointTexture = data.hitPointTexture;
                    rtrUpscaleResources.outputTexture = data.outputTexture;
                    UpscaleRTReflections(ctx.cmd, data.parameters, rtrUpscaleResources);
                });

                return passData.outputTexture;
            }
        }

        static RTHandle RequestRayTracedReflectionsHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection,
                ReflectionHistoryBufferAllocatorFunction, 1);
        }

        TextureHandle RenderReflectionsPerformance(RenderGraph renderGraph, HDCamera hdCamera,
                                                    TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle rayCountTexture, TextureHandle clearCoatTexture, Texture skyTexture,
                                                    int frameCount, ShaderVariablesRaytracing shaderVariablesRaytracing, bool transparent)
        {
            // Pointer to the final result
            TextureHandle rtrResult;

            // Fetch all the settings
            ScreenSpaceReflection settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            RTReflectionDirGenParameters rtrDirGenParameters = PrepareRTReflectionDirGenParameters(hdCamera, transparent, settings);
            TextureHandle directionBuffer = DirGenRTR(renderGraph, in rtrDirGenParameters, depthPyramid, stencilBuffer, normalBuffer, clearCoatTexture);

            DeferredLightingRTParameters deferredParamters = PrepareReflectionDeferredLightingRTParameters(hdCamera);
            TextureHandle lightingBuffer = DeferredLightingRT(renderGraph, in deferredParamters, directionBuffer, depthPyramid, normalBuffer, skyTexture, rayCountTexture);

            RTReflectionUpscaleParameters rtrUpscaleParameters = PrepareRTReflectionUpscaleParameters(hdCamera, settings);
            rtrResult = UpscaleRTR(renderGraph, in rtrUpscaleParameters,
                                    depthPyramid, normalBuffer, clearCoatTexture, lightingBuffer, directionBuffer);

            // Denoise if required
            if (settings.denoise && !transparent)
            {
                // Grab the history buffer
                RTHandle reflectionHistory = RequestRayTracedReflectionsHistoryTexture(hdCamera);

                // Prepare the parameters and the resources
                HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                ReflectionDenoiserParameters reflDenoiserParameters = reflectionDenoiser.PrepareReflectionDenoiserParameters(hdCamera, EvaluateHistoryValidity(hdCamera), settings.denoiserRadius, true);
                RTHandle historySignal = RequestRayTracedReflectionsHistoryTexture(hdCamera);
                rtrResult = reflectionDenoiser.DenoiseRTR(renderGraph, in reflDenoiserParameters, hdCamera, depthPyramid, normalBuffer, motionVectors, clearCoatTexture, rtrResult, historySignal);
            }

            return rtrResult;
        }

        class TraceQualityRTRPassData
        {
            public RTRQualityRenderingParameters parameters;
            public TextureHandle depthBuffer;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle clearCoatMaskTexture;
            public TextureHandle rayCountTexture;
            public TextureHandle outputTexture;
        }

        TextureHandle QualityRTR(RenderGraph renderGraph, in RTRQualityRenderingParameters parameters, TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle clearCoatTexture, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddRenderPass<TraceQualityRTRPassData>("Quality RT Reflections", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionEvaluation)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.clearCoatMaskTexture = builder.ReadTexture(clearCoatTexture);
                passData.rayCountTexture = builder.WriteTexture(builder.ReadTexture(rayCountTexture));
                passData.outputTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced Reflections" }));

                builder.SetRenderFunc(
                (TraceQualityRTRPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    RTRQualityRenderingResources rtrQRenderingResources = new RTRQualityRenderingResources();
                    rtrQRenderingResources.depthBuffer = data.depthBuffer;
                    rtrQRenderingResources.stencilBuffer = data.stencilBuffer;
                    rtrQRenderingResources.normalBuffer = data.normalBuffer;
                    rtrQRenderingResources.clearCoatMaskTexture = data.clearCoatMaskTexture;
                    rtrQRenderingResources.rayCountTexture = data.rayCountTexture;
                    rtrQRenderingResources.outputTexture = data.outputTexture;
                    RenderQualityRayTracedReflections(ctx.cmd, data.parameters, rtrQRenderingResources);
                });

                return passData.outputTexture;
            }
        }

        TextureHandle RenderReflectionsQuality(RenderGraph renderGraph, HDCamera hdCamera,
                                            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle rayCountTexture, TextureHandle clearCoatTexture, Texture skyTexture,
                                            int frameCount, ShaderVariablesRaytracing shaderVariablesRaytracing, bool transparent)
        {
            TextureHandle rtrResult;

            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            RTRQualityRenderingParameters rtrQRenderingParameters = PrepareRTRQualityRenderingParameters(hdCamera, settings, transparent);
            rtrResult = QualityRTR(renderGraph, in rtrQRenderingParameters, depthPyramid, stencilBuffer, normalBuffer, clearCoatTexture, rayCountTexture);

            // Denoise if required
            if (settings.denoise && !transparent)
            {
                // Grab the history buffer
                RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                // Prepare the parameters and the resources
                HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                ReflectionDenoiserParameters reflDenoiserParameters = reflectionDenoiser.PrepareReflectionDenoiserParameters(hdCamera, EvaluateHistoryValidity(hdCamera), settings.denoiserRadius, rtrQRenderingParameters.bounceCount == 1);
                RTHandle historySignal = RequestRayTracedReflectionsHistoryTexture(hdCamera);
                rtrResult = reflectionDenoiser.DenoiseRTR(renderGraph, in reflDenoiserParameters, hdCamera, depthPyramid, normalBuffer, motionVectors, clearCoatTexture, rtrResult, historySignal);
            }

            return rtrResult;
        }

        TextureHandle RenderRayTracedReflections(RenderGraph renderGraph, HDCamera hdCamera,
                                        TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle clearCoatTexture, Texture skyTexture, TextureHandle rayCountTexture,
                                        int frameCount, ShaderVariablesRaytracing shaderVariablesRaytracing, bool transparent)
        {
            ScreenSpaceReflection reflectionSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            TextureHandle rtreflResult;
            bool qualityMode = false;

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                qualityMode = reflectionSettings.mode.value == RayTracingMode.Quality;
            else
                qualityMode = m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality;


            if (qualityMode)
                rtreflResult = RenderReflectionsQuality(renderGraph, hdCamera,
                                                    depthPyramid, stencilBuffer, normalBuffer, motionVectors, rayCountTexture, clearCoatTexture, skyTexture,
                                                    frameCount, shaderVariablesRaytracing, transparent);
            else
                rtreflResult = RenderReflectionsPerformance(renderGraph, hdCamera,
                                                    depthPyramid, stencilBuffer, normalBuffer, motionVectors, rayCountTexture, clearCoatTexture, skyTexture,
                                                    frameCount, shaderVariablesRaytracing, transparent);

            return rtreflResult;
        }
    }
}
