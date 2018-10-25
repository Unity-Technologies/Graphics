using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Perform post-processing using the given color attachment
    /// as the source and the given color attachment as the destination.
    ///
    /// You can use this pass to apply post-processing to the given color
    /// buffer. The pass uses the currently configured post-process stack.
    /// </summary>
    public class PostProcessPass : ScriptableRenderPass
    {
        const string k_PostProcessingTag = "Render PostProcess Effects";
        private RenderTargetHandle source { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private RenderTargetHandle destination { get; set; }
        private bool flip { get; set; }
        bool opaquePost { get; set; }

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
            if (sourceHandle == destinationHandle)
                throw new InvalidOperationException($"{nameof(sourceHandle)} should not be the same as {nameof(destinationHandle)}");

            source = sourceHandle;
            destination = destinationHandle;
            descriptor = baseDescriptor;
            this.flip = flip;
            this.opaquePost = opaquePost;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            
            CommandBuffer cmd = CommandBufferPool.Get(k_PostProcessingTag);
            renderer.RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, source.Identifier(), destination.Identifier(), opaquePost, flip);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
