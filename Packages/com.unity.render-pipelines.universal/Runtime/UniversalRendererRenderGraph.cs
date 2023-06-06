using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
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
        DBufferDepth
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
        private static RTHandle m_RenderGraphCameraDepthHandle;
        private static int m_CurrentColorHandle = 0;

        private RTHandle currentRenderGraphCameraColorHandle => (m_RenderGraphCameraColorHandles[m_CurrentColorHandle]);

        // get the next m_RenderGraphCameraColorHandles and make it the new current for future accesses
        private RTHandle nextRenderGraphCameraColorHandle
        {
            get
            {
                m_CurrentColorHandle = (m_CurrentColorHandle + 1) % 2;
                return m_RenderGraphCameraColorHandles[m_CurrentColorHandle];
            }
        }

        private UniversalResource m_ActiveColorID;
        private UniversalResource m_ActiveDepthID;

        /// <summary>
        /// Returns the current active color target texture. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <returns>TextureHandle</returns>
        public TextureHandle activeColorTexture => (resources.GetTexture(m_ActiveColorID));

        /// <summary>
        /// Returns the current active depth target texture. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <returns>TextureHandle</returns>
        public TextureHandle activeDepthTexture => (resources.GetTexture(m_ActiveDepthID));

        /// <summary>
        /// True if the current active target is the backbuffer. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <returns>bool</returns>
        public bool isActiveTargetBackBuffer
        {
            get
            {
                if (!resources.isAccessible)
                {
                    Debug.LogError("Trying to access FrameResources outside of the current frame setup.");
                    return false;
                }

                return (m_ActiveColorID == UniversalResource.BackBufferColor);
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
            m_RenderGraphCameraDepthHandle?.Release();
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
            rgDesc.enableRandomWrite = false;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;
            rgDesc.isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None;
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
            rgDesc.enableRandomWrite = false;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;

            return renderGraph.CreateTexture(rgDesc);
        }

        bool ShouldApplyPostProcessing(ref RenderingData renderingData)
        {
            return renderingData.cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
        }

        bool CameraHasPostProcessingWithDepth(ref RenderingData renderingData)
        {
            return ShouldApplyPostProcessing(ref renderingData) && renderingData.cameraData.postProcessingRequiresDepthTexture;
        }

        void RequiresColorAndDepthTextures(RenderGraph renderGraph, out bool createColorTexture, out bool createDepthTexture, ref RenderingData renderingData, RenderPassInputSummary renderPassInputs)
        {
            bool isPreviewCamera = renderingData.cameraData.isPreviewCamera;
            bool requiresDepthPrepass = RequireDepthPrepass(ref renderingData, renderPassInputs);

            createColorTexture = (rendererFeatures.Count != 0 && m_IntermediateTextureMode == IntermediateTextureMode.Always) && !isPreviewCamera;
            createColorTexture |= RequiresIntermediateColorTexture(ref renderingData.cameraData);
            createColorTexture &= !isPreviewCamera;

            createDepthTexture = RequireDepthTexture(ref renderingData, renderPassInputs, requiresDepthPrepass);

#if ENABLE_VR && ENABLE_XR_MODULE
            // URP can't handle msaa/size mismatch between depth RT and color RT(for now we create intermediate textures to ensure they match)
            if (renderingData.cameraData.xr.enabled)
                createColorTexture |= createDepthTexture;
#endif
#if UNITY_ANDROID || UNITY_WEBGL
            // GLES can not use render texture's depth buffer with the color buffer of the backbuffer
            // in such case we create a color texture for it too.
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
                createColorTexture |= createDepthTexture;
#endif
            useDepthPriming = IsDepthPrimingEnabled(ref renderingData.cameraData);

            if (useRenderPassEnabled || useDepthPriming)
                createColorTexture |= createDepthTexture;

            //Scene filtering redraws the objects on top of the resulting frame. It has to draw directly to the sceneview buffer.
            bool sceneViewFilterEnabled = renderingData.cameraData.camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered;
            bool intermediateRenderTexture = (createColorTexture || createDepthTexture) && !sceneViewFilterEnabled;
            createDepthTexture = intermediateRenderTexture;
        }

        void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;

            RenderTargetIdentifier targetColorId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.Depth;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                targetColorId = cameraData.xr.renderTarget;
                targetDepthId = cameraData.xr.renderTarget;
            }
