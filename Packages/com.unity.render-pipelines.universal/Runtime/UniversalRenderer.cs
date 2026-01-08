using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Rendering modes for Universal renderer.
    /// </summary>
    public enum RenderingMode
    {
        /// <summary>Render all objects and lighting in one pass, with a hard limit on the number of lights that can be applied on an object.</summary>
        Forward = 0,
        /// <summary>Render all objects and lighting in one pass using a clustered data structure to access lighting data.</summary>
        [InspectorName("Forward+")]
        ForwardPlus = 2,
        /// <summary>Render all objects first in a g-buffer pass, then apply all lighting in a separate pass using deferred shading.</summary>
        Deferred = 1,
        /// <summary>Render all objects first in a g-buffer pass, then apply all lighting in a separate pass using deferred shading. Use a clustered data structure to access lighting data where possible.</summary>
        [InspectorName("Deferred+")]
        DeferredPlus = 3,
    };

    /// <summary>
    /// When the Universal Renderer should use Depth Priming in Forward mode.
    /// </summary>
    public enum DepthPrimingMode
    {
        /// <summary>Depth Priming will never be used.</summary>
        Disabled,
        /// <summary>Depth Priming will only be used if there is a depth prepass needed by any of the render passes.</summary>
        Auto,
        /// <summary>A depth prepass will be explicitly requested so Depth Priming can be used.</summary>
        Forced,
    }

    /// <summary>
    /// Definition of stencil bits for stencil-based cross-fade LOD.
    /// </summary>
    [Flags]
    public enum UniversalRendererStencilRef
    {
        /// <summary>The first stencil bit for stencil-based cross-fade LOD.</summary>
        CrossFadeStencilRef_0 = 1 << 2,
        /// <summary>The second stencil bit for stencil-based cross-fade LOD.</summary>
        CrossFadeStencilRef_1 = 1 << 3,
        /// <summary>All stencil bits for stencil-based cross-fade LOD.</summary>
        CrossFadeStencilRef_All = CrossFadeStencilRef_0 + CrossFadeStencilRef_1,
    }

    /// <summary>
    /// Default renderer for Universal RP.
    /// This renderer is supported on all Universal RP supported platforms.
    /// It uses a classic forward rendering strategy with per-object light culling.
    /// </summary>
    public sealed partial class UniversalRenderer : ScriptableRenderer
    {
        const int k_FinalBlitPassQueueOffset = 1;
        const int k_AfterFinalBlitPassQueueOffset = k_FinalBlitPassQueueOffset + 1;

        /// <inheritdoc/>
        public override int SupportedCameraStackingTypes()
        {
            switch (m_RenderingMode)
            {
                case RenderingMode.Forward:
                case RenderingMode.ForwardPlus:
                    return 1 << (int)CameraRenderType.Base | 1 << (int)CameraRenderType.Overlay;
                case RenderingMode.Deferred:
                case RenderingMode.DeferredPlus:
                    return 1 << (int)CameraRenderType.Base;
                default:
                    return 0;
            }
        }

        /// <inheritdoc/>
        protected internal override bool SupportsMotionVectors()
        {
            // Motion vector pass for TAA and per-object motion blur (etc.) is available.
            return true;
        }

        /// <inheritdoc/>
        protected internal override bool SupportsCameraOpaque()
        {
            return true;
        }

        /// <inheritdoc/>
        protected internal override bool SupportsCameraNormals()
        {
            return true;
        }

        // Rendering mode setup from UI. The final rendering mode used can be different. See renderingModeActual.
        internal RenderingMode renderingModeRequested => m_RenderingMode;

        bool deferredModeUnsupported => GL.wireframe ||
                                              (DebugHandler != null && DebugHandler.IsActiveModeUnsupportedForDeferred) ||
                                              m_DeferredLights == null ||
                                              !m_DeferredLights.IsRuntimeSupportedThisFrame();

        // Actual rendering mode, which may be different (ex: wireframe rendering, hardware not capable of deferred rendering).
        internal RenderingMode renderingModeActual  {
            get
            {
                switch (renderingModeRequested)
                {
                    case RenderingMode.Deferred:
                        return deferredModeUnsupported ? RenderingMode.Forward : RenderingMode.Deferred;

                    case RenderingMode.DeferredPlus:
                        return deferredModeUnsupported ? RenderingMode.ForwardPlus : RenderingMode.DeferredPlus;

                    case RenderingMode.Forward:
                    case RenderingMode.ForwardPlus:
                    default:
                        return renderingModeRequested;
                }
            }
        }

        internal bool usesDeferredLighting => renderingModeActual == RenderingMode.Deferred || renderingModeActual == RenderingMode.DeferredPlus;

        internal bool usesClusterLightLoop => renderingModeActual == RenderingMode.ForwardPlus || renderingModeActual == RenderingMode.DeferredPlus;

        internal bool accurateGbufferNormals => m_DeferredLights != null ? m_DeferredLights.AccurateGbufferNormals : false;

#if ENABLE_ADAPTIVE_PERFORMANCE
        internal bool needTransparencyPass { get { return (UniversalRenderPipeline.asset?.useAdaptivePerformance == false) || !AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipTransparentObjects;; } }
#endif
        /// <summary>Property to control the depth priming behavior of the forward rendering path.</summary>
        public DepthPrimingMode depthPrimingMode { get { return m_DepthPrimingMode; } set { m_DepthPrimingMode = value; } }

        DepthOnlyPass m_DepthPrepass;
        DepthNormalOnlyPass m_DepthNormalPrepass;
        MotionVectorRenderPass m_MotionVectorPass;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        GBufferPass m_GBufferPass;
        DeferredPass m_DeferredPass;
        DrawObjectsPass m_RenderOpaqueForwardOnlyPass;
        DrawObjectsPass m_RenderOpaqueForwardPass;
        DrawObjectsWithRenderingLayersPass m_RenderOpaqueForwardWithRenderingLayersPass;
        DrawSkyboxPass m_DrawSkyboxPass;
        CopyDepthPass m_CopyDepthPass;
        CopyColorPass m_CopyColorPass;
        TransparentSettingsPass m_TransparentSettingsPass;
        DrawObjectsPass m_RenderTransparentForwardPass;
        InvokeOnRenderObjectCallbackPass m_OnRenderObjectCallbackPass;
        FinalBlitPass m_FinalBlitPass;
        FinalBlitPass m_OffscreenUICoverPrepass;
        CapturePass m_CapturePass;
#if ENABLE_VR && ENABLE_XR_MODULE
        XROcclusionMeshPass m_XROcclusionMeshPass;
        CopyDepthPass m_XRCopyDepthPass;
        XRDepthMotionPass m_XRDepthMotionPass;
#endif
#if UNITY_EDITOR
        CopyDepthPass m_FinalDepthCopyPass;
        ProbeVolumeDebugPass m_ProbeVolumeDebugPass;
#endif
        DrawScreenSpaceUIPass m_DrawOffscreenUIPass;
        DrawScreenSpaceUIPass m_DrawOverlayUIPass;

        CopyColorPass m_HistoryRawColorCopyPass;
        CopyDepthPass m_HistoryRawDepthCopyPass;

        StencilCrossFadeRenderPass m_StencilCrossFadeRenderPass;

        RTHandle m_TargetColorHandle;
        RTHandle m_TargetDepthHandle;

        ForwardLights m_ForwardLights;
        DeferredLights m_DeferredLights;
        RenderingMode m_RenderingMode;
        DepthPrimingMode m_DepthPrimingMode;
        CopyDepthMode m_CopyDepthMode;
        DepthFormat m_CameraDepthAttachmentFormat;
        DepthFormat m_CameraDepthTextureFormat;
        StencilState m_DefaultStencilState;
        LightCookieManager m_LightCookieManager;
        IntermediateTextureMode m_IntermediateTextureMode;

        // Materials used in URP Scriptable Render Passes
        Material m_BlitMaterial = null;
        Material m_BlitHDRMaterial = null;
        Material m_SamplingMaterial = null;
        Material m_BlitOffscreenUICoverMaterial = null;
        Material m_StencilDeferredMaterial = null;
        Material m_ClusterDeferredMaterial = null;
        Material m_CameraMotionVecMaterial = null;

        internal bool isPostProcessActive { get => m_PostProcess != null; }

        internal DeferredLights deferredLights { get => m_DeferredLights; }
        internal LayerMask prepassLayerMask { get; set; }
        internal LayerMask opaqueLayerMask { get; set; }
        internal LayerMask transparentLayerMask { get; set; }
        internal bool shadowTransparentReceive { get; set; }
        internal bool onTileValidation { get; set; }

        internal GraphicsFormat cameraDepthTextureFormat { get => (m_CameraDepthTextureFormat != DepthFormat.Default) ? (GraphicsFormat)m_CameraDepthTextureFormat : CoreUtils.GetDefaultDepthStencilFormat(); }
        internal GraphicsFormat cameraDepthAttachmentFormat { get => (m_CameraDepthAttachmentFormat != DepthFormat.Default) ? (GraphicsFormat)m_CameraDepthAttachmentFormat : CoreUtils.GetDefaultDepthStencilFormat(); }

        /// <summary>
        /// Constructor for the Universal Renderer.
        /// </summary>
        /// <param name="data">The settings to create the renderer with.</param>
        public UniversalRenderer(UniversalRendererData data) : base(data)
        {
            // Query and cache runtime platform info first before setting up URP.
            PlatformAutoDetect.Initialize();

#if ENABLE_VR && ENABLE_XR_MODULE
            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineRuntimeXRResources>(out var xrResources))
            {
                Experimental.Rendering.XRSystem.Initialize(XRPassUniversal.Create, xrResources.xrOcclusionMeshPS, xrResources.xrMirrorViewPS);
                m_XRDepthMotionPass = new XRDepthMotionPass(RenderPassEvent.BeforeRenderingPrePasses, xrResources.xrMotionVector);
            }
#endif
            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineRuntimeShaders>(
                    out var shadersResources))
            {
                m_BlitMaterial = CoreUtils.CreateEngineMaterial(shadersResources.coreBlitPS);
                m_BlitHDRMaterial = CoreUtils.CreateEngineMaterial(shadersResources.blitHDROverlay);
                m_SamplingMaterial = CoreUtils.CreateEngineMaterial(shadersResources.samplingPS);
                // Share viewport for all the cameras.
                m_BlitOffscreenUICoverMaterial = CoreUtils.CreateEngineMaterial(shadersResources.blitHDROverlay);
            }

            Shader copyDephPS = null;
            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRendererResources>(
                    out var universalRendererShaders))
            {
                copyDephPS = universalRendererShaders.copyDepthPS;
                m_StencilDeferredMaterial = CoreUtils.CreateEngineMaterial(universalRendererShaders.stencilDeferredPS);
                m_ClusterDeferredMaterial = CoreUtils.CreateEngineMaterial(universalRendererShaders.clusterDeferred);
                m_CameraMotionVecMaterial = CoreUtils.CreateEngineMaterial(universalRendererShaders.cameraMotionVector);

                m_StencilCrossFadeRenderPass = new StencilCrossFadeRenderPass(universalRendererShaders.stencilDitherMaskSeedPS);
            }

            StencilStateData stencilData = data.defaultStencilState;
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

            m_IntermediateTextureMode = data.intermediateTextureMode;
            prepassLayerMask = data.prepassLayerMask;
            opaqueLayerMask = data.opaqueLayerMask;
            transparentLayerMask = data.transparentLayerMask;
            shadowTransparentReceive = data.shadowTransparentReceive;

            onTileValidation = data.onTileValidation;

            var asset = UniversalRenderPipeline.asset;
            if (asset != null && asset.supportsLightCookies)
            {
                var settings = LightCookieManager.Settings.Create();
                if (asset)
                {
                    settings.atlas.format = asset.additionalLightsCookieFormat;
                    settings.atlas.resolution = asset.additionalLightsCookieResolution;
                }

                m_LightCookieManager = new LightCookieManager(ref settings);
            }

            this.stripShadowsOffVariants = data.stripShadowsOffVariants;
            this.stripAdditionalLightOffVariants = data.stripAdditionalLightOffVariants;
