using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        Material m_DepthResolveMaterial;
        // Need to cache to avoid alloc of arrays...
        GBufferOutput m_GBufferOutput;
        DBufferOutput m_DBufferOutput;

        HDUtils.PackedMipChainInfo m_DepthBufferMipChainInfo;

        Vector2Int ComputeDepthBufferMipChainSize(Vector2Int screenSize)
        {
            m_DepthBufferMipChainInfo.ComputePackedMipChainInfo(screenSize);
            return m_DepthBufferMipChainInfo.textureSize;
        }

        // Avoid GCAlloc by capturing functor...
        TextureDesc m_DepthPyramidDesc;

        void InitializePrepass(HDRenderPipelineAsset hdAsset)
        {
            m_DepthResolveMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.depthValuesPS);

            m_GBufferOutput = new GBufferOutput();
            m_GBufferOutput.mrt = new TextureHandle[RenderGraph.kMaxMRTCount];

            m_DBufferOutput = new DBufferOutput();
            m_DBufferOutput.mrt = new TextureHandle[(int)Decal.DBufferMaterial.Count];

            m_DepthBufferMipChainInfo = new HDUtils.PackedMipChainInfo();
            m_DepthBufferMipChainInfo.Allocate();

            m_DepthPyramidDesc = new TextureDesc(ComputeDepthBufferMipChainSize, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "CameraDepthBufferMipChain" };

        }

        void CleanupPrepass()
        {
            CoreUtils.Destroy(m_DepthResolveMaterial);
        }

        bool NeedClearGBuffer()
        {
            // TODO: Add an option to force clear
            return m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();
        }

        HDUtils.PackedMipChainInfo GetDepthBufferMipChainInfo()
        {
            return m_DepthBufferMipChainInfo;
        }

        struct PrepassOutput
        {
            // Buffers that may be output by the prepass.
            // They will be MSAA depending on the frame settings
            public TextureHandle        depthBuffer;
            public TextureHandle        depthAsColor;
            public TextureHandle        normalBuffer;
            public TextureHandle        motionVectorsBuffer;

            // GBuffer output. Will also contain a reference to the normal buffer (as it is shared between deferred and forward objects)
            public GBufferOutput        gbuffer;

            public DBufferOutput        dbuffer;

            // Additional buffers only for MSAA
            public TextureHandle        depthValuesMSAA;

            // Resolved buffers for MSAA. When MSAA is off, they will be the same reference as the buffers above.
            public TextureHandle        resolvedDepthBuffer;
            public TextureHandle        resolvedNormalBuffer;
            public TextureHandle        resolvedMotionVectorsBuffer;

            // Copy of the resolved depth buffer with mip chain
            public TextureHandle        depthPyramidTexture;
            // Depth buffer used for low res transparents.
            public TextureHandle        downsampledDepthBuffer;

            public TextureHandle        stencilBuffer;
            public ComputeBufferHandle  coarseStencilBuffer;

            public TextureHandle        flagMaskBuffer;
        }

        TextureHandle CreateDepthBuffer(RenderGraph renderGraph, bool clear, bool msaa)
        {
#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc fastMemDesc;
            fastMemDesc.inFastMemory = true;
            fastMemDesc.residencyFraction = 1.0f;
            fastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            TextureDesc depthDesc = new TextureDesc(Vector2.one, true, true)
                { depthBufferBits = DepthBits.Depth32, bindTextureMS = msaa, enableMSAA = msaa, clearBuffer = clear, name = msaa ? "CameraDepthStencilMSAA" : "CameraDepthStencil"
#if UNITY_2020_2_OR_NEWER
                , fastMemoryDesc = fastMemDesc
#endif
            };

            return renderGraph.CreateTexture(depthDesc);
        }

        TextureHandle CreateNormalBuffer(RenderGraph renderGraph, bool msaa)
        {
#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc fastMemDesc;
            fastMemDesc.inFastMemory = true;
            fastMemDesc.residencyFraction = 1.0f;
            fastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            TextureDesc normalDesc = new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = NeedClearGBuffer(), clearColor = Color.black, bindTextureMS = msaa, enableMSAA = msaa, enableRandomWrite = !msaa, name = msaa ? "NormalBufferMSAA" : "NormalBuffer"
#if UNITY_2020_2_OR_NEWER
                , fastMemoryDesc = fastMemDesc
#endif
            };
            return renderGraph.CreateTexture(normalDesc);
        }

        TextureHandle CreateDecalPrepassBuffer(RenderGraph renderGraph, bool msaa)
        {
            TextureDesc decalDesc = new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = true, clearColor = Color.clear, bindTextureMS = false, enableMSAA = msaa, enableRandomWrite = !msaa, name = msaa ? "DecalPrepassBufferMSAA" : "DecalPrepassBuffer" };
            return renderGraph.CreateTexture(decalDesc);
        }

        TextureHandle CreateMotionVectorBuffer(RenderGraph renderGraph, bool msaa, bool clear)
        {
            TextureDesc motionVectorDesc = new TextureDesc(Vector2.one, true, true)
                { colorFormat = Builtin.GetMotionVectorFormat(), bindTextureMS = msaa, enableMSAA = msaa, clearBuffer = clear, clearColor = Color.clear, name = msaa ? "Motion Vectors MSAA" : "Motion Vectors" };
            return renderGraph.CreateTexture(motionVectorDesc);
        }

        void BindPrepassColorBuffers(in RenderGraphBuilder builder, in PrepassOutput prepassOutput, HDCamera hdCamera)
        {
            int index = 0;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                builder.UseColorBuffer(prepassOutput.depthAsColor, index++);
            }
            builder.UseColorBuffer(prepassOutput.normalBuffer, index++);
        }

        void BindMotionVectorPassColorBuffers(in RenderGraphBuilder builder, in PrepassOutput prepassOutput, TextureHandle decalBuffer, HDCamera hdCamera)
        {
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            bool decalLayerEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers);

            if (msaa)
            {
                builder.UseColorBuffer(prepassOutput.depthAsColor, 0);
                builder.UseColorBuffer(prepassOutput.motionVectorsBuffer, 1);
                if (decalLayerEnabled)
                    builder.UseColorBuffer(decalBuffer, 2);
                builder.UseColorBuffer(prepassOutput.normalBuffer, decalLayerEnabled ? 3 : 2);
            }
            else
            {
                builder.UseColorBuffer(prepassOutput.motionVectorsBuffer, 0);
                if (decalLayerEnabled)
                    builder.UseColorBuffer(decalBuffer, 1);
                builder.UseColorBuffer(prepassOutput.normalBuffer, decalLayerEnabled ? 2 : 1);
            }
        }

        PrepassOutput RenderPrepass(RenderGraph     renderGraph,
                                    TextureHandle   colorBuffer,
                                    TextureHandle   sssBuffer,
                                    TextureHandle   vtFeedbackBuffer,
                                    CullingResults  cullingResults,
                                    CullingResults  customPassCullingResults,
                                    HDCamera        hdCamera,
                                    AOVRequestData  aovRequest,
                                    List<RTHandle>  aovBuffers)
        {
            m_IsDepthBufferCopyValid = false;

            var result = new PrepassOutput();
            result.gbuffer = m_GBufferOutput;
            result.dbuffer = m_DBufferOutput;

            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            bool clearMotionVectors = hdCamera.camera.cameraType == CameraType.SceneView && !hdCamera.animateMaterials;
            bool motionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

            // TODO: See how to clean this. Some buffers are created outside, some inside functions...
            result.motionVectorsBuffer = motionVectors ? CreateMotionVectorBuffer(renderGraph, msaa, clearMotionVectors) : renderGraph.defaultResources.blackTextureXR;
            result.depthBuffer = CreateDepthBuffer(renderGraph, hdCamera.clearDepth, msaa);
            result.stencilBuffer = result.depthBuffer;
            result.flagMaskBuffer = CreateFlagMaskTexture(renderGraph);

            RenderXROcclusionMeshes(renderGraph, hdCamera, colorBuffer, result.depthBuffer);

            using (new XRSinglePassScope(renderGraph, hdCamera))
            {
                // Bind the custom color/depth before the first custom pass
                BindCustomPassBuffers(renderGraph, hdCamera);

                RenderCustomPass(renderGraph, hdCamera, colorBuffer, result, customPassCullingResults, CustomPassInjectionPoint.BeforeRendering, aovRequest, aovBuffers);

                RenderRayTracingPrepass(renderGraph, cullingResults, hdCamera, result.flagMaskBuffer, result.depthBuffer, false);

                // When evaluating probe volumes in material pass, we build a custom probe volume light list.
                // When evaluating probe volumes in light loop, probe volumes are folded into the standard light loop data.
                // Build probe volumes light list async during depth prepass.
                // TODO: (Nick): Take a look carefully at data dependancies - could this be moved even earlier? Directly after PrepareVisibleProbeVolumeList?
                // The probe volume light lists do not depend on any of the framebuffer RTs being cleared - do they depend on anything in PushGlobalParams()?
                // Do they depend on hdCamera.xr.StartSinglePass()?
                BuildGPULightListOutput probeVolumeListOutput = new BuildGPULightListOutput();
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) && ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.MaterialPass)
                {
                    probeVolumeListOutput = BuildGPULightList(m_RenderGraph, hdCamera, m_ProbeVolumeClusterData, m_ProbeVolumeCount, ref m_ShaderVariablesProbeVolumeLightListCB, result.depthBuffer, result.stencilBuffer, result.gbuffer);
                }

                bool shouldRenderMotionVectorAfterGBuffer = RenderDepthPrepass(renderGraph, cullingResults, hdCamera, ref result, out var decalBuffer);

                if (!shouldRenderMotionVectorAfterGBuffer)
                {
                    // If objects motion vectors are enabled, this will render the objects with motion vector into the target buffers (in addition to the depth)
                    // Note: An object with motion vector must not be render in the prepass otherwise we can have motion vector write that should have been rejected
                    RenderObjectsMotionVectors(renderGraph, cullingResults, hdCamera, decalBuffer, result);
                }

                // If we have MSAA, we need to complete the motion vector buffer before buffer resolves, hence we need to run camera mv first.
                // This is always fine since shouldRenderMotionVectorAfterGBuffer is always false for forward.
                bool needCameraMVBeforeResolve = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
                if (needCameraMVBeforeResolve)
                {
                    RenderCameraMotionVectors(renderGraph, hdCamera, result.depthBuffer, result.motionVectorsBuffer);
                }

                PreRenderSky(renderGraph, hdCamera, colorBuffer, result.depthBuffer, result.normalBuffer);

                // At this point in forward all objects have been rendered to the prepass (depth/normal/motion vectors) so we can resolve them
                ResolvePrepassBuffers(renderGraph, hdCamera, ref result);

                RenderDBuffer(renderGraph, hdCamera, decalBuffer, ref result, cullingResults);

                RenderGBuffer(renderGraph, sssBuffer, vtFeedbackBuffer, ref result, probeVolumeListOutput, cullingResults, hdCamera);

                DecalNormalPatch(renderGraph, hdCamera, ref result);

                // After Depth and Normals/roughness including decals
                bool depthBufferModified = RenderCustomPass(renderGraph, hdCamera, colorBuffer, result, customPassCullingResults, CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, aovRequest, aovBuffers);

                // If the depth was already copied in RenderDBuffer, we force the copy again because the custom pass modified the depth.
                if (depthBufferModified)
                    m_IsDepthBufferCopyValid = false;

                // Only on consoles is safe to read and write from/to the depth atlas
                bool mip1FromDownsampleForLowResTrans = SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation4 ||
                                                        SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOne ||
                                                        SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12;
                mip1FromDownsampleForLowResTrans = mip1FromDownsampleForLowResTrans && hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent);

                DownsampleDepthForLowResTransparency(renderGraph, hdCamera, mip1FromDownsampleForLowResTrans, ref result);

                // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
                GenerateDepthPyramid(renderGraph, hdCamera, mip1FromDownsampleForLowResTrans, ref result);

                if (shouldRenderMotionVectorAfterGBuffer)
                {
                    // See the call RenderObjectsMotionVectors() above and comment
                    RenderObjectsMotionVectors(renderGraph, cullingResults, hdCamera, decalBuffer, result);
                }

                // In case we don't have MSAA, we always run camera motion vectors when is safe to assume Object MV are rendered
                if (!needCameraMVBeforeResolve)
                {
                    RenderCameraMotionVectors(renderGraph, hdCamera, result.depthBuffer, result.resolvedMotionVectorsBuffer);
                }

                // NOTE: Currently we profiled that generating the HTile for SSR and using it is not worth it the optimization.
                // However if the generated HTile will be used for something else but SSR, this should be made NOT resolve only and
                // re-enabled in the shader.
                BuildCoarseStencilAndResolveIfNeeded(renderGraph, hdCamera, resolveOnly: true, ref result);

                RenderTransparencyOverdraw(renderGraph, result.depthBuffer, cullingResults, hdCamera);
            }

            return result;
        }

        class DepthPrepassData
        {
            public FrameSettings        frameSettings;
            public bool                 msaaEnabled;
            public bool                 decalLayersEnabled;
            public bool                 hasDepthDeferredPass;
            public TextureHandle        depthBuffer;
            public TextureHandle        depthAsColorBuffer;
            public TextureHandle        normalBuffer;
            public TextureHandle        decalBuffer;

            public RendererListHandle   rendererListDepthForward;
            public RendererListHandle   rendererListDepthDeferred;
        }

        // RenderDepthPrepass render both opaque and opaque alpha tested based on engine configuration.
        // Lit Forward only: We always render all materials
        // Lit Deferred: We always render depth prepass for alpha tested (optimization), other deferred material are render based on engine configuration.
        // Forward opaque with deferred renderer (DepthForwardOnly pass): We always render all materials
        // True is returned if motion vector must be rendered after GBuffer pass
        bool RenderDepthPrepass(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, ref PrepassOutput output, out TextureHandle decalBuffer)
        {
            var depthPrepassParameters = PrepareDepthPrepass(cull, hdCamera);

            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            bool decalLayersEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers);

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                decalBuffer = renderGraph.defaultResources.blackTextureXR;
                output.depthAsColor = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, bindTextureMS = true, enableMSAA = true, name = "DepthAsColorMSAA" });
                output.normalBuffer = CreateNormalBuffer(renderGraph, msaa);
                return false;
            }

            using (var builder = renderGraph.AddRenderPass<DepthPrepassData>(depthPrepassParameters.passName, out var passData, ProfilingSampler.Get(depthPrepassParameters.profilingId)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.msaaEnabled = msaa;
                passData.decalLayersEnabled = decalLayersEnabled;
                passData.hasDepthDeferredPass = depthPrepassParameters.hasDepthDeferredPass;

                passData.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                passData.normalBuffer = builder.WriteTexture(CreateNormalBuffer(renderGraph, msaa));
                if (decalLayersEnabled)
                    passData.decalBuffer = builder.WriteTexture(CreateDecalPrepassBuffer(renderGraph, msaa));
                // This texture must be used because reading directly from an MSAA Depth buffer is way to expensive.
                // The solution that we went for is writing the depth in an additional color buffer (10x cheaper to solve on ps4)
                if (msaa)
                {
                    passData.depthAsColorBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, bindTextureMS = true, enableMSAA = true, name = "DepthAsColorMSAA" }));
                }

                if (passData.hasDepthDeferredPass)
                {
                    passData.rendererListDepthDeferred = builder.UseRendererList(renderGraph.CreateRendererList(depthPrepassParameters.depthDeferredRendererListDesc));
                }

                passData.rendererListDepthForward = builder.UseRendererList(renderGraph.CreateRendererList(depthPrepassParameters.depthForwardRendererListDesc));

                output.depthBuffer = passData.depthBuffer;
                output.depthAsColor = passData.depthAsColorBuffer;
                output.normalBuffer = passData.normalBuffer;
                if (decalLayersEnabled)
                    decalBuffer = passData.decalBuffer;
                else
                    decalBuffer = renderGraph.defaultResources.blackTextureXR;

                builder.SetRenderFunc(
                (DepthPrepassData data, RenderGraphContext context) =>
                {
                    RenderTargetIdentifier[] deferredMrt = null;
                    if (data.hasDepthDeferredPass && data.decalLayersEnabled)
                    {
                        deferredMrt = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(1);
                        deferredMrt[0] = data.decalBuffer;
                    }

                    var forwardMrt = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>((data.msaaEnabled ? 2 : 1) + (data.decalLayersEnabled ? 1 : 0));
                    if (data.msaaEnabled)
                    {
                        forwardMrt[0] = data.depthAsColorBuffer;
                        forwardMrt[1] = data.normalBuffer;
                        if (data.decalLayersEnabled)
                            forwardMrt[2] = data.decalBuffer;
                    }
                    else
                    {
                        forwardMrt[0] = data.normalBuffer;
                        if (data.decalLayersEnabled)
                            forwardMrt[1] = data.decalBuffer;
                    }

                    RenderDepthPrepass(context.renderContext, context.cmd, data.frameSettings
                                    , deferredMrt
                                    , forwardMrt
                                    , data.depthBuffer
                                    , data.rendererListDepthDeferred
                                    , data.rendererListDepthForward
                                    , data.hasDepthDeferredPass
                                    );
                });
            }

            return depthPrepassParameters.shouldRenderMotionVectorAfterGBuffer;
        }

        class ObjectMotionVectorsPassData
        {
            public FrameSettings        frameSettings;
            public TextureHandle        depthBuffer;
            public RendererListHandle   rendererList;
        }

        void RenderObjectsMotionVectors(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, TextureHandle decalBuffer, in PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors) ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return;

            using (var builder = renderGraph.AddRenderPass<ObjectMotionVectorsPassData>("Objects Motion Vectors Rendering", out var passData, ProfilingSampler.Get(HDProfileId.ObjectsMotionVector)))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                passData.frameSettings = hdCamera.frameSettings;
                passData.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                BindMotionVectorPassColorBuffers(builder, output, decalBuffer, hdCamera);

                RenderStateBlock? stateBlock = null;
                if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.AlphaToMask))
                    stateBlock = m_AlphaToMaskBlock;
                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_MotionVectorsName, PerObjectData.MotionVectors, stateBlock: stateBlock)));

                builder.SetRenderFunc(
                (ObjectMotionVectorsPassData data, RenderGraphContext context) =>
                {
                    // Disable write to normal buffer for unlit shader (the normal buffer binding change when using MSAA)
                    context.cmd.SetGlobalInt(HDShaderIDs._ColorMaskNormal, data.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? (int)ColorWriteMask.All : 0);

                    DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                });
            }
        }

        class GBufferPassData
        {
            public FrameSettings        frameSettings;
            public RendererListHandle   rendererList;
            public TextureHandle[]      gbufferRT = new TextureHandle[RenderGraph.kMaxMRTCount];
            public TextureHandle        depthBuffer;
            public DBufferOutput        dBuffer;
            public bool                 needProbeVolumeLightLists;
            public ComputeBufferHandle  probeVolumeBigTile;
            public ComputeBufferHandle  probeVolumePerVoxelOffset;
            public ComputeBufferHandle  probeVolumePerVoxelLightList;
        }

        struct GBufferOutput
        {
            public TextureHandle[] mrt;
            public int gBufferCount;
            public int lightLayersTextureIndex;
            public int shadowMaskTextureIndex;
        }

        void SetupGBufferTargets(RenderGraph renderGraph, HDCamera hdCamera, GBufferPassData passData, TextureHandle sssBuffer, TextureHandle vtFeedbackBuffer, ref PrepassOutput prepassOutput, FrameSettings frameSettings, RenderGraphBuilder builder)
        {
            bool clearGBuffer = NeedClearGBuffer();
            bool lightLayers = frameSettings.IsEnabled(FrameSettingsField.LightLayers);
            bool shadowMasks = frameSettings.IsEnabled(FrameSettingsField.Shadowmask);

            int currentIndex = 0;
            passData.depthBuffer = builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);
            passData.gbufferRT[currentIndex++] = builder.UseColorBuffer(sssBuffer, 0);
            passData.gbufferRT[currentIndex++] = builder.UseColorBuffer(prepassOutput.normalBuffer, 1);

