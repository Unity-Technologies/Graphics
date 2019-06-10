using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingAmbientOcclusion
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
        RTHandleSystem.RTHandle m_IntermediateBuffer = null;
        RTHandleSystem.RTHandle m_ViewSpaceNormalBuffer = null;

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

            // Intermediate buffer that holds the pre-denoised texture
            m_IntermediateBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateAOBuffer");

            // Buffer that holds the uncompressed normal buffer
            m_ViewSpaceNormalBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "ViewSpaceNormalBuffer");
        }

        public void Release()
        {
            RTHandles.Release(m_ViewSpaceNormalBuffer);
            RTHandles.Release(m_IntermediateBuffer);
        }

        static RTHandleSystem.RTHandle AmbientOcclusionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("AmbientOcclusionHistoryBuffer{0}", frameIndex));
        }


        public void SetDefaultAmbientOcclusionTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, TextureXR.GetBlackTexture());
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
        }

        public void RenderAO(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            // Let's check all the resources
            HDRaytracingEnvironment rtEnvironment = m_RaytracingManager.CurrentEnvironment();
            ComputeShader aoFilter = m_PipelineRayTracingResources.raytracingAOFilterCS;
            RaytracingShader aoShader = m_PipelineRayTracingResources.aoRaytracing;
            var aoSettings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            // Check if the state is valid for evaluating ambient occlusion
            bool invalidState = rtEnvironment == null
            || aoFilter == null || aoShader == null
            || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null;

            // If any of the previous requirements is missing, the effect is not requested or no acceleration structure, set the default one and leave right away
            if (invalidState)
            {
                SetDefaultAmbientOcclusionTexture(cmd);
                return;
            }

            // Grab the acceleration structure for the target camera
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironment.aoLayerMask);

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(aoShader, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(aoShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRaytracingTextureParam(aoShader, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRaytracingTextureParam(aoShader, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            // Inject the ray generation data
            cmd.SetRaytracingFloatParams(aoShader, HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);
            cmd.SetRaytracingFloatParams(aoShader, HDShaderIDs._RaytracingRayMaxLength, aoSettings.rayLength.value);
            cmd.SetRaytracingIntParams(aoShader, HDShaderIDs._RaytracingNumSamples, aoSettings.numSamples.value);

            // Set the data for the ray generation
            cmd.SetRaytracingTextureParam(aoShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRaytracingTextureParam(aoShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Value used to scale the ao intensity
            cmd.SetRaytracingFloatParam(aoShader, HDShaderIDs._RaytracingAOIntensity, aoSettings.intensity.value);

            cmd.SetRaytracingIntParam(aoShader, HDShaderIDs._RayCountEnabled, m_RaytracingManager.rayCountManager.RayCountIsEnabled());
            cmd.SetRaytracingTextureParam(aoShader, HDShaderIDs._RayCountTexture, m_RaytracingManager.rayCountManager.rayCountTexture);

            // Set the output textures
            cmd.SetRaytracingTextureParam(aoShader, HDShaderIDs._AmbientOcclusionTextureRW, m_IntermediateBuffer);
            cmd.SetRaytracingTextureParam(aoShader, HDShaderIDs._RaytracingVSNormalTexture, m_ViewSpaceNormalBuffer);

            // Run the computation
            cmd.DispatchRays(aoShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

            using (new ProfilingSample(cmd, "Filter Reflection", CustomSamplerId.RaytracingAmbientOcclusion.GetSampler()))
            {
                if(aoSettings.enableFilter.value)
                {
                    // Grab the history buffer
                    RTHandleSystem.RTHandle ambientOcclusionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion, AmbientOcclusionHistoryBufferAllocatorFunction, 1);

                    // Texture dimensions
                    int texWidth = hdCamera.actualWidth;
                    int texHeight = hdCamera.actualHeight;

                    // Evaluate the dispatch parameters
                    int areaTileSize = 8;
                    int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                    int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                    m_KernelFilter = aoFilter.FindKernel("AOApplyTAA");

                    // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
                    var historyScale = new Vector2(hdCamera.actualWidth / (float)ambientOcclusionHistory.rt.width, hdCamera.actualHeight / (float)ambientOcclusionHistory.rt.height);
                    cmd.SetComputeVectorParam(aoFilter, HDShaderIDs._RTHandleScaleHistory, historyScale);

                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer);
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._AOHistorybuffer, ambientOcclusionHistory);
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_ViewSpaceNormalBuffer);
                    cmd.DispatchCompute(aoFilter, m_KernelFilter, numTilesX, numTilesY, 1);

                    // Output the new history
                    m_KernelFilter = aoFilter.FindKernel("AOCopyHistory");

                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_ViewSpaceNormalBuffer);
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, ambientOcclusionHistory);
                    cmd.DispatchCompute(aoFilter, m_KernelFilter, numTilesX, numTilesY, 1);

                    m_KernelFilter = aoFilter.FindKernel("AOBilateralFilterH");

                    // Horizontal pass of the bilateral filter
                    cmd.SetComputeIntParam(aoFilter, HDShaderIDs._RaytracingDenoiseRadius, aoSettings.filterRadius.value);
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_ViewSpaceNormalBuffer);
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer);
                    cmd.DispatchCompute(aoFilter, m_KernelFilter, numTilesX, numTilesY, 1);

                    m_KernelFilter = aoFilter.FindKernel("AOBilateralFilterV");

                    // Horizontal pass of the bilateral filter
                    cmd.SetComputeIntParam(aoFilter, HDShaderIDs._RaytracingDenoiseRadius, aoSettings.filterRadius.value);
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer);
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(aoFilter, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputTexture);
                    cmd.DispatchCompute(aoFilter, m_KernelFilter, numTilesX, numTilesY, 1);
                }
                else
                {
                    HDUtils.BlitCameraTexture(cmd, m_IntermediateBuffer, outputTexture);
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
