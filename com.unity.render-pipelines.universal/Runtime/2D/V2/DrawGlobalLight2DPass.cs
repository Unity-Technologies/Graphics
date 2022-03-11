namespace UnityEngine.Rendering.Universal
{
    internal class DrawGlobalLight2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingDrawLights = new ProfilingSampler("Draw 2D GLobal Lights");
        private readonly Renderer2DData m_RendererData;
        private LayerBatch m_LayerBatch;
        private RTHandle[] m_GBuffers;
        private Material m_Material;
        private RTHandle m_DepthHandle;
        private static string[] ColorNames = {"_Color0", "_Color1", "_Color2", "_Color3"};

        public DrawGlobalLight2DPass(Renderer2DData rendererData, Material material)
        {
            this.m_RendererData = rendererData;
            m_Material = material;
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
                // cmd.EnableShaderKeyword("_USE_DRAW_PROCEDURAL");
                // cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Quads, 4, 1);
                // cmd.DisableShaderKeyword("_USE_DRAW_PROCEDURAL");
                float flipSign = false ? -1.0f : 1.0f;
                Vector4 scaleBiasRt = (flipSign < 0.0f)
                    ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                    : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBiasRt);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(m_GBuffers, m_DepthHandle);
        }

        public void Setup(LayerBatch layerBatch, RTHandle[] gbuffers, RTHandle depthHandle)
        {
            this.m_LayerBatch = layerBatch;
            this.m_GBuffers = gbuffers;
            this.m_DepthHandle = depthHandle;
        }
    }
}
