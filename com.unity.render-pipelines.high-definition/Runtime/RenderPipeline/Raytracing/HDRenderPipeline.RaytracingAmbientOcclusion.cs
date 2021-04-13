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
            m_RTAOApplyIntensityKernel = asset.renderPipelineRayTracingResources.aoRaytracingCS.FindKernel("RTAOApplyIntensity");
        }

        float EvaluateRTSpecularOcclusionFlag(HDCamera hdCamera, AmbientOcclusion ssoSettings)
        {
            float remappedRayLength = (Mathf.Clamp(ssoSettings.rayLength, 1.25f, 1.5f) - 1.25f) / 0.25f;
            return Mathf.Lerp(0.0f, 1.0f, 1.0f - remappedRayLength);
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
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            TextureHandle result;

            if (GetRayTracingState())
            {
                // Trace the signal
                var traceResult = TraceAO(renderGraph, hdCamera, depthBuffer, normalBuffer, rayCountTexture, shaderVariablesRaytracing);

                // Denoise if required
                result = DenoiseAO(renderGraph, hdCamera, traceResult, depthBuffer, normalBuffer, motionVectors, historyValidationBuffer);

                // Compose the result to be done
                result = ComposeAO(renderGraph, hdCamera, result);
            }
            else
            {
                result = renderGraph.defaultResources.blackTextureXR;
            }
            return result;
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

                var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

                // Camera data
                passData.actualWidth = hdCamera.actualWidth;
                passData.actualHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.rayLength = aoSettings.rayLength;
                passData.sampleCount = aoSettings.sampleCount;
                passData.denoise = aoSettings.denoise;

                // Other parameters
                passData.raytracingCB = shaderVariablesRaytracing;
                passData.aoShaderRT = asset.renderPipelineRayTracingResources.aoRaytracingRT;
                passData.rayTracingAccelerationStructure = RequestAccelerationStructure();
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
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            if (aoSettings.denoise)
            {
                // Evaluate the history's validity
                float historyValidity = EvaluateHistoryValidity(hdCamera);

                // Run the temporal denoiser
                HDTemporalFilter temporalFilter = GetTemporalFilter();
                TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, true, historyValidity);
                TextureHandle historyBuffer = renderGraph.ImportTexture(RequestAmbientOcclusionHistoryTexture(hdCamera));
                TextureHandle denoisedRTAO = temporalFilter.Denoise(renderGraph, hdCamera, tfParameters, traceAOResult.signalBuffer, traceAOResult.velocityBuffer, historyBuffer, depthBuffer, normalBuffer, motionVectorBuffer, historyValidationBuffer);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();
                DiffuseDenoiserParameters ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, true, aoSettings.denoiserRadius, false, false);
                return diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTAO, depthBuffer, normalBuffer, traceAOResult.signalBuffer);
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

                var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

                passData.intensity = aoSettings.intensity.value;
                passData.actualWidth = hdCamera.actualWidth;
                passData.actualHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.aoShaderCS = asset.renderPipelineRayTracingResources.aoRaytracingCS;
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

        static RTHandle RequestAmbientOcclusionHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion,
                AmbientOcclusionHistoryBufferAllocatorFunction, 1);
        }
    }
}
