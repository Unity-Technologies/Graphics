using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values (ray tracing gen shaders)
        const string m_RayGenAreaShadowSingleName = "RayGenAreaShadowSingle";
        const string m_RayGenDirectionalShadowSingleName = "RayGenDirectionalShadowSingle";
        const string m_RayGenDirectionalColorShadowSingleName = "RayGenDirectionalColorShadowSingle";
        const string m_RayGenShadowSegmentSingleName = "RayGenShadowSegmentSingle";
        const string m_RayGenSemiTransparentShadowSegmentSingleName = "RayGenSemiTransparentShadowSegmentSingle";

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
        int m_OutputColorShadowTextureKernel;

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
        // Temporary variables that allow us to read and write the right channels from the history buffer
        Vector4 m_ShadowChannelMask0 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        Vector4 m_ShadowChannelMask1 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        Vector4 m_ShadowChannelMask2 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        // Screen space shadow material
        static Material s_ScreenSpaceShadowsMat;

        // This buffer holds the unfiltered, accumulated, shadow values, it is accessed with the same index as the one used at runtime (aka screen space shadow slot)
        static RTHandle ShadowHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            GraphicsFormat graphicsFormat = (GraphicsFormat)hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            int numShadowSlices = Math.Max((int)Math.Ceiling(hdrp.m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            return rtHandleSystem.Alloc(Vector2.one, slices: numShadowSlices * TextureXR.slices, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: graphicsFormat,
                enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("ScreenSpaceShadowHistoryBuffer{0}", frameIndex));
        }


        // This value holds additional values that are required for the filtering process.
        // For directional, punctual and spot light it holds the sample accumulation count and for the area light it holds the analytic value.
        // It is accessed with the same index used at runtime (aka screen space shadow slot)
        static RTHandle ShadowHistoryValidityBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            GraphicsFormat graphicsFormat = (GraphicsFormat)hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            int numShadowSlices = Math.Max((int)Math.Ceiling(hdrp.m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            return rtHandleSystem.Alloc(Vector2.one, slices: numShadowSlices * TextureXR.slices, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: graphicsFormat,
                        enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("ShadowHistoryValidityBuffer{0}", frameIndex));
        }

        // The three types of shadows that we currently support
        enum ScreenSpaceShadowType
        {
            GrayScale,
            Area,
            Color
        }

        // This functions returns a mask that tells us for a given slot and based on the light type, which channels should hold the shadow information.
        static void GetShadowChannelMask(int shadowSlot, ScreenSpaceShadowType shadowType, ref Vector4 outputMask)
        {
            int outputChannel = shadowSlot % 4;
            if (shadowType == ScreenSpaceShadowType.GrayScale)
            {
                switch (outputChannel)
                {
                    case 0:
                        {
                            outputMask.Set(1.0f, 0.0f, 0.0f, 0.0f);
                            break;
                        }
                    case 1:
                        {
                            outputMask.Set(0.0f, 1.0f, 0.0f, 0.0f);
                            break;
                        }
                    case 2:
                        {
                            outputMask.Set(0.0f, 0.0f, 1.0f, 0.0f);
                            break;
                        }
                    case 3:
                        {
                            outputMask.Set(0.0f, 0.0f, 0.0f, 1.0f);
                            break;
                        }
                }
            }
            else if (shadowType == ScreenSpaceShadowType.Area)
            {
                switch (outputChannel)
                {
                    case 0:
                        {
                            outputMask.Set(1.0f, 1.0f, 0.0f, 0.0f);
                            break;
                        }
                    case 1:
                        {
                            outputMask.Set(0.0f, 1.0f, 1.0f, 0.0f);
                            break;
                        }
                    case 2:
                        {
                            outputMask.Set(0.0f, 0.0f, 1.0f, 1.0f);
                            break;
                        }
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            else if (shadowType == ScreenSpaceShadowType.Color)
            {
                switch (outputChannel)
                {
                    case 0:
                        {
                            outputMask.Set(1.0f, 1.0f, 1.0f, 0.0f);
                            break;
                        }
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

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
                m_OutputColorShadowTextureKernel = m_ScreenSpaceShadowsCS.FindKernel("OutputColorShadowTexture");
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
            }

            // Directional shadow material
            s_ScreenSpaceShadowsMat = CoreUtils.CreateEngineMaterial(screenSpaceShadowsShader);

            // Allocate the final result texture
            int numShadowTextures = Math.Max((int)Math.Ceiling(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            GraphicsFormat graphicsFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            m_ScreenSpaceShadowTextureArray = RTHandles.Alloc(Vector2.one, slices: numShadowTextures * TextureXR.slices, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: graphicsFormat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AreaShadowArrayBuffer");
        }

        void ReleaseScreenSpaceShadows()
        {
            CoreUtils.Destroy(s_ScreenSpaceShadowsMat);
            RTHandles.Release(m_ScreenSpaceShadowTextureArray);
        }

        void BindBlackShadowTexture(CommandBuffer cmd)
        {
            // We always need to bind an array here.
            cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, TextureXR.GetBlackTextureArray());
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
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ScreenSpaceShadows)))
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

        // Generic function that writes in the screen space shadow buffer
        void WriteToScreenSpaceShadowBuffer(CommandBuffer cmd, HDCamera hdCamera, RTHandle source, int shadowSlot, ScreenSpaceShadowType shadowType)
        {
            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Define which kernel we should be using
            int shadowKernel = shadowType == ScreenSpaceShadowType.Color ? m_OutputColorShadowTextureKernel : m_OutputShadowTextureKernel;

            // Bind the input data
            GetShadowChannelMask(shadowSlot, shadowType, ref m_ShadowChannelMask0);
            cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingShadowSlot, shadowSlot / 4);
            cmd.SetComputeVectorParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingChannelMask, m_ShadowChannelMask0);
            cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._RaytracedShadowIntegration, source);

            // Bind the output texture
            cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);

            //Do our copy
            cmd.DispatchCompute(m_ScreenSpaceShadowsCS, shadowKernel, numTilesX, numTilesY, hdCamera.viewCount);
        }

        void RenderDirectionalLightScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera)
        {
            // Render directional screen space shadow if required
            if (m_CurrentSunLightAdditionalLightData != null && m_CurrentSunLightAdditionalLightData.WillRenderScreenSpaceShadow())
            {
                // If the shadow is flagged as ray traced, we need to evaluate it completely
                if (m_CurrentSunLightAdditionalLightData.WillRenderRayTracedShadow())
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingDirectionalLightShadow)))
                    {
                        // Request the intermediate buffers we shall be using
                        RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
                        RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
                        RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
                        RTHandle distanceBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Distance);
                        RTHandle velocityBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R1);

                        // Texture dimensions
                        int texWidth = hdCamera.actualWidth;
                        int texHeight = hdCamera.actualHeight;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Clear the integration texture
                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, HDShaderIDs._RaytracedShadowIntegration, intermediateBuffer0);
                        cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, numTilesX, numTilesY, hdCamera.viewCount);

                        cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, HDShaderIDs._RaytracedShadowIntegration, velocityBuffer);
                        cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, numTilesX, numTilesY, hdCamera.viewCount);

                        // Grab and bind the acceleration structure for the target camera
                        RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
                        cmd.SetRayTracingAccelerationStructure(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                        // Inject the ray-tracing sampling data
                        m_BlueNoise.BindDitheredRNGData8SPP(cmd);

                        // Compute the current frame index
                        int frameIndex = RayTracingFrameIndex(hdCamera);
                        cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                        // Inject the ray generation data
                        RayTracingSettings rayTracingSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
                        cmd.SetRayTracingFloatParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingRayBias, rayTracingSettings.rayBias.value);

                        // Make sure the right closest hit/any hit will be triggered by using the right multi compile
                        CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", m_CurrentSunLightAdditionalLightData.colorShadow);

                        // Define which ray generation shaders we shall be using
                        string directionaLightShadowShader = m_CurrentSunLightAdditionalLightData.colorShadow ? m_RayGenDirectionalColorShadowSingleName : m_RayGenDirectionalShadowSingleName;

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
                            cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_RaytracingDirectionalShadowSample, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);

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
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                            cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingNumSamples, m_CurrentSunLightAdditionalLightData.numRayTracingSamples);

                            // Output buffer
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, m_CurrentSunLightAdditionalLightData.colorShadow ? HDShaderIDs._RaytracedColorShadowIntegration : HDShaderIDs._RaytracedShadowIntegration, intermediateBuffer0);
                            cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._VelocityBuffer, velocityBuffer);

                            // Evaluate the visibility
                            cmd.DispatchRays(m_ScreenSpaceShadowsRT, directionaLightShadowShader, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
                        }

                        // Now that we are done with the ray tracing bit, disable the multi compile that was potentially enabled
                        CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", false);

                        // Grab the history buffers for shadows
                        RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory)
                            ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory, ShadowHistoryBufferAllocatorFunction, 1);
                        RTHandle shadowHistoryValidityArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity)
                            ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity, ShadowHistoryValidityBufferAllocatorFunction, 1);

                        // Grab the slot of the directional light (given that it may be a color shadow, we need to use the mask to get the actual slot index)
                        int dirShadowIndex = m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex & (int)LightDefinitions.s_ScreenSpaceShadowIndexMask;
                        GetShadowChannelMask(dirShadowIndex, m_CurrentSunLightAdditionalLightData.colorShadow ? ScreenSpaceShadowType.Color : ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask0);

                        // Apply the simple denoiser (if required)
                        if (m_CurrentSunLightAdditionalLightData.filterTracedShadow)
                        {
                            // We need to set the history as invalid if the directional light has rotated
                            float historyValidity = 1.0f;
                            if (m_CurrentSunLightAdditionalLightData.previousTransform.rotation != m_CurrentSunLightAdditionalLightData.transform.localToWorldMatrix.rotation
                                || !hdCamera.ValidShadowHistory(m_CurrentSunLightAdditionalLightData, dirShadowIndex, GPULightType.Directional))
                                historyValidity = 0.0f;

                        #if UNITY_HDRP_DXR_TESTS_DEFINE
                            if (Application.isPlaying)
                                historyValidity = 0.0f;
                            else
                        #endif
                                // We need to check if something invalidated the history buffers
                                historyValidity *= ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;
                        
                            // Apply the temporal denoiser
                            HDTemporalFilter temporalFilter = GetTemporalFilter();
                            temporalFilter.DenoiseBuffer(cmd, hdCamera, intermediateBuffer0, shadowHistoryArray, shadowHistoryValidityArray, velocityBuffer, intermediateBuffer1, dirShadowIndex / 4, m_ShadowChannelMask0, singleChannel: !m_CurrentSunLightAdditionalLightData.colorShadow, historyValidity: historyValidity);

                            // Apply the spatial denoiser
                            HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                            simpleDenoiser.DenoiseBufferNoHistory(cmd, hdCamera, intermediateBuffer1, intermediateBuffer0, m_CurrentSunLightAdditionalLightData.filterSizeTraced, singleChannel: !m_CurrentSunLightAdditionalLightData.colorShadow);

                            // Now that we have overriden this history, mark is as used by this light
                            hdCamera.PropagateShadowHistory(m_CurrentSunLightAdditionalLightData, dirShadowIndex, GPULightType.Directional);
                        }

                        // Write the result texture to the screen space shadow buffer
                        WriteToScreenSpaceShadowBuffer(cmd, hdCamera, intermediateBuffer0, dirShadowIndex, m_CurrentSunLightAdditionalLightData.colorShadow ? ScreenSpaceShadowType.Color : ScreenSpaceShadowType.GrayScale);
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
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingLightShadow)))
            {
                // Grab the history buffers for shadows
                RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory, ShadowHistoryBufferAllocatorFunction, 1);
                RTHandle shadowHistoryValidityArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity, ShadowHistoryValidityBufferAllocatorFunction, 1);

                // Grab the acceleration structure for the target camera
                RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
                // Set the acceleration structure for the pass
                cmd.SetRayTracingAccelerationStructure(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                // Define the shader pass to use for the reflection pass
                cmd.SetRayTracingShaderPass(m_ScreenSpaceShadowsRT, "VisibilityDXR");

                // Inject the ray-tracing sampling data
                m_BlueNoise.BindDitheredRNGData8SPP(cmd);

                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingLightShadow)))
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
                                RenderAreaScreenSpaceShadow(cmd, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex, shadowHistoryArray, shadowHistoryValidityArray);
                            }
                            break;
                            case GPULightType.Point:
                            case GPULightType.Spot:
                            {
                                RenderPunctualScreenSpaceShadow(cmd, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex, shadowHistoryArray, shadowHistoryValidityArray);
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
                , RTHandle shadowHistoryArray, RTHandle shadowHistoryValidityArray)
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

                RTHandle intermediateBufferRG0 = GetRayTracingBuffer(InternalRayTracingBuffers.RG0);
                RTHandle intermediateBufferRGBA0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
                RTHandle intermediateBufferRGBA1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
                RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
                RTHandle distanceBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Distance);

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
                int frameIndex = RayTracingFrameIndex(hdCamera);
                cmd.SetComputeIntParam(m_ScreenSpaceShadowsCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                // Bind the input buffers
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._CookieAtlas, m_TextureCaches.lightCookieManager.atlasTexture);

                // Bind the output buffers
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowIntegration, intermediateBufferRGBA0);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowSample, intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._RaytracingDistanceBuffer, distanceBuffer);
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, HDShaderIDs._AnalyticProbBuffer, intermediateBufferRG0);
                cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowPrepassKernel, numTilesX, numTilesY, hdCamera.viewCount);

                // Set ray count texture
                RayCountManager rayCountManager = GetRayCountManager();
                cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

                // Input data
                cmd.SetRayTracingBufferParam(m_ScreenSpaceShadowsRT, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, intermediateBufferRG0);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, intermediateBufferRGBA1);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDistanceBuffer, distanceBuffer);
                cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, lightIndex);
                RayTracingSettings rayTracingSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
                cmd.SetRayTracingFloatParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingRayBias, rayTracingSettings.rayBias.value);

                // Output data
                cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, intermediateBufferRGBA0);

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
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._CookieAtlas, m_TextureCaches.lightCookieManager.atlasTexture);

                    // Output buffers
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._RaytracedAreaShadowSample, intermediateBufferRGBA1);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._RaytracingDistanceBuffer, distanceBuffer);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, HDShaderIDs._AnalyticProbBuffer, intermediateBufferRG0);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_AreaRaytracingAreaShadowNewSampleKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Input buffers
                    cmd.SetRayTracingBufferParam(m_ScreenSpaceShadowsRT, HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, intermediateBufferRGBA1);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDistanceBuffer, distanceBuffer);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, intermediateBufferRG0);
                    cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, lightIndex);

                    // Output buffers
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, intermediateBufferRGBA0);

                    // Evaluate the intersection
                    cmd.DispatchRays(m_ScreenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
                }

                if (additionalLightData.filterTracedShadow)
                {
                    int areaShadowSlot = m_lightList.lights[lightIndex].screenSpaceShadowIndex;
                    GetShadowChannelMask(areaShadowSlot, ScreenSpaceShadowType.Area, ref m_ShadowChannelMask0);
                    GetShadowChannelMask(areaShadowSlot, ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask1);
                    GetShadowChannelMask(areaShadowSlot + 1, ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask2);

                    // Global parameters
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._RaytracingDenoiseRadius, additionalLightData.filterSizeTraced);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistorySlice, areaShadowSlot / 4);
                    cmd.SetComputeVectorParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMask, m_ShadowChannelMask0);
                    cmd.SetComputeVectorParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskSn, m_ShadowChannelMask1);
                    cmd.SetComputeVectorParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskUn, m_ShadowChannelMask2);

                    // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
                    var historyScale = new Vector2(hdCamera.actualWidth / (float)shadowHistoryArray.rt.width, hdCamera.actualHeight / (float)shadowHistoryArray.rt.height);
                    cmd.SetComputeVectorParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._RTHandleScaleHistory, historyScale);

                    float historyValidity = 1.0f;
                #if UNITY_HDRP_DXR_TESTS_DEFINE
                    if (Application.isPlaying)
                        historyValidity = 0.0f;
                    else
                #endif
                    // We need to check if something invalidated the history buffers
                    historyValidity = ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._AnalyticProbBuffer, intermediateBufferRG0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._AreaShadowHistory, shadowHistoryArray);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._AnalyticHistoryBuffer, shadowHistoryValidityArray);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._DenoiseInputTexture, intermediateBufferRGBA0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBufferRGBA1);
                    cmd.SetComputeFloatParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._HistoryValidity, historyValidity);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaShadowApplyTAAKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Update the shadow history buffer
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticProbBuffer, intermediateBufferRG0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticHistoryBuffer, shadowHistoryValidityArray);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateAnalyticHistoryKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Update the analytic history buffer
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateShadowHistoryKernel, HDShaderIDs._DenoiseInputTexture, intermediateBufferRGBA1);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateShadowHistoryKernel, HDShaderIDs._AreaShadowHistoryRW, shadowHistoryArray);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaUpdateShadowHistoryKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Inject parameters for noise estimation
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

                    // Noise estimation pre-pass
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._DenoiseInputTexture, intermediateBufferRGBA1);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBufferRGBA0);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaEstimateNoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Reinject parameters for denoising
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);

                    // First denoising pass
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._DenoiseInputTexture, intermediateBufferRGBA0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBufferRGBA1);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaFirstDenoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Re-inject parameters for denoising
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

                    // Second (and final) denoising pass
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._DenoiseInputTexture, intermediateBufferRGBA1);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBufferRGBA0);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaSecondDenoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);

                    // Write the result texture to the screen space shadow buffer
                    WriteToScreenSpaceShadowBuffer(cmd, hdCamera, intermediateBufferRGBA0, areaShadowSlot, ScreenSpaceShadowType.Area);

                    // Do not forget to update the identification of shadow history usage
                    hdCamera.PropagateShadowHistory(additionalLightData, areaShadowSlot, GPULightType.Rectangle);
                }
                else
                {
                    int areaShadowSlot = lightData.screenSpaceShadowIndex;
                    int areaShadowSlice = areaShadowSlot / 4;
                    GetShadowChannelMask(areaShadowSlot, ScreenSpaceShadowType.Area, ref m_ShadowChannelMask0);
                    cmd.SetComputeVectorParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMask, m_ShadowChannelMask0);
                    cmd.SetComputeIntParam(m_ScreenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistorySlice, areaShadowSlice);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowNoDenoiseKernel, HDShaderIDs._DenoiseInputTexture, intermediateBufferRGBA0);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsFilterCS, m_AreaShadowNoDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                    cmd.DispatchCompute(m_ScreenSpaceShadowsFilterCS, m_AreaShadowNoDenoiseKernel, numTilesX, numTilesY, hdCamera.viewCount);
                }
            }

            void RenderPunctualScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera
                , in LightData lightData, HDAdditionalLightData additionalLightData, int lightIndex
                , RTHandle shadowHistoryArray, RTHandle shadowHistoryValidityArray)
            {
                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Request the intermediate buffers we shall be using
                RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
                RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
                RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
                RTHandle distanceBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Distance);
                RTHandle velocityBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R1);

                // Clear the integration texture
                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, HDShaderIDs._RaytracedShadowIntegration, intermediateBuffer0);
                cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, numTilesX, numTilesY, hdCamera.viewCount);

                cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, HDShaderIDs._RaytracedShadowIntegration, velocityBuffer);
                cmd.DispatchCompute(m_ScreenSpaceShadowsCS, m_ClearShadowTexture, numTilesX, numTilesY, hdCamera.viewCount);

                // Bind the ray generation scalars
                RayTracingSettings rayTracingSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
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
                    int frameIndex = RayTracingFrameIndex(hdCamera);
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
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                    cmd.SetComputeTextureParam(m_ScreenSpaceShadowsCS, shadowKernel, HDShaderIDs._RaytracingDistanceBuffer, distanceBuffer);

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
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingDistanceBuffer, distanceBuffer);
                    cmd.SetRayTracingIntParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracingNumSamples, additionalLightData.numRayTracingSamples);

                    // Output buffer
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._RaytracedShadowIntegration, intermediateBuffer0);
                    cmd.SetRayTracingTextureParam(m_ScreenSpaceShadowsRT, HDShaderIDs._VelocityBuffer, velocityBuffer);

                    CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", additionalLightData.semiTransparentShadow);
                    cmd.DispatchRays(m_ScreenSpaceShadowsRT, additionalLightData.semiTransparentShadow ? m_RayGenSemiTransparentShadowSegmentSingleName : m_RayGenShadowSegmentSingleName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
                    CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", false);
                }

                // Apply the simple denoiser (if required)
                GetShadowChannelMask(lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask0);
                if (additionalLightData.filterTracedShadow)
                {
                    // We need to set the history as invalid if the light has moved (rotated or translated), 
                    float historyValidity = 1.0f;
                    if (additionalLightData.previousTransform != additionalLightData.transform.localToWorldMatrix
                        || !hdCamera.ValidShadowHistory(additionalLightData, lightData.screenSpaceShadowIndex, lightData.lightType))
                        historyValidity = 0.0f;

                #if UNITY_HDRP_DXR_TESTS_DEFINE
                    if (Application.isPlaying)
                        historyValidity = 0.0f;
                    else
                #endif
                        // We need to check if something invalidated the history buffers
                        historyValidity *= ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

                    // Apply the temporal denoiser
                    HDTemporalFilter temporalFilter = GetTemporalFilter();
                    temporalFilter.DenoiseBuffer(cmd, hdCamera, intermediateBuffer0, shadowHistoryArray, shadowHistoryValidityArray, velocityBuffer, intermediateBuffer1, lightData.screenSpaceShadowIndex / 4, m_ShadowChannelMask0, singleChannel: true, historyValidity: historyValidity);

                    // Apply the spatial denoiser
                    HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                    simpleDenoiser.DenoiseBufferNoHistory(cmd, hdCamera, intermediateBuffer1, intermediateBuffer0, additionalLightData.filterSizeTraced, singleChannel: true);

                // Now that we have overriden this history, mark is as used by this light
                hdCamera.PropagateShadowHistory(additionalLightData, lightData.screenSpaceShadowIndex, lightData.lightType);
            }

                // Write the result texture to the screen space shadow buffer
                WriteToScreenSpaceShadowBuffer(cmd, hdCamera, intermediateBuffer0, lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale);
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

                RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);

                // Clear the output texture
                CoreUtils.SetRenderTarget(cmd, intermediateBuffer0, clearFlag: ClearFlag.Color);

                // If the screen space shadows we are asked to deliver is available output it to the intermediate texture
                if (m_ScreenSpaceShadowChannelSlot > hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex)
                {
                    int targetKernel = shadowFilter.FindKernel("WriteShadowTextureDebug");
                    cmd.SetComputeIntParam(shadowFilter, HDShaderIDs._DenoisingHistorySlot, (int)hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex);
                    cmd.SetComputeTextureParam(shadowFilter, targetKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, m_ScreenSpaceShadowTextureArray);
                    cmd.SetComputeTextureParam(shadowFilter, targetKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBuffer0);
                    cmd.DispatchCompute(shadowFilter, targetKernel, numTilesX, numTilesY, hdCamera.viewCount);
                }

                // Push the full screen debug texture
                hdrp.PushFullScreenDebugTexture(hdCamera, cmd, intermediateBuffer0, FullScreenDebugMode.ScreenSpaceShadows);
            }
        }
    }
}
