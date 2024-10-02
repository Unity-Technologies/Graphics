using System;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

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
        int m_SampleOffsetShaderHandle;
        Material m_SamplingMaterial;
        Downsampling m_DownsamplingMethod;
        Material m_CopyColorMaterial;

        private RTHandle source { get; set; }

        private RTHandle destination { get; set; }

        private PassData m_PassData;

        /// <summary>
        /// Creates a new <c>CopyColorPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="samplingMaterial">The <c>Material</c> to use for downsampling quarter-resolution image with box filtering.</param>
        /// <param name="copyColorMaterial">The <c>Material</c> to use for other downsampling options.</param>
        /// <param name="customPassName">An optional custom profiling name to disambiguate multiple copy passes.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="Downsampling"/>
        public CopyColorPass(RenderPassEvent evt, Material samplingMaterial, Material copyColorMaterial = null, string customPassName = null)
        {
            profilingSampler = customPassName != null ? new ProfilingSampler(customPassName) : ProfilingSampler.Get(URPProfileId.CopyColor);

            m_PassData = new PassData();

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
        public static void ConfigureDescriptor(Downsampling downsamplingMethod, ref RenderTextureDescriptor descriptor, out FilterMode filterMode)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthStencilFormat = GraphicsFormat.None;
            if (downsamplingMethod == Downsampling._2xBilinear)
            {
                descriptor.width = Mathf.Max(1, descriptor.width / 2);
                descriptor.height = Mathf.Max(1, descriptor.height / 2);
            }
            else if (downsamplingMethod == Downsampling._4xBox || downsamplingMethod == Downsampling._4xBilinear)
            {
                descriptor.width = Mathf.Max(1, descriptor.width / 4);
                descriptor.height = Mathf.Max(1, descriptor.height / 4);
            }

            filterMode = downsamplingMethod == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source render target.</param>
        /// <param name="destination">Destination render target.</param>
        /// <param name="downsampling">The downsampling method to use.</param>
        [Obsolete("Use RTHandles for source and destination.", true)]
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
        {
            throw new NotSupportedException("Setup with RenderTargetIdentifier has been deprecated. Use it with RTHandles instead.");
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source render target.</param>
        /// <param name="destination">Destination render target.</param>
        /// <param name="downsampling">The downsampling method to use.</param>
        public void Setup(RTHandle source, RTHandle destination, Downsampling downsampling)
        {
            this.source = source;
            this.destination = destination;
            m_DownsamplingMethod = downsampling;
        }

        /// <inheritdoc />
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.SetGlobalTexture(destination.name, destination.nameID);
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.samplingMaterial = m_SamplingMaterial;
            m_PassData.copyColorMaterial = m_CopyColorMaterial;
            m_PassData.downsamplingMethod = m_DownsamplingMethod;
            m_PassData.sampleOffsetShaderHandle = m_SampleOffsetShaderHandle;

            var cmd = renderingData.commandBuffer;

            // TODO RENDERGRAPH: Do we need a similar check in the RenderGraph path?
            //It is possible that the given color target is now the frontbuffer
            if (source == renderingData.cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (renderingData.cameraData.xr.supportsFoveatedRendering)
                cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
#endif
            ScriptableRenderer.SetRenderTarget(cmd, destination, k_CameraTarget, clearFlag, clearColor);
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), m_PassData, source, renderingData.cameraData.xr.enabled);
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RTHandle source,  bool useDrawProceduralBlit)
        {
            var samplingMaterial = passData.samplingMaterial;
            var copyColorMaterial = passData.copyColorMaterial;
            var downsamplingMethod = passData.downsamplingMethod;
            var sampleOffsetShaderHandle = passData.sampleOffsetShaderHandle;

            if (samplingMaterial == null)
            {
                Debug.LogErrorFormat(
                    "Missing {0}. Copy Color render pass will not execute. Check for missing reference in the renderer resources.",
                    samplingMaterial);
                return;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyColor)))
            {
                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;

                switch (downsamplingMethod)
                {
                    case Downsampling.None:
                        Blitter.BlitTexture(cmd, source, viewportScale, copyColorMaterial, 0);
                        break;
                    case Downsampling._2xBilinear:
                        Blitter.BlitTexture(cmd, source, viewportScale, copyColorMaterial, 1);
                        break;
                    case Downsampling._4xBox:
                        samplingMaterial.SetFloat(sampleOffsetShaderHandle, 2);
                        Blitter.BlitTexture(cmd, source, viewportScale, samplingMaterial, 0);
                        break;
                    case Downsampling._4xBilinear:
                        Blitter.BlitTexture(cmd, source, viewportScale, copyColorMaterial, 1);
                        break;
                }
            }
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;
            // internal RenderingData renderingData;
            internal bool useProceduralBlit;
            internal Material samplingMaterial;
            internal Material copyColorMaterial;
            internal Downsampling downsamplingMethod;
            internal int sampleOffsetShaderHandle;
        }

        internal TextureHandle Render(RenderGraph renderGraph, ContextContainer frameData, out TextureHandle destination, in TextureHandle source, Downsampling downsampling)
        {
            m_DownsamplingMethod = downsampling;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            ConfigureDescriptor(downsampling, ref descriptor, out var filterMode);

            destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_CameraOpaqueTexture", true, filterMode);
            
            RenderInternal(renderGraph, destination, source, cameraData.xr.enabled);                    

            return destination;
        }



        // This will not create a new texture, but will reuse an existing one as destination.
        // Typical use case is a persistent texture imported to the render graph. For example history textures.
        // Note that the amount of downsampling is determined by the destination size.
        // Therefore, the downsampling param controls only the algorithm (shader) used for the downsampling, not size.
        internal void RenderToExistingTexture(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle destination, in TextureHandle source, Downsampling downsampling = Downsampling.None)
        {
            m_DownsamplingMethod = downsampling;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderInternal(renderGraph, destination, source, cameraData.xr.enabled);
        }

        private void RenderInternal(RenderGraph renderGraph, in TextureHandle destination, in TextureHandle source, bool useProceduralBlit)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.destination = destination;
                builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                passData.source = source;
                builder.UseTexture(source, AccessFlags.Read);
                passData.useProceduralBlit = useProceduralBlit;
                passData.samplingMaterial = m_SamplingMaterial;
                passData.copyColorMaterial = m_CopyColorMaterial;
                passData.downsamplingMethod = m_DownsamplingMethod;
                passData.sampleOffsetShaderHandle = m_SampleOffsetShaderHandle;

                if (destination.IsValid())
                    builder.SetGlobalTextureAfterPass(destination, Shader.PropertyToID("_CameraOpaqueTexture"));

                // TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.source, data.useProceduralBlit);
                });
            }
        }
    }
}
