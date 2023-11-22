using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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

        // Specific to RG cases which have to go through Unsafe commands
        private static void ExecutePass(UnsafeCommandBuffer commandBuffer, UnsafePassData passData, RendererList rendererList)
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
        public void Setup(UniversalCameraData cameraData, GraphicsFormat depthStencilFormat)
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
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if(m_RenderOffscreen)
            {
                ConfigureTarget(m_ColorTarget, m_DepthTarget);
                ConfigureClear(ClearFlag.Color, Color.clear);
                cmd?.SetGlobalTexture(ShaderPropertyId.overlayUITexture, m_ColorTarget);
            }
            else
            {
                UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
                DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
                bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);

                if (resolveToDebugScreen)
                {
                    ConfigureTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                }
                else
                {
                    // Get RTHandle alias to use RTHandle apis
                    var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
                    RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
                    var colorTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

                    ConfigureTarget(colorTargetHandle);
                }
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
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

        // Specific to RG cases which have to go through Unsafe commands
        private class UnsafePassData
        {
            internal RendererListHandle rendererList;
            internal TextureHandle colorTarget;
            internal TextureHandle depthTarget;
        }

        internal void RenderOffscreen(RenderGraph renderGraph, ContextContainer frameData, GraphicsFormat depthStencilFormat, out TextureHandle output)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor colorDescriptor = cameraData.cameraTargetDescriptor;
            ConfigureColorDescriptor(ref colorDescriptor, cameraData.pixelWidth, cameraData.pixelHeight);
            output = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDescriptor, "_OverlayUITexture", true);
            RenderTextureDescriptor depthDescriptor = cameraData.cameraTargetDescriptor;
            ConfigureDepthDescriptor(ref depthDescriptor, depthStencilFormat, cameraData.pixelWidth, cameraData.pixelHeight);
            TextureHandle depthBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "_OverlayUITexture_Depth", false);

            bool isVulkan = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan);

            // Vulkan backend doesn't support SetSRGBWrite() calls in Render Pass and we have some of them at IMGUI levels on native side
            // So we need to use a low level render pass for those specific UI rendering calls
            if(isVulkan)
            {
                // Render uGUI and UIToolkit overlays
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Screen Space UIToolkit/uGUI Pass - Offscreen", out var passData, base.profilingSampler))
                {
                    builder.SetRenderAttachment(output, 0);

                    passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.UIToolkit_UGUI);
                    builder.UseRendererList(passData.rendererList);

                    builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                    if (output.IsValid())
                        builder.SetGlobalTextureAfterPass(output, ShaderPropertyId.overlayUITexture);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(context.cmd, data, data.rendererList);
                    });
                }
                // Render IMGUI overlay and software cursor
                using (var builder = renderGraph.AddUnsafePass<UnsafePassData>("Screen Space IMGUI/SoftwareCursor Pass - Offscreen", out var passData, base.profilingSampler))
                {
                    passData.colorTarget = output;
                    builder.UseTexture(output, AccessFlags.Write);

                    passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.LowLevel);
                    builder.UseRendererList(passData.rendererList);

                    passData.depthTarget = depthBuffer;
                    builder.UseTexture(depthBuffer, AccessFlags.ReadWrite);

                    if (output.IsValid())
                        builder.SetGlobalTextureAfterPass(output, ShaderPropertyId.overlayUITexture);

                    builder.SetRenderFunc((UnsafePassData data, UnsafeGraphContext context) =>
                    {
                        context.cmd.SetRenderTarget(data.colorTarget, data.depthTarget);

                        ExecutePass(context.cmd, data, data.rendererList);
                    });
                }
            }
            else
            {
                // Render all UI at once
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Screen Space UI Pass - Offscreen", out var passData, base.profilingSampler))
                {
                    builder.SetRenderAttachment(output, 0);

                    passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.All);
                    builder.UseRendererList(passData.rendererList);

                    builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                    if (output.IsValid())
                        builder.SetGlobalTextureAfterPass(output, ShaderPropertyId.overlayUITexture);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(context.cmd, data, data.rendererList);
                    });
                }
            }
        }

        internal void RenderOverlay(RenderGraph renderGraph, Camera camera, in TextureHandle colorBuffer, in TextureHandle depthBuffer)
        {
            bool isVulkan = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan);

            // Vulkan backend doesn't support SetSRGBWrite() calls in Render Pass and we have some of them at IMGUI levels on native side
            // So we need to use a low level render pass for those specific UI rendering calls
            if(isVulkan)
            {
                // Render uGUI and UIToolkit overlays
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Screen Space UIToolkit/uGUI Pass - Overlay", out var passData, base.profilingSampler))
                {
                    builder.SetRenderAttachment(colorBuffer, 0);
                    builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                    passData.rendererList = renderGraph.CreateUIOverlayRendererList(camera, UISubset.UIToolkit_UGUI);
                    builder.UseRendererList(passData.rendererList);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(context.cmd, data, data.rendererList);
                    });
                }
                // Render IMGUI overlay and software cursor
                using (var builder = renderGraph.AddUnsafePass<UnsafePassData>("Screen Space IMGUI/SoftwareCursor Pass - Overlay", out var passData, base.profilingSampler))
                {
                    passData.colorTarget = colorBuffer;
                    builder.UseTexture(colorBuffer, AccessFlags.Write);
                    passData.depthTarget = depthBuffer;
                    builder.UseTexture(depthBuffer, AccessFlags.Write);

                    passData.rendererList = renderGraph.CreateUIOverlayRendererList(camera, UISubset.LowLevel);
                    builder.UseRendererList(passData.rendererList);

                    builder.SetRenderFunc((UnsafePassData data, UnsafeGraphContext context) =>
                    {
                        context.cmd.SetRenderTarget(data.colorTarget, data.depthTarget);

                        ExecutePass(context.cmd, data, data.rendererList);
                    });
                }
            }
            else
            {
                // Render all UI at once
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Screen Space UI Pass - Overlay", out var passData, base.profilingSampler))
                {
                    builder.SetRenderAttachment(colorBuffer, 0);
                    builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                    passData.rendererList = renderGraph.CreateUIOverlayRendererList(camera, UISubset.All);
                    builder.UseRendererList(passData.rendererList);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(context.cmd, data, data.rendererList);
                    });
                }
            }
        }
    }
}