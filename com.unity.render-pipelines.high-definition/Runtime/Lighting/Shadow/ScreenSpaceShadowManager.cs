using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public partial class HDRenderPipeline
    {
#if ENABLE_RAYTRACING
        // String values
        const string m_RayGenAreaShadowName = "RayGenAreaShadows";
        const string m_RayGenAreaShadowSingleName = "RayGenAreaShadowSingle";
        const string m_RayGenDirectionalShadowSingleName = "RayGenDirectionalShadowSingle";
        const string m_MissShaderName = "MissShaderShadows";

        // Buffers used to evaluate the area ray traced shadows
        RTHandle m_AnalyticProbBuffer;
        RTHandle m_DenoiseBuffer0;
        RTHandle m_DenoiseBuffer1;
        RTHandle m_RaytracingDirectionBuffer;
        RTHandle m_RaytracingDistanceBuffer;

        // Temporary variable that allows us to store the world to local matrix of the lights
        Matrix4x4 worldToLocalArea = new Matrix4x4();
#endif
        static Material s_ScreenSpaceShadowsMat;

        // Array that holds the shadow textures for the area lights
        RTHandle m_ScreenSpaceShadowTextureArray;

        public void InitializeScreenSpaceShadows()
        {
            if (!m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows)
                return;

#if ENABLE_RAYTRACING
            // Allocate the intermediate buffers
            m_AnalyticProbBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AnalyticProbBuffer");
            m_DenoiseBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true,  useMipMap: false, name: "DenoiseBuffer0");
            m_DenoiseBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "DenoiseBuffer1");
            m_RaytracingDirectionBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true,useMipMap: false, name: "RaytracingDirectionBuffer");
            m_RaytracingDistanceBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RaytracingDistanceBuffer");
#endif
            s_ScreenSpaceShadowsMat = CoreUtils.CreateEngineMaterial(screenSpaceShadowsShader);

            // Allocate the final result texture
            int numMaxShadows = Math.Max(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows, 1);
            m_ScreenSpaceShadowTextureArray = RTHandles.Alloc(Vector2.one, slices: numMaxShadows * TextureXR.slices, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AreaShadowArrayBuffer");
        }

#if ENABLE_RAYTRACING
        public RTHandleSystem.RTHandle GetIntegrationTexture()
        {
            return m_DenoiseBuffer0;
        }
#endif

        public void ReleaseScreenSpaceShadows()
        {
            RTHandles.Release(m_ScreenSpaceShadowTextureArray);

            CoreUtils.Destroy(s_ScreenSpaceShadowsMat);

#if ENABLE_RAYTRACING
            RTHandles.Release(m_RaytracingDistanceBuffer);
            RTHandles.Release(m_RaytracingDirectionBuffer);
            RTHandles.Release(m_DenoiseBuffer1);
            RTHandles.Release(m_DenoiseBuffer0);
            RTHandles.Release(m_AnalyticProbBuffer);
#endif
        }

        void BindBlackShadowTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, TextureXR.GetBlackTexture());
        }

        static RTHandleSystem.RTHandle ShadowHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, slices:4 * TextureXR.slices, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16_SFloat,
                enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("AreaShadowHistoryBuffer{0}", frameIndex));
        }

        static RTHandleSystem.RTHandle AreaAnalyticHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, slices:4 * TextureXR.slices, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16_SFloat,
                        enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("AreaShadowHistoryBuffer{0}", frameIndex));
        }

        public void RenderScreenSpaceShadows(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows))
            {
                BindBlackShadowTexture(cmd);
                return;
            }

#if ENABLE_RAYTRACING
            using (new ProfilingSample(cmd, "Screen Space Shadows", CustomSamplerId.TPScreenSpaceShadows.GetSampler()))
            {
                RenderDirectionalLightScreenSpaceShadow(cmd, hdCamera, m_FrameCount);
                RenderAreaShadows(hdCamera, cmd, m_FrameCount);
                EvaluateShadowDebugView(cmd, hdCamera);
                cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, m_ScreenSpaceShadowTextureArray);
            }
#else
            BindBlackShadowTexture(cmd);
