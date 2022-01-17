using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct VBufferOITOutput
        {
            public bool valid;
            public TextureHandle stencilBuffer;
            public ComputeBufferHandle histogramBuffer;
            public ComputeBufferHandle prefixedHistogramBuffer;
            public ComputeBufferHandle sampleListCountBuffer;
            public ComputeBufferHandle sampleListOffsetBuffer;
            public ComputeBufferHandle sublistCounterBuffer;
            public ComputeBufferHandle oitVisibilityBuffer;
            public RenderBRGBindingData BRGBindingData;

            public static VBufferOITOutput NewDefault()
            {
                return new VBufferOITOutput()
                {
                    valid = false,
                    stencilBuffer = TextureHandle.nullHandle,
                    BRGBindingData = RenderBRGBindingData.NewDefault()
                };
            }

            public VBufferOITOutput Read(RenderGraphBuilder builder)
            {
                VBufferOITOutput readVBuffer = VBufferOITOutput.NewDefault();
                readVBuffer.valid = valid;
                readVBuffer.stencilBuffer = builder.ReadTexture(stencilBuffer);
                if (!valid)
                    return readVBuffer;

                readVBuffer.histogramBuffer = builder.ReadComputeBuffer(histogramBuffer);
                readVBuffer.prefixedHistogramBuffer = builder.ReadComputeBuffer(prefixedHistogramBuffer);
                readVBuffer.sampleListCountBuffer = builder.ReadComputeBuffer(sampleListCountBuffer);
                readVBuffer.sampleListOffsetBuffer = builder.ReadComputeBuffer(sampleListOffsetBuffer);
                readVBuffer.sublistCounterBuffer = builder.ReadComputeBuffer(sublistCounterBuffer);
                readVBuffer.oitVisibilityBuffer = builder.ReadComputeBuffer(oitVisibilityBuffer);
                readVBuffer.BRGBindingData = BRGBindingData;
                return readVBuffer;
            }
        }

        int GetOITVisibilityBufferSize()
        {
            return sizeof(uint) * 3; //12 bytes
        }

        int GetMaxMaterialOITSampleCount()
        {
            float budget = currentAsset.currentPlatformRenderPipelineSettings.orderIndependentTransparentSettings.memoryBudget;
            float coef;
            if (currentAsset.currentPlatformRenderPipelineSettings.orderIndependentTransparentSettings.oitLightingMode == OITLightingMode.DeferredSSTracing)
            {
                coef = 2.0f; // { Normal_Roughess, DiffuseAlbedo }
            }
            else
            {
                coef = 1.0f;
            }
            float availableBytes = coef * budget * 1024.0f * 1024.0f;

            //for now store visibility
            float visibilityCost = GetOITVisibilityBufferSize();
            return (int)Math.Max(1, Math.Ceiling(availableBytes / visibilityCost));
        }

        internal bool IsVisibilityOITPassEnabled()
        {
            return currentAsset != null && currentAsset.VisibilityOITMaterial != null && currentAsset.currentPlatformRenderPipelineSettings.orderIndependentTransparentSettings.enabled && RenderBRG.GetRenderBRGMaterialBindingData().valid; ;
        }

        void RenderVBufferOIT(RenderGraph renderGraph, TextureHandle colorBuffer, HDCamera hdCamera, CullingResults cullResults, ref PrepassOutput output)
        {
            output.vbufferOIT = VBufferOITOutput.NewDefault();

            var BRGBindingData = RenderBRG.GetRenderBRGMaterialBindingData();
            if (!IsVisibilityOITPassEnabled())
            {
                output.vbufferOIT.stencilBuffer = renderGraph.defaultResources.blackTextureXR;
                return;
            }

            output.vbufferOIT.valid = true;

            output.vbufferOIT.stencilBuffer = RenderVBufferOITCountPass(renderGraph, colorBuffer, hdCamera, cullResults, BRGBindingData);

            var screenSize = new Vector2Int((int)hdCamera.screenSize.x, (int)hdCamera.screenSize.y);
            int histogramSize, tileSize;

            var histogramBuffer = ComputeOITTiledHistogram(renderGraph, screenSize, hdCamera.viewCount, output.vbufferOIT.stencilBuffer, out histogramSize, out tileSize);
            var prefixedHistogramBuffer = ComputeOITTiledPrefixSumHistogramBuffer(renderGraph, histogramBuffer, histogramSize);

            int maxMaterialSampleCount = GetMaxMaterialOITSampleCount();
            ComputeOITAllocateSampleLists(
                renderGraph, maxMaterialSampleCount, screenSize, output.vbufferOIT.stencilBuffer, prefixedHistogramBuffer,
                out ComputeBufferHandle sampleListCountBuffer, out ComputeBufferHandle sampleListOffsetBuffer, out ComputeBufferHandle sublistCounterBuffer);

            ComputeBufferHandle oitVisibilityBuffer = RenderVBufferOITStoragePass(
                renderGraph, maxMaterialSampleCount, hdCamera, cullResults, BRGBindingData,
                sampleListCountBuffer, sampleListOffsetBuffer, ref sublistCounterBuffer);

            output.vbufferOIT.histogramBuffer = histogramBuffer;
            output.vbufferOIT.prefixedHistogramBuffer = prefixedHistogramBuffer;
            output.vbufferOIT.sampleListCountBuffer = sampleListCountBuffer;
            output.vbufferOIT.sampleListOffsetBuffer = sampleListOffsetBuffer;
            output.vbufferOIT.sublistCounterBuffer = sublistCounterBuffer;
            output.vbufferOIT.oitVisibilityBuffer = oitVisibilityBuffer;
            output.vbufferOIT.BRGBindingData = BRGBindingData;
        }


        class VBufferOITCountPassData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
            public RenderBRGBindingData BRGBindingData;
        }

        TextureHandle RenderVBufferOITCountPass(RenderGraph renderGraph, TextureHandle colorBuffer, HDCamera hdCamera, CullingResults cullResults, in RenderBRGBindingData BRGBindingData)
        {
            TextureHandle outputStencilCount;
            using (var builder = renderGraph.AddRenderPass<VBufferOITCountPassData>("VBufferOITCount", out var passData, ProfilingSampler.Get(HDProfileId.VBufferOITCount)))
            {
                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;

                outputStencilCount = builder.UseDepthBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                {
                    depthBufferBits = DepthBits.Depth24,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "VisOITStencilCount"
                }), DepthAccess.ReadWrite);

                passData.BRGBindingData = BRGBindingData;
                passData.rendererList = builder.UseRendererList(
                   renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(
                        cullResults, hdCamera.camera,
                        HDShaderPassNames.s_VBufferOITCountName,
                        m_CurrentRendererConfigurationBakedLighting,
                        new RenderQueueRange() { lowerBound = (int)HDRenderQueue.Priority.OrderIndependentTransparent, upperBound = (int)(int)HDRenderQueue.Priority.OrderIndependentTransparent })));

                builder.SetRenderFunc(
                    (VBufferOITCountPassData data, RenderGraphContext context) =>
                    {
                        data.BRGBindingData.globalGeometryPool.BindResourcesGlobal(context.cmd);
                        DrawTransparentRendererList(context, data.frameSettings, data.rendererList);
                    });
            }

            return outputStencilCount;
        }

        class OITTileHistogramPassData
        {
            public int tileSize;
            public int histogramSize;
            public Vector2Int screenSize;
            public ComputeShader cs;
            public Texture2D ditherTexture;
            public TextureHandle stencilBuffer;
            public ComputeBufferHandle histogramBuffer;
        }

        ComputeBufferHandle ComputeOITTiledHistogram(RenderGraph renderGraph, Vector2Int screenSize, int viewCount, TextureHandle stencilBuffer, out int histogramSize, out int tileSize)
        {
            ComputeBufferHandle histogramBuffer = ComputeBufferHandle.nullHandle;
            tileSize = 128;
            histogramSize = tileSize * tileSize;
            using (var builder = renderGraph.AddRenderPass<OITTileHistogramPassData>("OITTileHistogramPassData", out var passData, ProfilingSampler.Get(HDProfileId.OITHistogram)))
            {
                passData.cs = defaultResources.shaders.oitTileHistogramCS;
                passData.screenSize = screenSize;
                passData.tileSize = tileSize;
                passData.ditherTexture = defaultResources.textures.blueNoise128RTex;
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.histogramSize = histogramSize;
                passData.histogramBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(new ComputeBufferDesc(histogramSize, sizeof(uint), ComputeBufferType.Raw) { name = "OITHistogram" }));
                histogramBuffer = passData.histogramBuffer;

                builder.SetRenderFunc(
                    (OITTileHistogramPassData data, RenderGraphContext context) =>
                    {
                        int clearKernel = data.cs.FindKernel("MainClearHistogram");
                        context.cmd.SetComputeBufferParam(data.cs, clearKernel, HDShaderIDs._VisOITHistogramOutput, data.histogramBuffer);
                        context.cmd.DispatchCompute(data.cs, clearKernel, HDUtils.DivRoundUp(passData.histogramSize, 64), 1, 1);

                        int histogramKernel = data.cs.FindKernel("MainCreateStencilHistogram");
                        context.cmd.SetComputeTextureParam(data.cs, histogramKernel, HDShaderIDs._OITDitherTexture, data.ditherTexture);
                        context.cmd.SetComputeTextureParam(data.cs, histogramKernel, HDShaderIDs._VisOITCount, (RenderTexture)data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        context.cmd.SetComputeBufferParam(data.cs, histogramKernel, HDShaderIDs._VisOITHistogramOutput, data.histogramBuffer);
                        context.cmd.DispatchCompute(data.cs, histogramKernel, HDUtils.DivRoundUp(data.screenSize.x, 8), HDUtils.DivRoundUp(data.screenSize.y, 8), viewCount);
                    });
            }

            return histogramBuffer;
        }

        class OITHistogramPrefixSumPassData
        {
            public int inputCount;
            public ComputeBufferHandle inputBuffer;
            public GpuPrefixSum prefixSumSystem;
            public GpuPrefixSumRenderGraphResources resources;
        }

        ComputeBufferHandle ComputeOITTiledPrefixSumHistogramBuffer(RenderGraph renderGraph, ComputeBufferHandle histogramInput, int histogramSize)
        {
            ComputeBufferHandle output;
            using (var builder = renderGraph.AddRenderPass<OITHistogramPrefixSumPassData>("OITHistogramPrefixSum", out var passData, ProfilingSampler.Get(HDProfileId.OITHistogramPrefixSum)))
            {
                passData.inputCount = histogramSize;
                passData.inputBuffer = builder.ReadComputeBuffer(histogramInput);
                passData.prefixSumSystem = m_PrefixSumSystem;
                passData.resources = GpuPrefixSumRenderGraphResources.Create(histogramSize, renderGraph, builder);
                output = passData.resources.output;

                builder.SetRenderFunc(
                    (OITHistogramPrefixSumPassData data, RenderGraphContext context) =>
                    {
                        var resources = GpuPrefixSumSupportResources.Load(data.resources);
                        data.prefixSumSystem.DispatchDirect(context.cmd, new GpuPrefixSumDirectArgs()
                        { exclusive = false, inputCount = data.inputCount, input = data.inputBuffer, supportResources = resources });
                    });
            }

            return output;
        }

        class OITAllocateSampleListsPassData
        {
            public ComputeShader cs;
            public GpuPrefixSum prefixSumSystem;
            public Vector2Int screenSize;
            public Vector4 packedArgs;
            public Texture2D ditherTexture;
            public TextureHandle stencilBuffer;
            public ComputeBufferHandle prefixedHistogramBuffer;
            public ComputeBufferHandle outCountBuffer;
            public ComputeBufferHandle outSublistCounterBuffer;
            public GpuPrefixSumRenderGraphResources prefixResources;
        }

        void ComputeOITAllocateSampleLists(
            RenderGraph renderGraph, int maxMaterialSampleCount, Vector2Int screenSize, TextureHandle stencilBuffer, ComputeBufferHandle prefixedHistogramBuffer,
            out ComputeBufferHandle outCountBuffer, out ComputeBufferHandle outOffsetBuffer, out ComputeBufferHandle outSublistCounterBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<OITAllocateSampleListsPassData>("OITAllocateSampleLists", out var passData, ProfilingSampler.Get(HDProfileId.OITAllocateSampleLists)))
            {
                passData.cs = defaultResources.shaders.oitTileHistogramCS;
                passData.prefixSumSystem = m_PrefixSumSystem;
                passData.screenSize = screenSize;
                passData.ditherTexture = defaultResources.textures.blueNoise128RTex;
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.prefixedHistogramBuffer = builder.ReadComputeBuffer(prefixedHistogramBuffer);
                passData.outCountBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(new ComputeBufferDesc(screenSize.x * screenSize.y, sizeof(uint), ComputeBufferType.Raw) { name = "OITMaterialCountBuffer" }));
                passData.outSublistCounterBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(new ComputeBufferDesc(screenSize.x * screenSize.y, sizeof(uint), ComputeBufferType.Raw) { name = "OITMaterialSublistCounter" }));
                passData.prefixResources = GpuPrefixSumRenderGraphResources.Create(screenSize.x * screenSize.y, renderGraph, builder);

                float maxMaterialSampleCountAsFloat; unsafe { maxMaterialSampleCountAsFloat = *((float*)&maxMaterialSampleCount); };
                passData.packedArgs = new Vector4(maxMaterialSampleCountAsFloat, 0.0f, 0.0f, 0.0f);

                outCountBuffer = passData.outCountBuffer;
                outOffsetBuffer = passData.prefixResources.output;
                outSublistCounterBuffer = passData.outSublistCounterBuffer;

                builder.SetRenderFunc(
                    (OITAllocateSampleListsPassData data, RenderGraphContext context) =>
                    {
                        int flatCountKernel = data.cs.FindKernel("MainFlatEnableActiveCounts");
                        context.cmd.SetComputeVectorParam(data.cs, HDShaderIDs._PackedOITTileHistogramArgs, data.packedArgs);
                        context.cmd.SetComputeTextureParam(data.cs, flatCountKernel, HDShaderIDs._OITDitherTexture, data.ditherTexture);
                        context.cmd.SetComputeTextureParam(data.cs, flatCountKernel, HDShaderIDs._VisOITCount, (RenderTexture)data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        context.cmd.SetComputeBufferParam(data.cs, flatCountKernel, HDShaderIDs._VisOITPrefixedHistogramBuffer, data.prefixedHistogramBuffer);
                        context.cmd.SetComputeBufferParam(data.cs, flatCountKernel, HDShaderIDs._OITOutputActiveCounts, data.outCountBuffer);
                        context.cmd.SetComputeBufferParam(data.cs, flatCountKernel, HDShaderIDs._OITOutputSublistCounter, data.outSublistCounterBuffer);
                        context.cmd.DispatchCompute(data.cs, flatCountKernel, HDUtils.DivRoundUp(data.screenSize.x, 8), HDUtils.DivRoundUp(data.screenSize.y, 8), 1);

                        var prefixResources = GpuPrefixSumSupportResources.Load(data.prefixResources);
                        data.prefixSumSystem.DispatchDirect(context.cmd, new GpuPrefixSumDirectArgs()
                        { exclusive = true, inputCount = data.screenSize.x * data.screenSize.y, input = data.outCountBuffer, supportResources = prefixResources });
                    });
            }
        }

        class VBufferOITStoragePassData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
            public RenderBRGBindingData BRGBindingData;
            public ComputeBufferHandle countBuffer;
            public ComputeBufferHandle offsetBuffer;
            public ComputeBufferHandle sublistCounterBuffer;
            public ComputeBufferHandle outVisibilityBuffer;
        }

        ComputeBufferHandle RenderVBufferOITStoragePass(
            RenderGraph renderGraph, int maxMaterialSampleCount, HDCamera hdCamera, CullingResults cullResults, in RenderBRGBindingData BRGBindingData,
            ComputeBufferHandle countBuffer, ComputeBufferHandle offsetBuffer, ref ComputeBufferHandle sublistCounterBuffer)
        {
            ComputeBufferHandle outVisibilityBuffer;
            using (var builder = renderGraph.AddRenderPass<VBufferOITStoragePassData>("VBufferOITStorage", out var passData, ProfilingSampler.Get(HDProfileId.VBufferOITStorage)))
            {
                builder.AllowRendererListCulling(false);

                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;

                passData.countBuffer = builder.ReadComputeBuffer(countBuffer);
                passData.offsetBuffer = builder.ReadComputeBuffer(offsetBuffer);
                passData.sublistCounterBuffer = builder.WriteComputeBuffer(sublistCounterBuffer);
                passData.outVisibilityBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(new ComputeBufferDesc(maxMaterialSampleCount, GetOITVisibilityBufferSize(), ComputeBufferType.Raw) { name = "OITVisibilityBuffer" }));

                outVisibilityBuffer = passData.outVisibilityBuffer;
                sublistCounterBuffer = passData.sublistCounterBuffer;

                passData.BRGBindingData = BRGBindingData;
                passData.rendererList = builder.UseRendererList(
                   renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(
                        cullResults, hdCamera.camera,
                        HDShaderPassNames.s_VBufferOITStorageName,
                        m_CurrentRendererConfigurationBakedLighting,
                        new RenderQueueRange() { lowerBound = (int)HDRenderQueue.Priority.OrderIndependentTransparent, upperBound = (int)(int)HDRenderQueue.Priority.OrderIndependentTransparent })));

                builder.SetRenderFunc(
                    (VBufferOITStoragePassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITListsCounts, passData.countBuffer);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITListsOffsets, passData.offsetBuffer);
                        context.cmd.SetRandomWriteTarget(1, passData.sublistCounterBuffer);
                        context.cmd.SetRandomWriteTarget(2, passData.outVisibilityBuffer);
                        data.BRGBindingData.globalGeometryPool.BindResourcesGlobal(context.cmd);
                        DrawTransparentRendererList(context, data.frameSettings, data.rendererList);
                    });
            }

            return outVisibilityBuffer;
        }

        class VBufferOITLightingOffscreen : ForwardOpaquePassData
        {
            public RenderBRGBindingData BRGBindingData;
            public ComputeBufferHandle oitVisibilityBuffer;
            public Vector4 packedArgs;
            public Vector2Int offscreenDimensions;
        }

        Vector2Int GetOITOffscreenLightingSize(HDCamera hdCamera, int maxSampleCounts)
        {
            int acceptedWidth = Math.Max(hdCamera.actualWidth, 1024);
            return new Vector2Int(acceptedWidth, (int)HDUtils.DivRoundUp(maxSampleCounts, acceptedWidth));
        }

        void RenderOITLighting(
            RenderGraph renderGraph,
            CullingResults cull,
            HDCamera hdCamera,
            ShadowResult shadowResult,
            in BuildGPULightListOutput lightLists,
            in PrepassOutput prepassData,
            in TextureHandle depthBuffer,
            ref TextureHandle colorBuffer)
        {
            if (!prepassData.vbufferOIT.valid)
                return;

            if (m_Asset.currentPlatformRenderPipelineSettings.orderIndependentTransparentSettings.oitLightingMode == OITLightingMode.ForwardFast)
            {
                TextureHandle offscreenLightingTexture = RenderOITVBufferLightingOffscreenForwardFast(
                    renderGraph, cull, hdCamera, shadowResult, prepassData.vbufferOIT, lightLists, prepassData, out var offscreenDimensions);

                OITResolveLightingForwardFast(renderGraph, hdCamera, prepassData.vbufferOIT, offscreenLightingTexture, offscreenDimensions, depthBuffer, ref colorBuffer);
            }
            else //if (m_Asset.currentPlatformRenderPipelineSettings.orderIndependentTransparentSettings.oitLightingMode == OITLightingMode.DeferredSSTracing)
            {
                //TextureHandle normalRoughnessTexture;
                //TextureHandle diffuseAlbedoTexture;
                TextureHandle normalRoughnessDiffuseAlbedoTexture;
                RenderOITVBufferLightingOffscreenDeferredSSTracing(
                    renderGraph, cull, hdCamera, shadowResult, prepassData.vbufferOIT, lightLists, prepassData, out normalRoughnessDiffuseAlbedoTexture, out var offscreenDimensions);

                OITResolveLightingDeferredSSTracing(renderGraph, hdCamera, prepassData.vbufferOIT, normalRoughnessDiffuseAlbedoTexture, offscreenDimensions, depthBuffer, ref colorBuffer);
            }
        }

        TextureHandle RenderOITVBufferLightingOffscreenForwardFast(
            RenderGraph renderGraph,
            CullingResults cull,
            HDCamera hdCamera,
            ShadowResult shadowResult,
            in VBufferOITOutput vbufferOIT,
            in BuildGPULightListOutput lightLists,
            in PrepassOutput prepassData,
            out Vector2Int offscreenDimensions)
        {
            var BRGBindingData = RenderBRG.GetRenderBRGMaterialBindingData();

            int maxSampleCounts = GetMaxMaterialOITSampleCount();
            offscreenDimensions = GetOITOffscreenLightingSize(hdCamera, maxSampleCounts);

            TextureHandle outputColor;
            using (var builder = renderGraph.AddRenderPass<VBufferOITLightingOffscreen>("VBufferOITLightingOffscreenForwardFast", out var passData, ProfilingSampler.Get(HDProfileId.VBufferOITLightingOffscreenForwardFast)))
            {
                var renderListDesc = CreateOpaqueRendererListDesc(
                                    cull,
                                    hdCamera.camera,
                                    HDShaderPassNames.s_VBufferLightingOffscreenForwardFastName, m_CurrentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_AllTransparentOIT);
                //TODO: hide this from the UI!!
                renderListDesc.renderingLayerMask = DeferredMaterialBRG.RenderLayerMask;
                PrepareCommonForwardPassData(renderGraph, builder, passData, true, hdCamera.frameSettings, renderListDesc, lightLists, shadowResult);

                outputColor = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(offscreenDimensions.x, offscreenDimensions.y, false, true)
                    {
                        colorFormat = GetColorBufferFormat(),
                        name = "OITOffscreenLightingForwardFast"
                    }), 0);

                passData.BRGBindingData = BRGBindingData;
                passData.oitVisibilityBuffer = builder.ReadComputeBuffer(vbufferOIT.oitVisibilityBuffer);

                float packedWidth; unsafe { int offscreenDimsWidth = offscreenDimensions.x; packedWidth = *((float*)&offscreenDimsWidth); };
                float packedMaxSamples; unsafe { packedMaxSamples = *((float*)&maxSampleCounts); };
                passData.packedArgs = new Vector4(packedWidth, packedMaxSamples, 0.0f, 0.0f);
                passData.offscreenDimensions = offscreenDimensions;

                builder.SetRenderFunc(
                    (VBufferOITLightingOffscreen data, RenderGraphContext context) =>
                    {
                        BindGlobalLightListBuffers(data, context);
                        BindDBufferGlobalData(data.dbuffer, context);

                        CoreUtils.SetKeyword(context.cmd, "USE_FPTL_LIGHTLIST", false);
                        CoreUtils.SetKeyword(context.cmd, "USE_CLUSTERED_LIGHTLIST", true);

                        data.BRGBindingData.globalGeometryPool.BindResourcesGlobal(context.cmd);
                        Rect targetViewport = new Rect(0.0f, 0.0f, (float)data.offscreenDimensions.x, (float)data.offscreenDimensions.y);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITBuffer, data.oitVisibilityBuffer);
                        context.cmd.SetGlobalVector(HDShaderIDs._VBufferLightingOffscreenParams, data.packedArgs);
                        context.cmd.SetViewport(targetViewport);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }

            return outputColor;
        }

        void RenderOITVBufferLightingOffscreenDeferredSSTracing(
            RenderGraph renderGraph,
            CullingResults cull,
            HDCamera hdCamera,
            ShadowResult shadowResult,
            in VBufferOITOutput vbufferOIT,
            in BuildGPULightListOutput lightLists,
            in PrepassOutput prepassData,
            out TextureHandle normalRoughnessDiffuseAlbedoTexture,
            out Vector2Int offscreenDimensions)
        {
            var BRGBindingData = RenderBRG.GetRenderBRGMaterialBindingData();

            int maxSampleCounts = GetMaxMaterialOITSampleCount();
            offscreenDimensions = GetOITOffscreenLightingSize(hdCamera, maxSampleCounts);

            using (var builder = renderGraph.AddRenderPass<VBufferOITLightingOffscreen>("VBufferOITLightingOffscreenDeferredSSTracing", out var passData, ProfilingSampler.Get(HDProfileId.VBufferOITLightingOffscreenDeferredSSTracing)))
            {
                var renderListDesc = CreateOpaqueRendererListDesc(
                                    cull,
                                    hdCamera.camera,
                                    HDShaderPassNames.s_VBufferLightingOffscreenDeferredSSTracingName, m_CurrentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_AllTransparentOIT);
                //TODO: hide this from the UI!!
                renderListDesc.renderingLayerMask = DeferredMaterialBRG.RenderLayerMask;
                PrepareCommonForwardPassData(renderGraph, builder, passData, true, hdCamera.frameSettings, renderListDesc, lightLists, shadowResult);

                normalRoughnessDiffuseAlbedoTexture = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(offscreenDimensions.x, offscreenDimensions.y, false, true)
                    {
                        colorFormat = GraphicsFormat.R16G16B16A16_UInt, // TODO: test with GraphicsFormat.A2R10G10B10_UNormPack32
                        name = "OITOffscreenLightingDeferredSSTracing_Normal_Roughness_DiffuseAlbedo"
                    }), 0);

                passData.BRGBindingData = BRGBindingData;
                passData.oitVisibilityBuffer = builder.ReadComputeBuffer(vbufferOIT.oitVisibilityBuffer);

                float packedWidth; unsafe { int offscreenDimsWidth = offscreenDimensions.x; packedWidth = *((float*)&offscreenDimsWidth); };
                float packedMaxSamples; unsafe { packedMaxSamples = *((float*)&maxSampleCounts); };
                passData.packedArgs = new Vector4(packedWidth, packedMaxSamples, 0.0f, 0.0f);
                passData.offscreenDimensions = offscreenDimensions;

                builder.SetRenderFunc(
                    (VBufferOITLightingOffscreen data, RenderGraphContext context) =>
                    {
                        BindGlobalLightListBuffers(data, context);
                        BindDBufferGlobalData(data.dbuffer, context);

                        //CoreUtils.SetKeyword(context.cmd, "USE_FPTL_LIGHTLIST", false);
                        //CoreUtils.SetKeyword(context.cmd, "USE_CLUSTERED_LIGHTLIST", true);

                        data.BRGBindingData.globalGeometryPool.BindResourcesGlobal(context.cmd);
                        Rect targetViewport = new Rect(0.0f, 0.0f, (float)data.offscreenDimensions.x, (float)data.offscreenDimensions.y);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITBuffer, data.oitVisibilityBuffer);
                        context.cmd.SetGlobalVector(HDShaderIDs._VBufferLightingOffscreenParams, data.packedArgs);
                        context.cmd.SetViewport(targetViewport);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }
        }

        class OITResolveForwardFastRenderPass
        {
            public ComputeShader cs;
            public Vector2Int screenSize;
            public TextureHandle offscreenLighting;
            public TextureHandle depthBuffer;
            public ComputeBufferHandle oitVisibilityBuffer;
            public ComputeBufferHandle offsetListBuffer;
            public ComputeBufferHandle sublistCounterBuffer;
            public TextureHandle outputColor;
            public Vector4 packedArgs;
        }

        void OITResolveLightingForwardFast(RenderGraph renderGraph, HDCamera hdCamera,
            in VBufferOITOutput vbufferOIT,
            TextureHandle offscreenLighting,
            Vector2Int offscreenLightingSize,
            TextureHandle depthBuffer, ref TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<OITResolveForwardFastRenderPass>("OITResolveForwardFastRenderPass", out var passData, ProfilingSampler.Get(HDProfileId.OITResolveLightingForwardFast)))
            {
                passData.cs = defaultResources.shaders.oitResolveForwardFastCS;
                passData.screenSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);
                passData.oitVisibilityBuffer = builder.ReadComputeBuffer(vbufferOIT.oitVisibilityBuffer);
                passData.sublistCounterBuffer = builder.ReadComputeBuffer(vbufferOIT.sublistCounterBuffer);
                passData.offsetListBuffer = builder.ReadComputeBuffer(vbufferOIT.sampleListOffsetBuffer);
                passData.offscreenLighting = builder.ReadTexture(offscreenLighting);
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.outputColor = builder.WriteTexture(colorBuffer);

                float offscreenWidthAsFloat; unsafe { int offscreenWidthInt = offscreenLightingSize.x; offscreenWidthAsFloat = *((float*)&offscreenWidthInt); }
                passData.packedArgs = new Vector4(offscreenWidthAsFloat, 0.0f, 0.0f, 0.0f);

                colorBuffer = passData.outputColor;

                builder.SetRenderFunc(
                    (OITResolveForwardFastRenderPass data, RenderGraphContext context) =>
                    {
                        int kernel = data.cs.FindKernel("MainResolveOffscreenLighting");
                        context.cmd.SetComputeVectorParam(data.cs, HDShaderIDs._VBufferLightingOffscreenParams, data.packedArgs);
                        context.cmd.SetComputeBufferParam(data.cs, kernel, HDShaderIDs._VisOITBuffer, data.oitVisibilityBuffer);
                        context.cmd.SetComputeBufferParam(data.cs, kernel, HDShaderIDs._VisOITSubListsCounts, data.sublistCounterBuffer);
                        context.cmd.SetComputeBufferParam(data.cs, kernel, HDShaderIDs._VisOITListsOffsets, data.offsetListBuffer);
                        context.cmd.SetComputeTextureParam(data.cs, kernel, HDShaderIDs._VisOITOffscreenLighting, data.offscreenLighting);
                        context.cmd.SetComputeTextureParam(data.cs, kernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        context.cmd.SetComputeTextureParam(data.cs, kernel, HDShaderIDs._OutputTexture, data.outputColor);
                        context.cmd.DispatchCompute(data.cs, kernel, HDUtils.DivRoundUp(data.screenSize.x, 8), HDUtils.DivRoundUp(data.screenSize.y, 8), 1);
                    });
            }
        }

        class OITResolveDeferredSSTracingRenderPass
        {
            public ComputeShader cs;
            public Vector2Int screenSize;
            public TextureHandle normalRoughnessDiffuseAlbedo;
            public TextureHandle depthBuffer;
            public ComputeBufferHandle oitVisibilityBuffer;
            public ComputeBufferHandle offsetListBuffer;
            public ComputeBufferHandle sublistCounterBuffer;
            public TextureHandle outputColor;
            public Vector4 packedArgs;
        }

        void OITResolveLightingDeferredSSTracing(RenderGraph renderGraph, HDCamera hdCamera,
            in VBufferOITOutput vbufferOIT,
            TextureHandle normalRoughnessDiffuseAlbedoTexture,
            Vector2Int offscreenLightingSize,
            TextureHandle depthBuffer, ref TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<OITResolveDeferredSSTracingRenderPass>("OITResolveDeferredSSTracingRenderPass", out var passData, ProfilingSampler.Get(HDProfileId.OITResolveLightingDeferredSSTracing)))
            {
                passData.cs = defaultResources.shaders.oitResolveForwardFastCS;
                passData.screenSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);
                passData.oitVisibilityBuffer = builder.ReadComputeBuffer(vbufferOIT.oitVisibilityBuffer);
                passData.sublistCounterBuffer = builder.ReadComputeBuffer(vbufferOIT.sublistCounterBuffer);
                passData.offsetListBuffer = builder.ReadComputeBuffer(vbufferOIT.sampleListOffsetBuffer);
                passData.normalRoughnessDiffuseAlbedo = builder.ReadTexture(normalRoughnessDiffuseAlbedoTexture);
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.outputColor = builder.WriteTexture(colorBuffer);

                float offscreenWidthAsFloat; unsafe { int offscreenWidthInt = offscreenLightingSize.x; offscreenWidthAsFloat = *((float*)&offscreenWidthInt); }
                passData.packedArgs = new Vector4(offscreenWidthAsFloat, 0.0f, 0.0f, 0.0f);

                colorBuffer = passData.outputColor;

                builder.SetRenderFunc(
                    (OITResolveDeferredSSTracingRenderPass data, RenderGraphContext context) =>
                    {
                        //CoreUtils.SetKeyword(context.cmd, "USE_FPTL_LIGHTLIST", false);
                        //CoreUtils.SetKeyword(context.cmd, "USE_CLUSTERED_LIGHTLIST", true);

                        int kernel = data.cs.FindKernel("MainResolveOffscreenLighting");
                        context.cmd.SetComputeVectorParam(data.cs, HDShaderIDs._VBufferLightingOffscreenParams, data.packedArgs);
                        context.cmd.SetComputeBufferParam(data.cs, kernel, HDShaderIDs._VisOITBuffer, data.oitVisibilityBuffer);
                        context.cmd.SetComputeBufferParam(data.cs, kernel, HDShaderIDs._VisOITSubListsCounts, data.sublistCounterBuffer);
                        context.cmd.SetComputeBufferParam(data.cs, kernel, HDShaderIDs._VisOITListsOffsets, data.offsetListBuffer);
                        //context.cmd.SetComputeTextureParam(data.cs, kernel, HDShaderIDs._VisOITOffscreenLighting, data.offscreenLighting);
                        context.cmd.SetComputeTextureParam(data.cs, kernel, HDShaderIDs._VisOITOffscreenLighting, data.normalRoughnessDiffuseAlbedo);
                        context.cmd.SetComputeTextureParam(data.cs, kernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        context.cmd.SetComputeTextureParam(data.cs, kernel, HDShaderIDs._OutputTexture, data.outputColor);
                        context.cmd.DispatchCompute(data.cs, kernel, HDUtils.DivRoundUp(data.screenSize.x, 8), HDUtils.DivRoundUp(data.screenSize.y, 8), 1);
                    });
            }
        }

        class VBufferOITTestLighting
        {
            public RenderBRGBindingData BRGBindingData;
            public Material testLightingMaterial;
            public ComputeBufferHandle oitVisibilityBuffer;
            public ComputeBufferHandle sampleListCountBuffer;
            public ComputeBufferHandle sampleListOffsetBuffer;
            public ComputeBufferHandle sublistCounterBuffer;
            public int testLightingPass;
            public int width;
            public int height;
        }

        TextureHandle TestOITLighting(RenderGraph renderGraph, ref PrepassOutput prepassData, TextureHandle colorBuffer, HDCamera hdCamera)
        {
            var vbufferOIT = prepassData.vbufferOIT;
            if (!vbufferOIT.valid)
                return colorBuffer;

            Material testLightingMaterial = currentAsset.VisibilityOITMaterial;
            int testLightingPass = -1;
            for (int i = 0; i < testLightingMaterial.passCount; ++i)
            {
                if (testLightingMaterial.GetPassName(i).IndexOf("VBufferTestLighting") < 0)
                    continue;

                testLightingPass = i;
                break;
            }

            if (testLightingPass < 0)
                return colorBuffer;

            var BRGBindingData = RenderBRG.GetRenderBRGMaterialBindingData();

            TextureHandle outputColor;
            using (var builder = renderGraph.AddRenderPass<VBufferOITTestLighting>("VBufferTestLighting", out var passData, ProfilingSampler.Get(HDProfileId.VBufferOITTestLighting)))
            {
                outputColor = builder.UseColorBuffer(colorBuffer, 0);
                passData.BRGBindingData = BRGBindingData;
                passData.testLightingMaterial = testLightingMaterial;
                passData.testLightingPass = testLightingPass;
                passData.oitVisibilityBuffer = builder.ReadComputeBuffer(vbufferOIT.oitVisibilityBuffer);
                passData.sampleListCountBuffer = builder.ReadComputeBuffer(vbufferOIT.sampleListCountBuffer);
                passData.sampleListOffsetBuffer = builder.ReadComputeBuffer(vbufferOIT.sampleListOffsetBuffer);
                passData.sublistCounterBuffer = builder.ReadComputeBuffer(vbufferOIT.sublistCounterBuffer);
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;

                builder.SetRenderFunc(
                    (VBufferOITTestLighting data, RenderGraphContext context) =>
                    {
                        data.BRGBindingData.globalGeometryPool.BindResourcesGlobal(context.cmd);
                        Rect targetViewport = new Rect(0.0f, 0.0f, data.width, data.height);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITBuffer, data.oitVisibilityBuffer);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITListsCounts, data.sampleListCountBuffer);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITListsOffsets, data.sampleListOffsetBuffer);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._VisOITSubListsCounts, data.sublistCounterBuffer);
                        context.cmd.SetViewport(targetViewport);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.testLightingMaterial, data.testLightingPass, MeshTopology.Triangles, 3, 1, null);
                    });
            }

            return outputColor;
        }
    }
}
