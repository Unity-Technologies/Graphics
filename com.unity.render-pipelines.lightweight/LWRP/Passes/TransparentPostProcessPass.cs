using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class TransparentPostProcessPass : ScriptableRenderPass
    {
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private PostProcessRenderContext postContext { get; set; }

        public void Setup(
            PostProcessRenderContext postProcessRenderContext,
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.postContext = postProcessRenderContext;
            descriptor = baseDescriptor;
        }

        public override void Execute(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render PostProcess Effects");
            LightweightPipeline.RenderPostProcess(cmd, postContext, ref renderingData.cameraData, descriptor.colorFormat, colorAttachmentHandle.Identifier(), BuiltinRenderTextureType.CameraTarget, false);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