#if ENABLE_VR && ENABLE_XR_MODULE
#if PLATFORM_WINRT || PLATFORM_ANDROID
            // AdditionalLightOff variant is available on HL&Quest platform due to performance consideration.
            this.stripAdditionalLightOffVariants = !PlatformAutoDetect.isXRMobile;
#endif
#endif

            ForwardLights.InitParams forwardInitParams;
            forwardInitParams.lightCookieManager = m_LightCookieManager;
            forwardInitParams.forwardPlus = data.renderingMode == RenderingMode.DeferredPlus || data.renderingMode == RenderingMode.ForwardPlus;
            m_ForwardLights = new ForwardLights(forwardInitParams);
            //m_DeferredLights.LightCulling = data.lightCulling;
            this.m_RenderingMode = data.renderingMode;
            this.m_DepthPrimingMode = data.depthPrimingMode;
            this.m_CopyDepthMode = data.copyDepthMode;
            this.m_CameraDepthAttachmentFormat = data.depthAttachmentFormat;
            this.m_CameraDepthTextureFormat = data.depthTextureFormat;

            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);

#if ENABLE_VR && ENABLE_XR_MODULE
            m_XROcclusionMeshPass = new XROcclusionMeshPass(RenderPassEvent.BeforeRenderingOpaques);
            // Schedule XR copydepth right after m_FinalBlitPass
            m_XRCopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRendering + k_AfterFinalBlitPassQueueOffset, copyDephPS);
