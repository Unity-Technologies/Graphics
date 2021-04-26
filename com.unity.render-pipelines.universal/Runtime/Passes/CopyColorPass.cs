using System;

namespace UnityEngine.Rendering.Universal.Internal
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
        static readonly RTHandle k_CameraTarget = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);

        int m_SampleOffsetShaderHandle;
        Material m_SamplingMaterial;
        public Downsampling m_DownsamplingMethod { get; private set; }
        Material m_CopyColorMaterial;

        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public CopyColorPass(RenderPassEvent evt, Material samplingMaterial, Material copyColorMaterial = null)
        {
            base.profilingSampler = new ProfilingSampler(nameof(CopyColorPass));

            m_SamplingMaterial = samplingMaterial;
            m_CopyColorMaterial = copyColorMaterial;
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
            renderPassEvent = evt;
            m_DownsamplingMethod = Downsampling.None;
            base.useNativeRenderPass = false;
            destination = null;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RTHandle source, RTHandle destination, Downsampling downsampling)
        {
            this.source = source;
            this.destination = destination;
            m_DownsamplingMethod = downsampling;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_SamplingMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_SamplingMaterial, GetType().Name);
                return;
            }

            //It is possible that the given color target is now the frontbuffer
            if(source == renderingData.cameraData.renderer.cameraColorFrontBuffer)
            {
                source = renderingData.cameraData.renderer.cameraColorTarget;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyColor)))
            {
                ScriptableRenderer.SetRenderTarget(cmd, destination ?? k_CameraTarget, k_CameraTarget, clearFlag,
                    clearColor);

                bool useDrawProceduleBlit = renderingData.cameraData.xr.enabled;
                switch (m_DownsamplingMethod)
                {
                    case Downsampling.None:
                        RenderingUtils.Blit(cmd, source, destination ?? k_CameraTarget, m_CopyColorMaterial, 0, useDrawProceduleBlit);
                        break;
                    case Downsampling._2xBilinear:
                        RenderingUtils.Blit(cmd, source, destination ?? k_CameraTarget, m_CopyColorMaterial, 0, useDrawProceduleBlit);
                        break;
                    case Downsampling._4xBox:
                        m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                        RenderingUtils.Blit(cmd, source, destination ?? k_CameraTarget, m_SamplingMaterial, 0, useDrawProceduleBlit);
                        break;
                    case Downsampling._4xBilinear:
                        RenderingUtils.Blit(cmd, source, destination ?? k_CameraTarget, m_CopyColorMaterial, 0, useDrawProceduleBlit);
                        break;
                }
            }

            // Preivous to RTHandles, GetTemporaryRT would implicitly do a SetGlobalTexture
            // Subsequent passes assume the global texture has been set
            if (destination != null)
                cmd.SetGlobalTexture(destination.name, destination);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            destination = null;
        }
    }
}
