using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Perform final frame post-processing using the given color attachment
    /// as the source and the current camera target as the destination.
    ///
    /// You can use this pass to apply post-processing to the given color
    /// buffer. The pass uses the currently configured post-process stack,
    /// and it copies the result to the Camera target.
    /// </summary>
    public class TransparentPostProcessPass : ScriptableRenderPass
    {
        const string k_PostProcessingTag = "Render PostProcess Effects";
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private RenderTargetIdentifier destination { get; set; }

        /// <summary>
        /// Setup the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorAttachmentHandle">Source of rendering to execute the post on</param>
        /// <param name="destination">Destination target for the final blit</param>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetIdentifier destination)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.destination = destination;
            descriptor = baseDescriptor;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            CommandBuffer cmd = CommandBufferPool.Get(k_PostProcessingTag);
            renderer.RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, colorAttachmentHandle.Identifier(), destination, false);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
