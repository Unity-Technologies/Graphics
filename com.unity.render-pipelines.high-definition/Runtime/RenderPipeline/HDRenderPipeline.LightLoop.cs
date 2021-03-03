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
            // TODO RENDERGRAPH: Those two buffers aren't really lighting buffers but only used for SSS
            // We should probably move them out of here.
            public TextureHandle    sssBuffer;
            public TextureHandle    diffuseLightingBuffer;

            public TextureHandle    ambientOcclusionBuffer;
            public TextureHandle    ssrLightingBuffer;
            public TextureHandle    ssgiLightingBuffer;
            public TextureHandle    contactShadowsBuffer;
            public TextureHandle    screenspaceShadowBuffer;
        }

        static LightingBuffers ReadLightingBuffers(in LightingBuffers buffers, RenderGraphBuilder builder)
        {
            var result = new LightingBuffers();
            // We only read those buffers because sssBuffer and diffuseLightingBuffer our just output of the lighting process, not inputs.
            result.ambientOcclusionBuffer = builder.ReadTexture(buffers.ambientOcclusionBuffer);
            result.ssrLightingBuffer = builder.ReadTexture(buffers.ssrLightingBuffer);
            result.ssgiLightingBuffer = builder.ReadTexture(buffers.ssgiLightingBuffer);
            result.contactShadowsBuffer = builder.ReadTexture(buffers.contactShadowsBuffer);
            result.screenspaceShadowBuffer = builder.ReadTexture(buffers.screenspaceShadowBuffer);

            return result;
        }

        static void BindGlobalLightingBuffers(in LightingBuffers buffers, CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, buffers.ambientOcclusionBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, buffers.ssrLightingBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, buffers.ssgiLightingBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._ContactShadowTexture, buffers.contactShadowsBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, buffers.screenspaceShadowBuffer);
        }

        class BuildGPULightListPassData
        {
            public BuildGPULightListParameters  buildGPULightListParameters;
            public TextureHandle                depthBuffer;
            public TextureHandle                stencilTexture;
            public TextureHandle[]              gBuffer = new TextureHandle[RenderGraph.kMaxMRTCount];
            public int                          gBufferCount;

            // Buffers filled with the CPU outside of render graph.
            public ComputeBufferHandle          convexBoundsBuffer;
            public ComputeBufferHandle          AABBBoundsBuffer;

            // Transient buffers that are not used outside of BuildGPULight list so they don't need to go outside the pass.
            public ComputeBufferHandle          globalLightListAtomic;
            public ComputeBufferHandle          lightVolumeDataBuffer;

            public BuildGPULightListOutput      output = new BuildGPULightListOutput();
        }

        struct BuildGPULightListOutput
        {
            // Tile
            public ComputeBufferHandle lightList;
            public ComputeBufferHandle tileList;
            public ComputeBufferHandle tileFeatureFlags;
            public ComputeBufferHandle dispatchIndirectBuffer;

            // Big Tile
            public ComputeBufferHandle bigTileLightList;

            // Cluster
            public ComputeBufferHandle perVoxelOffset;
            public ComputeBufferHandle perVoxelLightLists;
            public ComputeBufferHandle perTileLogBaseTweak;
        }

        static BuildGPULightListResources PrepareBuildGPULightListResources(RenderGraphContext context, BuildGPULightListPassData data)
        {
            var buildLightListResources = new BuildGPULightListResources();

            // Depending on frame setting configurations we might not have written to a depth buffer yet.
            RTHandle depthBuffer = data.depthBuffer;

            if (depthBuffer == null)
            {
                buildLightListResources.depthBuffer = context.defaultResources.blackTextureXR;
                buildLightListResources.stencilTexture = context.defaultResources.blackTextureXR;
            }
            else
            {
                buildLightListResources.depthBuffer = data.depthBuffer;
                buildLightListResources.stencilTexture = data.stencilTexture;
            }

            if (data.buildGPULightListParameters.computeMaterialVariants && data.buildGPULightListParameters.enableFeatureVariants)
            {
                buildLightListResources.gBuffer = context.renderGraphPool.GetTempArray<RTHandle>(data.gBufferCount);
                for (int i = 0; i < data.gBufferCount; ++i)
                    buildLightListResources.gBuffer[i] = data.gBuffer[i];
            }

            buildLightListResources.lightVolumeDataBuffer = data.lightVolumeDataBuffer;
            buildLightListResources.convexBoundsBuffer = data.convexBoundsBuffer;
            buildLightListResources.AABBBoundsBuffer = data.AABBBoundsBuffer;
            buildLightListResources.globalLightListAtomic = data.globalLightListAtomic;

            buildLightListResources.tileFeatureFlags = data.output.tileFeatureFlags;
            buildLightListResources.dispatchIndirectBuffer = data.output.dispatchIndirectBuffer;
            buildLightListResources.perVoxelOffset = data.output.perVoxelOffset;
            buildLightListResources.perTileLogBaseTweak = data.output.perTileLogBaseTweak;
            buildLightListResources.tileList = data.output.tileList;
            buildLightListResources.bigTileLightList = data.output.bigTileLightList;
            buildLightListResources.perVoxelLightLists = data.output.perVoxelLightLists;
            buildLightListResources.lightList = data.output.lightList;

            return buildLightListResources;
        }

        BuildGPULightListOutput BuildGPULightList(RenderGraph                     renderGraph,
            HDCamera                        hdCamera,
            TileAndClusterData              tileAndClusterData,
            int                             totalLightCount,
            ref ShaderVariablesLightList    constantBuffer,
            TextureHandle                   depthStencilBuffer,
            TextureHandle                   stencilBufferCopy,
            GBufferOutput                   gBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<BuildGPULightListPassData>("Build Light List", out var passData, ProfilingSampler.Get(HDProfileId.BuildLightList)))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.BuildLightListRunsAsync());

                passData.buildGPULightListParameters = PrepareBuildGPULightListParameters(hdCamera, tileAndClusterData, ref constantBuffer, totalLightCount);
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.stencilTexture = builder.ReadTexture(stencilBufferCopy);
                if (passData.buildGPULightListParameters.computeMaterialVariants && passData.buildGPULightListParameters.enableFeatureVariants)
                {
                    for (int i = 0; i < gBuffer.gBufferCount; ++i)
                        passData.gBuffer[i] = builder.ReadTexture(gBuffer.mrt[i]);
                    passData.gBufferCount = gBuffer.gBufferCount;
                }

                // Here we use m_MaxViewCount/m_MaxWidthHeight to avoid always allocating buffers of different sizes for each camera.
                // This way we'll be reusing them more often.

                // Those buffer are filled with the CPU outside of the render graph.
                passData.convexBoundsBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(tileAndClusterData.convexBoundsBuffer));
                passData.lightVolumeDataBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(tileAndClusterData.lightVolumeDataBuffer));

                passData.globalLightListAtomic = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(1, sizeof(uint)) { name = "LightListAtomic"});
                passData.AABBBoundsBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(m_MaxViewCount * 2 * tileAndClusterData.maxLightCount, 4 * sizeof(float)) { name = "AABBBoundBuffer" });

                var nrTilesX = (m_MaxCameraWidth + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                var nrTilesY = (m_MaxCameraHeight + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                var nrTiles = nrTilesX * nrTilesY * m_MaxViewCount;
                const int capacityUShortsPerTile = 32;
                const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1; // room for 31 lights and a nrLights value.

                if (tileAndClusterData.hasTileBuffers)
                {
                    // note that nrTiles include the viewCount in allocation below
                    // Tile buffers
                    passData.output.lightList = builder.WriteComputeBuffer(
                        renderGraph.CreateComputeBuffer(new ComputeBufferDesc((int)LightCategory.Count * dwordsPerTile * nrTiles, sizeof(uint)) { name = "LightList" }));
                    passData.output.tileList = builder.WriteComputeBuffer(
                        renderGraph.CreateComputeBuffer(new ComputeBufferDesc(LightDefinitions.s_NumFeatureVariants * nrTiles, sizeof(uint)) { name = "TileList" }));
                    passData.output.tileFeatureFlags = builder.WriteComputeBuffer(
                        renderGraph.CreateComputeBuffer(new ComputeBufferDesc(nrTiles, sizeof(uint)) { name = "TileFeatureFlags" }));
                    // DispatchIndirect: Buffer with arguments has to have three integer numbers at given argsOffset offset: number of work groups in X dimension, number of work groups in Y dimension, number of work groups in Z dimension.
                    // DrawProceduralIndirect: Buffer with arguments has to have four integer numbers at given argsOffset offset: vertex count per instance, instance count, start vertex location, and start instance location
                    // Use use max size of 4 unit for allocation
                    passData.output.dispatchIndirectBuffer = builder.WriteComputeBuffer(
                        renderGraph.CreateComputeBuffer(new ComputeBufferDesc(m_MaxViewCount * LightDefinitions.s_NumFeatureVariants * 4, sizeof(uint), ComputeBufferType.IndirectArguments) { name = "DispatchIndirectBuffer" }));
                }

                // Big Tile buffer
                if (passData.buildGPULightListParameters.runBigTilePrepass)
                {
                    var nrBigTilesX = (m_MaxCameraWidth + 63) / 64;
                    var nrBigTilesY = (m_MaxCameraHeight + 63) / 64;
                    var nrBigTiles = nrBigTilesX * nrBigTilesY * m_MaxViewCount;
                    // TODO: (Nick) In the case of Probe Volumes, this buffer could be trimmed down / tuned more specifically to probe volumes if we added a s_MaxNrBigTileProbeVolumesPlusOne value.
                    passData.output.bigTileLightList = builder.WriteComputeBuffer(
                        renderGraph.CreateComputeBuffer(new ComputeBufferDesc(LightDefinitions.s_MaxNrBigTileLightsPlusOne * nrBigTiles, sizeof(uint)) { name = "BigTiles" }));
                }

                // Cluster buffers
                var nrClustersX = (m_MaxCameraWidth + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                var nrClustersY = (m_MaxCameraHeight + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                var nrClusterTiles = nrClustersX * nrClustersY * m_MaxViewCount;

                passData.output.perVoxelOffset = builder.WriteComputeBuffer(
                    renderGraph.CreateComputeBuffer(new ComputeBufferDesc((int)LightCategory.Count * (1 << k_Log2NumClusters) * nrClusterTiles, sizeof(uint)) { name = "PerVoxelOffset" }));
                passData.output.perVoxelLightLists = builder.WriteComputeBuffer(
                    renderGraph.CreateComputeBuffer(new ComputeBufferDesc(NumLightIndicesPerClusteredTile() * nrClusterTiles, sizeof(uint)) { name = "PerVoxelLightList" }));
                if (tileAndClusterData.clusterNeedsDepth)
                {
                    passData.output.perTileLogBaseTweak = builder.WriteComputeBuffer(
                        renderGraph.CreateComputeBuffer(new ComputeBufferDesc(nrClusterTiles, sizeof(float)) { name = "PerTileLogBaseTweak" }));
                }

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
                    });

                return passData.output;
            }
        }

        class PushGlobalCameraParamPassData
        {
            public HDCamera                 hdCamera;
            public ShaderVariablesGlobal    globalCB;
            public ShaderVariablesXR        xrCB;
        }

        void PushGlobalCameraParams(RenderGraph renderGraph, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<PushGlobalCameraParamPassData>("Push Global Camera Parameters", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.xrCB = m_ShaderVariablesXRCB;

                builder.SetRenderFunc(
                    (PushGlobalCameraParamPassData data, RenderGraphContext context) =>
                    {
                        data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);
                        ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                        data.hdCamera.UpdateShaderVariablesXRCB(ref data.xrCB);
                        ConstantBuffer.PushGlobal(context.cmd, data.xrCB, HDShaderIDs._ShaderVariablesXR);
                    });
            }
        }

        internal ShadowResult RenderShadows(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullResults, ref ShadowResult result)
        {
            m_ShadowManager.RenderShadows(m_RenderGraph, m_ShaderVariablesGlobalCB, hdCamera, cullResults, ref result);
            // Need to restore global camera parameters.
            PushGlobalCameraParams(renderGraph, hdCamera);
            return result;
        }

        TextureHandle CreateDiffuseLightingBuffer(RenderGraph renderGraph, bool msaa)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = !msaa,
                bindTextureMS = msaa, enableMSAA = msaa, clearBuffer = true, clearColor = Color.clear, name = msaa ? "CameraSSSDiffuseLightingMSAA" : "CameraSSSDiffuseLighting"
            });
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
            public int                          shadowMaskTextureIndex;
            public TextureHandle[]              gbuffer = new TextureHandle[8];

            public ComputeBufferHandle          lightListBuffer;
            public ComputeBufferHandle          tileFeatureFlagsBuffer;
            public ComputeBufferHandle          tileListBuffer;
            public ComputeBufferHandle          dispatchIndirectBuffer;

            public LightingBuffers              lightingBuffers;
        }

        struct LightingOutput
        {
            public TextureHandle colorBuffer;
        }

        LightingOutput RenderDeferredLighting(RenderGraph                 renderGraph,
            HDCamera                    hdCamera,
            TextureHandle               colorBuffer,
            TextureHandle               depthStencilBuffer,
            TextureHandle               depthPyramidTexture,
            in LightingBuffers          lightingBuffers,
            in GBufferOutput            gbuffer,
            in ShadowResult             shadowResult,
            in BuildGPULightListOutput  lightLists)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
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
                    passData.sssDiffuseLightingBuffer = builder.CreateTransientTexture(new TextureDesc(1, 1, true, true) { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true });
                }
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.depthTexture = builder.ReadTexture(depthPyramidTexture);

                passData.lightingBuffers = ReadLightingBuffers(lightingBuffers, builder);

                passData.lightLayersTextureIndex = gbuffer.lightLayersTextureIndex;
                passData.shadowMaskTextureIndex = gbuffer.shadowMaskTextureIndex;
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
                        resources.colorBuffers[0] = data.colorBuffer;
                        resources.colorBuffers[1] = data.sssDiffuseLightingBuffer;
                        resources.depthStencilBuffer = data.depthBuffer;
                        resources.depthTexture = data.depthTexture;

                        resources.lightListBuffer = data.lightListBuffer;
                        resources.tileFeatureFlagsBuffer = data.tileFeatureFlagsBuffer;
                        resources.tileListBuffer = data.tileListBuffer;
                        resources.dispatchIndirectBuffer = data.dispatchIndirectBuffer;

                        // TODO RENDERGRAPH: try to find a better way to bind this.
                        // Issue is that some GBuffers have several names (for example normal buffer is both NormalBuffer and GBuffer1)
                        // So it's not possible to use auto binding via dependency to shaderTagID
                        // Should probably get rid of auto binding and go explicit all the way (might need to wait for us to remove non rendergraph code path).
                        for (int i = 0; i < data.gbufferCount; ++i)
                            context.cmd.SetGlobalTexture(HDShaderIDs._GBufferTexture[i], data.gbuffer[i]);

                        if (data.lightLayersTextureIndex != -1)
                            context.cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, data.gbuffer[data.lightLayersTextureIndex]);
                        else
                            context.cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, TextureXR.GetWhiteTexture());

                        if (data.shadowMaskTextureIndex != -1)
                            context.cmd.SetGlobalTexture(HDShaderIDs._ShadowMaskTexture, data.gbuffer[data.shadowMaskTextureIndex]);
                        else
                            context.cmd.SetGlobalTexture(HDShaderIDs._ShadowMaskTexture, TextureXR.GetWhiteTexture());

                        // TODO RENDERGRAPH: Remove these SetGlobal and properly send these textures to the deferred passes and bind them directly to compute shaders.
                        // This can wait that we remove the old code path.
                        BindGlobalLightingBuffers(data.lightingBuffers, context.cmd);

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
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;
            public TextureHandle colorPyramid;
            public TextureHandle stencilBuffer;
            public TextureHandle hitPointsTexture;
            public TextureHandle ssrAccum;
            public TextureHandle lightingTexture;
            public TextureHandle ssrAccumPrev;
            public TextureHandle clearCoatMask;
            public ComputeBufferHandle coarseStencilBuffer;
            public BlueNoise blueNoise;
            public HDCamera hdCamera;
            //public TextureHandle debugTexture;
        }

        TextureHandle RenderSSR(RenderGraph         renderGraph,
            HDCamera            hdCamera,
            ref PrepassOutput   prepassOutput,
            TextureHandle       clearCoatMask,
            TextureHandle       rayCountTexture,
            Texture             skyTexture,
            bool                transparent)
        {
            if (!hdCamera.IsSSREnabled(transparent))
                return renderGraph.defaultResources.blackTextureXR;

            TextureHandle result;

            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            bool usesRaytracedReflections = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value;
            if (usesRaytracedReflections)
            {
                result = RenderRayTracedReflections(renderGraph, hdCamera,
                    prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.normalBuffer, prepassOutput.resolvedMotionVectorsBuffer, clearCoatMask, skyTexture, rayCountTexture,
                    m_ShaderVariablesRayTracingCB, transparent);
            }
            else
            {
                if (transparent)
                {
                    // NOTE: Currently we profiled that generating the HTile for SSR and using it is not worth it the optimization.
                    // However if the generated HTile will be used for something else but SSR, this should be made NOT resolve only and
                    // re-enabled in the shader.
                    BuildCoarseStencilAndResolveIfNeeded(renderGraph, hdCamera, resolveOnly: true, ref prepassOutput);
                }

                using (var builder = renderGraph.AddRenderPass<RenderSSRPassData>("Render SSR", out var passData))
                {
                    builder.EnableAsyncCompute(hdCamera.frameSettings.SSRRunsAsync());

                    hdCamera.AllocateScreenSpaceAccumulationHistoryBuffer(1.0f);

                    bool usePBRAlgo = !transparent && settings.usedAlgorithm.value == ScreenSpaceReflectionAlgorithm.PBRAccumulation;
                    var colorPyramid = renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain));

                    passData.parameters = PrepareSSRParameters(hdCamera, m_DepthBufferMipChainInfo, transparent);
                    passData.depthBuffer = builder.ReadTexture(prepassOutput.depthBuffer);
                    passData.depthPyramid = builder.ReadTexture(prepassOutput.depthPyramidTexture);
                    passData.colorPyramid = builder.ReadTexture(colorPyramid);
                    passData.stencilBuffer = builder.ReadTexture(prepassOutput.stencilBuffer);
                    passData.clearCoatMask = builder.ReadTexture(clearCoatMask);
                    passData.coarseStencilBuffer = builder.ReadComputeBuffer(prepassOutput.coarseStencilBuffer);

                    passData.normalBuffer = builder.ReadTexture(prepassOutput.resolvedNormalBuffer);
                    passData.motionVectorsBuffer = builder.ReadTexture(prepassOutput.resolvedMotionVectorsBuffer);

                    passData.hdCamera = hdCamera;
                    passData.blueNoise = GetBlueNoiseManager();

                    ScreenSpaceReflection ssrVolumeSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

                    // In practice, these textures are sparse (mostly black). Therefore, clearing them is fast (due to CMASK),
                    // and much faster than fully overwriting them from within SSR shaders.
                    passData.hitPointsTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16_UNorm, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = transparent ? "SSR_Hit_Point_Texture_Trans" : "SSR_Hit_Point_Texture" });

                    if (usePBRAlgo)
                    {
                        TextureHandle ssrAccum = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation));
                        TextureHandle ssrAccumPrev = renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation));;
                        passData.ssrAccum = builder.WriteTexture(ssrAccum);
                        passData.ssrAccumPrev = builder.WriteTexture(ssrAccumPrev);
                        passData.lightingTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Lighting_Texture" });
                    }
                    else
                    {
                        passData.lightingTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Lighting_Texture" }));
                    }

                    builder.SetRenderFunc(
                        (RenderSSRPassData data, RenderGraphContext context) =>
                        {
                            RenderSSR(data.parameters,
                                data.hdCamera,
                                data.blueNoise,
                                data.depthBuffer,
                                data.depthPyramid,
                                data.normalBuffer,
                                data.motionVectorsBuffer,
                                data.hitPointsTexture,
                                data.stencilBuffer,
                                data.clearCoatMask,
                                data.colorPyramid,
                                data.ssrAccum,
                                data.lightingTexture,
                                data.ssrAccumPrev,
                                data.coarseStencilBuffer,
                                context.cmd, context.renderContext);
                        });

                    if (usePBRAlgo)
                    {
                        result = passData.ssrAccum;

                        PushFullScreenDebugTexture(renderGraph, passData.ssrAccum, FullScreenDebugMode.ScreenSpaceReflectionsAccum);
                        PushFullScreenDebugTexture(renderGraph, passData.ssrAccumPrev, FullScreenDebugMode.ScreenSpaceReflectionsPrev);
                    }
                    else
                    {
                        result = passData.lightingTexture;
                    }
                }

                if (!hdCamera.colorPyramidHistoryIsValid)
                {
                    hdCamera.colorPyramidHistoryIsValid = true; // For the next frame...
                    result = renderGraph.defaultResources.blackTextureXR;
                }
            }

            PushFullScreenDebugTexture(renderGraph, result, transparent ? FullScreenDebugMode.TransparentScreenSpaceReflections : FullScreenDebugMode.ScreenSpaceReflections);

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

        TextureHandle RenderContactShadows(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, in BuildGPULightListOutput lightLists, int firstMipOffsetY)
        {
            if (!WillRenderContactShadow())
                return renderGraph.defaultResources.blackUIntTextureXR;

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
                    { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, clearBuffer = clearBuffer, clearColor = Color.clear, name = "ContactShadowsBuffer" }));

                result = passData.contactShadowsTexture;

                builder.SetRenderFunc(
                    (RenderContactShadowPassData data, RenderGraphContext context) =>
                    {
                        RenderContactShadows(data.parameters, data.contactShadowsTexture, data.depthTexture, data.lightLoopLightData, data.lightList, context.cmd);
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

        TextureHandle VolumeVoxelizationPass(RenderGraph         renderGraph,
            HDCamera            hdCamera,
            ComputeBuffer       visibleVolumeBoundsBuffer,
            ComputeBuffer       visibleVolumeDataBuffer,
            ComputeBufferHandle bigTileLightList)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                using (var builder = renderGraph.AddRenderPass<VolumeVoxelizationPassData>("Volume Voxelization", out var passData))
                {
                    builder.EnableAsyncCompute(hdCamera.frameSettings.VolumeVoxelizationRunsAsync());

                    passData.parameters = PrepareVolumeVoxelizationParameters(hdCamera);
                    passData.visibleVolumeBoundsBuffer = visibleVolumeBoundsBuffer;
                    passData.visibleVolumeDataBuffer = visibleVolumeDataBuffer;
                    if (passData.parameters.tiledLighting)
                        passData.bigTileLightListBuffer = builder.ReadComputeBuffer(bigTileLightList);

                    float tileSize = 0;
                    Vector3Int viewportSize = ComputeVolumetricViewportSize(hdCamera, ref tileSize);

                    passData.densityBuffer = builder.WriteTexture(renderGraph.ImportTexture(m_DensityBuffer));

                    builder.SetRenderFunc(
                        (VolumeVoxelizationPassData data, RenderGraphContext ctx) =>
                        {
                            VolumeVoxelizationPass(data.parameters,
                                data.densityBuffer,
                                data.visibleVolumeBoundsBuffer,
                                data.visibleVolumeDataBuffer,
                                data.bigTileLightListBuffer,
                                ctx.cmd);
                        });

                    return passData.densityBuffer;
                }
            }
            return TextureHandle.nullHandle;
        }

        class GenerateMaxZMaskPassData
        {
            public GenerateMaxZParameters parameters;
            public TextureHandle          depthTexture;
            public TextureHandle          maxZ8xBuffer;
            public TextureHandle          maxZBuffer;
            public TextureHandle          dilatedMaxZBuffer;
        }

        TextureHandle GenerateMaxZPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, HDUtils.PackedMipChainInfo depthMipInfo)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                {
                    return renderGraph.defaultResources.blackTextureXR;
                }

                using (var builder = renderGraph.AddRenderPass<GenerateMaxZMaskPassData>("Generate Max Z Mask for Volumetric", out var passData))
                {
                    passData.parameters = PrepareGenerateMaxZParameters(hdCamera, depthMipInfo);
                    passData.depthTexture = builder.ReadTexture(depthTexture);
                    passData.maxZ8xBuffer = builder.ReadTexture(renderGraph.ImportTexture(m_MaxZMask8x));
                    passData.maxZ8xBuffer = builder.WriteTexture(passData.maxZ8xBuffer);
                    passData.maxZBuffer = builder.ReadTexture(renderGraph.ImportTexture(m_MaxZMask));
                    passData.maxZBuffer = builder.WriteTexture(passData.maxZBuffer);
                    passData.dilatedMaxZBuffer = builder.ReadTexture(renderGraph.ImportTexture(m_DilatedMaxZMask));
                    passData.dilatedMaxZBuffer = builder.WriteTexture(passData.dilatedMaxZBuffer);

                    builder.SetRenderFunc(
                        (GenerateMaxZMaskPassData data, RenderGraphContext ctx) =>
                        {
                            GenerateMaxZ(data.parameters, data.depthTexture, data.maxZ8xBuffer, data.maxZBuffer, data.dilatedMaxZBuffer, ctx.cmd);
                        });

                    return passData.dilatedMaxZBuffer;
                }
            }

            return TextureHandle.nullHandle;
        }

        class VolumetricLightingPassData
        {
            public VolumetricLightingParameters parameters;
            public TextureHandle                densityBuffer;
            public TextureHandle                depthTexture;
            public TextureHandle                lightingBuffer;
            public TextureHandle                maxZBuffer;
            public TextureHandle                historyBuffer;
            public TextureHandle                feedbackBuffer;
            public ComputeBufferHandle          bigTileLightListBuffer;
        }

        TextureHandle VolumetricLightingPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, TextureHandle densityBuffer, TextureHandle maxZBuffer, ComputeBufferHandle bigTileLightListBuffer, ShadowResult shadowResult)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                // Evaluate the parameters
                var parameters = PrepareVolumetricLightingParameters(hdCamera);

                using (var builder = renderGraph.AddRenderPass<VolumetricLightingPassData>("Volumetric Lighting", out var passData))
                {
                    // TODO RENDERGRAPH
                    //builder.EnableAsyncCompute(hdCamera.frameSettings.VolumetricLightingRunsAsync());

                    passData.parameters = parameters;
                    if (passData.parameters.tiledLighting)
                        passData.bigTileLightListBuffer = builder.ReadComputeBuffer(bigTileLightListBuffer);
                    passData.densityBuffer = builder.ReadTexture(densityBuffer);
                    passData.depthTexture = builder.ReadTexture(depthTexture);
                    passData.maxZBuffer = builder.ReadTexture(maxZBuffer);

                    float tileSize = 0;
                    Vector3Int viewportSize = ComputeVolumetricViewportSize(hdCamera, ref tileSize);

                    // TODO RENDERGRAPH: Auto-scale of 3D RTs is not supported yet so we need to find a better solution for this. Or keep it as is?
                    passData.lightingBuffer = builder.WriteTexture(renderGraph.ImportTexture(m_LightingBuffer));

                    if (passData.parameters.enableReprojection)
                    {
                        int frameIndex = (int)VolumetricFrameIndex(hdCamera);
                        var currIdx = (frameIndex + 0) & 1;
                        var prevIdx = (frameIndex + 1) & 1;

                        passData.feedbackBuffer = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[currIdx]));
                        passData.historyBuffer  = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[prevIdx]));
                    }

                    HDShadowManager.ReadShadowResult(shadowResult, builder);

                    builder.SetRenderFunc(
                        (VolumetricLightingPassData data, RenderGraphContext ctx) =>
                        {
                            VolumetricLightingPass(data.parameters,
                                data.depthTexture,
                                data.densityBuffer,
                                data.lightingBuffer,
                                data.maxZBuffer,
                                data.parameters.enableReprojection ? data.historyBuffer  : (RTHandle)null,
                                data.parameters.enableReprojection ? data.feedbackBuffer : (RTHandle)null,
                                data.bigTileLightListBuffer,
                                ctx.cmd);

                            if (data.parameters.filterVolume)
                                FilterVolumetricLighting(data.parameters, data.lightingBuffer, ctx.cmd);
                        });

                    if (parameters.enableReprojection && hdCamera.volumetricValidFrames > 1)
                        hdCamera.volumetricHistoryIsValid = true; // For the next frame..
                    else
                        hdCamera.volumetricValidFrames++;

                    return passData.lightingBuffer;
                }
            }

            return renderGraph.ImportTexture(HDUtils.clearTexture3DRTH);
        }
    }
}
