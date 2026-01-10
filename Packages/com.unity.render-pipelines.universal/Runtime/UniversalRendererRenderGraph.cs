using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// [Obsolete]
        /// </summary>
        [System.Obsolete("AfterPostProcessColor has never been implemented and is obsolete")]
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
        private static RTHandle[] s_RenderGraphCameraColorHandles = new RTHandle[]
        {
            null, null
        };

        private static RTHandle s_RenderGraphCameraDepthHandle;
        private static int s_CurrentColorHandle = 0;
        private static RTHandle s_RenderGraphDebugTextureHandle;
        private static RTHandle s_OffscreenUIColorHandle;

        private RTHandle currentRenderGraphCameraColorHandle
        {
            get
            {
                Debug.Assert(s_CurrentColorHandle >= 0,
                            "currentRenderGraphCameraColorHandle should not be accessed in single camera mode.");

                if (s_CurrentColorHandle < 0)
                    return null;

                return s_RenderGraphCameraColorHandles[s_CurrentColorHandle];
            }
        }

        // get the next m_RenderGraphCameraColorHandles and make it the new current for future accesses
        private RTHandle nextRenderGraphCameraColorHandle
        {
            get
            {
                Debug.Assert(s_CurrentColorHandle >= 0,
                            "nextRenderGraphCameraColorHandle should not be accessed in single camera mode.");

                if (s_CurrentColorHandle < 0)
                    return null;

                s_CurrentColorHandle = (s_CurrentColorHandle + 1) % 2;
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

        // Post-Processing
        ColorGradingLutPass m_ColorGradingLutPassRenderGraph;
        PostProcess m_PostProcess;

        private void CleanupRenderGraphResources()
        {
            s_RenderGraphCameraColorHandles[0]?.Release();
            s_RenderGraphCameraColorHandles[1]?.Release();
            s_RenderGraphCameraDepthHandle?.Release();

            s_RenderGraphDebugTextureHandle?.Release();
            s_OffscreenUIColorHandle?.Release();
        }

        /// <summary>
        /// Utility method to convert RenderTextureDescriptor to TextureHandle and create a RenderGraph texture.
        /// The use of RenderTextureDescriptor is obsolete with RenderGraph, use TextureDesc instead.
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
            TextureDesc rgDesc;
            GetTextureDesc(in desc, out rgDesc);

            rgDesc.clearBuffer = clear;
            rgDesc.name = name;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;

            return renderGraph.CreateTexture(rgDesc);
        }

        internal static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, in RenderTextureDescriptor desc, string name, bool clear, Color color,
            FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp, bool discardOnLastUse = false)
        {
            TextureDesc rgDesc;
            GetTextureDesc(in desc, out rgDesc);

            rgDesc.clearBuffer = clear;
            rgDesc.clearColor = color;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.name = name;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;
            rgDesc.discardBuffer = discardOnLastUse;

            return renderGraph.CreateTexture(rgDesc);
        }

        internal static void GetTextureDesc(in RenderTextureDescriptor desc, out TextureDesc rgDesc)
        {
            rgDesc = new TextureDesc(desc.width, desc.height);
            rgDesc.dimension = desc.dimension;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.format = (desc.depthStencilFormat != GraphicsFormat.None) ? desc.depthStencilFormat : desc.graphicsFormat;
            rgDesc.isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None && desc.depthStencilFormat != GraphicsFormat.None;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.enableRandomWrite = desc.enableRandomWrite;
            rgDesc.enableShadingRate = desc.enableShadingRate;
            rgDesc.useDynamicScale = desc.useDynamicScale;
            rgDesc.useDynamicScaleExplicit = desc.useDynamicScaleExplicit;
            rgDesc.vrUsage = desc.vrUsage;
        }

        internal static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, in TextureDesc desc, string name, bool clear, Color clearColor,
                FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp, bool discardOnLastUse = false)
        {
            TextureDesc outDesc = desc;
            outDesc.name = name;
            outDesc.clearBuffer = clear;
            outDesc.clearColor = clearColor;
            outDesc.filterMode = filterMode;
            outDesc.wrapMode = wrapMode;
            outDesc.discardBuffer = discardOnLastUse;

            return renderGraph.CreateTexture(outDesc);
        }

        bool RequiresIntermediateAttachments(UniversalCameraData cameraData, in RenderPassInputSummary renderPassInputs, bool requireCopyFromDepth, bool applyPostProcessing)
        {
            var requireColorTexture = HasActiveRenderFeatures(rendererFeatures) && m_IntermediateTextureMode == IntermediateTextureMode.Always;
            requireColorTexture |= HasPassesRequiringIntermediateTexture(activeRenderPassQueue);
            requireColorTexture |= RequiresIntermediateColorTexture(cameraData, in renderPassInputs, usesDeferredLighting, applyPostProcessing);

            bool requestedDepthHistory = (cameraData.historyManager == null) ? false : cameraData.historyManager.IsAccessRequested<RawDepthHistory>();

            // Intermediate texture has different yflip state than backbuffer. In case we use intermediate texture, we must use both color and depth together.
            return requireColorTexture || requireCopyFromDepth || requestedDepthHistory;
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
        const string _SingleCameraTargetAttachmentName = "_CameraTargetAttachment";
        const string _CameraDepthAttachmentName = "_CameraDepthAttachment";


        void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, bool isCameraTargetOffscreenDepth, bool requireIntermediateAttachments, bool depthTextureIsDepthFormat)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var clearCameraParams = GetClearCameraParams(cameraData);

            // Setup backbuffer RTHandles before importing them to RG
            SetupTargetHandles(cameraData);

            // Gather render pass history requests and update history textures.
            UpdateCameraHistory(cameraData);

            // Import backbuffers to Render Graph
            ImportBackBuffers(renderGraph, cameraData, clearCameraParams.clearValue, isCameraTargetOffscreenDepth);

            TextureDesc cameraDescriptor;
            GetTextureDesc(in cameraData.cameraTargetDescriptor, out cameraDescriptor);
            cameraDescriptor.useMipMap = false;
            cameraDescriptor.autoGenerateMips = false;
            cameraDescriptor.mipMapBias = 0;
            cameraDescriptor.anisoLevel = 1;

            if (requireIntermediateAttachments)
            {
                cameraDescriptor.format = cameraData.cameraTargetDescriptor.graphicsFormat;

                if (!isCameraTargetOffscreenDepth)
                    CreateIntermediateCameraColorAttachment(renderGraph, cameraData, in cameraDescriptor, clearCameraParams.mustClearColor, clearCameraParams.clearValue);

                cameraDescriptor.format = cameraData.cameraTargetDescriptor.depthStencilFormat;

                CreateIntermediateCameraDepthAttachment(renderGraph, cameraData, in cameraDescriptor, clearCameraParams.mustClearDepth, clearCameraParams.clearValue, depthTextureIsDepthFormat);
            }
            else
            {
                resourceData.SwitchActiveTexturesToBackbuffer();
            }

            CreateCameraDepthCopyTexture(renderGraph, cameraDescriptor, depthTextureIsDepthFormat, clearCameraParams.clearValue);

            CreateCameraNormalsTexture(renderGraph, cameraDescriptor);

            CreateMotionVectorTextures(renderGraph, cameraDescriptor);

            CreateRenderingLayersTexture(renderGraph, cameraDescriptor);

            if (cameraData.isHDROutputActive && cameraData.rendersOverlayUI)
                CreateOffscreenUITexture(renderGraph, cameraDescriptor);
        }

        private readonly struct ClearCameraParams
        {
            internal readonly bool mustClearColor;
            internal readonly bool mustClearDepth;
            internal readonly Color clearValue;

            internal ClearCameraParams(bool clearColor, bool clearDepth, Color clearVal)
            {
                mustClearColor = clearColor;
                mustClearDepth = clearDepth;
                clearValue = clearVal;
            }
        }

        ClearCameraParams GetClearCameraParams(UniversalCameraData cameraData)
        {
            bool clearColor = cameraData.renderType == CameraRenderType.Base;
            bool clearDepth = cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth;

            // if the camera background type is "uninitialized" clear using a yellow color, so users can clearly understand the underlying behaviour
            // only exception being if we are rendering to an external texture
            Color clearVal = (cameraData.camera.clearFlags == CameraClearFlags.Nothing && cameraData.targetTexture == null) ? Color.yellow : cameraData.backgroundColor;

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
                clearVal.a = 0;
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
                    DebugHandler.TryGetScreenClearColor(ref clearVal);
                }
            }

            return new ClearCameraParams(clearColor, clearDepth, clearVal);
        }

        void SetupTargetHandles(UniversalCameraData cameraData)
        {
            RenderTargetIdentifier targetColorId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.Depth;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                targetColorId = cameraData.xr.renderTarget;
                targetDepthId = cameraData.xr.renderTarget;
            }
