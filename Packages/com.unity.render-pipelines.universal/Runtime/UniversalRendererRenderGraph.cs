using System;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Enumerates the identifiers to use with the FrameResource manager to get/set URP frame resources.
    /// </summary>
    public enum UniversalResource
    {
        /// <summary>
        /// The backbuffer color used to render directly to screen. All passes can write to it depending on frame setup.
        /// </summary>
        BackBufferColor,

        /// <summary>
        /// The backbuffer depth used to render directly to screen. All passes can write to it depending on frame setup.
        /// </summary>
        BackBufferDepth,

        // intermediate camera targets

        /// <summary>
        /// Main offscreen camera color target. All passes can write to it depending on frame setup.
        /// Can hold multiple samples if MSAA is enabled.
        /// </summary>
        CameraColor,
        /// <summary>
        /// Main offscreen camera depth target. All passes can write to it depending on frame setup.
        /// Can hold multiple samples if MSAA is enabled.
        /// </summary>
        CameraDepth,

        // shadows

        /// <summary>
        /// Main shadow map.
        /// </summary>
        MainShadowsTexture,
        /// <summary>
        /// Additional shadow map.
        /// </summary>
        AdditionalShadowsTexture,

        // gbuffer targets

        /// <summary>
        /// GBuffer0. Written to by the GBuffer pass.
        /// </summary>
        GBuffer0,
        /// <summary>
        /// GBuffer1. Written to by the GBuffer pass.
        /// </summary>
        GBuffer1,
        /// <summary>
        /// GBuffer2. Written to by the GBuffer pass.
        /// </summary>
        GBuffer2,
        /// <summary>
        /// GBuffer3. Written to by the GBuffer pass.
        /// </summary>
        GBuffer3,
        /// <summary>
        /// GBuffer4. Written to by the GBuffer pass.
        /// </summary>
        GBuffer4,
        /// <summary>
        /// GBuffer5. Written to by the GBuffer pass.
        /// </summary>
        GBuffer5,
        /// <summary>
        /// GBuffer6. Written to by the GBuffer pass.
        /// </summary>
        GBuffer6,

        // camera opaque/depth/normal

        /// <summary>
        /// Camera opaque texture. Contains a copy of CameraColor if the CopyColor pass is executed.
        /// </summary>
        CameraOpaqueTexture,
        /// <summary>
        /// Camera depth texture. Contains the scene depth if the CopyDepth or Depth Prepass passes are executed.
        /// </summary>
        CameraDepthTexture,
        /// <summary>
        /// Camera normals texture. Contains the scene depth if the DepthNormals Prepass pass is executed.
        /// </summary>
        CameraNormalsTexture,

        // motion vector

        /// <summary>
        /// Motion Vector Color. Written to by the Motion Vector passes.
        /// </summary>
        MotionVectorColor,
        /// <summary>
        /// Motion Vector Depth. Written to by the Motion Vector passes.
        /// </summary>
        MotionVectorDepth,

        // postFx

        /// <summary>
        /// Internal Color LUT. Written to by the InternalLUT pass.
        /// </summary>
        InternalColorLut,
        /// <summary>
        /// Color output of post-process passes (uberPost and finalPost) when HDR debug views are enabled. It replaces
        /// the backbuffer color as standard output because the later cannot be sampled back (or may not be in HDR format).
        /// If used, DebugHandler will perform the blit from DebugScreenTexture to BackBufferColor.
        /// </summary>
        DebugScreenColor,
        /// <summary>
        /// Depth output of post-process passes (uberPost and finalPost) when HDR debug views are enabled. It replaces
        /// the backbuffer depth as standard output because the later cannot be sampled back.
        /// </summary>
        DebugScreenDepth,
        /// <summary>
        /// After Post Process Color. Stores the contents of the main color target after the post processing passes.
        /// </summary>
        AfterPostProcessColor,
        /// <summary>
        /// Overlay UI Texture. The DrawScreenSpaceUI pass writes to this texture when rendering off-screen.
        /// </summary>
        OverlayUITexture,

        // rendering layers

        /// <summary>
        /// Rendering Layers Texture. Can be written to by the DrawOpaques pass or DepthNormals prepass based on settings.
        /// </summary>
        RenderingLayersTexture,

        // decals

        /// <summary>
        /// DBuffer0. Written to by the Decals pass.
        /// </summary>
        DBuffer0,
        /// <summary>
        /// DBuffer1. Written to by the Decals pass.
        /// </summary>
        DBuffer1,
        /// <summary>
        /// DBuffer2. Written to by the Decals pass.
        /// </summary>
        DBuffer2,

        /// <summary>
        /// DBufferDepth. Written to by the Decals pass.
        /// </summary>
        DBufferDepth,

        /// <summary>
        /// Screen Space Ambient Occlusion texture. Written to by the SSAO pass.
        /// </summary>
        SSAOTexture
    }

    public sealed partial class UniversalRenderer
    {
        // TODO RENDERGRAPH: Once all cameras will run in a single RenderGraph we should remove all RTHandles and use per frame RG textures.
        // We use 2 camera color handles so we can handle the edge case when a pass might want to read and write the same target.
        // This is not allowed so we just swap the current target, this keeps camera stacking working and avoids an extra blit pass.
        private static RTHandle[] m_RenderGraphCameraColorHandles = new RTHandle[]
        {
            null, null
        };
        private static RTHandle[] m_RenderGraphUpscaledCameraColorHandles = new RTHandle[]
        {
            null, null
        };
        private static RTHandle m_RenderGraphCameraDepthHandle;
        private static int m_CurrentColorHandle = 0;
        private static bool m_UseUpscaledColorHandle = false;

        private static RTHandle m_RenderGraphDebugTextureHandle;

        private RTHandle currentRenderGraphCameraColorHandle
        {
            get
            {
                // Select between the pre-upscale and post-upscale color handle sets based on the current upscaling state
                return m_UseUpscaledColorHandle ? m_RenderGraphUpscaledCameraColorHandles[m_CurrentColorHandle]
                                                : m_RenderGraphCameraColorHandles[m_CurrentColorHandle];
            }
        }

        // get the next m_RenderGraphCameraColorHandles and make it the new current for future accesses
        private RTHandle nextRenderGraphCameraColorHandle
        {
            get
            {
                m_CurrentColorHandle = (m_CurrentColorHandle + 1) % 2;
                return currentRenderGraphCameraColorHandle;
            }
        }

        // rendering layers
        private bool m_RequiresRenderingLayer;
        private RenderingLayerUtils.Event m_RenderingLayersEvent;
        private RenderingLayerUtils.MaskSize m_RenderingLayersMaskSize;
        private bool m_RenderingLayerProvidesRenderObjectPass;
        private bool m_RenderingLayerProvidesByDepthNormalPass;
        private string m_RenderingLayersTextureName;

        private void CleanupRenderGraphResources()
        {
            m_RenderGraphCameraColorHandles[0]?.Release();
            m_RenderGraphCameraColorHandles[1]?.Release();
            m_RenderGraphUpscaledCameraColorHandles[0]?.Release();
            m_RenderGraphUpscaledCameraColorHandles[1]?.Release();
            m_RenderGraphCameraDepthHandle?.Release();

            m_RenderGraphDebugTextureHandle?.Release();
        }

        /// <summary>
        /// Utility method to convert RenderTextureDescriptor to TextureHandle and create a RenderGraph texture
        /// </summary>
        /// <param name="renderGraph"></param>
        /// <param name="desc"></param>
        /// <param name="name"></param>
        /// <param name="clear"></param>
        /// <param name="filterMode"></param>
        /// <param name="wrapMode"></param>
        /// <returns></returns>
        public static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc, string name, bool clear,
            FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            TextureDesc rgDesc = new TextureDesc(desc.width, desc.height);
            rgDesc.dimension = desc.dimension;
            rgDesc.clearBuffer = clear;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.colorFormat = desc.graphicsFormat;
            rgDesc.depthBufferBits = (DepthBits)desc.depthBufferBits;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.name = name;
            rgDesc.enableRandomWrite = desc.enableRandomWrite;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;
            rgDesc.isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None && desc.depthStencilFormat != GraphicsFormat.None;
            rgDesc.vrUsage = desc.vrUsage;
            // TODO RENDERGRAPH: depthStencilFormat handling?

            return renderGraph.CreateTexture(rgDesc);
        }

        internal static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc, string name, bool clear, Color color,
            FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            TextureDesc rgDesc = new TextureDesc(desc.width, desc.height);
            rgDesc.dimension = desc.dimension;
            rgDesc.clearBuffer = clear;
            rgDesc.clearColor = color;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.colorFormat = desc.graphicsFormat;
            rgDesc.depthBufferBits = (DepthBits)desc.depthBufferBits;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.name = name;
            rgDesc.enableRandomWrite = desc.enableRandomWrite;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;

            return renderGraph.CreateTexture(rgDesc);
        }

        bool ShouldApplyPostProcessing(bool postProcessEnabled)
        {
            return postProcessEnabled && m_PostProcessPasses.isCreated;
        }

        bool CameraHasPostProcessingWithDepth(UniversalCameraData cameraData)
        {
            return ShouldApplyPostProcessing(cameraData.postProcessEnabled) && cameraData.postProcessingRequiresDepthTexture;
        }

        void RequiresColorAndDepthTextures(RenderGraph renderGraph, out bool createColorTexture, out bool createDepthTexture, UniversalCameraData cameraData, ref RenderPassInputSummary renderPassInputs)
        {
            bool isPreviewCamera = cameraData.isPreviewCamera;
            bool requiresDepthPrepass = RequireDepthPrepass(cameraData, ref renderPassInputs);

            var requireColorTexture = HasActiveRenderFeatures() && m_IntermediateTextureMode == IntermediateTextureMode.Always;
            requireColorTexture |= HasPassesRequiringIntermediateTexture();
            requireColorTexture |= Application.isEditor && m_Clustering;
            requireColorTexture |= RequiresIntermediateColorTexture(cameraData, ref renderPassInputs);
            requireColorTexture &= !isPreviewCamera;

            var requireDepthTexture = RequireDepthTexture(cameraData, requiresDepthPrepass, ref renderPassInputs);

            useDepthPriming = IsDepthPrimingEnabled(cameraData);

            // Intermediate texture has different yflip state than backbuffer. In case we use intermediate texture, we must use both color and depth together.
            bool intermediateRenderTexture = (requireColorTexture || requireDepthTexture);
            createDepthTexture = intermediateRenderTexture;
            createColorTexture = intermediateRenderTexture;
        }

        // Gather history render requests and manage camera history texture life-time.
        private void UpdateCameraHistory(UniversalCameraData cameraData)
        {
            // NOTE: Can be null for non-game cameras.
            // Technically each camera has AdditionalCameraData which owns the historyManager.
            if (cameraData != null && cameraData.historyManager != null)
            {
                // XR multipass renders the frame twice, avoid updating camera history twice.
                bool xrMultipassEnabled = false;
                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;
                multipassId = cameraData.xr.multipassId;
#endif
                bool isNewFrame = !xrMultipassEnabled || (multipassId == 0);

                if (isNewFrame)
                {
                    var history = cameraData.historyManager;

                    // Gather all external user requests by callback.
                    history.GatherHistoryRequests();

                    // Typically we would also gather all the internal requests here before checking for unused textures.
                    // However the requests are versioned in the history manager, so we can defer the clean up for couple frames.

                    // Garbage collect all the unused persistent data instances. Free GPU resources if any.
                    // This will start a new "history frame".
                    history.ReleaseUnusedHistory();

                    // Swap and cycle camera history RTHandles. Update the reference size for the camera history RTHandles.
                    history.SwapAndSetReferenceSize(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
                }
            }
        }

        const string _CameraTargetAttachmentAName = "_CameraTargetAttachmentA";
        const string _CameraTargetAttachmentBName = "_CameraTargetAttachmentB";
        const string _CameraUpscaledTargetAttachmentAName = "_CameraUpscaledTargetAttachmentA";
        const string _CameraUpscaledTargetAttachmentBName = "_CameraUpscaledTargetAttachmentB";

        void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, bool isCameraTargetOffscreenDepth)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            bool lastCameraInTheStack = cameraData.resolveFinalTarget;
            bool isBuiltInTexture = (cameraData.targetTexture == null);

            RenderTargetIdentifier targetColorId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.Depth;

            bool clearColor = cameraData.renderType == CameraRenderType.Base;
            bool clearDepth = cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth;
            // if the camera background type is "uninitialized" clear using a yellow color, so users can clearly understand the underlying behaviour
            // only exception being if we are rendering to an external texture
            Color cameraBackgroundColor = (cameraData.camera.clearFlags == CameraClearFlags.Nothing && cameraData.targetTexture == null) ? Color.yellow : cameraData.backgroundColor;

            // If scene filtering is enabled (prefab edit mode), the filtering is implemented compositing some builtin ImageEffect passes.
            // For the composition to work, we need to clear the color buffer alpha to 0
            // How filtering works:
            // - SRP frame is fully rendered as background
            // - builtin ImageEffect pass grey-out of the full scene previously rendered
            // - SRP frame rendering only the objects belonging to the prefab being edited (with clearColor.a = 0)
            // - builtin ImageEffect pass compositing the two previous passes
            // TODO: We should implement filtering fully in SRP to remove builtin dependencies
            if (IsSceneFilteringEnabled(cameraData.camera))
            {
                cameraBackgroundColor.a = 0;
                clearDepth = false;
            }

            // Certain debug modes (e.g. wireframe/overdraw modes) require that we override clear flags and clear everything.
            var debugHandler = cameraData.renderer.DebugHandler;
            if (debugHandler != null && debugHandler.IsActiveForCamera(cameraData.isPreviewCamera) && debugHandler.IsScreenClearNeeded)
            {
                clearColor = true;
                clearDepth = true;
                if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
                {
                    DebugHandler.TryGetScreenClearColor(ref cameraBackgroundColor);
                }
            }

            ImportResourceParams importColorParams = new ImportResourceParams();
            importColorParams.clearOnFirstUse = clearColor; // && cameraData.camera.clearFlags != CameraClearFlags.Nothing;
            importColorParams.clearColor = cameraBackgroundColor;
            importColorParams.discardOnLastUse = false;

            ImportResourceParams importDepthParams = new ImportResourceParams();
            importDepthParams.clearOnFirstUse = clearDepth;
            importDepthParams.clearColor = cameraBackgroundColor;
            importDepthParams.discardOnLastUse = false;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                targetColorId = cameraData.xr.renderTarget;
                targetDepthId = cameraData.xr.renderTarget;
                isBuiltInTexture = false;
            }
