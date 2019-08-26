using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
#if ENABLE_RAYTRACING
    class HDRaytracingAmbientOcclusion
    {
        // External structures
        RenderPipelineResources m_PipelineResources = null;
        HDRenderPipelineRayTracingResources m_PipelineRayTracingResources = null;
        RenderPipelineSettings m_PipelineSettings;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;

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

        public void Init(RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rpRTResources, RenderPipelineSettings pipelineSettings, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager)
        {
            // Keep track of the pipeline asset
            m_PipelineSettings = pipelineSettings;
            m_PipelineResources = rpResources;
            m_PipelineRayTracingResources = rpRTResources;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

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
                                        name: string.Format("AmbientOcclusionHistoryBuffer{0}", frameIndex));
        }


        public void SetDefaultAmbientOcclusionTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, TextureXR.GetBlackTexture());
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
        }

        public void RenderAO(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            // Let's check all the resources
            HDRaytracingEnvironment rtEnvironment = m_RaytracingManager.CurrentEnvironment();
            BlueNoise blueNoise = m_RaytracingManager.GetBlueNoiseManager();

            // Check if the state is valid for evaluating ambient occlusion
            bool invalidState = rtEnvironment == null;

            // If any of the previous requirements is missing, the effect is not requested or no acceleration structure, set the default one and leave right away
            if (invalidState)
            {
                SetDefaultAmbientOcclusionTexture(cmd);
                return;
            }

            RayTracingShader aoShader = m_PipelineRayTracingResources.aoRaytracing;
            var aoSettings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            // Grab the acceleration structure for the target camera
            RayTracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironment.aoLayerMask);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(aoShader, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(aoShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray generation data
            cmd.SetRayTracingFloatParams(aoShader, HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);
            cmd.SetRayTracingFloatParams(aoShader, HDShaderIDs._RaytracingRayMaxLength, aoSettings.rayLength.value);
            cmd.SetRayTracingIntParams(aoShader, HDShaderIDs._RaytracingNumSamples, aoSettings.sampleCount.value);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Inject the ray-tracing sampling data
            blueNoise.BindDitheredRNGData8SPP(cmd);
            
            // Value used to scale the ao intensity
            cmd.SetRayTracingFloatParam(aoShader, HDShaderIDs._RaytracingAOIntensity, aoSettings.intensity.value);

            cmd.SetRayTracingIntParam(aoShader, HDShaderIDs._RayCountEnabled, m_RaytracingManager.rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._RayCountTexture, m_RaytracingManager.rayCountManager.GetRayCountTexture());

            // Set the output textures
            cmd.SetRayTracingTextureParam(aoShader, HDShaderIDs._AmbientOcclusionTextureRW, m_AOIntermediateBuffer0);

            // Run the computation
            cmd.DispatchRays(aoShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

            using (new ProfilingSample(cmd, "Filter Ambient Occlusion", CustomSamplerId.RaytracingAmbientOcclusion.GetSampler()))
            {
                if(aoSettings.denoise.value)
                {
                    // Grab the history buffer
                    RTHandle ambientOcclusionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion, AmbientOcclusionHistoryBufferAllocatorFunction, 1);

                    // Apply the temporal denoiser
                    HDTemporalFilter temporalFilter = m_RaytracingManager.GetTemporalFilter();
                    temporalFilter.DenoiseBuffer(cmd, hdCamera, m_AOIntermediateBuffer0, ambientOcclusionHistory, m_AOIntermediateBuffer1);

                    // Apply the diffuse denoiser
                    HDDiffuseDenoiser diffuseDenoiser = m_RaytracingManager.GetDiffuseDenoiser();
                    diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, m_AOIntermediateBuffer1, outputTexture, aoSettings.denoiserRadius.value);
                }
                else
                {
                    HDUtils.BlitCameraTexture(cmd, m_AOIntermediateBuffer0, outputTexture);
                }
            }

            // Bind the textures and the params
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, outputTexture);
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(0f, 0f, 0f, VolumeManager.instance.stack.GetComponent<AmbientOcclusion>().directLightingStrength.value));

            // TODO: All the push-debug stuff should be centralized somewhere
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, outputTexture, FullScreenDebugMode.SSAO);
        }
    }
#endif
}
