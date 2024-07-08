using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct LightingBuffers
        {
            public TextureHandle sssBuffer;
            public TextureHandle diffuseLightingBuffer;

            public TextureHandle ambientOcclusionBuffer;
            public TextureHandle ssrLightingBuffer;
            public TextureHandle ssgiLightingBuffer;
            public TextureHandle contactShadowsBuffer;
            public TextureHandle screenspaceShadowBuffer;
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

        static void BindDefaultTexturesLightingBuffers(RenderGraphDefaultResources defaultResources, CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, defaultResources.blackTextureXR);
            cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, defaultResources.blackTextureXR);
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, defaultResources.blackTextureXR);
            cmd.SetGlobalTexture(HDShaderIDs._ContactShadowTexture, defaultResources.blackUIntTextureXR);
            cmd.SetGlobalTexture(HDShaderIDs._ScreenSpaceShadowsTexture, defaultResources.blackTextureXR);
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
            public LightList lightList;
            public bool canClearLightList;
            public int directionalLightCount;

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

            public TextureHandle depthBuffer;
            public TextureHandle stencilTexture;
            public TextureHandle[] gBuffer = new TextureHandle[RenderGraph.kMaxMRTCount];
            public int gBufferCount;

            // Buffers filled with the CPU outside of render graph.
            public ComputeBufferHandle convexBoundsBuffer;
            public ComputeBufferHandle AABBBoundsBuffer;

            // Transient buffers that are not used outside of BuildGPULight list so they don't need to go outside the pass.
            public ComputeBufferHandle globalLightListAtomic;
            public ComputeBufferHandle lightVolumeDataBuffer;

            public BuildGPULightListOutput output = new BuildGPULightListOutput();
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
                if (data.canClearLightList) // This can happen when we dont have a GPULight list builder and a light list instantiated.
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
                    if (data.directionalLightCount > 0)
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

                    // If we haven't run the light list building, we are missing some basic lighting flags.
                    if (!tileFlagsWritten)
                    {
                        if (data.directionalLightCount > 0)
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

                    for (int i = 0; i < data.gBufferCount; ++i)
                        cmd.SetComputeTextureParam(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._GBufferTexture[i], data.gBuffer[i]);

                    RTHandle stencilTexture = data.stencilTexture;
                    if (stencilTexture?.rt != null && stencilTexture.rt.stencilFormat != GraphicsFormat.None)
                    {
                        cmd.SetComputeTextureParam(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, data.stencilTexture, 0, RenderTextureSubElement.Stencil);
                    }
                    else // We are accessing MSAA resolved version or default black texture and not the depth stencil buffer directly.
                    {
                        cmd.SetComputeTextureParam(data.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, data.stencilTexture);
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
            RenderGraph renderGraph,
            RenderGraphBuilder builder,
            HDCamera hdCamera,
            TileAndClusterData tileAndClusterData,
            ref ShaderVariablesLightList constantBuffer,
            int totalLightCount,
            TextureHandle depthStencilBuffer,
            TextureHandle stencilBufferCopy,
            GBufferOutput gBuffer,
            BuildGPULightListPassData passData)
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
                // Note: we need to take into account the TAA jitter when indexing the light list
                proj = hdCamera.RequiresCameraJitter() ? hdCamera.GetJitteredProjectionMatrix(proj) : proj;

                m_LightListProjMatrices[viewIndex] = proj * s_FlipMatrixLHSRHS;

                var tempMatrix = temp * m_LightListProjMatrices[viewIndex];
                var invTempMatrix = tempMatrix.inverse;

                for (int i = 0; i < 16; ++i)
                {
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

                var tempMatrix = temp * m_LightListProjMatrices[viewIndex];
                var invTempMatrix = tempMatrix.inverse;

                for (int i = 0; i < 16; ++i)
                {
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
            cb._EnvLightIndexShift = (uint)m_GpuLightsBuilder.lightsCount;
            cb._DecalIndexShift = (uint)(m_GpuLightsBuilder.lightsCount + m_lightList.envLights.Count);

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
            passData.directionalLightCount = m_GpuLightsBuilder.directionalLightCount;
            passData.canClearLightList = m_GpuLightsBuilder != null && m_lightList != null;
            passData.skyEnabled = m_SkyManager.IsLightingSkyValid(hdCamera);
            passData.useComputeAsPixel = DeferredUseComputeAsPixel(hdCamera.frameSettings);

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

            // Depending on frame setting configurations we might not have written to a depth buffer yet so when executing the pass it might not be valid.
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
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
                // When opaques are disabled, gbuffer count is zero.
                // Unfortunately, compute shader will then complains some textures aren't bound, so we need to use black textures instead.
                for (int i = 0; i < gBuffer.gBufferCount; ++i)
                    passData.gBuffer[i] = builder.ReadTexture(gBuffer.mrt[i]);
                for (int i = gBuffer.gBufferCount; i < 7; ++i)
                    passData.gBuffer[i] = renderGraph.defaultResources.blackTextureXR;
                passData.gBufferCount = 7;
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

            if (tileAndClusterData.hasTileBuffers)
            {
                // note that nrTiles include the viewCount in allocation below
                // Tile buffers
                passData.output.lightList = builder.WriteComputeBuffer(
                    renderGraph.CreateComputeBuffer(new ComputeBufferDesc((int)LightCategory.Count * InternalLightCullingDefs.s_LightDwordPerFptlTile * nrTiles, sizeof(uint)) { name = "LightList" }));
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
                passData.output.bigTileLightList = builder.WriteComputeBuffer(
                    renderGraph.CreateComputeBuffer(new ComputeBufferDesc(InternalLightCullingDefs.s_MaxNrBigTileLightsPlusOne * nrBigTiles, sizeof(uint)) { name = "BigTiles" }));
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
            RenderGraph renderGraph,
            HDCamera hdCamera,
            TileAndClusterData tileAndClusterData,
            int totalLightCount,
            ref ShaderVariablesLightList constantBuffer,
            TextureHandle depthStencilBuffer,
            TextureHandle stencilBufferCopy,
            GBufferOutput gBuffer)
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
            public HDCamera hdCamera;
            public ShaderVariablesGlobal globalCB;
            public ShaderVariablesXR xrCB;
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
                colorFormat = GraphicsFormat.B10G11R11_UFloatPack32,
                enableRandomWrite = !msaa,
                bindTextureMS = msaa,
                msaaSamples = msaaSamples,
                clearBuffer = true,
                clearColor = Color.clear,
                name = msaa ? "CameraSSSDiffuseLightingMSAA" : "CameraSSSDiffuseLighting"
            });
        }

        class DeferredLightingPassData
        {
            public int numTilesX;
            public int numTilesY;
            public int numTiles;
            public bool enableTile;
            public bool outputSplitLighting;
            public bool useComputeLightingEvaluation;
            public bool enableFeatureVariants;
            public bool enableShadowMasks;
            public int numVariants;
            public DebugDisplaySettings debugDisplaySettings;

            // Compute Lighting
            public ComputeShader deferredComputeShader;
            public int viewCount;

            // Full Screen Pixel (debug)
            public Material splitLightingMat;
            public Material regularLightingMat;

            public TextureHandle colorBuffer;
            public TextureHandle sssDiffuseLightingBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle depthTexture;

            public int gbufferCount;
            public int lightLayersTextureIndex;
            public int shadowMaskTextureIndex;
            public TextureHandle[] gbuffer = new TextureHandle[8];

            public ComputeBufferHandle lightListBuffer;
            public ComputeBufferHandle tileFeatureFlagsBuffer;
            public ComputeBufferHandle tileListBuffer;
            public ComputeBufferHandle dispatchIndirectBuffer;

            public LightingBuffers lightingBuffers;
        }

        struct LightingOutput
        {
            public TextureHandle colorBuffer;
        }

        static void RenderComputeDeferredLighting(DeferredLightingPassData data, RenderTargetIdentifier[] colorBuffers, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingCompute)))
            {
                data.deferredComputeShader.shaderKeywords = null;

                switch (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowFilteringQuality)
                {
                    case HDShadowFilteringQuality.Low:
                        data.deferredComputeShader.EnableKeyword("SHADOW_LOW");
                        break;
                    case HDShadowFilteringQuality.Medium:
                        data.deferredComputeShader.EnableKeyword("SHADOW_MEDIUM");
                        break;
                    case HDShadowFilteringQuality.High:
                        data.deferredComputeShader.EnableKeyword("SHADOW_HIGH");
                        break;
                    default:
                        data.deferredComputeShader.EnableKeyword("SHADOW_MEDIUM");
                        break;
                }

                switch (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.areaShadowFilteringQuality)
                {
                    case HDAreaShadowFilteringQuality.Medium:
                        data.deferredComputeShader.EnableKeyword("AREA_SHADOW_MEDIUM");
                        break;
                    case HDAreaShadowFilteringQuality.High:
                        data.deferredComputeShader.EnableKeyword("AREA_SHADOW_HIGH");
                        break;
                    default:
                        data.deferredComputeShader.EnableKeyword("AREA_SHADOW_MEDIUM");
                        break;
                }

                if (data.enableShadowMasks)
                {
                    data.deferredComputeShader.EnableKeyword("SHADOWS_SHADOWMASK");
                }

                if (data.enableFeatureVariants)
                {
                    for (int variant = 0; variant < data.numVariants; variant++)
                    {
                        var kernel = s_shadeOpaqueIndirectFptlKernels[variant];

                        cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);

                        // TODO: Is it possible to setup this outside the loop ? Can figure out how, get this: Property (specularLightingUAV) at kernel index (21) is not set
                        cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs.specularLightingUAV, colorBuffers[0]);
                        cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs.diffuseLightingUAV, colorBuffers[1]);
                        cmd.SetComputeBufferParam(data.deferredComputeShader, kernel, HDShaderIDs.g_vLightListTile, data.lightListBuffer);

                        cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);

                        // always do deferred lighting in blocks of 16x16 (not same as tiled light size)
                        cmd.SetComputeBufferParam(data.deferredComputeShader, kernel, HDShaderIDs.g_TileFeatureFlags, data.tileFeatureFlagsBuffer);
                        cmd.SetComputeIntParam(data.deferredComputeShader, HDShaderIDs.g_TileListOffset, variant * data.numTiles * data.viewCount);
                        cmd.SetComputeBufferParam(data.deferredComputeShader, kernel, HDShaderIDs.g_TileList, data.tileListBuffer);
                        cmd.DispatchCompute(data.deferredComputeShader, kernel, data.dispatchIndirectBuffer, (uint)variant * 3 * sizeof(uint));
                    }
                }
                else
                {
                    var kernel = data.debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectFptlDebugDisplayKernel : s_shadeOpaqueDirectFptlKernel;

                    cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);

                    cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs.specularLightingUAV, colorBuffers[0]);
                    cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs.diffuseLightingUAV, colorBuffers[1]);
                    cmd.SetComputeBufferParam(data.deferredComputeShader, kernel, HDShaderIDs.g_vLightListTile, data.lightListBuffer);

                    cmd.SetComputeTextureParam(data.deferredComputeShader, kernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);

                    // 4x 8x8 groups per a 16x16 tile.
                    cmd.DispatchCompute(data.deferredComputeShader, kernel, data.numTilesX * 2, data.numTilesY * 2, data.viewCount);
                }

            }
        }

        static void RenderComputeAsPixelDeferredLighting(DeferredLightingPassData data, RenderTargetIdentifier[] colorBuffers, Material deferredMat, bool outputSplitLighting, CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, "OUTPUT_SPLIT_LIGHTING", outputSplitLighting);
            CoreUtils.SetKeyword(cmd, "SHADOWS_SHADOWMASK", data.enableShadowMasks);

            if (data.enableFeatureVariants)
            {
                if (outputSplitLighting)
                    CoreUtils.SetRenderTarget(cmd, colorBuffers, data.depthBuffer);
                else
                    CoreUtils.SetRenderTarget(cmd, colorBuffers[0], data.depthBuffer);

                for (int variant = 0; variant < data.numVariants; variant++)
                {
                    cmd.SetGlobalInt(HDShaderIDs.g_TileListOffset, variant * data.numTiles);

                    cmd.EnableShaderKeyword(s_variantNames[variant]);

                    MeshTopology topology = k_HasNativeQuadSupport ? MeshTopology.Quads : MeshTopology.Triangles;
                    cmd.DrawProceduralIndirect(Matrix4x4.identity, deferredMat, 0, topology, data.dispatchIndirectBuffer, variant * 4 * sizeof(uint), null);

                    // Must disable variant keyword because it will not get overridden.
                    cmd.DisableShaderKeyword(s_variantNames[variant]);
                }
            }
            else
            {
                CoreUtils.SetKeyword(cmd, "DEBUG_DISPLAY", data.debugDisplaySettings.IsDebugDisplayEnabled());

                if (outputSplitLighting)
                    CoreUtils.DrawFullScreen(cmd, deferredMat, colorBuffers, data.depthBuffer, null, 1);
                else
                    CoreUtils.DrawFullScreen(cmd, deferredMat, colorBuffers[0], data.depthBuffer, null, 1);
            }
        }

        static void RenderComputeAsPixelDeferredLighting(DeferredLightingPassData data, RenderTargetIdentifier[] colorBuffers, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingComputeAsPixel)))
            {
                cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, data.depthTexture);
                cmd.SetGlobalBuffer(HDShaderIDs.g_TileFeatureFlags, data.tileFeatureFlagsBuffer);
                cmd.SetGlobalBuffer(HDShaderIDs.g_TileList, data.tileListBuffer);

                // If SSS is disabled, do lighting for both split lighting and no split lighting
                // Must set stencil parameters through Material.
                if (data.outputSplitLighting)
                {
                    s_DeferredTileSplitLightingMat.SetBuffer(HDShaderIDs.g_vLightListTile, data.lightListBuffer);
                    s_DeferredTileRegularLightingMat.SetBuffer(HDShaderIDs.g_vLightListTile, data.lightListBuffer);

                    RenderComputeAsPixelDeferredLighting(data, colorBuffers, s_DeferredTileSplitLightingMat, true, cmd);
                    RenderComputeAsPixelDeferredLighting(data, colorBuffers, s_DeferredTileRegularLightingMat, false, cmd);
                }
                else
                {
                    s_DeferredTileMat.SetBuffer(HDShaderIDs.g_vLightListTile, data.lightListBuffer);
                    RenderComputeAsPixelDeferredLighting(data, colorBuffers, s_DeferredTileMat, false, cmd);
                }
            }
        }

        static void RenderPixelDeferredLighting(DeferredLightingPassData data, RenderTargetIdentifier[] colorBuffers, CommandBuffer cmd)
        {
            // First, render split lighting.
            if (data.outputSplitLighting)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingSinglePassMRT)))
                {
                    CoreUtils.DrawFullScreen(cmd, data.splitLightingMat, colorBuffers, data.depthBuffer);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingSinglePass)))
            {
                var currentLightingMaterial = data.regularLightingMat;
                // If SSS is disable, do lighting for both split lighting and no split lighting
                // This is for debug purpose, so fine to use immediate material mode here to modify render state
                if (!data.outputSplitLighting)
                {
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.Clear);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.NotEqual);
                }
                else
                {
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.RequiresDeferredLighting);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.RequiresDeferredLighting);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                }

                currentLightingMaterial.SetBuffer(HDShaderIDs.g_vLightListTile, data.lightListBuffer);

                CoreUtils.DrawFullScreen(cmd, currentLightingMaterial, colorBuffers[0], data.depthBuffer);
            }
        }

        LightingOutput RenderDeferredLighting(
            RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle depthStencilBuffer,
            TextureHandle depthPyramidTexture,
            in LightingBuffers lightingBuffers,
            in GBufferOutput gbuffer,
            in ShadowResult shadowResult,
            in BuildGPULightListOutput lightLists)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return new LightingOutput();

            using (var builder = renderGraph.AddRenderPass<DeferredLightingPassData>("Deferred Lighting", out var passData))
            {
                bool debugDisplayOrSceneLightOff = CoreUtils.IsSceneLightingDisabled(hdCamera.camera) || m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();

                int w = hdCamera.actualWidth;
                int h = hdCamera.actualHeight;
                passData.numTilesX = (w + 15) / 16;
                passData.numTilesY = (h + 15) / 16;
                passData.numTiles = passData.numTilesX * passData.numTilesY;
                passData.enableTile = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DeferredTile);
                passData.outputSplitLighting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering);
                passData.useComputeLightingEvaluation = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeLightEvaluation);
                passData.enableFeatureVariants = GetFeatureVariantsEnabled(hdCamera.frameSettings) && !debugDisplayOrSceneLightOff;
                passData.enableShadowMasks = m_EnableBakeShadowMask;
                passData.numVariants = LightDefinitions.s_NumFeatureVariants;
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;

                // Compute Lighting
                passData.deferredComputeShader = deferredComputeShader;
                passData.viewCount = hdCamera.viewCount;

                // Full Screen Pixel (debug)
                passData.splitLightingMat = GetDeferredLightingMaterial(true /*split lighting*/, passData.enableShadowMasks, debugDisplayOrSceneLightOff);
                passData.regularLightingMat = GetDeferredLightingMaterial(false /*split lighting*/, passData.enableShadowMasks, debugDisplayOrSceneLightOff);

                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                if (passData.outputSplitLighting)
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
                        var colorBuffers = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                        colorBuffers[0] = data.colorBuffer;
                        colorBuffers[1] = data.sssDiffuseLightingBuffer;

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

                        if (data.enableTile)
                        {
                            bool useCompute = data.useComputeLightingEvaluation && !k_PreferFragment;
                            if (useCompute)
                                RenderComputeDeferredLighting(data, colorBuffers, context.cmd);
                            else
                                RenderComputeAsPixelDeferredLighting(data, colorBuffers, context.cmd);
                        }
                        else
                        {
                            RenderPixelDeferredLighting(data, colorBuffers, context.cmd);
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
            public int accumulateNoWorldSpeedRejectionBothKernel;
            public int accumulateNoWorldSpeedRejectionSurfaceKernel;
            public int accumulateNoWorldSpeedRejectionHitKernel;
            public int accumulateHardThresholdSpeedRejectionBothKernel;
            public int accumulateHardThresholdSpeedRejectionSurfaceKernel;
            public int accumulateHardThresholdSpeedRejectionHitKernel;
            public int accumulateSmoothSpeedRejectionBothKernel;
            public int accumulateSmoothSpeedRejectionSurfaceKernel;
            public int accumulateSmoothSpeedRejectionHitKernel;

            public int accumulateNoWorldSpeedRejectionBothDebugKernel;
            public int accumulateNoWorldSpeedRejectionSurfaceDebugKernel;
            public int accumulateNoWorldSpeedRejectionHitDebugKernel;
            public int accumulateHardThresholdSpeedRejectionBothDebugKernel;
            public int accumulateHardThresholdSpeedRejectionSurfaceDebugKernel;
            public int accumulateHardThresholdSpeedRejectionHitDebugKernel;
            public int accumulateSmoothSpeedRejectionBothDebugKernel;
            public int accumulateSmoothSpeedRejectionSurfaceDebugKernel;
            public int accumulateSmoothSpeedRejectionHitDebugKernel;

            public bool transparentSSR;
            public bool usePBRAlgo;
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
            public TextureHandle ssrAccumPrev;
            public TextureHandle clearCoatMask;

            public ComputeBufferHandle coarseStencilBuffer;

            public BlueNoise blueNoise;
            public HDCamera hdCamera;

            public ComputeShader clearBuffer2DCS;
            public int clearBuffer2DKernel;

            public bool useAsync;

            public float frameIndex;
            public float roughnessBiasFactor;
            public float speedRejection;
            public float speedRejectionFactor;

            public bool debugDisplaySpeed;
            public bool enableWorldSmoothRejection;
            public bool smoothSpeedRejection;
            public bool motionVectorFromSurface;
            public bool motionVectorFromHit;
        }

        static void ClearColorBuffer2D(RenderSSRPassData data, CommandBuffer cmd, TextureHandle rt, Color clearColor, bool async)
        {
            if (!async)
            {
                CoreUtils.SetRenderTarget(cmd, rt, ClearFlag.Color, clearColor);
            }
            else
            {
                cmd.SetComputeTextureParam(data.clearBuffer2DCS, data.clearBuffer2DKernel, HDShaderIDs._Buffer2D, rt);
                cmd.SetComputeVectorParam(data.clearBuffer2DCS, HDShaderIDs._ClearValue, clearColor);
                cmd.SetComputeVectorParam(data.clearBuffer2DCS, HDShaderIDs._BufferSize, new Vector4((float)data.width, (float)data.height, 0.0f, 0.0f));
                cmd.DispatchCompute(data.clearBuffer2DCS, data.clearBuffer2DKernel, HDUtils.DivRoundUp(data.width, 8), HDUtils.DivRoundUp(data.height, 8), data.viewCount);
            }
        }

        void UpdateSSRConstantBuffer(HDCamera hdCamera, ScreenSpaceReflection settings, bool isTransparent, ref ShaderVariablesScreenSpaceReflection cb)
        {
            float n = hdCamera.camera.nearClipPlane;
            float f = hdCamera.camera.farClipPlane;
            float thickness = settings.depthBufferThickness.value;

            cb._SsrThicknessScale = 1.0f / (1.0f + thickness);
            cb._SsrThicknessBias = -n / (f - n) * (thickness * cb._SsrThicknessScale);
            cb._SsrIterLimit = settings.rayMaxIterations;
            // We disable sky reflection for transparent in case of a scenario where a transparent object seeing the sky through it is visible in the reflection.
            // As it has no depth it will appear extremely distorted (depth at infinity). This scenario happen frequently when you have transparent objects above water.
            // Note that the sky is still visible, it just takes its value from reflection probe/skybox rather than on screen.
            cb._SsrReflectsSky = isTransparent ? 0 : (settings.reflectSky.value ? 1 : 0);
            cb._SsrStencilBit = (int)StencilUsage.TraceReflectionRay;
            float roughnessFadeStart = 1 - settings.smoothnessFadeStart;
            cb._SsrRoughnessFadeEnd = 1 - settings.minSmoothness;
            float roughnessFadeLength = cb._SsrRoughnessFadeEnd - roughnessFadeStart;
            cb._SsrRoughnessFadeEndTimesRcpLength = (roughnessFadeLength != 0) ? (cb._SsrRoughnessFadeEnd * (1.0f / roughnessFadeLength)) : 1;
            cb._SsrRoughnessFadeRcpLength = (roughnessFadeLength != 0) ? (1.0f / roughnessFadeLength) : 0;
            cb._SsrEdgeFadeRcpLength = Mathf.Min(1.0f / settings.screenFadeDistance.value, float.MaxValue);
            cb._SsrColorPyramidMaxMip = hdCamera.colorPyramidHistoryMipCount - 1;
            cb._SsrDepthPyramidMaxMip = hdCamera.depthBufferMipChainInfo.mipLevelCount - 1;
            if (hdCamera.isFirstFrame || hdCamera.cameraFrameCount <= 3)
            {
                cb._SsrAccumulationAmount = 1.0f;
            }
            else
            {
                cb._SsrAccumulationAmount = Mathf.Pow(2, Mathf.Lerp(0.0f, -7.0f, settings.accumulationFactor.value));
            }

            if (settings.enableWorldSpeedRejection.value && !settings.speedSmoothReject.value)
                cb._SsrPBRSpeedRejection = Mathf.Clamp01(1.0f - settings.speedRejectionParam.value);
            else
                cb._SsrPBRSpeedRejection = Mathf.Clamp01(settings.speedRejectionParam.value);
            cb._SsrPBRBias = settings.biasFactor.value;
            cb._SsrPRBSpeedRejectionScalerFactor = Mathf.Pow(settings.speedRejectionScalerFactor.value * 0.1f, 2.0f);
        }

        TextureHandle RenderSSR(RenderGraph renderGraph,
            HDCamera hdCamera,
            ref PrepassOutput prepassOutput,
            TextureHandle clearCoatMask,
            TextureHandle rayCountTexture,
            Texture skyTexture,
            bool transparent)
        {
            if (!hdCamera.IsSSREnabled(transparent))
                return renderGraph.defaultResources.blackTextureXR;

            TextureHandle result;

            bool debugDisplaySpeed = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflectionSpeedRejection;

            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // We can use the ray tracing version of the effect if:
            // - It is enabled in the frame settings
            // - It is enabled in the volume
            // - The RTAS has been build validated
            // - The RTLightCluster has been validated
            bool usesRaytracedReflections = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                                            && ScreenSpaceReflection.RayTracingActive(settings)
                                            && GetRayTracingState() && GetRayTracingClusterState();
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

                // The first color pyramid of the frame is generated after the SSR transparent, so we have no choice but to use the previous
                // frame color pyramid (that includes transparents from the previous frame).
                RTHandle colorPyramidRT = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                if (colorPyramidRT == null)
                    return renderGraph.defaultResources.blackTextureXR;

                using (var builder = renderGraph.AddRenderPass<RenderSSRPassData>("Render SSR", out var passData))
                {
                    // We disable async for transparent SSR as it would cause direct sync to the graphics pipe and would compete with other heavy passes for GPU resource.
                    bool useAsync = hdCamera.frameSettings.SSRRunsAsync() && !transparent;
                    builder.EnableAsyncCompute(useAsync);

                    bool usePBRAlgo = !transparent && settings.usedAlgorithm.value == ScreenSpaceReflectionAlgorithm.PBRAccumulation;
                    var colorPyramid = renderGraph.ImportTexture(colorPyramidRT);
                    var volumeSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

                    hdCamera.AllocateScreenSpaceAccumulationHistoryBuffer(1.0f);

                    UpdateSSRConstantBuffer(hdCamera, volumeSettings, transparent, ref passData.cb);

                    passData.hdCamera = hdCamera;
                    passData.blueNoise = GetBlueNoiseManager();
                    passData.ssrCS = m_ScreenSpaceReflectionsCS;
                    passData.tracingKernel = m_SsrTracingKernel;
                    passData.reprojectionKernel = m_SsrReprojectionKernel;
                    passData.accumulateNoWorldSpeedRejectionBothKernel = m_SsrAccumulateNoWorldSpeedRejectionBothKernel;
                    passData.accumulateNoWorldSpeedRejectionSurfaceKernel = m_SsrAccumulateNoWorldSpeedRejectionSurfaceKernel;
                    passData.accumulateNoWorldSpeedRejectionHitKernel = m_SsrAccumulateNoWorldSpeedRejectionHitKernel;
                    passData.accumulateHardThresholdSpeedRejectionBothKernel = m_SsrAccumulateHardThresholdSpeedRejectionBothKernel;
                    passData.accumulateHardThresholdSpeedRejectionSurfaceKernel = m_SsrAccumulateHardThresholdSpeedRejectionSurfaceKernel;
                    passData.accumulateHardThresholdSpeedRejectionHitKernel = m_SsrAccumulateHardThresholdSpeedRejectionHitKernel;
                    passData.accumulateSmoothSpeedRejectionBothKernel = m_SsrAccumulateSmoothSpeedRejectionBothKernel;
                    passData.accumulateSmoothSpeedRejectionSurfaceKernel = m_SsrAccumulateSmoothSpeedRejectionSurfaceKernel;
                    passData.accumulateSmoothSpeedRejectionHitKernel = m_SsrAccumulateSmoothSpeedRejectionHitKernel;

                    passData.accumulateNoWorldSpeedRejectionBothDebugKernel = m_SsrAccumulateNoWorldSpeedRejectionBothDebugKernel;
                    passData.accumulateNoWorldSpeedRejectionSurfaceDebugKernel = m_SsrAccumulateNoWorldSpeedRejectionSurfaceDebugKernel;
                    passData.accumulateNoWorldSpeedRejectionHitDebugKernel = m_SsrAccumulateNoWorldSpeedRejectionHitDebugKernel;
                    passData.accumulateHardThresholdSpeedRejectionBothDebugKernel = m_SsrAccumulateHardThresholdSpeedRejectionBothDebugKernel;
                    passData.accumulateHardThresholdSpeedRejectionSurfaceDebugKernel = m_SsrAccumulateHardThresholdSpeedRejectionSurfaceDebugKernel;
                    passData.accumulateHardThresholdSpeedRejectionHitDebugKernel = m_SsrAccumulateHardThresholdSpeedRejectionHitDebugKernel;
                    passData.accumulateSmoothSpeedRejectionBothDebugKernel = m_SsrAccumulateSmoothSpeedRejectionBothDebugKernel;
                    passData.accumulateSmoothSpeedRejectionSurfaceDebugKernel = m_SsrAccumulateSmoothSpeedRejectionSurfaceDebugKernel;
                    passData.accumulateSmoothSpeedRejectionHitDebugKernel = m_SsrAccumulateSmoothSpeedRejectionHitDebugKernel;

                    passData.transparentSSR = transparent;
                    passData.usePBRAlgo = usePBRAlgo;
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;
                    passData.viewCount = hdCamera.viewCount;
                    passData.offsetBufferData = hdCamera.depthBufferMipChainInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);
                    passData.previousAccumNeedClear = usePBRAlgo && (hdCamera.isFirstFrame || hdCamera.resetPostProcessingHistory);
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
                    if (hdCamera.isFirstFrame || hdCamera.cameraFrameCount <= 2)
                    {
                        passData.frameIndex = 1.0f;
                    }
                    else
                    {
                        passData.frameIndex = ((float)hdCamera.cameraFrameCount);
                    }
                    passData.roughnessBiasFactor = volumeSettings.biasFactor.value;
                    passData.debugDisplaySpeed = debugDisplaySpeed;
                    passData.speedRejection = volumeSettings.speedRejectionParam.value;
                    passData.speedRejectionFactor = volumeSettings.speedRejectionScalerFactor.value;
                    passData.enableWorldSmoothRejection = volumeSettings.enableWorldSpeedRejection.value;
                    passData.smoothSpeedRejection = volumeSettings.speedSmoothReject.value;
                    passData.motionVectorFromSurface = volumeSettings.speedSurfaceOnly.value;
                    passData.motionVectorFromHit = volumeSettings.speedTargetOnly.value;

                    passData.clearBuffer2DCS = m_ClearBuffer2DCS;
                    passData.clearBuffer2DKernel = m_ClearBuffer2DKernel;
                    passData.useAsync = useAsync;

                    // In practice, these textures are sparse (mostly black). Therefore, clearing them is fast (due to CMASK),
                    // and much faster than fully overwriting them from within SSR shaders.
                    passData.hitPointsTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16_UNorm, clearBuffer = !useAsync, clearColor = Color.clear, enableRandomWrite = true, name = transparent ? "SSR_Hit_Point_Texture_Trans" : "SSR_Hit_Point_Texture" });

                    if (usePBRAlgo)
                    {
                        passData.ssrAccum = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation)));
                        passData.ssrAccumPrev = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation)));
                    }
                    else
                    {
                        passData.ssrAccum = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = !useAsync, clearColor = Color.clear, enableRandomWrite = true, name = "SSR_Lighting_Texture" }));
                    }

                    builder.SetRenderFunc(
                        (RenderSSRPassData data, RenderGraphContext ctx) =>
                        {
                            var cs = data.ssrCS;

                            CoreUtils.SetKeyword(ctx.cmd, "SSR_APPROX", !data.usePBRAlgo);
                            CoreUtils.SetKeyword(ctx.cmd, "DEPTH_SOURCE_NOT_FROM_MIP_CHAIN", data.transparentSSR);

                            if (data.usePBRAlgo)
                            {
                                ClearColorBuffer2D(data, ctx.cmd, data.ssrAccum, Color.clear, data.useAsync);

                                if (data.previousAccumNeedClear || data.debugDisplaySpeed)
                                    ClearColorBuffer2D(data, ctx.cmd, data.ssrAccumPrev, Color.clear, data.useAsync);
                            }
                            else if (data.useAsync)
                            {
                                // If the pass is synchronous, clear is done when the accumulation texture is created
                                ClearColorBuffer2D(data, ctx.cmd, data.ssrAccum, Color.clear, data.useAsync);
                            }

                            if (data.useAsync)
                            {
                                // If the pass is synchronous, clear is done when the hit point texture is created
                                ClearColorBuffer2D(data, ctx.cmd, data.hitPointsTexture, Color.clear, data.useAsync);
                            }

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

                                ctx.cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrPBRBias, data.roughnessBiasFactor);

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
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._SSRAccumTexture, data.ssrAccum);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMask);
                                ctx.cmd.SetComputeTextureParam(cs, data.reprojectionKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);

                                ConstantBuffer.Push(ctx.cmd, data.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                                ctx.cmd.DispatchCompute(cs, data.reprojectionKernel, HDUtils.DivRoundUp(data.width, 8), HDUtils.DivRoundUp(data.height, 8), data.viewCount);
                            }

                            if (data.usePBRAlgo)
                            {
                                if (!data.validColorPyramid)
                                {
                                    ClearColorBuffer2D(data, ctx.cmd, data.ssrAccum, Color.clear, data.useAsync);
                                    ClearColorBuffer2D(data, ctx.cmd, data.ssrAccumPrev, Color.clear, data.useAsync);
                                }
                                else
                                {
                                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.SsrAccumulate)))
                                    {
                                        int pass;
                                        if (data.debugDisplaySpeed)
                                        {
                                            if (!data.enableWorldSmoothRejection)
                                            {
                                                if (data.motionVectorFromSurface && data.motionVectorFromHit)
                                                    pass = data.accumulateNoWorldSpeedRejectionBothDebugKernel;
                                                else if (data.motionVectorFromHit)
                                                    pass = data.accumulateNoWorldSpeedRejectionHitDebugKernel;
                                                else
                                                    pass = data.accumulateNoWorldSpeedRejectionSurfaceDebugKernel;
                                            }
                                            else
                                            {
                                                if (data.smoothSpeedRejection)
                                                {
                                                    if (data.motionVectorFromSurface && data.motionVectorFromHit)
                                                        pass = data.accumulateSmoothSpeedRejectionBothDebugKernel;
                                                    else if (data.motionVectorFromHit)
                                                        pass = data.accumulateSmoothSpeedRejectionHitDebugKernel;
                                                    else
                                                        pass = data.accumulateSmoothSpeedRejectionSurfaceDebugKernel;
                                                }
                                                else
                                                {
                                                    if (data.motionVectorFromSurface && data.motionVectorFromHit)
                                                        pass = data.accumulateHardThresholdSpeedRejectionBothDebugKernel;
                                                    else if (data.motionVectorFromHit)
                                                        pass = data.accumulateHardThresholdSpeedRejectionHitDebugKernel;
                                                    else
                                                        pass = data.accumulateHardThresholdSpeedRejectionSurfaceDebugKernel;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!data.enableWorldSmoothRejection)
                                            {
                                                if (data.motionVectorFromSurface && data.motionVectorFromHit)
                                                    pass = data.accumulateNoWorldSpeedRejectionBothKernel;
                                                else if (data.motionVectorFromHit)
                                                    pass = data.accumulateNoWorldSpeedRejectionHitKernel;
                                                else
                                                    pass = data.accumulateNoWorldSpeedRejectionSurfaceKernel;
                                            }
                                            else
                                            {
                                                if (data.smoothSpeedRejection)
                                                {
                                                    if (data.motionVectorFromSurface && data.motionVectorFromHit)
                                                        pass = data.accumulateSmoothSpeedRejectionBothKernel;
                                                    else if (data.motionVectorFromHit)
                                                        pass = data.accumulateSmoothSpeedRejectionHitKernel;
                                                    else
                                                        pass = data.accumulateSmoothSpeedRejectionSurfaceKernel;
                                                }
                                                else
                                                {
                                                    if (data.motionVectorFromSurface && data.motionVectorFromHit)
                                                        pass = data.accumulateHardThresholdSpeedRejectionBothKernel;
                                                    else if (data.motionVectorFromHit)
                                                        pass = data.accumulateHardThresholdSpeedRejectionHitKernel;
                                                    else
                                                        pass = data.accumulateHardThresholdSpeedRejectionSurfaceKernel;
                                                }
                                            }
                                        }

                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._DepthTexture, data.depthBuffer);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._SsrHitPointTexture, data.hitPointsTexture);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._SSRAccumTexture, data.ssrAccum);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._SsrAccumPrev, data.ssrAccumPrev);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMask);
                                        ctx.cmd.SetComputeTextureParam(cs, pass, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);
                                        ctx.cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrFrameIndex, data.frameIndex);
                                        ctx.cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrPBRSpeedRejection, data.speedRejection);
                                        ctx.cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrPRBSpeedRejectionScalerFactor, data.speedRejectionFactor);

                                        ConstantBuffer.Push(ctx.cmd, data.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                                        ctx.cmd.DispatchCompute(cs, pass, HDUtils.DivRoundUp(data.width, 8), HDUtils.DivRoundUp(data.height, 8), data.viewCount);
                                    }
                                }
                            }
                        });

                    if (usePBRAlgo)
                    {
                        PushFullScreenDebugTexture(renderGraph, passData.ssrAccum, FullScreenDebugMode.ScreenSpaceReflectionsAccum);
                        PushFullScreenDebugTexture(renderGraph, passData.ssrAccumPrev, FullScreenDebugMode.ScreenSpaceReflectionsPrev);
                    }

                    PushFullScreenDebugTexture(renderGraph, passData.ssrAccum, FullScreenDebugMode.ScreenSpaceReflectionSpeedRejection);
                    
                    result = passData.ssrAccum;
                }

                if (!hdCamera.colorPyramidHistoryIsValid)
                {
                    result = renderGraph.defaultResources.blackTextureXR;
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

            public LightLoopLightData lightLoopLightData;
            public TextureHandle depthTexture;
            public TextureHandle contactShadowsTexture;
            public ComputeBufferHandle lightList;
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

                passData.rayTracingEnabled = RayTracedContactShadowsRequired() && GetRayTracingState();
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                {
                    passData.contactShadowsRTS = m_GlobalSettings.renderPipelineRayTracingResources.contactShadowRayTracingRT;
                    passData.accelerationStructure = RequestAccelerationStructure(hdCamera);

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
                        ctx.cmd.SetComputeBufferParam(data.contactShadowsCS, data.kernel, HDShaderIDs.g_vLightListTile, data.lightList);

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
                            ctx.cmd.SetRayTracingBufferParam(data.contactShadowsRTS, HDShaderIDs.g_vLightListTile, data.lightList);

                            ctx.cmd.SetRayTracingTextureParam(data.contactShadowsRTS, HDShaderIDs._DepthTexture, data.depthTexture);
                            ctx.cmd.SetRayTracingTextureParam(data.contactShadowsRTS, HDShaderIDs._ContactShadowTextureUAV, data.contactShadowsTexture);

                            ctx.cmd.DispatchRays(data.contactShadowsRTS, "RayGenContactShadows", (uint)data.actualWidth, (uint)data.actualHeight, (uint)data.viewCount);
                        }
                    });
            }

            PushFullScreenDebugTexture(renderGraph, result, FullScreenDebugMode.ContactShadows);
            return result;
        }
    }
}
