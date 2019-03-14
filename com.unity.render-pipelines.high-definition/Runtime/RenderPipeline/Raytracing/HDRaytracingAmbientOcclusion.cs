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
        RenderPipelineSettings m_PipelineSettings;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;

        // The target denoising kernel
        static int m_KernelFilter;

        // Intermediate buffer that stores the ambient occlusion pre-denoising
        RTHandleSystem.RTHandle m_IntermediateBuffer = null;
        RTHandleSystem.RTHandle m_HitDistanceBuffer = null;
        RTHandleSystem.RTHandle m_ViewSpaceNormalBuffer = null;

        // String values
        const string m_RayGenShaderName = "RayGenAmbientOcclusion";
        const string m_MissShaderName = "MissShaderAmbientOcclusion";
        const string m_ClosestHitShaderName = "ClosestHitMain";

        // Shader Identifiers
        public static readonly int _DenoiseRadius = Shader.PropertyToID("_DenoiseRadius");
        public static readonly int _GaussianSigma = Shader.PropertyToID("_GaussianSigma");

        public HDRaytracingAmbientOcclusion()
        {
        }

        public void Init(RenderPipelineResources pipelineResources, RenderPipelineSettings pipelineSettings, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager)
        {
            // Keep track of the pipeline asset
            m_PipelineSettings = pipelineSettings;
            m_PipelineResources = pipelineResources;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            // Intermediate buffer that holds the pre-denoised texture
            m_IntermediateBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "IntermediateAOBuffer");

            // Buffer that holds the average distance of the rays
            m_HitDistanceBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "HitDistanceBuffer");

            // Buffer that holds the uncompressed normal buffer
            m_ViewSpaceNormalBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "ViewSpaceNormalBuffer");
        }

        public void Release()
        {
            RTHandles.Release(m_ViewSpaceNormalBuffer);
            RTHandles.Release(m_HitDistanceBuffer);
            RTHandles.Release(m_IntermediateBuffer);
        }

        public void SetDefaultAmbientOcclusionTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, Texture2D.blackTexture);
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
        }

        public void RenderAO(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle outputTexture, ScriptableRenderContext renderContext, uint frameCount)
        {
            // Let's check all the resources
            HDRaytracingEnvironment rtEnvironement = m_RaytracingManager.CurrentEnvironment();
            ComputeShader bilateralFilter = m_PipelineResources.shaders.jointBilateralFilterCS;
            RaytracingShader aoShader = m_PipelineResources.shaders.aoRaytracing;
            var aoSettings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            // Check if the state is valid for evaluating ambient occlusion
            bool invalidState = rtEnvironement == null
            || bilateralFilter == null || aoShader == null 
            || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null
            || !(hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSAO) && aoSettings.intensity.value > 0f);

            // If any of the previous requirements is missing, the effect is not requested or no acceleration structure, set the default one and leave right away
            if (invalidState)
            {
                SetDefaultAmbientOcclusionTexture(cmd);
                return;
            }

            // Grab the acceleration structure for the target camera
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironement.aoLayerMask);

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(aoShader, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(aoShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            // Inject the ray generation data
            cmd.SetRaytracingFloatParams(aoShader, HDShaderIDs._RaytracingRayBias, rtEnvironement.rayBias);
            cmd.SetRaytracingFloatParams(aoShader, HDShaderIDs._RaytracingRayMaxLength, rtEnvironement.aoRayLength);
            cmd.SetRaytracingIntParams(aoShader, HDShaderIDs._RaytracingNumSamples, rtEnvironement.aoNumSamples);

            // Set the data for the ray generation
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._RaytracingHitDistanceTexture, m_HitDistanceBuffer);
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._RaytracingVSNormalTexture, m_ViewSpaceNormalBuffer);
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._AmbientOcclusionTextureRW, m_IntermediateBuffer);
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Value used to scale the ao intensity
            cmd.SetRaytracingFloatParam(aoShader, HDShaderIDs._RaytracingAOIntensity, aoSettings.intensity.value);

            cmd.SetRaytracingIntParam(aoShader, HDShaderIDs._RayCountEnabled, m_RaytracingManager.rayCountManager.RayCountIsEnabled());
            cmd.SetRaytracingTextureParam(aoShader, m_RayGenShaderName, HDShaderIDs._RayCountTexture, m_RaytracingManager.rayCountManager.rayCountTexture);

            // Run the calculus
            cmd.DispatchRays(aoShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

            using (new ProfilingSample(cmd, "Filter Reflection", CustomSamplerId.RaytracingAmbientOcclusion.GetSampler()))
            {
                switch(rtEnvironement.aoFilterMode)
                {
                    case HDRaytracingEnvironment.AOFilterMode.Nvidia:
                    {
                        cmd.DenoiseAmbientOcclusionTexture(m_IntermediateBuffer, m_HitDistanceBuffer, m_SharedRTManager.GetDepthStencilBuffer(), m_ViewSpaceNormalBuffer, outputTexture, hdCamera.viewMatrix, hdCamera.projMatrix, (uint)rtEnvironement.maxFilterWidthInPixels, rtEnvironement.filterRadiusInMeters, rtEnvironement.normalSharpness, 1.0f, 0.0f);
                    }
                    break;
                    case HDRaytracingEnvironment.AOFilterMode.Bilateral:
                    {
                        m_KernelFilter = bilateralFilter.FindKernel("JointBilateralFilter");
                        // Inject all the parameters for the compute
                        cmd.SetComputeIntParam(bilateralFilter, _DenoiseRadius, rtEnvironement.aoBilateralRadius);
                        cmd.SetComputeFloatParam(bilateralFilter, _GaussianSigma, rtEnvironement.aoBilateralSigma);
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, "_SourceTexture", m_IntermediateBuffer);
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

                        // Set the output slot
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, "_OutputTexture", outputTexture);

                        // Texture dimensions
                        int texWidth = outputTexture.rt.width;
                        int texHeight = outputTexture.rt.width;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Compute the texture
                        cmd.DispatchCompute(bilateralFilter, m_KernelFilter, numTilesX, numTilesY, 1);
                    }
                    break;
                    case HDRaytracingEnvironment.AOFilterMode.None:
                    {
                        HDUtils.BlitCameraTexture(cmd, hdCamera, m_IntermediateBuffer, outputTexture);
                    }
                    break;
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
