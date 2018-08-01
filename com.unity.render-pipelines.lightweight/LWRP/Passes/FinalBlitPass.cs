using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class FinalBlitPass : ScriptableRenderPass
    {
        const string k_FinalBlitTag = "Final Blit Pass";

        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.descriptor = baseDescriptor;
        }

        public override void Execute(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            Material material = renderingData.cameraData.isStereoEnabled ? null : renderer.GetMaterial(MaterialHandles.Blit);
            RenderTargetIdentifier sourceRT = colorAttachmentHandle.Identifier();

            CommandBuffer cmd = CommandBufferPool.Get(k_FinalBlitTag);
            cmd.SetGlobalTexture("_BlitTex", sourceRT);

            // We need to handle viewport on a RT. We do it by rendering a fullscreen quad + viewport
            if (!renderingData.cameraData.isDefaultViewport)
            {
                SetRenderTarget(
                    cmd,
                    BuiltinRenderTextureType.CameraTarget,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    ClearFlag.None,
                    Color.black,
                    descriptor.dimension);

                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(renderingData.cameraData.camera.pixelRect);
                LightweightPipeline.DrawFullScreen(cmd, material);
            }
            else
            {
                cmd.Blit(colorAttachmentHandle.Identifier(), BuiltinRenderTextureType.CameraTarget, material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