#endif

            if (m_XRTargetHandleAlias == null || m_XRTargetHandleAlias.nameID != targetColorId)
            {
                m_XRTargetHandleAlias?.Release();
                m_XRTargetHandleAlias = RTHandles.Alloc(targetColorId);
            }

            if (m_XRDepthHandleAlias == null || m_XRDepthHandleAlias.nameID != targetDepthId)
            {
                m_XRDepthHandleAlias?.Release();
                m_XRDepthHandleAlias = RTHandles.Alloc(targetDepthId);
            }

            resources.SetTexture(UniversalResource.BackBufferColor, renderGraph.ImportTexture(m_XRTargetHandleAlias));
            resources.SetTexture(UniversalResource.BackBufferDepth, renderGraph.ImportTexture(m_XRDepthHandleAlias));

            #region Intermediate Camera Target
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            // Enable depth normal prepass if it's needed by rendering layers
            if (m_RenderingLayerProvidesByDepthNormalPass)
                renderPassInputs.requiresNormalsTexture = true;

            // We configure this for the first camera of the stack and overlay camera will reuse create color/depth var
            // to pick the correct target, as if there is an intermediate texture, overlay cam should use them
            if (cameraData.renderType == CameraRenderType.Base)
                 RequiresColorAndDepthTextures(renderGraph, out m_CreateColorTexture, out m_CreateDepthTexture, ref renderingData, renderPassInputs);

            if (m_CreateColorTexture)
            {
                var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.useMipMap = false;
                cameraTargetDescriptor.autoGenerateMips = false;
                cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraColorHandles[0], cameraTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CameraTargetAttachmentA");
                RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraColorHandles[1], cameraTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CameraTargetAttachmentB");

                // Make sure that the base camera always starts rendering to the ColorAttachmentA for deterministic frame results.
                // Not doing so makes the targets look different every frame, causing the frame debugger to flash, and making debugging harder.
                if (renderingData.cameraData.renderType == CameraRenderType.Base)
                    m_CurrentColorHandle = 0;

                resources.SetTexture(UniversalResource.CameraColor, renderGraph.ImportTexture(currentRenderGraphCameraColorHandle));
                m_ActiveColorID = UniversalResource.CameraColor;
            }
            else
            {
                m_ActiveColorID = UniversalResource.BackBufferColor;
            }


            if (m_CreateDepthTexture)
            {
                var depthDescriptor = cameraData.cameraTargetDescriptor;
                depthDescriptor.useMipMap = false;
                depthDescriptor.autoGenerateMips = false;
                depthDescriptor.bindMS = false;

                bool hasMSAA = depthDescriptor.msaaSamples > 1 && (SystemInfo.supportsMultisampledTextures != 0);

                // TODO RENDERGRAPH: once all passes are ported to RasterCommandBuffers we need to reenable depth resolve
                m_CopyDepthPass.m_CopyResolvedDepth = false;

                if (hasMSAA)
                    depthDescriptor.bindMS = true;

                // binding MS surfaces is not supported by the GLES backend, and it won't be fixed after investigating
                // the high performance impact of potential fixes, which would make it more expensive than depth prepass (fogbugz 1339401 for more info)
                if (IsGLESDevice())
                    depthDescriptor.bindMS = false;

                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraDepthHandle, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthAttachment");

                resources.SetTexture(UniversalResource.CameraDepth, renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle));
                m_ActiveDepthID = UniversalResource.CameraDepth;
            }
            else
            {
            	m_ActiveDepthID = UniversalResource.BackBufferDepth;
            }
            #endregion

            CreateCameraDepthCopyTexture(renderGraph, cameraData.cameraTargetDescriptor,RequireDepthPrepass(ref renderingData, renderPassInputs) && this.renderingModeActual != RenderingMode.Deferred);

            CreateCameraNormalsTexture(renderGraph, cameraData.cameraTargetDescriptor);

            CreateMotionVectorTextures(renderGraph, cameraData.cameraTargetDescriptor);

            CreateRenderingLayersTexture(renderGraph, cameraData.cameraTargetDescriptor);

            CreateAfterPostProcessTexture(renderGraph, cameraData.cameraTargetDescriptor);
        }

        void SetupRenderingLayers(ref RenderingData renderingData)
        {
            // Gather render pass require rendering layers event and mask size
            m_RequiresRenderingLayer = RenderingLayerUtils.RequireRenderingLayers(this, rendererFeatures, renderingData.cameraData.cameraTargetDescriptor.msaaSamples,
                out m_RenderingLayersEvent, out m_RenderingLayersMaskSize);

            m_RenderingLayerProvidesRenderObjectPass = m_RequiresRenderingLayer && renderingModeActual == RenderingMode.Forward && m_RenderingLayersEvent == RenderingLayerUtils.Event.Opaque;
            m_RenderingLayerProvidesByDepthNormalPass = m_RequiresRenderingLayer && m_RenderingLayersEvent == RenderingLayerUtils.Event.DepthNormalPrePass;

            if (m_DeferredLights != null)
            {
                m_DeferredLights.RenderingLayerMaskSize = m_RenderingLayersMaskSize;
                m_DeferredLights.UseDecalLayers = m_RequiresRenderingLayer;
            }
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            useRenderPassEnabled = false;

            SetupRenderingLayers(ref renderingData);

            CreateRenderGraphCameraRenderTargets(renderGraph, ref renderingData);

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.BeforeRendering);

            SetupRenderGraphCameraProperties(renderGraph, ref renderingData, isActiveTargetBackBuffer);
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            ProcessVFXCameraCommand(renderGraph, ref renderingData);
#endif
            DebugHandler?.Setup(ref renderingData);


            cameraData.renderer.useDepthPriming = useDepthPriming;

            if (cameraData.camera.targetTexture != null && cameraData.camera.targetTexture.format == RenderTextureFormat.Depth)
            {
                OnOffscreenDepthTextureRendering(renderGraph, context, ref renderingData);
                return;
            }

            OnBeforeRendering(renderGraph, ref renderingData);

            BeginRenderGraphXRRendering(renderGraph, ref renderingData);

            OnMainRendering(renderGraph, context, ref renderingData);

            OnAfterRendering(renderGraph, ref renderingData);

            EndRenderGraphXRRendering(renderGraph, ref renderingData);
        }

        internal override void OnFinishRenderGraphRendering(ref RenderingData renderingData)
        {
            if (this.renderingModeActual == RenderingMode.Deferred)
                m_DeferredPass.OnCameraCleanup(renderingData.commandBuffer);

            m_CopyDepthPass.OnCameraCleanup(renderingData.commandBuffer);
        }

        private static bool m_CreateColorTexture;
        private static bool m_CreateDepthTexture;

        private void OnOffscreenDepthTextureRendering(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.BeforeRenderingShadows, RenderPassEvent.BeforeRenderingOpaques);
            m_RenderOpaqueForwardPass.Render(renderGraph, resources.GetTexture(UniversalResource.BackBufferColor), TextureHandle.nullHandle, TextureHandle.nullHandle, TextureHandle.nullHandle, ref renderingData);
            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingOpaques, RenderPassEvent.BeforeRenderingSkybox);
            m_DrawSkyboxPass.Render(renderGraph, context, resources.GetTexture(UniversalResource.BackBufferColor), TextureHandle.nullHandle, ref renderingData);
            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingSkybox, RenderPassEvent.BeforeRenderingTransparents);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            m_RenderTransparentForwardPass.Render(renderGraph, resources.GetTexture(UniversalResource.BackBufferColor), TextureHandle.nullHandle, TextureHandle.nullHandle, TextureHandle.nullHandle, ref renderingData);
            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingTransparents, RenderPassEvent.AfterRendering);
        }
        private void OnBeforeRendering(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            m_ForwardLights.PreSetup(ref renderingData);

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.BeforeRenderingShadows);

            bool renderShadows = false;

            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
            {
                renderShadows = true;
                TextureHandle mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderGraph, ref renderingData);
                resources.SetTexture(UniversalResource.MainShadowsTexture, mainShadowsTexture);
            }

            if (m_AdditionalLightsShadowCasterPass.Setup(ref renderingData))
            {
                renderShadows = true;
                TextureHandle additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderGraph, ref renderingData);
                resources.SetTexture(UniversalResource.AdditionalShadowsTexture, additionalShadowsTexture);
            }

            // The camera need to be setup again after the shadows since those passes override some settings
            // TODO RENDERGRAPH: move the setup code into the shadow passes
            if (renderShadows)
                SetupRenderGraphCameraProperties(renderGraph, ref renderingData, isActiveTargetBackBuffer);

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingShadows);
        }

        private void OnMainRendering(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RTClearFlags clearFlags = RTClearFlags.None;

            if (renderingData.cameraData.renderType == CameraRenderType.Base)
                clearFlags = RTClearFlags.All;
            else if (renderingData.cameraData.clearDepth)
                clearFlags = RTClearFlags.Depth;

            if (clearFlags != RTClearFlags.None)
                ClearTargetsPass.Render(renderGraph, activeColorTexture, activeDepthTexture, clearFlags, renderingData.cameraData.backgroundColor);

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.BeforeRenderingPrePasses);

            ref var cameraData = ref renderingData.cameraData;
            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = CameraHasPostProcessingWithDepth(ref renderingData);

            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            if (m_RenderingLayerProvidesByDepthNormalPass)
                renderPassInputs.requiresNormalsTexture = true;

