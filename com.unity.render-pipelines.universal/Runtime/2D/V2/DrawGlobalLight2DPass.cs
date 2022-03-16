namespace UnityEngine.Rendering.Universal
{
    internal class DrawGlobalLight2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingDrawLights = new ProfilingSampler("Draw 2D GLobal Lights");

        private readonly UniversalRenderer2D.GBuffers m_GBuffers;
        private readonly Renderer2DData m_RendererData;
        private Material m_Material;
        private LayerBatch m_LayerBatch;
        private static string[] ColorNames = {"_Color0", "_Color1", "_Color2", "_Color3"};

        public DrawGlobalLight2DPass(Material material, LayerBatch layerBatch, UniversalRenderer2D.GBuffers buffers)
        {
            m_GBuffers = buffers;
            m_Material = material;
            m_LayerBatch = layerBatch;
            useNativeRenderPass = true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingDrawLights))
            {
                for (var blendStyleIndex = 0; blendStyleIndex < 4; blendStyleIndex++)
                {
                    cmd.SetGlobalColor(ColorNames[blendStyleIndex], m_LayerBatch.clearColors[blendStyleIndex]);
                }
                cmd.SetGlobalColor("_NormalColor", RendererLighting.k_NormalClearColor);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(m_GBuffers.buffers, m_GBuffers.depthAttachment, m_GBuffers.formats);
            ConfigureClear(ClearFlag.None, Color.black);
        }
    }
}
