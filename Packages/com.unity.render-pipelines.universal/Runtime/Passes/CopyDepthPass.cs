using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given depth buffer into the given destination depth buffer.
    ///
    /// You can use this pass to copy a depth buffer to a destination,
    /// so you can use it later in rendering. If the source texture has MSAA
    /// enabled, the pass uses a custom MSAA resolve. If the source texture
    /// does not have MSAA enabled, the pass uses a Blit or a Copy Texture
    /// operation, depending on what the current platform supports.
    /// </summary>
    public class CopyDepthPass : ScriptableRenderPass
    {
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }
        internal int MssaSamples { get; set; }
        // In some cases (Scene view, XR and etc.) we actually want to output to depth buffer
        // So this variable needs to be set to true to enable the correct copy shader semantic
        internal bool CopyToDepth { get; set; }
        Material m_CopyDepthMaterial;

        internal bool m_CopyResolvedDepth;
        internal bool m_ShouldClear;
        private PassData m_PassData;

        /// <summary>
        /// Creates a new <c>CopyDepthPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="copyDepthMaterial">The <c>Material</c> to use for copying the depth.</param>
        /// <param name="shouldClear">Controls whether it should do a clear before copying the depth.</param>
        /// <param name="copyToDepth">Controls whether it should do a copy to a depth format target.</param>
        /// <param name="copyResolvedDepth">Set to true if the source depth is MSAA resolved.</param>
        /// <seealso cref="RenderPassEvent"/>
        public CopyDepthPass(RenderPassEvent evt, Material copyDepthMaterial, bool shouldClear = false, bool copyToDepth = false, bool copyResolvedDepth = false)
        {
            base.profilingSampler = new ProfilingSampler(nameof(CopyDepthPass));
            m_PassData = new PassData();
            CopyToDepth = copyToDepth;
            m_CopyDepthMaterial = copyDepthMaterial;
            renderPassEvent = evt;
            m_CopyResolvedDepth = copyResolvedDepth;
            m_ShouldClear = shouldClear;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RTHandle source, RTHandle destination)
        {
            this.source = source;
            this.destination = destination;
            this.MssaSamples = -1;
        }

        /// <inheritdoc />
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            var isDepth = (destination.rt && destination.rt.graphicsFormat == GraphicsFormat.None);
            descriptor.graphicsFormat = isDepth ? GraphicsFormat.D32_SFloat_S8_UInt : GraphicsFormat.R32_SFloat;
            descriptor.msaaSamples = 1;
            // This is a temporary workaround for Editor as not setting any depth here
            // would lead to overwriting depth in certain scenarios (reproducable while running DX11 tests)
#if UNITY_EDITOR
            // This is a temporary workaround for Editor as not setting any depth here
            // would lead to overwriting depth in certain scenarios (reproducable while running DX11 tests)
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
                ConfigureTarget(destination, destination);
            else
