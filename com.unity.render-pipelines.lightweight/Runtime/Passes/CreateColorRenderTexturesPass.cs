using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline {
    /// <summary>
    /// Generate rendering attachments that can be used for rendering.
    ///
    /// You can use this pass to generate valid rendering targets that
    /// the Lightweight Render Pipeline can use for rendering. For example,
    /// when you render a frame, the LWRP renders into a valid color and
    /// depth buffer.
    /// </summary>
    public class CreateColorRenderTexturesPass : ScriptableRenderPass
    {
        const string k_CreateRenderTexturesTag = "Create Render Texture";
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private SampleCount samples { get; set; }

        /// <summary>
        /// Configure the pass
        /// </summary>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            SampleCount samples)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.samples = samples;
            descriptor = baseDescriptor;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));

            CommandBuffer cmd = CommandBufferPool.Get(k_CreateRenderTexturesTag);
            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                var colorDescriptor = descriptor;
                colorDescriptor.depthBufferBits = 0;
                colorDescriptor.sRGB = true;
                colorDescriptor.msaaSamples = (int)samples;
                cmd.GetTemporaryRT(colorAttachmentHandle.id, colorDescriptor, FilterMode.Bilinear);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(colorAttachmentHandle.id);
                colorAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
