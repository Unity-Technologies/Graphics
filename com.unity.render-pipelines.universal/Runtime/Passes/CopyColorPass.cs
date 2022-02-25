using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        static int m_SampleOffsetShaderHandle;
        static Material m_SamplingMaterial;
        static Downsampling m_DownsamplingMethod;
        static Material m_CopyColorMaterial;

        private RTHandle source { get; set; }

        private RTHandle destination { get; set; }

        // TODO: Remove when Obsolete Setup is removed
        private int destinationID { get; set; }

        /// <summary>
        /// Creates a new <c>CopyColorPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="samplingMaterial">The <c>Material</c> to use for downsampling quarter-resolution image with box filtering.</param>
        /// <param name="copyColorMaterial">The <c>Material</c> to use for other downsampling options.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="Downsampling"/>
        public CopyColorPass(RenderPassEvent evt, Material samplingMaterial, Material copyColorMaterial = null)
        {
            base.profilingSampler = new ProfilingSampler(nameof(CopyColorPass));

            m_SamplingMaterial = samplingMaterial;
            m_CopyColorMaterial = copyColorMaterial;
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
            renderPassEvent = evt;
            m_DownsamplingMethod = Downsampling.None;
            base.useNativeRenderPass = false;
        }

        /// <summary>
        /// Get a descriptor and filter mode for the required texture for this pass
        /// </summary>
        /// <param name="downsamplingMethod"></param>
        /// <param name="descriptor"></param>
        /// <param name="filterMode"></param>
        /// <seealso cref="Downsampling"/>
        /// <seealso cref="RenderTextureDescriptor"/>
        /// <seealso cref="FilterMode"/>
        public static void ConfigureDescriptor(Downsampling downsamplingMethod, ref RenderTextureDescriptor descriptor,
            out FilterMode filterMode)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            if (downsamplingMethod == Downsampling._2xBilinear)
            {
                descriptor.width /= 2;
                descriptor.height /= 2;
            }
            else if (downsamplingMethod == Downsampling._4xBox || downsamplingMethod == Downsampling._4xBilinear)
            {
                descriptor.width /= 4;
                descriptor.height /= 4;
            }

            filterMode = downsamplingMethod == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        [Obsolete("Use RTHandles for source and destination.")]
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
        {
            this.source = RTHandles.Alloc(source);
            this.destination = RTHandles.Alloc(destination.Identifier());
            this.destinationID = destination.id;
            m_DownsamplingMethod = downsampling;
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

        /// <inheritdoc />
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (destination.rt == null)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                if (m_DownsamplingMethod == Downsampling._2xBilinear)
                {
                    descriptor.width /= 2;
                    descriptor.height /= 2;
                }
                else if (m_DownsamplingMethod == Downsampling._4xBox ||
                         m_DownsamplingMethod == Downsampling._4xBilinear)
                {
                    descriptor.width /= 4;
                    descriptor.height /= 4;
                }

                cmd.GetTemporaryRT(destinationID, descriptor,
                    m_DownsamplingMethod == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
            }
            else
            {
                cmd.SetGlobalTexture(destination.name, destination.nameID);
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_SamplingMaterial == null)
            {
                Debug.LogErrorFormat(
                    "Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.",
                    m_SamplingMaterial, GetType().Name);
                return;
            }

            var cmd = renderingData.commandBuffer;

            //It is possible that the given color target is now the frontbuffer
            if (source == renderingData.cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyColor)))
            {
                ScriptableRenderer.SetRenderTarget(cmd, destination, k_CameraTarget, clearFlag,
                    clearColor);

               ExecutePass(renderingData, source, destination);
            }
        }

        private static void ExecutePass(RenderingData renderingData, RTHandle source, RTHandle destination)
        {
            bool useDrawProceduralBlit = renderingData.cameraData.xr.enabled;
            var cmd = renderingData.commandBuffer;

            switch (m_DownsamplingMethod)
            {
                case Downsampling.None:
                    RenderingUtils.Blit(cmd, source, destination, m_CopyColorMaterial, 0, useDrawProceduralBlit,
                        RenderBufferLoadAction.DontCare);
                    break;
                case Downsampling._2xBilinear:
                    RenderingUtils.Blit(cmd, source, destination, m_CopyColorMaterial, 0, useDrawProceduralBlit,
                        RenderBufferLoadAction.DontCare);
                    break;
                case Downsampling._4xBox:
                    m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                    RenderingUtils.Blit(cmd, source, destination, m_SamplingMaterial, 0, useDrawProceduralBlit,
                        RenderBufferLoadAction.DontCare);
                    break;
                case Downsampling._4xBilinear:
                    RenderingUtils.Blit(cmd, source, destination, m_CopyColorMaterial, 0, useDrawProceduralBlit,
                        RenderBufferLoadAction.DontCare);
                    break;
            }
        }

    class PassData
        {
            public TextureHandle source;
            public TextureHandle destination;

            public int copyID;

            public RenderingData renderingData;
        }

        public TextureHandle Render(TextureHandle src, Downsampling downsampling, ref RenderingData renderingData)
        {
            using (var builder = renderingData.renderGraph.AddRenderPass<PassData>("Copy Color", out var passData, new ProfilingSampler("Copy Color Pass")))
            {
                passData.source = src;
                passData.renderingData = renderingData;

                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                ConfigureDescriptor(downsampling, ref descriptor, out var filterMode);

                passData.destination = UniversalRenderer.CreateRenderGraphTexture(renderingData.renderGraph, descriptor, "Camera Opaque Texture", true);
                builder.UseColorBuffer(passData.destination, 0);
                builder.ReadTexture(passData.source);

                passData.copyID = Shader.PropertyToID("_CameraOpaqueTexture");

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(data.renderingData, data.source, data.destination);
                    data.renderingData.commandBuffer.SetGlobalTexture(data.copyID, data.destination);

                });

                return passData.destination;
            }
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (destination.rt == null && destinationID != -1)
            {
                cmd.ReleaseTemporaryRT(destinationID);
                destination.Release();
                destination = null;
            }
        }
    }
}
