using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingReflections
    {
        // External structures
        HDRenderPipelineAsset m_PipelineAsset = null;
        RenderPipelineResources m_PipelineResources = null;
        SkyManager m_SkyManager = null;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;
        GBufferManager m_GbufferManager = null;

        // Intermediate buffer that stores the reflection pre-denoising
        RTHandleSystem.RTHandle m_LightingTexture = null;
        RTHandleSystem.RTHandle m_HitPdfTexture = null;
        RTHandleSystem.RTHandle m_VarianceBuffer = null;
        RTHandleSystem.RTHandle m_MinBoundBuffer = null;
        RTHandleSystem.RTHandle m_MaxBoundBuffer = null;

        // String values
        const string m_RayGenHalfResName = "RayGenHalfRes";
        const string m_RayGenIntegrationName = "RayGenIntegration";
        const string m_MissShaderName = "MissShaderReflections";

        public HDRaytracingReflections()
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
            m_GbufferManager = gbufferManager;

            m_LightingTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, xrInstancing: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "LightingBuffer");
            m_HitPdfTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, xrInstancing: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "HitPdfBuffer");
            m_VarianceBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8_UNorm, enableRandomWrite: true, xrInstancing: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "VarianceBuffer");
            m_MinBoundBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite: true, xrInstancing: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "MinBoundBuffer");
            m_MaxBoundBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite: true, xrInstancing: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "MaxBoundBuffer");
        }

        static RTHandleSystem.RTHandle ReflectionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false, xrInstancing: true,
                                        name: string.Format("ReflectionHistoryBuffer{0}", frameIndex));
        }

        public void Release()
        {
            RTHandles.Release(m_MinBoundBuffer);
            RTHandles.Release(m_MaxBoundBuffer);
            RTHandles.Release(m_VarianceBuffer);
            RTHandles.Release(m_HitPdfTexture);
            RTHandles.Release(m_LightingTexture);
        }

        public void RenderReflections(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle outputTexture, ScriptableRenderContext renderContext, uint frameCount)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironement = m_RaytracingManager.CurrentEnvironment();
            LightLoop lightLoop = m_RaytracingManager.GetLightLoop();
            BlueNoise blueNoise = m_RaytracingManager.GetBlueNoiseManager();
            ComputeShader reflectionFilter = m_PipelineAsset.renderPipelineResources.shaders.reflectionBilateralFilterCS;
            RaytracingShader reflectionShader = m_PipelineAsset.renderPipelineResources.shaders.reflectionRaytracing;

            bool invalidState = rtEnvironement == null || blueNoise == null
                || reflectionFilter == null || reflectionShader == null 
                || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null;

            // If no acceleration structure available, end it now
            if (invalidState)
                return;

            // Grab the acceleration structures and the light cluster to use
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironement.reflLayerMask);
            HDRaytracingLightCluster lightCluster = m_RaytracingManager.RequestLightCluster(rtEnvironement.reflLayerMask);

            // Compute the actual resolution that is needed base on the quality
            string targetRayGen = "";
            switch (rtEnvironement.reflQualityMode)
            {
                case HDRaytracingEnvironment.ReflectionsQuality.QuarterRes:
                {
                    targetRayGen = m_RayGenHalfResName;
                };
                break;
                case HDRaytracingEnvironment.ReflectionsQuality.Integration:
                {
                    targetRayGen = m_RayGenIntegrationName;
                };
                break;
            }

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(reflectionShader, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(reflectionShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            // Global reflection parameters
            cmd.SetRaytracingFloatParams(reflectionShader, HDShaderIDs._RaytracingIntensityClamp, rtEnvironement.reflClampValue);
            cmd.SetRaytracingFloatParams(reflectionShader, HDShaderIDs._RaytracingReflectionMinSmoothness, rtEnvironement.reflMinSmoothness);
            cmd.SetRaytracingFloatParams(reflectionShader, HDShaderIDs._RaytracingReflectionMaxDistance, rtEnvironement.reflBlendDistance);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironement.rayBias);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, rtEnvironement.reflRayLength);
            cmd.SetRaytracingIntParams(reflectionShader, HDShaderIDs._RaytracingNumSamples, rtEnvironement.reflNumMaxSamples);
            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Set the data for the ray generation
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._SsrLightingTextureRW, m_LightingTexture);
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._SsrHitPointTexture, m_HitPdfTexture);
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

            // Set ray count tex
            cmd.SetRaytracingIntParam(reflectionShader, HDShaderIDs._RayCountEnabled, m_RaytracingManager.rayCountManager.RayCountIsEnabled());
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._RayCountTexture, m_RaytracingManager.rayCountManager.rayCountTexture);

            // Compute the pixel spread value
            float pixelSpreadAngle = Mathf.Atan(2.0f * Mathf.Tan(hdCamera.camera.fieldOfView * Mathf.PI / 360.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.SetRaytracingFloatParam(reflectionShader, HDShaderIDs._RaytracingPixelSpreadAngle, pixelSpreadAngle);

            // LightLoop data
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());
            cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, lightCluster.GetMinClusterPos());
            cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, lightCluster.GetMaxClusterPos());
            cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, rtEnvironement.maxNumLightsPercell);
            cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, lightCluster.GetPunctualLightCount());
            cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, lightCluster.GetAreaLightCount());

            // Note: Just in case, we rebind the directional light data (in case they were not)
            cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, lightLoop.directionalLightDatas);
            cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, lightLoop.m_lightList.directionalLights.Count);

            // Evaluate the clear coat mask texture based on the lit shader mode
            RenderTargetIdentifier clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : Texture2D.blackTexture;
            cmd.SetRaytracingTextureParam(reflectionShader, targetRayGen, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMaskTexture);

            // Set the data for the ray miss
            cmd.SetRaytracingTextureParam(reflectionShader, m_MissShaderName, HDShaderIDs._SkyTexture, m_SkyManager.skyReflection);

            // Compute the actual resolution that is needed base on the quality
            uint widthResolution = 1, heightResolution = 1;
            switch (rtEnvironement.reflQualityMode)
            {
                case HDRaytracingEnvironment.ReflectionsQuality.QuarterRes:
                {
                    widthResolution = (uint)hdCamera.actualWidth / 2;
                    heightResolution = (uint)hdCamera.actualHeight / 2;
                };
                break;
                case HDRaytracingEnvironment.ReflectionsQuality.Integration:
                {
                    widthResolution = (uint)hdCamera.actualWidth;
                    heightResolution = (uint)hdCamera.actualHeight;
                };
                break;
            }

            // Force to disable specular lighting
            cmd.SetGlobalInt(HDShaderIDs._EnableSpecularLighting, 0);

            // Run the calculus
            cmd.DispatchRays(reflectionShader, targetRayGen, widthResolution, heightResolution, 1);

            // Restore the previous state of specular lighting
            cmd.SetGlobalInt(HDShaderIDs._EnableSpecularLighting, hdCamera.frameSettings.IsEnabled(FrameSettingsField.SpecularLighting) ? 1 : 0);

            using (new ProfilingSample(cmd, "Filter Reflection", CustomSamplerId.RaytracingFilterReflection.GetSampler()))
            {
                switch (rtEnvironement.reflQualityMode)
                {
                    case HDRaytracingEnvironment.ReflectionsQuality.QuarterRes:
                    {
                        // Fetch the right filter to use
                        int currentKernel = reflectionFilter.FindKernel("RaytracingReflectionFilter");

                        // Inject all the parameters for the compute
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrLightingTextureRW, m_LightingTexture);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrHitPointTexture, m_HitPdfTexture);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_NoiseTexture", blueNoise.textureArray16RGB);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_VarianceTexture", m_VarianceBuffer);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_MinColorRangeTexture", m_MinBoundBuffer);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_MaxColorRangeTexture", m_MaxBoundBuffer);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_RaytracingReflectionTexture", outputTexture);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);
                        cmd.SetComputeIntParam(reflectionFilter, HDShaderIDs._SpatialFilterRadius, rtEnvironement.reflSpatialFilterRadius);
                        cmd.SetComputeFloatParam(reflectionFilter, HDShaderIDs._RaytracingReflectionMinSmoothness, rtEnvironement.reflMinSmoothness);

                        // Texture dimensions
                        int texWidth = outputTexture.rt.width ;
                        int texHeight = outputTexture.rt.height;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesXHR = (texWidth  + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Bind the right texture for clear coat support
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMaskTexture);
                        
                        // Compute the texture
                        cmd.DispatchCompute(reflectionFilter, currentKernel, numTilesXHR, numTilesYHR, 1);
                        
                        int numTilesXFR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYFR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        RTHandleSystem.RTHandle history = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                            ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);
                        
                        // Fetch the right filter to use
                        currentKernel = reflectionFilter.FindKernel("TemporalAccumulationFilter");
                        cmd.SetComputeFloatParam(reflectionFilter, HDShaderIDs._TemporalAccumuationWeight, rtEnvironement.reflTemporalAccumulationWeight);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._AccumulatedFrameTexture, history);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._CurrentFrameTexture, outputTexture);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_MinColorRangeTexture", m_MinBoundBuffer);
                        cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_MaxColorRangeTexture", m_MaxBoundBuffer);
                        cmd.DispatchCompute(reflectionFilter, currentKernel, numTilesXFR, numTilesYFR, 1);
                    }
                    break;
                    case HDRaytracingEnvironment.ReflectionsQuality.Integration:
                    {
                            switch (rtEnvironement.reflFilterMode)
                            {
                                case HDRaytracingEnvironment.ReflectionsFilterMode.SpatioTemporal:
                                {
                                    // Grab the history buffer
                                    RTHandleSystem.RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                                    // Texture dimensions
                                    int texWidth = hdCamera.actualWidth;
                                    int texHeight = hdCamera.actualHeight;

                                    // Evaluate the dispatch parameters
                                    int areaTileSize = 8;
                                    int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                                    int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                                    int m_KernelFilter = reflectionFilter.FindKernel("RaytracingReflectionTAA");

                                    // Compute the combined TAA frame
                                    var historyScale = new Vector2(hdCamera.actualWidth / (float)reflectionHistory.rt.width, hdCamera.actualHeight / (float)reflectionHistory.rt.height);
                                    cmd.SetComputeVectorParam(reflectionFilter, HDShaderIDs._ScreenToTargetScaleHistory, historyScale);
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_LightingTexture);
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_HitPdfTexture);
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._ReflectionHistorybufferRW, reflectionHistory);
                                    cmd.DispatchCompute(reflectionFilter, m_KernelFilter, numTilesX, numTilesY, 1);

                                    // Output the new history
                                    HDUtils.BlitCameraTexture(cmd, hdCamera, m_HitPdfTexture, reflectionHistory);

                                    m_KernelFilter = reflectionFilter.FindKernel("ReflBilateralFilterH");

                                    // Horizontal pass of the bilateral filter
                                    cmd.SetComputeIntParam(reflectionFilter, HDShaderIDs._RaytracingDenoiseRadius, rtEnvironement.reflFilterRadius);
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, reflectionHistory);
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_HitPdfTexture);
                                    cmd.DispatchCompute(reflectionFilter, m_KernelFilter, numTilesX, numTilesY, 1);

                                    m_KernelFilter = reflectionFilter.FindKernel("ReflBilateralFilterV");

                                    // Horizontal pass of the bilateral filter
                                    cmd.SetComputeIntParam(reflectionFilter, HDShaderIDs._RaytracingDenoiseRadius, rtEnvironement.reflFilterRadius);
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_HitPdfTexture);
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                                    cmd.SetComputeTextureParam(reflectionFilter, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputTexture);
                                    cmd.DispatchCompute(reflectionFilter, m_KernelFilter, numTilesX, numTilesY, 1);
                                }
                                break;
                                case HDRaytracingEnvironment.ReflectionsFilterMode.None:
                                {
                                    HDUtils.BlitCameraTexture(cmd, hdCamera, m_LightingTexture, outputTexture);
                                }
                                break;
                            }
                    }
                    break;
                }
            }
        }
    }
#endif
}
