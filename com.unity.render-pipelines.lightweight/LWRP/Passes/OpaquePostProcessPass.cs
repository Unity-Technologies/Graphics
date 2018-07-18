using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class OpaquePostProcessPass : ScriptableRenderPass
    {

        public OpaquePostProcessPass(LightweightForwardRenderer renderer) : base(renderer)
        {}

        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            descriptor = baseDescriptor;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Opaque PostProcess Effects");

            RenderTargetIdentifier source = colorAttachmentHandle.Identifier();
            LightweightPipeline.RenderPostProcess(cmd, renderer.postProcessRenderContext, ref renderingData.cameraData, descriptor.colorFormat, source, colorAttachmentHandle.Identifier(), true);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
