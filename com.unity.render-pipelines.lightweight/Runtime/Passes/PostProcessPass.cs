using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Perform post-processing using the given color attachment
    /// as the source and the given color attachment as the destination.
    ///
    /// You can use this pass to apply post-processing to the given color
    /// buffer. The pass uses the currently configured post-process stack.
    /// </summary>
    internal class PostProcessPass : ScriptableRenderPass
    {
        RenderTargetHandle m_Source;
        RenderTargetHandle m_Destination;
        RenderTextureDescriptor m_Descriptor;

        RenderTargetHandle m_TemporaryColorTexture;
        bool m_IsOpaquePostProcessing;
        
        const string k_RenderPostProcessingTag = "Render PostProcessing Effects";

        public PostProcessPass(RenderPassEvent evt, bool renderOpaques = false)
        {
            m_IsOpaquePostProcessing = renderOpaques;
            m_TemporaryColorTexture.Init("_TemporaryColorTexture");

            renderPassEvent = evt;
        }

        /// <summary>
        /// Setup the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="sourceHandle">Source of rendering to execute the post on</param>
        /// <param name="destinationHandle">Destination target for the final blit</param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle sourceHandle, RenderTargetHandle destinationHandle)
        {
            m_Descriptor = baseDescriptor;
            m_Source = sourceHandle;
            m_Destination = destinationHandle;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            bool isLastRenderPass = (m_Destination == RenderTargetHandle.CameraTarget);
            bool flip = isLastRenderPass && cameraData.camera.targetTexture == null;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderPostProcessingTag);
            RenderPostProcessing(cmd, ref renderingData.cameraData, m_Descriptor, m_Source.Identifier(),
                    m_Destination.Identifier(), m_IsOpaquePostProcessing, flip);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
