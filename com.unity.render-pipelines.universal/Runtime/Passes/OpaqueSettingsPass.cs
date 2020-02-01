namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Applies relevant settings before rendering opaque objects
    /// </summary>

    internal class OpaqueSettingsPass : ScriptableRenderPass
    {
        private Vector4 m_drawObjectPassData;

        private const string m_ProfilerTag = "Opaque Settings Pass";
        private static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");


        public OpaqueSettingsPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public bool Setup(bool hasDepthTexture)
        {
            // Global render pass data containing various settings.
            m_drawObjectPassData = new Vector4(
                0.0f,                           // Unused
                0.0f,                           // Unused
                hasDepthTexture ? 1.0f : 0.0f,  // Do we have access to a depth texture or not
                1.0f                            // Are these objects opaque(1) or alpha blended(0)
            );

            return true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get a command buffer...
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            
            // Send the correct draw data for the objects in drawn in this pass
            cmd.SetGlobalVector(s_DrawObjectPassDataPropID, m_drawObjectPassData);
            
            // Execute and release the command buffer...
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
