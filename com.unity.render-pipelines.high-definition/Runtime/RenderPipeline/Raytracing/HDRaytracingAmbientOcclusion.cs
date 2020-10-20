using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRaytracingAmbientOcclusion
    {
        // External structures
        HDRenderPipelineRayTracingResources m_PipelineRayTracingResources = null;
        HDRenderPipeline m_RenderPipeline = null;

        // The target denoising kernel
        int m_RTAOApplyIntensityKernel;

        // Intermediate buffer that stores the ambient occlusion pre-denoising
        RTHandle m_AOIntermediateBuffer0 = null;
        RTHandle m_AOIntermediateBuffer1 = null;

        // String values
        const string m_RayGenShaderName = "RayGenAmbientOcclusion";
        const string m_MissShaderName = "MissShaderAmbientOcclusion";
        const string m_ClosestHitShaderName = "ClosestHitMain";

        public HDRaytracingAmbientOcclusion()
        {
        }

        public void Init(HDRenderPipeline renderPipeline)
        {
            // Keep track of the pipeline asset
            m_PipelineRayTracingResources = renderPipeline.asset.renderPipelineRayTracingResources;

            // keep track of the render pipeline
            m_RenderPipeline = renderPipeline;

            // Grab the kernels we need
            m_RTAOApplyIntensityKernel = m_PipelineRayTracingResources.aoRaytracingCS.FindKernel("RTAOApplyIntensity");
        }

        public void InitializeNonRenderGraphResources()
        {
            // Allocate the intermediate textures
            m_AOIntermediateBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "AOIntermediateBuffer0");
            m_AOIntermediateBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "AOIntermediateBuffer1");
        }

        public void CleanupNonRenderGraphResources()
        {
            RTHandles.Release(m_AOIntermediateBuffer1);
            RTHandles.Release(m_AOIntermediateBuffer0);
            m_AOIntermediateBuffer0 = null;
            m_AOIntermediateBuffer1 = null;
        }

        static RTHandle AmbientOcclusionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("{0}_AmbientOcclusionHistoryBuffer{1}", viewName, frameIndex));
        }


        static public void SetDefaultAmbientOcclusionTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, TextureXR.GetBlackTexture());
        }

        // The set of parameters that are used to trace the ambient occlusion
        public struct AmbientOcclusionTraceParameters
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

        public struct AmbientOcclusionTraceResources
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
            rtAOParameters.aoShaderRT = m_PipelineRayTracingResources.aoRaytracingRT;
            rtAOParameters.rayTracingAccelerationStructure = m_RenderPipeline.RequestAccelerationStructure();
            BlueNoise blueNoise = m_RenderPipeline.GetBlueNoiseManager();
            rtAOParameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return rtAOParameters;
        }

        AmbientOcclusionTraceResources PrepareAmbientOcclusionTraceResources(HDCamera hdCamera, RTHandle outputTexture)
        {
            AmbientOcclusionTraceResources rtAOResources = new AmbientOcclusionTraceResources();
            rtAOResources.depthStencilBuffer = m_RenderPipeline.sharedRTManager.GetDepthStencilBuffer();
            rtAOResources.normalBuffer = m_RenderPipeline.sharedRTManager.GetNormalBuffer();
            rtAOResources.rayCountTexture = m_RenderPipeline.GetRayCountManager().GetRayCountTexture();
            rtAOResources.outputTexture = outputTexture;
            return rtAOResources;
        }

        // The set of parameters that are used to trace the ambient occlusion
        public struct AmbientOcclusionComposeParameters
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
            aoComposeParameters.aoShaderCS = m_PipelineRayTracingResources.aoRaytracingCS;
            aoComposeParameters.intensityKernel = m_RTAOApplyIntensityKernel;
            return aoComposeParameters;
        }

        public void RenderRTAO(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ShaderVariablesRaytracing globalCB, ScriptableRenderContext renderContext, int frameCount)
        {
            if (!m_RenderPipeline.GetRayTracingState())
            {
                SetDefaultAmbientOcclusionTexture(cmd);
                return;
            }
            AmbientOcclusionTraceParameters aoTraceParameters = PrepareAmbientOcclusionTraceParameters(hdCamera, globalCB);
            AmbientOcclusionTraceResources aoTraceResources = PrepareAmbientOcclusionTraceResources(hdCamera, m_AOIntermediateBuffer0);
            // If any of the previous requirements is missing, the effect is not requested or no acceleration structure, set the default one and leave right away
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingAmbientOcclusion)))
            {
                TraceAO(cmd, aoTraceParameters, aoTraceResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterAmbientOcclusion)))
            {
                DenoiseAO(cmd, hdCamera, outputTexture);
            }

            AmbientOcclusionComposeParameters aoComposeParameters = PrepareAmbientOcclusionComposeParameters(hdCamera, globalCB);
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingComposeAmbientOcclusion)))
            {
                ComposeAO(cmd, aoComposeParameters, outputTexture);
            }

            // TODO: All the push-debug stuff should be centralized somewhere
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, outputTexture, FullScreenDebugMode.ScreenSpaceAmbientOcclusion);

        }

        static public void TraceAO(CommandBuffer cmd, AmbientOcclusionTraceParameters aoTraceParameters, AmbientOcclusionTraceResources aoTraceResources)
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

        public void DenoiseAO(CommandBuffer cmd, HDCamera hdCamera, RTHandle outputTexture)
        {
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            if (aoSettings.denoise)
            {
                // Evaluate the history's validity
                float historyValidity = historyValidity = HDRenderPipeline.ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

                // Grab the history buffer
                RTHandle aoHistory = RequestAmbientOcclusionHistoryTexture(hdCamera);

                // Prepare and execute the temporal filter
                HDTemporalFilter temporalFilter = m_RenderPipeline.GetTemporalFilter();
                TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, true, historyValidity);
                RTHandle validationBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.R0);
                TemporalFilterResources tfResources = temporalFilter.PrepareTemporalFilterResources(hdCamera, validationBuffer, m_AOIntermediateBuffer0, aoHistory, m_AOIntermediateBuffer1);
                HDTemporalFilter.DenoiseBuffer(cmd, tfParameters, tfResources);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser diffuseDenoiser = m_RenderPipeline.GetDiffuseDenoiser();
                DiffuseDenoiserParameters ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, true, aoSettings.denoiserRadius, false, false);
                RTHandle intermediateBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
                DiffuseDenoiserResources ddResources = diffuseDenoiser.PrepareDiffuseDenoiserResources(m_AOIntermediateBuffer1, intermediateBuffer, outputTexture);
                HDDiffuseDenoiser.DenoiseBuffer(cmd, ddParams, ddResources);
            }
            else
            {
                HDUtils.BlitCameraTexture(cmd, m_AOIntermediateBuffer0, outputTexture);
            }
        }

        static public void ComposeAO(CommandBuffer cmd, AmbientOcclusionComposeParameters aoComposeParameters, RTHandle outputTexture)
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
