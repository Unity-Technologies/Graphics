using System;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        internal static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
        static ShaderTagId s_ShaderTagLit = new ShaderTagId("Lit");
        static ShaderTagId s_ShaderTagSimpleLit = new ShaderTagId("SimpleLit");
        static ShaderTagId s_ShaderTagUnlit = new ShaderTagId("Unlit");
        static ShaderTagId s_ShaderTagComplexLit = new ShaderTagId("ComplexLit");
        static ShaderTagId s_ShaderTagUniversalGBuffer = new ShaderTagId("UniversalGBuffer");
        static ShaderTagId s_ShaderTagUniversalMaterialType = new ShaderTagId("UniversalMaterialType");

        static ProfilingSampler s_ProfilingSampler = new ProfilingSampler("Render GBuffer");

        DeferredLights m_DeferredLights;

        static ShaderTagId[] s_ShaderTagValues;
        static RenderStateBlock[] s_RenderStateBlocks;

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        private PassData m_PassData;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
        {
            base.profilingSampler = new ProfilingSampler(nameof(GBufferPass));
            base.renderPassEvent = evt;
            m_PassData = new PassData();

            m_DeferredLights = deferredLights;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            m_RenderStateBlock.stencilState = stencilState;
            m_RenderStateBlock.stencilReference = stencilReference;
            m_RenderStateBlock.mask = RenderStateMask.Stencil;

            if (s_ShaderTagValues == null)
            {
                s_ShaderTagValues = new ShaderTagId[5];
                s_ShaderTagValues[0] = s_ShaderTagLit;
                s_ShaderTagValues[1] = s_ShaderTagSimpleLit;
                s_ShaderTagValues[2] = s_ShaderTagUnlit;
                s_ShaderTagValues[3] = s_ShaderTagComplexLit;
                s_ShaderTagValues[4] = new ShaderTagId(); // Special catch all case for materials where UniversalMaterialType is not defined or the tag value doesn't match anything we know.
            }

            if (s_RenderStateBlocks == null)
            {
                s_RenderStateBlocks = new RenderStateBlock[5];
                s_RenderStateBlocks[0] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialLit);
                s_RenderStateBlocks[1] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialSimpleLit);
                s_RenderStateBlocks[2] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialUnlit);
                s_RenderStateBlocks[3] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialUnlit);  // Fill GBuffer, but skip lighting pass for ComplexLit
                s_RenderStateBlocks[4] = s_RenderStateBlocks[0];
            }
        }

        public void Dispose()
        {
            m_DeferredLights?.ReleaseGbufferResources();
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RTHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

            if (cmd != null)
            {
                var allocateGbufferDepth = true;
                if (m_DeferredLights.UseFramebufferFetch && (m_DeferredLights.DepthCopyTexture != null && m_DeferredLights.DepthCopyTexture.rt != null))
                {
                    m_DeferredLights.GbufferAttachments[m_DeferredLights.GbufferDepthIndex] = m_DeferredLights.DepthCopyTexture;
                    allocateGbufferDepth = false;
                }
                // Create and declare the render targets used in the pass
                for (int i = 0; i < gbufferAttachments.Length; ++i)
                {
                    // Lighting buffer has already been declared with line ConfigureCameraTarget(m_ActiveCameraColorAttachment.Identifier(), ...) in DeferredRenderer.Setup
                    if (i == m_DeferredLights.GBufferLightingIndex)
                        continue;

                    // Normal buffer may have already been created if there was a depthNormal prepass before.
                    // DepthNormal prepass is needed for forward-only materials when SSAO is generated between gbuffer and deferred lighting pass.
                    if (i == m_DeferredLights.GBufferNormalSmoothnessIndex && m_DeferredLights.HasNormalPrepass)
                        continue;

                    if (i == m_DeferredLights.GbufferDepthIndex && !allocateGbufferDepth)
                        continue;

                    // No need to setup temporaryRTs if we are using input attachments as they will be Memoryless
                    if (m_DeferredLights.UseFramebufferFetch && (i != m_DeferredLights.GbufferDepthIndex && !m_DeferredLights.HasDepthPrepass))
                        continue;

                    m_DeferredLights.ReAllocateGBufferIfNeeded(cameraTextureDescriptor, i);

                    cmd.SetGlobalTexture(m_DeferredLights.GbufferAttachments[i].name, m_DeferredLights.GbufferAttachments[i].nameID);
                }
            }

            if (m_DeferredLights.UseFramebufferFetch)
                m_DeferredLights.UpdateDeferredInputAttachments();

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            ConfigureTarget(m_DeferredLights.GbufferAttachments, m_DeferredLights.DepthAttachment, m_DeferredLights.GbufferFormats);

            // We must explicitly specify we don't want any clear to avoid unwanted side-effects.
            // ScriptableRenderer will implicitly force a clear the first time the camera color/depth targets are bound.
            ConfigureClear(ClearFlag.None, Color.black);
            #pragma warning restore CS0618
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            m_PassData.deferredLights = m_DeferredLights;
            InitRendererLists(ref m_PassData, context, default(RenderGraph), universalRenderingData, cameraData, lightData, false);

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, s_ProfilingSampler))
            {
                #if UNITY_EDITOR
                // Need to clear the bounded targets to get scene-view filtering working.
                if (CoreUtils.IsSceneFilteringEnabled() && cameraData.camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                    cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear);
                #endif

                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), m_PassData, m_PassData.rendererList, m_PassData.objectsWithErrorRendererList);

                // If any sub-system needs camera normal texture, make it available.
                // Input attachments will only be used when this is not needed so safe to skip in that case
                if (!m_DeferredLights.UseFramebufferFetch)
                    renderingData.commandBuffer.SetGlobalTexture(s_CameraNormalsTextureID, m_DeferredLights.GbufferAttachments[m_DeferredLights.GBufferNormalSmoothnessIndex]);
            }
        }

        static void ExecutePass(RasterCommandBuffer cmd, PassData data, RendererList rendererList, RendererList errorRendererList)
        {
            bool usesRenderingLayers = data.deferredLights.UseRenderingLayers && !data.deferredLights.HasRenderingLayerPrepass;
            if (usesRenderingLayers)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, true);

            if (data.deferredLights.IsOverlay)
                data.deferredLights.ClearStencilPartial(cmd);

            cmd.DrawRendererList(rendererList);

            // Render objects that did not match any shader pass with error shader
            RenderingUtils.DrawRendererListObjectsWithError(cmd, ref errorRendererList);

            // Clean up
            if (usesRenderingLayers)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, false);
        }

        /// <summary>
        /// Shared pass data
        /// </summary>
        private class PassData
        {
            internal TextureHandle[] gbuffer;
            internal TextureHandle depth;

            internal DeferredLights deferredLights;

            internal RendererListHandle rendererListHdl;
            internal RendererListHandle objectsWithErrorRendererListHdl;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
            internal RendererList objectsWithErrorRendererList;
        }


        private void InitRendererLists( ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, bool useRenderGraph)
        {
            // User can stack several scriptable renderers during rendering but deferred renderer should only lit pixels added by this gbuffer pass.
            // If we detect we are in such case (camera is in overlay mode), we clear the highest bits of stencil we have control of and use them to
            // mark what pixel to shade during deferred pass. Gbuffer will always mark pixels using their material types.
            ShaderTagId lightModeTag = s_ShaderTagUniversalGBuffer;
            var drawingSettings = CreateDrawingSettings(lightModeTag, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            var filterSettings = m_FilteringSettings;
            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            var param = new RendererListParams(renderingData.cullResults, drawingSettings, filterSettings)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                tagName = s_ShaderTagUniversalMaterialType,
                isPassTagName = false
            };
            if (useRenderGraph)
            {
                passData.rendererListHdl = renderGraph.CreateRendererList(param);
            }
            else
            {
                passData.rendererList = context.CreateRendererList(ref param);
            }
            tagValues.Dispose();
            stateBlocks.Dispose();

            if (useRenderGraph)
            {
                RenderingUtils.CreateRendererListObjectsWithError(renderGraph, ref renderingData.cullResults, cameraData.camera, filterSettings, SortingCriteria.None, ref passData.objectsWithErrorRendererListHdl);
            }
            else
            {
                RenderingUtils.CreateRendererListObjectsWithError(context, ref renderingData.cullResults, cameraData.camera, filterSettings, SortingCriteria.None, ref passData.objectsWithErrorRendererList);
            }
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            TextureHandle[] gbuffer;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("GBuffer Pass", out var passData, s_ProfilingSampler))
            {
                // Note: This code is pretty confusing as passData.gbuffer[i] and gbuffer[i] actually point to the same array but seem to be mixed in this code.
                passData.gbuffer = gbuffer = m_DeferredLights.GbufferTextureHandles;
                for (int i = 0; i < m_DeferredLights.GBufferSliceCount; i++)
                {
                    var gbufferSlice = cameraData.cameraTargetDescriptor;
                    gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                    gbufferSlice.stencilFormat = GraphicsFormat.None;

                    if (i == m_DeferredLights.GBufferNormalSmoothnessIndex && m_DeferredLights.HasNormalPrepass)
                        gbuffer[i] = resourceData.cameraNormalsTexture;
                    else if (m_DeferredLights.UseRenderingLayers && i == m_DeferredLights.GBufferRenderingLayers && !m_DeferredLights.UseLightLayers)
                        gbuffer[i] = resourceData.renderingLayersTexture;
                    else if (i != m_DeferredLights.GBufferLightingIndex)
                    {
                        gbufferSlice.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
                        gbuffer[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, gbufferSlice, DeferredLights.k_GBufferNames[i], true);
                    }
                    else
                        gbuffer[i] = cameraColor;

                    // Note: We don't store the returned handle here it is a versioned handle.
                    // In general it should be fine to use unversioned handles anyway especially unversioned resources
                    // should be registered in the frame data
                    builder.SetRenderAttachment(gbuffer[i], i, AccessFlags.Write);
                }

                RenderGraphUtils.UseDBufferIfValid(builder, resourceData);
                resourceData.gBuffer = gbuffer;

                passData.depth = cameraDepth;
                builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Write);
                passData.deferredLights = m_DeferredLights;

                InitRendererLists(ref passData, default(ScriptableRenderContext), renderGraph, renderingData, cameraData, lightData, true);
                builder.UseRendererList(passData.rendererListHdl);
                builder.UseRendererList(passData.objectsWithErrorRendererListHdl);

                // With NRP GBuffer textures are set after Deferred, we do this to avoid breaking the pass
                if (!renderGraph.nativeRenderPassesEnabled)
                    GBufferPass.SetGlobalGBufferTextures(builder, gbuffer, ref m_DeferredLights);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.rendererListHdl, data.objectsWithErrorRendererListHdl);
                });
            }
        }

        internal static void SetGlobalGBufferTextures(IRasterRenderGraphBuilder builder, TextureHandle[] gbuffer, ref DeferredLights deferredLights)
        {
            for (int i = 0; i < gbuffer.Length; i++)
            {
                if (i != deferredLights.GBufferLightingIndex && gbuffer[i].IsValid())
                    builder.SetGlobalTextureAfterPass(gbuffer[i], Shader.PropertyToID(DeferredLights.k_GBufferNames[i]));
            }

            // If any sub-system needs camera normal texture, make it available.
            // Input attachments will only be used when this is not needed so safe to skip in that case
            if (gbuffer[deferredLights.GBufferNormalSmoothnessIndex].IsValid())
                builder.SetGlobalTextureAfterPass(gbuffer[deferredLights.GBufferNormalSmoothnessIndex], s_CameraNormalsTextureID);

            if (deferredLights.UseRenderingLayers && gbuffer[deferredLights.GBufferRenderingLayers].IsValid())
            {
                builder.SetGlobalTextureAfterPass(gbuffer[deferredLights.GBufferRenderingLayers], Shader.PropertyToID(DeferredLights.k_GBufferNames[deferredLights.GBufferRenderingLayers]));
                builder.SetGlobalTextureAfterPass(gbuffer[deferredLights.GBufferRenderingLayers], Shader.PropertyToID("_CameraRenderingLayersTexture"));
            }
        }

        internal static void ResetGlobalGBufferTextures(RenderGraph renderGraph, TextureHandle[] gbuffer, TextureHandle depthTarget,
            UniversalResourceData resourcesData, ref DeferredLights deferredLights)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Reset Global GBuffer Textures",
                out var passData, s_ProfilingSampler))
            {
                passData.deferredLights = deferredLights;
                gbuffer = resourcesData.gBuffer;
                passData.gbuffer = deferredLights.GbufferTextureHandles;

                for (int i = 0; i < deferredLights.GBufferSliceCount; i++)
                {
                    if (i != deferredLights.GBufferLightingIndex)
                    {
                        passData.gbuffer[i] = gbuffer[i];
                        builder.SetRenderAttachment(gbuffer[i], i, AccessFlags.Write);
                    }
                }

                builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                });
            }
        }
    }
}