#endif

            if (m_TargetColorHandle == null)
            {
                m_TargetColorHandle = RTHandles.Alloc(targetColorId, "Backbuffer color");
            }
            else if(m_TargetColorHandle.nameID != targetColorId)
            {
                RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref m_TargetColorHandle, targetColorId);
            }

            if (m_TargetDepthHandle == null)
            {
                m_TargetDepthHandle = RTHandles.Alloc(targetDepthId, "Backbuffer depth");
            }
            else if (m_TargetDepthHandle.nameID != targetDepthId)
            {
                RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref m_TargetDepthHandle, targetDepthId);
            }

            // Gather render pass history requests and update history textures.
            UpdateCameraHistory(cameraData);

            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(cameraData.IsTemporalAAEnabled(), postProcessingData.isEnabled);

            // Enable depth normal prepass if it's needed by rendering layers
            if (m_RenderingLayerProvidesByDepthNormalPass)
                renderPassInputs.requiresNormalsTexture = true;

            // We configure this for the first camera of the stack and overlay camera will reuse create color/depth var
            // to pick the correct target, as if there is an intermediate texture, overlay cam should use them
            if (cameraData.renderType == CameraRenderType.Base)
                RequiresColorAndDepthTextures(renderGraph, out m_CreateColorTexture, out m_CreateDepthTexture, cameraData, ref renderPassInputs);


            // The final output back buffer should be cleared by the graph on first use only if we have no final blit pass.
            // If there is a final blit, that blit will write the buffers so on first sight an extra clear should not be problem,
            // blit will simply blit over the cleared values. BUT! the final blit may not write the whole buffer in case of a camera
            // with a Viewport Rect smaller than the full screen. So the existing backbuffer contents need to be preserved in this case.
            // Finally for non-base cameras the backbuffer should never be cleared. (Note that there might still be two base cameras
            // rendering to the same screen. See e.g. test foundation 014 that renders a minimap)
            bool clearBackbufferOnFirstUse = (cameraData.renderType == CameraRenderType.Base) && !m_CreateColorTexture;

            // force the clear if we are rendering to an offscreen depth texture
            clearBackbufferOnFirstUse |= isCameraTargetOffscreenDepth;

            // UI Overlay is rendered by native engine if not done within SRP
            // To check if the engine does it natively post-URP, we look at SupportedRenderingFeatures
            // and restrict it to cases where we resolve to screen and render UI overlay, i.e mostly final camera for game view
            // We cannot use directly !cameraData.rendersOverlayUI but this is similar logic
            bool isNativeUIOverlayRenderingAfterURP = !SupportedRenderingFeatures.active.rendersUIOverlay && cameraData.resolveToScreen;
            bool isNativeRenderingAfterURP = UnityEngine.Rendering.Watermark.IsVisible() || isNativeUIOverlayRenderingAfterURP;
            // If MSAA > 1, no extra native rendering after SRP and we target the BB directly (!m_CreateColorTexture)
            // then we can discard MSAA buffers and only resolve, otherwise we must store and resolve
            bool noStoreOnlyResolveBBColor = !m_CreateColorTexture && !isNativeRenderingAfterURP && (cameraData.cameraTargetDescriptor.msaaSamples > 1);

            ImportResourceParams importBackbufferColorParams = new ImportResourceParams();
            importBackbufferColorParams.clearOnFirstUse = clearBackbufferOnFirstUse;
            importBackbufferColorParams.clearColor = cameraBackgroundColor;
            importBackbufferColorParams.discardOnLastUse = noStoreOnlyResolveBBColor;

            ImportResourceParams importBackbufferDepthParams = new ImportResourceParams();
            importBackbufferDepthParams.clearOnFirstUse = clearBackbufferOnFirstUse;
            importBackbufferDepthParams.clearColor = cameraBackgroundColor;
            importBackbufferDepthParams.discardOnLastUse = !isCameraTargetOffscreenDepth;

