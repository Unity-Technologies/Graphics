using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipeline
    {
        // The target denoising kernel
        int m_RTAOApplyIntensityKernel;

        // String values
        const string m_MissShaderName = "MissShaderAmbientOcclusion";
        const string m_ClosestHitShaderName = "ClosestHitMain";

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

        // The set of parameters that are used to trace the ambient occlusion
        struct AmbientOcclusionTraceParameters
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
        }

        struct AmbientOcclusionTraceResources
        {
            // Input Buffer
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;

            // Debug textures
            public RTHandle rayCountTexture;

            // Output Buffer
            public RTHandle outputTexture;
        }

        AmbientOcclusionTraceParameters PrepareAmbientOcclusionTraceParameters(HDCamera hdCamera, ShaderVariablesRaytracing raytracingCB)
        {
            AmbientOcclusionTraceParameters rtAOParameters = new AmbientOcclusionTraceParameters();
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            // Camera data
            rtAOParameters.actualWidth = hdCamera.actualWidth;
            rtAOParameters.actualHeight = hdCamera.actualHeight;
            rtAOParameters.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            rtAOParameters.rayLength = aoSettings.rayLength;
            rtAOParameters.sampleCount = aoSettings.sampleCount;
            rtAOParameters.denoise = aoSettings.denoise;

            // Other parameters
            rtAOParameters.raytracingCB = raytracingCB;
            rtAOParameters.aoShaderRT = asset.renderPipelineRayTracingResources.aoRaytracingRT;
            rtAOParameters.rayTracingAccelerationStructure = RequestAccelerationStructure();
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtAOParameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return rtAOParameters;
        }

        TextureHandle RenderRTAO(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle rayCountTexture, ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            TextureHandle result;

            if (GetRayTracingState())
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
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);
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
                HDTemporalFilter temporalFilter = GetTemporalFilter();
                TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, true, historyValidity);
                TextureHandle historyBuffer = renderGraph.ImportTexture(RequestAmbientOcclusionHistoryTexture(hdCamera));
                TextureHandle denoisedRTAO = temporalFilter.Denoise(renderGraph, hdCamera, tfParameters, rayTracedAO, historyBuffer, depthPyramid, normalBuffer, motionVectorBuffer);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();
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

        // The set of parameters that are used to trace the ambient occlusion
        struct AmbientOcclusionComposeParameters
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
        }

        AmbientOcclusionComposeParameters PrepareAmbientOcclusionComposeParameters(HDCamera hdCamera, ShaderVariablesRaytracing raytracingCB)
        {
            AmbientOcclusionComposeParameters aoComposeParameters = new AmbientOcclusionComposeParameters();
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            aoComposeParameters.intensity = aoSettings.intensity.value;
            aoComposeParameters.actualWidth = hdCamera.actualWidth;
            aoComposeParameters.actualHeight = hdCamera.actualHeight;
            aoComposeParameters.viewCount = hdCamera.viewCount;
            aoComposeParameters.aoShaderCS = asset.renderPipelineRayTracingResources.aoRaytracingCS;
            aoComposeParameters.intensityKernel = m_RTAOApplyIntensityKernel;
            return aoComposeParameters;
        }

        static void TraceAO(CommandBuffer cmd, AmbientOcclusionTraceParameters aoTraceParameters, AmbientOcclusionTraceResources aoTraceResources)
        {
            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(aoTraceParameters.aoShaderRT, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(aoTraceParameters.aoShaderRT, HDShaderIDs._RaytracingAccelerationStructureName, aoTraceParameters.rayTracingAccelerationStructure);

            // Inject the ray generation data (be careful of the global constant buffer limitation)
            aoTraceParameters.raytracingCB._RaytracingRayMaxLength = aoTraceParameters.rayLength;
            aoTraceParameters.raytracingCB._RaytracingNumSamples = aoTraceParameters.sampleCount;
            ConstantBuffer.PushGlobal(cmd, aoTraceParameters.raytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._DepthTexture, aoTraceResources.depthStencilBuffer);
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._NormalBufferTexture, aoTraceResources.normalBuffer);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, aoTraceParameters.ditheredTextureSet);

            // Set the output textures
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._RayCountTexture, aoTraceResources.rayCountTexture);
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._AmbientOcclusionTextureRW, aoTraceResources.outputTexture);

            // Run the computation
            cmd.DispatchRays(aoTraceParameters.aoShaderRT, m_RayGenShaderName, (uint)aoTraceParameters.actualWidth, (uint)aoTraceParameters.actualHeight, (uint)aoTraceParameters.viewCount);
        }

        static RTHandle RequestAmbientOcclusionHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion,
                AmbientOcclusionHistoryBufferAllocatorFunction, 1);
        }

        static void ComposeAO(CommandBuffer cmd, AmbientOcclusionComposeParameters aoComposeParameters, RTHandle outputTexture)
        {
            cmd.SetComputeFloatParam(aoComposeParameters.aoShaderCS, HDShaderIDs._RaytracingAOIntensity, aoComposeParameters.intensity);
            cmd.SetComputeTextureParam(aoComposeParameters.aoShaderCS, aoComposeParameters.intensityKernel, HDShaderIDs._AmbientOcclusionTextureRW, outputTexture);
            int texWidth = aoComposeParameters.actualWidth;
            int texHeight = aoComposeParameters.actualHeight;
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;
            cmd.DispatchCompute(aoComposeParameters.aoShaderCS, aoComposeParameters.intensityKernel, numTilesXHR, numTilesYHR, aoComposeParameters.viewCount);

            // Bind the textures and the params
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, outputTexture);
        }
    }
}
