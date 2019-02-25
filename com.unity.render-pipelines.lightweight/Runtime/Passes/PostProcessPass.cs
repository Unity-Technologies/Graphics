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
        string m_ProfilerTag = "Render PostProcess Effects";

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
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle sourceHandle,
            RenderTargetHandle destinationHandle)
        {
            m_Descriptor = baseDescriptor;
            m_Source = sourceHandle;
            m_Destination = destinationHandle;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            bool isLastRenderPass = (m_Destination == RenderTargetHandle.CameraTarget) && !cameraData.isStereoEnabled;
            bool flip = isLastRenderPass && cameraData.camera.targetTexture == null;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            var layer = renderingData.cameraData.postProcessLayer;
            int effectsCount;
            if (m_IsOpaquePostProcessing)
            {
                effectsCount = layer.sortedBundles[PostProcessEvent.BeforeTransparent].Count;
            }
            else
            {
                effectsCount = layer.sortedBundles[PostProcessEvent.BeforeStack].Count +
                               layer.sortedBundles[PostProcessEvent.AfterStack].Count;
            }

            // If there's only one effect in the stack and soure is same as dest we
            // create an intermediate blit rendertarget to handle it.
            // Otherwise, PostProcessing system will create the intermediate blit targets itself.
            if (effectsCount == 1 && m_Source.id == m_Destination.id)
            {
                cmd.GetTemporaryRT(m_TemporaryColorTexture.id, m_Descriptor, FilterMode.Point);
                RenderingUtils.RenderPostProcess(cmd, ref renderingData.cameraData, m_Descriptor.colorFormat, m_Source.Identifier(),
                    m_TemporaryColorTexture.Identifier(), m_IsOpaquePostProcessing, flip);
                cmd.Blit(m_TemporaryColorTexture.Identifier(), m_Source.Identifier());
                cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
            }
            else
            {
                RenderingUtils.RenderPostProcess(cmd, ref renderingData.cameraData, m_Descriptor.colorFormat, m_Source.Identifier(),
                    m_Destination.Identifier(), m_IsOpaquePostProcessing, flip);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
