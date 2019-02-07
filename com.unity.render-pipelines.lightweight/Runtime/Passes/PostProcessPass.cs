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
        const string k_PostProcessingTag = "Render PostProcess Effects";
        private RenderTargetHandle source { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private RenderTargetHandle destination { get; set; }
        bool m_ReleaseTemporaryRenderTexture;

        RenderTargetHandle m_TemporaryColorTexture;
        bool m_IsOpaquePostProcessing;
        bool m_IsLastRenderPass;

        public PostProcessPass(bool renderOpaques = false)
        {
            m_IsOpaquePostProcessing = renderOpaques;
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
            RenderTargetHandle destinationHandle,
            bool isLastRenderPass)
        {
            source = sourceHandle;
            destination = destinationHandle;
            descriptor = baseDescriptor;
            m_TemporaryColorTexture.Init("_TemporaryColorTexture");
            m_ReleaseTemporaryRenderTexture = false;
            m_IsLastRenderPass = isLastRenderPass;
        }

        public override bool ShouldExecute(ref RenderingData renderingData)
        {
            return renderingData.cameraData.postProcessEnabled &&
                   (!m_IsOpaquePostProcessing || renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(postProcessRenderContext));
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_PostProcessingTag);

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
            if (effectsCount == 1 && source.id == destination.id)
            {
                m_ReleaseTemporaryRenderTexture = true;
                cmd.GetTemporaryRT(m_TemporaryColorTexture.id, descriptor, FilterMode.Point);
                RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, source.Identifier(),
                    m_TemporaryColorTexture.Identifier(), m_IsOpaquePostProcessing, m_IsLastRenderPass);
                cmd.Blit(m_TemporaryColorTexture.Identifier(), source.Identifier());
            }
            else
            {
                RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, source.Identifier(),
                    destination.Identifier(), m_IsOpaquePostProcessing, m_IsLastRenderPass);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (m_ReleaseTemporaryRenderTexture)
                cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
        }
    }
}