#endif
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, prepassLayerMask);
            m_DepthNormalPrepass = new DepthNormalOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, prepassLayerMask);
            if (renderingModeRequested == RenderingMode.Deferred || renderingModeRequested == RenderingMode.DeferredPlus)
            {
                var deferredInitParams = new DeferredLights.InitParams();
                deferredInitParams.stencilDeferredMaterial = m_StencilDeferredMaterial;
                deferredInitParams.clusterDeferredMaterial = m_ClusterDeferredMaterial;
                deferredInitParams.lightCookieManager = m_LightCookieManager;
                deferredInitParams.deferredPlus = renderingModeRequested == RenderingMode.DeferredPlus;
                m_DeferredLights = new DeferredLights(deferredInitParams);
                m_DeferredLights.AccurateGbufferNormals = data.accurateGbufferNormals;

                m_GBufferPass = new GBufferPass(RenderPassEvent.BeforeRenderingGbuffer, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference, m_DeferredLights);
                // Forward-only pass only runs if deferred renderer is enabled.
                // It allows specific materials to be rendered in a forward-like pass.
                // We render both gbuffer pass and forward-only pass before the deferred lighting pass so we can minimize copies of depth buffer and
                // benefits from some depth rejection.
                // - If a material can be rendered either forward or deferred, then it should declare a UniversalForward and a UniversalGBuffer pass.
                // - If a material cannot be lit in deferred (unlit, bakedLit, special material such as hair, skin shader), then it should declare UniversalForwardOnly pass
                // - Legacy materials have unamed pass, which is implicitely renamed as SRPDefaultUnlit. In that case, they are considered forward-only too.
                // TO declare a material with unnamed pass and UniversalForward/UniversalForwardOnly pass is an ERROR, as the material will be rendered twice.
                StencilState forwardOnlyStencilState = DeferredLights.OverwriteStencil(m_DefaultStencilState, (int)StencilUsage.MaterialMask);
                ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[]
                {
                    new ShaderTagId("UniversalForwardOnly"),
                    new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                    new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                };
                int forwardOnlyStencilRef = stencilData.stencilReference | (int)StencilUsage.MaterialUnlit;
                m_DeferredPass = new DeferredPass(RenderPassEvent.BeforeRenderingDeferredLights, m_DeferredLights);
                m_RenderOpaqueForwardOnlyPass = new DrawObjectsPass("Draw Opaques Forward Only", forwardOnlyShaderTagIds, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, forwardOnlyStencilState, forwardOnlyStencilRef);
            }

            // Always create this pass even in deferred because we use it for wireframe rendering in the Editor or offscreen depth texture rendering.
            m_RenderOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawOpaqueObjects, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_RenderOpaqueForwardWithRenderingLayersPass = new DrawObjectsWithRenderingLayersPass(URPProfileId.DrawOpaqueObjects, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);

            //This is calculated later for RG, so dummy value.
            RenderPassEvent copyDepthEvent = RenderPassEvent.AfterRenderingSkybox;

            m_CopyDepthPass = new CopyDepthPass(
                copyDepthEvent,
                copyDephPS,
                shouldClear: true);

            // Motion vectors depend on the (copy) depth texture. Depth is reprojected to calculate motion vectors.
            m_MotionVectorPass = new MotionVectorRenderPass(copyDepthEvent + 1, m_CameraMotionVecMaterial, data.opaqueLayerMask);

            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
            m_CopyColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, m_SamplingMaterial, m_BlitMaterial);