#if UNITY_EDITOR
            if (m_ProbeVolumeDebugPass.NeedsNormal())
                renderPassInputs.requiresNormalsTexture = true;
#endif

            bool requiresDepthPrepass = RequireDepthPrepass(ref renderingData, renderPassInputs);
            bool requiresDepthCopyPass = !requiresDepthPrepass
                                         && (cameraData.requiresDepthTexture || cameraHasPostProcessingWithDepth || renderPassInputs.requiresDepthTexture)
                                         && m_CreateDepthTexture; // we create both intermediate textures if this is true, so instead of repeating the checks we reuse this
            bool requiresColorCopyPass = renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture;
            requiresColorCopyPass &= !cameraData.isPreviewCamera;
            bool requiredColorGradingLutPass = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            bool isDeferred = this.renderingModeActual == RenderingMode.Deferred;

            if (requiresDepthPrepass)
            {
                // TODO RENDERGRAPH: is this always a valid assumption for deferred rendering?
                TextureHandle depthTarget = (renderingModeActual == RenderingMode.Deferred) ? activeDepthTexture : resources.GetTexture(UniversalResource.CameraDepthTexture);
                depthTarget = (useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth)) ? activeDepthTexture : depthTarget;

                if (renderPassInputs.requiresNormalsTexture)
                    DepthNormalPrepassRender(renderGraph, renderPassInputs, depthTarget, ref renderingData);
                else
                    m_DepthPrepass.Render(renderGraph, ref depthTarget, ref renderingData);
            }

            // depth priming still needs to copy depth because the prepass doesn't target anymore CameraDepthTexture
            // TODO: this is unoptimal, investigate optimizations
            if (useDepthPriming)
            {
                TextureHandle depth = resources.GetTexture(UniversalResource.CameraDepth);
                TextureHandle cameraDepthTexture = resources.GetTexture(UniversalResource.CameraDepthTexture);
                m_PrimedDepthCopyPass.Render(renderGraph, cameraDepthTexture, depth, ref renderingData);
            }

            if (cameraData.renderType == CameraRenderType.Base && !requiresDepthPrepass && !requiresDepthCopyPass)
                RenderGraphUtils.SetGlobalTexture(renderGraph, "_CameraDepthTexture", SystemInfo.usesReversedZBuffer ? renderGraph.defaultResources.blackTexture : renderGraph.defaultResources.whiteTexture, "Set default Camera Depth Texture");

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingPrePasses);

            if (requiredColorGradingLutPass)
            {
                TextureHandle internalColorLut;
                m_PostProcessPasses.colorGradingLutPass.Render(renderGraph, out internalColorLut, ref renderingData);
                resources.SetTexture(UniversalResource.InternalColorLut, internalColorLut);
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                m_XROcclusionMeshPass.Render(renderGraph, activeDepthTexture, ref renderingData);
#endif

            if (isDeferred)
            {
                m_DeferredLights.Setup(m_AdditionalLightsShadowCasterPass);
                if (m_DeferredLights != null)
                {
                    m_DeferredLights.UseRenderPass = false;
                    m_DeferredLights.HasNormalPrepass = renderPassInputs.requiresNormalsTexture;
                    m_DeferredLights.HasDepthPrepass = requiresDepthPrepass;
                    m_DeferredLights.ResolveMixedLightingMode(ref renderingData);
                    m_DeferredLights.IsOverlay = cameraData.renderType == CameraRenderType.Overlay;
                }

                RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.BeforeRenderingGbuffer);

                m_GBufferPass.Render(renderGraph, activeColorTexture, activeDepthTexture, ref renderingData, resources);
                TextureHandle cameraDepthTexture = resources.GetTexture(UniversalResource.CameraDepthTexture);
                m_GBufferCopyDepthPass.Render(renderGraph, cameraDepthTexture, activeDepthTexture, ref renderingData, "GBuffer Depth Copy");

                RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingGbuffer, RenderPassEvent.BeforeRenderingDeferredLights);

                TextureHandle[] gbuffer = m_GBufferPass.GetFrameResourcesGBufferArray(resources);
                m_DeferredPass.Render(renderGraph, activeColorTexture, activeDepthTexture, gbuffer, ref renderingData);

                RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingDeferredLights, RenderPassEvent.BeforeRenderingOpaques);

                TextureHandle mainShadowsTexture = resources.GetTexture(UniversalResource.MainShadowsTexture);
                TextureHandle additionalShadowsTexture = resources.GetTexture(UniversalResource.AdditionalShadowsTexture);
                m_RenderOpaqueForwardOnlyPass.Render(renderGraph, activeColorTexture, activeDepthTexture, mainShadowsTexture, additionalShadowsTexture, ref renderingData);
            }
            else
            {
                RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.BeforeRenderingOpaques);

                if (m_RenderingLayerProvidesRenderObjectPass)
                {
                    TextureHandle renderingLayersTexture = resources.GetTexture(UniversalResource.RenderingLayersTexture);
                    TextureHandle mainShadowsTexture = resources.GetTexture(UniversalResource.MainShadowsTexture);
                    TextureHandle additionalShadowsTexture = resources.GetTexture(UniversalResource.AdditionalShadowsTexture);
                    m_RenderOpaqueForwardWithRenderingLayersPass.Render(renderGraph, activeColorTexture, renderingLayersTexture, activeDepthTexture, mainShadowsTexture, additionalShadowsTexture, m_RenderingLayersMaskSize, ref renderingData);
                    SetRenderingLayersGlobalTextures(renderGraph);
                }
                else
                {
                    TextureHandle mainShadowsTexture = resources.GetTexture(UniversalResource.MainShadowsTexture);
                    TextureHandle additionalShadowsTexture = resources.GetTexture(UniversalResource.AdditionalShadowsTexture);
                    m_RenderOpaqueForwardPass.Render(renderGraph, activeColorTexture, activeDepthTexture, mainShadowsTexture, additionalShadowsTexture, ref renderingData);
                }
            }

            // Custom passes come before built-in passes to keep parity with non-RG code path where custom passes are added before renderer Setup.
            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingOpaques, RenderPassEvent.BeforeRenderingSkybox);

            if (cameraData.camera.clearFlags == CameraClearFlags.Skybox && cameraData.renderType != CameraRenderType.Overlay)
            {
                if (RenderSettings.skybox != null || (cameraData.camera.TryGetComponent(out Skybox cameraSkybox) && cameraSkybox.material != null))
                    m_DrawSkyboxPass.Render(renderGraph, context, activeColorTexture, activeDepthTexture, ref renderingData);
            }

            m_CopyDepthMode = renderPassInputs.requiresDepthTextureEarliestEvent < RenderPassEvent.AfterRenderingTransparents ? CopyDepthMode.AfterOpaques : m_CopyDepthMode;
            if (requiresDepthCopyPass && m_CopyDepthMode != CopyDepthMode.AfterTransparents)
            {
                TextureHandle cameraDepthTexture = resources.GetTexture(UniversalResource.CameraDepthTexture);
                m_CopyDepthPass.Render(renderGraph, cameraDepthTexture, activeDepthTexture, ref renderingData);
            }

            if (requiresColorCopyPass)
            {
                TextureHandle activeColor = activeColorTexture;
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                TextureHandle cameraOpaqueTexture;
                m_CopyColorPass.Render(renderGraph, out cameraOpaqueTexture, in activeColor, downsamplingMethod, ref renderingData);
                resources.SetTexture(UniversalResource.CameraOpaqueTexture, cameraOpaqueTexture);
            }

