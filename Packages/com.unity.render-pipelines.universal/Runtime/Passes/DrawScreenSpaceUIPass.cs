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
        
        public RTHandle colorTarget { get => m_ColorTarget; }

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
        /// Get a descriptor for the required color texture for this pass
        /// </summary>
        /// <param name="descriptor"></param>
        /// <seealso cref="RenderTextureDescriptor"/>
        public static void ConfigureDescriptor(ref RenderTextureDescriptor descriptor)
        {
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
            descriptor.depthBufferBits = 0;
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
        }

        /// <summary>
        /// Configure the pass with the off-screen destination color texture and the depth texture to execute the pass on.
        /// </summary>
        /// <param name="descriptor">Descriptor for the color buffer.</param>
        /// <param name="depthTexture">Depth texture to render to.</param>
        public void Setup(RenderTextureDescriptor descriptor, in RTHandle depthTexture)
        {
            if (m_RenderOffscreen)
            {
                DrawScreenSpaceUIPass.ConfigureDescriptor(ref descriptor);
                RenderingUtils.ReAllocateIfNeeded(ref m_ColorTarget, descriptor, name: "_OverlayUITexture");
                m_DepthTarget = depthTexture;
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
                    CoreUtils.SetRenderTarget(renderingData.commandBuffer, debugHandler.DebugScreenTextureHandle);
                }
                else
                {
                    // Create RTHandle alias to use RTHandle apis
                    if (m_ColorTarget != cameraTarget)
                    {
                        m_ColorTarget?.Release();
                        m_ColorTarget = RTHandles.Alloc(cameraTarget);
                    }

                    CoreUtils.SetRenderTarget(renderingData.commandBuffer, m_ColorTarget);
                }
            }

            using (new ProfilingScope(renderingData.commandBuffer, ProfilingSampler.Get(URPProfileId.DrawScreenSpaceUI)))
            {
                ExecutePass(context, m_PassData);
            }
        }

        //RenderGraph path
        internal void RenderOffscreen(RenderGraph renderGraph, out TextureHandle output, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Screen Space UI Pass - Offscreen", out var passData, base.profilingSampler))
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                ConfigureDescriptor(ref descriptor);
                output = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_OverlayUITexture", true);
                builder.UseColorBuffer(output, 0);

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

        internal void RenderOverlay(RenderGraph renderGraph, in TextureHandle colorBuffer, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Screen Space UI Pass - Overlay", out var passData, base.profilingSampler))
            {
                builder.WriteTexture(colorBuffer);

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
