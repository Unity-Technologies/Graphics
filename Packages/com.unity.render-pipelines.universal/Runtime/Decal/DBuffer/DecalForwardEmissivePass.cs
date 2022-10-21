using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawFowardEmissiveSystem : DecalDrawSystem
    {
        public DecalDrawFowardEmissiveSystem(DecalEntityManager entityManager) : base("DecalDrawFowardEmissiveSystem.Execute", entityManager) { }
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexEmissive;
    }

    internal class DecalForwardEmissivePass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawFowardEmissiveSystem m_DrawSystem;
        private PassData m_PassData;

        public DecalForwardEmissivePass(DecalDrawFowardEmissiveSystem drawSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = drawSystem;
            m_ProfilingSampler = new ProfilingSampler("Decal Forward Emissive Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalMeshForwardEmissive));

            m_PassData = new PassData();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref m_PassData);
            ExecutePass(context, m_PassData, ref renderingData, renderingData.commandBuffer);
        }

        private class PassData
        {
            internal FilteringSettings filteringSettings;
            internal ProfilingSampler profilingSampler;
            internal List<ShaderTagId> shaderTagIdList;
            internal DecalDrawFowardEmissiveSystem drawSystem;

            internal RenderingData renderingData;
        }

        void InitPassData(ref PassData passData)
        {
            passData.filteringSettings = m_FilteringSettings;
            passData.profilingSampler = m_ProfilingSampler;
            passData.shaderTagIdList = m_ShaderTagIdList;
            passData.drawSystem = m_DrawSystem;
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

                passData.drawSystem.Execute(cmd);

                cmd.DrawRendererList(rl);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Decal Forward Emissive Pass", out var passData, m_ProfilingSampler))
            {
                InitPassData(ref passData);
                passData.renderingData = renderingData;

                builder.UseColorBuffer(UniversalRenderer.m_ActiveRenderGraphColor, 0);
                builder.UseDepthBuffer(UniversalRenderer.m_ActiveRenderGraphDepth, DepthAccess.Read);

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.renderContext, data, ref data.renderingData, data.renderingData.commandBuffer);
                });
            }
        }
    }
}