#endif

            if (m_TargetColorHandle == null)
            {
                m_TargetColorHandle = RTHandles.Alloc(targetColorId, "Backbuffer color");
            }
            else if (m_TargetColorHandle.nameID != targetColorId)
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
        }

        void SetupRenderingLayers(int msaaSamples)
        {
            // Gather render pass require rendering layers event and mask size
            m_RequiresRenderingLayer = RenderingLayerUtils.RequireRenderingLayers(this, rendererFeatures, msaaSamples,
                out m_RenderingLayersEvent, out m_RenderingLayersMaskSize);

            m_RenderingLayerProvidesRenderObjectPass = m_RequiresRenderingLayer && m_RenderingLayersEvent == RenderingLayerUtils.Event.Opaque;
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
            if (usesDeferredLighting)
            {
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
                            m_HistoryRawColorCopyPass.RenderToExistingTexture(renderGraph, frameData, colorHistoryTarget, resourceData.cameraColor, Downsampling.None);
                        }
                    }
                }

                if (history.IsAccessRequested<RawDepthHistory>() && resourceData.cameraDepth.IsValid() && CanCopyDepth(cameraData))
                {
                    var depthHistory = history.GetHistoryForWrite<RawDepthHistory>();
                    if (depthHistory != null)
                    {
                        var tempColorDepthDesc = cameraData.cameraTargetDescriptor;

                        //On GLES we don't support sampling the MSAA targets, so if auto depth resolve is not available, the only thing that works is rendering to a color target.
                        //This has been the behavior from at least 6.0. However, it results in the format mostly being color on the different graphics APIs, even when
                        //it could be a depth format if MSAA sampling for depht is allowed.
                        if (RenderingUtils.MultisampleDepthResolveSupported())
                        {
                            tempColorDepthDesc.graphicsFormat = GraphicsFormat.None;
                        }
                        else
                        {
                            tempColorDepthDesc.graphicsFormat = GraphicsFormat.R32_SFloat;
                            tempColorDepthDesc.depthStencilFormat = GraphicsFormat.None;
                        }

                        depthHistory.Update(ref tempColorDepthDesc, xrMultipassEnabled);

                        if (depthHistory.GetCurrentTexture(multipassId) != null)
                        {
                            var depthHistoryTarget = renderGraph.ImportTexture(depthHistory.GetCurrentTexture(multipassId));

                            // See pass create in UniversalRenderer() for execution order.
                            m_HistoryRawDepthCopyPass.Render(renderGraph, frameData, depthHistoryTarget, resourceData.cameraDepth, false);
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

        static void ApplyConstraints(bool onTileValidation, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalPostProcessingData postProcessingData, List<ScriptableRenderPass> activeRenderPassQueue, ref RenderingMode renderingMode, ref DepthPrimingMode depthPrimingMode)
        {
            if (!onTileValidation)
                return;

            cameraData.requiresOpaqueTexture = false;
            cameraData.requiresDepthTexture = false;

            cameraData.postProcessEnabled = false;
            cameraData.stackAnyPostProcessingEnabled = false;
            postProcessingData.isEnabled = false;

            cameraData.useGPUOcclusionCulling = false;
            cameraData.isHdrEnabled = false;

            if (!PlatformAutoDetect.isXRMobile)
            {
                cameraData.renderScale = 1.0f;
                cameraData.imageScalingMode = ImageScalingMode.None;
            }

            if (renderingData.renderingMode == RenderingMode.DeferredPlus)
                renderingData.renderingMode = RenderingMode.ForwardPlus;

            if (renderingMode == RenderingMode.DeferredPlus)
                renderingMode = RenderingMode.ForwardPlus;

            if (renderingData.renderingMode == RenderingMode.Deferred)
                renderingData.renderingMode = RenderingMode.Forward;

            if (renderingMode == RenderingMode.Deferred)
                renderingMode = RenderingMode.Forward;

            if (cameraData.baseCamera != null && cameraData.baseCamera != cameraData.camera)
                throw new ArgumentException("The active URP Renderer has 'On Tile Validation' on. This currently does not allow Camera Stacking usage. Check your scene and remove all overlay Cameras.");

            if (activeRenderPassQueue.Count > 0)
                throw new ArgumentException("The active URP Renderer has 'On Tile Validation' on. This currently does not allow any ScriptableRenderFeature enabled, and it does not allow enqueuing any ScriptableRenderPass. Check your Renderer asset and disable all Renderer Features. Also, ensure that no C# script enqueus any passes on the renderer.");
        }

        //Catches mistakes by Unity devs with regards to clearing the settings
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        static void ValidateCorrectnessOfConstraints(bool onTileValidation, in RenderPassInputSummary renderPassInputs, bool requireIntermediateTextures)
        {
            if (!onTileValidation)
                return;

            if (renderPassInputs is { requiresColorTexture: false, requiresDepthTexture: false, requiresMotionVectors: false, requiresNormalsTexture: false} && !requireIntermediateTextures)
                return;

            throw new ArgumentException("On Tile Validation is on but certain features still added requirements that would validate this.");
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            ApplyConstraints(onTileValidation, renderingData, cameraData, postProcessingData,activeRenderPassQueue, ref m_RenderingMode, ref m_DepthPrimingMode);

            MotionVectorRenderPass.SetRenderGraphMotionVectorGlobalMatrices(renderGraph, cameraData);

            SetupRenderGraphLights(renderGraph, renderingData, cameraData, lightData);

            SetupRenderingLayers(cameraData.cameraTargetDescriptor.msaaSamples);

            bool isCameraTargetOffscreenDepth = cameraData.camera.targetTexture != null && cameraData.camera.targetTexture.format == RenderTextureFormat.Depth;
            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcess != null;

            //First get the input requirements for the the ScriptableRenderPasses. These are all user passes plus potentially some that URP adds.
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(activeRenderPassQueue);
            //Then add all the requirements of internal features that are not implemented as ScriptableRenderPass's. After this call we have a complete view on the render pass input requirements for the entire frame.
            AddRequirementsOfInternalFeatures(ref renderPassInputs, cameraData, applyPostProcessing, m_RenderingLayerProvidesByDepthNormalPass, m_MotionVectorPass, m_CopyDepthMode);

            bool requirePrepassForTextures = RequirePrepassForTextures(cameraData, in renderPassInputs);

            useDepthPriming = IsDepthPrimingEnabledRenderGraph(cameraData, in renderPassInputs, m_DepthPrimingMode, requirePrepassForTextures, usesDeferredLighting);

            bool requirePrepass = requirePrepassForTextures || useDepthPriming;

            // Only use a depth format when we do a prepass directly the cameraDepthTexture. If we do depth priming (ie, prepass to the activeCameraDepth), we don't do a prepass to the texture. Instead, we do a copy from the primed attachment.
            bool prepassToCameraDepthTexture = requirePrepassForTextures && !usesDeferredLighting && !useDepthPriming;
            bool depthTextureIsDepthFormat = prepassToCameraDepthTexture;
            bool requireCopyFromDepth = renderPassInputs.requiresDepthTexture && !prepassToCameraDepthTexture;

            // We configure this for the first camera of the stack and overlay camera will reuse create color/depth var
            // to pick the correct target, as if there is an intermediate texture, overlay cam should use them
            if (cameraData.renderType == CameraRenderType.Base)
                s_RequiresIntermediateAttachments = RequiresIntermediateAttachments(cameraData, in renderPassInputs, requireCopyFromDepth, applyPostProcessing);

            ValidateCorrectnessOfConstraints(onTileValidation, renderPassInputs, s_RequiresIntermediateAttachments);

            CreateRenderGraphCameraRenderTargets(renderGraph, isCameraTargetOffscreenDepth, s_RequiresIntermediateAttachments, depthTextureIsDepthFormat);

            if (DebugHandler != null)
                DebugHandler.Setup(renderGraph, cameraData.isPreviewCamera);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRendering);

            SetupRenderGraphCameraProperties(renderGraph, resourceData.activeColorTexture.IsValid() ? resourceData.activeColorTexture : resourceData.activeDepthTexture);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            ProcessVFXCameraCommand(renderGraph);
#endif

            if (isCameraTargetOffscreenDepth)
            {
                OnOffscreenDepthTextureRendering(renderGraph, context, resourceData, cameraData);
                return;
            }

            OnBeforeRendering(renderGraph);

            BeginRenderGraphXRRendering(renderGraph);

            OnMainRendering(renderGraph, context, in renderPassInputs, requirePrepass);

            OnAfterRendering(renderGraph, applyPostProcessing);

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
            if (usesDeferredLighting)
                m_DeferredPass.OnCameraCleanup(cmd);

            m_CopyDepthPass.OnCameraCleanup(cmd);
            m_DepthNormalPrepass.OnCameraCleanup(cmd);
        }

        private bool m_IssuedGPUOcclusionUnsupportedMsg = false;

        /// <summary>
        /// Used to determine if this renderer supports the use of GPU occlusion culling.
        /// </summary>
        public override bool supportsGPUOcclusion
        {
            get
            {
                // UUM-82677: GRD GPU Occlusion Culling on Vulkan breaks rendering on some mobile GPUs
                //
                // We currently disable gpu occlusion culling when running on Qualcomm GPUs due to suspected driver issues.
                // Once the issue is resolved, this logic should be removed.
                const int kQualcommVendorId = 0x5143;
                bool isGpuSupported = SystemInfo.graphicsDeviceVendorID != kQualcommVendorId;

                if (!isGpuSupported && !m_IssuedGPUOcclusionUnsupportedMsg)
                {
                    Debug.LogWarning("The GPU Occlusion Culling feature is currently unavailable on this device due to suspected driver issues.");
                    m_IssuedGPUOcclusionUnsupportedMsg = true;
                }

                return isGpuSupported;
            }
        }

        private static bool s_RequiresIntermediateAttachments;

        private void OnOffscreenDepthTextureRendering(RenderGraph renderGraph, ScriptableRenderContext context, UniversalResourceData resourceData, UniversalCameraData cameraData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            if (m_MainLightShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData))
            {
                resourceData.mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderGraph, frameData);
            }

            if (m_AdditionalLightsShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData))
            {
                resourceData.additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderGraph, frameData);
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingShadows, RenderPassEvent.BeforeRenderingOpaques);
            m_RenderOpaqueForwardPass.Render(renderGraph, frameData, TextureHandle.nullHandle, resourceData.backBufferDepth, TextureHandle.nullHandle, TextureHandle.nullHandle, uint.MaxValue);
            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingOpaques, RenderPassEvent.BeforeRenderingTransparents);
