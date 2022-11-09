using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalPreviewPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;
        private ProfilingSampler m_ProfilingSampler;

        private PassData m_PassData;

        public DecalPreviewPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_ProfilingSampler = new ProfilingSampler("Decal Preview Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceMesh));

            m_PassData = new PassData();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref m_PassData);
            ExecutePass(context, m_PassData, ref renderingData, renderingData.commandBuffer);
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData, CommandBuffer cmd)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(passData.shaderTagIdList, ref renderingData, sortingCriteria);
            var param = new RendererListParams(renderingData.cullResults, drawingSettings, passData.filteringSettings);
            var rl = context.CreateRendererList(ref param);

            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.DrawRendererList(rl);
            }
        }

        private class PassData
        {
            internal FilteringSettings filteringSettings;
            internal List<ShaderTagId> shaderTagIdList;
            internal ProfilingSampler profilingSampler;

            internal RenderingData renderingData;
        }

        void InitPassData(ref PassData passData)
        {
            passData.filteringSettings = m_FilteringSettings;
            passData.shaderTagIdList = m_ShaderTagIdList;
            passData.profilingSampler = m_ProfilingSampler;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Decal Preview Pass", out var passData, m_ProfilingSampler))
            {
                InitPassData(ref passData);
                passData.renderingData = renderingData;

                UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

                builder.UseColorBuffer(renderer.activeColorTexture, 0);
                builder.UseDepthBuffer(renderer.activeDepthTexture, DepthAccess.Read);

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.renderContext, data, ref data.renderingData, rgContext.cmd);
                });
            }
        }
    }
}
