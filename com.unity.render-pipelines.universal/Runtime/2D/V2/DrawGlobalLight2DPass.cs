namespace UnityEngine.Rendering.Universal
{
    internal class DrawGlobalLight2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingDrawLights = new ProfilingSampler("Draw 2D GLobal Lights");
        private readonly Renderer2DData m_RendererData;
        private Material m_Material;
        private LayerBatch m_LayerBatch;
        private static string[] ColorNames = {"_Color0", "_Color1", "_Color2", "_Color3"};

        public DrawGlobalLight2DPass(Material material, bool isNative)
        {
            m_Material = material;
            useNativeRenderPass = isNative;
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

        public void Setup(LayerBatch layerBatch)
        {
            m_LayerBatch = layerBatch;
        }

    }
}
