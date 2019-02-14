using System;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Copy the given color buffer to the given destination color buffer.
    ///
    /// You can use this pass to copy a color buffer to the destination,
    /// so you can use it later in rendering. For example, you can copy
    /// the opaque texture to use it for distortion effects.
    /// </summary>
    internal class BlitPass : ScriptableRenderPass
    {
        public enum RenderTarget
        {
            Color,
            Depth,
            RenderTexture
        }
        
        string blitTag { get; set; }
        public Material blitMaterial = null;
        public int blitShaderPassIndex = 0;
        public FilterMode filterMode { get; set; }

        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public BlitPass(RenderPassEvent renderPassEvent, Material blitMaterial, int blitShaderPassIndex, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = blitMaterial;
            this.blitShaderPassIndex = blitShaderPassIndex;
            blitTag = tag;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(blitTag);
            
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            //opaqueDesc.msaaSamples = 1;

            RenderTargetIdentifier src = source.Identifier();
            RenderTargetIdentifier dest = destination.Identifier();

            cmd.GetTemporaryRT(destination.id, opaqueDesc, filterMode);

            if (true)
            {
                cmd.Blit(src, dest, blitMaterial, blitShaderPassIndex);
            }
            else
            {
/*                cmd.SetGlobalTexture("_BlitTex", m_Source.Identifier());

                SetRenderTarget(
                    cmd,
                    BuiltinRenderTextureType.CameraTarget,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    ClearFlag.None,
                    Color.black,
                    m_TargetDimension);

                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(renderingData.cameraData.camera.pixelRect);
                RenderFullscreenQuad(cmd, m_BlitMaterial);*/
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