#if UNITY_EDITOR
            // on TBDR GPUs like Apple M1/M2, we need to preserve the backbuffer depth for overlay cameras in Editor for Gizmos
            if (cameraData.isSceneViewCamera)
                importBackbufferDepthParams.discardOnLastUse = false;
#endif

            // For BuiltinRenderTextureType wrapping RTHandles RenderGraph can't know what they are so we have to pass it in.
            RenderTargetInfo importInfo = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();

            // So the render target we pass into render graph is
            // RTHandles(RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget))
            // or
            // RTHandles(RenderTargetIdentifier(RenderTexture(cameraData.targetTexture)))
            //
            // Because of the RenderTargetIdentifier in the "wrapper chain" the graph can't know
            // the size of the passed in textures so we need to provide it for the graph.
            // The amount of texture handle wrappers and their subtleties is probably something to be investigated.
            if (isBuiltInTexture)
            {
                bool msaaSamplesChangedThisFrame = false;
                int oldSamples = cameraData.cameraTargetDescriptor.msaaSamples;
#if !UNITY_EDITOR
                // for safety do this only for the NRP path, even though works also on non NRP, but would need extensive testing
                if (m_CreateColorTexture && renderGraph.nativeRenderPassesEnabled && Screen.msaaSamples > 1)
                {
                    oldSamples = Mathf.Max(Screen.msaaSamples, oldSamples);
                    msaaSamplesChangedThisFrame = true;
                    Screen.SetMSAASamples(1);
                }
#endif
                int numSamples = Mathf.Max(Screen.msaaSamples, 1);

                // Handle edge cases regarding numSamples setup
                // On OSX player, the Screen API MSAA samples change request is only applied in the following frame,
                // as a workaround we keep the old MSAA sample count for the previous frame
                // this workaround can be removed once the Screen API issue (UUM-42825) is fixed
                // The editor always allocates the system rendertarget with a single msaa sample
                // See: ConfigureTargetTexture in PlayModeView.cs
                if (msaaSamplesChangedThisFrame && Application.platform == RuntimePlatform.OSXPlayer)
                    numSamples = oldSamples;
                else if (Application.isEditor)
                    numSamples = 1;

                //BuiltinRenderTextureType.CameraTarget so this is either system render target or camera.targetTexture if non null
                //NOTE: Careful what you use here as many of the properties bake-in the camera rect so for example
                //cameraData.cameraTargetDescriptor.width is the width of the rectangle but not the actual render target
                //same with cameraData.camera.pixelWidth
                importInfo.width = Screen.width;
                importInfo.height = Screen.height;
                importInfo.volumeDepth = 1;
                importInfo.msaaSamples = numSamples;

                importInfo.format = UniversalRenderPipeline.MakeRenderTextureGraphicsFormat(cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, Graphics.preserveFramebufferAlpha);

                importInfoDepth = importInfo;
                importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }
            else
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    importInfo.width = cameraData.xr.renderTargetDesc.width;
                    importInfo.height = cameraData.xr.renderTargetDesc.height;
                    importInfo.volumeDepth = cameraData.xr.renderTargetDesc.volumeDepth;
                    importInfo.msaaSamples = cameraData.xr.renderTargetDesc.msaaSamples;
                    importInfo.format = cameraData.xr.renderTargetDesc.graphicsFormat;

                    importInfoDepth = importInfo;
                    importInfoDepth.format = cameraData.xr.renderTargetDesc.depthStencilFormat;
                }
                else
