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
            public TextureHandle    sssBuffer;
            public TextureHandle    diffuseLightingBuffer;
            public TextureHandle    ambientOcclusionBuffer;
            public TextureHandle    ssrLightingBuffer;
            public TextureHandle    contactShadowsBuffer;
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
            public LightLoopGlobalParameters    lightLoopGlobalParameters;

            public BuildGPULightListParameters  buildGPULightListParameters;
            public TextureHandle                depthBuffer;
            public TextureHandle                stencilTexture;
            public TextureHandle[]              gBuffer = new TextureHandle[RenderGraph.kMaxMRTCount];
            public int                          gBufferCount;

            // These buffers are not used outside of BuildGPULight list so they don't need to be known by the render graph.
            public ComputeBuffer                lightVolumeDataBuffer;
            public ComputeBuffer                convexBoundsBuffer;
            public ComputeBuffer                AABBBoundsBuffer;
            public ComputeBuffer                globalLightListAtomic;

            public BuildGPULightListOutput      output = new BuildGPULightListOutput();
        }

        struct BuildGPULightListOutput
        {
            public ComputeBufferHandle tileFeatureFlags;
            public ComputeBufferHandle dispatchIndirectBuffer;
            public ComputeBufferHandle perVoxelOffset;
            public ComputeBufferHandle perTileLogBaseTweak;
            public ComputeBufferHandle tileList;
            public ComputeBufferHandle bigTileLightList;
            public ComputeBufferHandle perVoxelLightLists;
            public ComputeBufferHandle lightList;
        }

        static BuildGPULightListResources PrepareBuildGPULightListResources(RenderGraphContext context, BuildGPULightListPassData data)
        {
            var buildLightListResources = new BuildGPULightListResources();

            buildLightListResources.depthBuffer = context.resources.GetTexture(data.depthBuffer);
            buildLightListResources.stencilTexture = context.resources.GetTexture(data.stencilTexture);
            if (data.buildGPULightListParameters.computeMaterialVariants && data.buildGPULightListParameters.enableFeatureVariants)
            {
                buildLightListResources.gBuffer = context.renderGraphPool.GetTempArray<RTHandle>(data.gBufferCount);
                for (int i = 0; i < data.gBufferCount; ++i)
                    buildLightListResources.gBuffer[i] = context.resources.GetTexture(data.gBuffer[i]);
            }

            buildLightListResources.lightVolumeDataBuffer = data.lightVolumeDataBuffer;
            buildLightListResources.convexBoundsBuffer = data.convexBoundsBuffer;
            buildLightListResources.AABBBoundsBuffer = data.AABBBoundsBuffer;
            buildLightListResources.globalLightListAtomic = data.globalLightListAtomic;

            buildLightListResources.tileFeatureFlags = context.resources.GetComputeBuffer(data.output.tileFeatureFlags);
            buildLightListResources.dispatchIndirectBuffer = context.resources.GetComputeBuffer(data.output.dispatchIndirectBuffer);
            buildLightListResources.perVoxelOffset = context.resources.GetComputeBuffer(data.output.perVoxelOffset);
            buildLightListResources.perTileLogBaseTweak = context.resources.GetComputeBuffer(data.output.perTileLogBaseTweak);
            buildLightListResources.tileList = context.resources.GetComputeBuffer(data.output.tileList);
            buildLightListResources.bigTileLightList = context.resources.GetComputeBuffer(data.output.bigTileLightList);
            buildLightListResources.perVoxelLightLists = context.resources.GetComputeBuffer(data.output.perVoxelLightLists);
            buildLightListResources.lightList = context.resources.GetComputeBuffer(data.output.lightList);

            return buildLightListResources;
        }

        BuildGPULightListOutput BuildGPULightList(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle stencilBufferCopy, GBufferOutput gBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<BuildGPULightListPassData>("Build Light List", out var passData, ProfilingSampler.Get(HDProfileId.BuildLightList)))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.BuildLightListRunsAsync());

                passData.lightLoopGlobalParameters = PrepareLightLoopGlobalParameters(hdCamera, m_TileAndClusterData);
                passData.buildGPULightListParameters = PrepareBuildGPULightListParameters(hdCamera, m_TileAndClusterData, ref m_ShaderVariablesLightListCB, m_TotalLightCount);
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.stencilTexture = builder.ReadTexture(stencilBufferCopy);
                if (passData.buildGPULightListParameters.computeMaterialVariants && passData.buildGPULightListParameters.enableFeatureVariants)
                {
                    for (int i = 0; i < gBuffer.gBufferCount; ++i)
                        passData.gBuffer[i] = builder.ReadTexture(gBuffer.mrt[i]);
                    passData.gBufferCount = gBuffer.gBufferCount;
                }

                passData.lightVolumeDataBuffer = m_TileAndClusterData.lightVolumeDataBuffer;
                passData.convexBoundsBuffer = m_TileAndClusterData.convexBoundsBuffer;
                passData.AABBBoundsBuffer = m_TileAndClusterData.AABBBoundsBuffer;
                passData.globalLightListAtomic = m_TileAndClusterData.globalLightListAtomic;

                passData.output.tileFeatureFlags = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.tileFeatureFlags));
                passData.output.dispatchIndirectBuffer = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.dispatchIndirectBuffer));
                passData.output.perVoxelOffset = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.perVoxelOffset));
                passData.output.perTileLogBaseTweak = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.perTileLogBaseTweak));
                passData.output.tileList = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.tileList));
                passData.output.bigTileLightList = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.bigTileLightList));
                passData.output.perVoxelLightLists = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.perVoxelLightLists));
                passData.output.lightList = builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_TileAndClusterData.lightList));

                builder.SetRenderFunc(
                (BuildGPULightListPassData data, RenderGraphContext context) =>
                {
                    bool tileFlagsWritten = false;

                    var buildLightListResources = PrepareBuildGPULightListResources(context, data);

                    ClearLightLists(data.buildGPULightListParameters, buildLightListResources, context.cmd);
                    GenerateLightsScreenSpaceAABBs(data.buildGPULightListParameters, buildLightListResources, context.cmd);
                    BigTilePrepass(data.buildGPULightListParameters, buildLightListResources, context.cmd);
                    BuildPerTileLightList(data.buildGPULightListParameters, buildLightListResources, ref tileFlagsWritten, context.cmd);
                    VoxelLightListGeneration(data.buildGPULightListParameters, buildLightListResources, context.cmd);

                    BuildDispatchIndirectArguments(data.buildGPULightListParameters, buildLightListResources, tileFlagsWritten, context.cmd);

                    // TODO RENDERGRAPH WARNING: Note that the three sets of variables are bound here, but it should be handled differently.
                    PushLightLoopGlobalParams(data.lightLoopGlobalParameters, context.cmd);
                });

                return passData.output;
            }
        }

        class PushGlobalCameraParamPassData
        {
            public HDCamera                 hdCamera;
            public int                      frameCount;
            public ShaderVariablesGlobal    globalCB;
            public ShaderVariablesXR        xrCB;

        }

        void PushGlobalCameraParams(RenderGraph renderGraph, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<PushGlobalCameraParamPassData>("Push Global Camera Parameters", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.frameCount = m_FrameCount;
                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.xrCB = m_ShaderVariablesXRCB;

                builder.SetRenderFunc(
                (PushGlobalCameraParamPassData data, RenderGraphContext context) =>
                {
                    data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB, data.frameCount);
                    ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                    data.hdCamera.UpdateShaderVariablesXRCB(ref data.xrCB);
                    ConstantBuffer.PushGlobal(context.cmd, data.xrCB, HDShaderIDs._ShaderVariablesXR);
                });
            }
        }

        internal ShadowResult RenderShadows(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullResults)
        {
            var result = m_ShadowManager.RenderShadows(m_RenderGraph, m_ShaderVariablesGlobalCB, hdCamera, cullResults);
            // Need to restore global camera parameters.
            PushGlobalCameraParams(renderGraph, hdCamera);
            return result;
        }

        TextureHandle CreateDiffuseLightingBuffer(RenderGraph renderGraph, bool msaa)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = !msaa,
                    bindTextureMS = msaa, enableMSAA = msaa, clearBuffer = true, clearColor = Color.clear, name = string.Format("CameraSSSDiffuseLighting{0}", msaa ? "MSAA" : "") });
        }

        class DeferredLightingPassData
        {
            public DeferredLightingParameters   parameters;

            public TextureHandle                colorBuffer;
            public TextureHandle                sssDiffuseLightingBuffer;
            public TextureHandle                depthBuffer;
            public TextureHandle                depthTexture;

            public int                          gbufferCount;
            public int                          lightLayersTextureIndex;
            public TextureHandle[]              gbuffer = new TextureHandle[8];

            public ComputeBufferHandle          lightListBuffer;
            public ComputeBufferHandle          tileFeatureFlagsBuffer;
            public ComputeBufferHandle          tileListBuffer;
            public ComputeBufferHandle          dispatchIndirectBuffer;
        }

        struct LightingOutput
        {
            public TextureHandle colorBuffer;
        }

        LightingOutput RenderDeferredLighting(  RenderGraph                 renderGraph,
                                                HDCamera                    hdCamera,
                                                TextureHandle               colorBuffer,
                                                TextureHandle               depthStencilBuffer,
                                                TextureHandle               depthPyramidTexture,
                                                in LightingBuffers          lightingBuffers,
                                                in GBufferOutput            gbuffer,
                                                in ShadowResult             shadowResult,
                                                in BuildGPULightListOutput  lightLists)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred)
                return new LightingOutput();

            using (var builder = renderGraph.AddRenderPass<DeferredLightingPassData>("Deferred Lighting", out var passData))
            {
                passData.parameters = PrepareDeferredLightingParameters(hdCamera, m_CurrentDebugDisplaySettings);

                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                if (passData.parameters.outputSplitLighting)
                {
                    passData.sssDiffuseLightingBuffer = builder.WriteTexture(lightingBuffers.diffuseLightingBuffer);
                }
                else
                {
                    // TODO RENDERGRAPH: Check how to avoid this kind of pattern.
                    // Unfortunately, the low level needs this texture to always be bound with UAV enabled, so in order to avoid effectively creating the full resolution texture here,
                    // we need to create a small dummy texture.
                    passData.sssDiffuseLightingBuffer = builder.CreateTransientTexture(new TextureDesc(1, 1, true, true) { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true } );
                }
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.depthTexture = builder.ReadTexture(depthPyramidTexture);

                // TODO RENDERGRAPH: Check why this is needed
                ReadLightingBuffers(lightingBuffers, builder);

                passData.lightLayersTextureIndex = gbuffer.lightLayersTextureIndex;
                passData.gbufferCount = gbuffer.gBufferCount;
                for (int i = 0; i < gbuffer.gBufferCount; ++i)
                    passData.gbuffer[i] = builder.ReadTexture(gbuffer.mrt[i]);

                HDShadowManager.ReadShadowResult(shadowResult, builder);

                passData.lightListBuffer = builder.ReadComputeBuffer(lightLists.lightList);
                passData.tileFeatureFlagsBuffer = builder.ReadComputeBuffer(lightLists.tileFeatureFlags);
                passData.tileListBuffer = builder.ReadComputeBuffer(lightLists.tileList);
                passData.dispatchIndirectBuffer = builder.ReadComputeBuffer(lightLists.dispatchIndirectBuffer);

                var output = new LightingOutput();
                output.colorBuffer = passData.colorBuffer;

                builder.SetRenderFunc(
                (DeferredLightingPassData data, RenderGraphContext context) =>
                {
                    var resources = new DeferredLightingResources();

                    resources.colorBuffers = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                    resources.colorBuffers[0] = context.resources.GetTexture(data.colorBuffer);
                    resources.colorBuffers[1] = context.resources.GetTexture(data.sssDiffuseLightingBuffer);
                    resources.depthStencilBuffer = context.resources.GetTexture(data.depthBuffer);
                    resources.depthTexture = context.resources.GetTexture(data.depthTexture);

                    resources.lightListBuffer = context.resources.GetComputeBuffer(data.lightListBuffer);
                    resources.tileFeatureFlagsBuffer = context.resources.GetComputeBuffer(data.tileFeatureFlagsBuffer);
                    resources.tileListBuffer = context.resources.GetComputeBuffer(data.tileListBuffer);
                    resources.dispatchIndirectBuffer = context.resources.GetComputeBuffer(data.dispatchIndirectBuffer);

                    // TODO RENDERGRAPH: try to find a better way to bind this.
                    // Issue is that some GBuffers have several names (for example normal buffer is both NormalBuffer and GBuffer1)
                    // So it's not possible to use auto binding via dependency to shaderTagID
                    // Should probably get rid of auto binding and go explicit all the way (might need to wait for us to remove non rendergraph code path).
                    for (int i = 0; i < data.gbufferCount; ++i)
                        context.cmd.SetGlobalTexture(HDShaderIDs._GBufferTexture[i], context.resources.GetTexture(data.gbuffer[i]));

                    if (data.lightLayersTextureIndex != -1)
                        context.cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, context.resources.GetTexture(data.gbuffer[data.lightLayersTextureIndex]));
                    else
                        context.cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, TextureXR.GetWhiteTexture());

                    if (data.parameters.enableTile)
                    {
                        bool useCompute = data.parameters.useComputeLightingEvaluation && !k_PreferFragment;
                        if (useCompute)
                            RenderComputeDeferredLighting(data.parameters, resources, context.cmd);
                        else
                            RenderComputeAsPixelDeferredLighting(data.parameters, resources, context.cmd);
                    }
                    else
                    {
                        RenderPixelDeferredLighting(data.parameters, resources, context.cmd);
                    }
                });

                return output;
            }
        }

        class RenderSSRPassData
        {
            public RenderSSRParameters parameters;
            public TextureHandle depthBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle colorPyramid;
            public TextureHandle stencilBuffer;
            public TextureHandle hitPointsTexture;
            public TextureHandle lightingTexture;
            public TextureHandle clearCoatMask;
            //public TextureHandle debugTexture;
        }

        TextureHandle RenderSSR(    RenderGraph     renderGraph,
                                    HDCamera        hdCamera,
                                    TextureHandle   normalBuffer,
                                    TextureHandle   motionVectorsBuffer,
                                    TextureHandle   depthBuffer,
                                    TextureHandle   depthPyramid,
                                    TextureHandle   stencilBuffer,
                                    TextureHandle   clearCoatMask)
        {
            var ssrBlackTexture = renderGraph.ImportTexture(TextureXR.GetBlackTexture(), HDShaderIDs._SsrLightingTexture);

            if (!hdCamera.IsSSREnabled())
                return ssrBlackTexture;

            TextureHandle result;

            // TODO RENDERGRAPH
            //var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            //bool usesRaytracedReflections = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value;
            //if (usesRaytracedReflections)
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

                    passData.parameters = PrepareSSRParameters(hdCamera, m_DepthBufferMipChainInfo, true);
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.depthPyramid = builder.ReadTexture(depthPyramid);
                    passData.colorPyramid = builder.ReadTexture(colorPyramid);
                    passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                    passData.clearCoatMask = builder.ReadTexture(clearCoatMask);

                    builder.ReadTexture(normalBuffer);
                    builder.ReadTexture(motionVectorsBuffer);

                    // In practice, these textures are sparse (mostly black). Therefore, clearing them is fast (due to CMASK),
                    // and much faster than fully overwriting them from within SSR shaders.
                    passData.hitPointsTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16_UNorm, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Hit_Point_Texture" });
                    passData.lightingTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Lighting_Texture" }, HDShaderIDs._SsrLightingTexture));
                    //passData.hitPointsTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    //    { colorFormat = GraphicsFormat.ARGBFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Debug_Texture" }));

                    builder.SetRenderFunc(
                    (RenderSSRPassData data, RenderGraphContext context) =>
                    {
                        var res = context.resources;
                        RenderSSR(data.parameters,
                                    res.GetTexture(data.depthBuffer),
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
            public TextureHandle                depthTexture;
            public TextureHandle                contactShadowsTexture;
            public ComputeBufferHandle          lightList;
        }

        TextureHandle RenderContactShadows(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, BuildGPULightListOutput lightLists, int firstMipOffsetY)
        {
            if (!WillRenderContactShadow())
                return renderGraph.ImportTexture(TextureXR.GetClearTexture(), HDShaderIDs._ContactShadowTexture);

            TextureHandle result;
            using (var builder = renderGraph.AddRenderPass<RenderContactShadowPassData>("Contact Shadows", out var passData))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.ContactShadowsRunAsync());

                // Avoid garbage when visualizing contact shadows.
                bool clearBuffer = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ContactShadows;

                passData.parameters = PrepareContactShadowsParameters(hdCamera, firstMipOffsetY);
                passData.lightLoopLightData = m_LightLoopLightData;
                passData.lightList = builder.ReadComputeBuffer(lightLists.lightList);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.contactShadowsTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, clearBuffer = clearBuffer, clearColor = Color.clear, name = "ContactShadowsBuffer" }, HDShaderIDs._ContactShadowTexture));

                result = passData.contactShadowsTexture;

                builder.SetRenderFunc(
                (RenderContactShadowPassData data, RenderGraphContext context) =>
                {
                    var res = context.resources;
                    RenderContactShadows(data.parameters, res.GetTexture(data.contactShadowsTexture), res.GetTexture(data.depthTexture), data.lightLoopLightData, res.GetComputeBuffer(data.lightList), context.cmd);
                });
            }

            PushFullScreenDebugTexture(renderGraph, result, FullScreenDebugMode.ContactShadows);
            return result;
        }

        class VolumeVoxelizationPassData
        {
            public VolumeVoxelizationParameters parameters;
            public TextureHandle                densityBuffer;
            public ComputeBufferHandle          bigTileLightListBuffer;
            public ComputeBuffer                visibleVolumeBoundsBuffer;
            public ComputeBuffer                visibleVolumeDataBuffer;
        }

        TextureHandle VolumeVoxelizationPass(   RenderGraph         renderGraph,
                                                HDCamera            hdCamera,
                                                ComputeBuffer       visibleVolumeBoundsBuffer,
                                                ComputeBuffer       visibleVolumeDataBuffer,
                                                ComputeBufferHandle bigTileLightList,
                                                int                 frameIndex)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                using (var builder = renderGraph.AddRenderPass<VolumeVoxelizationPassData>("Volume Voxelization", out var passData))
                {
                    builder.EnableAsyncCompute(hdCamera.frameSettings.VolumeVoxelizationRunsAsync());

                    passData.parameters = PrepareVolumeVoxelizationParameters(hdCamera, frameIndex);
                    passData.visibleVolumeBoundsBuffer = visibleVolumeBoundsBuffer;
                    passData.visibleVolumeDataBuffer = visibleVolumeDataBuffer;
                    passData.bigTileLightListBuffer = builder.ReadComputeBuffer(bigTileLightList);

                    float tileSize = 0;
                    Vector3Int viewportSize = ComputeVolumetricViewportSize(hdCamera, ref tileSize);

                    passData.densityBuffer = builder.WriteTexture(renderGraph.ImportTexture(m_DensityBuffer));

                    builder.SetRenderFunc(
                    (VolumeVoxelizationPassData data, RenderGraphContext ctx) =>
                    {
                        VolumeVoxelizationPass( data.parameters,
                                                ctx.resources.GetTexture(data.densityBuffer),
                                                data.visibleVolumeBoundsBuffer,
                                                data.visibleVolumeDataBuffer,
                                                ctx.resources.GetComputeBuffer(data.bigTileLightListBuffer),
                                                ctx.cmd);
                    });

                    return passData.densityBuffer;
                }
            }
            return new TextureHandle();
        }

        class VolumetricLightingPassData
        {
            public VolumetricLightingParameters parameters;
            public TextureHandle                densityBuffer;
            public TextureHandle                lightingBuffer;
            public TextureHandle                historyBuffer;
            public TextureHandle                feedbackBuffer;
            public ComputeBufferHandle          bigTileLightListBuffer;
        }

        TextureHandle VolumetricLightingPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle densityBuffer, ComputeBufferHandle bigTileLightListBuffer, ShadowResult shadowResult, int frameIndex)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                var parameters = PrepareVolumetricLightingParameters(hdCamera, frameIndex);

                using (var builder = renderGraph.AddRenderPass<VolumetricLightingPassData>("Volumetric Lighting", out var passData))
                {
                    // TODO RENDERGRAPH
                    //builder.EnableAsyncCompute(hdCamera.frameSettings.VolumetricLightingRunsAsync());

                    passData.parameters = parameters;
                    passData.bigTileLightListBuffer = builder.ReadComputeBuffer(bigTileLightListBuffer);
                    passData.densityBuffer = builder.ReadTexture(densityBuffer);

                    float tileSize = 0;
                    Vector3Int viewportSize = ComputeVolumetricViewportSize(hdCamera, ref tileSize);

                    // TODO RENDERGRAPH: Auto-scale of 3D RTs is not supported yet so we need to find a better solution for this. Or keep it as is?
                    passData.lightingBuffer = builder.WriteTexture(renderGraph.ImportTexture(m_LightingBuffer, HDShaderIDs._VBufferLighting));

                    if (passData.parameters.enableReprojection)
                    {
                        var currIdx = (frameIndex + 0) & 1;
                        var prevIdx = (frameIndex + 1) & 1;

                        passData.feedbackBuffer = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[currIdx]));
                        passData.historyBuffer  = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[prevIdx]));
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
                                                data.parameters.enableReprojection ? ctx.resources.GetTexture(data.historyBuffer)  : null,
                                                data.parameters.enableReprojection ? ctx.resources.GetTexture(data.feedbackBuffer) : null,
                                                ctx.resources.GetComputeBuffer(data.bigTileLightListBuffer),
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
