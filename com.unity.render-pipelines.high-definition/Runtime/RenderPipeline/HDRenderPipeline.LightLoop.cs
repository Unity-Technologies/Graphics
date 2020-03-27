//using System;
//using UnityEngine.Rendering;
//using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct LightingBuffers
        {
            public RenderGraphMutableResource   sssBuffer;
            public RenderGraphMutableResource   diffuseLightingBuffer;
            public RenderGraphResource          ambientOcclusionBuffer;
            public RenderGraphResource          ssrLightingBuffer;
            public RenderGraphResource          contactShadowsBuffer;
        }

        static void ReadLightingBuffers(LightingBuffers buffers, RenderGraphBuilder builder)
        {
            // We only read those buffers because sssBuffer and diffuseLightingBuffer our just output of the lighting process, not inputs.
            builder.ReadTexture(buffers.ambientOcclusionBuffer);
            builder.ReadTexture(buffers.ssrLightingBuffer);
            builder.ReadTexture(buffers.contactShadowsBuffer);
        }

        class BuildGPULightListPassData
        {
            public LightDataGlobalParameters lightDataGlobalParameters;
            public ShadowGlobalParameters shadowGlobalParameters;
            public LightLoopGlobalParameters lightLoopGlobalParameters;

            public BuildGPULightListParameters buildGPULightListParameters;
            public BuildGPULightListResources buildGPULightListResources;
            public RenderGraphResource depthBuffer;
            public RenderGraphResource stencilTexture;
            public RenderGraphResource[] gBuffer = new RenderGraphResource[RenderGraph.kMaxMRTCount];
            public int gBufferCount;
        }

        void BuildGPULightList(RenderGraph renderGraph, HDCamera hdCamera, RenderGraphResource depthStencilBuffer, RenderGraphResource stencilBufferCopy, GBufferOutput gBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<BuildGPULightListPassData>("Build Light List", out var passData, ProfilingSampler.Get(HDProfileId.BuildLightList)))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.BuildLightListRunsAsync());

                passData.lightDataGlobalParameters = PrepareLightDataGlobalParameters(hdCamera);
                passData.shadowGlobalParameters = PrepareShadowGlobalParameters(hdCamera);
                passData.lightLoopGlobalParameters = PrepareLightLoopGlobalParameters(hdCamera);
                passData.buildGPULightListParameters = PrepareBuildGPULightListParameters(hdCamera);
                // TODO: Move this inside the render function onces compute buffers are RenderGraph ready
                passData.buildGPULightListResources = PrepareBuildGPULightListResources(m_TileAndClusterData, null, null);
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.stencilTexture = builder.ReadTexture(stencilBufferCopy);
                if (passData.buildGPULightListParameters.computeMaterialVariants && passData.buildGPULightListParameters.enableFeatureVariants)
                {
                    for (int i = 0; i < gBuffer.gBufferCount; ++i)
                        passData.gBuffer[i] = builder.ReadTexture(gBuffer.mrt[i]);
                    passData.gBufferCount = gBuffer.gBufferCount;
                }

                builder.SetRenderFunc(
                (BuildGPULightListPassData data, RenderGraphContext context) =>
                {
                    bool tileFlagsWritten = false;

                    data.buildGPULightListResources.depthBuffer = context.resources.GetTexture(data.depthBuffer);
                    data.buildGPULightListResources.stencilTexture = context.resources.GetTexture(data.stencilTexture);
                    if (passData.buildGPULightListParameters.computeMaterialVariants && passData.buildGPULightListParameters.enableFeatureVariants)
                    {
                        data.buildGPULightListResources.gBuffer = context.renderGraphPool.GetTempArray<RTHandle>(data.gBufferCount);
                        for (int i = 0; i < data.gBufferCount; ++i)
                            data.buildGPULightListResources.gBuffer[i] = context.resources.GetTexture(data.gBuffer[i]);
                    }

                    GenerateLightsScreenSpaceAABBs(data.buildGPULightListParameters, data.buildGPULightListResources, context.cmd);
                    BigTilePrepass(data.buildGPULightListParameters, data.buildGPULightListResources, context.cmd);
                    BuildPerTileLightList(data.buildGPULightListParameters, data.buildGPULightListResources, ref tileFlagsWritten, context.cmd);
                    VoxelLightListGeneration(data.buildGPULightListParameters, data.buildGPULightListResources, context.cmd);

                    BuildDispatchIndirectArguments(data.buildGPULightListParameters, data.buildGPULightListResources, tileFlagsWritten, context.cmd);

                    // WARNING: Note that the three set of variables are bound here, but it should be handled differently.
                    PushLightDataGlobalParams(data.lightDataGlobalParameters, context.cmd);
                    PushShadowGlobalParams(data.shadowGlobalParameters, context.cmd);
                    PushLightLoopGlobalParams(data.lightLoopGlobalParameters, context.cmd);
                });

            }
        }

        class PushGlobalCameraParamPassData
        {
            public HDCamera    hdCamera;
            public int         frameCount;

        }

        void PushGlobalCameraParams(RenderGraph renderGraph, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<PushGlobalCameraParamPassData>("Push Global Camera Parameters", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.frameCount = m_FrameCount;

                builder.SetRenderFunc(
                (PushGlobalCameraParamPassData data, RenderGraphContext context) =>
                {
                    data.hdCamera.SetupGlobalParams(context.cmd, data.frameCount);
                });
            }
        }

        internal ShadowResult RenderShadows(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullResults)
        {
            var result = m_ShadowManager.RenderShadows(m_RenderGraph, hdCamera, cullResults);

            // TODO: Remove this once shadows don't pollute global parameters anymore.
            PushGlobalCameraParams(renderGraph, hdCamera);
            return result;
        }

        RenderGraphMutableResource CreateDiffuseLightingBuffer(RenderGraph renderGraph, bool msaa)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = !msaa,
                    bindTextureMS = msaa, enableMSAA = msaa, clearBuffer = true, clearColor = Color.clear, name = string.Format("CameraSSSDiffuseLighting{0}", msaa ? "MSAA" : "") });
        }

        class DeferredLightingPassData
        {
            public DeferredLightingParameters   parameters;
            public DeferredLightingResources    resources;

            public RenderGraphMutableResource   colorBuffer;
            public RenderGraphMutableResource   sssDiffuseLightingBuffer;
            public RenderGraphResource          depthBuffer;
            public RenderGraphResource          depthTexture;

            public int                          gbufferCount;
            public RenderGraphResource[]        gbuffer = new RenderGraphResource[8];
        }

        struct LightingOutput
        {
            public RenderGraphMutableResource colorBuffer;
        }

        LightingOutput RenderDeferredLighting(  RenderGraph                 renderGraph,
                                                HDCamera                    hdCamera,
                                                RenderGraphMutableResource  colorBuffer,
                                                RenderGraphResource         depthStencilBuffer,
                                                RenderGraphResource         depthPyramidTexture,
                                                in LightingBuffers          lightingBuffers,
                                                in GBufferOutput            gbuffer,
                                                in ShadowResult             shadowResult)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred)
                return new LightingOutput();

            using (var builder = renderGraph.AddRenderPass<DeferredLightingPassData>("Deferred Lighting", out var passData))
            {
                passData.parameters = PrepareDeferredLightingParameters(hdCamera, m_CurrentDebugDisplaySettings);

                // TODO: Move this inside the render function onces compute buffers are RenderGraph ready
                passData.resources = new  DeferredLightingResources();
                passData.resources.lightListBuffer = m_TileAndClusterData.lightList;
                passData.resources.tileFeatureFlagsBuffer = m_TileAndClusterData.tileFeatureFlags;
                passData.resources.tileListBuffer = m_TileAndClusterData.tileList;
                passData.resources.dispatchIndirectBuffer = m_TileAndClusterData.dispatchIndirectBuffer;

                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                if (passData.parameters.outputSplitLighting)
                {
                    passData.sssDiffuseLightingBuffer = builder.WriteTexture(lightingBuffers.diffuseLightingBuffer);
                }
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.depthTexture = builder.ReadTexture(depthPyramidTexture);

                ReadLightingBuffers(lightingBuffers, builder);

                passData.gbufferCount = gbuffer.gBufferCount;
                for (int i = 0; i < gbuffer.gBufferCount; ++i)
                    passData.gbuffer[i] = builder.ReadTexture(gbuffer.mrt[i]);

                HDShadowManager.ReadShadowResult(shadowResult, builder);

                var output = new LightingOutput();
                output.colorBuffer = passData.colorBuffer;

                builder.SetRenderFunc(
                (DeferredLightingPassData data, RenderGraphContext context) =>
                {
                    data.resources.colorBuffers = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                    data.resources.colorBuffers[0] = context.resources.GetTexture(data.colorBuffer);
                    if (data.parameters.outputSplitLighting)
                        data.resources.colorBuffers[1] = context.resources.GetTexture(data.sssDiffuseLightingBuffer);
                    data.resources.depthStencilBuffer = context.resources.GetTexture(data.depthBuffer);
                    data.resources.depthTexture = context.resources.GetTexture(data.depthTexture);

                    // TODO: try to find a better way to bind this.
                    // Issue is that some GBuffers have several names (for example normal buffer is both NormalBuffer and GBuffer1)
                    // So it's not possible to use auto binding via dependency to shaderTagID
                    for (int i = 0; i < data.gbufferCount; ++i)
                        context.cmd.SetGlobalTexture(HDShaderIDs._GBufferTexture[i], context.resources.GetTexture(data.gbuffer[i]));

                    if (data.parameters.enableTile)
                    {
                        bool useCompute = data.parameters.useComputeLightingEvaluation && !k_PreferFragment;
                        if (useCompute)
                            RenderComputeDeferredLighting(data.parameters, data.resources, context.cmd);
                        else
                            RenderComputeAsPixelDeferredLighting(data.parameters, data.resources, context.cmd);
                    }
                    else
                    {
                        RenderPixelDeferredLighting(data.parameters, data.resources, context.cmd);
                    }
                });

                return output;
            }
        }

        class RenderSSRPassData
        {
            public RenderSSRParameters parameters;
            public RenderGraphResource depthPyramid;
            public RenderGraphResource colorPyramid;
            public RenderGraphResource stencilBuffer;
            public RenderGraphMutableResource hitPointsTexture;
            public RenderGraphMutableResource lightingTexture;
            public RenderGraphResource clearCoatMask;
            //public RenderGraphMutableResource debugTexture;
        }

        RenderGraphResource RenderSSR(  RenderGraph         renderGraph,
                                        HDCamera            hdCamera,
                                        RenderGraphResource normalBuffer,
                                        RenderGraphResource motionVectorsBuffer,
                                        RenderGraphResource depthPyramid,
                                        RenderGraphResource stencilBuffer,
                                        RenderGraphResource clearCoatMask)
        {
            var ssrBlackTexture = renderGraph.ImportTexture(TextureXR.GetBlackTexture(), HDShaderIDs._SsrLightingTexture);

            if (!hdCamera.IsSSREnabled())
                return ssrBlackTexture;

            RenderGraphResource result;

            // TODO RENDERGRAPH
            //var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            //if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value)
            //{
            //    hdCamera.xr.StartSinglePass(cmd);
            //    RenderRayTracedReflections(hdCamera, cmd, m_SsrLightingTexture, renderContext, m_FrameCount);
            //    hdCamera.xr.StopSinglePass(cmd);
            //}
            //else
            {
                using (var builder = renderGraph.AddRenderPass<RenderSSRPassData>("Render SSR", out var passData))
                {
                    builder.EnableAsyncCompute(hdCamera.frameSettings.SSRRunsAsync());

                    var colorPyramid = renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain));

                    passData.parameters = PrepareSSRParameters(hdCamera);
                    passData.depthPyramid = builder.ReadTexture(depthPyramid);
                    passData.colorPyramid = builder.ReadTexture(colorPyramid);
                    passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                    passData.clearCoatMask = builder.ReadTexture(clearCoatMask);

                    builder.ReadTexture(normalBuffer);
                    builder.ReadTexture(motionVectorsBuffer);

                    // In practice, these textures are sparse (mostly black). Therefore, clearing them is fast (due to CMASK),
                    // and much faster than fully overwriting them from within SSR shaders.
                    passData.hitPointsTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16_UNorm, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Hit_Point_Texture" }));
                    passData.lightingTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Lighting_Texture" }, HDShaderIDs._SsrLightingTexture));
                    //passData.hitPointsTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    //    { colorFormat = GraphicsFormat.ARGBFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Debug_Texture" }));

                    builder.SetRenderFunc(
                    (RenderSSRPassData data, RenderGraphContext context) =>
                    {
                        var res = context.resources;
                        RenderSSR(data.parameters,
                                    res.GetTexture(data.depthPyramid),
                                    res.GetTexture(data.hitPointsTexture),
                                    res.GetTexture(data.stencilBuffer),
                                    res.GetTexture(data.clearCoatMask),
                                    res.GetTexture(data.colorPyramid),
                                    res.GetTexture(data.lightingTexture),
                                    context.cmd, context.renderContext);
                    });

                    result = passData.lightingTexture;
                }

                if (!hdCamera.colorPyramidHistoryIsValid)
                {
                    hdCamera.colorPyramidHistoryIsValid = true; // For the next frame...
                    result = ssrBlackTexture;
                }
            }

            PushFullScreenDebugTexture(renderGraph, result, FullScreenDebugMode.ScreenSpaceReflections);
            return result;
        }

        class RenderContactShadowPassData
        {
            public ContactShadowsParameters     parameters;
            public LightLoopLightData           lightLoopLightData;
            public TileAndClusterData           tileAndClusterData;
            public RenderGraphResource          depthTexture;
            public RenderGraphMutableResource   contactShadowsTexture;
            public HDShadowManager              shadowManager;
        }

        RenderGraphResource RenderContactShadows(RenderGraph renderGraph, HDCamera hdCamera, RenderGraphResource depthTexture, int firstMipOffsetY)
        {
            if (!WillRenderContactShadow())
                return renderGraph.ImportTexture(TextureXR.GetClearTexture(), HDShaderIDs._ContactShadowTexture);

            RenderGraphResource result;
            using (var builder = renderGraph.AddRenderPass<RenderContactShadowPassData>("Contact Shadows", out var passData))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.ContactShadowsRunAsync());

                // Avoid garbage when visualizing contact shadows.
                bool clearBuffer = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ContactShadows;

                passData.parameters = PrepareContactShadowsParameters(hdCamera, firstMipOffsetY);
                passData.lightLoopLightData = m_LightLoopLightData;
                passData.tileAndClusterData = m_TileAndClusterData;
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.shadowManager = m_ShadowManager;
                passData.contactShadowsTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, clearBuffer = clearBuffer, clearColor = Color.clear, name = "ContactShadowsBuffer" }, HDShaderIDs._ContactShadowTexture));

                result = passData.contactShadowsTexture;

                builder.SetRenderFunc(
                (RenderContactShadowPassData data, RenderGraphContext context) =>
                {
                    var res = context.resources;
                    data.shadowManager.PushGlobalParameters(context.cmd);

                    RenderContactShadows(data.parameters, res.GetTexture(data.contactShadowsTexture), res.GetTexture(data.depthTexture), data.lightLoopLightData, data.tileAndClusterData, context.cmd);
                });
            }

            PushFullScreenDebugTexture(renderGraph, result, FullScreenDebugMode.ContactShadows);
            return result;
        }

        class VolumeVoxelizationPassData
        {
            public VolumeVoxelizationParameters parameters;
            public RenderGraphMutableResource   densityBuffer;
            public ComputeBuffer                visibleVolumeBoundsBuffer;
            public ComputeBuffer                visibleVolumeDataBuffer;
            public ComputeBuffer                bigTileLightListBuffer;
        }

        RenderGraphResource VolumeVoxelizationPass( RenderGraph         renderGraph,
                                                    HDCamera            hdCamera,
                                                    ComputeBuffer       visibleVolumeBoundsBuffer,
                                                    ComputeBuffer       visibleVolumeDataBuffer,
                                                    ComputeBuffer       bigTileLightListBuffer)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                using (var builder = renderGraph.AddRenderPass<VolumeVoxelizationPassData>("Volume Voxelization", out var passData))
                {
                    builder.EnableAsyncCompute(hdCamera.frameSettings.VolumeVoxelizationRunsAsync());

                    passData.parameters = PrepareVolumeVoxelizationParameters(hdCamera);
                    passData.visibleVolumeBoundsBuffer = visibleVolumeBoundsBuffer;
                    passData.visibleVolumeDataBuffer = visibleVolumeDataBuffer;
                    passData.bigTileLightListBuffer = bigTileLightListBuffer;
                    passData.densityBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(ComputeVBufferResolutionXY, false, false)
                    {
                        dimension = TextureDimension.Tex3D,
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                        enableRandomWrite = true,
                        slices = ComputeVBufferSliceCount(volumetricLightingPreset),
                        /* useDynamicScale: true, // <- TODO ,*/
                        name = "VBufferDensity"
                    }));

                    builder.SetRenderFunc(
                    (VolumeVoxelizationPassData data, RenderGraphContext ctx) =>
                    {
                        VolumeVoxelizationPass(data.parameters,
                                                ctx.resources.GetTexture(data.densityBuffer),
                                                data.visibleVolumeBoundsBuffer,
                                                data.visibleVolumeDataBuffer,
                                                data.bigTileLightListBuffer,
                                                ctx.cmd);
                    });

                    return passData.densityBuffer;
                }
            }
            return new RenderGraphResource();
        }

        class VolumetricLightingPassData
        {
            public VolumetricLightingParameters parameters;
            public RenderGraphResource          densityBuffer;
            public RenderGraphMutableResource   lightingBuffer;
            public RenderGraphResource          historyBuffer;
            public RenderGraphMutableResource   feedbackBuffer;
            public ComputeBuffer                bigTileLightListBuffer;
        }

        RenderGraphResource VolumetricLightingPass(RenderGraph renderGraph, HDCamera hdCamera, RenderGraphResource densityBuffer, ComputeBuffer bigTileLightListBuffer, ShadowResult shadowResult, int frameIndex)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                var parameters = PrepareVolumetricLightingParameters(hdCamera, frameIndex);

                using (var builder = renderGraph.AddRenderPass<VolumetricLightingPassData>("Volumetric Lighting", out var passData))
                {
                    //builder.EnableAsyncCompute(hdCamera.frameSettings.VolumetricLightingRunsAsync());

                    passData.parameters = parameters;
                    passData.bigTileLightListBuffer = bigTileLightListBuffer;
                    passData.densityBuffer = builder.ReadTexture(densityBuffer);
                    passData.lightingBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(ComputeVBufferResolutionXY, false, false)
                    {
                        dimension = TextureDimension.Tex3D,
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                        enableRandomWrite = true,
                        slices = ComputeVBufferSliceCount(volumetricLightingPreset),
                        /* useDynamicScale: true, // <- TODO ,*/
                        name = "VBufferIntegral"
                    }, HDShaderIDs._VBufferLighting));
                    if (passData.parameters.enableReprojection)
                    {
                        passData.historyBuffer = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting)));
                        passData.feedbackBuffer = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting)));
                    }

                    HDShadowManager.ReadShadowResult(shadowResult, builder);

                    builder.SetRenderFunc(
                    (VolumetricLightingPassData data, RenderGraphContext ctx) =>
                    {
                        RTHandle densityBufferRT = ctx.resources.GetTexture(data.densityBuffer);
                        RTHandle lightinBufferRT = ctx.resources.GetTexture(data.lightingBuffer);
                        VolumetricLightingPass( data.parameters,
                                                densityBufferRT,
                                                lightinBufferRT,
                                                data.parameters.enableReprojection ? ctx.resources.GetTexture(data.historyBuffer) : null,
                                                data.parameters.enableReprojection ? ctx.resources.GetTexture(data.feedbackBuffer) : null,
                                                data.bigTileLightListBuffer,
                                                ctx.cmd);

                        if (data.parameters.filterVolume)
                            FilterVolumetricLighting(data.parameters, densityBufferRT, lightinBufferRT, ctx.cmd);
                    });

                    if (parameters.enableReprojection)
                        hdCamera.volumetricHistoryIsValid = true; // For the next frame..

                    return passData.lightingBuffer;
                }
            }

            return renderGraph.ImportTexture(HDUtils.clearTexture3DRTH);
        }
    }
}