#endif
                {
                    importInfo.width = cameraData.targetTexture.width;
                    importInfo.height = cameraData.targetTexture.height;
                    importInfo.volumeDepth = cameraData.targetTexture.volumeDepth;
                    importInfo.msaaSamples = cameraData.targetTexture.antiAliasing;
                    importInfo.format = cameraData.targetTexture.graphicsFormat;

                    importInfoDepth = importInfo;
                    importInfoDepth.format = cameraData.targetTexture.depthStencilFormat;
                }

                // We let users know that a depth format is required for correct usage, but we fallback to the old default depth format behaviour to avoid regressions
                if (importInfoDepth.format == GraphicsFormat.None)
                {
                    importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
                    Debug.LogWarning("Trying to render to a rendertexture without a depth buffer. URP+RG needs a depthbuffer to render.");
                }
            }

            // TODO: Don't think the backbuffer color and depth should be imported at all if !isBuiltinTexture, double check
            if (!isCameraTargetOffscreenDepth)
                resourceData.backBufferColor = renderGraph.ImportTexture(m_TargetColorHandle, importInfo, importBackbufferColorParams);

            resourceData.backBufferDepth = renderGraph.ImportTexture(m_TargetDepthHandle, importInfoDepth, importBackbufferDepthParams);

            #region Intermediate Camera Target

            if (m_CreateColorTexture && !isCameraTargetOffscreenDepth)
            {
                var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.useMipMap = false;
                cameraTargetDescriptor.autoGenerateMips = false;
                cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

                RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraColorHandles[0], cameraTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: _CameraTargetAttachmentAName);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraColorHandles[1], cameraTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: _CameraTargetAttachmentBName);

                // Make sure that the base camera always starts rendering to the ColorAttachmentA for deterministic frame results.
                // Not doing so makes the targets look different every frame, causing the frame debugger to flash, and making debugging harder.
                if (cameraData.renderType == CameraRenderType.Base)
                {
                    m_CurrentColorHandle = 0;

                    // Base camera rendering always starts with a pre-upscale size color target
                    // If upscaling happens during the frame, we'll switch to the post-upscale color target size and any overlay camera that renders on top should inherit the upscaled size
                    m_UseUpscaledColorHandle = false;
                }

                importColorParams.discardOnLastUse = lastCameraInTheStack;
                resourceData.cameraColor = renderGraph.ImportTexture(currentRenderGraphCameraColorHandle, importColorParams);
                resourceData.activeColorID = UniversalResourceData.ActiveID.Camera;

                // If STP is enabled, we'll be upscaling the rendered frame during the post processing logic.
                // Once upscaling occurs, we must use different set of color handles that reflect the upscaled size.
                if (cameraData.IsSTPEnabled())
                {
                    var upscaledTargetDesc = cameraTargetDescriptor;
                    upscaledTargetDesc.width = cameraData.pixelWidth;
                    upscaledTargetDesc.height = cameraData.pixelHeight;

                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphUpscaledCameraColorHandles[0], upscaledTargetDesc, FilterMode.Point, TextureWrapMode.Clamp, name: _CameraUpscaledTargetAttachmentAName);
                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphUpscaledCameraColorHandles[1], upscaledTargetDesc, FilterMode.Point, TextureWrapMode.Clamp, name: _CameraUpscaledTargetAttachmentBName);
                }
            }
            else
            {
                resourceData.activeColorID = UniversalResourceData.ActiveID.BackBuffer;
            }


            if (m_CreateDepthTexture)
            {
                var depthDescriptor = cameraData.cameraTargetDescriptor;
                depthDescriptor.useMipMap = false;
                depthDescriptor.autoGenerateMips = false;
                depthDescriptor.bindMS = false;

                bool hasMSAA = depthDescriptor.msaaSamples > 1 && (SystemInfo.supportsMultisampledTextures != 0);
                bool resolveDepth = RenderingUtils.MultisampleDepthResolveSupported() && renderGraph.nativeRenderPassesEnabled;

                // TODO RENDERGRAPH: once all passes are ported to RasterCommandBuffers we need to reenable depth resolve
                m_CopyDepthMode = renderPassInputs.requiresDepthTextureEarliestEvent < RenderPassEvent.AfterRenderingTransparents ? CopyDepthMode.AfterOpaques : m_CopyDepthMode;
                m_CopyDepthPass.m_CopyResolvedDepth = resolveDepth && m_CopyDepthMode == CopyDepthMode.AfterTransparents;

                if (hasMSAA)
                {
                    // if depth priming is enabled the copy depth primed pass is meant to do the MSAA resolve, so we want to bind the MS surface
                    if (IsDepthPrimingEnabled(cameraData))
                        depthDescriptor.bindMS = true;
                    else
                        depthDescriptor.bindMS = !(resolveDepth && m_CopyDepthMode == CopyDepthMode.AfterTransparents);
                }

                // binding MS surfaces is not supported by the GLES backend, and it won't be fixed after investigating
                // the high performance impact of potential fixes, which would make it more expensive than depth prepass (fogbugz 1339401 for more info)
                if (IsGLESDevice())
                    depthDescriptor.bindMS = false;

                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;

                RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphCameraDepthHandle, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthAttachment");

                importDepthParams.discardOnLastUse = lastCameraInTheStack;
                resourceData.cameraDepth = renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle, importDepthParams);
                resourceData.activeDepthID = UniversalResourceData.ActiveID.Camera;
            }
            else
            {
                resourceData.activeDepthID = UniversalResourceData.ActiveID.BackBuffer;
            }
            #endregion

            CreateCameraDepthCopyTexture(renderGraph, cameraData.cameraTargetDescriptor, RequireDepthPrepass(cameraData, ref renderPassInputs) && this.renderingModeActual != RenderingMode.Deferred);

            CreateCameraNormalsTexture(renderGraph, cameraData.cameraTargetDescriptor);

            CreateMotionVectorTextures(renderGraph, cameraData.cameraTargetDescriptor);

            CreateRenderingLayersTexture(renderGraph, cameraData.cameraTargetDescriptor);

            if (!isCameraTargetOffscreenDepth)
                CreateAfterPostProcessTexture(renderGraph, cameraData.cameraTargetDescriptor);
        }

        void SetupRenderingLayers(int msaaSamples)
        {
            // Gather render pass require rendering layers event and mask size
            m_RequiresRenderingLayer = RenderingLayerUtils.RequireRenderingLayers(this, rendererFeatures, msaaSamples,
                out m_RenderingLayersEvent, out m_RenderingLayersMaskSize);

            m_RenderingLayerProvidesRenderObjectPass = m_RequiresRenderingLayer && renderingModeActual == RenderingMode.Forward && m_RenderingLayersEvent == RenderingLayerUtils.Event.Opaque;
            m_RenderingLayerProvidesByDepthNormalPass = m_RequiresRenderingLayer && m_RenderingLayersEvent == RenderingLayerUtils.Event.DepthNormalPrePass;

            if (m_DeferredLights != null)
            {
                m_DeferredLights.RenderingLayerMaskSize = m_RenderingLayersMaskSize;
                m_DeferredLights.UseDecalLayers = m_RequiresRenderingLayer;
            }
        }

        internal void SetupRenderGraphLights(RenderGraph renderGraph, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            m_ForwardLights.SetupRenderGraphLights(renderGraph, renderingData, cameraData, lightData);
            if (this.renderingModeActual == RenderingMode.Deferred)
            {
                m_DeferredLights.UseFramebufferFetch = renderGraph.nativeRenderPassesEnabled;
                m_DeferredLights.SetupRenderGraphLights(renderGraph, cameraData, lightData);
            }
        }

        // "Raw render" color/depth history.
        // Should include opaque and transparent geometry before TAA or any post-processing effects. No UI overlays etc.
        private void RenderRawColorDepthHistory(RenderGraph renderGraph, UniversalCameraData cameraData, UniversalResourceData resourceData)
        {
            if (cameraData != null && cameraData.historyManager != null && resourceData != null)
            {
                UniversalCameraHistory history = cameraData.historyManager;

                bool xrMultipassEnabled = false;
                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;
                multipassId = cameraData.xr.multipassId;
#endif

                if (history.IsAccessRequested<RawColorHistory>() && resourceData.cameraColor.IsValid())
                {
                    var colorHistory = history.GetHistoryForWrite<RawColorHistory>();
                    if (colorHistory != null)
                    {
                        colorHistory.Update(ref cameraData.cameraTargetDescriptor, xrMultipassEnabled);
                        if (colorHistory.GetCurrentTexture(multipassId) != null)
                        {
                            var colorHistoryTarget = renderGraph.ImportTexture(colorHistory.GetCurrentTexture(multipassId));
                            // See pass create in UniversalRenderer() for execution order.
                            m_HistoryRawColorCopyPass.RenderToExistingTexture(renderGraph, frameData, colorHistoryTarget, resourceData.cameraColor, Downsampling.None, "Copy Raw Color History");
                        }
                    }
                }

                if (history.IsAccessRequested<RawDepthHistory>() && resourceData.cameraDepth.IsValid())
                {
                    var depthHistory = history.GetHistoryForWrite<RawDepthHistory>();
                    if (depthHistory != null)
                    {
                        if (m_HistoryRawDepthCopyPass.CopyToDepth == false)
                        {
                            // Fall back to R32_Float if depth copy is disabled.
                            var tempColorDepthDesc = cameraData.cameraTargetDescriptor;
                            tempColorDepthDesc.colorFormat = RenderTextureFormat.RFloat;
                            tempColorDepthDesc.graphicsFormat = GraphicsFormat.R32_SFloat;
                            tempColorDepthDesc.depthBufferBits = 0;
                            depthHistory.Update(ref tempColorDepthDesc, xrMultipassEnabled);
                        }
                        else
                            depthHistory.Update(ref cameraData.cameraTargetDescriptor, xrMultipassEnabled);

                        if (depthHistory.GetCurrentTexture(multipassId) != null)
                        {
                            var depthHistoryTarget = renderGraph.ImportTexture(depthHistory.GetCurrentTexture(multipassId));
                            // See pass create in UniversalRenderer() for execution order.
                            m_HistoryRawDepthCopyPass.Render(renderGraph, frameData, depthHistoryTarget, resourceData.cameraDepth, false, "Copy Raw Depth History");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called before recording the render graph. Can be used to initialize resources.
        /// </summary>
        public override void OnBeginRenderGraphFrame()
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            resourceData.InitFrame();
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            useRenderPassEnabled = renderGraph.nativeRenderPassesEnabled;

            MotionVectorRenderPass.SetRenderGraphMotionVectorGlobalMatrices(renderGraph, cameraData);

            SetupRenderGraphLights(renderGraph, renderingData, cameraData, lightData);

            SetupRenderingLayers(cameraData.cameraTargetDescriptor.msaaSamples);

            bool isCameraTargetOffscreenDepth = cameraData.camera.targetTexture != null && cameraData.camera.targetTexture.format == RenderTextureFormat.Depth;

            CreateRenderGraphCameraRenderTargets(renderGraph, isCameraTargetOffscreenDepth);

            if (DebugHandler != null)
                DebugHandler.Setup(renderGraph, cameraData.isPreviewCamera);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRendering);

            SetupRenderGraphCameraProperties(renderGraph, resourceData.isActiveTargetBackBuffer);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            ProcessVFXCameraCommand(renderGraph);
#endif
            cameraData.renderer.useDepthPriming = useDepthPriming;

            if (isCameraTargetOffscreenDepth)
            {
                OnOffscreenDepthTextureRendering(renderGraph, context, resourceData, cameraData);
                return;
            }

            OnBeforeRendering(renderGraph);

            BeginRenderGraphXRRendering(renderGraph);

            OnMainRendering(renderGraph, context);

            OnAfterRendering(renderGraph);

            EndRenderGraphXRRendering(renderGraph);
        }

        /// <summary>
        /// Called after recording the render graph. Can be used to clean up resources.
        /// </summary>
        public override void OnEndRenderGraphFrame()
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            resourceData.EndFrame();
        }

        internal override void OnFinishRenderGraphRendering(CommandBuffer cmd)
        {
            if (this.renderingModeActual == RenderingMode.Deferred)
                m_DeferredPass.OnCameraCleanup(cmd);

            m_CopyDepthPass.OnCameraCleanup(cmd);
            m_DepthNormalPrepass.OnCameraCleanup(cmd);
        }

        /// <summary>
        /// Used to determine if this renderer supports the use of GPU occlusion culling.
        /// </summary>
        public override bool supportsGPUOcclusion => m_RenderingMode != RenderingMode.Deferred;

        private static bool m_CreateColorTexture;
        private static bool m_CreateDepthTexture;

        private void OnOffscreenDepthTextureRendering(RenderGraph renderGraph, ScriptableRenderContext context, UniversalResourceData resourceData, UniversalCameraData cameraData)
        {
            if (!renderGraph.nativeRenderPassesEnabled)
                ClearTargetsPass.Render(renderGraph, resourceData.activeColorTexture, resourceData.backBufferDepth, RTClearFlags.Depth, cameraData.backgroundColor);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingShadows, RenderPassEvent.BeforeRenderingOpaques);
            m_RenderOpaqueForwardPass.Render(renderGraph, frameData, TextureHandle.nullHandle, resourceData.backBufferDepth, TextureHandle.nullHandle, TextureHandle.nullHandle, uint.MaxValue);
            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingOpaques, RenderPassEvent.BeforeRenderingTransparents);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            m_RenderTransparentForwardPass.Render(renderGraph, frameData, TextureHandle.nullHandle, resourceData.backBufferDepth, TextureHandle.nullHandle, TextureHandle.nullHandle, uint.MaxValue);
            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingTransparents, RenderPassEvent.AfterRendering);
        }
        private void OnBeforeRendering(RenderGraph renderGraph)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            m_ForwardLights.PreSetup(renderingData, cameraData, lightData);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingShadows);

            bool renderShadows = false;

            if (m_MainLightShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData))
            {
                renderShadows = true;
                resourceData.mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderGraph, frameData);
            }

            if (m_AdditionalLightsShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData))
            {
                renderShadows = true;
                resourceData.additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderGraph, frameData);
            }

            // The camera need to be setup again after the shadows since those passes override some settings
            // TODO RENDERGRAPH: move the setup code into the shadow passes
            if (renderShadows)
                SetupRenderGraphCameraProperties(renderGraph, resourceData.isActiveTargetBackBuffer);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingShadows);
        }

        private void UpdateInstanceOccluders(RenderGraph renderGraph, UniversalCameraData cameraData, TextureHandle depthTexture)
        {
            int scaledWidth = (int)(cameraData.pixelWidth * cameraData.renderScale);
            int scaledHeight = (int)(cameraData.pixelHeight * cameraData.renderScale);
            bool isSinglePassXR = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;
            var occluderParams = new OccluderParameters(cameraData.camera.GetInstanceID())
            {
                subviewCount = isSinglePassXR ? 2 : 1,
                depthTexture = depthTexture,
                depthSize = new Vector2Int(scaledWidth, scaledHeight),
                depthIsArray = isSinglePassXR,
            };
            Span<OccluderSubviewUpdate> occluderSubviewUpdates = stackalloc OccluderSubviewUpdate[occluderParams.subviewCount];
            for (int subviewIndex = 0; subviewIndex < occluderParams.subviewCount; ++subviewIndex)
            {
                var viewMatrix = cameraData.GetViewMatrix(subviewIndex);
                var projMatrix = cameraData.GetProjectionMatrix(subviewIndex);
                occluderSubviewUpdates[subviewIndex] = new OccluderSubviewUpdate(subviewIndex)
                {
                    depthSliceIndex = subviewIndex,
                    viewMatrix = viewMatrix,
                    invViewMatrix = viewMatrix.inverse,
                    gpuProjMatrix = GL.GetGPUProjectionMatrix(projMatrix, true),
                    viewOffsetWorldSpace = Vector3.zero,
                };
            }
            GPUResidentDrawer.UpdateInstanceOccluders(renderGraph, occluderParams, occluderSubviewUpdates);
        }

        private void InstanceOcclusionTest(RenderGraph renderGraph, UniversalCameraData cameraData, OcclusionTest occlusionTest)
        {
            bool isSinglePassXR = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;
            int subviewCount = isSinglePassXR ? 2 : 1;
            var settings = new OcclusionCullingSettings(cameraData.camera.GetInstanceID(), occlusionTest)
            {
                instanceMultiplier = (isSinglePassXR && !SystemInfo.supportsMultiview) ? 2 : 1,
            };
            Span<SubviewOcclusionTest> subviewOcclusionTests = stackalloc SubviewOcclusionTest[subviewCount];
            for (int subviewIndex = 0; subviewIndex < subviewCount; ++subviewIndex)
            {
                subviewOcclusionTests[subviewIndex] = new SubviewOcclusionTest()
                {
                    cullingSplitIndex = 0,
                    occluderSubviewIndex = subviewIndex,
                };
            }
            GPUResidentDrawer.InstanceOcclusionTest(renderGraph, settings, subviewOcclusionTests);
        }

        private void OnMainRendering(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            if (!renderGraph.nativeRenderPassesEnabled)
            {
                RTClearFlags clearFlags = (RTClearFlags) GetCameraClearFlag(cameraData);

                if (clearFlags != RTClearFlags.None)
                    ClearTargetsPass.Render(renderGraph, resourceData.activeColorTexture, resourceData.activeDepthTexture, clearFlags, cameraData.backgroundColor);
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingPrePasses);

            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = CameraHasPostProcessingWithDepth(cameraData);

            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(cameraData.IsTemporalAAEnabled(), postProcessingData.isEnabled);

            if (m_RenderingLayerProvidesByDepthNormalPass)
                renderPassInputs.requiresNormalsTexture = true;

#if UNITY_EDITOR
            if (ProbeReferenceVolume.instance.IsProbeSamplingDebugEnabled())
                renderPassInputs.requiresNormalsTexture = true;
#endif

            bool requiresDepthPrepass = RequireDepthPrepass(cameraData, ref renderPassInputs);
            bool requiresDepthCopyPass = !requiresDepthPrepass
                                         && (cameraData.requiresDepthTexture || cameraHasPostProcessingWithDepth || renderPassInputs.requiresDepthTexture)
                                         && m_CreateDepthTexture; // we create both intermediate textures if this is true, so instead of repeating the checks we reuse this

            requiresDepthCopyPass |= !requiresDepthPrepass && DebugHandlerRequireDepthPass(frameData.Get<UniversalCameraData>());
            bool requiresColorCopyPass = cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture;
            requiresColorCopyPass &= !cameraData.isPreviewCamera;
            bool requiredColorGradingLutPass = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            bool isDeferred = this.renderingModeActual == RenderingMode.Deferred;

            bool needsOccluderUpdate = cameraData.useGPUOcclusionCulling;
            if (requiresDepthPrepass)
            {
                // TODO RENDERGRAPH: is this always a valid assumption for deferred rendering?
                TextureHandle depthTarget = (renderingModeActual == RenderingMode.Deferred) ? resourceData.activeDepthTexture : resourceData.cameraDepthTexture;
                depthTarget = (useDepthPriming && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth)) ? resourceData.activeDepthTexture : depthTarget;

                var passCount = needsOccluderUpdate ? 2 : 1;
                for (int passIndex = 0; passIndex < passCount; ++passIndex)
                {
                    uint batchLayerMask = uint.MaxValue;
                    if (needsOccluderUpdate)
                    {
                        // first pass: test everything against previous frame final depth pyramid
                        // second pass: re-test culled against current frame intermediate depth pyramid
                        OcclusionTest occlusionTest = (passIndex == 0) ? OcclusionTest.TestAll : OcclusionTest.TestCulled;
                        InstanceOcclusionTest(renderGraph, cameraData, occlusionTest);
                        batchLayerMask = occlusionTest.GetBatchLayerMask();
                    }

                    bool isLastPass = (passIndex == (passCount - 1));
                    if (renderPassInputs.requiresNormalsTexture)
                        DepthNormalPrepassRender(renderGraph, renderPassInputs, depthTarget, batchLayerMask, isLastPass);
                    else
                    {
                        m_DepthPrepass.Render(renderGraph, frameData, ref depthTarget, batchLayerMask);
                        if (isLastPass && !useDepthPriming && depthTarget.IsValid())
                            RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID("_CameraDepthTexture"), depthTarget, "Set Global Depth Texture");
                    }

                    if (needsOccluderUpdate)
                    {
                        // first pass: make current frame intermediate depth pyramid
                        // second pass: make current frame final depth pyramid, set occlusion test results for later passes
                        UpdateInstanceOccluders(renderGraph, cameraData, depthTarget);
                        if (passIndex != 0)
                            InstanceOcclusionTest(renderGraph, cameraData, OcclusionTest.TestAll);
                    }
                }
                needsOccluderUpdate = false;
            }

            // depth priming still needs to copy depth because the prepass doesn't target anymore CameraDepthTexture
            // TODO: this is unoptimal, investigate optimizations
            if (useDepthPriming)
            {
                TextureHandle depth = resourceData.cameraDepth;
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                m_PrimedDepthCopyPass.Render(renderGraph, frameData, cameraDepthTexture, depth, true);
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingPrePasses);

            if (requiredColorGradingLutPass)
            {
                TextureHandle internalColorLut;
                m_PostProcessPasses.colorGradingLutPass.Render(renderGraph, frameData, out internalColorLut);
                resourceData.internalColorLut = internalColorLut;
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                m_XROcclusionMeshPass.Render(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture);
#endif

            if (isDeferred)
            {
                m_DeferredLights.Setup(m_AdditionalLightsShadowCasterPass);
                if (m_DeferredLights != null)
                {
                    // We need to be sure there are no custom passes in between GBuffer/Deferred passes, if there are - we disable fb fetch just to be safe`
                    m_DeferredLights.UseFramebufferFetch = renderGraph.nativeRenderPassesEnabled;
                    m_DeferredLights.HasNormalPrepass = renderPassInputs.requiresNormalsTexture;
                    m_DeferredLights.HasDepthPrepass = requiresDepthPrepass;
                    m_DeferredLights.ResolveMixedLightingMode(lightData);
                    m_DeferredLights.IsOverlay = cameraData.renderType == CameraRenderType.Overlay;
                }

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingGbuffer);

                m_GBufferPass.Render(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture);

                if (!renderGraph.nativeRenderPassesEnabled)
                {
                    TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                    m_GBufferCopyDepthPass.Render(renderGraph, frameData, cameraDepthTexture, resourceData.activeDepthTexture, true, "GBuffer Depth Copy");
                }
                else
                {
                    // if nativeRenderPassesEnabled, we write the camera depth to gBuffer[4], to be used with framebuffer fetch
                    resourceData.cameraDepthTexture = resourceData.gBuffer[m_DeferredLights.GbufferDepthIndex];
                }

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingGbuffer, RenderPassEvent.BeforeRenderingDeferredLights);

                var gfxDeviceType = SystemInfo.graphicsDeviceType;
                // We double check for features in between GBuffer and Deferred passes just in case to know whether we need to reload the attachments
                if (InterruptFramebufferFetch(FramebufferFetchEvent.FetchGbufferInDeferred,RenderPassEvent.AfterRenderingGbuffer, RenderPassEvent.BeforeRenderingDeferredLights))
                    GBufferPass.ResetGlobalGBufferTextures(renderGraph, resourceData.gBuffer, resourceData.activeDepthTexture, resourceData, ref m_DeferredLights);

                m_DeferredPass.Render(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, resourceData.gBuffer);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingDeferredLights, RenderPassEvent.BeforeRenderingOpaques);

                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;
                m_RenderOpaqueForwardOnlyPass.Render(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, mainShadowsTexture, additionalShadowsTexture, uint.MaxValue);
            }
            else
            {
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingOpaques);

                var passCount = needsOccluderUpdate ? 2 : 1;
                for (int passIndex = 0; passIndex < passCount; ++passIndex)
                {
                    uint batchLayerMask = uint.MaxValue;
                    if (needsOccluderUpdate)
                    {
                        // first pass: test everything against previous frame final depth pyramid
                        // second pass: re-test culled against current frame intermediate depth pyramid
                        OcclusionTest occlusionTest = (passIndex) == 0 ? OcclusionTest.TestAll : OcclusionTest.TestCulled;
                        InstanceOcclusionTest(renderGraph, cameraData, occlusionTest);
                        batchLayerMask = occlusionTest.GetBatchLayerMask();
                    }

                    if (m_RenderingLayerProvidesRenderObjectPass)
                    {
                        m_RenderOpaqueForwardWithRenderingLayersPass.Render(
                            renderGraph,
                            frameData,
                            resourceData.activeColorTexture,
                            resourceData.renderingLayersTexture,
                            resourceData.activeDepthTexture,
                            resourceData.mainShadowsTexture,
                            resourceData.additionalShadowsTexture,
                            m_RenderingLayersMaskSize,
                            batchLayerMask);
                        SetRenderingLayersGlobalTextures(renderGraph);
                    }
                    else
                    {
                        m_RenderOpaqueForwardPass.Render(
                            renderGraph,
                            frameData,
                            resourceData.activeColorTexture,
                            resourceData.activeDepthTexture,
                            resourceData.mainShadowsTexture,
                            resourceData.additionalShadowsTexture,
                            batchLayerMask);
                    }

                    if (needsOccluderUpdate)
                    {
                        // first pass: make current frame intermediate depth pyramid
                        // second pass: make current frame final depth pyramid, set occlusion test results for later passes
                        UpdateInstanceOccluders(renderGraph, cameraData, resourceData.activeDepthTexture);
                        if (passIndex != 0)
                            InstanceOcclusionTest(renderGraph, cameraData, OcclusionTest.TestAll);
                    }
                }
                needsOccluderUpdate = false;
            }

            // Custom passes come before built-in passes to keep parity with non-RG code path where custom passes are added before renderer Setup.
            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingOpaques);

            if (requiresDepthCopyPass && m_CopyDepthMode != CopyDepthMode.AfterTransparents)
            {
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                m_CopyDepthPass.Render(renderGraph, frameData, cameraDepthTexture, resourceData.activeDepthTexture, true);
            }

            // Depends on the camera (copy) depth texture. Depth is reprojected to calculate motion vectors.
            if (renderPassInputs.requiresMotionVectors && m_CopyDepthMode != CopyDepthMode.AfterTransparents)
                m_MotionVectorPass.Render(renderGraph, frameData, resourceData.cameraDepthTexture, resourceData.motionVectorColor, resourceData.motionVectorDepth);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingSkybox);

            if (cameraData.camera.clearFlags == CameraClearFlags.Skybox && cameraData.renderType != CameraRenderType.Overlay)
            {
                if (RenderSettings.skybox != null || (cameraData.camera.TryGetComponent(out Skybox cameraSkybox) && cameraSkybox.material != null))
                    m_DrawSkyboxPass.Render(renderGraph, frameData, context, resourceData.activeColorTexture, resourceData.activeDepthTexture, requiresDepthCopyPass && m_CopyDepthMode != CopyDepthMode.AfterTransparents);
            }

            if (requiresColorCopyPass)
            {
                TextureHandle activeColor = resourceData.activeColorTexture;
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                TextureHandle cameraOpaqueTexture;
                m_CopyColorPass.Render(renderGraph, frameData, out cameraOpaqueTexture, in activeColor, downsamplingMethod);
                resourceData.cameraOpaqueTexture = cameraOpaqueTexture;
            }

