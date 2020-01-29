namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Applies relevant settings before rendering opaque objects
    /// </summary>

    internal class OpaqueSettingsPass : ScriptableRenderPass
    {
        private bool m_HasDepthTexture;

        private const string m_ProfilerTag = "Opaque Settings Pass";
        private static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");


        public OpaqueSettingsPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public bool Setup(bool hasDepthTexture)
        {
            m_HasDepthTexture = hasDepthTexture;

            return true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get a command buffer...
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            // Global render pass data containing various settings.
            Vector4 drawObjectPassData = new Vector4(
                0.0f,                           // Unused
                0.0f,                           // Unused
                m_HasDepthTexture ? 1.0f : 0.0f,// Do we have access to a depth texture or not
                1.0f                            // Are these objects opaque(1) or alpha blended(0)
            );
            cmd.SetGlobalVector(s_DrawObjectPassDataPropID, drawObjectPassData);

            // Execute and release the command buffer...
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
