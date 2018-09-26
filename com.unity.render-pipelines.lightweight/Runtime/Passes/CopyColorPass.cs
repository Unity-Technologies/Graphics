using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// Copy the given color buffer to the given destination color buffer.
    ///
    /// You can use this pass to copy a color buffer to the destination,
    /// so you can use it later in rendering. For example, you can copy
    /// the opaque texture to use it for distortion effects.
    /// </summary>
    public class CopyColorPass : ScriptableRenderPass
    {
        const string k_CopyColorTag = "Copy Color";
        float[] m_OpaqueScalerValues = {1.0f, 0.5f, 0.25f, 0.25f};
        int m_SampleOffsetShaderHandle;

        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public CopyColorPass()
        {
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
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
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
                
            CommandBuffer cmd = CommandBufferPool.Get(k_CopyColorTag);
            Downsampling downsampling = renderingData.cameraData.opaqueTextureDownsampling;
            float opaqueScaler = m_OpaqueScalerValues[(int)downsampling];

            RenderTextureDescriptor opaqueDesc = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData, opaqueScaler);
            RenderTargetIdentifier colorRT = source.Identifier();
            RenderTargetIdentifier opaqueColorRT = destination.Identifier();

            cmd.GetTemporaryRT(destination.id, opaqueDesc, renderingData.cameraData.opaqueTextureDownsampling == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
            switch (downsampling)
            {
                case Downsampling.None:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
                case Downsampling._2xBilinear:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
                case Downsampling._4xBox:
                    Material samplingMaterial = renderer.GetMaterial(MaterialHandle.Sampling);
                    samplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                    cmd.Blit(colorRT, opaqueColorRT, samplingMaterial, 0);
                    break;
                case Downsampling._4xBilinear:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            
            if (destination != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
