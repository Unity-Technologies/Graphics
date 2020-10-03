using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        int m_AreaRaytracingShadowPrepassKernel;
        int m_AreaRaytracingShadowNewSampleKernel;
        int m_AreaShadowApplyTAAKernel;
        int m_AreaUpdateAnalyticHistoryKernel;
        int m_AreaUpdateShadowHistoryKernel;
        int m_AreaEstimateNoiseKernel;
        int m_AreaFirstDenoiseKernel;
        int m_AreaSecondDenoiseKernel;
        int m_AreaShadowNoDenoiseKernel;
        int m_WriteShadowTextureDebugKernel;

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
                enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("{0}_ScreenSpaceShadowHistoryBuffer{1}", viewName, frameIndex));
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
                        enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("{0}_ShadowHistoryValidityBuffer{1}", viewName, frameIndex));
        }

        static RTHandle ShadowHistoryDistanceBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            GraphicsFormat graphicsFormat = (GraphicsFormat)hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            int numShadowSlices = Math.Max((int)Math.Ceiling(hdrp.m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            return rtHandleSystem.Alloc(Vector2.one, slices: numShadowSlices * TextureXR.slices, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: graphicsFormat,
                        enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("{0}_ShadowHistoryDistanceBuffer{1}", viewName, frameIndex));
        }

        RTHandle RequestShadowHistoryBuffer(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory, ShadowHistoryBufferAllocatorFunction, 1);
        }

        RTHandle RequestShadowHistoryValidityBuffer(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity, ShadowHistoryValidityBufferAllocatorFunction, 1);
        }

        RTHandle RequestShadowHistoryDistanceBuffer(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowDistanceValidity)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowDistanceValidity, ShadowHistoryDistanceBufferAllocatorFunction, 1);
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
                m_AreaRaytracingShadowPrepassKernel = m_ScreenSpaceShadowsCS.FindKernel("RaytracingAreaShadowPrepass");
                m_AreaRaytracingShadowNewSampleKernel = m_ScreenSpaceShadowsCS.FindKernel("RaytracingAreaShadowNewSample");
                m_AreaShadowApplyTAAKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowApplyTAA");
                m_AreaUpdateAnalyticHistoryKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaAnalyticHistoryCopy");
                m_AreaUpdateShadowHistoryKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowHistoryCopy");
                m_AreaEstimateNoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowEstimateNoise");
                m_AreaFirstDenoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowDenoiseFirstPass");
                m_AreaSecondDenoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowDenoiseSecondPass");
                m_AreaShadowNoDenoiseKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("AreaShadowNoDenoise");

                // Debug kernel
                m_WriteShadowTextureDebugKernel = m_ScreenSpaceShadowsFilterCS.FindKernel("WriteShadowTextureDebug");
            }

            // Directional shadow material
            s_ScreenSpaceShadowsMat = CoreUtils.CreateEngineMaterial(screenSpaceShadowsShader);

            switch (m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowFilteringQuality)
            {
                case HDShadowFilteringQuality.Low:
                    s_ScreenSpaceShadowsMat.EnableKeyword("SHADOW_LOW");
                    break;
                case HDShadowFilteringQuality.Medium:
                    s_ScreenSpaceShadowsMat.EnableKeyword("SHADOW_MEDIUM");
                    break;
                case HDShadowFilteringQuality.High:
                    s_ScreenSpaceShadowsMat.EnableKeyword("SHADOW_HIGH");
                    break;
                default:
                    s_ScreenSpaceShadowsMat.EnableKeyword("SHADOW_MEDIUM");
                    break;
            }
        }

        void ScreenSpaceShadowInitializeNonRenderGraphResources()
        {
            // Allocate the final result texture
            int numShadowTextures = Math.Max((int)Math.Ceiling(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            GraphicsFormat graphicsFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            m_ScreenSpaceShadowTextureArray = RTHandles.Alloc(Vector2.one, slices: numShadowTextures * TextureXR.slices, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: graphicsFormat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AreaShadowArrayBuffer");
        }

        void ScreenSpaceShadowCleanupNonRenderGraphResources()
        {
            RTHandles.Release(m_ScreenSpaceShadowTextureArray);
            m_ScreenSpaceShadowTextureArray = null;
        }

        void ReleaseScreenSpaceShadows()
        {
            CoreUtils.Destroy(s_ScreenSpaceShadowsMat);
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

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ScreenSpaceShadows)))
            {
                // First of all we handle the directional light
                RenderDirectionalLightScreenSpaceShadow(cmd, hdCamera);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                {
                    // We handle the other light sources
                    RenderLightScreenSpaceShadows(hdCamera, cmd);
                }

                // We do render the debug view
                EvaluateShadowDebugView(cmd, hdCamera);

                // Bind the right texture
                cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, m_ScreenSpaceShadowTextureArray);
            }
        }

        bool RenderLightScreenSpaceShadows(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingLightShadow)))
            {
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
                                RenderAreaScreenSpaceShadow(cmd, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex);
                            }
                            break;
                            case GPULightType.Point:
                            case GPULightType.Spot:
                            {
                                RenderPunctualScreenSpaceShadow(cmd, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex);
                            }
                            break;
                        }
                    }
                }
                return true;
            }
        }

        struct WriteScreenSpaceShadowParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public Vector4 shadowChannelMask;
            public int shadowSlot;

            // Kernel
            public int shadowKernel;

            // Other parameters
            public ComputeShader screenSpaceShadowCS;
        }

        WriteScreenSpaceShadowParameters PrepareWriteScreenSpaceShadowParameters(HDCamera hdCamera, int shadowSlot, ScreenSpaceShadowType shadowType)
        {
            WriteScreenSpaceShadowParameters wsssParams = new WriteScreenSpaceShadowParameters();

            // Set the camera parameters
            wsssParams.texWidth = hdCamera.actualWidth;
            wsssParams.texHeight = hdCamera.actualHeight;
            wsssParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            GetShadowChannelMask(shadowSlot, shadowType, ref wsssParams.shadowChannelMask);
            wsssParams.shadowSlot = shadowSlot;

            // Kernel
            wsssParams.shadowKernel = (shadowType == ScreenSpaceShadowType.Color ? m_OutputColorShadowTextureKernel : m_OutputShadowTextureKernel);

            // Other parameters
            wsssParams.screenSpaceShadowCS = m_ScreenSpaceShadowsCS;

            return wsssParams;
        }

        struct WriteScreenSpaceShadowResources
        {
            public RTHandle inputShadowBuffer;
            public RTHandle outputShadowArrayBuffer;
        }

        WriteScreenSpaceShadowResources PrepareWriteScreenSpaceShadowResources(RTHandle inputShadowBuffer)
        {
            WriteScreenSpaceShadowResources wsssResources = new WriteScreenSpaceShadowResources();
            wsssResources.inputShadowBuffer = inputShadowBuffer;
            wsssResources.outputShadowArrayBuffer = m_ScreenSpaceShadowTextureArray;
            return wsssResources;
        }

        static void ExecuteWriteScreenSpaceShadow(CommandBuffer cmd, WriteScreenSpaceShadowParameters wsssParams, WriteScreenSpaceShadowResources wsssResources)
        {
            // Evaluate the dispatch parameters
            int shadowTileSize = 8;
            int numTilesX = (wsssParams.texWidth + (shadowTileSize - 1)) / shadowTileSize;
            int numTilesY = (wsssParams.texHeight + (shadowTileSize - 1)) / shadowTileSize;

            // Bind the input data
            cmd.SetComputeIntParam(wsssParams.screenSpaceShadowCS, HDShaderIDs._RaytracingShadowSlot, wsssParams.shadowSlot / 4);
            cmd.SetComputeVectorParam(wsssParams.screenSpaceShadowCS, HDShaderIDs._RaytracingChannelMask, wsssParams.shadowChannelMask);
            cmd.SetComputeTextureParam(wsssParams.screenSpaceShadowCS, wsssParams.shadowKernel, HDShaderIDs._RaytracedShadowIntegration, wsssResources.inputShadowBuffer);

            // Bind the output texture
            cmd.SetComputeTextureParam(wsssParams.screenSpaceShadowCS, wsssParams.shadowKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, wsssResources.outputShadowArrayBuffer);

            //Do our copy
            cmd.DispatchCompute(wsssParams.screenSpaceShadowCS, wsssParams.shadowKernel, numTilesX, numTilesY, wsssParams.viewCount);
        }

        struct SSShadowDebugParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public int targetShadow;

            // Kernel
            public int debugKernel;

            // Other parameters
            public ComputeShader shadowFilter;
        }

        SSShadowDebugParameters PrepareSSShadowDebugParameters(HDCamera hdCamera, int targetShadow)
        {
            SSShadowDebugParameters sssdParams = new SSShadowDebugParameters();

            // Set the camera parameters
            sssdParams.texWidth = hdCamera.actualWidth;
            sssdParams.texHeight = hdCamera.actualHeight;
            sssdParams.viewCount = hdCamera.viewCount;

            // Evaluation params
            sssdParams.targetShadow = targetShadow;

            // Kernel to be used
            sssdParams.debugKernel = m_WriteShadowTextureDebugKernel;

            // TODO: move the debug kernel outside of the ray tracing resources
            sssdParams.shadowFilter = m_Asset.renderPipelineRayTracingResources.shadowFilterCS;
            return sssdParams;
        }

        struct SSShadowDebugResources
        {
            public RTHandle screenSpaceShadowArray;
            public RTHandle outputBuffer;
        }

        SSShadowDebugResources PrepareSSShadowDebugResources(RTHandle debugResultBuffer)
        {
            SSShadowDebugResources sssdResources = new SSShadowDebugResources();
            sssdResources.screenSpaceShadowArray = m_ScreenSpaceShadowTextureArray;
            sssdResources.outputBuffer = debugResultBuffer;
            return sssdResources;
        }

        static void ExecuteShadowDebugView(CommandBuffer cmd, SSShadowDebugParameters sssdParams, SSShadowDebugResources sssdResources)
        {
            // Evaluate the dispatch parameters
            int shadowTileSize = 8;
            int numTilesX = (sssdParams.texWidth + (shadowTileSize - 1)) / shadowTileSize;
            int numTilesY = (sssdParams.texHeight + (shadowTileSize - 1)) / shadowTileSize;

            // If the screen space shadows we are asked to deliver is available output it to the intermediate texture
            cmd.SetComputeIntParam(sssdParams.shadowFilter, HDShaderIDs._DenoisingHistorySlot, sssdParams.targetShadow);
            cmd.SetComputeTextureParam(sssdParams.shadowFilter, sssdParams.debugKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, sssdResources.screenSpaceShadowArray);
            cmd.SetComputeTextureParam(sssdParams.shadowFilter, sssdParams.debugKernel, HDShaderIDs._DenoiseOutputTextureRW, sssdResources.outputBuffer);
            cmd.DispatchCompute(sssdParams.shadowFilter, sssdParams.debugKernel, numTilesX, numTilesY, sssdParams.viewCount);
        }

        void EvaluateShadowDebugView(CommandBuffer cmd, HDCamera hdCamera)
        {
            // If this is the right debug mode and the index we are asking for is in the range
            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            if (FullScreenDebugMode.ScreenSpaceShadows == hdrp.m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                if (!hdrp.rayTracingSupported || (m_ScreenSpaceShadowChannelSlot <= hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex))
                {
                    // In this case we have not rendered any screenspace shadows, so push a black texture on the debug display
                    hdrp.PushFullScreenDebugTexture(hdCamera, cmd, TextureXR.GetBlackTextureArray(), FullScreenDebugMode.ScreenSpaceShadows);
                    return;
                }

                // Fetch the buffer where we we will store our result
                RTHandle debugResultBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);

                // Generate the debug view
                SSShadowDebugParameters sssdParams = PrepareSSShadowDebugParameters(hdCamera, (int)hdrp.m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex);
                SSShadowDebugResources sssdResources = PrepareSSShadowDebugResources(debugResultBuffer);
                ExecuteShadowDebugView(cmd, sssdParams, sssdResources);

                // Push the full screen debug texture
                hdrp.PushFullScreenDebugTexture(hdCamera, cmd, debugResultBuffer, FullScreenDebugMode.ScreenSpaceShadows);
            }
        }
    }
}
