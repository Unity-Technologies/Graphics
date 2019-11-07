using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values (ray tracing gen shaders)
        const string m_RayGenAreaShadowName = "RayGenAreaShadows";
        const string m_RayGenAreaShadowSingleName = "RayGenAreaShadowSingle";
        const string m_RayGenDirectionalShadowSingleName = "RayGenDirectionalShadowSingle";
        const string m_RayGenShadowSegmentSingleName = "RayGenShadowSegmentSingle";

        // Intermediate buffers
        RTHandle m_ShadowIntermediateBufferRG0;
        RTHandle m_ShadowIntermediateBufferRGBA0;
        RTHandle m_ShadowIntermediateBufferRGBA1;

        // Output shadow texture
        RTHandle m_ScreenSpaceShadowTextureArray;

        // Shaders
        RayTracingShader m_ScreenSpaceShadowsRT;
        ComputeShader m_ScreenSpaceShadowsCS;
        ComputeShader m_ScreenSpaceShadowsFilterCS;

        // Kernels
            // Shared shadow kernels
        int m_ClearShadowTexture;
        int m_OutputShadowTextureKernel;

        // Directional shadow kernels
        int m_RaytracingDirectionalShadowSample;

        // Punctual shadow kernels
        int m_RaytracingPointShadowSample;
        int m_RaytracingSpotShadowSample;

        // Area shadow kernels
        int m_AreaRaytracingAreaShadowPrepassKernel;
        int m_AreaRaytracingAreaShadowNewSampleKernel;
        int m_AreaShadowApplyTAAKernel;
        int m_AreaUpdateAnalyticHistoryKernel;
        int m_AreaUpdateShadowHistoryKernel;
        int m_AreaEstimateNoiseKernel;
        int m_AreaFirstDenoiseKernel;
        int m_AreaSecondDenoiseKernel;
        int m_AreaShadowNoDenoiseKernel;

        // Temporary variable that allows us to store the world to local matrix of the lights
        Matrix4x4 m_WorldToLocalArea = new Matrix4x4();

        // Screen space shadow material
        static Material s_ScreenSpaceShadowsMat;

        void InitializeScreenSpaceShadows()
        {
            if (!m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows)
                return;

            // Fetch the shaders
            if (m_RayTracingSupported)
            {
                m_ScreenSpaceShadowsCS = m_Asset.renderPipelineRayTracingResources.shadowRaytracingCS;
                m_ScreenSpaceShadowsFilterCS = m_Asset.renderPipelineRayTracingResources.shadowFilterCS;
                m_ScreenSpaceShadowsRT = m_Asset.renderPipelineRayTracingResources.shadowRaytracingRT;

                // Directional shadow kernels
                m_ClearShadowTexture = m_ScreenSpaceShadowsCS.FindKernel("ClearShadowTexture");
                m_OutputShadowTextureKernel = m_ScreenSpaceShadowsCS.FindKernel("OutputShadowTexture");
                m_RaytracingDirectionalShadowSample = m_ScreenSpaceShadowsCS.FindKernel("RaytracingDirectionalShadowSample");
                m_RaytracingPointShadowSample = m_ScreenSpaceShadowsCS.FindKernel("RaytracingPointShadowSample");
                m_RaytracingSpotShadowSample = m_ScreenSpaceShadowsCS.FindKernel("RaytracingSpotShadowSample");

                // Area shadow kernels
                m_AreaRaytracingAreaShadowPrepassKernel = m_ScreenSpaceShadowsCS.FindKernel("RaytracingAreaShadowPrepass");
                m_AreaRaytracingAreaShadowNewSampleKernel = m_ScreenSpaceShadowsCS.FindKernel("RaytracingAreaShadowNewSample");
                m_AreaShadowApplyTAAKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowApplyTAA");
                m_AreaUpdateAnalyticHistoryKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaAnalyticHistoryCopy");
                m_AreaUpdateShadowHistoryKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowHistoryCopy");
                m_AreaEstimateNoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowEstimateNoise");
                m_AreaFirstDenoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowDenoiseFirstPass");
                m_AreaSecondDenoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowDenoiseSecondPass");
                m_AreaShadowNoDenoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowNoDenoise");

                // Allocate the intermediate buffers
                m_ShadowIntermediateBufferRG0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "ShadowIntermediateBufferRG0");
                m_ShadowIntermediateBufferRGBA0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "ShadowIntermediateBufferRGBA0");
                m_ShadowIntermediateBufferRGBA1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "ShadowIntermediateBufferRGBA1");
            }

            // Directional shadow material
            s_ScreenSpaceShadowsMat = CoreUtils.CreateEngineMaterial(screenSpaceShadowsShader);

            // Allocate the final result texture
            int numMaxShadows = Math.Max(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows, 1);
            m_ScreenSpaceShadowTextureArray = RTHandles.Alloc(Vector2.one, slices: numMaxShadows * TextureXR.slices, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AreaShadowArrayBuffer");
        }

        void ReleaseScreenSpaceShadows()
        {
            CoreUtils.Destroy(s_ScreenSpaceShadowsMat);

            RTHandles.Release(m_ScreenSpaceShadowTextureArray);

            if (m_RayTracingSupported)
            {
                RTHandles.Release(m_ShadowIntermediateBufferRGBA1);
                RTHandles.Release(m_ShadowIntermediateBufferRGBA0);
                RTHandles.Release(m_ShadowIntermediateBufferRG0);
            }
        }

        void BindBlackShadowTexture(CommandBuffer cmd)
        {
            // We always need to bind an array here.
            cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, TextureXR.GetBlackTextureArray());
        }

        static RTHandle ShadowHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, slices:4 * TextureXR.slices, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16_SFloat,
                enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("ScreenSpaceShadowHistoryBuffer{0}", frameIndex));
        }

        static RTHandle AreaAnalyticHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, slices:4 * TextureXR.slices, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16_SFloat,
                        enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("AreaAnalyticHistoryBuffer{0}", frameIndex));
        }

        void RenderScreenSpaceShadows(HDCamera hdCamera, CommandBuffer cmd)
        {
            // If screen space shadows are not supported for this camera, we are done
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows))
            {
                BindBlackShadowTexture(cmd);
                return;
            }

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
            {
                using (new ProfilingSample(cmd, "Screen Space Shadows", CustomSamplerId.TPScreenSpaceShadows.GetSampler()))
                {
                    // First of all we handle the directional light
                    RenderDirectionalLightScreenSpaceShadow(cmd, hdCamera);

                    // We handle the other light sources
                    RenderLightScreenSpaceShadows(hdCamera, cmd);

                    // We do render the debug view
                    EvaluateShadowDebugView(cmd, hdCamera);

                    // Big the right texture
                    cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, m_ScreenSpaceShadowTextureArray);
                }
            }
            else
            {
                // We bind the black texture in this case
                BindBlackShadowTexture(cmd);
            }
        }

        void RenderDirectionalLightScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera)
        {
            // Render directional screen space shadow if required
            if (m_CurrentSunLightAdditionalLightData != null && m_CurrentSunLightAdditionalLightData.WillRenderScreenSpaceShadow())
            {
                // If the shadow is flagged as ray traced, we need to evaluate it completely
                if (m_CurrentSunLightAdditionalLightData.WillRenderRayTracedShadow())
                {
                    using (new ProfilingSample(cmd, "Directional Light Ray Traced Shadow", CustomSamplerId.RaytracingDirectionalLightShadow.GetSampler()))
                    {
                        // Texture dimensions
                        int texWidth = hdCamera.actualWidth;
                        int texHeight = hdCamera.actualHeight;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Clear the integration texture
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_ShadowIntermediateBufferRGBA0);
                        cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, numTilesX, numTilesY, hdCamera.viewCount);

                        // Grab and bind the acceleration structure for the target camera
                        RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
                        cmd.SetRayTracingAccelerationStructure(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                        // Inject the ray-tracing sampling data
                        m_BlueNoise.BindDitheredRNGData8SPP(cmd);

                        // Compute the current frame index
                        int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
                        cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                        // Inject the ray generation data
                        RayTracingSettings rayTracingSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();
                        cmd.SetRayTracingFloatParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingRayBias, rayTracingSettings.rayBias.value);

                        // Loop through the samples of this frame
                        for (int sampleIdx = 0; sampleIdx < m_CurrentSunLightAdditionalLightData.numRayTracingSamples; ++sampleIdx)
                        {
                            // Bind the light & sampling data
                            cmd.SetComputeBufferParam(m_ScreenSpaceShadowsCS, m_RaytracingDirectionalShadowSample, HDShaderIDs._DirectionalLightDatas, m_LightLoopLightData.directionalLightData);
                            cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._DirectionalShadowIndex, m_CurrentShadowSortedSunLightIndex);
                            cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingSampleIndex, sampleIdx);
                            cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingNumSamples, m_CurrentSunLightAdditionalLightData.numRayTracingSamples);

                            // Input Buffer
                            cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_RaytracingDirectionalShadowSample, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                            cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_RaytracingDirectionalShadowSample, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

                            // Output buffer
                            cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_RaytracingDirectionalShadowSample, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);

                            // Generate a new direction
                            cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_RaytracingDirectionalShadowSample, numTilesX, numTilesY, hdCamera.viewCount);

                            // Define the shader pass to use for the shadow pass
                            cmd.SetRayTracingShaderPass(m_ScreenSpaceShadowsRT, "VisibilityDXR");

                            // Set ray count texture
                            RayCountManager rayCountManager = GetRayCountManager();
                            cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

                            // Input buffers
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                            cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingNumSamples, m_CurrentSunLightAdditionalLightData.numRayTracingSamples);

                            // Output buffer
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_ShadowIntermediateBufferRGBA0);

                            // Evaluate the visibility
                            cmd.DispatchRays(m_ScreenSpaceShadowsRT, m_RayGenDirectionalShadowSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
                        }

                        // Grab the history buffer for shadows
                        RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow)
                            ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow, ShadowHistoryBufferAllocatorFunction, 1);

                        // Apply the simple denoiser (if required)
                        if (m_CurrentSunLightAdditionalLightData.filterTracedShadow)
                        {
                            // Apply the temporal denoiser
                            HDTemporalFilter temporalFilter = GetTemporalFilter();
                            temporalFilter.DenoiseBuffer(cmd, hdCamera, m_ShadowIntermediateBufferRGBA0, shadowHistoryArray, m_ShadowIntermediateBufferRGBA1, singleChannel: true, slotIndex: m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex);

                            // Apply the spatial denoiser
                            HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                            simpleDenoiser.DenoiseBufferNoHistory(cmd, hdCamera, m_ShadowIntermediateBufferRGBA1, m_ShadowIntermediateBufferRGBA0, m_CurrentSunLightAdditionalLightData.filterSizeTraced, singleChannel: true);
                        }

                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_OutputShadowTextureKernel, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_ShadowIntermediateBufferRGBA0);
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_OutputShadowTextureKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                        cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingShadowSlot, m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex);
                        cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_OutputShadowTextureKernel, numTilesX, numTilesY, hdCamera.viewCount);
                    }
                }
                else
                {
                    // If it is screen space but not ray traced, then we can rely on the shadow map
                    CoreUtils.SetRenderTarget(cmd, m_ScreenSpaceShadowTextureArray, depthSlice: m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex);
                    HDUtils.DrawFullScreen(cmd, s_ScreenSpaceShadowsMat, m_ScreenSpaceShadowTextureArray);
                }
            }
        }
        bool RenderLightScreenSpaceShadows(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Light Ray Traced Shadows", CustomSamplerId.RaytracingLightShadow.GetSampler()))
            {
                // Grab the history buffer
                RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadow, ShadowHistoryBufferAllocatorFunction, 1);

                // Grab the acceleration structure for the target camera
                RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
                // Set the acceleration structure for the pass
                cmd.SetRayTracingAccelerationStructure(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                // Define the shader pass to use for the reflection pass
                cmd.SetRayTracingShaderPass(m_ScreenSpaceShadowsRT, "VisibilityDXR");

                // Inject the ray-tracing sampling data
                m_BlueNoise.BindDitheredRNGData8SPP(cmd);

                using (new ProfilingSample(cmd, "Ray Traced Shadows", CustomSamplerId.RaytracingLightShadow.GetSampler()))
                {
                    // Loop through all the potential screen space light shadows
                    for (int lightIdx = 0; lightIdx < m_ScreenSpaceShadowIndex; ++lightIdx)
                    {
                        // This matches the directional light
                        if (!m_CurrentScreenSpaceShadowData[lightIdx].valid) continue;

                        // Fetch the light data and additional light data
                        LightData currentLight = m_lightList.lights[m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex];
                        HDAdditionalLightData currentAdditionalLightData = m_CurrentScreenSpaceShadowData[lightIdx].additionalLightData;

                        // Trigger the right algorithm based on the light type
                        switch (currentLight.lightType)
                        {
                            case GPULightType.Rectangle:
                            {
                                RenderAreaScreenSpaceShadow(cmd, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex, shadowHistoryArray);
                            }
                            break;
                            case GPULightType.Point:
                            case GPULightType.Spot:
                            {
                                RenderPunctualScreenSpaceShadow(cmd, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex, shadowHistoryArray);
                            }
                            break;
                        }
                    }
                }
                return true;
            }
        }

            void RenderAreaScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera
                , in LightData lightData, HDAdditionalLightData additionalLightData, int lightIndex
                , RTHandle shadowHistoryArray)
            {
                // We only support ray traced area shadows if we are in deferred mode
                if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred)
                    return;

                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // We need to build the world to area light matrix
                m_WorldToLocalArea.SetColumn(0, lightData.right);
                m_WorldToLocalArea.SetColumn(1, lightData.up);
                m_WorldToLocalArea.SetColumn(2, lightData.forward);

                // Compensate the  relative rendering if active
                Vector3 lightPositionWS = lightData.positionRWS;
                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    lightPositionWS += hdCamera.camera.transform.position;
                }
                m_WorldToLocalArea.SetColumn(3, lightPositionWS);
                m_WorldToLocalArea.m33 = 1.0f;
                m_WorldToLocalArea = m_WorldToLocalArea.inverse;

                // We have noticed from extensive profiling that ray-trace shaders are not as effective for running per-pixel computation. In order to reduce that,
                // we do a first prepass that compute the analytic term and probability and generates the first integration sample

                // Bind the light data
                cmd.SetComputeBufferParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                cmd.SetComputeMatrixParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, m_WorldToLocalArea);
                cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingTargetAreaLight, lightIndex);
                cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingNumSamples, additionalLightData.numRayTracingSamples);
                int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
                cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                // Bind the input buffers
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._AreaCookieTextures, m_TextureCaches.areaLightCookieManager.GetTexCache());

                // Bind the output buffers
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowIntegration, m_ShadowIntermediateBufferRGBA0);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowSample, m_ShadowIntermediateBufferRGBA1);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._AnalyticProbBuffer, m_ShadowIntermediateBufferRG0);
                cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, numTilesX, numTilesY, hdCamera.viewCount);

                // Set ray count texture
                RayCountManager rayCountManager = GetRayCountManager();
                cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

                // Input data
                cmd.SetRayTracingBufferParam(m_ScreenSpaceShadowsRT, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, m_ShadowIntermediateBufferRG0);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, m_ShadowIntermediateBufferRGBA1);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, lightIndex);
                RayTracingSettings rayTracingSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();
                cmd.SetRayTracingFloatParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingRayBias, rayTracingSettings.rayBias.value);

                // Output data
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, m_ShadowIntermediateBufferRGBA0);

                // Evaluate the intersection
                cmd.DispatchRays(m_ScreenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);

                // Let's do the following samples (if any)
                for (int sampleIndex = 1; sampleIndex < additionalLightData.numRayTracingSamples; ++sampleIndex)
                {
                    // Bind the light data
                    cmd.SetComputeBufferParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingTargetAreaLight, lightIndex);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingSampleIndex, sampleIndex);
                    cmd.SetComputeMatrixParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, m_WorldToLocalArea);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingNumSamples, additionalLightData.numRayTracingSamples);

                    // Input Buffers
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._AreaCookieTextures, m_TextureCaches.areaLightCookieManager.GetTexCache());

                    // Output buffers
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._RaytracedAreaShadowSample, m_ShadowIntermediateBufferRGBA1);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._AnalyticProbBuffer, m_ShadowIntermediateBufferRG0);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Input buffers
                    cmd.SetRayTracingBufferParam(m_ScreenSpaceShadowsRT, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, m_ShadowIntermediateBufferRGBA1);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, m_ShadowIntermediateBufferRG0);
                    cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, lightIndex);

                    // Output buffers
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, m_ShadowIntermediateBufferRGBA0);

                    // Evaluate the intersection
                    cmd.DispatchRays(m_ScreenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
                }

                /*
                // This should go in tier2
                {
                    // This pass generates the analytic value and will do the full integration
                    cmd.SetRayTracingBufferParam(shadowRayTrace, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                    cmd.SetRayTracingIntParam(shadowRayTrace, HDShaderIDs._RaytracingTargetAreaLight, lightIdx);
                    cmd.SetRayTracingIntParam(shadowRayTrace, HDShaderIDs._RaytracingNumSamples, additionalLightData.numRayTracingSamples);
                    cmd.SetRayTracingMatrixParam(shadowRayTrace, HDShaderIDs._RaytracingAreaWorldToLocal, worldToLocalArea);
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                    cmd.SetRayTracingIntParam(shadowRayTrace, HDShaderIDs._RayCountEnabled, m_RayTracingManager.rayCountManager.RayCountIsEnabled());
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RayCountTexture, m_RayTracingManager.rayCountManager.GetRayCountTexture());
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._AreaCookieTextures, m_TextureCaches.areaLightCookieManager.GetTexCache());
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._AnalyticProbBuffer, m_AnalyticProbBuffer);
                    cmd.SetRayTracingTextureParam(shadowRayTrace, HDShaderIDs._RaytracedAreaShadowIntegration, m_DenoiseBuffer0);
                    cmd.DispatchRays(shadowRayTrace, m_RayGenAreaShadowName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
                }
                */

                if (additionalLightData.filterTracedShadow)
                {
                    // Fetch the analytic history buffer
                    RTHandle areaAnalyticHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAreaAnalytic)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAreaAnalytic, AreaAnalyticHistoryBufferAllocatorFunction, 1);

                    // Global parameters
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._RaytracingDenoiseRadius, additionalLightData.filterSizeTraced);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._RaytracingShadowSlot, m_lightList.lights[lightIndex].screenSpaceShadowIndex);

                    // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
                    var historyScale = new Vector2(hdCamera.actualWidth / (float)shadowHistoryArray.rt.width, hdCamera.actualHeight / (float)shadowHistoryArray.rt.height);
                    cmd.SetComputeVectorParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._RTHandleScaleHistory, historyScale);

                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._AnalyticProbBuffer, m_ShadowIntermediateBufferRG0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._AreaShadowHistory, shadowHistoryArray);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._AnalyticHistoryBuffer, areaAnalyticHistoryArray);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._DenoiseInputTexture, m_ShadowIntermediateBufferRGBA0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._DenoiseOutputTextureRW, m_ShadowIntermediateBufferRGBA1);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Update the shadow history buffer
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticProbBuffer, m_ShadowIntermediateBufferRG0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticHistoryBuffer, areaAnalyticHistoryArray);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateAnalyticHistoryKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Update the analytic history buffer
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateShadowHistoryKernel, HDShaderIDs._DenoiseInputTexture, m_ShadowIntermediateBufferRGBA1);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateShadowHistoryKernel, HDShaderIDs._AreaShadowHistoryRW, shadowHistoryArray);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateShadowHistoryKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    if (additionalLightData.filterSizeTraced > 0)
                    {
                        // Inject parameters for noise estimation
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

                        // Noise estimation pre-pass
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._DenoiseInputTexture, m_ShadowIntermediateBufferRGBA1);
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, m_ShadowIntermediateBufferRGBA0);
                        cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);

                        // Reinject parameters for denoising
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);

                        // First denoising pass
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._DenoiseInputTexture, m_ShadowIntermediateBufferRGBA0);
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, m_ShadowIntermediateBufferRGBA1);
                        cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);
                    }

                    // Re-inject parameters for denoising
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);

                    // Second (and final) denoising pass
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._DenoiseInputTexture, m_ShadowIntermediateBufferRGBA1);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);
                }
                else
                {
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._RaytracingShadowSlot, lightData.screenSpaceShadowIndex);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowNoDenoiseKernel, HDShaderIDs._DenoiseInputTexture, m_ShadowIntermediateBufferRGBA0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowNoDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaShadowNoDenoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);
                }
            }

            void RenderPunctualScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera
                , in LightData lightData, HDAdditionalLightData additionalLightData, int lightIndex
                , RTHandle shadowHistoryArray)
            {
                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Clear the integration texture
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_ShadowIntermediateBufferRGBA0);
                cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, numTilesX, numTilesY, hdCamera.viewCount);

                RayTracingSettings rayTracingSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();
                cmd.SetRayTracingFloatParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingRayBias, rayTracingSettings.rayBias.value);

                // Loop through the samples of this frame
                for (int sampleIdx = 0; sampleIdx < additionalLightData.numRayTracingSamples; ++sampleIdx)
                {
                    // Bind the right kernel
                    int shadowKernel = lightData.lightType == GPULightType.Point ? m_RaytracingPointShadowSample : m_RaytracingSpotShadowSample;

                    // Bind the light & sampling data
                    cmd.SetComputeBufferParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingTargetAreaLight, lightIndex);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingSampleIndex, sampleIdx);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingNumSamples, additionalLightData.numRayTracingSamples);
                    cmd.SetComputeFloatParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingLightRadius, additionalLightData.shapeRadius);
                    int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                    // If this is a spot light, inject the spot angle in radians
                    if (lightData.lightType == GPULightType.Spot)
                    {
                        float spotAngleRadians = additionalLightData.legacyLight.spotAngle * (float)Math.PI / 180.0f;
                        cmd.SetComputeFloatParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingSpotAngle, spotAngleRadians);
                    }

                    // Input Buffer
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

                    // Output buffers
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);

                    // Generate a new direction
                    cmd.DispatchCompute(m_ScreenSpaceShadowsCS, shadowKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Define the shader pass to use for the shadow pass
                    cmd.SetRayTracingShaderPass(m_ScreenSpaceShadowsRT, "VisibilityDXR");

                    // Set ray count texture
                    RayCountManager rayCountManager = GetRayCountManager();
                    cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

                    // Input buffers
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);
                    cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingNumSamples, additionalLightData.numRayTracingSamples);

                    // Output buffer
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_ShadowIntermediateBufferRGBA0);

                    // Evaluate the visibility
                    cmd.DispatchRays(m_ScreenSpaceShadowsRT, m_RayGenShadowSegmentSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
                }

                // Apply the simple denoiser (if required)
                if (additionalLightData.filterTracedShadow)
                {
                    // Apply the temporal denoiser
                    HDTemporalFilter temporalFilter = GetTemporalFilter();
                    temporalFilter.DenoiseBuffer(cmd, hdCamera, m_ShadowIntermediateBufferRGBA0, shadowHistoryArray, m_ShadowIntermediateBufferRGBA1, singleChannel: true, slotIndex: lightData.screenSpaceShadowIndex);

                    // Apply the spatial denoiser
                    HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                    simpleDenoiser.DenoiseBufferNoHistory(cmd, hdCamera, m_ShadowIntermediateBufferRGBA1, m_ShadowIntermediateBufferRGBA0, additionalLightData.filterSizeTraced, singleChannel: true);
                }

                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_OutputShadowTextureKernel, HDShaderIDs._RaytracedDirectionalShadowIntegration, m_ShadowIntermediateBufferRGBA0);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_OutputShadowTextureKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingShadowSlot, lightData.screenSpaceShadowIndex);
                cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_OutputShadowTextureKernel, numTilesX, numTilesY, hdCamera.viewCount);
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
                CoreUtils.SetRenderTarget(cmd, m_ShadowIntermediateBufferRGBA0, clearFlag: ClearFlag.Color);

                // If the screen space shadows we are asked to deliver is available output it to the intermediate texture
                if (m_ScreenSpaceShadowIndex > hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex)
                {
                    int targetKernel = shadowFilter.FindKernel("WriteShadowTextureDebug");
                    cmd.SetComputeIntParam(shadowFilter, HDShaderIDs._RaytracingShadowSlot, (int)hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex);
                    cmd.SetComputeTextureParam(shadowFilter, targetKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                    cmd.SetComputeTextureParam(shadowFilter, targetKernel, HDShaderIDs._DenoiseOutputTextureRW, m_ShadowIntermediateBufferRGBA0);
                    cmd.DispatchCompute(shadowFilter, targetKernel, numTilesX, numTilesY, hdCamera.viewCount);
                }

                // Push the full screen debug texture
                hdrp.PushFullScreenDebugTexture(hdCamera, cmd, m_ShadowIntermediateBufferRGBA0, FullScreenDebugMode.ScreenSpaceShadows);
            }
        }
    }
}
