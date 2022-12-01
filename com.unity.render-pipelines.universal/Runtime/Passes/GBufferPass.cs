using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
        static ShaderTagId s_ShaderTagLit = new ShaderTagId("Lit");
        static ShaderTagId s_ShaderTagSimpleLit = new ShaderTagId("SimpleLit");
        static ShaderTagId s_ShaderTagUnlit = new ShaderTagId("Unlit");
        static ShaderTagId s_ShaderTagUniversalGBuffer = new ShaderTagId("UniversalGBuffer");
        static ShaderTagId s_ShaderTagUniversalMaterialType = new ShaderTagId("UniversalMaterialType");

        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render GBuffer");

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
                s_ShaderTagValues = new ShaderTagId[4];
                s_ShaderTagValues[0] = s_ShaderTagLit;
                s_ShaderTagValues[1] = s_ShaderTagSimpleLit;
                s_ShaderTagValues[2] = s_ShaderTagUnlit;
                s_ShaderTagValues[3] = new ShaderTagId(); // Special catch all case for materials where UniversalMaterialType is not defined or the tag value doesn't match anything we know.
            }

            if (s_RenderStateBlocks == null)
            {
                s_RenderStateBlocks = new RenderStateBlock[4];
                s_RenderStateBlocks[0] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialLit);
                s_RenderStateBlocks[1] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialSimpleLit);
                s_RenderStateBlocks[2] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialUnlit);
                s_RenderStateBlocks[3] = s_RenderStateBlocks[0];
            }
        }

        public void Dispose()
        {
            m_DeferredLights.ReleaseGbufferResources();
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RTHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

            if (cmd != null)
            {
                var allocateGbufferDepth = true;
                if (m_DeferredLights.UseRenderPass && (m_DeferredLights.DepthCopyTexture != null && m_DeferredLights.DepthCopyTexture.rt != null))
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
                    if (m_DeferredLights.UseRenderPass && i != m_DeferredLights.GBufferShadowMask && i != m_DeferredLights.GBufferRenderingLayers && (i != m_DeferredLights.GbufferDepthIndex && !m_DeferredLights.HasDepthPrepass))
                        continue;

                    RenderTextureDescriptor gbufferSlice = cameraTextureDescriptor;
                    gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                    gbufferSlice.stencilFormat = GraphicsFormat.None;
                    gbufferSlice.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
                    RenderingUtils.ReAllocateIfNeeded(ref m_DeferredLights.GbufferAttachments[i], gbufferSlice, FilterMode.Point, TextureWrapMode.Clamp, name: DeferredLights.k_GBufferNames[i]);
                    cmd.SetGlobalTexture(m_DeferredLights.GbufferAttachments[i].name, m_DeferredLights.GbufferAttachments[i].nameID);
                }
            }

            if (m_DeferredLights.UseRenderPass)
                m_DeferredLights.UpdateDeferredInputAttachments();

            ConfigureTarget(m_DeferredLights.GbufferAttachments, m_DeferredLights.DepthAttachment, m_DeferredLights.GbufferFormats);

            // We must explicitly specify we don't want any clear to avoid unwanted side-effects.
            // ScriptableRenderer will implicitly force a clear the first time the camera color/depth targets are bound.
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            m_PassData.filteringSettings = m_FilteringSettings;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                m_PassData.deferredLights = m_DeferredLights;

                // User can stack several scriptable renderers during rendering but deferred renderer should only lit pixels added by this gbuffer pass.
                // If we detect we are in such case (camera is in overlay mode), we clear the highest bits of stencil we have control of and use them to
                // mark what pixel to shade during deferred pass. Gbuffer will always mark pixels using their material types.


                ref CameraData cameraData = ref renderingData.cameraData;
                ShaderTagId lightModeTag = s_ShaderTagUniversalGBuffer;
                m_PassData.drawingSettings = CreateDrawingSettings(lightModeTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                ExecutePass(context, m_PassData, ref renderingData);

                // If any sub-system needs camera normal texture, make it available.
                // Input attachments will only be used when this is not needed so safe to skip in that case
                if (!m_DeferredLights.UseRenderPass)
                    renderingData.commandBuffer.SetGlobalTexture(s_CameraNormalsTextureID, m_DeferredLights.GbufferAttachments[m_DeferredLights.GBufferNormalSmoothnessIndex]);
            }
        }

        static void ExecutePass(ScriptableRenderContext context, PassData data, ref RenderingData renderingData, bool useRenderGraph = false)
        {
            var cmd = renderingData.commandBuffer;
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.WriteRenderingLayers, data.deferredLights.UseRenderingLayers);

            if (data.deferredLights.IsOverlay)
            {
                data.deferredLights.ClearStencilPartial(renderingData.commandBuffer);
                context.ExecuteCommandBuffer(renderingData.commandBuffer);
                renderingData.commandBuffer.Clear();
            }

            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);

            var param = new RendererListParams(renderingData.cullResults, data.drawingSettings, data.filteringSettings)
            {
                tagValues =  tagValues,
                stateBlocks = stateBlocks,
                tagName = s_ShaderTagUniversalMaterialType,
                isPassTagName = false
            };
            var rl = context.CreateRendererList(ref param);
            cmd.DrawRendererList(rl);

            tagValues.Dispose();
            stateBlocks.Dispose();

            // Render objects that did not match any shader pass with error shader
            RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, renderingData.cameraData.camera, data.filteringSettings, SortingCriteria.None, cmd);
        }

        private class PassData
        {
            internal TextureHandle[] gbuffer;
            internal TextureHandle depth;

            internal RenderingData renderingData;

            internal DeferredLights deferredLights;
            internal FilteringSettings filteringSettings;
            internal DrawingSettings drawingSettings;
        }

        internal TextureHandle[] GetFrameResourcesGBufferArray(FrameResources frameResources)
        {
            TextureHandle[] gbuffer = m_DeferredLights.GbufferTextureHandles;

            Debug.Assert(gbuffer.Length <= 5, "GBufferPass.GetFrameResourcesGBufferArray: the gbuffer frame resources are limited to 5!");

            for (int i = 0; i < gbuffer.Length; ++i)
            {
                gbuffer[i] = frameResources.GetTexture((UniversalResource) (UniversalResource.GBuffer0 + i));
            }

            return gbuffer;
        }

        internal void SetFrameResourcesGBufferArray(FrameResources frameResources, TextureHandle[] gbuffer)
        {
            Debug.Assert(gbuffer.Length <= 5, "GBufferPass.SetFrameResourcesGBufferArray: the gbuffer frame resources are limited to 5!");

            for (int i = 0; i < gbuffer.Length; ++i)
                frameResources.SetTexture((UniversalResource) (UniversalResource.GBuffer0 + i), gbuffer[i]);
        }

        internal void Render(RenderGraph renderGraph, TextureHandle cameraColor, TextureHandle cameraDepth,
            ref RenderingData renderingData, FrameResources frameResources)
        {
            TextureHandle[] gbuffer;

            using (var builder = renderGraph.AddRenderPass<PassData>("GBuffer Pass", out var passData, m_ProfilingSampler))
            {
                passData.gbuffer = gbuffer = m_DeferredLights.GbufferTextureHandles;
                for (int i = 0; i < m_DeferredLights.GBufferSliceCount; i++)
                {
                    var gbufferSlice = renderingData.cameraData.cameraTargetDescriptor;
                    gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                    gbufferSlice.stencilFormat = GraphicsFormat.None;

                    if (i == m_DeferredLights.GBufferNormalSmoothnessIndex && m_DeferredLights.HasNormalPrepass)
                        gbuffer[i] = frameResources.GetTexture(UniversalResource.CameraNormalsTexture);
                    else if (m_DeferredLights.UseRenderingLayers && i == m_DeferredLights.GBufferRenderingLayers && !m_DeferredLights.UseLightLayers)
                        gbuffer[i] = frameResources.GetTexture(UniversalResource.RenderingLayersTexture);
                    else if (i != m_DeferredLights.GBufferLightingIndex)
                    {
                        gbufferSlice.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
                        gbuffer[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, gbufferSlice, DeferredLights.k_GBufferNames[i], true);
                    }
                    else
                        gbuffer[i] = cameraColor;

                    passData.gbuffer[i] = builder.UseColorBuffer(gbuffer[i], i);
                }

                SetFrameResourcesGBufferArray(frameResources, gbuffer);

                passData.deferredLights = m_DeferredLights;
                passData.depth = builder.UseDepthBuffer(cameraDepth, DepthAccess.Write);

                passData.renderingData = renderingData;

                builder.AllowPassCulling(false);

                passData.filteringSettings = m_FilteringSettings;
                ShaderTagId lightModeTag = s_ShaderTagUniversalGBuffer;
                passData.drawingSettings = CreateDrawingSettings(lightModeTag, ref passData.renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data, ref data.renderingData, true);
                });
            }

            using (var builder = renderGraph.AddRenderPass<PassData>("Set Global GBuffer Textures", out var passData, m_ProfilingSampler))
            {
                passData.gbuffer = gbuffer = GetFrameResourcesGBufferArray(frameResources);

                passData.deferredLights = m_DeferredLights;

                for (int i = 0; i < m_DeferredLights.GBufferSliceCount; i++)
                {
                    if (i != m_DeferredLights.GBufferLightingIndex)
                        passData.gbuffer[i] = builder.ReadTexture(gbuffer[i]);
                }

                for (int i = 0; i < RenderGraphUtils.DBufferSize; ++i)
                {
                    var dbuffer = frameResources.GetTexture((UniversalResource) (UniversalResource.DBuffer0 + i));
                    if (dbuffer.IsValid())
                        builder.ReadTexture(dbuffer);
                }

                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    for (int i = 0; i < data.gbuffer.Length; i++)
                    {
                        if (i != data.deferredLights.GBufferLightingIndex)
                            context.cmd.SetGlobalTexture(DeferredLights.k_GBufferNames[i], data.gbuffer[i]);
                    }
                    // If any sub-system needs camera normal texture, make it available.
                    // Input attachments will only be used when this is not needed so safe to skip in that case
                    context.cmd.SetGlobalTexture(s_CameraNormalsTextureID, data.gbuffer[data.deferredLights.GBufferNormalSmoothnessIndex]);

                    if (data.deferredLights.UseRenderingLayers)
                    {
                        context.cmd.SetGlobalTexture(DeferredLights.k_GBufferNames[data.deferredLights.GBufferRenderingLayers], data.gbuffer[data.deferredLights.GBufferRenderingLayers]);
                        context.cmd.SetGlobalTexture("_CameraRenderingLayersTexture", data.gbuffer[data.deferredLights.GBufferRenderingLayers]);
                    }
                });
            }
        }
    }
}
