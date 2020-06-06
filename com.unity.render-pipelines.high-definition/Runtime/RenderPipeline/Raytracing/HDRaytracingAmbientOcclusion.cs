using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class HDRaytracingAmbientOcclusion
    {
        // External structures
        RenderPipelineResources m_PipelineResources = null;
        HDRenderPipelineRayTracingResources m_PipelineRayTracingResources = null;
        RenderPipelineSettings m_PipelineSettings;
        HDRenderPipeline m_RenderPipeline = null;

        // The target denoising kernel
        static int m_KernelFilter;

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
            m_PipelineSettings = renderPipeline.currentPlatformRenderPipelineSettings;
            m_PipelineResources = renderPipeline.asset.renderPipelineResources;
            m_PipelineRayTracingResources = renderPipeline.asset.renderPipelineRayTracingResources;

            // keep track of the render pipeline
            m_RenderPipeline = renderPipeline;

            // Allocate the intermediate textures
            m_AOIntermediateBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "AOIntermediateBuffer0");
            m_AOIntermediateBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "AOIntermediateBuffer1");
        }

        public void Release()
        {
            RTHandles.Release(m_AOIntermediateBuffer1);
            RTHandles.Release(m_AOIntermediateBuffer0);
        }

        static RTHandle AmbientOcclusionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("{0}_AmbientOcclusionHistoryBuffer{1}", viewName, frameIndex));
        }


        public void SetDefaultAmbientOcclusionTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, TextureXR.GetBlackTexture());
        }

        public void RenderAO(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ShaderVariablesRaytracing globalCB, ScriptableRenderContext renderContext, int frameCount)
        {
            // If any of the previous requirements is missing, the effect is not requested or no acceleration structure, set the default one and leave right away
            if (!m_RenderPipeline.GetRayTracingState())
            {
                SetDefaultAmbientOcclusionTexture(cmd);
                return;
            }

            RayTracingShader aoShader = m_PipelineRayTracingResources.aoRaytracing;
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            RayCountManager rayCountManager = m_RenderPipeline.GetRayCountManager();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingAmbientOcclusion)))
            {
                // Grab the acceleration structure for the target camera
                RayTracingAccelerationStructure accelerationStructure = m_RenderPipeline.RequestAccelerationStructure();

                // Define the shader pass to use for the reflection pass
                cmd.SetRayTracingShaderPass(aoShader, "VisibilityDXR");

                // Set the acceleration structure for the pass
                cmd.SetRayTracingAccelerationStructure(aoShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                // Inject the ray generation data (be careful of the global constant buffer limitation)
                globalCB._RaytracingRayMaxLength = aoSettings.rayLength.value;
                globalCB._RaytracingNumSamples = aoSettings.sampleCount.value;
                ConstantBuffer.PushGlobal(cmd, globalCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Set the data for the ray generation
                cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._DepthTexture, m_RenderPipeline.sharedRTManager.GetDepthStencilBuffer());
                cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._NormalBufferTexture, m_RenderPipeline.sharedRTManager.GetNormalBuffer());

                // Inject the ray-tracing sampling data
                BlueNoise blueNoise = m_RenderPipeline.GetBlueNoiseManager();
                blueNoise.BindDitheredRNGData8SPP(cmd);

                // Value used to scale the ao intensity
                cmd.SetRayTracingFloatParam(aoShader, HDShaderIDs._RaytracingAOIntensity, aoSettings.intensity.value);

                // Set the output textures
                cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());
                cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._AmbientOcclusionTextureRW, m_AOIntermediateBuffer0);

                // Run the computation
                cmd.DispatchRays(aoShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterAmbientOcclusion)))
            {
                if(aoSettings.denoise.value)
                {
                    // Grab the history buffer
                    RTHandle ambientOcclusionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion, AmbientOcclusionHistoryBufferAllocatorFunction, 1);

                        float historyValidity = 1.0f;
                    #if UNITY_HDRP_DXR_TESTS_DEFINE
                        if (Application.isPlaying)
                            historyValidity = 0.0f;
                        else
                    #endif
                            // We need to check if something invalidated the history buffers
                            historyValidity = m_RenderPipeline.ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

                    // Apply the temporal denoiser
                    HDTemporalFilter temporalFilter = m_RenderPipeline.GetTemporalFilter();
                    temporalFilter.DenoiseBuffer(cmd, hdCamera, m_AOIntermediateBuffer0, ambientOcclusionHistory, m_AOIntermediateBuffer1, historyValidity: historyValidity);

                    // Apply the diffuse denoiser
                    HDDiffuseDenoiser diffuseDenoiser = m_RenderPipeline.GetDiffuseDenoiser();
                    diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, m_AOIntermediateBuffer1, outputTexture, aoSettings.denoiserRadius.value);
                }
                else
                {
                    HDUtils.BlitCameraTexture(cmd, m_AOIntermediateBuffer0, outputTexture);
                }
            }

            // Bind the textures and the params
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, outputTexture);

            // TODO: All the push-debug stuff should be centralized somewhere
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, outputTexture, FullScreenDebugMode.SSAO);
        }
    }
}
