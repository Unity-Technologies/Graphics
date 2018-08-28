using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class OpaquePostProcessPass : ScriptableRenderPass
    {
        const string k_OpaquePostProcessTag = "Render Opaque PostProcess Effects";
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        public void Setup(
            PostProcessRenderContext postProcessRenderContext,
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            descriptor = baseDescriptor;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_OpaquePostProcessTag);

            RenderTargetIdentifier source = colorAttachmentHandle.Identifier();
            renderer.RenderPostProcess(cmd, ref renderingData.cameraData, descriptor.colorFormat, source, colorAttachmentHandle.Identifier(), true);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