#endif
            ConfigureTarget(destination);
            if (m_ShouldClear)
                ConfigureClear(ClearFlag.All, Color.black);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;
            internal CommandBuffer cmd;
            internal CameraData cameraData;
            internal Material copyDepthMaterial;
            internal int msaaSamples;
            internal bool copyResolvedDepth;
            internal bool copyToDepth;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.copyDepthMaterial = m_CopyDepthMaterial;
            m_PassData.msaaSamples = MssaSamples;
            m_PassData.copyResolvedDepth = m_CopyResolvedDepth;
            m_PassData.copyToDepth = CopyToDepth || !RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R32_SFloat, FormatUsage.Render);
            renderingData.commandBuffer.SetGlobalTexture("_CameraDepthAttachment", source.nameID);
            ExecutePass(context, m_PassData, ref renderingData.commandBuffer, ref renderingData.cameraData, source, destination);
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref CommandBuffer cmd, ref CameraData cameraData, RTHandle source, RTHandle destination)
        {
            var copyDepthMaterial = passData.copyDepthMaterial;
            var msaaSamples = passData.msaaSamples;
            var copyResolvedDepth = passData.copyResolvedDepth;
            var copyToDepth = passData.copyToDepth;

            if (copyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. Copy Depth render pass will not execute. Check for missing reference in the renderer resources.", copyDepthMaterial);
                return;
            }
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyDepth)))
            {
                int cameraSamples = 0;
                if (msaaSamples == -1)
                {
                    RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                    cameraSamples = descriptor.msaaSamples;
                }
                else
                    cameraSamples = msaaSamples;

                // When depth resolve is supported or multisampled texture is not supported, set camera samples to 1
                if (SystemInfo.supportsMultisampledTextures == 0 || copyResolvedDepth)
                    cameraSamples = 1;

                switch (cameraSamples)
                {
                    case 8:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 4:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 2:
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    // MSAA disabled, auto resolve supported or ms textures not supported
                    default:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;
                }

                if (copyToDepth || destination.rt.graphicsFormat == GraphicsFormat.None)
                    cmd.EnableShaderKeyword("_OUTPUT_DEPTH");
                else
                    cmd.DisableShaderKeyword("_OUTPUT_DEPTH");

                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                // We y-flip if
                // 1) we are blitting from render texture to back buffer(UV starts at bottom) and
                // 2) renderTexture starts UV at top
                bool isGameViewFinalTarget = cameraData.cameraType == CameraType.Game && destination.nameID == BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    if (cameraData.xr.supportsFoveatedRendering)
                        cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);

                    isGameViewFinalTarget |= new RenderTargetIdentifier(destination.nameID, 0, CubemapFace.Unknown, 0) == new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, 0);
                }
#endif
                bool yflip = cameraData.IsHandleYFlipped(source) != cameraData.IsHandleYFlipped(destination);
                Vector4 scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);
                if (isGameViewFinalTarget)
                    cmd.SetViewport(cameraData.pixelRect);
                else
                    cmd.SetViewport(new Rect(0, 0, cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height));
                Blitter.BlitTexture(cmd, source, scaleBias, copyDepthMaterial, 0);
            }
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            destination = k_CameraTarget;
        }

        internal void Render(RenderGraph renderGraph, out TextureHandle destination, in TextureHandle source, ref RenderingData renderingData)
        {
            // TODO RENDERGRAPH: should call the equivalent of Setup() to initialise everything correctly
            MssaSamples = -1;

            // TODO RENDERGRAPH: should refactor this as utility method for other passes to set Global textures
            using (var builder = renderGraph.AddRenderPass<PassData>("Setup Global Depth", out var passData, base.profilingSampler))
            {
                passData.source = builder.ReadTexture(source);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture("_CameraDepthAttachment", data.source);
                });
            }

            using (var builder = renderGraph.AddRenderPass<PassData>("Copy Depth", out var passData, base.profilingSampler))
            {
                var depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                depthDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
                depthDescriptor.depthStencilFormat = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = 0;
                depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
                destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "_CameraDepthTexture", true);

                passData.copyDepthMaterial = m_CopyDepthMaterial;
                passData.msaaSamples = MssaSamples;
                passData.cameraData = renderingData.cameraData;
                passData.cmd = renderingData.commandBuffer;
                passData.copyResolvedDepth = m_CopyResolvedDepth;
                passData.copyToDepth = CopyToDepth;
                passData.source = builder.ReadTexture(source);
                passData.destination = builder.UseColorBuffer(destination, 0);

                // TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data, ref data.cmd, ref data.cameraData, data.source, data.destination);
                });
            }

            using (var builder = renderGraph.AddRenderPass<PassData>("Setup Global Copy Depth", out var passData, base.profilingSampler))
            {
                passData.cmd = renderingData.commandBuffer;
                passData.destination = builder.UseColorBuffer(destination, 0);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    data.cmd.SetGlobalTexture("_CameraDepthTexture", data.destination);
                });
            }
        }
    }
}
