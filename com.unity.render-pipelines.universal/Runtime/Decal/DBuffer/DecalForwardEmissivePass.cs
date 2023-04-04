using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
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
            var param = InitRendererListParams(ref renderingData);

            var rendererList = context.CreateRendererList(ref param);
            using (new ProfilingScope(renderingData.commandBuffer, m_ProfilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList, ref renderingData);
            }
        }

        private class PassData
        {
            internal DecalDrawFowardEmissiveSystem drawSystem;

            internal RenderingData renderingData;
            internal RendererListHandle rendererList;
        }

        private void InitPassData(ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
        }

        private RendererListParams InitRendererListParams(ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            return new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList, ref RenderingData renderingData)
        {
            passData.drawSystem.Execute(cmd);
            cmd.DrawRendererList(rendererList);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Decal Forward Emissive Pass", out var passData, m_ProfilingSampler))
            {
                InitPassData(ref passData);
                var param = InitRendererListParams(ref renderingData);
                passData.renderingData = renderingData;
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;
                builder.UseTextureFragment(renderer.activeColorTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentDepth(renderer.activeDepthTexture, IBaseRenderGraphBuilder.AccessFlags.Read);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.cmd, data, data.rendererList, ref data.renderingData);
                });
            }
        }
    }
}
