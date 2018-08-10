using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTargetHandle depthAttachmentHandle { get; set; }
        
        public void Setup(RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
        {
            this.colorAttachmentHandle = colorHandle;
            this.depthAttachmentHandle = depthHandle;
        }
        
        public override void Execute(ScriptableRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            
            CommandBuffer cmd = CommandBufferPool.Get("Draw Skybox (Set RT's)");
            cmd.SetRenderTarget(colorAttachmentHandle.Identifier(), depthAttachmentHandle.Identifier());
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
            context.DrawSkybox(renderingData.cameraData.camera);
        }

    }
}