#if ENABLE_ADAPTIVE_PERFORMANCE
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
                SetupRenderGraphCameraProperties(renderGraph, resourceData.activeColorTexture.IsValid()  ? resourceData.activeColorTexture : resourceData.activeDepthTexture);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingShadows);

            bool requiredColorGradingLutPass = cameraData.postProcessEnabled && m_PostProcess != null;
            if (requiredColorGradingLutPass)
            {
                m_ColorGradingLutPassRenderGraph.RecordRenderGraph(renderGraph, frameData);
            }
        }

        private enum OccluderPass
        {
            None,
            DepthPrepass,
            ForwardOpaque,
            GBuffer
        }

        private void UpdateInstanceOccluders(RenderGraph renderGraph, UniversalCameraData cameraData, TextureHandle depthTexture)
        {
            int scaledWidth = (int)(cameraData.pixelWidth * cameraData.renderScale);
            int scaledHeight = (int)(cameraData.pixelHeight * cameraData.renderScale);
            bool isSinglePassXR = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;
            var occluderParams = new OccluderParameters(cameraData.camera.GetEntityId())
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
            var settings = new OcclusionCullingSettings(cameraData.camera.GetEntityId(), occlusionTest)
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

        // Records the depth copy pass along with the specified custom passes in a way that properly handles depth read dependencies
        // This function will also trigger motion vector rendering if required by the current frame since its availability is intended to match depth's.
        private void RecordCustomPassesWithDepthCopyAndMotion(RenderGraph renderGraph, UniversalResourceData resourceData, RenderPassEvent earliestDepthReadEvent, RenderPassEvent currentEvent, bool renderMotionVectors)
        {
            // Custom passes typically come before built-in passes but there's an exception for passes that require depth.
            // In cases where custom passes passes may depend on depth, we split the event range and execute the depth copy as late as possible while still ensuring valid depth reads.

            CalculateSplitEventRange(currentEvent, earliestDepthReadEvent, out var startEvent, out var splitEvent, out var endEvent);

            RecordCustomRenderGraphPassesInEventRange(renderGraph, startEvent, splitEvent);

            ExecuteScheduledDepthCopyWithMotion(renderGraph, resourceData, renderMotionVectors);

            RecordCustomRenderGraphPassesInEventRange(renderGraph, splitEvent, endEvent);
        }

        // Returns true if the current render pass inputs allow us to perform a partial depth normals prepass
        //
        // During a partial prepass, we only render forward opaque objects that aren't rendered into the gbuffer.
        // This produces a set of partial depth & normal buffers that must be completed by the gbuffer pass later in the frame.
        // This allows us to produce complete depth & normals data before lighting takes place, but it isn't valid when custom
        // passes require this data before the gbuffer pass finishes.
        private static bool AllowPartialDepthNormalsPrepass(bool isDeferred, RenderPassInputSummary renderPassInputSummary, bool useDepthPriming)
        {
            bool requiresDepthAfterGbuffer = RenderPassEvent.AfterRenderingGbuffer <= renderPassInputSummary.requiresDepthTextureEarliestEvent;
            bool requiresNormalAfterGbuffer = RenderPassEvent.AfterRenderingGbuffer <= renderPassInputSummary.requiresNormalTextureEarliestEvent;

            return isDeferred && requiresDepthAfterGbuffer && requiresNormalAfterGbuffer && !useDepthPriming;
        }

        // Enumeration of possible positions within the frame where the depth copy can occur
        private enum DepthCopySchedule
        {
            // In some cases, we can render depth directly to the depth texture during the depth prepass
            DuringPrepass,

            AfterPrepass,
            AfterGBuffer,
            AfterOpaques,
            AfterSkybox,
            AfterTransparents,

            // None is always the last value so we can easily check if the depth has already been copied in the current frame via comparison
            None
        }

        /// <summary>
        /// Calculates where the depth copy pass should be scheduled within the frame.
        /// This function is only intended to be called in cases where we've determined that an explicit depth copy pass is required.
        /// The copy will be scheduled as late as possible in the frame while still respecting user selections and custom pass requirements.
        /// </summary>
        /// <param name="earliestDepthReadEvent">The earliest render pass event in the frame that reads from the depth texture</param>
        /// <param name="hasFullPrepass">True if we've determined that the current frame will include a full prepass</param>
        /// <returns>The position within the frame where the depth copy pass should be executed</returns>
        static private DepthCopySchedule CalculateDepthCopySchedule(RenderPassEvent earliestDepthReadEvent, bool hasFullPrepass)
        {
            DepthCopySchedule schedule;

            if(earliestDepthReadEvent < RenderPassEvent.AfterRenderingGbuffer)
            {
                // If we have a full prepass, we can copy depth immediately after since a full prepass guarantees complete depth data.
                schedule = DepthCopySchedule.AfterPrepass;

                // Make sure we aren't scheduling the depth copy later than the event reading depth.
                // The only way this could happen is if we executed a partial prepass in a case where we should have done a full prepass.
                Debug.Assert(hasFullPrepass, "Doing a partial prepass when the full depth data is needed before 'AfterRenderingGBuffer'");
            }
            else if ((earliestDepthReadEvent < RenderPassEvent.AfterRenderingOpaques))
            {
                // If we have a partial prepass (or no prepass), we must finish rendering the gbuffer before complete depth data is available.
                schedule = DepthCopySchedule.AfterGBuffer;
            }
            else if (earliestDepthReadEvent < RenderPassEvent.AfterRenderingSkybox)
            {
                schedule = DepthCopySchedule.AfterOpaques;
            }
            else if ((earliestDepthReadEvent < RenderPassEvent.AfterRenderingTransparents))
            {
                schedule = DepthCopySchedule.AfterSkybox;
            }
            else
            {
                schedule = DepthCopySchedule.AfterTransparents;
            }

            return schedule;
        }

        private DepthCopySchedule CalculateDepthCopySchedules(UniversalCameraData cameraData, in RenderPassInputSummary renderPassInputs, bool isDeferred, bool requiresDepthPrepass, bool hasFullPrepass)
        {
            // Assume the depth texture is unused and no copy is needed until we determine otherwise
            DepthCopySchedule depth = DepthCopySchedule.None;

            // If the depth texture is read during the frame, determine when the copy should occur
            if (renderPassInputs.requiresDepthTexture)
            {
                //The prepass will render directly to the depthTexture when not using depth priming. Therefore we don't need a copy in that case.
                bool depthTextureRequiresCopy = isDeferred || (!requiresDepthPrepass || useDepthPriming);

                depth = depthTextureRequiresCopy ? CalculateDepthCopySchedule(renderPassInputs.requiresDepthTextureEarliestEvent, hasFullPrepass)
                                                 : DepthCopySchedule.DuringPrepass;
            }
            return depth;
        }

        private void CopyDepthToDepthTexture(RenderGraph renderGraph, UniversalResourceData resourceData)
        {
            m_CopyDepthPass.Render(renderGraph, frameData, resourceData.cameraDepthTexture, resourceData.activeDepthTexture, true);
        }

        private void RenderMotionVectors(RenderGraph renderGraph, UniversalResourceData resourceData)
        {
            m_MotionVectorPass.Render(renderGraph, frameData, resourceData.cameraDepthTexture, resourceData.motionVectorColor, resourceData.motionVectorDepth);
        }

        private void ExecuteScheduledDepthCopyWithMotion(RenderGraph renderGraph, UniversalResourceData resourceData, bool renderMotionVectors)
        {
            CopyDepthToDepthTexture(renderGraph, resourceData);

            if (renderMotionVectors)
                RenderMotionVectors(renderGraph, resourceData);
        }

        private void OnMainRendering(RenderGraph renderGraph, ScriptableRenderContext context, in RenderPassInputSummary renderPassInputs, bool requiresPrepass)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            if (renderingData.stencilLodCrossFadeEnabled)
                m_StencilCrossFadeRenderPass.Render(renderGraph, context, resourceData.activeDepthTexture);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingPrePasses);

            bool isDepthOnlyPrepass = requiresPrepass && !renderPassInputs.requiresNormalsTexture;
            bool isDepthNormalPrepass = requiresPrepass && renderPassInputs.requiresNormalsTexture;

            // The depth prepass is considered "full" (renders the entire scene, not a partial subset), when we either:
            // - Have a depth only prepass (URP always renders the full scene in depth only mode)
            // - Have a depth normals prepass that does not allow the partial prepass optimization
            bool hasFullPrepass = isDepthOnlyPrepass || (isDepthNormalPrepass && !AllowPartialDepthNormalsPrepass(usesDeferredLighting, renderPassInputs, useDepthPriming));

            var depthCopySchedule = CalculateDepthCopySchedules(cameraData, renderPassInputs, usesDeferredLighting, requiresPrepass, hasFullPrepass);

            // Decide if & when to use GPU Occlusion Culling.
            // In deferred, do it during gbuffer laydown unless we are forced to do a *full* prepass by a render pass.
            // In forward, if there's a depth prepass, we prefer to do it there, otherwise we do it during the opaque pass.
            bool requiresDepthAfterGbuffer = RenderPassEvent.AfterRenderingGbuffer <= renderPassInputs.requiresDepthTextureEarliestEvent
                                             && renderPassInputs.requiresDepthTextureEarliestEvent <= RenderPassEvent.BeforeRenderingOpaques;
            bool occlusionTestDuringPrepass = requiresPrepass && (!usesDeferredLighting || !requiresDepthAfterGbuffer);

            OccluderPass occluderPass = OccluderPass.None;

            if (cameraData.useGPUOcclusionCulling)
            {
                if (occlusionTestDuringPrepass)
                {
                    occluderPass = OccluderPass.DepthPrepass;
                }
                else
                {
                    occluderPass = usesDeferredLighting ? OccluderPass.GBuffer : OccluderPass.ForwardOpaque;
                }
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled && cameraData.xr.hasMotionVectorPass)
            {
                // Update prevView and View matrices.
                m_XRDepthMotionPass?.Update(ref cameraData);

                // Record depthMotion pass and import XR resources into the rendergraph.
                m_XRDepthMotionPass?.Render(renderGraph, frameData);
            }