#if ENABLE_ADAPTIVE_PERFORMANCE
            if (needTransparencyPass)
#endif
            {
                m_TransparentSettingsPass = new TransparentSettingsPass(RenderPassEvent.BeforeRenderingTransparents, data.shadowTransparentReceive);
                m_RenderTransparentForwardPass = new DrawObjectsPass(URPProfileId.DrawTransparentObjects, false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, data.transparentLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            }
            m_OnRenderObjectCallbackPass = new InvokeOnRenderObjectCallbackPass(RenderPassEvent.BeforeRenderingPostProcessing);

            // History generation passes for "raw color/depth". These execute only if explicitly requested by users.
            // VFX system particles uses these. See RawColorHistory.cs.
            m_HistoryRawColorCopyPass = new CopyColorPass(RenderPassEvent.BeforeRenderingPostProcessing, m_SamplingMaterial, m_BlitMaterial, customPassName: "Copy Color Raw History");
            m_HistoryRawDepthCopyPass = new CopyDepthPass(RenderPassEvent.BeforeRenderingPostProcessing, copyDephPS, false, customPassName: "Copy Depth Raw History");

            m_DrawOffscreenUIPass = new DrawScreenSpaceUIPass(RenderPassEvent.BeforeRenderingPostProcessing);
            m_DrawOverlayUIPass = new DrawScreenSpaceUIPass(RenderPassEvent.AfterRendering + k_AfterFinalBlitPassQueueOffset); // after m_FinalBlitPass

            //No postProcessData means that post processes are disabled
            if (data.postProcessData != null)
            {
                m_PostProcess = new PostProcess(data.postProcessData);
                m_ColorGradingLutPassRenderGraph = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, data.postProcessData);
            }

            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + k_FinalBlitPassQueueOffset, m_BlitMaterial, m_BlitHDRMaterial);
            m_OffscreenUICoverPrepass = new FinalBlitPass(RenderPassEvent.BeforeRenderingPostProcessing + k_FinalBlitPassQueueOffset, m_BlitMaterial, m_BlitOffscreenUICoverMaterial);

