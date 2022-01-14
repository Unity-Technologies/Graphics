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

        // Shaders
        RayTracingShader m_ScreenSpaceShadowsRT;
        ComputeShader m_ScreenSpaceShadowsCS;
        ComputeShader m_ScreenSpaceShadowsFilterCS;

        // Kernels
        // Shared shadow kernels
        int m_ClearShadowTexture;
        int m_OutputShadowTextureKernel;
        int m_OutputColorShadowTextureKernel;
        int m_OutputSpecularShadowTextureKernel;

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
            HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            HDRenderPipelineAsset hdPipelineAsset = hdrp.m_Asset;
            GraphicsFormat graphicsFormat = (GraphicsFormat)hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            int numShadowSlices = Math.Max((int)Math.Ceiling(hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            return rtHandleSystem.Alloc(Vector2.one, slices: numShadowSlices * TextureXR.slices, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: graphicsFormat,
                enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("{0}_ScreenSpaceShadowHistoryBuffer{1}", viewName, frameIndex));
        }

        // This value holds additional values that are required for the filtering process.
        // For directional, punctual and spot light it holds the sample accumulation count and for the area light it holds the analytic value.
        // It is accessed with the same index used at runtime (aka screen space shadow slot)
        static RTHandle ShadowHistoryValidityBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            HDRenderPipelineAsset hdPipelineAsset = hdrp.m_Asset;
            GraphicsFormat graphicsFormat = (GraphicsFormat)hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            int numShadowSlices = Math.Max((int)Math.Ceiling(hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            return rtHandleSystem.Alloc(Vector2.one, slices: numShadowSlices * TextureXR.slices, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: graphicsFormat,
                enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("{0}_ShadowHistoryValidityBuffer{1}", viewName, frameIndex));
        }

        static RTHandle ShadowHistoryDistanceBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            HDRenderPipelineAsset hdPipelineAsset = hdrp.m_Asset;
            GraphicsFormat graphicsFormat = (GraphicsFormat)hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            int numShadowSlices = Math.Max((int)Math.Ceiling(hdPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
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
                m_ScreenSpaceShadowsCS = m_GlobalSettings.renderPipelineRayTracingResources.shadowRaytracingCS;
                m_ScreenSpaceShadowsFilterCS = m_GlobalSettings.renderPipelineRayTracingResources.shadowFilterCS;
                m_ScreenSpaceShadowsRT = m_GlobalSettings.renderPipelineRayTracingResources.shadowRaytracingRT;

                // Directional shadow kernels
                m_ClearShadowTexture = m_ScreenSpaceShadowsCS.FindKernel("ClearShadowTexture");
                m_OutputShadowTextureKernel = m_ScreenSpaceShadowsCS.FindKernel("OutputShadowTexture");
                m_OutputColorShadowTextureKernel = m_ScreenSpaceShadowsCS.FindKernel("OutputColorShadowTexture");
                m_OutputSpecularShadowTextureKernel = m_ScreenSpaceShadowsCS.FindKernel("OutputSpecularShadowTexture");
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
                case HDShadowFilteringQuality.VeryHigh:
                    s_ScreenSpaceShadowsMat.EnableKeyword("SHADOW_VERY_HIGH");
                    break;
                default:
                    s_ScreenSpaceShadowsMat.EnableKeyword("SHADOW_MEDIUM");
                    break;
            }
        }

        void ReleaseScreenSpaceShadows()
        {
            CoreUtils.Destroy(s_ScreenSpaceShadowsMat);
        }

        internal TextureHandle CreateScreenSpaceShadowTextureArray(RenderGraph renderGraph)
        {
            int numShadowTextures = Math.Max((int)Math.Ceiling(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            GraphicsFormat graphicsFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = graphicsFormat,
                slices = numShadowTextures * TextureXR.slices,
                dimension = TextureDimension.Tex2DArray,
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "ScreenSpaceShadowArrayBuffer"
            });
        }

        class ScreenSpaceShadowDebugPassData
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

            public TextureHandle screenSpaceShadowArray;
            public TextureHandle outputBuffer;
        }

        TextureHandle EvaluateShadowDebugView(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle screenSpaceShadowArray)
        {
            // If this is the right debug mode and the index we are asking for is in the range
            if (!rayTracingSupported || (m_ScreenSpaceShadowChannelSlot <= m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex))
                return m_RenderGraph.defaultResources.blackTextureXR;

            using (var builder = renderGraph.AddRenderPass<ScreenSpaceShadowDebugPassData>("Screen Space Shadows Debug", out var passData, ProfilingSampler.Get(HDProfileId.ScreenSpaceShadowsDebug)))
            {
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation params
                passData.targetShadow = (int)m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex;

                // Kernel to be used
                passData.debugKernel = m_WriteShadowTextureDebugKernel;

                // TODO: move the debug kernel outside of the ray tracing resources
                passData.shadowFilter = m_GlobalSettings.renderPipelineRayTracingResources.shadowFilterCS;

                passData.screenSpaceShadowArray = builder.ReadTexture(screenSpaceShadowArray);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "EvaluateShadowDebug" }));

                builder.SetRenderFunc(
                    (ScreenSpaceShadowDebugPassData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int shadowTileSize = 8;
                        int numTilesX = (data.texWidth + (shadowTileSize - 1)) / shadowTileSize;
                        int numTilesY = (data.texHeight + (shadowTileSize - 1)) / shadowTileSize;

                        // If the screen space shadows we are asked to deliver is available output it to the intermediate texture
                        ctx.cmd.SetComputeIntParam(data.shadowFilter, HDShaderIDs._DenoisingHistorySlot, data.targetShadow);
                        ctx.cmd.SetComputeTextureParam(data.shadowFilter, data.debugKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, data.screenSpaceShadowArray);
                        ctx.cmd.SetComputeTextureParam(data.shadowFilter, data.debugKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);
                        ctx.cmd.DispatchCompute(data.shadowFilter, data.debugKernel, numTilesX, numTilesY, data.viewCount);
                    });
                return passData.outputBuffer;
            }
        }

        class WriteScreenSpaceShadowPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public Vector4 shadowChannelMask;
            public Vector4 shadowChannelMask0;
            public Vector4 shadowChannelMask1;
            public int shadowSlot;

            // Kernel
            public int shadowKernel;

            // Other parameters
            public ComputeShader screenSpaceShadowCS;

            public TextureHandle inputShadowBuffer;
            public TextureHandle outputShadowArrayBuffer;
        }

        void WriteScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle shadowTexture, TextureHandle screenSpaceShadowArray, int shadowIndex, ScreenSpaceShadowType shadowType)
        {
            // Write the result texture to the screen space shadow buffer
            using (var builder = renderGraph.AddRenderPass<WriteScreenSpaceShadowPassData>("Write Screen Space Shadows", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingWriteShadow)))
            {
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                GetShadowChannelMask(shadowIndex, shadowType, ref passData.shadowChannelMask);
                // If the light is an area, we also need to grab the individual channels
                if (shadowType == ScreenSpaceShadowType.Area)
                {
                    GetShadowChannelMask(shadowIndex, ScreenSpaceShadowType.GrayScale, ref passData.shadowChannelMask0);
                    GetShadowChannelMask(shadowIndex + 1, ScreenSpaceShadowType.GrayScale, ref passData.shadowChannelMask1);
                }
                passData.shadowSlot = shadowIndex;

                // Kernel
                switch (shadowType)
                {
                    case ScreenSpaceShadowType.GrayScale:
                    {
                        passData.shadowKernel = m_OutputShadowTextureKernel;
                    }
                    break;
                    case ScreenSpaceShadowType.Area:
                    {
                        passData.shadowKernel = m_OutputSpecularShadowTextureKernel;
                    }
                    break;
                    case ScreenSpaceShadowType.Color:
                    {
                        passData.shadowKernel = m_OutputColorShadowTextureKernel;
                    }
                    break;
                }

                // Other parameters
                passData.screenSpaceShadowCS = m_ScreenSpaceShadowsCS;

                passData.inputShadowBuffer = builder.ReadTexture(shadowTexture);
                passData.outputShadowArrayBuffer = builder.ReadWriteTexture(screenSpaceShadowArray);

                builder.SetRenderFunc(
                    (WriteScreenSpaceShadowPassData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int shadowTileSize = 8;
                        int numTilesX = (data.texWidth + (shadowTileSize - 1)) / shadowTileSize;
                        int numTilesY = (data.texHeight + (shadowTileSize - 1)) / shadowTileSize;

                        // Bind the input data
                        ctx.cmd.SetComputeIntParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingShadowSlot, data.shadowSlot / 4);
                        ctx.cmd.SetComputeVectorParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingChannelMask, data.shadowChannelMask);
                        ctx.cmd.SetComputeVectorParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingChannelMask0, data.shadowChannelMask0);
                        ctx.cmd.SetComputeVectorParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingChannelMask1, data.shadowChannelMask1);
                        ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.inputShadowBuffer);

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, data.outputShadowArrayBuffer);

                        //Do our copy
                        ctx.cmd.DispatchCompute(data.screenSpaceShadowCS, data.shadowKernel, numTilesX, numTilesY, data.viewCount);
                    });
            }
        }

        bool RenderLightScreenSpaceShadows(RenderGraph renderGraph, HDCamera hdCamera,
            PrepassOutput prepassOutput, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle historyValidityBuffer,
            TextureHandle rayCountTexture, TextureHandle screenSpaceShadowArray)
        {
            // Loop through all the potential screen space light shadows
            for (int lightIdx = 0; lightIdx < m_ScreenSpaceShadowIndex; ++lightIdx)
            {
                // This matches the directional light
                if (!m_CurrentScreenSpaceShadowData[lightIdx].valid) continue;

                // Fetch the light data and additional light data
                LightData currentLight = m_GpuLightsBuilder.lights[m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex];
                HDAdditionalLightData currentAdditionalLightData = m_CurrentScreenSpaceShadowData[lightIdx].additionalLightData;

                // Trigger the right algorithm based on the light type
                switch (currentLight.lightType)
                {
                    case GPULightType.Rectangle:
                    {
                        RenderAreaScreenSpaceShadow(renderGraph, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex,
                            prepassOutput, depthBuffer, normalBuffer, motionVectorsBuffer, rayCountTexture, screenSpaceShadowArray);
                    }
                    break;
                    case GPULightType.Point:
                    case GPULightType.Spot:
                    {
                        RenderPunctualScreenSpaceShadow(renderGraph, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex,
                            prepassOutput, depthBuffer, normalBuffer, motionVectorsBuffer, historyValidityBuffer, rayCountTexture, screenSpaceShadowArray);
                    }
                    break;
                }
            }
            return true;
        }

        bool RequestedScreenSpaceShadows()
        {
            // We have screen space shadows that needs to be evaluated if we have one of these:
            // - A screen space directional shadow
            // - A ray traced directional shadow
            bool screenSpaceShadowDirectionalRequired = m_CurrentSunLightAdditionalLightData != null && (m_CurrentSunShadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderScreenSpaceShadow) != 0;
            // - A ray traced spot or point shadow
            // - A ray traced area light shadow
            bool pointOrAreaLightShadowRequired = false;
            for (int lightIdx = 0; lightIdx < m_ScreenSpaceShadowIndex; ++lightIdx)
            {
                // This matches the directional light
                if (!m_CurrentScreenSpaceShadowData[lightIdx].valid) continue;

                pointOrAreaLightShadowRequired = true;
                break;
            }

            return screenSpaceShadowDirectionalRequired || pointOrAreaLightShadowRequired;
        }

        TextureHandle RenderScreenSpaceShadows(RenderGraph renderGraph, HDCamera hdCamera,
            PrepassOutput prepassOutput, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle historyValidityBuffer, TextureHandle rayCountTexture)
        {
            // If screen space shadows are not supported for this camera, we are done
            bool validConditions = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows) && RequestedScreenSpaceShadows();
            if (!validConditions)
            {
                // We push the debug texture anyway if we are not evaluating any screen space shadows.
                PushFullScreenDebugTexture(m_RenderGraph, m_RenderGraph.defaultResources.whiteTextureXR, FullScreenDebugMode.ScreenSpaceShadows);
                return m_RenderGraph.defaultResources.blackTextureArrayXR;
            }

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.ScreenSpaceShadows)))
            {
                // Request the output texture
                TextureHandle screenSpaceShadowTexture = CreateScreenSpaceShadowTextureArray(renderGraph);

                // First of all we handle the directional light
                RenderDirectionalLightScreenSpaceShadow(renderGraph, hdCamera, depthBuffer, normalBuffer, motionVectorsBuffer, historyValidityBuffer, rayCountTexture, screenSpaceShadowTexture);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                {
                    // We handle the other light sources
                    RenderLightScreenSpaceShadows(renderGraph, hdCamera, prepassOutput, depthBuffer, normalBuffer, motionVectorsBuffer, historyValidityBuffer, rayCountTexture, screenSpaceShadowTexture);
                }

                // We render the debug view, if the texture is not used, it is not evaluated anyway
                TextureHandle screenSpaceShadowDebug = EvaluateShadowDebugView(renderGraph, hdCamera, screenSpaceShadowTexture);
                PushFullScreenDebugTexture(m_RenderGraph, screenSpaceShadowDebug, FullScreenDebugMode.ScreenSpaceShadows);

                return screenSpaceShadowTexture;
            }
        }
    }
}
