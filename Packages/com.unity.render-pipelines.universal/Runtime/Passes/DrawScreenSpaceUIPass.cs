using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw screen space overlay UI into the given color and depth target
    /// </summary>
    internal class DrawScreenSpaceUIPass : ScriptableRenderPass
    {
        RTHandle m_ColorTarget;
        RTHandle m_DepthTarget;

        // Whether to render on an offscreen render texture or on the current active render target
        bool m_RenderOffscreen;

#if URP_COMPATIBILITY_MODE
        PassData m_PassData;
#endif

        /// <summary>
        /// Creates a new <c>DrawScreenSpaceUIPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public DrawScreenSpaceUIPass(RenderPassEvent evt, bool renderOffscreen)
        {
            profilingSampler = ProfilingSampler.Get(URPProfileId.DrawScreenSpaceUI);
            renderPassEvent = evt;
            m_RenderOffscreen = renderOffscreen;

#if URP_COMPATIBILITY_MODE
            useNativeRenderPass = false;
            m_PassData = new PassData();
#endif
        }

        /// <summary>
        /// Get a descriptor for the required color texture for this pass.
        /// </summary>
        /// <param name="descriptor">Camera target descriptor.</param>
        /// <param name="cameraWidth">Unscaled pixel width of the camera.</param>
        /// <param name="cameraHeight">Unscaled pixel height of the camera.</param>
        /// <seealso cref="RenderTextureDescriptor"/>
        private static void ConfigureColorDescriptor(ref RenderTextureDescriptor descriptor, int cameraWidth, int cameraHeight)
        {
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
            descriptor.depthStencilFormat = GraphicsFormat.None;
            descriptor.width = cameraWidth;
            descriptor.height = cameraHeight;
        }

        /// <summary>
        /// Get a descriptor for the required color texture for offscreen UI pass.
        /// </summary>
        internal static void ConfigureOffscreenUITextureDesc(ref TextureDesc textureDesc)
        {
            textureDesc.format = GraphicsFormat.R8G8B8A8_SRGB;
            textureDesc.depthBufferBits = 0;
            textureDesc.width = Screen.width;
            textureDesc.height = Screen.height;
        }

        /// <summary>
        /// Get a descriptor for the required depth texture for this pass.
        /// </summary>
        /// <param name="descriptor">Camera target descriptor.</param>
        /// <param name="depthStencilFormat">Depth stencil format required.</param>
        /// <param name="targetWidth">The target texture width.</param>
        /// <param name="targetHeight">The target texture height.</param>
        /// <seealso cref="RenderTextureDescriptor"/>
        private static void ConfigureDepthDescriptor(ref RenderTextureDescriptor descriptor, GraphicsFormat depthStencilFormat, int targetWidth, int targetHeight)
        {
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthStencilFormat = depthStencilFormat;
            descriptor.width = targetWidth;
            descriptor.height = targetHeight;
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
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_ColorTarget, colorDescriptor, name: "_OverlayUITexture");

                RenderTextureDescriptor depthDescriptor = cameraData.cameraTargetDescriptor;
                ConfigureDepthDescriptor(ref depthDescriptor, depthStencilFormat, cameraData.pixelWidth, cameraData.pixelHeight);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_DepthTarget, depthDescriptor, name: "_OverlayUITexture_Depth");
            }
        }

#if URP_COMPATIBILITY_MODE
        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsoleteFrom2023_3)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if(m_RenderOffscreen)
            {
                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                ConfigureTarget(m_ColorTarget, m_DepthTarget);
                ConfigureClear(ClearFlag.Color, Color.clear);
                #pragma warning restore CS0618
                cmd?.SetGlobalTexture(ShaderPropertyId.overlayUITexture, m_ColorTarget);
            }
            else
            {
                UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
                DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
                bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);

                if (resolveToDebugScreen)
                {
                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    ConfigureTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                    #pragma warning restore CS0618
                }
                else
                {
                    // Get RTHandle alias to use RTHandle apis
                    var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
                    RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
                    var colorTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    ConfigureTarget(colorTargetHandle);
                    #pragma warning restore CS0618
                }
            }
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsoleteFrom2023_3)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            using (new ProfilingScope(renderingData.commandBuffer, profilingSampler))
            {
                RendererList rendererList = context.CreateUIOverlayRendererList(renderingData.cameraData.camera);
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList);
            }
        }
#endif

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
        }

        internal void RenderOffscreen(RenderGraph renderGraph, ContextContainer frameData, GraphicsFormat depthStencilFormat, TextureHandle overlayUITexture)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor depthDescriptor = cameraData.cameraTargetDescriptor;
            ConfigureDepthDescriptor(ref depthDescriptor, depthStencilFormat, Screen.width, Screen.height);
            TextureHandle depthBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "_OverlayUITexture_Depth", false);

            // Render uGUI and UIToolkit overlays
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Screen Space UIToolkit/uGUI - Offscreen", out var passData, profilingSampler))
            {
                // UIToolkit/uGUI pass accept custom shaders, we need to make sure we use all global textures
                builder.UseAllGlobalTextures(true);

                builder.SetRenderAttachment(overlayUITexture, 0);

                passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.UIToolkit_UGUI);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                if (overlayUITexture.IsValid())
                    builder.SetGlobalTextureAfterPass(overlayUITexture, ShaderPropertyId.overlayUITexture);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(true, true, Color.clear);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
            // Render IMGUI overlay and software cursor in a UnsafePass
            // Doing so allow us to safely cover cases when graphics commands called through onGUI() in user scripts are not supported by RenderPass API
            // Besides, Vulkan backend doesn't support SetSRGWrite() in RenderPass API and we have some of them at IMGUI levels
            // Note, these specific UI calls doesn't need depth buffer unlike UIToolkit/uGUI
            using (var builder = renderGraph.AddUnsafePass<UnsafePassData>("Draw Screen Space IMGUI/SoftwareCursor - Offscreen", out var passData, profilingSampler))
            {
                passData.colorTarget = overlayUITexture;
                builder.UseTexture(overlayUITexture, AccessFlags.Write);

                passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.LowLevel);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc((UnsafePassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.colorTarget);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }

        internal void RenderOverlay(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle colorBuffer, in TextureHandle depthBuffer)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;

            // Render uGUI and UIToolkit overlays
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw UIToolkit/uGUI Overlay", out var passData, profilingSampler))
            {
                // UIToolkit/uGUI pass accept custom shaders, we need to make sure we use all global textures
                builder.UseAllGlobalTextures(true);

                builder.SetRenderAttachment(colorBuffer, 0);
                builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.UIToolkit_UGUI);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
            // Render IMGUI overlay and software cursor in a UnsafePass
            // Doing so allow us to safely cover cases when graphics commands called through onGUI() in user scripts are not supported by RenderPass API
            // Besides, Vulkan backend doesn't support SetSRGWrite() in RenderPass API and we have some of them at IMGUI levels
            // Note, these specific UI calls doesn't need depth buffer unlike UIToolkit/uGUI
            using (var builder = renderGraph.AddUnsafePass<UnsafePassData>("Draw IMGUI/SoftwareCursor Overlay", out var passData, profilingSampler))
            {
                passData.colorTarget = colorBuffer;
                builder.UseTexture(colorBuffer, AccessFlags.Write);

                passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.LowLevel);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc((UnsafePassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.colorTarget);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }
    }
}
