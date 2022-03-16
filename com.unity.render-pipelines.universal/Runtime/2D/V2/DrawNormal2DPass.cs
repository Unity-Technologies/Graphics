namespace UnityEngine.Rendering.Universal
{
    internal class DrawNormal2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Draw 2D Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

        private LayerBatch m_LayerBatch;
        private readonly UniversalRenderer2D.GBuffers m_GBuffers;

        public DrawNormal2DPass(LayerBatch layerBatch, UniversalRenderer2D.GBuffers buffers)
        {
            m_LayerBatch = layerBatch;
            m_GBuffers = buffers;
            useNativeRenderPass = true;
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

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(m_GBuffers.buffers[UniversalRenderer2D.GBuffers.k_NormalTexture], m_GBuffers.depthAttachment);
            ConfigureClear(ClearFlag.None, Color.black);
        }
    }
}
