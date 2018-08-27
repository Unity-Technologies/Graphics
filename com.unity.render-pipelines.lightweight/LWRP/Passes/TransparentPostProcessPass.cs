using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class TransparentPostProcessPass : ScriptableRenderPass
    {
        const string k_PostProcessingTag = "Render PostProcess Effects";
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private RenderTargetIdentifier destination { get; set; }

        public void Setup(
            PostProcessRenderContext postProcessRenderContext,
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetIdentifier destination)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.destination = destination;
            descriptor = baseDescriptor;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_PostProcessingTag);
            renderer.RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, colorAttachmentHandle.Identifier(), destination, false);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