#if UNITY_EDITOR
            m_FinalDepthCopyPass = new CopyDepthPass(RenderPassEvent.AfterRendering + 9, copyDephPS, false, customPassName: "Copy Final Depth");
            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineDebugShaders>(out var debugShaders))
                m_ProbeVolumeDebugPass = new ProbeVolumeDebugPass(RenderPassEvent.BeforeRenderingTransparents, debugShaders.probeVolumeSamplingDebugComputeShader);
#endif

            supportedRenderingFeatures = new RenderingFeatures();

            if (renderingModeRequested is RenderingMode.Deferred or RenderingMode.DeferredPlus)
            {
                // Deferred rendering does not support MSAA.
                // if On-Tile Validation is enabled, Deferred(+) will fallback to Forward(+). Thus we must not fix msaa value in this case.
                if (!onTileValidation)
                    supportedRenderingFeatures.msaa = false;
            }

            LensFlareCommonSRP.mergeNeeded = 0;
            LensFlareCommonSRP.maxLensFlareWithOcclusionTemporalSample = 1;
            LensFlareCommonSRP.Initialize();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            m_ForwardLights.Cleanup();

            m_FinalBlitPass?.Dispose();
            m_OffscreenUICoverPrepass?.Dispose();
            m_DrawOffscreenUIPass?.Dispose();
            m_DrawOverlayUIPass?.Dispose();

            m_CopyDepthPass?.Dispose();
#if UNITY_EDITOR
            m_FinalDepthCopyPass?.Dispose();
#endif
            m_HistoryRawDepthCopyPass?.Dispose();

#if ENABLE_VR && ENABLE_XR_MODULE
            m_XRCopyDepthPass?.Dispose();
            m_XRDepthMotionPass?.Dispose();
#endif

            m_StencilCrossFadeRenderPass?.Dispose();

            // RG
            m_PostProcess?.Dispose();
            m_ColorGradingLutPassRenderGraph?.Cleanup();

            m_TargetColorHandle?.Release();
            m_TargetDepthHandle?.Release();
            ReleaseRenderTargets();

            base.Dispose(disposing);
            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_BlitHDRMaterial);
            CoreUtils.Destroy(m_BlitOffscreenUICoverMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);
            CoreUtils.Destroy(m_StencilDeferredMaterial);
            CoreUtils.Destroy(m_ClusterDeferredMaterial);
            CoreUtils.Destroy(m_CameraMotionVecMaterial);

            CleanupRenderGraphResources();

            LensFlareCommonSRP.Dispose();

#if ENABLE_VR && ENABLE_XR_MODULE
            XRSystem.Dispose();
