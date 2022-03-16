using UnityEngine.Rendering.UI;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawNormal2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Draw 2D Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private LayerBatch m_LayerBatch;
        private UniversalRenderer2D.Attachments m_Attachments;

        public DrawNormal2DPass(LayerBatch layerBatch, UniversalRenderer2D.Attachments attachments)
        {
            m_LayerBatch = layerBatch;
            m_Attachments = attachments;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var filterSettings = new FilteringSettings();
                filterSettings.renderQueueRange = RenderQueueRange.all;
                filterSettings.layerMask = -1;
                filterSettings.renderingLayerMask = 0xFFFFFFFF;
                filterSettings.sortingLayerRange = new SortingLayerRange(m_LayerBatch.layerRange.lowerBound, m_LayerBatch.layerRange.upperBound);
                var drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                CoreUtils.SetRenderTarget(cmd, m_Attachments.normalAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Color, RendererLighting.k_NormalClearColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
