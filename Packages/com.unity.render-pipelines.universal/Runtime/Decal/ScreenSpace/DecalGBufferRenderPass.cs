using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawGBufferSystem : DecalDrawSystem
    {
        public DecalDrawGBufferSystem(DecalEntityManager entityManager) : base("DecalDrawGBufferSystem.Execute", entityManager) { }
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexGBuffer;
    }

    internal class DecalGBufferRenderPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawGBufferSystem m_DrawSystem;
        private DecalScreenSpaceSettings m_Settings;
        private DeferredLights m_DeferredLights;
        private bool m_DecalLayers;

        public DecalGBufferRenderPass(DecalScreenSpaceSettings settings, DecalDrawGBufferSystem drawSystem, bool decalLayers)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;

            m_DrawSystem = drawSystem;
            m_Settings = settings;
            profilingSampler = new ProfilingSampler("Draw Decal To GBuffer");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            m_DecalLayers = decalLayers;

            m_ShaderTagIdList = new List<ShaderTagId>();
            if (drawSystem == null)
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalGBufferProjector));
            else
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalGBufferMesh));
        }

        internal void Setup(DeferredLights deferredLights)
        {
            m_DeferredLights = deferredLights;
        }

        private class PassData
        {
            internal DecalDrawGBufferSystem drawSystem;
            internal DecalScreenSpaceSettings settings;
            internal bool decalLayers;

            internal UniversalCameraData cameraData;
            internal RendererListHandle rendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.decalLayers = m_DecalLayers;
            passData.cameraData = cameraData;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {
            NormalReconstruction.SetupProperties(cmd, passData.cameraData);

            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendLow, passData.settings.normalBlend == DecalNormalBlend.Low);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendMedium, passData.settings.normalBlend == DecalNormalBlend.Medium);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendHigh, passData.settings.normalBlend == DecalNormalBlend.High);

            cmd.SetKeyword(ShaderGlobalKeywords.DecalLayers, passData.decalLayers);

            passData.drawSystem?.Execute(cmd);
            cmd.DrawRendererList(rendererList);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle renderingLayersTexture = resourceData.renderingLayersTexture;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                InitPassData(cameraData, ref passData);

                // GBuffers 0 - 4
                for (int i = 0; i <= m_DeferredLights.GBufferLightingIndex; i++)
                {
                    if (resourceData.gBuffer[i].IsValid())
                    {
                        builder.SetRenderAttachment(resourceData.gBuffer[i], i, AccessFlags.Write);
                    }
                }
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                if (renderGraph.nativeRenderPassesEnabled)
                {
                    if (resourceData.gBuffer[4].IsValid())
                        builder.SetInputAttachment(resourceData.gBuffer[4], 0);
                    if (m_DecalLayers && resourceData.gBuffer[5].IsValid())
                        builder.SetInputAttachment(resourceData.gBuffer[5], 1);
                }
                else
                {
                    if (cameraDepthTexture.IsValid())
                        builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                    if(m_DecalLayers && renderingLayersTexture.IsValid())
                        builder.UseTexture(renderingLayersTexture, AccessFlags.Read);
                }

                SortingCriteria sortingCriteria = passData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData,
                    passData.cameraData, lightData, sortingCriteria);
                var param = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.cmd, data, data.rendererList);
                });
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendLow, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendMedium, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendHigh, false);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalLayers, false);
        }
    }
}