#endif
        }

        public void RenderDirectionalLightScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera, int frameCount)
        {
            // Render directional screen space shadow if required
            if (m_CurrentSunLightAdditionalLightData != null && m_CurrentSunLightAdditionalLightData.WillRenderScreenSpaceShadow())
            {
#if ENABLE_RAYTRACING
                // If the shadow is flagged as ray traced, we need to evaluate it completely
                if (m_CurrentSunLightAdditionalLightData.WillRenderRayTracedShadow())
                {
                    HDRaytracingEnvironment rtEnvironment = m_RayTracingManager.CurrentEnvironment();
                    ComputeShader shadowsCompute = m_Asset.renderPipelineRayTracingResources.shadowRaytracingCS;
                    RayTracingShader shadowRayTrace = m_Asset.renderPipelineRayTracingResources.shadowRaytracingRT;

                    // Texture dimensions
                    int texWidth = hdCamera.actualWidth;
                    int texHeight = hdCamera.actualHeight;

                    // Evaluate the dispatch parameters
                    int areaTileSize = 8;
                    int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                    int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                    int shadowComputeKernel = shadowsCompute.FindKernel("ClearShadowTexture");
                    cmd.SetComputeBufferParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                    cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_DenoiseBuffer0);
                    cmd.DispatchCompute(shadowsCompute, shadowComputeKernel, numTilesX, numTilesY, 1);

                    cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_Asset.renderPipelineResources.textures.owenScrambledTex);
                    cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);
            
                    for(int i = 0; i < m_CurrentSunLightAdditionalLightData.numRayTracingSamples; ++i)
                    {
                        shadowComputeKernel = shadowsCompute.FindKernel("RaytracingDirectionalShadowSample");

                        int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;

                        // This pass evaluates the analytic value and the generates and outputs the first sample
                        cmd.SetComputeBufferParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                        cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingFrameIndex, frameIndex);
                        cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingNumSamples, m_CurrentSunLightAdditionalLightData.numRayTracingSamples);
                        cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingSampleIndex, i);
                        cmd.SetComputeMatrixParam(shadowsCompute, HDShaderIDs._RaytracingAreaWorldToLocal, worldToLocalArea);
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_DenoiseBuffer0);
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                        cmd.SetComputeFloatParam(shadowsCompute, HDShaderIDs._DirectionalLightAngle, m_CurrentSunLightAdditionalLightData.sunLightConeAngle);

                        cmd.DispatchCompute(shadowsCompute, shadowComputeKernel, numTilesX, numTilesY, 1);

                        // Grab the acceleration structure for the target camera
                        RayTracingAccelerationStructure accelerationStructure = m_RayTracingManager.RequestAccelerationStructure(rtEnvironment.shadowLayerMask);

                        // Define the shader pass to use for the reflection pass
                        cmd.SetRayTracingShaderPass(shadowRayTrace, "VisibilityDXR");

                        // Set the acceleration structure for the pass
                        cmd.SetRayTracingAccelerationStructure(shadowRayTrace, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                        // This pass will use the previously generated sample and add it to the integration buffer
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_DenoiseBuffer0);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                        cmd.SetRayTracingFloatParam(shadowRayTrace, HDShaderIDs._DirectionalLightAngle, m_CurrentSunLightAdditionalLightData.sunLightConeAngle);
                        cmd.SetRayTracingIntParam(shadowRayTrace, HDShaderIDs._RaytracingNumSamples, 1);
                        cmd.DispatchRays(shadowRayTrace, m_RayGenDirectionalShadowSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
                    }

                    RTHandleSystem.RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow, ShadowHistoryBufferAllocatorFunction, 1);

                    // Apply the simple denoiser (if required)
                    if (m_CurrentSunLightAdditionalLightData.filterTracedShadow)
                    {
                        HDSimpleDenoiser simpleDenoiser = m_RayTracingManager.GetSimpleDenoiser();
                        simpleDenoiser.DenoiseBuffer(cmd, hdCamera, m_DenoiseBuffer0, shadowHistoryArray, m_DenoiseBuffer1, m_CurrentSunLightAdditionalLightData.filterSizeTraced, slotIndex: m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex);
                    }
                    else
                    {
                        HDUtils.BlitCameraTexture(cmd, m_DenoiseBuffer0, m_DenoiseBuffer1);
                    }

                    shadowComputeKernel = shadowsCompute.FindKernel("OutputShadowTexture");
                    cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_DenoiseBuffer1);
                    cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                    cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingShadowSlot, m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex);
                    cmd.DispatchCompute(shadowsCompute, shadowComputeKernel, numTilesX, numTilesY, 1);
                }
                else