#endif

            if (requiresPrepass)
            {
                // If we're in deferred mode, prepasses always render directly to the depth attachment rather than the camera depth texture.
                // In non-deferred mode, we only render to the depth attachment directly when depth priming is enabled and we're starting with an empty depth buffer.
                bool renderToAttachment = (usesDeferredLighting || useDepthPriming);
                TextureHandle depthTarget = renderToAttachment ? resourceData.activeDepthTexture : resourceData.cameraDepthTexture;

                // Prepare stencil buffer for stencil-based cross-fade lod in depth normal prepass. Depth prepass doesn't use stencil test (same as shadow).
                if (renderingData.stencilLodCrossFadeEnabled && isDepthNormalPrepass && !renderToAttachment)
                    m_StencilCrossFadeRenderPass.Render(renderGraph, context, resourceData.cameraDepthTexture);

                bool needsOccluderUpdate = occluderPass == OccluderPass.DepthPrepass;
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

                    // The prepasses are executed multiple times when GRD occlusion is active.
                    // We only want to set global textures after all executions are complete.
                    bool isLastPass = (passIndex == (passCount - 1));

                    // When we render to the depth attachment, a copy must happen later to populate the camera depth texture and the copy will handle setting globals.
                    // If we're rendering to the camera depth texture, we can set the globals immediately.
                    bool setGlobalDepth = isLastPass && !renderToAttachment;

                    // There's no special copy logic for the camera normals texture, so we can set the global as long as we're not performing a partial prepass.
                    // In the case of a partial prepass, the global will be set later by the gbuffer pass once it completes the data in the texture.
                    bool setGlobalTextures = isLastPass && hasFullPrepass;

                    if (isDepthNormalPrepass)
                    {
                        // We set camera properties once per execution of the URP render graph, y-flip status is determined based on whether we are rendering to the backbuffer or not.
                        // DepthNormal prepass always renders to an intermediate render target which is assumed to be y-flipped by all other logic in our codebase.
                        // Therefore we need to set the camera properties for the DepthNormal to be consistent with rendering to an intermediate render target.
                        if (resourceData.isActiveTargetBackBuffer)
                        {
                            SetupRenderGraphCameraProperties(renderGraph, depthTarget);
                        }
                        DepthNormalPrepassRender(renderGraph, renderPassInputs, depthTarget, batchLayerMask, setGlobalDepth, setGlobalTextures, !hasFullPrepass);
                        // Restore camera properties for the rest of the render graph execution.
                        if (resourceData.isActiveTargetBackBuffer)
                        {
                            SetupRenderGraphCameraProperties(renderGraph, resourceData.activeColorTexture.IsValid() ? resourceData.activeColorTexture : resourceData.activeDepthTexture);
                        }
                    }
                    else
                        m_DepthPrepass.Render(renderGraph, frameData, in depthTarget, batchLayerMask, setGlobalDepth);

                    if (needsOccluderUpdate)
                    {
                        // first pass: make current frame intermediate depth pyramid
                        // second pass: make current frame final depth pyramid, set occlusion test results for later passes
                        UpdateInstanceOccluders(renderGraph, cameraData, depthTarget);
                        if (passIndex != 0)
                            InstanceOcclusionTest(renderGraph, cameraData, OcclusionTest.TestAll);
                    }
                }
            }

            // After the prepass completes, we should copy depth if necessary and also render motion vectors. (they're expected to be available whenever depth is)
            // In the case where depth is rendered as part of the prepass and no copy is necessary, we still need to render motion vectors here to ensure they're available
            // with depth before any user passes are executed.
            if (depthCopySchedule == DepthCopySchedule.AfterPrepass)
                ExecuteScheduledDepthCopyWithMotion(renderGraph, resourceData, renderPassInputs.requiresMotionVectors);
            else if ((depthCopySchedule == DepthCopySchedule.DuringPrepass) && renderPassInputs.requiresMotionVectors)
                RenderMotionVectors(renderGraph, resourceData);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingPrePasses);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                m_XROcclusionMeshPass.Render(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture);
