using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingIndirectDiffuse
    {
        // External structures
        HDRenderPipelineAsset m_PipelineAsset = null;
        RenderPipelineResources m_PipelineResources = null;
        SkyManager m_SkyManager = null;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;
        GBufferManager m_GBufferManager = null;

        // Intermediate buffer that stores the indirect diffuse pre-denoising
        RTHandleSystem.RTHandle m_IndirectDiffuseTexture = null;
        RTHandleSystem.RTHandle m_DenoiseBuffer0 = null;

        // String values
        const string m_RayGenIndirectDiffuseName = "RayGenIndirectDiffuse";
        const string m_MissShaderName = "MissShaderIndirectDiffuse";
        const string m_ClosestHitShaderName = "ClosestHitMain";

        public HDRaytracingIndirectDiffuse()
        {
        }

        public void Init(HDRenderPipelineAsset asset, SkyManager skyManager, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager, GBufferManager gbufferManager)
        {
            // Keep track of the pipeline asset
            m_PipelineAsset = asset;
            m_PipelineResources = asset.renderPipelineResources;

            // Keep track of the sky manager
            m_SkyManager = skyManager;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
            m_GBufferManager = gbufferManager;

            m_IndirectDiffuseTexture = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseBuffer");
            m_DenoiseBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseDenoiseBuffer");
        }

        public void Release()
        {
            RTHandles.Release(m_DenoiseBuffer0);
            RTHandles.Release(m_IndirectDiffuseTexture);
        }

        void BindIndirectDiffuseTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseTexture);
        }

        static RTHandleSystem.RTHandle IndirectDiffuseHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("IndirectDiffuseHistoryBuffer{0}", frameIndex));
        }

        public RTHandleSystem.RTHandle GetIndirectDiffuseTexture()
        {
            return m_IndirectDiffuseTexture;
        }

        public bool ValidIndirectDiffuseState()
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironment = m_RaytracingManager.CurrentEnvironment();
            RayTracingShader indirectDiffuseShader = m_PipelineAsset.renderPipelineRayTracingResources.indirectDiffuseRaytracing;
            var settings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();

            return !(rtEnvironment == null || !settings.enableRayTracing.value
                || indirectDiffuseShader == null
                || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null);
        }

        public bool RenderIndirectDiffuse(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Bind the indirect diffuse texture
            BindIndirectDiffuseTexture(cmd);

            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironment = m_RaytracingManager.CurrentEnvironment();
            RayTracingShader indirectDiffuseShader = m_PipelineAsset.renderPipelineRayTracingResources.indirectDiffuseRaytracing;
            ComputeShader indirectDiffuseAccumulation = m_PipelineAsset.renderPipelineRayTracingResources.indirectDiffuseAccumulation;
            var settings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();
            var lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();

            bool invalidState = rtEnvironment == null || !settings.enableRayTracing.value
                || indirectDiffuseShader == null || indirectDiffuseAccumulation == null
                || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null;

            // If no acceleration structure available, end it now
            if (invalidState)
                return false;

            // Grab the acceleration structures and the light cluster to use
            RayTracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironment.indirectDiffuseLayerMask);
            HDRaytracingLightCluster lightCluster = m_RaytracingManager.RequestLightCluster(rtEnvironment.indirectDiffuseLayerMask);

            // Compute the actual resolution that is needed base on the quality
            string targetRayGen = m_RayGenIndirectDiffuseName;

            // Define the shader pass to use for the indirect diffuse pass
            cmd.SetRayTracingShaderPass(indirectDiffuseShader, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(indirectDiffuseShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, settings.rayLength.value);
            cmd.SetRayTracingIntParams(indirectDiffuseShader, HDShaderIDs._RaytracingNumSamples, settings.numSamples.value);
            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._IndirectDiffuseTextureRW, m_IndirectDiffuseTexture);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

            // Set the indirect diffuse parameters
            cmd.SetRayTracingFloatParams(indirectDiffuseShader, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);

            // Set ray count texture
            cmd.SetRayTracingIntParam(indirectDiffuseShader, HDShaderIDs._RayCountEnabled, m_RaytracingManager.rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._RayCountTexture, m_RaytracingManager.rayCountManager.rayCountTexture);

            // Compute the pixel spread value
            float pixelSpreadAngle = Mathf.Atan(2.0f * Mathf.Tan(hdCamera.camera.fieldOfView * Mathf.PI / 360.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.SetRayTracingFloatParam(indirectDiffuseShader, HDShaderIDs._RaytracingPixelSpreadAngle, pixelSpreadAngle);

            // LightLoop data
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());
            cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, lightCluster.GetMinClusterPos());
            cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, lightCluster.GetMaxClusterPos());
            cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, lightClusterSettings.maxNumLightsPercell.value);
            cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, lightCluster.GetPunctualLightCount());
            cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, lightCluster.GetAreaLightCount());

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._SkyTexture, m_SkyManager.skyReflection);

            // Compute the actual resolution that is needed base on the quality
            int widthResolution = hdCamera.actualWidth;
            int heightResolution = hdCamera.actualHeight;

            // Run the computation
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", true);
            cmd.DispatchRays(indirectDiffuseShader, targetRayGen, (uint)widthResolution, (uint)heightResolution, 1);
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", false);

            if(settings.enableFilter.value)
            {
                // Grab the history buffer
                RTHandleSystem.RTHandle indirectDiffuseHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuse)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuse, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                int m_KernelFilter = indirectDiffuseAccumulation.FindKernel("RaytracingIndirectDiffuseTAA");

                // Compute the combined TAA frame
                var historyScale = new Vector2(hdCamera.actualWidth / (float)indirectDiffuseHistory.rt.width, hdCamera.actualHeight / (float)indirectDiffuseHistory.rt.height);
                cmd.SetComputeVectorParam(indirectDiffuseAccumulation, HDShaderIDs._RTHandleScaleHistory, historyScale);
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IndirectDiffuseTexture);
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer0);
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._IndirectDiffuseHistorybufferRW, indirectDiffuseHistory);
                cmd.DispatchCompute(indirectDiffuseAccumulation, m_KernelFilter, numTilesX, numTilesY, 1);

                // Output the new history
                HDUtils.BlitCameraTexture(cmd, m_DenoiseBuffer0, indirectDiffuseHistory);

                m_KernelFilter = indirectDiffuseAccumulation.FindKernel("IndirectDiffuseFilterH");

                // Horizontal pass of the bilateral filter
                cmd.SetComputeIntParam(indirectDiffuseAccumulation, HDShaderIDs._RaytracingDenoiseRadius, settings.filterRadius.value);
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, indirectDiffuseHistory);
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer0);
                cmd.DispatchCompute(indirectDiffuseAccumulation, m_KernelFilter, numTilesX, numTilesY, 1);

                m_KernelFilter = indirectDiffuseAccumulation.FindKernel("IndirectDiffuseFilterV");

                // Horizontal pass of the bilateral filter
                cmd.SetComputeIntParam(indirectDiffuseAccumulation, HDShaderIDs._RaytracingDenoiseRadius, settings.filterRadius.value);
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer0);
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_IndirectDiffuseTexture);
                cmd.DispatchCompute(indirectDiffuseAccumulation, m_KernelFilter, numTilesX, numTilesY, 1);
            }

            // If we are in deferred mode, we need to make sure to add the indirect diffuse (that we intentionally ignored during the GBuffer pass)
            // Note that this discards the texture/object ambient occlusion. But we consider that okay given that the ray traced indirect diffuse
            // is a physically correct evaluation of that quantity
            if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                int indirectDiffuseKernel = indirectDiffuseAccumulation.FindKernel("IndirectDiffuseAccumulation");

                // Bind the source texture
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, indirectDiffuseKernel, HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseTexture);

                // Bind the output texture
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, indirectDiffuseKernel, HDShaderIDs._GBufferTexture[0], m_GBufferManager.GetBuffer(0));
                cmd.SetComputeTextureParam(indirectDiffuseAccumulation, indirectDiffuseKernel, HDShaderIDs._GBufferTexture[3], m_GBufferManager.GetBuffer(3));

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (widthResolution + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (heightResolution + (areaTileSize - 1)) / areaTileSize;

                // Add the indirect diffuse to the GBuffer
                cmd.DispatchCompute(indirectDiffuseAccumulation, indirectDiffuseKernel, numTilesX, numTilesY, 1);
            }

            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_IndirectDiffuseTexture, FullScreenDebugMode.IndirectDiffuse);

            return true;
        }
    }
#endif
}
