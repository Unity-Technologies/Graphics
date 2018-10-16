using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Perform Opaque post-processing using the given color attachment as the source
    /// and destination
    ///
    /// You can use this pass to apply post-processing to the given color buffer. The
    /// pass uses the currently configured post-process stack, and it copies the result
    /// back to the source texture.
    /// </summary>
    public class OpaquePostProcessPass : ScriptableRenderPass
    {
        const string k_OpaquePostProcessTag = "Render Opaque PostProcess Effects";
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        /// <summary>
        /// Setup the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorAttachmentHandle"></param>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            descriptor = baseDescriptor;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            CommandBuffer cmd = CommandBufferPool.Get(k_OpaquePostProcessTag);

            RenderTargetIdentifier source = colorAttachmentHandle.Identifier();
            renderer.RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, source, colorAttachmentHandle.Identifier(), true);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