#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc gbufferFastMemDesc;
            gbufferFastMemDesc.inFastMemory = true;
            gbufferFastMemDesc.residencyFraction = 1.0f;
            gbufferFastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            // If we are in deferred mode and the SSR is enabled, we need to make sure that the second gbuffer is cleared given that we are using that information for clear coat selection
            bool clearGBuffer2 = clearGBuffer || hdCamera.IsSSREnabled();
            passData.gbufferRT[currentIndex++] = builder.UseColorBuffer(renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = clearGBuffer2, clearColor = Color.clear, name = "GBuffer2"
#if UNITY_2020_2_OR_NEWER
                    , fastMemoryDesc = gbufferFastMemDesc
#endif
                }), 2);
            passData.gbufferRT[currentIndex++] = builder.UseColorBuffer(renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true) { colorFormat = Builtin.GetLightingBufferFormat(), clearBuffer = clearGBuffer, clearColor = Color.clear, name = "GBuffer3"
#if UNITY_2020_2_OR_NEWER
                  , fastMemoryDesc = gbufferFastMemDesc
#endif
                }), 3);

#if ENABLE_VIRTUALTEXTURES
            passData.gbufferRT[currentIndex++] = builder.UseColorBuffer(vtFeedbackBuffer, 4);
#endif

            prepassOutput.gbuffer.lightLayersTextureIndex = -1;
            prepassOutput.gbuffer.shadowMaskTextureIndex = -1;
            if (lightLayers)
            {
                passData.gbufferRT[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = clearGBuffer, clearColor = Color.clear, name = "LightLayers" }), currentIndex);
                prepassOutput.gbuffer.lightLayersTextureIndex = currentIndex++;
            }
            if (shadowMasks)
            {
                passData.gbufferRT[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = Builtin.GetShadowMaskBufferFormat(), clearBuffer = clearGBuffer, clearColor = Color.clear, name = "ShadowMasks" }), currentIndex);
                prepassOutput.gbuffer.shadowMaskTextureIndex = currentIndex++;
            }

            prepassOutput.gbuffer.gBufferCount = currentIndex;
            for (int i = 0; i < currentIndex; ++i)
            {
                prepassOutput.gbuffer.mrt[i] = passData.gbufferRT[i];
            }
        }

        // TODO RENDERGRAPH: For now we just bind globally for GBuffer/Forward passes.
        // We need to find a nice way to invalidate this kind of buffers when they should not be used anymore (after the last read).
        static void BindDBufferGlobalData(in DBufferOutput dBufferOutput, in RenderGraphContext ctx)
        {
            for (int i = 0; i < dBufferOutput.dBufferCount; ++i)
                ctx.cmd.SetGlobalTexture(HDShaderIDs._DBufferTexture[i], dBufferOutput.mrt[i]);
        }

        static void BindProbeVolumeGlobalData(in FrameSettings frameSettings, GBufferPassData data, in RenderGraphContext ctx)
        {
            if (!data.needProbeVolumeLightLists)
                return;

            if (frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
                ctx.cmd.SetGlobalBuffer(HDShaderIDs.g_vBigTileLightList, data.probeVolumeBigTile);
            ctx.cmd.SetGlobalBuffer(HDShaderIDs.g_vProbeVolumesLayeredOffsetsBuffer, data.probeVolumePerVoxelOffset);
            ctx.cmd.SetGlobalBuffer(HDShaderIDs.g_vProbeVolumesLightListGlobal, data.probeVolumePerVoxelLightList);
            // int useDepthBuffer = 0;
            // cmd.SetGlobalInt(HDShaderIDs.g_isLogBaseBufferEnabled, useDepthBuffer);
        }

        // RenderGBuffer do the gbuffer pass. This is only called with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque alpha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(RenderGraph renderGraph, TextureHandle sssBuffer, TextureHandle vtFeedbackBuffer, ref PrepassOutput prepassOutput, in BuildGPULightListOutput probeVolumeLightList, CullingResults cull, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                prepassOutput.gbuffer.gBufferCount = 0;
                return;
            }

            using (var builder = renderGraph.AddRenderPass<GBufferPassData>("GBuffer", out var passData, ProfilingSampler.Get(HDProfileId.GBuffer)))
            {
                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;
                SetupGBufferTargets(renderGraph, hdCamera, passData, sssBuffer, vtFeedbackBuffer, ref prepassOutput, frameSettings, builder);
                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_GBufferName, m_CurrentRendererConfigurationBakedLighting)));

                passData.dBuffer = ReadDBuffer(prepassOutput.dbuffer, builder);

                passData.needProbeVolumeLightLists = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) && ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.MaterialPass;
                if (passData.needProbeVolumeLightLists)
                {
                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
                        passData.probeVolumeBigTile = builder.ReadComputeBuffer(probeVolumeLightList.bigTileLightList);
                    passData.probeVolumePerVoxelLightList = builder.ReadComputeBuffer(probeVolumeLightList.perVoxelLightLists);
                    passData.probeVolumePerVoxelOffset = builder.ReadComputeBuffer(probeVolumeLightList.perVoxelOffset);
                }

                builder.SetRenderFunc(
                (GBufferPassData data, RenderGraphContext context) =>
                {
                    BindProbeVolumeGlobalData(data.frameSettings, data, context);
                    BindDBufferGlobalData(data.dBuffer, context);
                    DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                });
            }
        }

        class ResolvePrepassData
        {
            public TextureHandle    depthBuffer;
            public TextureHandle    depthValuesBuffer;
            public TextureHandle    normalBuffer;
            public TextureHandle    motionVectorsBuffer;
            public TextureHandle    depthAsColorBufferMSAA;
            public TextureHandle    normalBufferMSAA;
            public TextureHandle    motionVectorBufferMSAA;
            public Material         depthResolveMaterial;
            public int              depthResolvePassIndex;
            public bool             needMotionVectors;
        }

        void ResolvePrepassBuffers(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                output.resolvedNormalBuffer = output.normalBuffer;
                output.resolvedDepthBuffer = output.depthBuffer;
                output.resolvedMotionVectorsBuffer = output.motionVectorsBuffer;

                return;
            }

            using (var builder = renderGraph.AddRenderPass<ResolvePrepassData>("Resolve Prepass MSAA", out var passData))
            {
                // This texture stores a set of depth values that are required for evaluating a bunch of effects in MSAA mode (R = Samples Max Depth, G = Samples Min Depth, G =  Samples Average Depth)
                TextureHandle depthValuesBuffer = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = "DepthValuesBuffer" });

                passData.needMotionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

                passData.depthResolveMaterial = m_DepthResolveMaterial;
                passData.depthResolvePassIndex = SampleCountToPassIndex(m_MSAASamples);

                passData.depthBuffer = builder.UseDepthBuffer(CreateDepthBuffer(renderGraph, true, false), DepthAccess.Write);
                passData.depthValuesBuffer = builder.UseColorBuffer(depthValuesBuffer, 0);
                passData.normalBuffer = builder.UseColorBuffer(CreateNormalBuffer(renderGraph, false), 1);
                if (passData.needMotionVectors)
                    passData.motionVectorsBuffer = builder.UseColorBuffer(CreateMotionVectorBuffer(renderGraph, false, false), 2);
                else
                    passData.motionVectorsBuffer = TextureHandle.nullHandle;

                passData.normalBufferMSAA = builder.ReadTexture(output.normalBuffer);
                passData.depthAsColorBufferMSAA = builder.ReadTexture(output.depthAsColor);
                if (passData.needMotionVectors)
                    passData.motionVectorBufferMSAA = builder.ReadTexture(output.motionVectorsBuffer);

                output.resolvedNormalBuffer = passData.normalBuffer;
                output.resolvedDepthBuffer = passData.depthBuffer;
                output.resolvedMotionVectorsBuffer = passData.motionVectorsBuffer;
                output.depthValuesMSAA = passData.depthValuesBuffer;

                builder.SetRenderFunc(
                (ResolvePrepassData data, RenderGraphContext context) =>
                {
                    data.depthResolveMaterial.SetTexture(HDShaderIDs._NormalTextureMS, data.normalBufferMSAA);
                    data.depthResolveMaterial.SetTexture(HDShaderIDs._DepthTextureMS, data.depthAsColorBufferMSAA);
                    if (data.needMotionVectors)
                    {
                        data.depthResolveMaterial.SetTexture(HDShaderIDs._MotionVectorTextureMS, data.motionVectorBufferMSAA);
                    }

                    CoreUtils.SetKeyword(context.cmd, "_HAS_MOTION_VECTORS", data.needMotionVectors);

                    context.cmd.DrawProcedural(Matrix4x4.identity, data.depthResolveMaterial, data.depthResolvePassIndex, MeshTopology.Triangles, 3, 1);
                });
            }
        }

        class CopyDepthPassData
        {
            public TextureHandle    inputDepth;
            public TextureHandle    outputDepth;
            public GPUCopy          GPUCopy;
            public int              width;
            public int              height;
        }

        void CopyDepthBufferIfNeeded(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if(!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                output.depthPyramidTexture = renderGraph.defaultResources.blackTextureXR;
                return;
            }

            if (!m_IsDepthBufferCopyValid)
            {
                using (var builder = renderGraph.AddRenderPass<CopyDepthPassData>("Copy depth buffer", out var passData, ProfilingSampler.Get(HDProfileId.CopyDepthBuffer)))
                {
                    passData.inputDepth = builder.ReadTexture(output.resolvedDepthBuffer);
                    passData.outputDepth = builder.WriteTexture(renderGraph.CreateTexture(m_DepthPyramidDesc));
                    passData.GPUCopy = m_GPUCopy;
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;

                    output.depthPyramidTexture = passData.outputDepth;

                    builder.SetRenderFunc(
                    (CopyDepthPassData data, RenderGraphContext context) =>
                    {
                        // TODO: maybe we don't actually need the top MIP level?
                        // That way we could avoid making the copy, and build the MIP hierarchy directly.
                        // The downside is that our SSR tracing accuracy would decrease a little bit.
                        // But since we never render SSR at full resolution, this may be acceptable.

                        // TODO: reading the depth buffer with a compute shader will cause it to decompress in place.
                        // On console, to preserve the depth test performance, we must NOT decompress the 'm_CameraDepthStencilBuffer' in place.
                        // We should call decompressDepthSurfaceToCopy() and decompress it to 'm_CameraDepthBufferMipChain'.
                        data.GPUCopy.SampleCopyChannel_xyzw2x(context.cmd, data.inputDepth, data.outputDepth, new RectInt(0, 0, data.width, data.height));
                    });
                }

                m_IsDepthBufferCopyValid = true;
            }
        }

        class ResolveStencilPassData
        {
            public BuildCoarseStencilAndResolveParameters parameters;
            public TextureHandle inputDepth;
            public TextureHandle resolvedStencil;
            public ComputeBufferHandle coarseStencilBuffer;
        }

        // This pass build the coarse stencil buffer if requested (i.e. when resolveOnly: false) and perform the MSAA resolve of the
        // full res stencil buffer if needed (a pass requires it and MSAA is on).
        void BuildCoarseStencilAndResolveIfNeeded(RenderGraph renderGraph, HDCamera hdCamera, bool resolveOnly, ref PrepassOutput output)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveStencilPassData>("Resolve Stencil", out var passData, ProfilingSampler.Get(HDProfileId.ResolveStencilBuffer)))
            {
                passData.parameters = PrepareBuildCoarseStencilParameters(hdCamera, resolveOnly);
                passData.inputDepth = output.depthBuffer;
                passData.coarseStencilBuffer = builder.WriteComputeBuffer(
                    renderGraph.CreateComputeBuffer(new ComputeBufferDesc(HDUtils.DivRoundUp(m_MaxCameraWidth, 8) * HDUtils.DivRoundUp(m_MaxCameraHeight, 8) * m_MaxViewCount, sizeof(uint)) { name = "CoarseStencilBuffer" }));
                if (passData.parameters.resolveIsNecessary)
                    passData.resolvedStencil = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8_UInt, enableRandomWrite = true, name = "StencilBufferResolved" }));
                else
                    passData.resolvedStencil = output.stencilBuffer;
                builder.SetRenderFunc(
                (ResolveStencilPassData data, RenderGraphContext context) =>
                {
                    BuildCoarseStencilAndResolveIfNeeded(data.parameters, data.inputDepth, data.resolvedStencil, data.coarseStencilBuffer, context.cmd);
                });

                bool isMSAAEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
                if (isMSAAEnabled)
                {
                    output.stencilBuffer = passData.resolvedStencil;
                }
                output.coarseStencilBuffer = passData.coarseStencilBuffer;
            }
        }

        class RenderDBufferPassData
        {
            public RenderDBufferParameters  parameters;
            public TextureHandle[]          mrt = new TextureHandle[Decal.GetMaterialDBufferCount()];
            public int                      dBufferCount;
            public RendererListHandle       meshDecalsRendererList;
            public TextureHandle            depthStencilBuffer;
            public TextureHandle            depthTexture;
            public TextureHandle            decalBuffer;
        }

        struct DBufferOutput
        {
            public TextureHandle[]      mrt;
            public int                  dBufferCount;
        }

        class DBufferNormalPatchData
        {
            public DBufferNormalPatchParameters parameters;
            public DBufferOutput dBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
        }

        static string[] s_DBufferNames = { "DBuffer0", "DBuffer1", "DBuffer2", "DBuffer3" };

        void SetupDBufferTargets(RenderGraph renderGraph, RenderDBufferPassData passData, bool use4RTs, ref PrepassOutput output, RenderGraphBuilder builder)
        {
            GraphicsFormat[] rtFormat;
            Decal.GetMaterialDBufferDescription(out rtFormat);
            passData.dBufferCount = use4RTs ? 4 : 3;

            for (int dbufferIndex = 0; dbufferIndex < passData.dBufferCount; ++dbufferIndex)
            {
                passData.mrt[dbufferIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = rtFormat[dbufferIndex], name = s_DBufferNames[dbufferIndex] }), dbufferIndex);
            }

            int propertyMaskBufferSize = ((m_MaxCameraWidth + 7) / 8) * ((m_MaxCameraHeight + 7) / 8);
            propertyMaskBufferSize = ((propertyMaskBufferSize + 63) / 64) * 64; // round off to nearest multiple of 64 for ease of use in CS

            passData.depthStencilBuffer = builder.UseDepthBuffer(output.resolvedDepthBuffer, DepthAccess.Write);

            output.dbuffer.dBufferCount = passData.dBufferCount;
            for (int i = 0; i < passData.dBufferCount; ++i)
            {
                output.dbuffer.mrt[i] = passData.mrt[i];
            }
        }

        static DBufferOutput ReadDBuffer(DBufferOutput dBufferOutput, RenderGraphBuilder builder)
        {
            // We do the reads "in place" because we don't want to allocate a struct with dynamic arrays each time we do that and we want to keep loops for code sanity.
            for (int i = 0; i < dBufferOutput.dBufferCount; ++i)
                dBufferOutput.mrt[i] = builder.ReadTexture(dBufferOutput.mrt[i]);

            return dBufferOutput;
        }

        void RenderDBuffer(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle decalBuffer, ref PrepassOutput output, CullingResults cullingResults)
        {
            bool use4RTs = m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals) ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                // Return all black textures for default values.
                var blackTexture = renderGraph.defaultResources.blackTextureXR;
                output.depthPyramidTexture = blackTexture;
                output.dbuffer.dBufferCount = use4RTs ? 4 : 3;
                for (int i = 0; i < output.dbuffer.dBufferCount; ++i)
                    output.dbuffer.mrt[i] = blackTexture;
                return;
            }

            bool canReadBoundDepthBuffer = SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation4 ||
                                            SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOne ||
                                            SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12;

            if (!canReadBoundDepthBuffer)
            {
                // We need to copy depth buffer texture if we want to bind it at this stage
                CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);
            }

            // If we have an incomplete depth buffer use for decal we will need to do another copy
            // after the rendering of the GBuffer
            if ((   hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred) &&
                    !hdCamera.frameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering))
                m_IsDepthBufferCopyValid = false;

            using (var builder = renderGraph.AddRenderPass<RenderDBufferPassData>("DBufferRender", out var passData, ProfilingSampler.Get(HDProfileId.DBufferRender)))
            {
                passData.parameters = PrepareRenderDBufferParameters(hdCamera);
                passData.meshDecalsRendererList = builder.UseRendererList(renderGraph.CreateRendererList(PrepareMeshDecalsRendererList(cullingResults, hdCamera, use4RTs)));
                SetupDBufferTargets(renderGraph, passData, use4RTs, ref output, builder);
                passData.decalBuffer = builder.ReadTexture(decalBuffer);
                passData.depthTexture = canReadBoundDepthBuffer ? builder.ReadTexture(output.resolvedDepthBuffer) : builder.ReadTexture(output.depthPyramidTexture);

                builder.SetRenderFunc(
                (RenderDBufferPassData data, RenderGraphContext context) =>
                {
                    RenderTargetIdentifier[] rti = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(data.dBufferCount);
                    RTHandle[] rt = context.renderGraphPool.GetTempArray<RTHandle>(data.dBufferCount);

                    // TODO : Remove once we remove old renderer
                    // This way we can directly use the UseColorBuffer API and set clear color directly at resource creation and not in the RenderDBuffer shared function.
                    for (int i = 0; i < data.dBufferCount; ++i)
                    {
                        rt[i] = data.mrt[i];
                        rti[i] = rt[i];
                    }

                    RenderDBuffer(  data.parameters,
                                    rti,
                                    rt,
                                    data.depthStencilBuffer,
                                    data.depthTexture,
                                    data.meshDecalsRendererList,
                                    data.decalBuffer,
                                    context.renderContext,
                                    context.cmd);
                });
            }
        }

        void DecalNormalPatch(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            // Integrated Intel GPU on Mac don't support the texture format use for normal (RGBA_8UNORM) for SetRandomWriteTarget
            // So on Metal for now we don't patch normal buffer if we detect an intel GPU
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && SystemInfo.graphicsDeviceName.Contains("Intel"))
            {
                return;
            }

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals) &&
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) &&
                hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects)) // MSAA not supported
            {
                using (var builder = renderGraph.AddRenderPass<DBufferNormalPatchData>("DBuffer Normal (forward)", out var passData, ProfilingSampler.Get(HDProfileId.DBufferNormal)))
                {
                    passData.parameters = PrepareDBufferNormalPatchParameters(hdCamera);
                    passData.dBuffer = ReadDBuffer(output.dbuffer, builder);

                    passData.normalBuffer = builder.WriteTexture(output.resolvedNormalBuffer);
                    passData.depthStencilBuffer = builder.ReadTexture(output.resolvedDepthBuffer);

                    builder.SetRenderFunc(
                    (DBufferNormalPatchData data, RenderGraphContext ctx) =>
                    {
                        RTHandle[] mrt = ctx.renderGraphPool.GetTempArray<RTHandle>(data.dBuffer.dBufferCount);
                        for (int i = 0; i < data.dBuffer.dBufferCount; ++i)
                            mrt[i] = data.dBuffer.mrt[i];

                        DecalNormalPatch(data.parameters, mrt, data.depthStencilBuffer, data.normalBuffer, ctx.cmd);
                    });
                }
            }
        }

        class DownsampleDepthForLowResPassData
        {
            public Material downsampleDepthMaterial;
            public TextureHandle depthTexture;
            public TextureHandle downsampledDepthBuffer;

            // Data needed for potentially writing
            public Vector2Int mip0Offset;
            public bool computesMip1OfAtlas;
        }

        void DownsampleDepthForLowResTransparency(RenderGraph renderGraph, HDCamera hdCamera, bool computeMip1OfPyramid, ref PrepassOutput output)
        {
            // If the depth buffer hasn't been already copied by the decal depth buffer pass, then we do the copy here.
            CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<DownsampleDepthForLowResPassData>("Downsample Depth Buffer for Low Res Transparency", out var passData, ProfilingSampler.Get(HDProfileId.DownsampleDepth)))
            {
                // TODO: Add option to switch modes at runtime
                if (m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings.checkerboardDepthBuffer)
                {
                    m_DownsampleDepthMaterial.EnableKeyword("CHECKERBOARD_DOWNSAMPLE");
                }

                passData.computesMip1OfAtlas = computeMip1OfPyramid;
                if (computeMip1OfPyramid)
                {
                    passData.mip0Offset = GetDepthBufferMipChainInfo().mipLevelOffsets[1];
                    m_DownsampleDepthMaterial.EnableKeyword("OUTPUT_FIRST_MIP_OF_MIPCHAIN");
                }

                passData.downsampleDepthMaterial = m_DownsampleDepthMaterial;
                passData.depthTexture = builder.ReadTexture(output.depthPyramidTexture);
                if(computeMip1OfPyramid)
                {
                    passData.depthTexture = builder.WriteTexture(passData.depthTexture);
                }
                passData.downsampledDepthBuffer = builder.UseDepthBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * 0.5f, true, true) { depthBufferBits = DepthBits.Depth32, name = "LowResDepthBuffer" }), DepthAccess.Write);

                builder.SetRenderFunc(
                (DownsampleDepthForLowResPassData data, RenderGraphContext context) =>
                {
                    if (data.computesMip1OfAtlas)
                    {
                        data.downsampleDepthMaterial.SetVector(HDShaderIDs._DstOffset, new Vector4(data.mip0Offset.x, data.mip0Offset.y, 0.0f, 0.0f));
                        context.cmd.SetRandomWriteTarget(1, data.depthTexture);
                    }

                    context.cmd.DrawProcedural(Matrix4x4.identity, data.downsampleDepthMaterial, 0, MeshTopology.Triangles, 3, 1, null);

                    if (data.computesMip1OfAtlas)
                    {
                        context.cmd.ClearRandomWriteTargets();
                    }
                });

                output.downsampledDepthBuffer = passData.downsampledDepthBuffer;
            }
        }

        class GenerateDepthPyramidPassData
        {
            public TextureHandle                depthTexture;
            public HDUtils.PackedMipChainInfo   mipInfo;
            public MipGenerator                 mipGenerator;

            public bool                         mip0AlreadyComputed;
        }

        void GenerateDepthPyramid(RenderGraph renderGraph, HDCamera hdCamera, bool mip0AlreadyComputed, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                output.depthPyramidTexture = renderGraph.defaultResources.blackTextureXR;
                return;
            }

            // If the depth buffer hasn't been already copied by the decal or low res depth buffer pass, then we do the copy here.
            CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<GenerateDepthPyramidPassData>("Generate Depth Buffer MIP Chain", out var passData, ProfilingSampler.Get(HDProfileId.DepthPyramid)))
            {
                passData.depthTexture = builder.WriteTexture(output.depthPyramidTexture);
                passData.mipInfo = GetDepthBufferMipChainInfo();
                passData.mipGenerator = m_MipGenerator;
                passData.mip0AlreadyComputed = mip0AlreadyComputed;

                builder.SetRenderFunc(
                (GenerateDepthPyramidPassData data, RenderGraphContext context) =>
                {
                    data.mipGenerator.RenderMinDepthPyramid(context.cmd, data.depthTexture, data.mipInfo, data.mip0AlreadyComputed);
                });

                output.depthPyramidTexture = passData.depthTexture;
            }

            // TODO: This is currently useless as the depth pyramid is an atlas.
            // The mip choice is directly resolved in the full screen debug pass.
            // Re-enable this when/if depth texture becomes an actual texture with mip again.
            //PushFullScreenDebugTextureMip(renderGraph, output.depthPyramidTexture, GetDepthBufferMipChainInfo().mipLevelCount, renderGraph.rtHandleProperties.rtHandleScale, FullScreenDebugMode.DepthPyramid);
        }

        class CameraMotionVectorsPassData
        {
            public Material cameraMotionVectorsMaterial;
            public TextureHandle motionVectorsBuffer;
            public TextureHandle depthBuffer;
        }

        void RenderCameraMotionVectors(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle motionVectorsBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                return;

            using (var builder = renderGraph.AddRenderPass<CameraMotionVectorsPassData>("Camera Motion Vectors Rendering", out var passData, ProfilingSampler.Get(HDProfileId.CameraMotionVectors)))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                m_CameraMotionVectorsMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.ObjectMotionVector);
                m_CameraMotionVectorsMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.ObjectMotionVector);
                passData.cameraMotionVectorsMaterial = m_CameraMotionVectorsMaterial;
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.motionVectorsBuffer = builder.WriteTexture(motionVectorsBuffer);

                builder.SetRenderFunc(
                (CameraMotionVectorsPassData data, RenderGraphContext context) =>
                {
                    HDUtils.DrawFullScreen(context.cmd, data.cameraMotionVectorsMaterial,data.motionVectorsBuffer, data.depthBuffer, null, 0);
                });
            }
        }
    }
}
