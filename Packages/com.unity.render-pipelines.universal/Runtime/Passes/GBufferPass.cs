using System;
using Unity.Collections;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        // Statics
        private static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
        private static readonly int s_CameraRenderingLayersTextureID = Shader.PropertyToID("_CameraRenderingLayersTexture");
        private static readonly ShaderTagId s_ShaderTagLit = new ShaderTagId("Lit");
        private static readonly ShaderTagId s_ShaderTagSimpleLit = new ShaderTagId("SimpleLit");
        private static readonly ShaderTagId s_ShaderTagUnlit = new ShaderTagId("Unlit");
        private static readonly ShaderTagId s_ShaderTagComplexLit = new ShaderTagId("ComplexLit");
        private static readonly ShaderTagId s_ShaderTagUniversalGBuffer = new ShaderTagId("UniversalGBuffer");
        private static readonly ShaderTagId s_ShaderTagUniversalMaterialType = new ShaderTagId("UniversalMaterialType");

        DeferredLights m_DeferredLights;

        static ShaderTagId[] s_ShaderTagValues;
        static RenderStateBlock[] s_RenderStateBlocks;

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
        {
            base.profilingSampler = new ProfilingSampler("Draw GBuffer");
            base.renderPassEvent = evt;

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
        
        static void ExecutePass(RasterCommandBuffer cmd, PassData data, RendererList rendererList, RendererList errorRendererList)
        {
            bool usesRenderingLayers = data.deferredLights.UseRenderingLayers && !data.deferredLights.HasRenderingLayerPrepass;
            if (usesRenderingLayers)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, true);

            bool useScreenSpaceIrradiance = data.screenSpaceIrradianceHdl.IsValid();
            cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceIrradiance, useScreenSpaceIrradiance);
            if (useScreenSpaceIrradiance)
            {
                cmd.SetGlobalTexture(ShaderPropertyId.screenSpaceIrradiance, data.screenSpaceIrradianceHdl);
            }

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
            internal DeferredLights deferredLights;
            internal RendererListHandle rendererListHdl;
            internal RendererListHandle objectsWithErrorRendererListHdl;

            internal TextureHandle screenSpaceIrradianceHdl;
        }

        private void InitRendererLists( ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, uint batchLayerMask = uint.MaxValue)
        {
            // User can stack several scriptable renderers during rendering but deferred renderer should only lit pixels added by this gbuffer pass.
            // If we detect we are in such case (camera is in overlay mode), we clear the highest bits of stencil we have control of and use them to
            // mark what pixel to shade during deferred pass. Gbuffer will always mark pixels using their material types.
            ShaderTagId lightModeTag = s_ShaderTagUniversalGBuffer;
            var drawingSettings = CreateDrawingSettings(lightModeTag, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            var filterSettings = m_FilteringSettings;
            filterSettings.batchLayerMask = batchLayerMask;
#if UNITY_EDITOR
            // When rendering the preview camera, we want the layer mask to be forced to Everything
            if (cameraData.isPreviewCamera)
                filterSettings.layerMask = -1;
#endif

            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            var param = new RendererListParams(renderingData.cullResults, drawingSettings, filterSettings)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                tagName = s_ShaderTagUniversalMaterialType,
                isPassTagName = false
            };
            passData.rendererListHdl = renderGraph.CreateRendererList(param);
            RenderingUtils.CreateRendererListObjectsWithError(renderGraph, ref renderingData.cullResults, cameraData.camera, filterSettings, SortingCriteria.None, ref passData.objectsWithErrorRendererListHdl);

            tagValues.Dispose();
            stateBlocks.Dispose();
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth, bool setGlobalTextures, uint batchLayerMask = uint.MaxValue)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler);
            bool useCameraRenderingLayersTexture = m_DeferredLights.UseRenderingLayers && !m_DeferredLights.UseLightLayers;

            var gbuffer = m_DeferredLights.GbufferTextureHandles;
            for (int i = 0; i < m_DeferredLights.GBufferSliceCount; i++)
            {
                Debug.Assert(gbuffer[i].IsValid());
                builder.SetRenderAttachment(gbuffer[i], i, AccessFlags.Write);
            }

            TextureHandle irradianceTexture = resourceData.irradianceTexture;
            if (irradianceTexture.IsValid())
            {
                passData.screenSpaceIrradianceHdl = irradianceTexture;
                builder.UseTexture(irradianceTexture, AccessFlags.Read);
            }

            RenderGraphUtils.UseDBufferIfValid(builder, resourceData);

            builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.ReadWrite);
            passData.deferredLights = m_DeferredLights;

            InitRendererLists(ref passData, default, renderGraph, renderingData, cameraData, lightData);
            builder.UseRendererList(passData.rendererListHdl);
            builder.UseRendererList(passData.objectsWithErrorRendererListHdl);

            if (setGlobalTextures)
            {
                builder.SetGlobalTextureAfterPass(resourceData.cameraNormalsTexture, s_CameraNormalsTextureID);

                if (useCameraRenderingLayersTexture)
                    builder.SetGlobalTextureAfterPass(resourceData.renderingLayersTexture, s_CameraRenderingLayersTextureID);
            }

            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                ExecutePass(context.cmd, data, data.rendererListHdl, data.objectsWithErrorRendererListHdl);
            });
        }
    }
}
