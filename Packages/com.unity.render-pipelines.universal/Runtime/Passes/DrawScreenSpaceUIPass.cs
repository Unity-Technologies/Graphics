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
            m_RenderOffscreen = renderOffscreen;
            m_PassData = new PassData();
        }

        // Common to RenderGraph and non-RenderGraph paths
        private class PassData
        {
            internal CommandBuffer cmd;
            internal Camera camera;
            internal TextureHandle offscreenTexture;
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
            descriptor.depthBufferBits = 0;
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
        public static void ConfigureDepthDescriptor(ref RenderTextureDescriptor descriptor, int depthBufferBits, int cameraWidth, int cameraHeight)
        {
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthBufferBits = depthBufferBits;
            descriptor.width = cameraWidth;
            descriptor.height = cameraHeight;
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData)
        {
            context.ExecuteCommandBuffer(passData.cmd);
            passData.cmd.Clear();
            context.DrawUIOverlay(passData.camera);
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
        /// <param name="depthBufferBits">Depth buffer bits required for depth/stencil effects.</param>
        public void Setup(ref CameraData cameraData, int depthBufferBits)
        {
            if (m_RenderOffscreen)
            {
                RenderTextureDescriptor colorDescriptor = cameraData.cameraTargetDescriptor;
                ConfigureColorDescriptor(ref colorDescriptor, cameraData.pixelWidth, cameraData.pixelHeight);
                RenderingUtils.ReAllocateIfNeeded(ref m_ColorTarget, colorDescriptor, name: "_OverlayUITexture");

                RenderTextureDescriptor depthDescriptor = cameraData.cameraTargetDescriptor;
                ConfigureDepthDescriptor(ref depthDescriptor, depthBufferBits, cameraData.pixelWidth, cameraData.pixelHeight);
                RenderingUtils.ReAllocateIfNeeded(ref m_DepthTarget, depthDescriptor, name: "_OverlayUITexture_Depth");
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.cmd = renderingData.commandBuffer;
            m_PassData.camera = renderingData.cameraData.camera;

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
                ExecutePass(context, m_PassData);
            }
        }

        //RenderGraph path
        internal void RenderOffscreen(RenderGraph renderGraph, int depthBufferBits, out TextureHandle output, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Screen Space UI Pass - Offscreen", out var passData, base.profilingSampler))
            {
                RenderTextureDescriptor colorDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                ConfigureColorDescriptor(ref colorDescriptor, renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);
                output = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDescriptor, "_OverlayUITexture", true);
                builder.UseColorBuffer(output, 0);

                RenderTextureDescriptor depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                ConfigureDepthDescriptor(ref depthDescriptor, depthBufferBits, renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);
                TextureHandle depthBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "_OverlayUITexture_Depth", false);
                builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                passData.cmd = renderingData.commandBuffer;
                passData.camera = renderingData.cameraData.camera;
                passData.offscreenTexture = output;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data);
                    data.cmd.SetGlobalTexture(ShaderPropertyId.overlayUITexture, data.offscreenTexture);
                });
            }
        }

        internal void RenderOverlay(RenderGraph renderGraph, in TextureHandle colorBuffer, in TextureHandle depthBuffer, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Screen Space UI Pass - Overlay", out var passData, base.profilingSampler))
            {
                builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                passData.cmd = renderingData.commandBuffer;
                passData.camera = renderingData.cameraData.camera;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data);
                });
            }
        }
    }
}