#endif

            if (usesDeferredLighting)
            {
                m_DeferredLights.Setup(m_AdditionalLightsShadowCasterPass);

                m_DeferredLights.ResolveMixedLightingMode(lightData);
                // Once the mixed lighting mode has been discovered, we know how many MRTs we need for the gbuffer.
                // Subtractive mixed lighting requires shadowMask output, which is actually used to store unity_ProbesOcclusion values.
                m_DeferredLights.CreateGbufferTextures(renderGraph, resourceData, isDepthNormalPrepass);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingGbuffer);

                bool needsOccluderUpdate = occluderPass == OccluderPass.GBuffer;
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

	                // When we have a partial depth normals prepass, we must wait until the gbuffer pass to set global textures.
	                // In this case, the incoming global texture data is incomplete and the gbuffer pass is required to complete it.
	                bool setGlobalTextures = isDepthNormalPrepass && !hasFullPrepass;
                    m_GBufferPass.Render(renderGraph, frameData, setGlobalTextures, batchLayerMask);

                    if (needsOccluderUpdate)
                    {
                        // first pass: make current frame intermediate depth pyramid
                        // second pass: make current frame final depth pyramid, set occlusion test results for later passes
                        UpdateInstanceOccluders(renderGraph, cameraData, resourceData.activeDepthTexture);
                        if (passIndex != 0)
                            InstanceOcclusionTest(renderGraph, cameraData, OcclusionTest.TestAll);
                    }
                }

                // In addition to regularly scheduled depth copies here, we also need to copy depth when native render passes aren't available.
                // This is required because deferred lighting must read depth as a texture, but it must also bind depth as a depth write attachment at the same time.
                // When native render passes are available, we write depth into an internal gbuffer slice and read via framebuffer fetch so a depth copy is no longer required.
                if (depthCopySchedule == DepthCopySchedule.AfterGBuffer)
                    ExecuteScheduledDepthCopyWithMotion(renderGraph, resourceData, renderPassInputs.requiresMotionVectors);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingGbuffer, RenderPassEvent.BeforeRenderingDeferredLights);

                m_DeferredPass.RecordRenderGraph(renderGraph, frameData);

                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingDeferredLights, RenderPassEvent.BeforeRenderingOpaques);

                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;
                m_RenderOpaqueForwardOnlyPass.Render(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, mainShadowsTexture, additionalShadowsTexture, uint.MaxValue);
            }
            else
            {
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingGbuffer, RenderPassEvent.BeforeRenderingOpaques);

                bool needsOccluderUpdate = occluderPass == OccluderPass.ForwardOpaque;
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
                            batchLayerMask,
                            true);
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
            }

            if (depthCopySchedule == DepthCopySchedule.AfterOpaques)
                RecordCustomPassesWithDepthCopyAndMotion(renderGraph, resourceData, renderPassInputs.requiresDepthTextureEarliestEvent, RenderPassEvent.AfterRenderingOpaques, renderPassInputs.requiresMotionVectors);
            else
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingOpaques);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingSkybox);

            if (cameraData.camera.clearFlags == CameraClearFlags.Skybox && cameraData.renderType != CameraRenderType.Overlay)
            {
                cameraData.camera.TryGetComponent(out Skybox cameraSkybox);
                Material skyboxMaterial = cameraSkybox != null ? cameraSkybox.material : RenderSettings.skybox;
                if (skyboxMaterial != null)
                    m_DrawSkyboxPass.Render(renderGraph, frameData, context, resourceData.activeColorTexture, resourceData.activeDepthTexture, skyboxMaterial);
            }

            if (depthCopySchedule == DepthCopySchedule.AfterSkybox)
                ExecuteScheduledDepthCopyWithMotion(renderGraph, resourceData, renderPassInputs.requiresMotionVectors);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingSkybox);

            if (renderPassInputs.requiresColorTexture)
            {
                TextureHandle cameraColor = resourceData.cameraColor;
                Debug.Assert(cameraColor.IsValid());
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                TextureHandle cameraOpaqueTexture;
                m_CopyColorPass.Render(renderGraph, frameData, out cameraOpaqueTexture, in cameraColor, downsamplingMethod);
                resourceData.cameraOpaqueTexture = cameraOpaqueTexture;
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingTransparents);

#if UNITY_EDITOR
            {
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
                m_ProbeVolumeDebugPass.Render(renderGraph, frameData, cameraDepthTexture, cameraNormalsTexture);
            }
#endif

#if ENABLE_ADAPTIVE_PERFORMANCE
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

            if (depthCopySchedule == DepthCopySchedule.AfterTransparents)
                RecordCustomPassesWithDepthCopyAndMotion(renderGraph, resourceData, renderPassInputs.requiresDepthTextureEarliestEvent, RenderPassEvent.AfterRenderingTransparents, renderPassInputs.requiresMotionVectors);
            else
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingTransparents);

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
                if (cameraData.rendersOffscreenUI)
                {
                    m_DrawOffscreenUIPass.RenderOffscreen(renderGraph, frameData, cameraDepthAttachmentFormat, resourceData.overlayUITexture);
                    if (cameraData.blitsOffscreenUICover)
                        m_OffscreenUICoverPrepass.Render(renderGraph, cameraData, resourceData, renderGraph.defaultResources.blackTexture, true);
                }
                else
                {
                    // When the first camera renders the shared offscreen UI texture, register it as a global texture so subsequent cameras can use it in their final passes.
                    RenderGraphUtils.SetGlobalTexture(renderGraph, ShaderPropertyId.overlayUITexture, resourceData.overlayUITexture);
                }
            }
        }

        private void OnAfterRendering(RenderGraph renderGraph, bool applyPostProcessing)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var postProcessingData = frameData.Get<UniversalPostProcessingData>();

            // if it's the last camera in the stack, setup the rendering debugger
            if (cameraData.resolveFinalTarget)
                SetupRenderGraphFinalPassDebug(renderGraph, frameData);

            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, GizmoSubset.PreImageEffects);

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.BeforeRenderingPostProcessing);

            // There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = postProcessingData.isEnabled && m_PostProcess != null;

            // When FXAA or scaling is active, we must perform an additional pass at the end of the frame for the following reasons:
            // 1. FXAA expects to be the last shader running on the image before it's presented to the screen. Since users are allowed
            //    to add additional render passes after post processing occurs, we can't run FXAA until all of those passes complete as well.
            //    The FinalPost pass is guaranteed to execute after user authored passes so FXAA is always run inside of it.
            // 2. UberPost can only handle upscaling with linear filtering. All other filtering methods require the FinalPost pass.
            // 3. TAA sharpening using standalone RCAS pass is required. (When upscaling is not enabled).
            bool applyFinalPostProcessing = anyPostProcessing && cameraData.resolveFinalTarget &&
                                            ((cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) ||
                                             ((cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (cameraData.upscalingFilter == ImageUpscalingFilter.FSR)) ||
                                             (cameraData.IsTemporalAAEnabled() && cameraData.taaSettings.contrastAdaptiveSharpening > 0.0f));
            bool hasCaptureActions = cameraData.captureActions != null && cameraData.resolveFinalTarget;

            //We'll skip RecordCustomRenderGraphPasses(RenderPassEvent.AfterRenderingPostProcessing) if this is false so be careful when changing the check.
            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent >= RenderPassEvent.AfterRenderingPostProcessing && x.renderPassEvent < RenderPassEvent.AfterRendering) != null;

            bool xrDepthTargetResolved = resourceData.activeDepthID == UniversalResourceData.ActiveID.BackBuffer;

            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            bool needsColorEncoding = !resolveToDebugScreen;

            //When the debughandler displays HDR debug views, it needs to redirect (final) post-process output to an intermediate color target (debugScreenTexture).
            //Therefore, we swap the backbuffer textures for the debug screen textures such that the next post processing passes don't need to be aware of the debug handler at all.
            //At the end, when the handler is active, we swap them back. This isolates the debug handler code from the sequence of post process passes and is a common pattern to
            //use the resourceData.
            var debugRealBackBufferColor = TextureHandle.nullHandle;
            var debugRealBackBufferDepth = TextureHandle.nullHandle;

            if (resolveToDebugScreen)
            {
                debugRealBackBufferColor = resourceData.backBufferColor;
                debugRealBackBufferDepth = resourceData.backBufferDepth;

                RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureColorDescriptorForDebugScreen(ref colorDesc, cameraData.pixelWidth, cameraData.pixelHeight);
                resourceData.backBufferColor = CreateRenderGraphTexture(renderGraph, colorDesc, "_DebugScreenColor", false);

                RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, cameraDepthAttachmentFormat, cameraData.pixelWidth, cameraData.pixelHeight);
                resourceData.backBufferDepth = CreateRenderGraphTexture(renderGraph, depthDesc, "_DebugScreenDepth", false);
            }

            if (applyPostProcessing)
            {
                bool isTargetBackbuffer = cameraData.resolveFinalTarget && !applyFinalPostProcessing && !hasPassesAfterPostProcessing && !hasCaptureActions;
                var isSingleCamera = cameraData.resolveFinalTarget && cameraData.renderType == CameraRenderType.Base;

                if (isTargetBackbuffer)
                {
                    //Switch target to backbuffer for post processing pass
                    resourceData.SwitchActiveTexturesToBackbuffer();
                }
                else if(!isSingleCamera)
                {
                    //When part of a camera stack, we need to ensure the date ends up in the persistent A/B camera attachments to propagate the output to the cameras of the stack.
                    ImportResourceParams importColorParams = new ImportResourceParams();
                    importColorParams.clearOnFirstUse = true;
                    importColorParams.clearColor = Color.black;
                    importColorParams.discardOnLastUse = cameraData.resolveFinalTarget;  // check if last camera in the stack

                    resourceData.destinationCameraColor = renderGraph.ImportTexture(nextRenderGraphCameraColorHandle, importColorParams);
                }

                bool doSRGBEncoding = isTargetBackbuffer && needsColorEncoding;

                m_PostProcess.RenderPostProcessing(renderGraph, frameData, applyFinalPostProcessing, doSRGBEncoding);

                // Handle any after-post rendering debugger overlays
                if (cameraData.resolveFinalTarget)
                    SetupAfterPostRenderGraphFinalPassDebug(renderGraph, frameData);
            }

            //We already checked the passes so we can skip here if there are none as a small optimization
            if (hasPassesAfterPostProcessing)
                RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRenderingPostProcessing);

            //TODO we should check if the custom (users) passes swapped the camera color when the camera is part of a stack, because this will break camera stacking.
            //We need to copy back to a persistent A/B texture if that is the case to ensure correctness.

            if (hasCaptureActions)
            {
                m_CapturePass.RecordRenderGraph(renderGraph, frameData);
            }

            if (applyFinalPostProcessing)
            {
                //Will swap the active camera targets to backbuffer (resourceData.SwitchActiveTexturesToBackbuffer)
                m_PostProcess.RenderFinalPostProcessing(renderGraph, frameData, needsColorEncoding);
            }

            //Keep in mind that also our users could have done the final blit / final post processing and called SwitchActiveTexturesToBackbuffer().
            //Checking resourceData.isActiveTargetBackBuffer is a robust way to check if this has happened, by our own code or by the user.
            if (!resourceData.isActiveTargetBackBuffer && cameraData.resolveFinalTarget)
            {
                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderGraph, cameraData, !resolveToDebugScreen);

                //Will swap the active camera targets to backbuffer (resourceData.SwitchActiveTexturesToBackbuffer)
                m_FinalBlitPass.RecordRenderGraph(renderGraph, frameData);
            }

            RecordCustomRenderGraphPasses(renderGraph, RenderPassEvent.AfterRendering);

            // We can explicitely render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            bool shouldRenderUI = cameraData.rendersOverlayUI && cameraData.isLastBaseCamera;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && !outputToHDR)
            {
                m_DrawOverlayUIPass.RenderOverlay(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture);
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // Populate XR depth as requested by XR provider.
                if (!xrDepthTargetResolved && cameraData.xr.copyDepth)
                {
                    m_XRCopyDepthPass.CopyToDepthXR = true;
                    m_XRCopyDepthPass.Render(renderGraph, frameData, resourceData.backBufferDepth, resourceData.cameraDepth, bindAsCameraDepth: false, "XR Depth Copy");
                }
            }
