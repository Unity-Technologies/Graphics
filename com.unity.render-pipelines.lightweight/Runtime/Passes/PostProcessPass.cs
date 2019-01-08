using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LWRP
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
        private bool flip { get; set; }
        bool opaquePost { get; set; }

        bool m_ReleaseTemporaryRenderTexture;

        RenderTargetHandle m_TemporaryColorTexture;

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
            bool opaquePost,
            bool flip)
        {
            source = sourceHandle;
            destination = destinationHandle;
            descriptor = baseDescriptor;
            this.flip = flip;
            this.opaquePost = opaquePost;
            m_TemporaryColorTexture.Init("_TemporaryColorTexture");
            m_ReleaseTemporaryRenderTexture = false;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));

            CommandBuffer cmd = CommandBufferPool.Get(k_PostProcessingTag);

            var layer = renderingData.cameraData.postProcessLayer;
            int effectsCount;
            if (opaquePost)
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
                renderer.RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, source.Identifier(), m_TemporaryColorTexture.Identifier(), opaquePost, flip);
                cmd.Blit(m_TemporaryColorTexture.Identifier(), source.Identifier());
            }
            else
            {
                renderer.RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, source.Identifier(), destination.Identifier(), opaquePost, flip);
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