#if UNITY_EDITOR
            {
                TextureHandle cameraDepthTexture = resources.GetTexture(UniversalResource.CameraDepthTexture);
                TextureHandle cameraNormalsTexture = resources.GetTexture(UniversalResource.CameraNormalsTexture);
                m_ProbeVolumeDebugPass.Render(renderGraph, ref renderingData, cameraDepthTexture, cameraNormalsTexture);
            }
#endif

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingSkybox, RenderPassEvent.BeforeRenderingTransparents);

            // TODO RENDERGRAPH: bind _CameraOpaqueTexture, _CameraDepthTexture in transparent pass?
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                TextureHandle mainShadowsTexture = resources.GetTexture(UniversalResource.MainShadowsTexture);
                TextureHandle additionalShadowsTexture = resources.GetTexture(UniversalResource.AdditionalShadowsTexture);
                m_RenderTransparentForwardPass.m_ShouldTransparentsReceiveShadows = !m_TransparentSettingsPass.Setup(ref renderingData);
                m_RenderTransparentForwardPass.Render(renderGraph, activeColorTexture, activeDepthTexture, mainShadowsTexture, additionalShadowsTexture, ref renderingData);
            }
            
            // Custom passes come before built-in passes to keep parity with non-RG code path where custom passes are added before renderer Setup.

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingTransparents);

            if (requiresDepthCopyPass && m_CopyDepthMode == CopyDepthMode.AfterTransparents)
            {
                TextureHandle cameraDepthTexture = resources.GetTexture(UniversalResource.CameraDepthTexture);
                m_CopyDepthPass.Render(renderGraph, cameraDepthTexture, activeDepthTexture, ref renderingData);
            }

            // TODO: Postprocess pass should be able configure its render pass inputs per camera per frame (settings) BEFORE building any of the graph
            // TODO: Alternatively we could always build the graph (a potential graph) and cull away unused passes if "record + cull" is fast enough.
            // TODO: Currently we just override "requiresMotionVectors" for TAA in GetRenderPassInputs()
            if (renderPassInputs.requiresMotionVectors)
            {
                TextureHandle cameraDepthTexture = resources.GetTexture(UniversalResource.CameraDepthTexture);
                TextureHandle motionVectorColor = resources.GetTexture(UniversalResource.MotionVectorColor);
                TextureHandle motionVectorDepth = resources.GetTexture(UniversalResource.MotionVectorDepth);
                // Depends on camera depth
                m_MotionVectorPass.Render(renderGraph, cameraDepthTexture, motionVectorColor, motionVectorDepth, ref renderingData);
            }

            m_OnRenderObjectCallbackPass.Render(renderGraph, activeColorTexture, activeDepthTexture, ref renderingData);
        }

        private void OnAfterRendering(RenderGraph renderGraph, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
#endif
            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, activeColorTexture, activeDepthTexture, GizmoSubset.PreImageEffects, ref renderingData);

            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.BeforeRenderingPostProcessing);

            bool cameraTargetResolved = false;
            bool applyPostProcessing = ShouldApplyPostProcessing(ref renderingData);
            // There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = renderingData.postProcessingEnabled && m_PostProcessPasses.isCreated;
            bool applyFinalPostProcessing = anyPostProcessing && renderingData.cameraData.resolveFinalTarget &&
                                            ((renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) ||
                                             ((renderingData.cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (renderingData.cameraData.upscalingFilter != ImageUpscalingFilter.Linear)));
            bool hasCaptureActions = renderingData.cameraData.captureActions != null && renderingData.cameraData.resolveFinalTarget;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;

            TextureHandle cameraColor = resources.GetTexture(UniversalResource.CameraColor);

            if (applyPostProcessing)
            {
                TextureHandle activeColor = activeColorTexture;
                TextureHandle backbuffer = resources.GetTexture(UniversalResource.BackBufferColor);
                TextureHandle internalColorLut = resources.GetTexture(UniversalResource.InternalColorLut);

                bool isTargetBackbuffer = (renderingData.cameraData.resolveFinalTarget && !applyFinalPostProcessing && !hasPassesAfterPostProcessing);
                // if the postprocessing pass is trying to read and write to the same CameraColor target, we need to swap so it writes to a different target,
                // since reading a pass attachment is not possible. Normally this would be possible using temporary RenderGraph managed textures.
                // The reason why in this case we need to use "external" RTHandles is to preserve the results for camera stacking.
                // TODO RENDERGRAPH: Once all cameras will run in a single RenderGraph we can just use temporary RenderGraph textures as intermediate buffer.
                if (!isTargetBackbuffer)
                {
                    cameraColor = renderGraph.ImportTexture(nextRenderGraphCameraColorHandle);
                    resources.SetTexture(UniversalResource.CameraColor, cameraColor);
                }

                var target = isTargetBackbuffer ? backbuffer : cameraColor;
                m_PostProcessPasses.postProcessPass.RenderPostProcessingRenderGraph(renderGraph, in activeColor, in internalColorLut, in target, ref renderingData, applyFinalPostProcessing);
            }

            if (applyFinalPostProcessing)
                m_PostProcessPasses.finalPostProcessPass.RenderFinalPassRenderGraph(renderGraph, in cameraColor, ref renderingData);

            cameraTargetResolved =
                // final PP always blit to camera target
                applyFinalPostProcessing ||
                // no final PP but we have PP stack. In that case it blit unless there are render pass after PP
                (applyPostProcessing && !hasPassesAfterPostProcessing && !hasCaptureActions);

            // TODO RENDERGRAPH: we need to discuss and decide if RenderPassEvent.AfterRendering injected passes should only be called after the last camera in the stack
            RecordCustomRenderGraphPasses(renderGraph, ref renderingData, RenderPassEvent.AfterRenderingPostProcessing, RenderPassEvent.AfterRendering);

            if (!isActiveTargetBackBuffer && renderingData.cameraData.resolveFinalTarget && !cameraTargetResolved)
            {
                m_FinalBlitPass.Render(renderGraph, ref renderingData, resources.GetTexture(UniversalResource.CameraColor), resources.GetTexture(UniversalResource.BackBufferColor), resources.GetTexture(UniversalResource.OverlayUITexture));
                m_ActiveColorID = UniversalResource.BackBufferColor;
                m_ActiveDepthID = UniversalResource.BackBufferDepth;
            }

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera || (isGizmosEnabled && renderingData.cameraData.resolveFinalTarget))
            {
                TextureHandle cameraDepthTexture = resources.GetTexture(UniversalResource.CameraDepthTexture);
                m_FinalDepthCopyPass.CopyToDepth = true;
                m_FinalDepthCopyPass.MssaSamples = 0;
                m_FinalDepthCopyPass.Render(renderGraph, activeDepthTexture, cameraDepthTexture, ref renderingData, "Final Depth Copy");
            }
