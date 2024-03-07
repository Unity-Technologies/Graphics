using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipeline
    {
        // The target denoising kernel
        int m_RTAOApplyIntensityKernel;

        // String values
        const string m_AORayGenShaderName = "RayGenAmbientOcclusion";

        void InitRayTracingAmbientOcclusion()
        {
            // Grab the kernels we need
            m_RTAOApplyIntensityKernel = m_GlobalSettings.renderPipelineRayTracingResources.aoRaytracingCS.FindKernel("RTAOApplyIntensity");
        }

        private float EvaluateRayTracedAmbientOcclusionHistoryValidity(HDCamera hdCamera)
        {
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedAmbientOcclusion, (int)HDCamera.HistoryEffectFlags.RayTraced) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private void PropagateRayTracedAmbientOcclusionHistoryValidity(HDCamera hdCamera)
        {
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedAmbientOcclusion, (int)HDCamera.HistoryEffectFlags.RayTraced);
        }

        float EvaluateRTSpecularOcclusionFlag(HDCamera hdCamera, ScreenSpaceAmbientOcclusion ssoSettings)
        {
            return ssoSettings.specularOcclusion.value;
        }

        static RTHandle AmbientOcclusionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_AmbientOcclusionHistoryBuffer{1}", viewName, frameIndex));
        }

        TextureHandle RenderRTAO(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle historyValidationBuffer,
            TextureHandle rayCountTexture, in ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            // Trace the signal
            var traceResult = TraceAO(renderGraph, hdCamera, depthBuffer, normalBuffer, rayCountTexture, shaderVariablesRaytracing);

            // Denoise if required
            TextureHandle denoisedAO = DenoiseAO(renderGraph, hdCamera, traceResult, depthBuffer, normalBuffer, motionVectors, historyValidationBuffer);

            // Compose the result to be done
            return ComposeAO(renderGraph, hdCamera, denoisedAO);
        }

        struct TraceAmbientOcclusionResult
        {
            public TextureHandle signalBuffer;
            public TextureHandle velocityBuffer;
        }

        class TraceRTAOPassData
        {
            // Camera data
            public int actualWidth;
            public int actualHeight;
            public int viewCount;

            // Evaluation parameters
            public float rayLength;
            public int sampleCount;
            public bool denoise;
            public bool occluderMotionRejection;
            public bool receiverMotionRejection;

            // Other parameters
            public RayTracingShader aoShaderRT;
            public ShaderVariablesRaytracing raytracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public RayTracingAccelerationStructure rayTracingAccelerationStructure;

            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputTexture;
            public TextureHandle velocityBuffer;
        }

        TraceAmbientOcclusionResult TraceAO(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle rayCountTexture, in ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            using (var builder = renderGraph.AddRenderPass<TraceRTAOPassData>("Tracing the rays for RTAO", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingAmbientOcclusion)))
            {
                TraceAmbientOcclusionResult traceOutput = new TraceAmbientOcclusionResult();

                builder.EnableAsyncCompute(false);

                var aoSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();

                // Camera data
                passData.actualWidth = hdCamera.actualWidth;
                passData.actualHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.rayLength = aoSettings.rayLength;
                passData.sampleCount = aoSettings.sampleCount;
                passData.denoise = aoSettings.denoise;
                passData.occluderMotionRejection = aoSettings.occluderMotionRejection.value;
                passData.receiverMotionRejection = aoSettings.receiverMotionRejection.value;

                // Other parameters
                passData.raytracingCB = shaderVariablesRaytracing;
                passData.aoShaderRT = m_GlobalSettings.renderPipelineRayTracingResources.aoRaytracingRT;
                passData.rayTracingAccelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

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
                        // Define the shader pass to use for the reflection pass
                        ctx.cmd.SetRayTracingShaderPass(data.aoShaderRT, "VisibilityDXR");

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.aoShaderRT, HDShaderIDs._RaytracingAccelerationStructureName, data.rayTracingAccelerationStructure);

                        // Inject the ray generation data (be careful of the global constant buffer limitation)
                        data.raytracingCB._RaytracingRayMaxLength = data.rayLength;
                        data.raytracingCB._RaytracingNumSamples = data.sampleCount;
                        ConstantBuffer.PushGlobal(ctx.cmd, data.raytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Set the data for the ray generation
                        ctx.cmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, data.depthBuffer, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Set the output textures
                        ctx.cmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);
                        ctx.cmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._AmbientOcclusionTextureRW, data.outputTexture);
                        ctx.cmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._VelocityBuffer, data.velocityBuffer);

                        // Run the computation
                        ctx.cmd.DispatchRays(data.aoShaderRT, m_AORayGenShaderName, (uint)data.actualWidth, (uint)data.actualHeight, (uint)data.viewCount);
                    });

                traceOutput.signalBuffer = passData.outputTexture;
                traceOutput.velocityBuffer = passData.velocityBuffer;
                return traceOutput;
            }
        }

        TextureHandle DenoiseAO(RenderGraph renderGraph, HDCamera hdCamera, TraceAmbientOcclusionResult traceAOResult, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationBuffer)
        {
            var aoSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();
            if (aoSettings.denoise)
            {
                // Evaluate the history's validity
                float historyValidity = EvaluateRayTracedAmbientOcclusionHistoryValidity(hdCamera);

                // Run the temporal denoiser
                TextureHandle historyBuffer = renderGraph.ImportTexture(RequestAmbientOcclusionHistoryTexture(renderGraph, hdCamera));
                HDTemporalFilter.TemporalFilterParameters filterParams;
                filterParams.singleChannel = true;
                filterParams.historyValidity = historyValidity;
                filterParams.occluderMotionRejection = aoSettings.occluderMotionRejection.value;
                filterParams.receiverMotionRejection = aoSettings.receiverMotionRejection.value;
                filterParams.exposureControl = false;
                filterParams.resolutionMultiplier = 1.0f;
                filterParams.historyResolutionMultiplier = 1.0f;

                TextureHandle denoisedRTAO = GetTemporalFilter().Denoise(renderGraph, hdCamera, filterParams,
                    traceAOResult.signalBuffer, traceAOResult.velocityBuffer, historyBuffer,
                    depthBuffer, normalBuffer, motionVectorBuffer, historyValidationBuffer);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();
                HDDiffuseDenoiser.DiffuseDenoiserParameters ddParams;
                ddParams.singleChannel = true;
                ddParams.kernelSize = aoSettings.denoiserRadius;
                ddParams.halfResolutionFilter = false;
                ddParams.jitterFilter = false;
                ddParams.resolutionMultiplier = 1.0f;
                TextureHandle result = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTAO, depthBuffer, normalBuffer, traceAOResult.signalBuffer);
                PropagateRayTracedAmbientOcclusionHistoryValidity(hdCamera);
                return result;
            }
            else
                return traceAOResult.signalBuffer;
        }

        class ComposeRTAOPassData
        {
            // Generic attributes
            public float intensity;

            // Camera data
            public int actualWidth;
            public int actualHeight;
            public int viewCount;

            // Kernels
            public int intensityKernel;

            // Shaders
            public ComputeShader aoShaderCS;

            public TextureHandle outputTexture;
        }

        TextureHandle ComposeAO(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle aoTexture)
        {
            using (var builder = renderGraph.AddRenderPass<ComposeRTAOPassData>("Composing the result of RTAO", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingComposeAmbientOcclusion)))
            {
                builder.EnableAsyncCompute(false);

                var aoSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();

                passData.intensity = aoSettings.intensity.value;
                passData.actualWidth = hdCamera.actualWidth;
                passData.actualHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.aoShaderCS = m_GlobalSettings.renderPipelineRayTracingResources.aoRaytracingCS;
                passData.intensityKernel = m_RTAOApplyIntensityKernel;
                passData.outputTexture = builder.ReadWriteTexture(aoTexture);

                builder.SetRenderFunc(
                    (ComposeRTAOPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeFloatParam(data.aoShaderCS, HDShaderIDs._RaytracingAOIntensity, data.intensity);
                        ctx.cmd.SetComputeTextureParam(data.aoShaderCS, data.intensityKernel, HDShaderIDs._AmbientOcclusionTextureRW, data.outputTexture);
                        int texWidth = data.actualWidth;
                        int texHeight = data.actualHeight;
                        int areaTileSize = 8;
                        int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;
                        ctx.cmd.DispatchCompute(data.aoShaderCS, data.intensityKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputTexture;
            }
        }

        class ClearRTAOHistoryData
        {
            public TextureHandle aoTexture;
        }

        static RTHandle RequestAmbientOcclusionHistoryTexture(RenderGraph renderGraph, HDCamera hdCamera)
        {
            var aoHistoryTex = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion);
            if (aoHistoryTex == null)
            {
                var newHistoryTexture = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion, AmbientOcclusionHistoryBufferAllocatorFunction, 1);
                using (var builder = renderGraph.AddRenderPass<ClearRTAOHistoryData>("Clearing the AO History Texture", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingClearHistoryAmbientOcclusion)))
                {
                    builder.EnableAsyncCompute(false);
                    passData.aoTexture = builder.ReadWriteTexture(renderGraph.ImportTexture(newHistoryTexture));

                    builder.SetRenderFunc(
                        (ClearRTAOHistoryData data, RenderGraphContext ctx) =>
                        {
                            CoreUtils.SetRenderTarget(ctx.cmd, data.aoTexture, clearFlag: ClearFlag.Color, Color.black);
                        });

                    return newHistoryTexture;
                }
            }
            else
            {
                return aoHistoryTex;
            }
        }
    }
}
