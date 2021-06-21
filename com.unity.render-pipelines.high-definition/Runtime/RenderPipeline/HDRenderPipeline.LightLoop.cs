using System;
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
            // Common
            public int totalLightCount; // Regular + Env + Decal + Local Volumetric Fog
            public int viewCount;
            public bool runLightList;
            public bool clearLightLists;
            public bool enableFeatureVariants;
            public bool computeMaterialVariants;
            public bool computeLightVariants;
            public bool skyEnabled;
            public bool probeVolumeEnabled;
            public LightList lightList;

            // Clear Light lists
            public ComputeShader clearLightListCS;
            public int clearLightListKernel;

            // Screen Space AABBs
            public ComputeShader screenSpaceAABBShader;
            public int screenSpaceAABBKernel;

            // Big Tile
            public ComputeShader bigTilePrepassShader;
            public int bigTilePrepassKernel;
            public bool runBigTilePrepass;
            public int numBigTilesX, numBigTilesY;

            // FPTL
            public ComputeShader buildPerTileLightListShader;
            public int buildPerTileLightListKernel;
            public bool runFPTL;
            public int numTilesFPTLX;
            public int numTilesFPTLY;
            public int numTilesFPTL;

            // Cluster
            public ComputeShader buildPerVoxelLightListShader;
            public ComputeShader clearClusterAtomicIndexShader;
            public int buildPerVoxelLightListKernel;
            public int numTilesClusterX;
            public int numTilesClusterY;
            public bool clusterNeedsDepth;

            // Build dispatch indirect
            public ComputeShader buildMaterialFlagsShader;
            public ComputeShader clearDispatchIndirectShader;
            public ComputeShader buildDispatchIndirectShader;
            public bool useComputeAsPixel;

            public ShaderVariablesLightList lightListCB;

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

        static void ClearLightList(BuildGPULightListPassData data, CommandBuffer cmd, ComputeBuffer bufferToClear)
        {
            cmd.SetComputeBufferParam(data.clearLightListCS, data.clearLightListKernel, HDShaderIDs._LightListToClear, bufferToClear);
            Vector2 countAndOffset = new Vector2Int(bufferToClear.count, 0);

            int groupSize = 64;
            int totalNumberOfGroupsNeeded = (bufferToClear.count + groupSize - 1) / groupSize;

            const int maxAllowedGroups = 65535;
            // On higher resolutions we might end up with more than 65535 group which is not allowed, so we need to to have multiple dispatches.
            int i = 0;
            while (totalNumberOfGroupsNeeded > 0)
            {
                countAndOffset.y = maxAllowedGroups * i;
                cmd.SetComputeVectorParam(data.clearLightListCS, HDShaderIDs._LightListEntriesAndOffset, countAndOffset);

                int currGroupCount = Math.Min(maxAllowedGroups, totalNumberOfGroupsNeeded);

                cmd.DispatchCompute(data.clearLightListCS, data.clearLightListKernel, currGroupCount, 1, 1);

                totalNumberOfGroupsNeeded -= currGroupCount;
                i++;
            }
        }

        static void ClearLightLists(BuildGPULightListPassData data, CommandBuffer cmd)
        {
            if (data.clearLightLists)
            {
                // Note we clear the whole content and not just the header since it is fast enough, happens only in one frame and is a bit more robust
                // to changes to the inner workings of the lists.
                // Also, we clear all the lists and to be resilient to changes in pipeline.
                if (data.runBigTilePrepass)
                    ClearLightList(data, cmd, data.output.bigTileLightList);
                if (data.lightList != null) // This can happen for probe volume light list build where we only generate clusters.
                    ClearLightList(data, cmd, data.output.lightList);
                ClearLightList(data, cmd, data.output.perVoxelOffset);
            }
        }

        // generate screen-space AABBs (used for both fptl and clustered).
        static void GenerateLightsScreenSpaceAABBs(BuildGPULightListPassData data, CommandBuffer cmd)
        {
            if (data.totalLightCount != 0)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.GenerateLightAABBs)))
                {
                    // With XR single-pass, we have one set of light bounds per view to iterate over (bounds are in view space for each view)
                    cmd.SetComputeBufferParam(data.screenSpaceAABBShader, data.screenSpaceAABBKernel, HDShaderIDs.g_data, data.convexBoundsBuffer);
                    cmd.SetComputeBufferParam(data.screenSpaceAABBShader, data.screenSpaceAABBKernel, HDShaderIDs.g_vBoundsBuffer, data.AABBBoundsBuffer);

                    ConstantBuffer.Push(cmd, data.lightListCB, data.screenSpaceAABBShader, HDShaderIDs._ShaderVariablesLightList);

                    const int threadsPerLight = 4;  // Shader: THREADS_PER_LIGHT (4)
                    const int threadsPerGroup = 64; // Shader: THREADS_PER_GROUP (64)

                    int groupCount = HDUtils.DivRoundUp(data.totalLightCount * threadsPerLight, threadsPerGroup);

                    cmd.DispatchCompute(data.screenSpaceAABBShader, data.screenSpaceAABBKernel, groupCount, data.viewCount, 1);
                }
            }
        }

        // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
        static void BigTilePrepass(BuildGPULightListPassData data, CommandBuffer cmd)
        {
            if (data.runLightList && data.runBigTilePrepass)
            {
                cmd.SetComputeBufferParam(data.bigTilePrepassShader, data.bigTilePrepassKernel, HDShaderIDs.g_vLightList, data.output.bigTileLightList);
                cmd.SetComputeBufferParam(data.bigTilePrepassShader, data.bigTilePrepassKernel, HDShaderIDs.g_vBoundsBuffer, data.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(data.bigTilePrepassShader, data.bigTilePrepassKernel, HDShaderIDs._LightVolumeData, data.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(data.bigTilePrepassShader, data.bigTilePrepassKernel, HDShaderIDs.g_data, data.convexBoundsBuffer);

                ConstantBuffer.Push(cmd, data.lightListCB, data.bigTilePrepassShader, HDShaderIDs._ShaderVariablesLightList);

                cmd.DispatchCompute(data.bigTilePrepassShader, data.bigTilePrepassKernel, data.numBigTilesX, data.numBigTilesY, data.viewCount);
            }
        }

        static void BuildPerTileLightList(BuildGPULightListPassData data, ref bool tileFlagsWritten, CommandBuffer cmd)
        {
            // optimized for opaques only
            if (data.runLightList && data.runFPTL)
            {
                cmd.SetComputeBufferParam(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, HDShaderIDs.g_vBoundsBuffer, data.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, HDShaderIDs._LightVolumeData, data.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, HDShaderIDs.g_data, data.convexBoundsBuffer);

                cmd.SetComputeTextureParam(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, HDShaderIDs.g_depth_tex, data.depthBuffer);
                cmd.SetComputeBufferParam(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, HDShaderIDs.g_vLightList, data.output.lightList);
                if (data.runBigTilePrepass)
                    cmd.SetComputeBufferParam(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, HDShaderIDs.g_vBigTileLightList, data.output.bigTileLightList);

                var localLightListCB = data.lightListCB;

                if (data.enableFeatureVariants)
                {
                    uint baseFeatureFlags = 0;
                    if (data.lightList.directionalLights.Count > 0)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Directional;
                    }
                    if (data.skyEnabled)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Sky;
                    }
                    if (!data.computeMaterialVariants)
                    {
                        baseFeatureFlags |= LightDefinitions.s_MaterialFeatureMaskFlags;
                    }

                    if (data.probeVolumeEnabled)
                    {
                        // If probe volume feature is enabled, we toggle this feature on for all tiles.
                        // This is necessary because all tiles must sample ambient probe fallback.
                        // It is possible we could save a little bit of work by having 2x feature flags for probe volumes:
                        // one specifiying which tiles contain probe volumes,
                        // and another triggered for all tiles to handle fallback.
                        baseFeatureFlags |= (uint)LightFeatureFlags.ProbeVolume;
                    }

                    localLightListCB.g_BaseFeatureFlags = baseFeatureFlags;

                    cmd.SetComputeBufferParam(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, HDShaderIDs.g_TileFeatureFlags, data.output.tileFeatureFlags);
                    tileFlagsWritten = true;
                }

                ConstantBuffer.Push(cmd, localLightListCB, data.buildPerTileLightListShader, HDShaderIDs._ShaderVariablesLightList);

                cmd.DispatchCompute(data.buildPerTileLightListShader, data.buildPerTileLightListKernel, data.numTilesFPTLX, data.numTilesFPTLY, data.viewCount);
            }
        }

        static void VoxelLightListGeneration(BuildGPULightListPassData data, CommandBuffer cmd)
        {
            if (data.runLightList)
            {
                // clear atomic offset index
                cmd.SetComputeBufferParam(data.clearClusterAtomicIndexShader, s_ClearVoxelAtomicKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, data.globalLightListAtomic);
                cmd.DispatchCompute(data.clearClusterAtomicIndexShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

                cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, data.globalLightListAtomic);
                cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_vLayeredLightList, data.output.perVoxelLightLists);
                cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_LayeredOffset, data.output.perVoxelOffset);
                cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, data.globalLightListAtomic);

                if (data.runBigTilePrepass)
                    cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_vBigTileLightList, data.output.bigTileLightList);

                if (data.clusterNeedsDepth)
                {
                    cmd.SetComputeTextureParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_depth_tex, data.depthBuffer);
                    cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_logBaseBuffer, data.output.perTileLogBaseTweak);
                }

                cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_vBoundsBuffer, data.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs._LightVolumeData, data.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, HDShaderIDs.g_data, data.convexBoundsBuffer);

                ConstantBuffer.Push(cmd, data.lightListCB, data.buildPerVoxelLightListShader, HDShaderIDs._ShaderVariablesLightList);

                cmd.DispatchCompute(data.buildPerVoxelLightListShader, data.buildPerVoxelLightListKernel, data.numTilesClusterX, data.numTilesClusterY, data.viewCount);
            }
        }

        static void BuildDispatchIndirectArguments(BuildGPULightListPassData data, bool tileFlagsWritten, CommandBuffer cmd)
        {
            if (data.enableFeatureVariants)
            {
                // We need to touch up the tile flags if we need material classification or, if disabled, to patch up for missing flags during the skipped light tile gen
                bool needModifyingTileFeatures = !tileFlagsWritten || data.computeMaterialVariants;
                if (needModifyingTileFeatures)
                {
                    int buildMaterialFlagsKernel = s_BuildMaterialFlagsWriteKernel;
                    data.buildMaterialFlagsShader.shaderKeywords = null;
                    if (tileFlagsWritten && data.computeLightVariants)
                    {
                        data.buildMaterialFlagsShader.EnableKeyword("USE_OR");
                    }

                    uint baseFeatureFlags = 0;
                    if (!data.computeLightVariants)
                    {
                        baseFeatureFlags |= LightDefinitions.s_LightFeatureMaskFlags;
                    }
                    if (data.probeVolumeEnabled)
                    {
                        // TODO: Verify that we should be globally enabling ProbeVolume feature for all tiles here, or if we should be using per-tile culling.
                        baseFeatureFlags |= (uint)LightFeatureFlags.ProbeVolume;
                    }

                    // If we haven't run the light list building, we are missing some basic lighting flags.
                    if (!tileFlagsWritten)
                    {
                        if (data.lightList.directionalLights.Count > 0)
                        {
                            baseFeatureFlags |= (uint)LightFeatureFlags.Directional;
                        }
                        if (data.skyEnabled)
                        {
                            baseFeatureFlags |= (uint)LightFeatureFlags.Sky;
                        }
                        if (!data.computeMaterialVariants)
                        {
                            baseFeatureFlags |= LightDefinitions.s_MaterialFeatureMaskFlags;
                        }
                    }

                    var localLightListCB = data.lightListCB;
                    localLightListCB.g_BaseFeatureFlags = baseFeatureFlags;

                    cmd.SetComputeBufferParam(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs.g_TileFeatureFlags, data.output.tileFeatureFlags);

                    for (int i = 0; i < data.gBuffer.Length; ++i)
                        cmd.SetComputeTextureParam(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._GBufferTexture[i], data.gBuffer[i]);

                    RTHandle stencilTexture = data.stencilTexture;
                    if (stencilTexture == null ||
                        stencilTexture.rt.stencilFormat == GraphicsFormat.None) // We are accessing MSAA resolved version and not the depth stencil buffer directly.
                    {
                        cmd.SetComputeTextureParam(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, data.stencilTexture);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, data.stencilTexture, 0, RenderTextureSubElement.Stencil);
                    }

                    ConstantBuffer.Push(cmd, localLightListCB, data.buildMaterialFlagsShader, HDShaderIDs._ShaderVariablesLightList);

                    cmd.DispatchCompute(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, data.numTilesFPTLX, data.numTilesFPTLY, data.viewCount);
                }

                // clear dispatch indirect buffer
                if (data.useComputeAsPixel)
                {
                    cmd.SetComputeBufferParam(data.clearDispatchIndirectShader, s_ClearDrawProceduralIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, data.output.dispatchIndirectBuffer);
                    cmd.SetComputeIntParam(data.clearDispatchIndirectShader, HDShaderIDs.g_NumTiles, data.numTilesFPTL);
                    cmd.SetComputeIntParam(data.clearDispatchIndirectShader, HDShaderIDs.g_VertexPerTile, k_HasNativeQuadSupport ? 4 : 6);
                    cmd.DispatchCompute(data.clearDispatchIndirectShader, s_ClearDrawProceduralIndirectKernel, 1, 1, 1);
                }
                else
                {
                    cmd.SetComputeBufferParam(data.clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, data.output.dispatchIndirectBuffer);
                    cmd.DispatchCompute(data.clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, 1, 1, 1);
                }

                // add tiles to indirect buffer
                cmd.SetComputeBufferParam(data.buildDispatchIndirectShader, s_BuildIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, data.output.dispatchIndirectBuffer);
                cmd.SetComputeBufferParam(data.buildDispatchIndirectShader, s_BuildIndirectKernel, HDShaderIDs.g_TileList, data.output.tileList);
                cmd.SetComputeBufferParam(data.buildDispatchIndirectShader, s_BuildIndirectKernel, HDShaderIDs.g_TileFeatureFlags, data.output.tileFeatureFlags);
                cmd.SetComputeIntParam(data.buildDispatchIndirectShader, HDShaderIDs.g_NumTiles, data.numTilesFPTL);
                cmd.SetComputeIntParam(data.buildDispatchIndirectShader, HDShaderIDs.g_NumTilesX, data.numTilesFPTLX);
                // Round on k_ThreadGroupOptimalSize so we have optimal thread for buildDispatchIndirectShader kernel
                cmd.DispatchCompute(data.buildDispatchIndirectShader, s_BuildIndirectKernel, (data.numTilesFPTL + k_ThreadGroupOptimalSize - 1) / k_ThreadGroupOptimalSize, 1, data.viewCount);
            }
        }

        static bool DeferredUseComputeAsPixel(FrameSettings frameSettings)
        {
            return frameSettings.IsEnabled(FrameSettingsField.DeferredTile) && (!frameSettings.IsEnabled(FrameSettingsField.ComputeLightEvaluation) || k_PreferFragment);
        }

        unsafe void PrepareBuildGPULightListPassData(
            RenderGraph                     renderGraph,
            RenderGraphBuilder              builder,
            HDCamera                        hdCamera,
            TileAndClusterData              tileAndClusterData,
            ref ShaderVariablesLightList    constantBuffer,
            int                             totalLightCount,
            TextureHandle                   depthStencilBuffer,
            TextureHandle                   stencilBufferCopy,
            GBufferOutput                   gBuffer,
            BuildGPULightListPassData       passData)
        {
            var camera = hdCamera.camera;

            var w = (int)hdCamera.screenSize.x;
            var h = (int)hdCamera.screenSize.y;

            // Fill the shared constant buffer.
            // We don't fill directly the one in the parameter struct because we will need those parameters for volumetric lighting as well.
            ref var cb = ref constantBuffer;
            var temp = new Matrix4x4();
            temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
            temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            // camera to screen matrix (and it's inverse)
            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            {
                var proj = hdCamera.xr.enabled ? hdCamera.xr.GetProjMatrix(viewIndex) : camera.projectionMatrix;
                m_LightListProjMatrices[viewIndex] = proj * s_FlipMatrixLHSRHS;

                for (int i = 0; i < 16; ++i)
                {
                    var tempMatrix = temp * m_LightListProjMatrices[viewIndex];
                    var invTempMatrix = tempMatrix.inverse;
                    cb.g_mScrProjectionArr[viewIndex * 16 + i] = tempMatrix[i];
                    cb.g_mInvScrProjectionArr[viewIndex * 16 + i] = invTempMatrix[i];
                }
            }

            // camera to screen matrix (and it's inverse)
            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            {
                temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                for (int i = 0; i < 16; ++i)
                {
                    var tempMatrix = temp * m_LightListProjMatrices[viewIndex];
                    var invTempMatrix = tempMatrix.inverse;
                    cb.g_mProjectionArr[viewIndex * 16 + i] = tempMatrix[i];
                    cb.g_mInvProjectionArr[viewIndex * 16 + i] = invTempMatrix[i];
                }
            }

            var decalDatasCount = Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen);

            cb.g_iNrVisibLights = totalLightCount;
            cb.g_screenSize = hdCamera.screenSize; // TODO remove and use global one.
            cb.g_viDimensions = new Vector2Int((int)hdCamera.screenSize.x, (int)hdCamera.screenSize.y);
            cb.g_isOrthographic = camera.orthographic ? 1u : 0u;
            cb.g_BaseFeatureFlags = 0; // Filled for each individual pass.
            cb.g_iNumSamplesMSAA = (int)hdCamera.msaaSamples;
            cb._EnvLightIndexShift = (uint)m_lightList.lights.Count;
            cb._DecalIndexShift = (uint)(m_lightList.lights.Count + m_lightList.envLights.Count);
            cb._LocalVolumetricFogIndexShift = (uint)(m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount);

            // Copy the constant buffer into the parameter struct.
            passData.lightListCB = cb;

            passData.totalLightCount = totalLightCount;
            passData.runLightList = passData.totalLightCount > 0;
            passData.clearLightLists = false;

            // TODO RENDERGRAPH: This logic is flawed with Render Graph.
            // In theory buffers memory might be reused from another usage entirely so keeping track of its "cleared" state does not represent the truth of their content.
            // In practice though, when resolution stays the same, buffers will be the same reused from one frame to another
            // because for now buffers are pooled based on their passData. When we do proper aliasing though, we might end up with any random chunk of memory.

            // Always build the light list in XR mode to avoid issues with multi-pass
            if (hdCamera.xr.enabled)
            {
                passData.runLightList = true;
            }
            else if (!passData.runLightList && !tileAndClusterData.listsAreClear)
            {
                passData.clearLightLists = true;
                // After that, No need to clear it anymore until we start and stop running light list building.
                tileAndClusterData.listsAreClear = true;
            }
            else if (passData.runLightList)
            {
                tileAndClusterData.listsAreClear = false;
            }

            passData.viewCount = hdCamera.viewCount;
            passData.enableFeatureVariants = GetFeatureVariantsEnabled(hdCamera.frameSettings) && tileAndClusterData.hasTileBuffers;
            passData.computeMaterialVariants = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeMaterialVariants);
            passData.computeLightVariants = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeLightVariants);
            passData.lightList = m_lightList;
            passData.skyEnabled = m_SkyManager.IsLightingSkyValid(hdCamera);
            passData.useComputeAsPixel = DeferredUseComputeAsPixel(hdCamera.frameSettings);
            passData.probeVolumeEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume);

            bool isProjectionOblique = GeometryUtils.IsProjectionMatrixOblique(m_LightListProjMatrices[0]);

            // Clear light lsts
            passData.clearLightListCS = defaultResources.shaders.clearLightListsCS;
            passData.clearLightListKernel = passData.clearLightListCS.FindKernel("ClearList");

            // Screen space AABB
            passData.screenSpaceAABBShader = buildScreenAABBShader;
            passData.screenSpaceAABBKernel = 0;

            // Big tile prepass
            passData.runBigTilePrepass = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
            passData.bigTilePrepassShader = buildPerBigTileLightListShader;
            passData.bigTilePrepassKernel = s_GenListPerBigTileKernel;
            passData.numBigTilesX = (w + 63) / 64;
            passData.numBigTilesY = (h + 63) / 64;

            // Fptl
            passData.runFPTL = hdCamera.frameSettings.fptl && tileAndClusterData.hasTileBuffers;
            passData.buildPerTileLightListShader = buildPerTileLightListShader;
            passData.buildPerTileLightListShader.shaderKeywords = null;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
            {
                passData.buildPerTileLightListShader.EnableKeyword("USE_TWO_PASS_TILED_LIGHTING");
            }
            if (isProjectionOblique)
            {
                passData.buildPerTileLightListShader.EnableKeyword("USE_OBLIQUE_MODE");
            }
            if (GetFeatureVariantsEnabled(hdCamera.frameSettings))
            {
                passData.buildPerTileLightListShader.EnableKeyword("USE_FEATURE_FLAGS");
            }
            passData.buildPerTileLightListKernel = s_GenListPerTileKernel;

            passData.numTilesFPTLX = GetNumTileFtplX(hdCamera);
            passData.numTilesFPTLY = GetNumTileFtplY(hdCamera);
            passData.numTilesFPTL = passData.numTilesFPTLX * passData.numTilesFPTLY;

            // Cluster
            bool msaa = hdCamera.msaaEnabled;
            var clustPrepassSourceIdx = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass) ? ClusterPrepassSource.BigTile : ClusterPrepassSource.None;
            var clustDepthSourceIdx = ClusterDepthSource.NoDepth;
            if (tileAndClusterData.clusterNeedsDepth)
                clustDepthSourceIdx = msaa ? ClusterDepthSource.MSAA_Depth : ClusterDepthSource.Depth;

            passData.buildPerVoxelLightListShader = buildPerVoxelLightListShader;
            passData.clearClusterAtomicIndexShader = clearClusterAtomicIndexShader;
            passData.buildPerVoxelLightListKernel = isProjectionOblique ? s_ClusterObliqueKernels[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx] : s_ClusterKernels[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx];
            passData.numTilesClusterX = GetNumTileClusteredX(hdCamera);
            passData.numTilesClusterY = GetNumTileClusteredY(hdCamera);
            passData.clusterNeedsDepth = tileAndClusterData.clusterNeedsDepth;

            // Build dispatch indirect
            passData.buildMaterialFlagsShader = buildMaterialFlagsShader;
            passData.clearDispatchIndirectShader = clearDispatchIndirectShader;
            passData.buildDispatchIndirectShader = buildDispatchIndirectShader;
            passData.buildDispatchIndirectShader.shaderKeywords = null;
            if (passData.useComputeAsPixel)
            {
                passData.buildDispatchIndirectShader.EnableKeyword("IS_DRAWPROCEDURALINDIRECT");
            }

            // Depending on frame setting configurations we might not have written to a depth buffer yet.
            if (depthStencilBuffer.IsValid())
            {
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.stencilTexture = builder.ReadTexture(stencilBufferCopy);
            }
            else
            {
                passData.depthBuffer = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
                passData.stencilTexture = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
            }

            if (passData.computeMaterialVariants && passData.enableFeatureVariants)
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

            passData.globalLightListAtomic = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(1, sizeof(uint)) { name = "LightListAtomic" });
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
            if (passData.runBigTilePrepass)
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
        }

        BuildGPULightListOutput BuildGPULightList(
            RenderGraph                     renderGraph,
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

                PrepareBuildGPULightListPassData(renderGraph, builder, hdCamera, tileAndClusterData, ref constantBuffer, totalLightCount, depthStencilBuffer, stencilBufferCopy, gBuffer, passData);

                builder.SetRenderFunc(
                    (BuildGPULightListPassData data, RenderGraphContext context) =>
                    {
                        bool tileFlagsWritten = false;

                        ClearLightLists(data, context.cmd);
                        GenerateLightsScreenSpaceAABBs(data, context.cmd);
                        BigTilePrepass(data, context.cmd);
                        BuildPerTileLightList(data, ref tileFlagsWritten, context.cmd);
                        VoxelLightListGeneration(data, context.cmd);

                        BuildDispatchIndirectArguments(data, tileFlagsWritten, context.cmd);
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

        TextureHandle CreateDiffuseLightingBuffer(RenderGraph renderGraph, MSAASamples msaaSamples)
        {
            bool msaa = msaaSamples != MSAASamples.None;
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = !msaa,
                bindTextureMS = msaa, msaaSamples = msaaSamples, clearBuffer = true, clearColor = Color.clear, name = msaa ? "CameraSSSDiffuseLightingMSAA" : "CameraSSSDiffuseLighting"
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

                        // TODO RENDERGRAPH: Remove these SetGlobal and properly send these textures to the deferred passes and bind them directly to compute shaders.
                        // This can wait that we remove the old code path.
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
            public ComputeShader ssrCS;
            public int tracingKernel;
            public int reprojectionKernel;
            public int accumulateKernel;
            public bool transparentSSR;
            public bool usePBRAlgo;
            public bool accumNeedClear;
            public bool previousAccumNeedClear;
            public bool validColorPyramid;

            public int width, height, viewCount;

            public ComputeBuffer offsetBufferData;

            public ShaderVariablesScreenSpaceReflection cb;

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
        }

        void UpdateSSRConstantBuffer(HDCamera hdCamera, ScreenSpaceReflection settings, ref ShaderVariablesScreenSpaceReflection cb)
        {
            float n = hdCamera.camera.nearClipPlane;
            float f = hdCamera.camera.farClipPlane;
            float thickness = settings.depthBufferThickness.value;

            cb._SsrThicknessScale = 1.0f / (1.0f + thickness);
            cb._SsrThicknessBias = -n / (f - n) * (thickness * cb._SsrThicknessScale);
            cb._SsrIterLimit = settings.rayMaxIterations;
            cb._SsrReflectsSky = settings.reflectSky.value ? 1 : 0;
            cb._SsrStencilBit = (int)StencilUsage.TraceReflectionRay;
            float roughnessFadeStart = 1 - settings.smoothnessFadeStart;
            cb._SsrRoughnessFadeEnd = 1 - settings.minSmoothness;
            float roughnessFadeLength = cb._SsrRoughnessFadeEnd - roughnessFadeStart;
            cb._SsrRoughnessFadeEndTimesRcpLength = (roughnessFadeLength != 0) ? (cb._SsrRoughnessFadeEnd * (1.0f / roughnessFadeLength)) : 1;
            cb._SsrRoughnessFadeRcpLength = (roughnessFadeLength != 0) ? (1.0f / roughnessFadeLength) : 0;
            cb._SsrEdgeFadeRcpLength = Mathf.Min(1.0f / settings.screenFadeDistance.value, float.MaxValue);
            cb._ColorPyramidUvScaleAndLimitPrevFrame = HDUtils.ComputeViewportScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize);
            cb._SsrColorPyramidMaxMip = hdCamera.colorPyramidHistoryMipCount - 1;
            cb._SsrDepthPyramidMaxMip = m_DepthBufferMipChainInfo.mipLevelCount - 1;
            if (hdCamera.isFirstFrame || hdCamera.cameraFrameCount <= 2)
                cb._SsrAccumulationAmount = 1.0f;
            else
                cb._SsrAccumulationAmount = Mathf.Pow(2, Mathf.Lerp(0.0f, -7.0f, settings.accumulationFactor.value));
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

            bool usesRaytracedReflections = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && ScreenSpaceReflection.RayTracingActive(settings);
            if (usesRaytracedReflections)
            {
                result = RenderRayTracedReflections(renderGraph, hdCamera,
                    prepassOutput, clearCoatMask, skyTexture, rayCountTexture,
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
                    var volumeSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

                    UpdateSSRConstantBuffer(hdCamera, volumeSettings, ref passData.cb);

                    passData.hdCamera = hdCamera;
                    passData.blueNoise = GetBlueNoiseManager();
                    passData.ssrCS = m_ScreenSpaceReflectionsCS;
                    passData.tracingKernel = m_SsrTracingKernel;
                    passData.reprojectionKernel = m_SsrReprojectionKernel;
                    passData.accumulateKernel = m_SsrAccumulateKernel;
                    passData.transparentSSR = transparent;
                    passData.usePBRAlgo = usePBRAlgo;
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;
                    passData.viewCount = hdCamera.viewCount;
                    passData.offsetBufferData = m_DepthBufferMipChainInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);
                    passData.accumNeedClear = usePBRAlgo;
                    passData.previousAccumNeedClear = usePBRAlgo && (hdCamera.currentSSRAlgorithm == ScreenSpaceReflectionAlgorithm.Approximation || hdCamera.isFirstFrame || hdCamera.resetPostProcessingHistory);
                    hdCamera.currentSSRAlgorithm = volumeSettings.usedAlgorithm.value; // Store for next frame comparison
                    passData.validColorPyramid = hdCamera.colorPyramidHistoryValidFrames > 1;

                    passData.depthBuffer = builder.ReadTexture(prepassOutput.depthBuffer);
                    passData.depthPyramid = builder.ReadTexture(prepassOutput.depthPyramidTexture);
                    passData.colorPyramid = builder.ReadTexture(colorPyramid);
                    passData.stencilBuffer = builder.ReadTexture(prepassOutput.stencilBuffer);
                    passData.clearCoatMask = builder.ReadTexture(clearCoatMask);
                    passData.coarseStencilBuffer = builder.ReadComputeBuffer(prepassOutput.coarseStencilBuffer);
                    passData.normalBuffer = builder.ReadTexture(prepassOutput.resolvedNormalBuffer);
                    passData.motionVectorsBuffer = builder.ReadTexture(prepassOutput.resolvedMotionVectorsBuffer);

                    // In practice, these textures are sparse (mostly black). Therefore, clearing them is fast (due to CMASK),
                    // and much faster than fully overwriting them from within SSR shaders.
                    passData.hitPointsTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16_UNorm, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = transparent ? "SSR_Hit_Point_Texture_Trans" : "SSR_Hit_Point_Texture" });

                    if (usePBRAlgo)
                    {
                        passData.ssrAccum = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation)));
                        passData.ssrAccumPrev = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation)));
                        passData.lightingTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Lighting_Texture" });
                    }
                    else
                    {
                        passData.lightingTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Lighting_Texture" }));
                    }

                    builder.SetRenderFunc(
                        (RenderSSRPassData data, RenderGraphContext ctx) =>
                        {
                            var cs = data.ssrCS;

                            if (data.accumNeedClear)
                                CoreUtils.SetRenderTarget(ctx.cmd, data.ssrAccum, ClearFlag.Color, Color.clear);
                            if (data.previousAccumNeedClear)
                                CoreUtils.SetRenderTarget(ctx.cmd, data.ssrAccumPrev, ClearFlag.Color, Color.clear);

                            if (!data.usePBRAlgo)
                                ctx.cmd.EnableShaderKeyword("SSR_APPROX");
                            else
                                ctx.cmd.DisableShaderKeyword("SSR_APPROX");

                            if (data.transparentSSR)
                                ctx.cmd.EnableShaderKeyword("DEPTH_SOURCE_NOT_FROM_MIP_CHAIN");
                            else
                                ctx.cmd.DisableShaderKeyword("DEPTH_SOURCE_NOT_FROM_MIP_CHAIN");

                            using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.SsrTracing)))
                            {
                                // cmd.SetComputeTextureParam(cs, kernel, "_SsrDebugTexture",    m_SsrDebugTexture);
                                // Bind the non mip chain if we are rendering the transparent version
                                ctx.cmd.SetComputeTextureParam(cs, data.tracingKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                                ctx.cmd.SetComputeTextureParam(cs, data.tracingKernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                                ctx.cmd.SetComputeTextureParam(cs, data.tracingKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                                ctx.cmd.SetComputeTextureParam(cs, data.tracingKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMask);
                                ctx.cmd.SetComputeTextureParam(cs, data.tracingKernel, HDShaderIDs._SsrHitPointTexture, data.hitPointsTexture);

                                RTHandle stencilBuffer = data.stencilBuffer;
                                if (stencilBuffer.rt.stencilFormat == GraphicsFormat.None)  // We are accessing MSAA resolved version and not the depth stencil buffer directly.
                                    ctx.cmd.SetComputeTextureParam(cs, data.tracingKernel, HDShaderIDs._StencilTexture, stencilBuffer);
                                else
                                    ctx.cmd.SetComputeTextureParam(cs, data.tracingKernel, HDShaderIDs._StencilTexture, stencilBuffer, 0, RenderTextureSubElement.Stencil);

                                ctx.cmd.SetComputeBufferParam(cs, data.tracingKernel, HDShaderIDs._CoarseStencilBuffer, data.coarseStencilBuffer);
                                ctx.cmd.SetComputeBufferParam(cs, data.tracingKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, data.offsetBufferData);

                                data.blueNoise.BindDitheredRNGData1SPP(ctx.cmd);

                                ConstantBuffer.Push(ctx.cmd, data.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                                ctx.cmd.DispatchCompute(cs, data.tracingKernel, HDUtils.DivRoundUp(data.width, 8), HDUtils.DivRoundUp(data.height, 8), data.viewCount);
                            }

                            using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.SsrReprojection)))
                            {
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._SsrHitPointTexture, data.hitPointsTexture);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._SSRAccumTexture, data.usePBRAlgo ? data.ssrAccum : data.lightingTexture);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMask);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);

                                ConstantBuffer.Push(ctx.cmd, data.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                                ctx.cmd.DispatchCompute(cs, data.reprojectionKernel, HDUtils.DivRoundUp(data.width, 8), HDUtils.DivRoundUp(data.height, 8), data.viewCount);
                            }

                            if (data.usePBRAlgo)
                            {
                                if (!data.validColorPyramid)
                                {
                                    CoreUtils.SetRenderTarget(ctx.cmd, data.ssrAccum, ClearFlag.Color, Color.clear);
                                    CoreUtils.SetRenderTarget(ctx.cmd, data.ssrAccumPrev, ClearFlag.Color, Color.clear);
                                }
                                else
                                {
                                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.SsrAccumulate)))
                                    {
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._SsrHitPointTexture, data.hitPointsTexture);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._SSRAccumTexture, data.ssrAccum);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._SsrLightingTextureRW, data.lightingTexture);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._SsrAccumPrev, data.ssrAccumPrev);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMask);
                                        ctx.cmd.SetComputeTextureParam(cs, data.accumulateKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);

                                        ConstantBuffer.Push(ctx.cmd, data.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                                        ctx.cmd.DispatchCompute(cs, data.accumulateKernel, HDUtils.DivRoundUp(data.width, 8), HDUtils.DivRoundUp(data.height, 8), data.viewCount);
                                    }
                                }
                            }
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
                    hdCamera.colorPyramidHistoryValidFrames = 0;
                    result = renderGraph.defaultResources.blackTextureXR;
                }
                else
                {
                    hdCamera.colorPyramidHistoryValidFrames++;
                }
            }

            PushFullScreenDebugTexture(renderGraph, result, transparent ? FullScreenDebugMode.TransparentScreenSpaceReflections : FullScreenDebugMode.ScreenSpaceReflections);

            return result;
        }

        class RenderContactShadowPassData
        {
            public ComputeShader contactShadowsCS;
            public int kernel;

            public Vector4 params1;
            public Vector4 params2;
            public Vector4 params3;

            public int numTilesX;
            public int numTilesY;
            public int viewCount;

            public bool rayTracingEnabled;
            public RayTracingShader contactShadowsRTS;
            public RayTracingAccelerationStructure accelerationStructure;
            public int actualWidth;
            public int actualHeight;
            public int depthTextureParameterName;

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
                builder.EnableAsyncCompute(hdCamera.frameSettings.ContactShadowsRunsAsync());

                // Avoid garbage when visualizing contact shadows.
                bool clearBuffer = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ContactShadows;
                bool msaa = hdCamera.msaaEnabled;

                passData.contactShadowsCS = contactShadowComputeShader;
                passData.contactShadowsCS.shaderKeywords = null;
                if (msaa)
                {
                    passData.contactShadowsCS.EnableKeyword("ENABLE_MSAA");
                }

                passData.rayTracingEnabled = RayTracedContactShadowsRequired();
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                {
                    passData.contactShadowsRTS = m_GlobalSettings.renderPipelineRayTracingResources.contactShadowRayTracingRT;
                    passData.accelerationStructure = RequestAccelerationStructure();

                    passData.actualWidth = hdCamera.actualWidth;
                    passData.actualHeight = hdCamera.actualHeight;
                }

                passData.kernel = s_deferredContactShadowKernel;

                float contactShadowRange = Mathf.Clamp(m_ContactShadows.fadeDistance.value, 0.0f, m_ContactShadows.maxDistance.value);
                float contactShadowFadeEnd = m_ContactShadows.maxDistance.value;
                float contactShadowOneOverFadeRange = 1.0f / Math.Max(1e-6f, contactShadowRange);

                float contactShadowMinDist = Mathf.Min(m_ContactShadows.minDistance.value, contactShadowFadeEnd);
                float contactShadowFadeIn = Mathf.Clamp(m_ContactShadows.fadeInDistance.value, 1e-6f, contactShadowFadeEnd);

                passData.params1 = new Vector4(m_ContactShadows.length.value, m_ContactShadows.distanceScaleFactor.value, contactShadowFadeEnd, contactShadowOneOverFadeRange);
                passData.params2 = new Vector4(firstMipOffsetY, contactShadowMinDist, contactShadowFadeIn, m_ContactShadows.rayBias.value * 0.01f);
                passData.params3 = new Vector4(m_ContactShadows.sampleCount, m_ContactShadows.thicknessScale.value * 10.0f, 0.0f, 0.0f);

                int deferredShadowTileSize = 8; // Must match ContactShadows.compute
                passData.numTilesX = (hdCamera.actualWidth + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
                passData.numTilesY = (hdCamera.actualHeight + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
                passData.viewCount = hdCamera.viewCount;

                passData.depthTextureParameterName = msaa ? HDShaderIDs._CameraDepthValuesTexture : HDShaderIDs._CameraDepthTexture;

                passData.lightLoopLightData = m_LightLoopLightData;
                passData.lightList = builder.ReadComputeBuffer(lightLists.lightList);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.contactShadowsTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, clearBuffer = clearBuffer, clearColor = Color.clear, name = "ContactShadowsBuffer" }));

                result = passData.contactShadowsTexture;

                builder.SetRenderFunc(
                    (RenderContactShadowPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeVectorParam(data.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters, data.params1);
                        ctx.cmd.SetComputeVectorParam(data.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters2, data.params2);
                        ctx.cmd.SetComputeVectorParam(data.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters3, data.params3);
                        ctx.cmd.SetComputeBufferParam(data.contactShadowsCS, data.kernel, HDShaderIDs._DirectionalLightDatas, data.lightLoopLightData.directionalLightData);

                        // Send light list to the compute
                        ctx.cmd.SetComputeBufferParam(data.contactShadowsCS, data.kernel, HDShaderIDs._LightDatas, data.lightLoopLightData.lightData);
                        ctx.cmd.SetComputeBufferParam(data.contactShadowsCS, data.kernel, HDShaderIDs.g_vLightListGlobal, data.lightList);

                        ctx.cmd.SetComputeTextureParam(data.contactShadowsCS, data.kernel, data.depthTextureParameterName, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.contactShadowsCS, data.kernel, HDShaderIDs._ContactShadowTextureUAV, data.contactShadowsTexture);

                        ctx.cmd.DispatchCompute(data.contactShadowsCS, data.kernel, data.numTilesX, data.numTilesY, data.viewCount);

                        if (data.rayTracingEnabled)
                        {
                            ctx.cmd.SetRayTracingShaderPass(data.contactShadowsRTS, "VisibilityDXR");
                            ctx.cmd.SetRayTracingAccelerationStructure(data.contactShadowsRTS, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                            ctx.cmd.SetRayTracingVectorParam(data.contactShadowsRTS, HDShaderIDs._ContactShadowParamsParameters, data.params1);
                            ctx.cmd.SetRayTracingVectorParam(data.contactShadowsRTS, HDShaderIDs._ContactShadowParamsParameters2, data.params2);
                            ctx.cmd.SetRayTracingBufferParam(data.contactShadowsRTS, HDShaderIDs._DirectionalLightDatas, data.lightLoopLightData.directionalLightData);

                            // Send light list to the compute
                            ctx.cmd.SetRayTracingBufferParam(data.contactShadowsRTS, HDShaderIDs._LightDatas, data.lightLoopLightData.lightData);
                            ctx.cmd.SetRayTracingBufferParam(data.contactShadowsRTS, HDShaderIDs.g_vLightListGlobal, data.lightList);

                            ctx.cmd.SetRayTracingTextureParam(data.contactShadowsRTS, HDShaderIDs._DepthTexture, data.depthTexture);
                            ctx.cmd.SetRayTracingTextureParam(data.contactShadowsRTS, HDShaderIDs._ContactShadowTextureUAV, data.contactShadowsTexture);

                            ctx.cmd.DispatchRays(data.contactShadowsRTS, "RayGenContactShadows", (uint)data.actualWidth, (uint)data.actualHeight, (uint)data.viewCount);
                        }
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

                    passData.densityBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                        { slices = s_CurrentVolumetricBufferSize.z, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferDensity" }));

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
                    //TODO: move the entire vbuffer to hardware DRS mode. When Hardware DRS is enabled we will save performance
                    // on these buffers, however the final vbuffer will be wasting resolution. This requires a bit of more work to optimize.
                    passData.parameters = PrepareGenerateMaxZParameters(hdCamera, depthMipInfo);
                    passData.depthTexture = builder.ReadTexture(depthTexture);
                    passData.maxZ8xBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.125f, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "MaxZ mask 8x" });
                    passData.maxZBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.125f, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "MaxZ mask" });
                    passData.dilatedMaxZBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one / 16.0f, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Dilated MaxZ mask" }));

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
                    passData.parameters = parameters;
                    if (passData.parameters.tiledLighting)
                        passData.bigTileLightListBuffer = builder.ReadComputeBuffer(bigTileLightListBuffer);
                    passData.densityBuffer = builder.ReadTexture(densityBuffer);
                    passData.depthTexture = builder.ReadTexture(depthTexture);
                    passData.maxZBuffer = builder.ReadTexture(maxZBuffer);
                    passData.lightingBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                        { slices = s_CurrentVolumetricBufferSize.z, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferLighting" }));

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