#endif
        }

        internal override void ReleaseRenderTargets()
        {
            m_MainLightShadowCasterPass?.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();
        }

        /// <summary>
        /// Returns if the camera renders to a offscreen depth texture.
        /// </summary>
        /// <param name="cameraData">The camera data for the camera being rendered.</param>
        /// <returns>Returns true if the camera renders to depth without any color buffer. It will return false otherwise.</returns>
        public static bool IsOffscreenDepthTexture(ref CameraData cameraData) => IsOffscreenDepthTexture(cameraData.universalCameraData);

        /// <summary>
        /// Returns if the camera renders to a offscreen depth texture.
        /// </summary>
        /// <param name="cameraData">The camera data for the camera being rendered.</param>
        /// <returns>Returns true if the camera renders to depth without any color buffer. It will return false otherwise.</returns>
        public static bool IsOffscreenDepthTexture(UniversalCameraData cameraData) => cameraData.targetTexture != null && cameraData.targetTexture.format == RenderTextureFormat.Depth;

        static bool IsWebGL()
        {
            // Both WebGL and WebGPU have issues with depth priming on Apple Arm64
#if PLATFORM_WEBGL
            return true;
#else
            return false;
#endif
        }

        static bool IsGLESDevice()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
        }

        static bool IsGLDevice()
        {
            return IsGLESDevice() || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        }

        static bool HasActiveRenderFeatures(List<ScriptableRendererFeature> rendererFeatures)
        {
            if (rendererFeatures.Count == 0)
                return false;

            foreach (var rf in rendererFeatures)
            {
                if (rf.isActive)
                    return true;
            }

            return false;
        }

        static bool HasPassesRequiringIntermediateTexture(List<ScriptableRenderPass> activeRenderPassQueue)
        {
            if (activeRenderPassQueue.Count == 0)
                return false;

            foreach (var pass in activeRenderPassQueue)
            {
                if (pass.requiresIntermediateTexture)
                    return true;
            }

            return false;
        }

        static void SetupVFXCameraBuffer(UniversalCameraData cameraData)
        {
            if (cameraData != null && cameraData.historyManager != null)
            {
                var vfxBufferNeeded = VFX.VFXManager.IsCameraBufferNeeded(cameraData.camera);
                if (vfxBufferNeeded.HasFlag(VFX.VFXCameraBufferTypes.Color))
                {
                    cameraData.historyManager.RequestAccess<RawColorHistory>();

                    var handle = cameraData.historyManager.GetHistoryForRead<RawColorHistory>()?.GetCurrentTexture();
                    VFX.VFXManager.SetCameraBuffer(cameraData.camera, VFX.VFXCameraBufferTypes.Color, handle, 0, 0,
                        (int)(cameraData.pixelWidth * cameraData.renderScale), (int)(cameraData.pixelHeight * cameraData.renderScale));
                }

                if (vfxBufferNeeded.HasFlag(VFX.VFXCameraBufferTypes.Depth))
                {
                    cameraData.historyManager.RequestAccess<RawDepthHistory>();

                    var handle = cameraData.historyManager.GetHistoryForRead<RawDepthHistory>()?.GetCurrentTexture();
                    VFX.VFXManager.SetCameraBuffer(cameraData.camera, VFX.VFXCameraBufferTypes.Depth, handle, 0, 0,
                        (int)(cameraData.pixelWidth * cameraData.renderScale), (int)(cameraData.pixelHeight * cameraData.renderScale));
                }
            }
        }

        /// <inheritdoc />
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {
            // TODO: PerObjectCulling also affect reflection probes. Enabling it for now.
            // if (asset.additionalLightsRenderingMode == LightRenderingMode.Disabled ||
            //     asset.maxAdditionalLightsCount == 0)
            bool usesReflectionProbeAtlas = UniversalRenderPipeline.asset.ShouldUseReflectionProbeAtlasBlending(renderingModeActual);

            if (usesClusterLightLoop && usesReflectionProbeAtlas)
            {
                cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            }

            // We disable shadow casters if both shadow casting modes are turned off
            // or the shadow distance has been turned down to zero
            bool isShadowCastingDisabled = !UniversalRenderPipeline.asset.supportsMainLightShadows && !UniversalRenderPipeline.asset.supportsAdditionalLightShadows;
            bool isShadowDistanceZero = Mathf.Approximately(cameraData.maxShadowDistance, 0.0f);
            if (isShadowCastingDisabled || isShadowDistanceZero)
            {
                cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;
            }

            if (usesClusterLightLoop)
            {
                // We don't add one to the maximum light because mainlight is treated as any other light.
                cullingParameters.maximumVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                // Do not sort reflection probe from engine it will come in reverse order from what we need.
                cullingParameters.reflectionProbeSortingCriteria = ReflectionProbeSortingCriteria.None;
            }
            else if (renderingModeActual == RenderingMode.Deferred)
            {
                cullingParameters.maximumVisibleLights = 0xFFFF;
            }
            else
            {
                // We set the number of maximum visible lights allowed and we add one for the mainlight...
                //
                // Note: However ScriptableRenderContext.Cull() does not differentiate between light types.
                //       If there is no active main light in the scene, ScriptableRenderContext.Cull() might return  ( cullingParameters.maximumVisibleLights )  visible additional lights.
                //       i.e ScriptableRenderContext.Cull() might return  ( UniversalRenderPipeline.maxVisibleAdditionalLights + 1 )  visible additional lights !
                cullingParameters.maximumVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights + 1;
            }
            cullingParameters.shadowDistance = cameraData.maxShadowDistance;

            cullingParameters.conservativeEnclosingSphere = UniversalRenderPipeline.asset.conservativeEnclosingSphere;

            cullingParameters.numIterationsEnclosingSphere = UniversalRenderPipeline.asset.numIterationsEnclosingSphere;
        }

        /// <inheritdoc />
        public override void FinishRendering(CommandBuffer cmd)
        {
        }

        struct RenderPassInputSummary
        {
            internal bool requiresDepthTexture;
            internal bool requiresNormalsTexture;
            internal bool requiresColorTexture;
            internal bool requiresMotionVectors;
            internal RenderPassEvent requiresNormalTextureEarliestEvent;
            internal RenderPassEvent requiresDepthTextureEarliestEvent;
        }

        static RenderPassInputSummary GetRenderPassInputs(List<ScriptableRenderPass> activeRenderPassQueue)
        {
            RenderPassInputSummary inputSummary = new RenderPassInputSummary
            {
                requiresNormalTextureEarliestEvent = RenderPassEvent.AfterRenderingPostProcessing,
                requiresDepthTextureEarliestEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
            for (int i = 0; i < activeRenderPassQueue.Count; ++i)
            {
                ScriptableRenderPass pass = activeRenderPassQueue[i];
                bool needsDepth     = (pass.input & ScriptableRenderPassInput.Depth) != ScriptableRenderPassInput.None;
                bool needsNormals   = (pass.input & ScriptableRenderPassInput.Normal) != ScriptableRenderPassInput.None;
                bool needsColor     = (pass.input & ScriptableRenderPassInput.Color) != ScriptableRenderPassInput.None;
                bool needsMotion    = (pass.input & ScriptableRenderPassInput.Motion) != ScriptableRenderPassInput.None;

                inputSummary.requiresDepthTexture   |= needsDepth;
                inputSummary.requiresNormalsTexture |= needsNormals;
                inputSummary.requiresColorTexture   |= needsColor;
                inputSummary.requiresMotionVectors  |= needsMotion;

                if (needsDepth)
                    inputSummary.requiresDepthTextureEarliestEvent = (RenderPassEvent)Mathf.Min((int)pass.renderPassEvent, (int)inputSummary.requiresDepthTextureEarliestEvent);
                if (needsNormals)
                    inputSummary.requiresNormalTextureEarliestEvent = (RenderPassEvent)Mathf.Min((int)pass.renderPassEvent, (int)inputSummary.requiresNormalTextureEarliestEvent);
            }
            return inputSummary;
        }

        void AddRequirementsOfInternalFeatures(ref RenderPassInputSummary inputSummary, UniversalCameraData cameraData, bool postProcessingEnabled, bool renderingLayerProvidesByDepthNormalPass, MotionVectorRenderPass motionVectorPass, CopyDepthMode copyDepthMode)
        {
            // TAA in postprocess requires it to function.
            if (cameraData.IsTemporalAAEnabled() )
                inputSummary.requiresMotionVectors = true;

            if(cameraData.requiresDepthTexture)
            {
                inputSummary.requiresDepthTexture = true;

                RenderPassEvent earliestDepth = RenderPassEvent.AfterRenderingTransparents;
                switch (copyDepthMode){
                    case CopyDepthMode.ForcePrepass:
                        earliestDepth = RenderPassEvent.AfterRenderingPrePasses;
                        break;
                    case CopyDepthMode.AfterOpaques:
                        earliestDepth = RenderPassEvent.AfterRenderingOpaques;
                        break;
                }

                inputSummary.requiresDepthTextureEarliestEvent = (RenderPassEvent)Mathf.Min((int)earliestDepth, (int)inputSummary.requiresDepthTextureEarliestEvent);
            }

            inputSummary.requiresColorTexture |= cameraData.requiresOpaqueTexture;

            // Object motion blur requires motion vectors.
            if (postProcessingEnabled)
            {
                var motionBlur = VolumeManager.instance.stack.GetComponent<MotionBlur>();
                if (motionBlur != null && motionBlur.IsActive() && motionBlur.mode.value == MotionBlurMode.CameraAndObjects)
                    inputSummary.requiresMotionVectors = true;

                if (cameraData.postProcessingRequiresDepthTexture)
                {
                    inputSummary.requiresDepthTexture = true;
                    inputSummary.requiresDepthTextureEarliestEvent = (RenderPassEvent)Mathf.Min( (int)RenderPassEvent.BeforeRenderingPostProcessing, (int)inputSummary.requiresDepthTextureEarliestEvent);
                }

            }

            // Motion vectors imply depth
            if (inputSummary.requiresMotionVectors)
            {
                inputSummary.requiresDepthTexture = true;
                inputSummary.requiresDepthTextureEarliestEvent = (RenderPassEvent)Mathf.Min((int)motionVectorPass.renderPassEvent, (int)inputSummary.requiresDepthTextureEarliestEvent);
            }

#if UNITY_EDITOR
            if (ProbeReferenceVolume.instance.IsProbeSamplingDebugEnabled() && cameraData.isSceneViewCamera)
                inputSummary.requiresNormalsTexture = true;
#endif

            if (renderingLayerProvidesByDepthNormalPass)
                inputSummary.requiresNormalsTexture = true;
        }

        internal static bool PlatformRequiresExplicitMsaaResolve()
        {
#if UNITY_EDITOR
            // In the editor play-mode we use a Game View Render Texture, with
            // samples count forced to 1 so we always need to do an explicit MSAA resolve.
            return true;
#else
            // On Metal/iOS the MSAA resolve is done implicitly as part of the renderpass, so we do not need an extra intermediate pass for the explicit autoresolve.
            // Note: On Vulkan Standalone, despite SystemInfo.supportsMultisampleAutoResolve being true, the backbuffer has only 1 sample, so we still require
            // the explicit resolve on non-mobile platforms with supportsMultisampleAutoResolve.
            return !(SystemInfo.supportsMultisampleAutoResolve && Application.isMobilePlatform)
                && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal;
#endif
        }

        /// <summary>
        /// Checks if the pipeline needs to create a intermediate render texture.
        /// </summary>
        /// <param name="cameraData">CameraData contains all relevant render target information for the camera.</param>
        /// <seealso cref="CameraData"/>
        /// <returns>Return true if pipeline needs to render to a intermediate render texture.</returns>
        static bool RequiresIntermediateColorTexture(UniversalCameraData cameraData, in RenderPassInputSummary renderPassInputs, bool usesDeferredLighting, bool applyPostProcessing)
        {
            // When rendering a camera stack we always create an intermediate render texture to composite camera results.
            // We create it upon rendering the Base camera.
            if (cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget)
                return true;

            // Always force rendering into intermediate color texture if deferred rendering mode is selected.
            // Reason: without intermediate color texture, the target camera texture is y-flipped.
            // However, the target camera texture is bound during gbuffer pass and deferred pass.
            // Gbuffer pass will not be y-flipped because it is MRT (see ScriptableRenderContext implementation),
            // while deferred pass will be y-flipped, which breaks rendering.
            // This incurs an extra blit into at the end of rendering.
            if (usesDeferredLighting)
                return true;

            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            int msaaSamples = cameraTargetDescriptor.msaaSamples;
            bool isScaledRender = cameraData.imageScalingMode != ImageScalingMode.None;
            bool isScalableBufferManagerUsed = IsScalableBufferManagerUsed(cameraData);
            bool isCompatibleBackbufferTextureDimension = cameraTargetDescriptor.dimension == TextureDimension.Tex2D;
            bool requiresExplicitMsaaResolve = msaaSamples > 1 && PlatformRequiresExplicitMsaaResolve();
            bool isOffscreenRender = cameraData.targetTexture != null && !isSceneViewCamera;
            bool isCapturing = cameraData.captureActions != null;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                isScaledRender = false;
                isScalableBufferManagerUsed = false;
                isCompatibleBackbufferTextureDimension = cameraData.xr.renderTargetDesc.dimension == cameraTargetDescriptor.dimension;
            }
#endif

            bool requestedColorHistory = (cameraData.historyManager ==null)? false : cameraData.historyManager.IsAccessRequested<RawColorHistory>();

            bool requiresBlitForOffscreenCamera = applyPostProcessing || renderPassInputs.requiresColorTexture || requiresExplicitMsaaResolve || !cameraData.isDefaultViewport || requestedColorHistory;

            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || isScaledRender || isScalableBufferManagerUsed || cameraData.isHdrEnabled ||
                !isCompatibleBackbufferTextureDimension || isCapturing || cameraData.requireSrgbConversion;
        }

        // There is two ways to control the dynamic resolution in URP:
        // - By using the ScalableBufferManager API (https://docs.unity3d.com/2022.2/Documentation/Manual/DynamicResolution.html).
        // - By using the cameraData.renderScale property on the URP asset.
        // When checking the requirements to use an intermediate texture, we only consider the cameraData.renderScale property and not the ScalableBufferManager API.
        // When Dynamic Resolution is enabled on the camera and a scale factor (from ScalableBufferManager) is different than 1, we need to use an intermediate texture.
        // Note: cameraData.renderScale resizes screen space textures, while dynamic resolution (ScalableBufferManager) doesn't and instead uses memory aliasing.
        // These features are different and should work independently, though they can be used together at the same time.
        static bool IsScalableBufferManagerUsed(UniversalCameraData cameraData)
        {
            const float epsilon = 0.0001f;

            bool dynamicResEnabled = cameraData.camera.allowDynamicResolution;
            bool scaledWidthActive = Mathf.Abs(ScalableBufferManager.widthScaleFactor - 1.0f) > epsilon;
            bool scaledHeightActive = Mathf.Abs(ScalableBufferManager.heightScaleFactor - 1.0f) > epsilon;

            return dynamicResEnabled && (scaledWidthActive || scaledHeightActive);
        }

        static bool CanCopyDepth(UniversalCameraData cameraData)
        {
            bool msaaEnabledForCamera = cameraData.cameraTargetDescriptor.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;

            // copying MSAA depth on GLES3 is giving invalid results. This won't be fixed by the driver team because it would introduce performance issues (more info in the Fogbugz issue 1339401 comments)
            if (IsGLESDevice() && msaaDepthResolve)
                return false;

            return supportsDepthCopy || msaaDepthResolve;
        }
    }
}
