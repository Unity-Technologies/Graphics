namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Applies relevant settings before rendering transparent objects
    /// </summary>

    internal class TransparentSettingsPass : ScriptableRenderPass
    {
        bool m_shouldReceiveShadows;
        bool m_shouldScreenSpaceShadows; //seongdae;oneMainShadow

        const string m_ProfilerTag = "Transparent Settings Pass";


        public TransparentSettingsPass(RenderPassEvent evt, bool shadowReceiveSupported)
        {
            renderPassEvent = evt;
            m_shouldReceiveShadows = shadowReceiveSupported;
        }

        public bool Setup(ref RenderingData renderingData)
        {
            // Currently we only need to enqueue this pass when the user
            // doesn't want transparent objects to receive shadows
            //return !m_shouldReceiveShadows; //seongdae;oneMainShadow

            // Currently we only need to enqueue this pass when the user
            // doesn't want transparent objects to receive shadows or turns on screen space shadows
            m_shouldScreenSpaceShadows = renderingData.shadowData.requiresScreenSpaceShadowResolve; //seongdae;oneMainShadow
            return !m_shouldReceiveShadows || m_shouldScreenSpaceShadows; //seongdae;oneMainShadow
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get a command buffer...
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            //seongdae;oneMainShadow
            // Toggle light's shadows based on the renderer setting set by receiving shadows or screen space shadows
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, m_shouldReceiveShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, !m_shouldScreenSpaceShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, m_shouldReceiveShadows);

            // Toggle light shadows enabled based on the renderer setting set in the constructor
            //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, m_shouldReceiveShadows);
            //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, m_shouldReceiveShadows);
            //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, m_shouldReceiveShadows);
            //seongdae;oneMainShadow

            // Execute and release the command buffer...
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