#endif

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, resources.GetTexture(UniversalResource.BackBufferColor), activeDepthTexture, GizmoSubset.PostImageEffects, ref renderingData);
        }

        bool RequireDepthPrepass(ref RenderingData renderingData, RenderPassInputSummary renderPassInputs)
        {
            ref var cameraData = ref renderingData.cameraData;
            bool applyPostProcessing = ShouldApplyPostProcessing(ref renderingData);
            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = CameraHasPostProcessingWithDepth(ref renderingData);

            bool forcePrepass = (m_CopyDepthMode == CopyDepthMode.ForcePrepass);
            bool depthPrimingEnabled = IsDepthPrimingEnabled(ref cameraData);

            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || depthPrimingEnabled;
            bool requiresDepthPrepass = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && (!CanCopyDepth(ref cameraData) || forcePrepass);
            requiresDepthPrepass |= cameraData.isSceneViewCamera;
            // requiresDepthPrepass |= isGizmosEnabled;
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

        bool RequireDepthTexture(ref RenderingData renderingData, RenderPassInputSummary renderPassInputs, bool requiresDepthPrepass)
        {
            bool depthPrimingEnabled = IsDepthPrimingEnabled(ref renderingData.cameraData);
            bool requiresDepthTexture = renderingData.cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || depthPrimingEnabled;
            bool cameraHasPostProcessingWithDepth = CameraHasPostProcessingWithDepth(ref renderingData);

            var createDepthTexture = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && !requiresDepthPrepass;
            createDepthTexture |= !renderingData.cameraData.resolveFinalTarget;
            // Deferred renderer always need to access depth buffer.
            createDepthTexture |= (renderingModeActual == RenderingMode.Deferred && !useRenderPassEnabled);
            // Some render cases (e.g. Material previews) have shown we need to create a depth texture when we're forcing a prepass.
            createDepthTexture |= depthPrimingEnabled;
            // TODO: seems like with mrt depth is not taken from first target. Investigate if this is needed
            createDepthTexture |= m_RenderingLayerProvidesRenderObjectPass;

            return createDepthTexture;
        }

        internal void SetRenderingLayersGlobalTextures(RenderGraph renderGraph)
        {
            RenderGraphUtils.SetGlobalTexture(renderGraph, m_RenderingLayersTextureName , resources.GetTexture(UniversalResource.RenderingLayersTexture), "Set Rendering Layers Texture");

            if (renderingModeActual == RenderingMode.Deferred) // As this is requested by render pass we still want to set it
                RenderGraphUtils.SetGlobalTexture(renderGraph, "_CameraRenderingLayersTexture", resources.GetTexture(UniversalResource.RenderingLayersTexture), "Set Deferred Rendering Layers Texture");
        }

        void CreateCameraDepthCopyTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor, bool isDepthTexture)
        {
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

            TextureHandle cameraDepthTexture = CreateRenderGraphTexture(renderGraph, depthDescriptor, "_CameraDepthTexture", true);
            resources.SetTexture(UniversalResource.CameraDepthTexture, cameraDepthTexture);
        }

        void CreateMotionVectorTextures(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            var colorDesc = descriptor;
            colorDesc.graphicsFormat = MotionVectorRenderPass.k_TargetFormat; colorDesc.depthBufferBits = (int)DepthBits.None;
            colorDesc.msaaSamples = 1;  // Disable MSAA, consider a pixel resolve for half left velocity and half right velocity --> no velocity, which is untrue.
            TextureHandle motionVectorColor = CreateRenderGraphTexture(renderGraph, colorDesc, MotionVectorRenderPass.k_MotionVectorTextureName, true);
            resources.SetTexture(UniversalResource.MotionVectorColor, motionVectorColor);

            var depthDescriptor = descriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.None;
            //TODO RENDERGRAPH: in some cornercases (f.e. rendering to targetTexture) this is needed. maybe this will be unnece
            depthDescriptor.depthBufferBits = depthDescriptor.depthBufferBits != 0 ? depthDescriptor.depthBufferBits : 32; depthDescriptor.msaaSamples = 1;
            TextureHandle motionVectorDepth  = CreateRenderGraphTexture(renderGraph, depthDescriptor, MotionVectorRenderPass.k_MotionVectorDepthTextureName, true);
            resources.SetTexture(UniversalResource.MotionVectorDepth, motionVectorDepth);
        }

        void CreateCameraNormalsTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            var normalDescriptor = descriptor;
            normalDescriptor.depthBufferBits = 0;
            // Never have MSAA on this depth texture. When doing MSAA depth priming this is the texture that is resolved to and used for post-processing.
            normalDescriptor.msaaSamples = useDepthPriming ? descriptor.msaaSamples : 1;// Depth-Only passes don't use MSAA, unless depth priming is enabled
            // Find compatible render-target format for storing normals.
            // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
            // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
            var normalsName = this.renderingModeActual != RenderingMode.Deferred ? "_CameraNormalsTexture" : DeferredLights.k_GBufferNames[m_DeferredLights.GBufferNormalSmoothnessIndex];
            normalDescriptor.graphicsFormat = this.renderingModeActual != RenderingMode.Deferred ? DepthNormalOnlyPass.GetGraphicsFormat() : m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferNormalSmoothnessIndex);
            TextureHandle cameraNormalsTexture = CreateRenderGraphTexture(renderGraph, normalDescriptor, normalsName, true);
            resources.SetTexture(UniversalResource.CameraNormalsTexture, cameraNormalsTexture);
        }

        void CreateRenderingLayersTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            if (m_RequiresRenderingLayer)
            {
                m_RenderingLayersTextureName = "_CameraRenderingLayersTexture";

                // TODO RENDERGRAPH: deferred optimization
                if (renderingModeActual == RenderingMode.Deferred && m_DeferredLights.UseRenderingLayers)
                {
                    //RTHandle renderingLayersTexture = frameResources.gbuffer[(int)m_DeferredLights.GBufferRenderingLayers];
                    //m_RenderingLayersTextureName = ""; //renderingLayersTexture.name;
                    m_RenderingLayersTextureName = DeferredLights.k_GBufferNames[m_DeferredLights.GBufferRenderingLayers];
                }

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

                TextureHandle renderingLayersTexture = CreateRenderGraphTexture(renderGraph, renderingLayersDescriptor, m_RenderingLayersTextureName, true);
                resources.SetTexture(UniversalResource.RenderingLayersTexture, renderingLayersTexture);
            }
        }

        void CreateAfterPostProcessTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            var desc = PostProcessPass.GetCompatibleDescriptor(descriptor, descriptor.width, descriptor.height, UniversalRenderPipeline.MakeUnormRenderTextureGraphicsFormat(), DepthBits.None);
            TextureHandle afterPostProcessColor = CreateRenderGraphTexture(renderGraph, desc, "_AfterPostProcessTexture", true);
            resources.SetTexture(UniversalResource.AfterPostProcessColor, afterPostProcessColor);
        }

        void DepthNormalPrepassRender(RenderGraph renderGraph, RenderPassInputSummary renderPassInputs, TextureHandle depthTarget, ref RenderingData renderingData)
        {
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
                // renderingLayersTexture = frameResources.gbuffer[m_DeferredLights.GBufferRenderingLayers]; // GBUffer texture here
            }

            TextureHandle normalsTexture = resources.GetTexture(UniversalResource.CameraNormalsTexture);
            TextureHandle renderingLayersTexture = resources.GetTexture(UniversalResource.RenderingLayersTexture);
            m_DepthNormalPrepass.Render(renderGraph, normalsTexture, depthTarget, renderingLayersTexture, ref renderingData);

            if (m_RequiresRenderingLayer)
                SetRenderingLayersGlobalTextures(renderGraph);
        }
    }

    static class RenderGraphUtils
    {
        static private ProfilingSampler s_SetGlobalTextureProfilingSampler = new ProfilingSampler("Set Global Texture");

        internal const int DBufferSize = 3;
        private class PassData
        {
            internal TextureHandle texture;
            internal string name;
            internal int nameID;
        }

        internal static void SetGlobalTexture(RenderGraph graph, string name, TextureHandle texture, string passName = "Set Global Texture")
        {
            using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, s_SetGlobalTextureProfilingSampler))
            {
                passData.texture = builder.UseTexture(texture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.name = name;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(data.name, data.texture);
                });
            }
        }

        internal static void SetGlobalTexture(RenderGraph graph, int nameID, TextureHandle texture, string passName = "Set Global Texture")
        {
            using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, s_SetGlobalTextureProfilingSampler))
            {
                passData.texture = builder.UseTexture(texture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.nameID = nameID;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(data.nameID, data.texture);
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
            CameraData cameraData)
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
            Debug.Assert(colorHandle.IsValid() || depthHandle.IsValid(), "Trying to clear an invalid render target");

            using (var builder = graph.AddRasterRenderPass<PassData>("Clear Targets Pass", out var passData, s_ClearProfilingSampler))
            {
                passData.color = builder.UseTextureFragment(colorHandle, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.depth = builder.UseTextureFragmentDepth(depthHandle, IBaseRenderGraphBuilder.AccessFlags.Write);
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
