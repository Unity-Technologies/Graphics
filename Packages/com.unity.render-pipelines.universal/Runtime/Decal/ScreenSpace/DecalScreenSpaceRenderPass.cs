using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
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
        private ProfilingSampler m_ProfilingSampler;
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
            m_ProfilingSampler = new ProfilingSampler("Decal Screen Space Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            m_DecalLayers = decalLayers;

            m_ShaderTagIdList = new List<ShaderTagId>();

            if (m_DrawSystem == null)
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceProjector));
            else
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceMesh));

            m_PassData = new PassData();
        }

        private RendererListParams CreateRenderListParams(ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = SortingCriteria.None;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            return new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref renderingData, ref m_PassData);
            RenderingUtils.SetScaleBiasRt(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), in renderingData);
            var param = CreateRenderListParams(ref renderingData);
            var rendererList = context.CreateRendererList(ref param);
            using (new ProfilingScope(renderingData.commandBuffer, m_ProfilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList, ref renderingData);
            }
        }

        private class PassData
        {
            internal DecalDrawScreenSpaceSystem drawSystem;
            internal DecalScreenSpaceSettings settings;
            internal bool decalLayers;
            internal bool isGLDevice;
            internal TextureHandle colorTarget;

            internal RenderingData renderingData;
            internal RendererListHandle rendererList;
        }

        private void InitPassData(ref RenderingData renderingData, ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
            passData.settings = m_Settings;
            passData.decalLayers = m_DecalLayers;
            passData.isGLDevice = DecalRendererFeature.isGLDevice;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList, ref RenderingData renderingData)
        {
            NormalReconstruction.SetupProperties(cmd, renderingData.cameraData);

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendLow, passData.settings.normalBlend == DecalNormalBlend.Low);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendMedium, passData.settings.normalBlend == DecalNormalBlend.Medium);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendHigh, passData.settings.normalBlend == DecalNormalBlend.High);

            if (!passData.isGLDevice)
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalLayers, passData.decalLayers);

            passData.drawSystem?.Execute(cmd);
            cmd.DrawRendererList(rendererList);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            TextureHandle cameraDepthTexture = frameResources.GetTexture(UniversalResource.CameraDepthTexture);

            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID("_CameraDepthTexture"), cameraDepthTexture);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Decal Screen Space Pass", out var passData, m_ProfilingSampler))
            {
                TextureHandle cameraColor = frameResources.GetTexture(UniversalResource.CameraColor);

                InitPassData(ref renderingData, ref passData);
                passData.colorTarget = cameraColor;
                passData.renderingData = renderingData;

                builder.UseTextureFragment(renderer.activeColorTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentDepth(renderer.activeDepthTexture, IBaseRenderGraphBuilder.AccessFlags.Read);

                var param = CreateRenderListParams(ref renderingData);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                if (cameraDepthTexture.IsValid())
                    builder.UseTexture(cameraDepthTexture, IBaseRenderGraphBuilder.AccessFlags.Read);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    RenderingUtils.SetScaleBiasRt(rgContext.cmd, in data.renderingData, data.colorTarget);
                    ExecutePass(rgContext.cmd, data, data.rendererList, ref data.renderingData);
                });
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendLow, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendMedium, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalNormalBlendHigh, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DecalLayers, false);

        }
    }
}
