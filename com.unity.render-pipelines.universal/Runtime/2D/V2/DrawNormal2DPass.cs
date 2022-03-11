using UnityEngine.Rendering.UI;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawNormal2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Draw 2D Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private LayerBatch layerBatch;
        private RTHandle normalTarget;
        private RTHandle depthTexture;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var filterSettings = new FilteringSettings();
                filterSettings.renderQueueRange = RenderQueueRange.all;
                filterSettings.layerMask = -1;
                filterSettings.renderingLayerMask = 0xFFFFFFFF;
                filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound);
                var drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                cmd.SetRenderTarget(
                    normalTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    depthTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                cmd.ClearRenderTarget(true, true, RendererLighting.k_NormalClearColor);

                // CoreUtils.SetRenderTarget(cmd, normalTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Color, RendererLighting.k_NormalClearColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     ConfigureTarget(normalTexture, depthTexture);
        //     ConfigureClear(ClearFlag.Color, RendererLighting.k_NormalClearColor);
        // }

        public void Setup(LayerBatch layerBatch, RTHandle normalTexture, RTHandle depthTextureHandle)
        {
            this.layerBatch = layerBatch;
            this.normalTarget = normalTexture;
            this.depthTexture = depthTextureHandle;
        }
    }
}