#endif
                {
                    // If it is screen space but not ray traced, then we can rely on the shadow map
                    HDUtils.SetRenderTarget(cmd, m_ScreenSpaceShadowTextureArray, depthSlice: m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex);
                    HDUtils.DrawFullScreen(cmd, s_ScreenSpaceShadowsMat, m_ScreenSpaceShadowTextureArray);
                }
            }
        }
#if ENABLE_RAYTRACING
        public bool RenderAreaShadows(HDCamera hdCamera, CommandBuffer cmd, int frameCount)
        {
            // Let's check all the resources and states to see if we should render the effect
            HDRaytracingEnvironment rtEnvironment = m_RayTracingManager.CurrentEnvironment();

            RayTracingShader shadowRayTrace = m_Asset.renderPipelineRayTracingResources.shadowRaytracingRT;
            ComputeShader shadowsCompute = m_Asset.renderPipelineRayTracingResources.shadowRaytracingCS;
            ComputeShader shadowFilter = m_Asset.renderPipelineRayTracingResources.shadowFilterCS;

            // Make sure everything is valid
            bool invalidState = rtEnvironment == null ||
					hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred ||
                    shadowRayTrace == null || shadowsCompute == null || shadowFilter == null ||
                    m_Asset.renderPipelineResources.textures.owenScrambledTex == null || m_Asset.renderPipelineResources.textures.scramblingTex == null;

            // If invalid state or ray-tracing acceleration structure, we stop right away
            if (invalidState)
                return false;

            // Grab the TAA history buffers (SN/UN and Analytic value)
            RTHandleSystem.RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow, ShadowHistoryBufferAllocatorFunction, 1);
            RTHandleSystem.RTHandle areaAnalyticHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAreaAnalytic)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAreaAnalytic, AreaAnalyticHistoryBufferAllocatorFunction, 1);

            // Grab the acceleration structure for the target camera
            RayTracingAccelerationStructure accelerationStructure = m_RayTracingManager.RequestAccelerationStructure(rtEnvironment.shadowLayerMask);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(shadowRayTrace, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(shadowRayTrace, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_Asset.renderPipelineResources.textures.owenScrambledTex);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Temporal Filtering kernels
            int applyTAAKernel       = shadowFilter.FindKernel("AreaShadowApplyTAA");
            int updateAnalyticHistory = shadowFilter.FindKernel("AreaAnalyticHistoryCopy");
            int updateShadowHistory = shadowFilter.FindKernel("AreaShadowHistoryCopy");

            // Spatial Filtering kernels
            int estimateNoiseKernel  = shadowFilter.FindKernel("AreaShadowEstimateNoise");
            int firstDenoiseKernel   = shadowFilter.FindKernel("AreaShadowDenoiseFirstPass");
            int secondDenoiseKernel  = shadowFilter.FindKernel("AreaShadowDenoiseSecondPass");

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);

            int numLights = m_lightList.lights.Count;

            for(int lightIdx = 0; lightIdx < numLights; ++lightIdx)
            {
                // If this is not a rectangular area light or it won't have shadows, skip it
                if (m_lightList.lights[lightIdx].lightType != GPULightType.Rectangle || m_lightList.lights[lightIdx].screenSpaceShadowIndex == -1) continue;

                LightData currentLight = m_lightList.lights[lightIdx];
                HDAdditionalLightData currentAdditionalLightData = GetCurrentRayTracedShadow(currentLight.screenSpaceShadowIndex);

                using (new ProfilingSample(cmd, "Ray Traced Area Shadow", CustomSamplerId.RaytracingShadowIntegration.GetSampler()))
                {
                    // We need to build the world to area light matrix
                    worldToLocalArea.SetColumn(0, currentLight.right);
                    worldToLocalArea.SetColumn(1, currentLight.up);
                    worldToLocalArea.SetColumn(2, currentLight.forward);

                    // Compensate the  relative rendering if active
                    Vector3 lightPositionWS = currentLight.positionRWS;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        lightPositionWS += hdCamera.camera.transform.position;
                    }
                    worldToLocalArea.SetColumn(3, lightPositionWS);
                    worldToLocalArea.m33 = 1.0f;
                    worldToLocalArea =  worldToLocalArea.inverse;

                    // We have noticed from extensive profiling that ray-trace shaders are not as effective for running per-pixel computation. In order to reduce that,
                    // we do a first prepass that compute the analytic term and probability and generates the first integration sample
                    if(true)
                    {
                        int shadowComputeKernel = shadowsCompute.FindKernel("RaytracingAreaShadowPrepass");

                        // This pass evaluates the analytic value and the generates and outputs the first sample
                        cmd.SetComputeBufferParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                        cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingTargetAreaLight, lightIdx);
                        cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingNumSamples, currentAdditionalLightData.numRayTracingSamples);
                        cmd.SetComputeMatrixParam(shadowsCompute, HDShaderIDs._RaytracingAreaWorldToLocal, worldToLocalArea);
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._AreaCookieTextures, m_TextureCaches.areaLightCookieManager.GetTexCache());
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracedAreaShadowIntegration, m_DenoiseBuffer0);
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracedAreaShadowSample, m_DenoiseBuffer1);
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                        cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                        cmd.DispatchCompute(shadowsCompute, shadowComputeKernel, numTilesX, numTilesY, 1);

                        // This pass will use the previously generated sample and add it to the integration buffer
                        cmd.SetRayTracingBufferParam(shadowRayTrace, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracedAreaShadowSample, m_DenoiseBuffer1);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracedAreaShadowIntegration, m_DenoiseBuffer0);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                        cmd.DispatchRays(shadowRayTrace, m_RayGenAreaShadowSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

                        // Let's do the following samples (if any)
                        for(int sampleIndex = 1; sampleIndex < currentAdditionalLightData.numRayTracingSamples; ++sampleIndex)
                        {
                            shadowComputeKernel = shadowsCompute.FindKernel("RaytracingAreaShadowNewSample");

                            // This pass generates a new sample based on the initial pre-pass
                            cmd.SetComputeBufferParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                            cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingTargetAreaLight, lightIdx);
                            cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingNumSamples, currentAdditionalLightData.numRayTracingSamples);
                            cmd.SetComputeIntParam(shadowsCompute, HDShaderIDs._RaytracingSampleIndex, sampleIndex);
                            cmd.SetComputeMatrixParam(shadowsCompute, HDShaderIDs._RaytracingAreaWorldToLocal, worldToLocalArea);
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._AreaCookieTextures, m_TextureCaches.areaLightCookieManager.GetTexCache());
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracedAreaShadowIntegration, m_DenoiseBuffer0);
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracedAreaShadowSample, m_DenoiseBuffer1);
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                            cmd.SetComputeTextureParam(shadowsCompute, shadowComputeKernel, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                            cmd.DispatchCompute(shadowsCompute, shadowComputeKernel, numTilesX, numTilesY, 1);

                            // This pass will use the previously generated sample and add it to the integration buffer
                            cmd.SetRayTracingBufferParam(shadowRayTrace, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                            cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                            cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracedAreaShadowSample, m_DenoiseBuffer1);
                            cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracedAreaShadowIntegration, m_DenoiseBuffer0);
                            cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                            cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                            cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                            cmd.DispatchRays(shadowRayTrace, m_RayGenAreaShadowSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
                        }
                    }
                    else
                    {
                        // This pass generates the analytic value and will do the full integration
                        cmd.SetRayTracingBufferParam(shadowRayTrace, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                        cmd.SetRayTracingIntParam(shadowRayTrace, HDShaderIDs._RaytracingTargetAreaLight, lightIdx);
                        cmd.SetRayTracingIntParam(shadowRayTrace, HDShaderIDs._RaytracingNumSamples, currentAdditionalLightData.numRayTracingSamples);
                        cmd.SetRayTracingMatrixParam(shadowRayTrace, HDShaderIDs._RaytracingAreaWorldToLocal, worldToLocalArea);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                        cmd.SetRayTracingIntParam(shadowRayTrace, HDShaderIDs._RayCountEnabled, m_RayTracingManager.rayCountManager.RayCountIsEnabled());
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RayCountTexture, m_RayTracingManager.rayCountManager.rayCountTexture);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._AreaCookieTextures, m_TextureCaches.areaLightCookieManager.GetTexCache());
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                        cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracedAreaShadowIntegration, m_DenoiseBuffer0);
                        cmd.DispatchRays(shadowRayTrace, m_RayGenAreaShadowName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
                    }
                }

                using (new ProfilingSample(cmd, "Combine Area Shadow", CustomSamplerId.RaytracingShadowCombination.GetSampler()))
                {
                    // Global parameters
                    cmd.SetComputeIntParam(shadowFilter, HDShaderIDs._RaytracingDenoiseRadius, currentAdditionalLightData.filterSizeTraced);
                    cmd.SetComputeIntParam(shadowFilter, HDShaderIDs._RaytracingShadowSlot, m_lightList.lights[lightIdx].screenSpaceShadowIndex);

                    // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
                    var historyScale = new Vector2(hdCamera.actualWidth / (float)shadowHistoryArray.rt.width, hdCamera.actualHeight / (float)shadowHistoryArray.rt.height);
                    cmd.SetComputeVectorParam(shadowFilter, HDShaderIDs._RTHandleScaleHistory, historyScale);

                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._AreaShadowHistory, shadowHistoryArray);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._AnalyticHistoryBuffer, areaAnalyticHistoryArray);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer0);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer1);
                    cmd.DispatchCompute(shadowFilter, applyTAAKernel, numTilesX, numTilesY, 1);

                    // Update the shadow history buffer
                    cmd.SetComputeTextureParam(shadowFilter, updateAnalyticHistory, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                    cmd.SetComputeTextureParam(shadowFilter, updateAnalyticHistory, HDShaderIDs._AnalyticHistoryBuffer, areaAnalyticHistoryArray);
                    cmd.DispatchCompute(shadowFilter, updateAnalyticHistory, numTilesX, numTilesY, 1);

                    // Update the analytic history buffer
                    cmd.SetComputeTextureParam(shadowFilter, updateShadowHistory, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer1);
                    cmd.SetComputeTextureParam(shadowFilter, updateShadowHistory, HDShaderIDs._AreaShadowHistoryRW, shadowHistoryArray);
                    cmd.DispatchCompute(shadowFilter, updateShadowHistory, numTilesX, numTilesY, 1);

                    if (currentAdditionalLightData.filterSizeTraced > 0)
                    {
                        // Inject parameters for noise estimation
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

                        // Noise estimation pre-pass
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer1);
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer0);
                        cmd.DispatchCompute(shadowFilter, estimateNoiseKernel, numTilesX, numTilesY, 1);

                        // Reinject parameters for denoising
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);

                        // First denoising pass
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer0);
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer1);
                        cmd.DispatchCompute(shadowFilter, firstDenoiseKernel, numTilesX, numTilesY, 1);
                    }

                    // Re-inject parameters for denoising
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);

                    // Second (and final) denoising pass
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer1);
                    cmd.DispatchCompute(shadowFilter, secondDenoiseKernel, numTilesX, numTilesY, 1);
                }
            }
            return true;
        }

        void EvaluateShadowDebugView(CommandBuffer cmd, HDCamera hdCamera)
        {
            ComputeShader shadowFilter = m_Asset.renderPipelineRayTracingResources.shadowFilterCS;

            // If this is the right debug mode and the index we are asking for is in the range
            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            if (FullScreenDebugMode.ScreenSpaceShadows == hdrp.m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Clear the output texture
                HDUtils.SetRenderTarget(cmd, m_DenoiseBuffer0, clearFlag: ClearFlag.Color);

                // If the screen space shadows we are asked to deliver is available output it to the intermediate texture
                if (m_ScreenSpaceShadowIndex > hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex)
                {
                    int targetKernel = shadowFilter.FindKernel("WriteShadowTextureDebug");
                    cmd.SetComputeIntParam(shadowFilter, HDShaderIDs._RaytracingShadowSlot, (int)hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex);
                    cmd.SetComputeTextureParam(shadowFilter, targetKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                    cmd.SetComputeTextureParam(shadowFilter, targetKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer0);
                    cmd.DispatchCompute(shadowFilter, targetKernel, numTilesX, numTilesY, 1);
                }

                // Push the full screen debug texture
                hdrp.PushFullScreenDebugTexture(hdCamera, cmd, m_DenoiseBuffer0, FullScreenDebugMode.ScreenSpaceShadows);
            }
        }
#endif
    }
}
