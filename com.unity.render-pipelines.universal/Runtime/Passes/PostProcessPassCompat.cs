namespace UnityEngine.Rendering.Universal
{
    internal class PostProcessPassCompat : ScriptableRenderPass
    {
        RenderTargetHandle m_Source;
        RenderTargetHandle m_Destination;
        RenderTextureDescriptor m_Descriptor;

        RenderTargetHandle m_TemporaryColorTexture;
        bool m_IsOpaquePostProcessing;

        const string k_RenderPostProcessingTag = "Render PostProcessing Effects (Compat)";

        public PostProcessPassCompat(RenderPassEvent evt, bool renderOpaques = false)
        {
            m_IsOpaquePostProcessing = renderOpaques;
            m_TemporaryColorTexture.Init("_TemporaryColorTexture");

            renderPassEvent = evt;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle sourceHandle, RenderTargetHandle destinationHandle)
        {
            m_Descriptor = baseDescriptor;
            m_Source = sourceHandle;
            m_Destination = destinationHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if POST_PROCESSING_STACK_2_0_0_OR_NEWER
#pragma warning disable 0618 // Obsolete
            ref CameraData cameraData = ref renderingData.cameraData;
            bool isLastRenderPass = (m_Destination == RenderTargetHandle.CameraTarget);
            bool flip = isLastRenderPass && cameraData.camera.targetTexture == null;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderPostProcessingTag);
            RenderPostProcessing(cmd, ref renderingData.cameraData, m_Descriptor, m_Source.Identifier(),
                    m_Destination.Identifier(), m_IsOpaquePostProcessing, flip);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
#pragma warning restore 0618
#endif
        }
    }
}
