using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw screen space overlay UI into the given color and depth target
    /// </summary>
    internal class DrawScreenSpaceUIPass : ScriptableRenderPass
    {
        PassData m_PassData;
        RTHandle m_ColorTarget;
        RTHandle m_DepthTarget;

        // Whether to render on an offscreen render texture or on the current active render target
        bool m_RenderOffscreen;

        /// <summary>
        /// Creates a new <c>DrawScreenSpaceUIPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public DrawScreenSpaceUIPass(RenderPassEvent evt, bool renderOffscreen)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DrawScreenSpaceUIPass));
            renderPassEvent = evt;
            useNativeRenderPass = false;
            m_RenderOffscreen = renderOffscreen;
            m_PassData = new PassData();
        }

        /// <summary>
        /// Get a descriptor for the required color texture for this pass.
        /// </summary>
        /// <param name="descriptor">Camera target descriptor.</param>
        /// <param name="cameraWidth">Unscaled pixel width of the camera.</param>
        /// <param name="cameraHeight">Unscaled pixel height of the camera.</param>
        /// <seealso cref="RenderTextureDescriptor"/>
        public static void ConfigureColorDescriptor(ref RenderTextureDescriptor descriptor, int cameraWidth, int cameraHeight)
        {
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
            descriptor.depthStencilFormat = GraphicsFormat.None;
            descriptor.width = cameraWidth;
            descriptor.height = cameraHeight;
        }

        /// <summary>
        /// Get a descriptor for the required depth texture for this pass.
        /// </summary>
        /// <param name="descriptor">Camera target descriptor.</param>
        /// <param name="depthStencilFormat">Depth stencil format required.</param>
        /// <param name="cameraWidth">Unscaled pixel width of the camera.</param>
        /// <param name="cameraHeight">Unscaled pixel height of the camera.</param>
        /// <seealso cref="RenderTextureDescriptor"/>
        public static void ConfigureDepthDescriptor(ref RenderTextureDescriptor descriptor, GraphicsFormat depthStencilFormat, int cameraWidth, int cameraHeight)
        {
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthStencilFormat = depthStencilFormat;
            descriptor.width = cameraWidth;
            descriptor.height = cameraHeight;
        }

        private static void ExecutePass(RasterCommandBuffer commandBuffer, PassData passData, RendererList rendererList)
        {
            commandBuffer.DrawRendererList(rendererList);
        }

        // Non-RenderGraph path
        public void Dispose()
        {
            m_ColorTarget?.Release();
            m_DepthTarget?.Release();
        }

        /// <summary>
        /// Configure the pass with the off-screen destination color texture and depth texture to execute the pass on.
        /// </summary>
        /// <param name="cameraData">Camera rendering data containing all relevant render target information.</param>
        /// <param name="depthStencilFormat">Depth stencil format required for depth/stencil effects.</param>
        public void Setup(ref CameraData cameraData, GraphicsFormat depthStencilFormat)
        {
            if (m_RenderOffscreen)
            {
                RenderTextureDescriptor colorDescriptor = cameraData.cameraTargetDescriptor;
                ConfigureColorDescriptor(ref colorDescriptor, cameraData.pixelWidth, cameraData.pixelHeight);
                RenderingUtils.ReAllocateIfNeeded(ref m_ColorTarget, colorDescriptor, name: "_OverlayUITexture");

                RenderTextureDescriptor depthDescriptor = cameraData.cameraTargetDescriptor;
                ConfigureDepthDescriptor(ref depthDescriptor, depthStencilFormat, cameraData.pixelWidth, cameraData.pixelHeight);
                RenderingUtils.ReAllocateIfNeeded(ref m_DepthTarget, depthDescriptor, name: "_OverlayUITexture_Depth");
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_RenderOffscreen)
            {
                CoreUtils.SetRenderTarget(renderingData.commandBuffer, m_ColorTarget, m_DepthTarget, ClearFlag.Color, Color.clear);
                renderingData.commandBuffer.SetGlobalTexture(ShaderPropertyId.overlayUITexture, m_ColorTarget);
            }
            else
            {
                DebugHandler debugHandler = GetActiveDebugHandler(ref renderingData);
                var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
                bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(ref renderingData.cameraData);

                if (resolveToDebugScreen)
                {
                    CoreUtils.SetRenderTarget(renderingData.commandBuffer, debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                }
                else
                {
                    // Get RTHandle alias to use RTHandle apis
                    RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
                    var colorTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

                    CoreUtils.SetRenderTarget(renderingData.commandBuffer, colorTargetHandle);
                }
            }

            using (new ProfilingScope(renderingData.commandBuffer, ProfilingSampler.Get(URPProfileId.DrawScreenSpaceUI)))
            {
                RendererList rendererList = context.CreateUIOverlayRendererList(renderingData.cameraData.camera);
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList);
            }
        }

        //RenderGraph path
        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        internal void RenderOffscreen(RenderGraph renderGraph, GraphicsFormat depthStencilFormat, out TextureHandle output, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Screen Space UI Pass - Offscreen", out var passData, base.profilingSampler))
            {
                RenderTextureDescriptor colorDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                ConfigureColorDescriptor(ref colorDescriptor, renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);
                output = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDescriptor, "_OverlayUITexture", true);
                builder.UseTextureFragment(output, 0);
                
                passData.rendererList = renderGraph.CreateUIOverlayRendererList(renderingData.cameraData.camera);
                builder.UseRendererList(passData.rendererList);

                RenderTextureDescriptor depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                ConfigureDepthDescriptor(ref depthDescriptor, depthStencilFormat, renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);
                TextureHandle depthBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "_OverlayUITexture_Depth", false);
                builder.UseTextureFragmentDepth(depthBuffer, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }

            RenderGraphUtils.SetGlobalTexture(renderGraph, ShaderPropertyId.overlayUITexture, output);
        }

        internal void RenderOverlay(RenderGraph renderGraph, in TextureHandle colorBuffer, in TextureHandle depthBuffer, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Screen Space UI Pass - Overlay", out var passData, base.profilingSampler))
            {
                builder.UseTextureFragment(colorBuffer, 0);
                builder.UseTextureFragmentDepth(depthBuffer, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);

                passData.rendererList = renderGraph.CreateUIOverlayRendererList(renderingData.cameraData.camera);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }
    }
}
