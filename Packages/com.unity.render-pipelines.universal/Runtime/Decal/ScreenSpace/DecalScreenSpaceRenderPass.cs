using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawScreenSpaceSystem : DecalDrawSystem
    {
        public DecalDrawScreenSpaceSystem(DecalEntityManager entityManager) : base("DecalDrawScreenSpaceSystem.Execute", entityManager) { }
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexScreenSpace;
    }

    internal class DecalScreenSpaceRenderPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawScreenSpaceSystem m_DrawSystem;
        private DecalScreenSpaceSettings m_Settings;
        private bool m_DecalLayers;
        private PassData m_PassData;

        public DecalScreenSpaceRenderPass(DecalScreenSpaceSettings settings, DecalDrawScreenSpaceSystem drawSystem, bool decalLayers)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

            var scriptableRenderPassInput = ScriptableRenderPassInput.Depth; // Require depth
            ConfigureInput(scriptableRenderPassInput);

            m_DrawSystem = drawSystem;
            m_Settings = settings;
            profilingSampler = new ProfilingSampler("Draw Decal Screen Space");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            m_DecalLayers = decalLayers;

            m_ShaderTagIdList = new List<ShaderTagId>();

            if (m_DrawSystem == null)
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceProjector));
            else
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceMesh));

            m_PassData = new PassData();
        }

        private RendererListParams CreateRenderListParams(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            SortingCriteria sortingCriteria = SortingCriteria.None;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, sortingCriteria);
            return new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();

            InitPassData(cameraData, ref m_PassData);
            RenderingUtils.SetScaleBiasRt(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), in renderingData);
            UniversalRenderingData universalRenderingData = renderingData.frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = renderingData.frameData.Get<UniversalLightData>();
            var param = CreateRenderListParams(universalRenderingData, cameraData, lightData);
            var rendererList = context.CreateRendererList(ref param);
            using (new ProfilingScope(renderingData.commandBuffer, profilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList);
            }
        }

        private class PassData
        {
            internal DecalDrawScreenSpaceSystem drawSystem;
            internal DecalScreenSpaceSettings settings;
            internal bool decalLayers;
            internal bool isGLDevice;
            internal TextureHandle colorTarget;

            internal UniversalCameraData cameraData;
            internal RendererListHandle rendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.decalLayers = m_DecalLayers;
            passData.isGLDevice = DecalRendererFeature.isGLDevice;
            passData.cameraData = cameraData;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {
            NormalReconstruction.SetupProperties(cmd, passData.cameraData);

            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendLow, passData.settings.normalBlend == DecalNormalBlend.Low);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendMedium, passData.settings.normalBlend == DecalNormalBlend.Medium);
            cmd.SetKeyword(ShaderGlobalKeywords.DecalNormalBlendHigh, passData.settings.normalBlend == DecalNormalBlend.High);

            if (!passData.isGLDevice)
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
                passData.colorTarget = resourceData.cameraColor;
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);


                var param = CreateRenderListParams(renderingData, passData.cameraData, lightData);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                if (cameraDepthTexture.IsValid())
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);

                if (passData.decalLayers && renderingLayersTexture.IsValid())
                    builder.UseTexture(renderingLayersTexture, AccessFlags.Read);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    RenderingUtils.SetScaleBiasRt(rgContext.cmd, in data.cameraData, data.colorTarget);
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
