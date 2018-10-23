using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Draw the skybox into the given color buffer using the given depth buffer for depth testing.
    ///
    /// This pass renders the standard Unity skybox.
    /// </summary>
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTargetHandle depthAttachmentHandle { get; set; }

        /// <summary>
        /// Configure the color and depth passes to use when rendering the skybox
        /// </summary>
        /// <param name="colorHandle">Color buffer to use</param>
        /// <param name="depthHandle">Depth buffer to use</param>
        public void Setup(RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
        {
            this.colorAttachmentHandle = colorHandle;
            this.depthAttachmentHandle = depthHandle;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            CommandBuffer cmd = CommandBufferPool.Get("Draw Skybox (Set RT's)");
            if (renderingData.cameraData.isStereoEnabled && XRGraphics.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
            {
                cmd.SetRenderTarget(colorAttachmentHandle.Identifier(), depthAttachmentHandle.Identifier(), 0, CubemapFace.Unknown, -1);
            }
            else
            {
                cmd.SetRenderTarget(colorAttachmentHandle.Identifier(), depthAttachmentHandle.Identifier());
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            context.DrawSkybox(renderingData.cameraData.camera);
        }
    }
}
