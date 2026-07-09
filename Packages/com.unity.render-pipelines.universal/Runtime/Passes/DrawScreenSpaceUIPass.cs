using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if ENABLE_UIELEMENTS_MODULE
using UnityEngine.UIElements;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw screen space overlay UI into the given color and depth target
    /// </summary>
    internal class DrawScreenSpaceUIPass : ScriptableRenderPass
    {
        // Renderer-owned materials (created and destroyed by the renderer, not this pass).
        readonly Material m_UIBackdropFilterCompositeMaterial;
        // Blits each camera's post-FX color into the composite buffer.
        readonly Material m_SeedBlitMaterial;
        const int k_CompositePassIndex = 0;

        bool m_RequiresComposition;

        // Backdrop-filter overlay buffers, owned by this pass; null on passes that never composite (e.g. 2D).
        RTHandle m_OverlayCompositeBuffer;        // overlay UI renders here, then blits to the backbuffer
        RTHandle m_OverlayCompositeBufferBefore;  // pre-UI snapshot the composite diffs against
        RTHandle m_OverlayDepthStencilBuffer;     // depth/stencil for the overlay UI (stencil clipping)

        // Per-frame render-graph handles, re-imported each frame (or null when the composite path is skipped).
        TextureHandle m_OverlayCompositeBufferHandle = TextureHandle.nullHandle;
        TextureHandle m_OverlayCompositeBufferBeforeHandle = TextureHandle.nullHandle;
        TextureHandle m_OverlayDepthStencilBufferHandle = TextureHandle.nullHandle;

        /// <summary>
        /// Creates a new <c>DrawScreenSpaceUIPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="uiBackdropFilterCompositeMaterial">Material for the diff composite blit, created and owned by the renderer. Pass null when this pass instance never runs the unsafe overlay path.</param>
        /// <param name="seedBlitMaterial">URP blit material for the per-camera scene-to-buffer seed step, created and owned by the renderer. Pass null when this pass instance never runs the unsafe overlay path.</param>
        /// <seealso cref="RenderPassEvent"/>
        public DrawScreenSpaceUIPass(RenderPassEvent evt, Material uiBackdropFilterCompositeMaterial = null, Material seedBlitMaterial = null)
        {
            profilingSampler = URPProfilingSamplers.DrawScreenSpaceUI;
            renderPassEvent = evt;
            m_UIBackdropFilterCompositeMaterial = uiBackdropFilterCompositeMaterial;
            m_SeedBlitMaterial = seedBlitMaterial;
        }

        // Computes this frame's composite need; the renderer calls it each frame like other passes' Setup().
        internal void Setup(UniversalCameraData cameraData)
        {
#if ENABLE_UIELEMENTS_MODULE
            m_RequiresComposition = cameraData.rendersOverlayUI
                && UIElementsRuntimeUtility.AnyOverlayPanelHasBackdropFilter()
                && !cameraData.stackLastCameraOutputToHDR;
#else
            m_RequiresComposition = false;
#endif
        }

        internal bool RequiresComposition() => m_RequiresComposition;

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
        /// <param name="screenWidth">The full screen width.</param>
        /// <param name="screenHeight">The full screen height.</param>
        /// <seealso cref="RenderTextureDescriptor"/>
        private static void ConfigureDepthDescriptor(ref RenderTextureDescriptor descriptor, GraphicsFormat depthStencilFormat, int screenWidth, int screenHeight)
        {
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthStencilFormat = depthStencilFormat;
            descriptor.width = screenWidth;
            descriptor.height = screenHeight;
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

        public void Dispose()
        {
            // Materials are owned by the renderer; this pass only releases the buffers it allocates.
            m_OverlayCompositeBuffer?.Release();
            m_OverlayCompositeBuffer = null;
            m_OverlayCompositeBufferBefore?.Release();
            m_OverlayCompositeBufferBefore = null;
            m_OverlayDepthStencilBuffer?.Release();
            m_OverlayDepthStencilBuffer = null;
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
            // Bound with color so stencil clipping works.
            internal TextureHandle depthTarget;
            // Pre-UI snapshot the composite diffs against.
            internal TextureHandle bufferBefore;
        }

        private class ViewportBlitPassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;
            internal Rect viewport;
            internal Material material;
            internal int passIndex;
        }

        // Runs before the final blit — afterwards cameraColor is the backbuffer and can't be read.
        internal void BlitCameraColorToOverlayCompositeBuffer(RenderGraph renderGraph, UniversalResourceData resourceData, Rect viewport, GraphicsFormat cameraDepthAttachmentFormat)
        {
            ImportOverlayCompositeTextures(renderGraph, resourceData, cameraDepthAttachmentFormat);

            AddViewportBlitPass(renderGraph, resourceData.cameraColor, m_OverlayCompositeBufferHandle,
                viewport, sampler: null, passName: "Overlay Composite Viewport Blit");
        }

        void ImportOverlayCompositeTextures(RenderGraph renderGraph, UniversalResourceData resourceData, GraphicsFormat cameraDepthAttachmentFormat)
        {
            var bbInfo = renderGraph.GetRenderTargetInfo(resourceData.backBufferColor);
            var compositeDesc = new RenderTextureDescriptor(bbInfo.width, bbInfo.height, bbInfo.format, depthBufferBits: 0)
            {
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
            };
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_OverlayCompositeBuffer, compositeDesc,
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_OverlayCompositeBuffer");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_OverlayCompositeBufferBefore, compositeDesc,
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_OverlayCompositeBufferBefore");

            // Dedicated buffer: the camera depth can be a placeholder or wrong size here.
            var depthStencilDesc = new RenderTextureDescriptor(bbInfo.width, bbInfo.height, GraphicsFormat.None, cameraDepthAttachmentFormat)
            {
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
            };
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_OverlayDepthStencilBuffer, depthStencilDesc,
                FilterMode.Point, TextureWrapMode.Clamp, name: "_OverlayDepthStencilBuffer");

            // Each base camera writes only its viewport; earlier cameras' regions must survive the import.
            var importParams = new ImportResourceParams
            {
                clearOnFirstUse = false,
                discardOnLastUse = false,
            };
            m_OverlayCompositeBufferHandle = renderGraph.ImportTexture(m_OverlayCompositeBuffer, importParams);
            m_OverlayCompositeBufferBeforeHandle = renderGraph.ImportTexture(m_OverlayCompositeBufferBefore, importParams);
            m_OverlayDepthStencilBufferHandle = renderGraph.ImportTexture(m_OverlayDepthStencilBuffer, importParams);
        }

        // No Y-flip here: source and destination share the same orientation (unlike a final blit to screen).
        internal void AddViewportBlitPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, Rect viewport, ProfilingSampler sampler, string passName)
        {
            if (m_SeedBlitMaterial == null)
                return;

            using (var builder = renderGraph.AddRasterRenderPass<ViewportBlitPassData>(passName, out var passData, sampler))
            {
                passData.source = source;
                passData.destination = destination;
                passData.viewport = viewport;
                passData.material = m_SeedBlitMaterial;
                passData.passIndex = 0;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (ViewportBlitPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetViewport(data.viewport);
                    RTHandle srcRTHandle = data.source;
                    Vector2 scale = (srcRTHandle != null && srcRTHandle.useScaling)
                        ? new Vector2(srcRTHandle.rtHandleProperties.rtHandleScale.x, srcRTHandle.rtHandleProperties.rtHandleScale.y)
                        : Vector2.one;
                    Vector4 scaleBias = new Vector4(scale.x, scale.y, 0f, 0f);
                    Blitter.BlitTexture(context.cmd, data.source, scaleBias, data.material, data.passIndex);
                });
            }
        }

        private class CompositePassData
        {
            internal TextureHandle source;
            internal TextureHandle sourceBefore;
            internal TextureHandle destination;
            internal Material material;
            internal int passIndex;
        }

        static readonly int s_BlitTextureBeforeId = Shader.PropertyToID("_BlitTexture_Before");

        // Composites the OverlayCompositeBuffer onto the backbuffer: pixels matching the pre-UI snapshot
        // (`sourceBefore`) are discarded (keeping FinalPost output); changed pixels write the post-UI RGB.
        internal void AddUIBackdropFilterCompositePass(RenderGraph renderGraph, TextureHandle source, TextureHandle sourceBefore, TextureHandle destination, ProfilingSampler sampler, string passName)
        {
            if (m_UIBackdropFilterCompositeMaterial == null)
                return;

            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(passName, out var passData, sampler))
            {
                passData.source = source;
                passData.sourceBefore = sourceBefore;
                passData.destination = destination;
                passData.material = m_UIBackdropFilterCompositeMaterial;
                passData.passIndex = k_CompositePassIndex;
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(sourceBefore, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                {
                    // Bind the pre-UI snapshot; Blitter sets _BlitTexture from data.source.
                    data.material.SetTexture(s_BlitTextureBeforeId, data.sourceBefore);
                    Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(context, in data.source, in data.destination);
                    Blitter.BlitTexture(context.cmd, data.source, scaleBias, data.material, data.passIndex);
                });
            }
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

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
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

                builder.SetRenderFunc(static (UnsafePassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.colorTarget);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }

        internal void RenderOverlay(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle colorBuffer, in TextureHandle depthBuffer)
        {
            RenderOverlayUIToolkitAndUGUI(renderGraph, frameData, in colorBuffer, in depthBuffer);
            RenderOverlayIMGUI(renderGraph, frameData, in colorBuffer, in depthBuffer);
        }

        // Default raster overlay path: used when no panel needs backdrop-filter (or the composite path can't
        // run). A stray backdrop-filter element here silently no-ops, which is intended.
        internal void RenderOverlayUIToolkitAndUGUI(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle colorBuffer, in TextureHandle depthBuffer)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw UIToolkit/uGUI Overlay", out var passData, profilingSampler))
            {
                // UIToolkit/uGUI pass accept custom shaders, we need to make sure we use all global textures
                builder.UseAllGlobalTextures(true);

                builder.SetRenderAttachment(colorBuffer, 0);
                builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.ReadWrite);

                passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.UIToolkit_UGUI);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }

        // Composite variant: used when an overlay panel has backdrop-filter. UIToolkit renders into
        // m_OverlayCompositeBuffer (pre-filled with the scene); a final diff-composite writes it to the backbuffer.
        internal void RenderOverlayUIToolkitAndUGUIComposite(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle destinationColor)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

            // Pass 1 — render UIToolkit/uGUI into the composite buffer (backdrop-filter samples it as it fills).
            using (var builder = renderGraph.AddUnsafePass<UnsafePassData>("Draw UIToolkit/uGUI Overlay (Backdrop Composite)", out var passData, profilingSampler))
            {
                // UIToolkit/uGUI pass accepts custom shaders; we need to make sure we use all global textures
                builder.UseAllGlobalTextures(true);

                passData.colorTarget = m_OverlayCompositeBufferHandle;
                passData.depthTarget = m_OverlayDepthStencilBufferHandle;
                passData.bufferBefore = m_OverlayCompositeBufferBeforeHandle;
                builder.UseTexture(m_OverlayCompositeBufferHandle, AccessFlags.ReadWrite);
                builder.UseTexture(m_OverlayDepthStencilBufferHandle, AccessFlags.ReadWrite);
                if (m_OverlayCompositeBufferBeforeHandle.IsValid())
                    builder.UseTexture(m_OverlayCompositeBufferBeforeHandle, AccessFlags.Write);

                passData.rendererList = renderGraph.CreateUIOverlayRendererList(cameraData.camera, UISubset.UIToolkit_UGUI);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc(static (UnsafePassData data, UnsafeGraphContext context) =>
                {
                    // Copy the pre-UI buffer with CopyTexture (an exact copy; a shader blit would shift
                    // values slightly and break the diff).
                    if (data.bufferBefore.IsValid())
                    {
                        RTHandle src = data.colorTarget;
                        RTHandle dst = data.bufferBefore;
                        if (src != null && dst != null)
                            context.cmd.CopyTexture(src.nameID, dst.nameID);
                    }

                    // Bind the composite buffer as active RT: UIToolkit's BackdropFilterHelper samples
                    // RenderTexture.active at submission, so it must be set before DrawRendererList.
                    context.cmd.SetRenderTarget(data.colorTarget, data.depthTarget);
                    // Clear depth/stencil only (keep the seeded color): a stale stencil would drop the UI.
                    context.cmd.ClearRenderTarget(RTClearFlags.DepthStencil, Color.clear, 1.0f, 0);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }

            // Pass 2 — diff-composite the buffer over the backbuffer (non-UI pixels keep their FinalPost output).
            AddUIBackdropFilterCompositePass(renderGraph, m_OverlayCompositeBufferHandle, m_OverlayCompositeBufferBeforeHandle, destinationColor, profilingSampler, "Overlay Composite To Backbuffer");
        }

        internal void RenderOverlayIMGUI(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle colorBuffer, in TextureHandle depthBuffer)
        {
            var cameraData = frameData.Get<UniversalCameraData>();

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

                builder.SetRenderFunc(static (UnsafePassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.colorTarget);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }
    }
}