#if UNITY_EDITOR
            {
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
                m_ProbeVolumeDebugPass.Render(renderGraph, frameData, cameraDepthTexture, cameraNormalsTexture);
            }
#endif

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingSkybox, RenderPassEvent.BeforeRenderingTransparents);

            // TODO RENDERGRAPH: bind _CameraOpaqueTexture, _CameraDepthTexture in transparent pass?
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                m_RenderTransparentForwardPass.m_ShouldTransparentsReceiveShadows = !m_TransparentSettingsPass.Setup();
                m_RenderTransparentForwardPass.Render(
                    renderGraph,
                    frameData,
                    resourceData.activeColorTexture,
                    resourceData.activeDepthTexture,
                    resourceData.mainShadowsTexture,
                    resourceData.additionalShadowsTexture);
            }

            // Custom passes come before built-in passes to keep parity with non-RG code path where custom passes are added before renderer Setup.

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingTransparents);

            if (requiresDepthCopyPass && m_CopyDepthMode == CopyDepthMode.AfterTransparents)
                m_CopyDepthPass.Render(renderGraph, frameData, resourceData.cameraDepthTexture, resourceData.activeDepthTexture, true);

            // TODO: Postprocess pass should be able configure its render pass inputs per camera per frame (settings) BEFORE building any of the graph
            // TODO: Alternatively we could always build the graph (a potential graph) and cull away unused passes if "record + cull" is fast enough.
            // TODO: Currently we just override "requiresMotionVectors" for TAA in GetRenderPassInputs()
            // Depends on camera (copy) depth texture
            if (renderPassInputs.requiresMotionVectors && m_CopyDepthMode == CopyDepthMode.AfterTransparents)
                m_MotionVectorPass.Render(renderGraph, frameData, resourceData.cameraDepthTexture, resourceData.motionVectorColor, resourceData.motionVectorDepth);

            if (context.HasInvokeOnRenderObjectCallbacks())
                m_OnRenderObjectCallbackPass.Render(renderGraph, resourceData.activeColorTexture, resourceData.activeDepthTexture);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            if (resourceData != null)
            {
                // SetupVFXCameraBuffer will interrogate VFXManager to automatically enable RequestAccess on RawColor and/or RawDepth. This must be done before SetupRawColorDepthHistory.
                // SetupVFXCameraBuffer will also provide the GetCurrentTexture from history manager to the VFXManager which can be sampled during the next VFX.Update for the following frame.
                SetupVFXCameraBuffer(cameraData);
            }