#endif

            if (resolveToDebugScreen)
            {
                //We swapped the backBuffer textures to the debug screen textures before post processing. This way, all those passes don't need to be aware of the debughandling at all.
                var debugScreenTexture = resourceData.backBufferColor;

                debugHandler.Render(renderGraph, cameraData, debugScreenTexture, resourceData.overlayUITexture, debugRealBackBufferColor);

                //Swapping the backbuffer textures back
                resourceData.backBufferColor = debugRealBackBufferColor;
                resourceData.backBufferDepth = debugRealBackBufferDepth;
            }

            if (cameraData.resolveFinalTarget)
            {
#if UNITY_EDITOR
                // If we render to an intermediate depth attachment instead of the backbuffer, we need to copy the result to the backbuffer in cases where backbuffer
                // depth data is required later in the frame.
                bool backbufferDepthRequired = (cameraData.isSceneViewCamera || cameraData.isPreviewCamera || UnityEditor.Handles.ShouldRenderGizmos());
                if (s_RequiresIntermediateAttachments && backbufferDepthRequired)
                {
                    m_FinalDepthCopyPass.Render(renderGraph, frameData, resourceData.backBufferDepth, resourceData.cameraDepth, false, "Final Depth Copy");
                }
#endif
                if (cameraData.isSceneViewCamera)
                    DrawRenderGraphWireOverlay(renderGraph, frameData, resourceData.activeColorTexture);

                if (drawGizmos)
                    DrawRenderGraphGizmos(renderGraph, frameData, resourceData.activeColorTexture, resourceData.activeDepthTexture, GizmoSubset.PostImageEffects);
            }
        }

        //Require Depth Prepass for depth, normals or layer texture
        bool RequirePrepassForTextures(UniversalCameraData cameraData, in RenderPassInputSummary renderPassInputs)
        {
            bool requiresDepthTexturePrepass = renderPassInputs.requiresDepthTexture && !CanCopyDepth(cameraData);

            // A depth prepass is always required when normals are needed because URP's forward passes don't support rendering into the normals texture
            // If depth is needed without normals, we only need a prepass when the event consuming depth occurs before opaque rendering is completed.
            requiresDepthTexturePrepass |= renderPassInputs.requiresNormalsTexture;
            requiresDepthTexturePrepass |= (renderPassInputs.requiresDepthTexture && renderPassInputs.requiresDepthTextureEarliestEvent < RenderPassEvent.AfterRenderingOpaques);

            requiresDepthTexturePrepass |= DebugHandlerRequireDepthPass(cameraData);

            return requiresDepthTexturePrepass;
        }

        /// <summary>
        /// When true the pipeline needs to add a full prepass that renders depth to the currentCameraDepth.
        /// Depth priming is an optimization (on certain devices/platforms). Features that want to leverage this as an optimization
        /// need to check UniversalRender.useDepthPriming (which equal to the result of this function)
        /// to ensure that the pipeline will actually do depth priming.
        /// When this is true then we are sure that after RenderPassEvent.AfterRenderingPrePasses the currentCameraDepth has been primed with the full depth.
        /// </summary>
        static bool IsDepthPrimingEnabledRenderGraph(UniversalCameraData cameraData, in RenderPassInputSummary renderPassInputs, DepthPrimingMode depthPrimingMode, bool requirePrepassForTextures, bool usesDeferredLighting)
        {
#if UNITY_EDITOR
            // We need to disable depth-priming for DrawCameraMode.Wireframe, since depth-priming forces ZTest to Equal
            // for opaques rendering, which breaks wireframe rendering.
            if (cameraData.isSceneViewCamera)
            {
                foreach (var sceneViewObject in UnityEditor.SceneView.sceneViews)
                {
                    var sceneView = sceneViewObject as UnityEditor.SceneView;
                    if (sceneView != null && sceneView.camera == cameraData.camera && sceneView.cameraMode.drawMode == UnityEditor.DrawCameraMode.Wireframe)
                        return false;
                }
            }
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (cameraData.renderer.DebugHandler is { IsDepthPrimingCompatible: false })
                return false;
#endif

#if UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_EMBEDDED_LINUX
            bool depthPrimingRecommended = false;
#else
            bool depthPrimingRecommended = true;
#endif

            // We only do one depth prepass per camera. If the texture requires a prepass, and priming is on, then we do priming and a copy afterwards to the cameraDepthTexture.
            // However, some platforms don't support this copy (like GLES when MSAA is on). When a copy is not supported we turn off priming so the prepass will target the cameraDepthTexture, avoiding the copy.
            // Note: From Unity 2021 to Unity 6.3, depth priming was disabled in renders for reflections probes as a brute-force bugfix for artefacts appearing in reflection probes when screen space shadows are enabled.
            // Depth priming has now been restored in reflection probe renders. Please consider a more targeted fix if issues with screen space shadows resurface again. (See UUM-99152 and UUM-12397)
            if (renderPassInputs.requiresDepthTexture && !CanCopyDepth(cameraData))
                return false;

            // Depth Priming causes rendering errors with WebGL and WebGPU on Apple Arm64 GPUs.
            bool isNotWebGL = !IsWebGL();
            bool depthPrimingRequested = (depthPrimingRecommended && depthPrimingMode == DepthPrimingMode.Auto) || depthPrimingMode == DepthPrimingMode.Forced;
            bool isNotMSAA = cameraData.cameraTargetDescriptor.msaaSamples == 1;

            bool isFirstCameraToWriteDepth = cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth;
            // Depth is not rendered in a depth-only camera setup with depth priming (UUM-38158)
            bool isNotOffscreenDepthTexture = !IsOffscreenDepthTexture(cameraData);

            return depthPrimingRequested && !usesDeferredLighting && isFirstCameraToWriteDepth && isNotOffscreenDepthTexture && isNotWebGL && isNotMSAA;
        }

        internal void SetRenderingLayersGlobalTextures(RenderGraph renderGraph)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.renderingLayersTexture.IsValid() && !usesDeferredLighting)
                RenderGraphUtils.SetGlobalTexture(renderGraph, Shader.PropertyToID(m_RenderingLayersTextureName), resourceData.renderingLayersTexture, "Set Global Rendering Layers Texture");
        }

        void ImportBackBuffers(RenderGraph renderGraph, UniversalCameraData cameraData, Color clearBackgroundColor, bool isCameraTargetOffscreenDepth)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // The final output back buffer should be cleared by the graph on first use only if we have no final blit pass.
            // If there is a final blit, that blit will write the buffers so on first sight an extra clear should not be problem,
            // blit will simply blit over the cleared values. BUT! the final blit may not write the whole buffer in case of a camera
            // with a Viewport Rect smaller than the full screen. So the existing backbuffer contents need to be preserved in this case.
            // Finally for non-base cameras the backbuffer should never be cleared. (Note that there might still be two base cameras
            // rendering to the same screen. See e.g. test foundation 014 that renders a minimap)
            bool clearBackbufferOnFirstUse = (cameraData.renderType == CameraRenderType.Base) && !s_RequiresIntermediateAttachments;

            // force the clear if we are rendering to an offscreen depth texture
            clearBackbufferOnFirstUse |= isCameraTargetOffscreenDepth;

            // UI Overlay is rendered by native engine if not done within SRP
            // To check if the engine does it natively post-URP, we look at SupportedRenderingFeatures
            // and restrict it to cases where we resolve to screen and render UI overlay, i.e mostly final camera for game view
            // We cannot use directly !cameraData.rendersOverlayUI but this is similar logic
            bool isNativeUIOverlayRenderingAfterURP = !SupportedRenderingFeatures.active.rendersUIOverlay && cameraData.resolveToScreen;
            bool isNativeRenderingAfterURP = UnityEngine.Rendering.Watermark.IsVisible() || isNativeUIOverlayRenderingAfterURP;
            // If MSAA > 1, no extra native rendering after SRP and we target the BB directly (!m_RequiresIntermediateAttachments)
            // then we can discard MSAA buffers and only resolve, otherwise we must store and resolve
            bool noStoreOnlyResolveBBColor = !s_RequiresIntermediateAttachments && !isNativeRenderingAfterURP && (cameraData.cameraTargetDescriptor.msaaSamples > 1);

            //Backbuffer orientation is used for either the actual backbuffer (not a texture), or in XR for the eye texture.
            bool useActualBackbufferOrienation = !cameraData.isSceneViewCamera && !cameraData.isPreviewCamera && cameraData.targetTexture == null;
            TextureUVOrigin backbufferTextureUVOrigin = useActualBackbufferOrienation ? (SystemInfo.graphicsUVStartsAtTop ? TextureUVOrigin.TopLeft : TextureUVOrigin.BottomLeft) : TextureUVOrigin.BottomLeft;

            ImportResourceParams importBackbufferColorParams = new ImportResourceParams();
            importBackbufferColorParams.clearOnFirstUse = clearBackbufferOnFirstUse;
            importBackbufferColorParams.clearColor = clearBackgroundColor;
            importBackbufferColorParams.discardOnLastUse = noStoreOnlyResolveBBColor;
            importBackbufferColorParams.textureUVOrigin = backbufferTextureUVOrigin;

            ImportResourceParams importBackbufferDepthParams = new ImportResourceParams();
            importBackbufferDepthParams.clearOnFirstUse = clearBackbufferOnFirstUse;
            importBackbufferDepthParams.clearColor = clearBackgroundColor;
            importBackbufferDepthParams.discardOnLastUse = !isCameraTargetOffscreenDepth;
            importBackbufferDepthParams.textureUVOrigin = backbufferTextureUVOrigin;

