namespace UnityEngine.Rendering.LWRP
{
    internal class SceneViewDepthCopyPass : ScriptableRenderPass
    {
        private RenderTargetHandle source { get; set; }

        Material m_CopyDepthMaterial;
        const string m_ProfilerTag = "Copy Depth for Scene View";

        public SceneViewDepthCopyPass(RenderPassEvent evt, Material copyDepthMaterial)
        {
            m_CopyDepthMaterial = copyDepthMaterial;
            renderPassEvent = evt;
        }

        public void Setup(RenderTargetHandle source)
        {
            this.source = source;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CopyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_CopyDepthMaterial, GetType().Name);
                return;
            }

            // Restore Render target for additional editor rendering.
            // Note: Scene view camera always perform depth prepass
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());
            cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
            cmd.Blit(source.Identifier(), BuiltinRenderTextureType.CameraTarget, m_CopyDepthMaterial);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