#endif

            RenderRawColorDepthHistory(renderGraph, cameraData, resourceData);

            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && outputToHDR)
            {
                TextureHandle overlayUI;
                m_DrawOffscreenUIPass.RenderOffscreen(renderGraph, frameData, k_DepthStencilFormat, out overlayUI);
                resourceData.overlayUITexture = overlayUI;
            }
        }

        private void OnAfterRendering(RenderGraph renderGraph)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            // if it's the last camera in the stack, setup the rendering debugger
            if (cameraData.resolveFinalTarget)
                SetupRenderGraphFinalPassDebug(renderGraph, frameData);

            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, GizmoSubset.PreImageEffects);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingPostProcessing);

            bool cameraTargetResolved = false;
            bool applyPostProcessing = ShouldApplyPostProcessing(cameraData.postProcessEnabled);
            // There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = postProcessingData.isEnabled && m_PostProcessPasses.isCreated;

            // When FXAA or scaling is active, we must perform an additional pass at the end of the frame for the following reasons:
            // 1. FXAA expects to be the last shader running on the image before it's presented to the screen. Since users are allowed
            //    to add additional render passes after post processing occurs, we can't run FXAA until all of those passes complete as well.
            //    The FinalPost pass is guaranteed to execute after user authored passes so FXAA is always run inside of it.
            // 2. UberPost can only handle upscaling with linear filtering. All other filtering methods require the FinalPost pass.
            // 3. TAA sharpening using standalone RCAS pass is required. (When upscaling is not enabled).
            bool applyFinalPostProcessing = anyPostProcessing && cameraData.resolveFinalTarget &&
                                            ((cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) ||
                                             ((cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (cameraData.upscalingFilter != ImageUpscalingFilter.Linear)) ||
                                             (cameraData.IsTemporalAAEnabled() && cameraData.taaSettings.contrastAdaptiveSharpening > 0.0f));
            bool hasCaptureActions = cameraData.captureActions != null && cameraData.resolveFinalTarget;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;

            bool resolvePostProcessingToCameraTarget = !hasCaptureActions && !hasPassesAfterPostProcessing && !applyFinalPostProcessing;
            bool needsColorEncoding = DebugHandler == null || !DebugHandler.HDRDebugViewIsActive(cameraData.resolveFinalTarget);
             
            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            // Allocate debug screen texture if the debug mode needs it.
            if (resolveToDebugScreen)
            {
                RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureColorDescriptorForDebugScreen(ref colorDesc, cameraData.pixelWidth, cameraData.pixelHeight);
                resourceData.debugScreenColor = CreateRenderGraphTexture(renderGraph, colorDesc, "_DebugScreenColor", false);

                RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, k_DepthStencilFormat, cameraData.pixelWidth, cameraData.pixelHeight);
                resourceData.debugScreenDepth = CreateRenderGraphTexture(renderGraph, depthDesc, "_DebugScreenDepth", false);
            }

            // If the debugHandler displays HDR debug views, it needs to redirect (final) post-process output to an intermediate color target (debugScreenTexture)
            // and it will write into the post-process intended output.
            TextureHandle debugHandlerColorTarget = resourceData.afterPostProcessColor;

            if (applyPostProcessing)
            {
                TextureHandle activeColor = resourceData.activeColorTexture;
                TextureHandle backbuffer = resourceData.backBufferColor;
                TextureHandle internalColorLut = resourceData.internalColorLut;
                TextureHandle overlayUITexture = resourceData.overlayUITexture;

                bool isTargetBackbuffer = (cameraData.resolveFinalTarget && !applyFinalPostProcessing && !hasPassesAfterPostProcessing);
                // if the postprocessing pass is trying to read and write to the same CameraColor target, we need to swap so it writes to a different target,
                // since reading a pass attachment is not possible. Normally this would be possible using temporary RenderGraph managed textures.
                // The reason why in this case we need to use "external" RTHandles is to preserve the results for camera stacking.
                // TODO RENDERGRAPH: Once all cameras will run in a single RenderGraph we can just use temporary RenderGraph textures as intermediate buffer.
                if (!isTargetBackbuffer)
                {
                    ImportResourceParams importColorParams = new ImportResourceParams();
                    importColorParams.clearOnFirstUse = true;
                    importColorParams.clearColor = Color.black;
                    importColorParams.discardOnLastUse = cameraData.resolveFinalTarget;  // check if last camera in the stack

                    // When STP is enabled, we must switch to the upscaled set of color handles before the next color handle value is queried. This ensures
                    // that the post processing output is rendered to a properly sized target. Any rendering performed beyond this point will also use the upscaled targets.
                    if (cameraData.IsSTPEnabled())
                        m_UseUpscaledColorHandle = true;
                   
                    resourceData.cameraColor = renderGraph.ImportTexture(nextRenderGraphCameraColorHandle, importColorParams); 
                }

                // Desired target for post-processing pass.
                var target = isTargetBackbuffer ? backbuffer : resourceData.cameraColor;

                // but we may actually render to an intermediate texture if debug views are enabled.
                // In that case, DebugHandler will eventually blit DebugScreenTexture into AfterPostProcessColor.
                if (resolveToDebugScreen && isTargetBackbuffer)
                {
                    debugHandlerColorTarget = target;
                    target = resourceData.debugScreenColor;
                }

                bool doSRGBEncoding = resolvePostProcessingToCameraTarget && needsColorEncoding;
                m_PostProcessPasses.postProcessPass.RenderPostProcessingRenderGraph(renderGraph, frameData, in activeColor, in internalColorLut, in overlayUITexture, in target, applyFinalPostProcessing, resolveToDebugScreen, doSRGBEncoding);

                // Handle any after-post rendering debugger overlays
                if (cameraData.resolveFinalTarget)
                    SetupAfterPostRenderGraphFinalPassDebug(renderGraph, frameData);

                if (isTargetBackbuffer)
                {
                    resourceData.activeColorID = UniversalResourceData.ActiveID.BackBuffer;
                    resourceData.activeDepthID = UniversalResourceData.ActiveID.BackBuffer;
                }
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingPostProcessing);

            if (applyFinalPostProcessing)
            {
                TextureHandle backbuffer = resourceData.backBufferColor;
                TextureHandle overlayUITexture = resourceData.overlayUITexture;

                // Desired target for post-processing pass.
                TextureHandle target = backbuffer;

                if (resolveToDebugScreen)
                {
                    debugHandlerColorTarget = target;
                    target = resourceData.debugScreenColor;
                }

                // make sure we are accessing the proper camera color in case it was replaced by injected passes
                var source = resourceData.cameraColor;
                m_PostProcessPasses.finalPostProcessPass.RenderFinalPassRenderGraph(renderGraph, frameData, in source, in overlayUITexture, in target, needsColorEncoding);

                resourceData.activeColorID = UniversalResourceData.ActiveID.BackBuffer;
                resourceData.activeDepthID = UniversalResourceData.ActiveID.BackBuffer;
            }

            if (cameraData.captureActions != null)
            {
                m_CapturePass.RecordRenderGraph(renderGraph, frameData);
            }

            cameraTargetResolved =
                // final PP always blit to camera target
                applyFinalPostProcessing ||
                // no final PP but we have PP stack. In that case it blit unless there are render pass after PP
                (applyPostProcessing && !hasPassesAfterPostProcessing && !hasCaptureActions);

            // TODO RENDERGRAPH: we need to discuss and decide if RenderPassEvent.AfterRendering injected passes should only be called after the last camera in the stack
            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRendering);

            if (!resourceData.isActiveTargetBackBuffer && cameraData.resolveFinalTarget && !cameraTargetResolved)
            {
                TextureHandle backbuffer = resourceData.backBufferColor;
                TextureHandle overlayUITexture = resourceData.overlayUITexture;
                TextureHandle target = backbuffer;

                if (resolveToDebugScreen)
                {
                    debugHandlerColorTarget = target;
                    target = resourceData.debugScreenColor;
                }

                // make sure we are accessing the proper camera color in case it was replaced by injected passes
                var source = resourceData.cameraColor;

                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderGraph, cameraData, !resolveToDebugScreen);

                m_FinalBlitPass.Render(renderGraph, cameraData, source, target, overlayUITexture);
                resourceData.activeColorID = UniversalResourceData.ActiveID.BackBuffer;
                resourceData.activeDepthID = UniversalResourceData.ActiveID.BackBuffer;
            }

            // We can explicitely render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && !outputToHDR)
            {
                TextureHandle depthBuffer = resourceData.backBufferDepth;
                TextureHandle target = resourceData.backBufferColor;

                if (resolveToDebugScreen)
                {
                    debugHandlerColorTarget = target;
                    target = resourceData.debugScreenColor;
                    depthBuffer = resourceData.debugScreenDepth;
                }

                m_DrawOverlayUIPass.RenderOverlay(renderGraph, frameData, in target, in depthBuffer);
            }

            if (debugHandler != null)
            {
                TextureHandle overlayUITexture = resourceData.overlayUITexture;
                TextureHandle debugScreenTexture = resourceData.debugScreenColor;

                debugHandler.Render(renderGraph, cameraData, debugScreenTexture, overlayUITexture, debugHandlerColorTarget);
            }

