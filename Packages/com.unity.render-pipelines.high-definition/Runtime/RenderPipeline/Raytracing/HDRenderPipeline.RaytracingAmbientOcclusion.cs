using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
            m_RTAOApplyIntensityKernel = rayTracingResources.aoRayTracingCS.FindKernel("RTAOApplyIntensity");
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
            TextureHandle depthBuffer, in TextureHandle normalBuffer, in TextureHandle motionVectors, in TextureHandle historyValidationBuffer,
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

        TraceAmbientOcclusionResult TraceAO(RenderGraph renderGraph, HDCamera hdCamera, in TextureHandle depthBuffer, in TextureHandle normalBuffer, in TextureHandle rayCountTexture, in ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            using (var builder = renderGraph.AddUnsafePass<TraceRTAOPassData>("Tracing the rays for RTAO", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingAmbientOcclusion)))
            {
                TraceAmbientOcclusionResult traceOutput = new TraceAmbientOcclusionResult();

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
                passData.aoShaderRT = rayTracingResources.aoRayTracingRT;
                passData.rayTracingAccelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                passData.depthBuffer = depthBuffer;
                builder.UseTexture(passData.depthBuffer, AccessFlags.Read);
                passData.normalBuffer = normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.rayCountTexture = rayCountTexture;
                builder.UseTexture(passData.rayCountTexture, AccessFlags.ReadWrite);
                // Depending of if we will have to denoise (or not), we need to allocate the final format, or a bigger texture
                passData.outputTexture = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8_UNorm, enableRandomWrite = true, name = "Ray Traced Ambient Occlusion" });
                builder.UseTexture(passData.outputTexture, AccessFlags.Write);
                passData.velocityBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8_SNorm, enableRandomWrite = true, name = "Velocity Buffer" });
                builder.UseTexture(passData.velocityBuffer, AccessFlags.ReadWrite);

                builder.SetRenderFunc(
                    static (TraceRTAOPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                        // Define the shader pass to use for the reflection pass
                        natCmd.SetRayTracingShaderPass(data.aoShaderRT, "VisibilityDXR");

                        // Set the acceleration structure for the pass
                        natCmd.SetRayTracingAccelerationStructure(data.aoShaderRT, HDShaderIDs._RaytracingAccelerationStructureName, data.rayTracingAccelerationStructure);

                        // Inject the ray generation data (be careful of the global constant buffer limitation)
                        data.raytracingCB._RaytracingRayMaxLength = data.rayLength;
                        data.raytracingCB._RaytracingNumSamples = data.sampleCount;
                        ConstantBuffer.PushGlobal(natCmd, data.raytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Set the data for the ray generation
                        natCmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._DepthTexture, data.depthBuffer);
                        natCmd.SetGlobalTexture(HDShaderIDs._StencilTexture, data.depthBuffer, RenderTextureSubElement.Stencil);
                        natCmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // Set the output textures
                        natCmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);
                        natCmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._AmbientOcclusionTextureRW, data.outputTexture);
                        natCmd.SetRayTracingTextureParam(data.aoShaderRT, HDShaderIDs._VelocityBuffer, data.velocityBuffer);

                        // Run the computation
                        natCmd.DispatchRays(data.aoShaderRT, m_AORayGenShaderName, (uint)data.actualWidth, (uint)data.actualHeight, (uint)data.viewCount, null);
                    });

                traceOutput.signalBuffer = passData.outputTexture;
                traceOutput.velocityBuffer = passData.velocityBuffer;
                return traceOutput;
            }
        }

        TextureHandle DenoiseAO(RenderGraph renderGraph, HDCamera hdCamera, TraceAmbientOcclusionResult traceAOResult, in TextureHandle depthBuffer, in TextureHandle normalBuffer, in TextureHandle motionVectorBuffer, in TextureHandle historyValidationBuffer)
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

        TextureHandle ComposeAO(RenderGraph renderGraph, HDCamera hdCamera, in TextureHandle aoTexture)
        {
            using (var builder = renderGraph.AddUnsafePass<ComposeRTAOPassData>("Composing the result of RTAO", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingComposeAmbientOcclusion)))
            {
                var aoSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();

                passData.intensity = aoSettings.intensity.value;
                passData.actualWidth = hdCamera.actualWidth;
                passData.actualHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.aoShaderCS = rayTracingResources.aoRayTracingCS;
                passData.intensityKernel = m_RTAOApplyIntensityKernel;
                passData.outputTexture = aoTexture;
                builder.UseTexture(passData.outputTexture, AccessFlags.ReadWrite);

                builder.SetRenderFunc(
                    static (ComposeRTAOPassData data, UnsafeGraphContext ctx) =>
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
                using (var builder = renderGraph.AddUnsafePass<ClearRTAOHistoryData>("Clearing the AO History Texture", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingClearHistoryAmbientOcclusion)))
                {
                    passData.aoTexture = renderGraph.ImportTexture(newHistoryTexture);
                    builder.UseTexture(passData.aoTexture, AccessFlags.ReadWrite);

                    builder.SetRenderFunc(
                        static (ClearRTAOHistoryData data, UnsafeGraphContext ctx) =>
                        {
                            CoreUtils.SetRenderTarget(CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd), data.aoTexture, clearFlag: ClearFlag.Color, Color.black);
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