#if UNITY_EDITOR
            // UUM-47698, UUM-97414: on TBDR GPUs like Apple M1/M2, we need to preserve the backbuffer depth for overlay cameras in Editor for Gizmos & preview grid
            if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera)
                importBackbufferDepthParams.discardOnLastUse = false;
#endif
#if ENABLE_VR && ENABLE_XR_MODULE
            // some XR devices require depth data to composite the final image. In such case, we need to preserve the eyetexture(backbuffer) depth.
            if (cameraData.xr.enabled && cameraData.xr.copyDepth)
            {
                importBackbufferDepthParams.discardOnLastUse = false;
            }
#endif

            // For BuiltinRenderTextureType wrapping RTHandles RenderGraph can't know what they are so we have to pass it in.
            RenderTargetInfo importInfo = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();

            bool isBuiltInTexture = (cameraData.targetTexture == null);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                isBuiltInTexture = false;
            }
#endif
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
                // Backbuffer is the final render target, we obtain its number of MSAA samples through Screen API
                // in some cases we disable multisampling for optimization purpose
                int numSamples = AdjustAndGetScreenMSAASamples(renderGraph, s_RequiresIntermediateAttachments);

                //BuiltinRenderTextureType.CameraTarget so this is either system render target or camera.targetTexture if non null
                //NOTE: Careful what you use here as many of the properties bake-in the camera rect so for example
                //cameraData.cameraTargetDescriptor.width is the width of the rectangle but not the actual render target
                //same with cameraData.camera.pixelWidth
                importInfo.width = Screen.width;
                importInfo.height = Screen.height;
                importInfo.volumeDepth = 1;
                importInfo.msaaSamples = numSamples;
                importInfo.format = cameraData.cameraTargetDescriptor.graphicsFormat;

                importInfoDepth = importInfo;
                importInfoDepth.format = cameraData.cameraTargetDescriptor.depthStencilFormat;
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
                    if (!PlatformRequiresExplicitMsaaResolve())
                        importInfo.bindMS = importInfo.msaaSamples > 1;

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
                    Debug.LogWarning("In the render graph API, the output Render Texture must have a depth buffer. When you select a Render Texture in any camera's Output Texture property, the Depth Stencil Format property of the texture must be set to a value other than None.");
                }
            }

            if (!isCameraTargetOffscreenDepth)
                resourceData.backBufferColor = renderGraph.ImportTexture(m_TargetColorHandle, importInfo, importBackbufferColorParams);

            resourceData.backBufferDepth = renderGraph.ImportTexture(m_TargetDepthHandle, importInfoDepth, importBackbufferDepthParams);
        }

        void CreateIntermediateCameraColorAttachment(RenderGraph renderGraph, UniversalCameraData cameraData, in TextureDesc cameraDescriptor, bool clearColor, Color clearBackgroundColor)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            var desc = cameraDescriptor;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            desc.filterMode = FilterMode.Bilinear;
            desc.wrapMode = TextureWrapMode.Clamp;

            // When there's a single camera setup, there's no need to do the double buffer technique with attachment A/B, in order to save memory allocation
            // and simplify the workflow by using a RenderGraph texture directly.
            var isSingleCamera = cameraData.resolveFinalTarget && cameraData.renderType == CameraRenderType.Base;
            if (isSingleCamera)
            {
                resourceData.cameraColor = CreateRenderGraphTexture(renderGraph, in desc, _SingleCameraTargetAttachmentName, clearColor, clearBackgroundColor, desc.filterMode, discardOnLastUse: cameraData.resolveFinalTarget);

                s_CurrentColorHandle = -1;
            }
            else
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref s_RenderGraphCameraColorHandles[0], desc, _CameraTargetAttachmentAName);
                RenderingUtils.ReAllocateHandleIfNeeded(ref s_RenderGraphCameraColorHandles[1], desc, _CameraTargetAttachmentBName);

                // Make sure that the base camera always starts rendering to the ColorAttachmentA for deterministic frame results.
                // Not doing so makes the targets look different every frame, causing the frame debugger to flash, and making debugging harder.
                if (cameraData.renderType == CameraRenderType.Base)
                {
                    s_CurrentColorHandle = 0;
                }

                ImportResourceParams importColorParams = new ImportResourceParams();
                importColorParams.clearOnFirstUse = clearColor;
                importColorParams.clearColor = clearBackgroundColor;
                importColorParams.discardOnLastUse = cameraData.resolveFinalTarget; // Last camera in stack
                resourceData.cameraColor = renderGraph.ImportTexture(currentRenderGraphCameraColorHandle, importColorParams);
            }

            resourceData.activeColorID = UniversalResourceData.ActiveID.Camera;
        }

        void CreateIntermediateCameraDepthAttachment(RenderGraph renderGraph, UniversalCameraData cameraData, in TextureDesc cameraDescriptor, bool clearDepth, Color clearBackgroundDepth, bool depthTextureIsDepthFormat)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            var desc = cameraDescriptor;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;

            bool hasMSAA = desc.msaaSamples != MSAASamples.None;

            // If we aren't using hardware depth resolves and we have MSAA, we need to resolve depth manually by binding as an MSAA texture.
            desc.bindTextureMS = hasMSAA && RenderingUtils.ShouldDepthAttachmentBindMS();

            desc.format = cameraDepthAttachmentFormat;
            desc.filterMode = FilterMode.Point;
            desc.wrapMode = TextureWrapMode.Clamp;

            bool discardOnLastUse = cameraData.resolveFinalTarget; // Last camera in stack