#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();

            if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera || (isGizmosEnabled && cameraData.resolveFinalTarget))
            {
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                m_FinalDepthCopyPass.MssaSamples = 0;
                m_FinalDepthCopyPass.Render(renderGraph, frameData, resourceData.activeDepthTexture, cameraDepthTexture, false, "Final Depth Copy");
            }
#endif
            if (cameraData.isSceneViewCamera)
                DrawRenderGraphWireOverlay(renderGraph, frameData, resourceData.backBufferColor);

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, resourceData.backBufferColor, resourceData.activeDepthTexture, GizmoSubset.PostImageEffects);
        }

        bool RequireDepthPrepass(UniversalCameraData cameraData, ref RenderPassInputSummary renderPassInputs)
        {
            bool applyPostProcessing = ShouldApplyPostProcessing(cameraData.postProcessEnabled);
            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = CameraHasPostProcessingWithDepth(cameraData);

            bool forcePrepass = (m_CopyDepthMode == CopyDepthMode.ForcePrepass);
            bool depthPrimingEnabled = IsDepthPrimingEnabled(cameraData);

#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
#else
            bool isGizmosEnabled = false;
#endif

            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || depthPrimingEnabled;
            bool requiresDepthPrepass = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && (!CanCopyDepth(cameraData) || forcePrepass);
            requiresDepthPrepass |= cameraData.isSceneViewCamera;
            requiresDepthPrepass |= isGizmosEnabled;
            requiresDepthPrepass |= cameraData.isPreviewCamera;
            requiresDepthPrepass |= renderPassInputs.requiresDepthPrepass;
            requiresDepthPrepass |= renderPassInputs.requiresNormalsTexture;

            // Current aim of depth prepass is to generate a copy of depth buffer, it is NOT to prime depth buffer and reduce overdraw on non-mobile platforms.
            // When deferred renderer is enabled, depth buffer is already accessible so depth prepass is not needed.
            // The only exception is for generating depth-normal textures: SSAO pass needs it and it must run before forward-only geometry.
            // DepthNormal prepass will render:
            // - forward-only geometry when deferred renderer is enabled
            // - all geometry when forward renderer is enabled
            if (requiresDepthPrepass && this.renderingModeActual == RenderingMode.Deferred && !renderPassInputs.requiresNormalsTexture)
                requiresDepthPrepass = false;

            requiresDepthPrepass |= depthPrimingEnabled;
            return requiresDepthPrepass;
        }

        bool RequireDepthTexture(UniversalCameraData cameraData, bool requiresDepthPrepass, ref RenderPassInputSummary renderPassInputs)
        {
            bool depthPrimingEnabled = IsDepthPrimingEnabled(cameraData);
            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || depthPrimingEnabled;
            bool cameraHasPostProcessingWithDepth = CameraHasPostProcessingWithDepth(cameraData);

            var createDepthTexture = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && !requiresDepthPrepass;
            createDepthTexture |= !cameraData.resolveFinalTarget;
            // Deferred renderer always need to access depth buffer.
            createDepthTexture |= (renderingModeActual == RenderingMode.Deferred && !useRenderPassEnabled);
            // Some render cases (e.g. Material previews) have shown we need to create a depth texture when we're forcing a prepass.
            createDepthTexture |= depthPrimingEnabled;
            // TODO: seems like with mrt depth is not taken from first target. Investigate if this is needed
            createDepthTexture |= m_RenderingLayerProvidesRenderObjectPass;

            createDepthTexture |= DebugHandlerRequireDepthPass(cameraData);

            return createDepthTexture;
        }

        internal void SetRenderingLayersGlobalTextures(RenderGraph renderGraph)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.renderingLayersTexture.IsValid() && renderingModeActual != RenderingMode.Deferred)
                RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID(m_RenderingLayersTextureName), resourceData.renderingLayersTexture, "Set Global Rendering Layers Texture");
        }

        void CreateCameraDepthCopyTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor, bool isDepthTexture)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var depthDescriptor = descriptor;
            depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA

            if (isDepthTexture)
            {
                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;
                depthDescriptor.depthBufferBits = k_DepthBufferBits;
            }
            else
            {
                depthDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
                depthDescriptor.depthStencilFormat = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = 0;
            }

            resourceData.cameraDepthTexture = CreateRenderGraphTexture(renderGraph, depthDescriptor, "_CameraDepthTexture", true);
        }

        void CreateMotionVectorTextures(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var colorDesc = descriptor;
            colorDesc.graphicsFormat = MotionVectorRenderPass.k_TargetFormat; colorDesc.depthBufferBits = (int)DepthBits.None;
            colorDesc.msaaSamples = 1;  // Disable MSAA, consider a pixel resolve for half left velocity and half right velocity --> no velocity, which is untrue.
            resourceData.motionVectorColor = CreateRenderGraphTexture(renderGraph, colorDesc, MotionVectorRenderPass.k_MotionVectorTextureName, true);

            var depthDescriptor = descriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.None;
            //TODO RENDERGRAPH: in some cornercases (f.e. rendering to targetTexture) this is needed. maybe this will be unnece
            depthDescriptor.depthBufferBits = depthDescriptor.depthBufferBits != 0 ? depthDescriptor.depthBufferBits : 32; depthDescriptor.msaaSamples = 1;
            resourceData.motionVectorDepth = CreateRenderGraphTexture(renderGraph, depthDescriptor, MotionVectorRenderPass.k_MotionVectorDepthTextureName, true);
        }

        void CreateCameraNormalsTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var normalDescriptor = descriptor;
            normalDescriptor.depthBufferBits = 0;
            // Never have MSAA on this depth texture. When doing MSAA depth priming this is the texture that is resolved to and used for post-processing.
            normalDescriptor.msaaSamples = useDepthPriming ? descriptor.msaaSamples : 1;// Depth-Only passes don't use MSAA, unless depth priming is enabled
            // Find compatible render-target format for storing normals.
            // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
            // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
            var normalsName = this.renderingModeActual != RenderingMode.Deferred ? "_CameraNormalsTexture" : DeferredLights.k_GBufferNames[m_DeferredLights.GBufferNormalSmoothnessIndex];
            normalDescriptor.graphicsFormat = this.renderingModeActual != RenderingMode.Deferred ? DepthNormalOnlyPass.GetGraphicsFormat() : m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferNormalSmoothnessIndex);
            resourceData.cameraNormalsTexture = CreateRenderGraphTexture(renderGraph, normalDescriptor, normalsName, true);
        }

        void CreateRenderingLayersTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            if (m_RequiresRenderingLayer)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                m_RenderingLayersTextureName = "_CameraRenderingLayersTexture";

                // TODO RENDERGRAPH: deferred optimization
                if (renderingModeActual == RenderingMode.Deferred && m_DeferredLights.UseRenderingLayers)
                    m_RenderingLayersTextureName = DeferredLights.k_GBufferNames[m_DeferredLights.GBufferRenderingLayers];

                RenderTextureDescriptor renderingLayersDescriptor = descriptor;
                renderingLayersDescriptor.depthBufferBits = 0;
                if (!m_RenderingLayerProvidesRenderObjectPass)
                    renderingLayersDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA

                // Find compatible render-target format for storing normals.
                // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
                // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
                if (renderingModeActual == RenderingMode.Deferred && m_RequiresRenderingLayer)
                    renderingLayersDescriptor.graphicsFormat = m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferRenderingLayers); // the one used by the gbuffer.
                else
                    renderingLayersDescriptor.graphicsFormat = RenderingLayerUtils.GetFormat(m_RenderingLayersMaskSize);

                resourceData.renderingLayersTexture = CreateRenderGraphTexture(renderGraph, renderingLayersDescriptor, m_RenderingLayersTextureName, true);
            }
        }

        void CreateAfterPostProcessTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var desc = PostProcessPass.GetCompatibleDescriptor(descriptor, descriptor.width, descriptor.height, descriptor.graphicsFormat, DepthBits.None);
            resourceData.afterPostProcessColor = CreateRenderGraphTexture(renderGraph, desc, "_AfterPostProcessTexture", true);
        }

        void DepthNormalPrepassRender(RenderGraph renderGraph, RenderPassInputSummary renderPassInputs, TextureHandle depthTarget, uint batchLayerMask, bool postSetGlobalTextures)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (m_RenderingLayerProvidesByDepthNormalPass)
            {
                m_DepthNormalPrepass.enableRenderingLayers = true;
                m_DepthNormalPrepass.renderingLayersMaskSize = m_RenderingLayersMaskSize;
            }
            else
            {
                m_DepthNormalPrepass.enableRenderingLayers = false;
            }

            if (renderingModeActual == RenderingMode.Deferred)
            {
                // Only render forward-only geometry, as standard geometry will be rendered as normal into the gbuffer.
                if (RenderPassEvent.AfterRenderingGbuffer <= renderPassInputs.requiresDepthNormalAtEvent &&
                    renderPassInputs.requiresDepthNormalAtEvent <= RenderPassEvent.BeforeRenderingOpaques)
                    m_DepthNormalPrepass.shaderTagIds = k_DepthNormalsOnly;

                // TODO RENDERGRAPH: commented this out since would be equivalent to the current behaviour? Double check
                //if (!m_RenderingLayerProvidesByDepthNormalPass)
                // renderingLayersTexture = resourceData.gbuffer[m_DeferredLights.GBufferRenderingLayers]; // GBUffer texture here
            }

            TextureHandle normalsTexture = resourceData.cameraNormalsTexture;
            TextureHandle renderingLayersTexture = resourceData.renderingLayersTexture;
            m_DepthNormalPrepass.Render(renderGraph, frameData, normalsTexture, depthTarget, renderingLayersTexture, batchLayerMask, postSetGlobalTextures);

            if (m_RequiresRenderingLayer)
                SetRenderingLayersGlobalTextures(renderGraph);
        }
    }

    static class RenderGraphUtils
    {
        static private ProfilingSampler s_SetGlobalTextureProfilingSampler = new ProfilingSampler("Set Global Texture");

        internal const int GBufferSize = 7;
        internal const int DBufferSize = 3;
        internal const int LightTextureSize = 4;

        internal static void UseDBufferIfValid(IRasterRenderGraphBuilder builder, UniversalResourceData resourceData)
        {
            TextureHandle[] dbufferHandles = resourceData.dBuffer;
            for (int i = 0; i < DBufferSize; ++i)
            {
                TextureHandle dbuffer = dbufferHandles[i];
                if (dbuffer.IsValid())
                    builder.UseTexture(dbuffer);
            }
        }

        private class PassData
        {
            internal TextureHandle texture;
            internal int nameID;
        }

        public static void SetGlobalTexture(RenderGraph graph, int nameId, TextureHandle handle, string passName = "Set Global Texture",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, s_SetGlobalTextureProfilingSampler, file, line))
            {
                passData.nameID = nameId;
                passData.texture = handle;
                builder.UseTexture(handle, AccessFlags.Read);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetGlobalTextureAfterPass(handle, nameId);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                });
            }
        }
    }
    class ClearTargetsPass
    {
        static private ProfilingSampler s_ClearProfilingSampler = new ProfilingSampler("Clear Targets");

        private class PassData
        {
            internal TextureHandle color;
            internal TextureHandle depth;

            internal RTClearFlags clearFlags;
            internal Color clearColor;
        }

        internal static void Render(RenderGraph graph, TextureHandle colorHandle, TextureHandle depthHandle,
            UniversalCameraData cameraData)
        {
            RTClearFlags clearFlags = RTClearFlags.None;

            if (cameraData.renderType == CameraRenderType.Base)
                clearFlags = RTClearFlags.All;
            else if (cameraData.clearDepth)
                clearFlags = RTClearFlags.Depth;

            if (clearFlags != RTClearFlags.None)
                Render(graph, colorHandle, depthHandle, clearFlags, cameraData.backgroundColor);
        }

        internal static void Render(RenderGraph graph, TextureHandle colorHandle, TextureHandle depthHandle, RTClearFlags clearFlags, Color clearColor)
        {
            using (var builder = graph.AddRasterRenderPass<PassData>("Clear Targets Pass", out var passData, s_ClearProfilingSampler))
            {
                if (colorHandle.IsValid())
                {
                    passData.color = colorHandle;
                    builder.SetRenderAttachment(colorHandle, 0, AccessFlags.Write);
                }

                if (depthHandle.IsValid())
                {
                    passData.depth = depthHandle;
                    builder.SetRenderAttachmentDepth(depthHandle, AccessFlags.Write);
                }

                passData.clearFlags = clearFlags;
                passData.clearColor = clearColor;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(data.clearFlags, data.clearColor, 1, 0);
                });
            }
        }
    }

}
