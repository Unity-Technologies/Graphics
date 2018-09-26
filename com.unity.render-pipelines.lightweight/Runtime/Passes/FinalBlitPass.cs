using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Copy the given color target to the current camera target
    ///
    /// You can use this pass to copy the result of rendering to
    /// the camera target. The pass takes the screen viewport into
    /// consideration.
    /// </summary>
    public class FinalBlitPass : ScriptableRenderPass
    {
        const string k_FinalBlitTag = "Final Blit Pass";

        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorAttachmentHandle"></param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.descriptor = baseDescriptor;
        }
        
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            Material material = renderingData.cameraData.isStereoEnabled ? null : renderer.GetMaterial(MaterialHandle.Blit);
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
                ScriptableRenderer.RenderFullscreenQuad(cmd, material);
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