#if UNITY_EDITOR
            // scene filtering will reuse "camera" depth  from the normal pass for the "filter highlight" effect
            if (cameraData.isSceneViewCamera && CoreUtils.IsSceneFilteringEnabled())
                discardOnLastUse = false;
#endif

            // When there's a single camera setup, we can simplify the workflow by using a RenderGraph texture directly.
            // In the multi camera setup case, we still have to use import mechanism because each camera records its own graph; they share the imported intermediate depth texture.
            var isSingleCamera = cameraData.resolveFinalTarget && cameraData.renderType == CameraRenderType.Base;
            if (isSingleCamera)
            {
                resourceData.cameraDepth = CreateRenderGraphTexture(renderGraph, desc, _CameraDepthAttachmentName, clearDepth, clearBackgroundDepth, desc.filterMode, desc.wrapMode, discardOnLastUse: discardOnLastUse);
            }
            else
            {

                RenderingUtils.ReAllocateHandleIfNeeded(ref s_RenderGraphCameraDepthHandle, desc, _CameraDepthAttachmentName);

                ImportResourceParams importDepthParams = new ImportResourceParams();
                importDepthParams.clearOnFirstUse = clearDepth;
                importDepthParams.clearColor = clearBackgroundDepth;
                importDepthParams.discardOnLastUse = discardOnLastUse;

                resourceData.cameraDepth = renderGraph.ImportTexture(s_RenderGraphCameraDepthHandle, importDepthParams);
            }

            resourceData.activeDepthID = UniversalResourceData.ActiveID.Camera;
        }

        void CreateCameraDepthCopyTexture(RenderGraph renderGraph, TextureDesc descriptor, bool isDepthTexture, Color clearColor)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var depthDescriptor = descriptor;
            depthDescriptor.msaaSamples = MSAASamples.None;// Depth-Only pass don't use MSAA


            if (isDepthTexture)
            {
                depthDescriptor.format = cameraDepthTextureFormat;
                depthDescriptor.clearBuffer = true; //will be rendered to
            }
            else
            {
                depthDescriptor.format = GraphicsFormat.R32_SFloat;
                depthDescriptor.clearBuffer = false; //will be copied to
            }

            resourceData.cameraDepthTexture = CreateRenderGraphTexture(renderGraph, depthDescriptor, "_CameraDepthTexture", depthDescriptor.clearBuffer, clearColor);
        }

        void CreateMotionVectorTextures(RenderGraph renderGraph, TextureDesc descriptor)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            descriptor.msaaSamples = MSAASamples.None; // Disable MSAA, consider a pixel resolve for half left velocity and half right velocity --> no velocity, which is untrue.

            descriptor.format = MotionVectorRenderPass.k_TargetFormat;

            resourceData.motionVectorColor = CreateRenderGraphTexture(renderGraph, descriptor, MotionVectorRenderPass.k_MotionVectorTextureName, true, Color.black);

            descriptor.format = cameraDepthAttachmentFormat;
            resourceData.motionVectorDepth = CreateRenderGraphTexture(renderGraph, descriptor, MotionVectorRenderPass.k_MotionVectorDepthTextureName, true, Color.black);
        }

        void CreateCameraNormalsTexture(RenderGraph renderGraph, TextureDesc descriptor)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            descriptor.msaaSamples = MSAASamples.None; // Never use MSAA for the normal texture!
            // Find compatible render-target format for storing normals.
            // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
            // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
            var normalsName = !usesDeferredLighting ?
                DepthNormalOnlyPass.k_CameraNormalsTextureName : DeferredLights.k_GBufferNames[m_DeferredLights.GBufferNormalSmoothnessIndex];
            descriptor.format = !usesDeferredLighting ?
                DepthNormalOnlyPass.GetGraphicsFormat() : m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferNormalSmoothnessIndex);
            resourceData.cameraNormalsTexture = CreateRenderGraphTexture(renderGraph, descriptor, normalsName, true, Color.black);
        }

        void CreateRenderingLayersTexture(RenderGraph renderGraph, TextureDesc descriptor)
        {
            if (m_RequiresRenderingLayer)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                m_RenderingLayersTextureName = "_CameraRenderingLayersTexture";

                // TODO RENDERGRAPH: deferred optimization
                if (usesDeferredLighting && m_DeferredLights.UseRenderingLayers)
                    m_RenderingLayersTextureName = DeferredLights.k_GBufferNames[m_DeferredLights.GBufferRenderingLayersIndex];

                if (!m_RenderingLayerProvidesRenderObjectPass)
                    descriptor.msaaSamples = MSAASamples.None;// Depth-Only pass don't use MSAA

                // Find compatible render-target format for storing normals.
                // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
                // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
                if (usesDeferredLighting && m_RequiresRenderingLayer)
                    descriptor.format = m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferRenderingLayersIndex); // the one used by the gbuffer.
                else
                    descriptor.format = RenderingLayerUtils.GetFormat(m_RenderingLayersMaskSize);

                resourceData.renderingLayersTexture = CreateRenderGraphTexture(renderGraph, descriptor, m_RenderingLayersTextureName, true, descriptor.clearColor);
            }
        }

        void CreateOffscreenUITexture(RenderGraph renderGraph, TextureDesc descriptor)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            DrawScreenSpaceUIPass.ConfigureOffscreenUITextureDesc(ref descriptor);
            RenderingUtils.ReAllocateHandleIfNeeded(ref s_OffscreenUIColorHandle, descriptor, name: "_OverlayUITexture");
            resourceData.overlayUITexture = renderGraph.ImportTexture(s_OffscreenUIColorHandle);
        }

        void DepthNormalPrepassRender(RenderGraph renderGraph, RenderPassInputSummary renderPassInputs, in TextureHandle depthTarget, uint batchLayerMask, bool setGlobalDepth, bool setGlobalTextures, bool partialPass)
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

            m_DepthNormalPrepass.Render(renderGraph, frameData, resourceData.cameraNormalsTexture, in depthTarget, resourceData.renderingLayersTexture, batchLayerMask, setGlobalDepth, setGlobalTextures, partialPass);

            if (m_RenderingLayerProvidesByDepthNormalPass)
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

                builder.AllowGlobalStateModification(true);

                builder.SetGlobalTextureAfterPass(handle, nameId);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
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

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(data.clearFlags, data.clearColor, 1, 0);
                });
            }
        }
    }

}
