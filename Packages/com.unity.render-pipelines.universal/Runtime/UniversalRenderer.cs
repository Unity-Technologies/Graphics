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
        Deferred = 1
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
    /// Default renderer for Universal RP.
    /// This renderer is supported on all Universal RP supported platforms.
    /// It uses a classic forward rendering strategy with per-object light culling.
    /// </summary>
    public sealed partial class UniversalRenderer : ScriptableRenderer
    {
        #if UNITY_SWITCH || UNITY_ANDROID
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
        const int k_DepthBufferBits = 24;
        #else
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
        const int k_DepthBufferBits = 32;
        #endif

        const int k_FinalBlitPassQueueOffset = 1;
        const int k_AfterFinalBlitPassQueueOffset = k_FinalBlitPassQueueOffset + 1;

        static readonly List<ShaderTagId> k_DepthNormalsOnly = new List<ShaderTagId> { new ShaderTagId("DepthNormalsOnly") };

        private static class Profiling
        {
            private const string k_Name = nameof(UniversalRenderer);
            public static readonly ProfilingSampler createCameraRenderTarget = new ProfilingSampler($"{k_Name}.{nameof(CreateCameraRenderTarget)}");
        }

        /// <inheritdoc/>
        public override int SupportedCameraStackingTypes()
        {
            switch (m_RenderingMode)
            {
                case RenderingMode.Forward:
                case RenderingMode.ForwardPlus:
                    return 1 << (int)CameraRenderType.Base | 1 << (int)CameraRenderType.Overlay;
                case RenderingMode.Deferred:
                    return 1 << (int)CameraRenderType.Base;
                default:
                    return 0;
            }
        }

        // Rendering mode setup from UI. The final rendering mode used can be different. See renderingModeActual.
        internal RenderingMode renderingModeRequested => m_RenderingMode;

        // Actual rendering mode, which may be different (ex: wireframe rendering, hardware not capable of deferred rendering).
        internal RenderingMode renderingModeActual => renderingModeRequested == RenderingMode.Deferred && (GL.wireframe || (DebugHandler != null && DebugHandler.IsActiveModeUnsupportedForDeferred) || m_DeferredLights == null || !m_DeferredLights.IsRuntimeSupportedThisFrame() || m_DeferredLights.IsOverlay)
        ? RenderingMode.Forward
        : this.renderingModeRequested;

        bool m_Clustering;

        internal bool accurateGbufferNormals => m_DeferredLights != null ? m_DeferredLights.AccurateGbufferNormals : false;

#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
        internal bool needTransparencyPass { get { return !UniversalRenderPipeline.asset.useAdaptivePerformance || !AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipTransparentObjects;; } }
#endif
        /// <summary>Property to control the depth priming behavior of the forward rendering path.</summary>
        public DepthPrimingMode depthPrimingMode { get { return m_DepthPrimingMode; } set { m_DepthPrimingMode = value; } }
        DepthOnlyPass m_DepthPrepass;
        DepthNormalOnlyPass m_DepthNormalPrepass;
        CopyDepthPass m_PrimedDepthCopyPass;
        MotionVectorRenderPass m_MotionVectorPass;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        GBufferPass m_GBufferPass;
        CopyDepthPass m_GBufferCopyDepthPass;
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
        CapturePass m_CapturePass;
#if ENABLE_VR && ENABLE_XR_MODULE
        XROcclusionMeshPass m_XROcclusionMeshPass;
        CopyDepthPass m_XRCopyDepthPass;
#endif
#if UNITY_EDITOR
        CopyDepthPass m_FinalDepthCopyPass;
#endif
        DrawScreenSpaceUIPass m_DrawOffscreenUIPass;
        DrawScreenSpaceUIPass m_DrawOverlayUIPass;

        internal RenderTargetBufferSystem m_ColorBufferSystem;

        internal RTHandle m_ActiveCameraColorAttachment;
        RTHandle m_ColorFrontBuffer;
        internal RTHandle m_ActiveCameraDepthAttachment;
        internal RTHandle m_CameraDepthAttachment;
        RTHandle m_XRTargetHandleAlias;
        internal RTHandle m_DepthTexture;
        RTHandle m_NormalsTexture;
        RTHandle m_DecalLayersTexture;
        RTHandle m_OpaqueColor;
        RTHandle m_MotionVectorColor;
        RTHandle m_MotionVectorDepth;

        ForwardLights m_ForwardLights;
        DeferredLights m_DeferredLights;
        RenderingMode m_RenderingMode;
        DepthPrimingMode m_DepthPrimingMode;
        CopyDepthMode m_CopyDepthMode;
        bool m_DepthPrimingRecommended;
        StencilState m_DefaultStencilState;
        LightCookieManager m_LightCookieManager;
        IntermediateTextureMode m_IntermediateTextureMode;

        // Materials used in URP Scriptable Render Passes
        Material m_BlitMaterial = null;
        Material m_BlitHDRMaterial = null;
        Material m_CopyDepthMaterial = null;
        Material m_SamplingMaterial = null;
        Material m_StencilDeferredMaterial = null;
        Material m_CameraMotionVecMaterial = null;
        Material m_ObjectMotionVecMaterial = null;

        PostProcessPasses m_PostProcessPasses;
        internal ColorGradingLutPass colorGradingLutPass { get => m_PostProcessPasses.colorGradingLutPass; }
        internal PostProcessPass postProcessPass { get => m_PostProcessPasses.postProcessPass; }
        internal PostProcessPass finalPostProcessPass { get => m_PostProcessPasses.finalPostProcessPass; }
        internal RTHandle colorGradingLut { get => m_PostProcessPasses.colorGradingLut; }
        internal DeferredLights deferredLights { get => m_DeferredLights; }

        /// <summary>
        /// Constructor for the Universal Renderer.
        /// </summary>
        /// <param name="data">The settings to create the renderer with.</param>
        public UniversalRenderer(UniversalRendererData data) : base(data)
        {
            // Query and cache runtime platform info first before setting up URP.
            PlatformAutoDetect.Initialize();

#if ENABLE_VR && ENABLE_XR_MODULE
            Experimental.Rendering.XRSystem.Initialize(XRPassUniversal.Create, data.xrSystemData.shaders.xrOcclusionMeshPS, data.xrSystemData.shaders.xrMirrorViewPS);
#endif
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.coreBlitPS);
            m_BlitHDRMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitHDROverlay);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyDepthPS);
            m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.shaders.samplingPS);
            m_StencilDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.stencilDeferredPS);
            m_CameraMotionVecMaterial = CoreUtils.CreateEngineMaterial(data.shaders.cameraMotionVector);
            m_ObjectMotionVecMaterial = CoreUtils.CreateEngineMaterial(data.shaders.objectMotionVector);

            StencilStateData stencilData = data.defaultStencilState;
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

            m_IntermediateTextureMode = data.intermediateTextureMode;

            if (UniversalRenderPipeline.asset?.supportsLightCookies ?? false)
            {
                var settings = LightCookieManager.Settings.Create();
                var asset = UniversalRenderPipeline.asset;
                if (asset)
                {
                    settings.atlas.format = asset.additionalLightsCookieFormat;
                    settings.atlas.resolution = asset.additionalLightsCookieResolution;
                }

                m_LightCookieManager = new LightCookieManager(ref settings);
            }

            this.stripShadowsOffVariants = true;
            this.stripAdditionalLightOffVariants = true;
#if ENABLE_VR && ENABLE_VR_MODULE
#if PLATFORM_WINRT || PLATFORM_ANDROID
            // AdditionalLightOff variant is available on HL&Quest platform due to performance consideration.
            this.stripAdditionalLightOffVariants = !PlatformAutoDetect.isXRMobile;
#endif
#endif

            ForwardLights.InitParams forwardInitParams;
            forwardInitParams.lightCookieManager = m_LightCookieManager;
            forwardInitParams.forwardPlus = data.renderingMode == RenderingMode.ForwardPlus;
            m_Clustering = data.renderingMode == RenderingMode.ForwardPlus;
            m_ForwardLights = new ForwardLights(forwardInitParams);
            //m_DeferredLights.LightCulling = data.lightCulling;
            this.m_RenderingMode = data.renderingMode;
            this.m_DepthPrimingMode = data.depthPrimingMode;
            this.m_CopyDepthMode = data.copyDepthMode;
            useRenderPassEnabled = data.useNativeRenderPass && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

#if UNITY_ANDROID || UNITY_IOS || UNITY_TVOS
            this.m_DepthPrimingRecommended = false;
#else
            this.m_DepthPrimingRecommended = true;
#endif

            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);

#if ENABLE_VR && ENABLE_XR_MODULE
            m_XROcclusionMeshPass = new XROcclusionMeshPass(RenderPassEvent.BeforeRenderingOpaques);
            // Schedule XR copydepth right after m_FinalBlitPass
            m_XRCopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRendering + k_AfterFinalBlitPassQueueOffset, m_CopyDepthMaterial);
#endif
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_DepthNormalPrepass = new DepthNormalOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_MotionVectorPass = new MotionVectorRenderPass(m_CameraMotionVecMaterial, m_ObjectMotionVecMaterial);

            if (renderingModeRequested == RenderingMode.Forward || renderingModeRequested == RenderingMode.ForwardPlus)
            {
                m_PrimedDepthCopyPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, m_CopyDepthMaterial, true);
            }

            if (this.renderingModeRequested == RenderingMode.Deferred)
            {
                var deferredInitParams = new DeferredLights.InitParams();
                deferredInitParams.stencilDeferredMaterial = m_StencilDeferredMaterial;
                deferredInitParams.lightCookieManager = m_LightCookieManager;
                m_DeferredLights = new DeferredLights(deferredInitParams, useRenderPassEnabled);
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
                m_GBufferCopyDepthPass = new CopyDepthPass(RenderPassEvent.BeforeRenderingGbuffer + 1, m_CopyDepthMaterial, true);
                m_DeferredPass = new DeferredPass(RenderPassEvent.BeforeRenderingDeferredLights, m_DeferredLights);
                m_RenderOpaqueForwardOnlyPass = new DrawObjectsPass("Render Opaques Forward Only", forwardOnlyShaderTagIds, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, forwardOnlyStencilState, forwardOnlyStencilRef);
            }

            // Always create this pass even in deferred because we use it for wireframe rendering in the Editor or offscreen depth texture rendering.
            m_RenderOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawOpaqueObjects, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_RenderOpaqueForwardWithRenderingLayersPass = new DrawObjectsWithRenderingLayersPass(URPProfileId.DrawOpaqueObjects, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);

            bool copyDepthAfterTransparents = m_CopyDepthMode == CopyDepthMode.AfterTransparents;

            m_CopyDepthPass = new CopyDepthPass(
                copyDepthAfterTransparents ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingSkybox,
                m_CopyDepthMaterial,
                shouldClear: true,
                copyResolvedDepth: RenderingUtils.MultisampleDepthResolveSupported() && copyDepthAfterTransparents);

            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
            m_CopyColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, m_SamplingMaterial, m_BlitMaterial);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                m_TransparentSettingsPass = new TransparentSettingsPass(RenderPassEvent.BeforeRenderingTransparents, data.shadowTransparentReceive);
                m_RenderTransparentForwardPass = new DrawObjectsPass(URPProfileId.DrawTransparentObjects, false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, data.transparentLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            }
            m_OnRenderObjectCallbackPass = new InvokeOnRenderObjectCallbackPass(RenderPassEvent.BeforeRenderingPostProcessing);

            m_DrawOffscreenUIPass = new DrawScreenSpaceUIPass(RenderPassEvent.BeforeRenderingPostProcessing, true);
            m_DrawOverlayUIPass = new DrawScreenSpaceUIPass(RenderPassEvent.AfterRendering + k_AfterFinalBlitPassQueueOffset, false); // after m_FinalBlitPass

            {
                var postProcessParams = PostProcessParams.Create();
                postProcessParams.blitMaterial = m_BlitMaterial;
                postProcessParams.requestHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                var asset = UniversalRenderPipeline.asset;
                if (asset)
                    postProcessParams.requestHDRFormat = UniversalRenderPipeline.MakeRenderTextureGraphicsFormat(asset.supportsHDR, asset.hdrColorBufferPrecision, false);

                m_PostProcessPasses = new PostProcessPasses(data.postProcessData, ref postProcessParams);
            }

            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + k_FinalBlitPassQueueOffset, m_BlitMaterial, m_BlitHDRMaterial);

#if UNITY_EDITOR
            m_FinalDepthCopyPass = new CopyDepthPass(RenderPassEvent.AfterRendering + 9, m_CopyDepthMaterial);
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_ColorBufferSystem = new RenderTargetBufferSystem("_CameraColorAttachment");

            supportedRenderingFeatures = new RenderingFeatures();

            if (this.renderingModeRequested == RenderingMode.Deferred)
            {
                // Deferred rendering does not support MSAA.
                this.supportedRenderingFeatures.msaa = false;

                // Avoid legacy platforms: use vulkan instead.
                unsupportedGraphicsDeviceTypes = new GraphicsDeviceType[]
                {
                    GraphicsDeviceType.OpenGLCore,
                    GraphicsDeviceType.OpenGLES2,
                    GraphicsDeviceType.OpenGLES3
                };
            }

            LensFlareCommonSRP.mergeNeeded = 0;
            LensFlareCommonSRP.maxLensFlareWithOcclusionTemporalSample = 1;
            LensFlareCommonSRP.Initialize();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            m_ForwardLights.Cleanup();
            m_GBufferPass?.Dispose();
            m_PostProcessPasses.Dispose();
            m_FinalBlitPass?.Dispose();
            m_DrawOffscreenUIPass?.Dispose();
            m_DrawOverlayUIPass?.Dispose();

            m_XRTargetHandleAlias?.Release();

            ReleaseRenderTargets();

            base.Dispose(disposing);
            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_BlitHDRMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);
            CoreUtils.Destroy(m_StencilDeferredMaterial);
            CoreUtils.Destroy(m_CameraMotionVecMaterial);
            CoreUtils.Destroy(m_ObjectMotionVecMaterial);

            CleanupRenderGraphResources();

            LensFlareCommonSRP.Dispose();
        }

        internal override void ReleaseRenderTargets()
        {
            m_ColorBufferSystem.Dispose();
            if (m_DeferredLights != null && !m_DeferredLights.UseRenderPass)
                m_GBufferPass?.Dispose();

            m_PostProcessPasses.ReleaseRenderTargets();
            m_MainLightShadowCasterPass?.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();

            m_CameraDepthAttachment?.Release();
            m_DepthTexture?.Release();
            m_NormalsTexture?.Release();
            m_DecalLayersTexture?.Release();
            m_OpaqueColor?.Release();
            m_MotionVectorColor?.Release();
            m_MotionVectorDepth?.Release();
            hasReleasedRTs = true;
        }

        private void SetupFinalPassDebug(ref CameraData cameraData)
        {
            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(ref cameraData))
            {
                if (DebugHandler.TryGetFullscreenDebugMode(out DebugFullScreenMode fullScreenDebugMode, out int textureHeightPercent) &&
                    (fullScreenDebugMode != DebugFullScreenMode.ReflectionProbeAtlas || m_Clustering))
                {
                    Camera camera = cameraData.camera;
                    float screenWidth = camera.pixelWidth;
                    float screenHeight = camera.pixelHeight;

                    var relativeSize = Mathf.Clamp01(textureHeightPercent / 100f);
                    var height = relativeSize * screenHeight;
                    var width = relativeSize * screenWidth;

                    if (fullScreenDebugMode == DebugFullScreenMode.ReflectionProbeAtlas)
                    {
                        // Ensure that atlas is not stretched, but doesn't take up more than the percentage in any dimension.
                        var texture = m_ForwardLights.reflectionProbeManager.atlasRT;
                        var targetWidth = height * texture.width / texture.height;
                        if (targetWidth > width)
                        {
                            height = width * texture.height / texture.width;
                        }
                        else
                        {
                            width = targetWidth;
                        }
                    }

                    float normalizedSizeX = width / screenWidth;
                    float normalizedSizeY = height / screenHeight;

                    Rect normalizedRect = new Rect(1 - normalizedSizeX, 1 - normalizedSizeY, normalizedSizeX, normalizedSizeY);

                    switch (fullScreenDebugMode)
                    {
                        case DebugFullScreenMode.Depth:
                        {
                            DebugHandler.SetDebugRenderTarget(m_DepthTexture.nameID, normalizedRect, true);
                            break;
                        }
                        case DebugFullScreenMode.AdditionalLightsShadowMap:
                        {
                            DebugHandler.SetDebugRenderTarget(m_AdditionalLightsShadowCasterPass.m_AdditionalLightsShadowmapHandle, normalizedRect, false);
                            break;
                        }
                        case DebugFullScreenMode.MainLightShadowMap:
                        {
                            DebugHandler.SetDebugRenderTarget(m_MainLightShadowCasterPass.m_MainLightShadowmapTexture, normalizedRect, false);
                            break;
                        }
                        case DebugFullScreenMode.ReflectionProbeAtlas:
                        {
                            DebugHandler.SetDebugRenderTarget(m_ForwardLights.reflectionProbeManager.atlasRT, normalizedRect, false);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }
                else
                {
                    DebugHandler.ResetDebugRenderTarget();
                }
            }
        }

        bool IsDepthPrimingEnabled(ref CameraData cameraData)
        {
            // depth priming requires an extra depth copy, disable it on platforms not supporting it (like GLES when MSAA is on)
            if (!CanCopyDepth(ref cameraData))
                return false;

            bool depthPrimingRequested = (m_DepthPrimingRecommended && m_DepthPrimingMode == DepthPrimingMode.Auto) || m_DepthPrimingMode == DepthPrimingMode.Forced;
            bool isForwardRenderingMode = m_RenderingMode == RenderingMode.Forward || m_RenderingMode == RenderingMode.ForwardPlus;
            bool isFirstCameraToWriteDepth = cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth;
            // Enabled Depth priming when baking Reflection Probes causes artefacts (UUM-12397)
            bool isNotReflectionCamera = cameraData.cameraType != CameraType.Reflection;

            return  depthPrimingRequested && isForwardRenderingMode && isFirstCameraToWriteDepth && isNotReflectionCamera;
        }

        bool IsGLESDevice()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
        }

        bool IsGLDevice()
        {
            return IsGLESDevice() || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.PreSetup(ref renderingData);

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;

            var cmd = renderingData.commandBuffer;
            if (DebugHandler != null)
            {
                DebugHandler.Setup(context, ref renderingData);
                
                if (DebugHandler.IsActiveForCamera(ref cameraData))
                {
                    if (DebugHandler.WriteToDebugScreenTexture(ref cameraData))
                    {
                        RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
                        DebugHandler.ConfigureColorDescriptorForDebugScreen(ref colorDesc, cameraData.pixelWidth, cameraData.pixelHeight);
                        RenderingUtils.ReAllocateIfNeeded(ref DebugHandler.DebugScreenColorHandle, colorDesc, name: "_DebugScreenColor");

                        RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                        DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, k_DepthBufferBits, cameraData.pixelWidth, cameraData.pixelHeight);
                        RenderingUtils.ReAllocateIfNeeded(ref DebugHandler.DebugScreenDepthHandle, depthDesc, name: "_DebugScreenDepth");
                    }

                    if (DebugHandler.HDRDebugViewIsActive(ref cameraData))
                    {
                        DebugHandler.hdrDebugViewPass.Setup(ref cameraData, DebugHandler.DebugDisplaySettings.lightingSettings.hdrDebugMode);
                        EnqueuePass(DebugHandler.hdrDebugViewPass);
                    }
                }
            }

            if (cameraData.cameraType != CameraType.Game)
                useRenderPassEnabled = false;

            // Special path for depth only offscreen cameras. Only write opaques + transparents.
            bool isOffscreenDepthTexture = cameraData.targetTexture != null && cameraData.targetTexture.format == RenderTextureFormat.Depth;
            if (isOffscreenDepthTexture)
            {
                ConfigureCameraTarget(k_CameraTarget, k_CameraTarget);
                SetupRenderPasses(in renderingData);
                EnqueuePass(m_RenderOpaqueForwardPass);

                // TODO: Do we need to inject transparents and skybox when rendering depth only camera? They don't write to depth.
                EnqueuePass(m_DrawSkyboxPass);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
                if (!needTransparencyPass)
                    return;
#endif
                EnqueuePass(m_RenderTransparentForwardPass);
                return;
            }

            // Assign the camera color target early in case it is needed during AddRenderPasses.
            bool isPreviewCamera = cameraData.isPreviewCamera;
            var createColorTexture = ((rendererFeatures.Count != 0 && m_IntermediateTextureMode == IntermediateTextureMode.Always) && !isPreviewCamera) ||
                (Application.isEditor && m_Clustering);

            // Gather render passe input requirements
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            // Gather render pass require rendering layers event and mask size
            bool requiresRenderingLayer = RenderingLayerUtils.RequireRenderingLayers(this, rendererFeatures,
                cameraTargetDescriptor.msaaSamples,
                out var renderingLayersEvent, out var renderingLayerMaskSize);

            // All passes that use write to rendering layers are excluded from gl
            // So we disable it to avoid setting multiple render targets
            if (IsGLDevice())
                requiresRenderingLayer = false;

            bool renderingLayerProvidesByDepthNormalPass = false;
            bool renderingLayerProvidesRenderObjectPass = false;
            if (requiresRenderingLayer && renderingModeActual != RenderingMode.Deferred)
            {
                switch (renderingLayersEvent)
                {
                    case RenderingLayerUtils.Event.DepthNormalPrePass:
                        renderingLayerProvidesByDepthNormalPass = true;
                        break;
                    case RenderingLayerUtils.Event.Opaque:
                        renderingLayerProvidesRenderObjectPass = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Enable depth normal prepass
            if (renderingLayerProvidesByDepthNormalPass)
                renderPassInputs.requiresNormalsTexture = true;

            // TODO: investigate the order of call, had to change because of requiresRenderingLayer
            if (m_DeferredLights != null)
            {
                m_DeferredLights.RenderingLayerMaskSize = renderingLayerMaskSize;
                m_DeferredLights.UseDecalLayers = requiresRenderingLayer;

                // TODO: This needs to be setup early, otherwise gbuffer attachments will be allocated with wrong size
                m_DeferredLights.HasNormalPrepass = renderPassInputs.requiresNormalsTexture;

                m_DeferredLights.ResolveMixedLightingMode(ref renderingData);
                m_DeferredLights.IsOverlay = cameraData.renderType == CameraRenderType.Overlay;
            }

            // Should apply post-processing after rendering this camera?
            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            // There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = renderingData.postProcessingEnabled && m_PostProcessPasses.isCreated;

            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = applyPostProcessing && cameraData.postProcessingRequiresDepthTexture;

            // TODO: We could cache and generate the LUT before rendering the stack
            bool generateColorGradingLUT = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool isSceneViewOrPreviewCamera = cameraData.isSceneViewCamera || cameraData.cameraType == CameraType.Preview;
            useDepthPriming = IsDepthPrimingEnabled(ref cameraData);
            // This indicates whether the renderer will output a depth texture.
            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || m_DepthPrimingMode == DepthPrimingMode.Forced;

#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
#else
            bool isGizmosEnabled = false;
#endif

            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);
            bool transparentsNeedSettingsPass = m_TransparentSettingsPass.Setup(ref renderingData);

            bool forcePrepass = (m_CopyDepthMode == CopyDepthMode.ForcePrepass);

            // Depth prepass is generated in the following cases:
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            // - Scene or preview cameras always require a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - Render passes require it
            bool requiresDepthPrepass = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && (!CanCopyDepth(ref renderingData.cameraData) || forcePrepass);
            requiresDepthPrepass |= isSceneViewOrPreviewCamera;
            requiresDepthPrepass |= isGizmosEnabled;
            requiresDepthPrepass |= isPreviewCamera;
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

            requiresDepthPrepass |= useDepthPriming;

            // If possible try to merge the opaque and skybox passes instead of splitting them when "Depth Texture" is required.
            // The copying of depth should normally happen after rendering opaques.
            // But if we only require it for post processing or the scene camera then we do it after rendering transparent objects
            // Aim to have the most optimized render pass event for Depth Copy (The aim is to minimize the number of render passes)
            if (requiresDepthTexture)
            {
                bool copyDepthAfterTransparents = m_CopyDepthMode == CopyDepthMode.AfterTransparents;

                RenderPassEvent copyDepthPassEvent = copyDepthAfterTransparents ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingOpaques;
                // RenderPassInputs's requiresDepthTexture is configured through ScriptableRenderPass's ConfigureInput function
                if (renderPassInputs.requiresDepthTexture)
                {
                    // Do depth copy before the render pass that requires depth texture as shader read resource
                    copyDepthPassEvent = (RenderPassEvent)Mathf.Min((int)RenderPassEvent.AfterRenderingTransparents, ((int)renderPassInputs.requiresDepthTextureEarliestEvent) - 1);
                }
                m_CopyDepthPass.renderPassEvent = copyDepthPassEvent;
            }
            else if (cameraHasPostProcessingWithDepth || isSceneViewOrPreviewCamera || isGizmosEnabled)
            {
                // If only post process requires depth texture, we can re-use depth buffer from main geometry pass instead of enqueuing a depth copy pass, but no proper API to do that for now, so resort to depth copy pass for now
                m_CopyDepthPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }


            createColorTexture |= RequiresIntermediateColorTexture(ref cameraData);
            createColorTexture |= renderPassInputs.requiresColorTexture;
            createColorTexture |= renderPassInputs.requiresColorTextureCreated;
            createColorTexture &= !isPreviewCamera;

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read later by effect requiring it.
            // When deferred renderer is enabled, we must always create a depth texture and CANNOT use BuiltinRenderTextureType.CameraTarget. This is to get
            // around a bug where during gbuffer pass (MRT pass), the camera depth attachment is correctly bound, but during
            // deferred pass ("camera color" + "camera depth"), the implicit depth surface of "camera color" is used instead of "camera depth",
            // because BuiltinRenderTextureType.CameraTarget for depth means there is no explicit depth attachment...
            bool createDepthTexture = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && !requiresDepthPrepass;
            createDepthTexture |= !cameraData.resolveFinalTarget;
            // Deferred renderer always need to access depth buffer.
            createDepthTexture |= (this.renderingModeActual == RenderingMode.Deferred && !useRenderPassEnabled);
            // Some render cases (e.g. Material previews) have shown we need to create a depth texture when we're forcing a prepass.
            createDepthTexture |= useDepthPriming;
            // Todo seems like with mrt depth is not taken from first target
            createDepthTexture |= (renderingLayerProvidesRenderObjectPass);

#if ENABLE_VR && ENABLE_XR_MODULE
            // URP can't handle msaa/size mismatch between depth RT and color RT(for now we create intermediate textures to ensure they match)
            if (cameraData.xr.enabled)
                createColorTexture |= createDepthTexture;
#endif
#if UNITY_ANDROID || UNITY_WEBGL
            // GLES can not use render texture's depth buffer with the color buffer of the backbuffer
            // in such case we create a color texture for it too.
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
                createColorTexture |= createDepthTexture;
#endif
            // If there is any scaling, the color and depth need to be the same resolution and the target texture
            // will not be the proper size in this case. Same happens with GameView.
            // This introduces the final blit pass.
            if (RTHandles.rtHandleProperties.rtHandleScale.x != 1.0f || RTHandles.rtHandleProperties.rtHandleScale.y != 1.0f)
                createColorTexture |= createDepthTexture;

            if (useRenderPassEnabled || useDepthPriming)
                createColorTexture |= createDepthTexture;

            //Set rt descriptors so preview camera's have access should it be needed
            var colorDescriptor = cameraTargetDescriptor;
            colorDescriptor.useMipMap = false;
            colorDescriptor.autoGenerateMips = false;
            colorDescriptor.depthBufferBits = (int)DepthBits.None;
            m_ColorBufferSystem.SetCameraSettings(colorDescriptor, FilterMode.Bilinear);

            // Configure all settings require to start a new camera stack (base camera only)
            if (cameraData.renderType == CameraRenderType.Base)
            {
                // Scene filtering redraws the objects on top of the resulting frame. It has to draw directly to the sceneview buffer.
                bool sceneViewFilterEnabled = camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered;
                bool intermediateRenderTexture = (createColorTexture || createDepthTexture) && !sceneViewFilterEnabled;

                // RTHandles do not support combining color and depth in the same texture so we create them separately
                // Should be independent from filtered scene view
                createDepthTexture |= createColorTexture;

                RenderTargetIdentifier targetId = BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                    targetId = cameraData.xr.renderTarget;
#endif
                if (m_XRTargetHandleAlias == null)
                {
                    m_XRTargetHandleAlias = RTHandles.Alloc(targetId);
                }
                else if (m_XRTargetHandleAlias.nameID != targetId)
                {
                    RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref m_XRTargetHandleAlias, targetId);
                }

                // Doesn't create texture for Overlay cameras as they are already overlaying on top of created textures.
                if (intermediateRenderTexture)
                    CreateCameraRenderTarget(context, ref cameraTargetDescriptor, useDepthPriming, cmd, ref cameraData);

                m_ActiveCameraColorAttachment = createColorTexture ? m_ColorBufferSystem.PeekBackBuffer() : m_XRTargetHandleAlias;
                m_ActiveCameraDepthAttachment = createDepthTexture ? m_CameraDepthAttachment : m_XRTargetHandleAlias;
            }
            else
            {
                cameraData.baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraData);
                var baseRenderer = (UniversalRenderer)baseCameraData.scriptableRenderer;
                if (m_ColorBufferSystem != baseRenderer.m_ColorBufferSystem)
                {
                    m_ColorBufferSystem.Dispose();
                    m_ColorBufferSystem = baseRenderer.m_ColorBufferSystem;
                }
                m_ActiveCameraColorAttachment = m_ColorBufferSystem.PeekBackBuffer();
                m_ActiveCameraDepthAttachment = baseRenderer.m_ActiveCameraDepthAttachment;
                m_XRTargetHandleAlias = baseRenderer.m_XRTargetHandleAlias;
            }

            if (rendererFeatures.Count != 0 && !isPreviewCamera)
                ConfigureCameraColorTarget(m_ColorBufferSystem.PeekBackBuffer());

            bool copyColorPass = renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture;
            // Check the createColorTexture logic above: intermediate color texture is not available for preview cameras.
            // Because intermediate color is not available and copyColor pass requires it, we disable CopyColor pass here.
            copyColorPass &= !isPreviewCamera;

            // Assign camera targets (color and depth)
            ConfigureCameraTarget(m_ActiveCameraColorAttachment, m_ActiveCameraDepthAttachment);

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;

            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);

            if (additionalLightShadows)
                EnqueuePass(m_AdditionalLightsShadowCasterPass);

            bool requiresDepthCopyPass = !requiresDepthPrepass
                && (renderingData.cameraData.requiresDepthTexture || cameraHasPostProcessingWithDepth || renderPassInputs.requiresDepthTexture)
                && createDepthTexture;

            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(ref cameraData))
            {
                DebugHandler.TryGetFullscreenDebugMode(out var fullScreenMode);
                if (fullScreenMode == DebugFullScreenMode.Depth)
                {
                    requiresDepthPrepass = true;
                }

                if (!DebugHandler.IsLightingActive)
                {
                    mainLightShadows = false;
                    additionalLightShadows = false;

                    if (!isSceneViewOrPreviewCamera)
                    {
                        requiresDepthPrepass = false;
                        useDepthPriming = false;
                        generateColorGradingLUT = false;
                        copyColorPass = false;
                        requiresDepthCopyPass = false;
                    }
                }

                if (useRenderPassEnabled)
                    useRenderPassEnabled = DebugHandler.IsRenderPassSupported;
            }

            cameraData.renderer.useDepthPriming = useDepthPriming;

            if (this.renderingModeActual == RenderingMode.Deferred)
            {
                if (m_DeferredLights.UseRenderPass && (RenderPassEvent.AfterRenderingGbuffer == renderPassInputs.requiresDepthNormalAtEvent || !useRenderPassEnabled))
                    m_DeferredLights.DisableFramebufferFetchInput();
            }

            // Allocate m_DepthTexture if used
            if ((this.renderingModeActual == RenderingMode.Deferred && !this.useRenderPassEnabled) || requiresDepthPrepass || requiresDepthCopyPass)
            {
                var depthDescriptor = cameraTargetDescriptor;
                if ((requiresDepthPrepass && this.renderingModeActual != RenderingMode.Deferred) || !RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R32_SFloat, FormatUsage.Render))
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

                depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
                RenderingUtils.ReAllocateIfNeeded(ref m_DepthTexture, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthTexture");

                cmd.SetGlobalTexture(m_DepthTexture.name, m_DepthTexture.nameID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            if (requiresRenderingLayer || (renderingModeActual == RenderingMode.Deferred && m_DeferredLights.UseRenderingLayers))
            {
                ref var renderingLayersTexture = ref m_DecalLayersTexture;
                string renderingLayersTextureName = "_CameraRenderingLayersTexture";

                if (this.renderingModeActual == RenderingMode.Deferred && m_DeferredLights.UseRenderingLayers)
                {
                    renderingLayersTexture = ref m_DeferredLights.GbufferAttachments[(int)m_DeferredLights.GBufferRenderingLayers];
                    renderingLayersTextureName = renderingLayersTexture.name;
                }

                var renderingLayersDescriptor = cameraTargetDescriptor;
                renderingLayersDescriptor.depthBufferBits = 0;
                // Never have MSAA on this depth texture. When doing MSAA depth priming this is the texture that is resolved to and used for post-processing.
                if (!renderingLayerProvidesRenderObjectPass)
                    renderingLayersDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
                // Find compatible render-target format for storing normals.
                // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
                // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
                if (this.renderingModeActual == RenderingMode.Deferred && m_DeferredLights.UseRenderingLayers)
                    renderingLayersDescriptor.graphicsFormat = m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferRenderingLayers); // the one used by the gbuffer.
                else
                    renderingLayersDescriptor.graphicsFormat = RenderingLayerUtils.GetFormat(renderingLayerMaskSize);

                if (renderingModeActual == RenderingMode.Deferred && m_DeferredLights.UseRenderingLayers)
                {
                    m_DeferredLights.ReAllocateGBufferIfNeeded(renderingLayersDescriptor, (int)m_DeferredLights.GBufferRenderingLayers);
                }
                else
                {
                    RenderingUtils.ReAllocateIfNeeded(ref renderingLayersTexture, renderingLayersDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: renderingLayersTextureName);
                }

                cmd.SetGlobalTexture(renderingLayersTexture.name, renderingLayersTexture.nameID);
                RenderingLayerUtils.SetupProperties(cmd, renderingLayerMaskSize);
                if (this.renderingModeActual == RenderingMode.Deferred) // As this is requested by render pass we still want to set it
                    cmd.SetGlobalTexture("_CameraRenderingLayersTexture", renderingLayersTexture.nameID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            // Allocate normal texture if used
            if (requiresDepthPrepass && renderPassInputs.requiresNormalsTexture)
            {
                ref var normalsTexture = ref m_NormalsTexture;
                string normalsTextureName = "_CameraNormalsTexture";

                if (this.renderingModeActual == RenderingMode.Deferred)
                {
                    normalsTexture = ref m_DeferredLights.GbufferAttachments[(int)m_DeferredLights.GBufferNormalSmoothnessIndex];
                    normalsTextureName = normalsTexture.name;
                }

                var normalDescriptor = cameraTargetDescriptor;
                normalDescriptor.depthBufferBits = 0;
                // Never have MSAA on this depth texture. When doing MSAA depth priming this is the texture that is resolved to and used for post-processing.
                normalDescriptor.msaaSamples = useDepthPriming ? cameraTargetDescriptor.msaaSamples : 1;// Depth-Only passes don't use MSAA, unless depth priming is enabled
                // Find compatible render-target format for storing normals.
                // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
                // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
                if (this.renderingModeActual == RenderingMode.Deferred)
                    normalDescriptor.graphicsFormat = m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferNormalSmoothnessIndex); // the one used by the gbuffer.
                else
                    normalDescriptor.graphicsFormat = DepthNormalOnlyPass.GetGraphicsFormat();

                if (this.renderingModeActual == RenderingMode.Deferred)
                {
                    m_DeferredLights.ReAllocateGBufferIfNeeded(normalDescriptor, (int)m_DeferredLights.GBufferNormalSmoothnessIndex);
                }
                else
                {
                    RenderingUtils.ReAllocateIfNeeded(ref normalsTexture, normalDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: normalsTextureName);
                }

                cmd.SetGlobalTexture(normalsTexture.name, normalsTexture.nameID);
                if (this.renderingModeActual == RenderingMode.Deferred) // As this is requested by render pass we still want to set it
                    cmd.SetGlobalTexture("_CameraNormalsTexture", normalsTexture.nameID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            if (requiresDepthPrepass)
            {
                if (renderPassInputs.requiresNormalsTexture)
                {
                    if (this.renderingModeActual == RenderingMode.Deferred)
                    {
                        // In deferred mode, depth-normal prepass does really primes the depth and normal buffers, instead of creating a copy.
                        // It is necessary because we need to render depth&normal for forward-only geometry and it is the only way
                        // to get them before the SSAO pass.

                        int gbufferNormalIndex = m_DeferredLights.GBufferNormalSmoothnessIndex;
                        if (m_DeferredLights.UseRenderingLayers)
                            m_DepthNormalPrepass.Setup(m_ActiveCameraDepthAttachment, m_DeferredLights.GbufferAttachments[gbufferNormalIndex], m_DeferredLights.GbufferAttachments[m_DeferredLights.GBufferRenderingLayers]);
                        else if (renderingLayerProvidesByDepthNormalPass)
                            m_DepthNormalPrepass.Setup(m_ActiveCameraDepthAttachment, m_DeferredLights.GbufferAttachments[gbufferNormalIndex], m_DecalLayersTexture);
                        else
                            m_DepthNormalPrepass.Setup(m_ActiveCameraDepthAttachment, m_DeferredLights.GbufferAttachments[gbufferNormalIndex]);

                        // Only render forward-only geometry, as standard geometry will be rendered as normal into the gbuffer.
                        if (RenderPassEvent.AfterRenderingGbuffer <= renderPassInputs.requiresDepthNormalAtEvent &&
                            renderPassInputs.requiresDepthNormalAtEvent <= RenderPassEvent.BeforeRenderingOpaques)
                            m_DepthNormalPrepass.shaderTagIds = k_DepthNormalsOnly;
                    }
                    else
                    {
                        if (renderingLayerProvidesByDepthNormalPass)
                            m_DepthNormalPrepass.Setup(m_DepthTexture, m_NormalsTexture, m_DecalLayersTexture);
                        else
                            m_DepthNormalPrepass.Setup(m_DepthTexture, m_NormalsTexture);
                    }

                    EnqueuePass(m_DepthNormalPrepass);
                }
                else
                {
                    // Deferred renderer does not require a depth-prepass to generate samplable depth texture.
                    if (this.renderingModeActual != RenderingMode.Deferred)
                    {
                        m_DepthPrepass.Setup(cameraTargetDescriptor, m_DepthTexture);
                        EnqueuePass(m_DepthPrepass);
                    }
                }
            }

            // depth priming still needs to copy depth because the prepass doesn't target anymore CameraDepthTexture
            // TODO: this is unoptimal, investigate optimizations
            if (useDepthPriming)
            {
                m_PrimedDepthCopyPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
                EnqueuePass(m_PrimedDepthCopyPass);
            }

            if (generateColorGradingLUT)
            {
                colorGradingLutPass.ConfigureDescriptor(in renderingData.postProcessingData, out var desc, out var filterMode);
                RenderingUtils.ReAllocateIfNeeded(ref m_PostProcessPasses.m_ColorGradingLut, desc, filterMode, TextureWrapMode.Clamp, anisoLevel: 0, name: "_InternalGradingLut");
                colorGradingLutPass.Setup(colorGradingLut);
                EnqueuePass(colorGradingLutPass);
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                EnqueuePass(m_XROcclusionMeshPass);
#endif

            bool lastCameraInTheStack = cameraData.resolveFinalTarget;

            if (this.renderingModeActual == RenderingMode.Deferred)
            {
                if (m_DeferredLights.UseRenderPass && (RenderPassEvent.AfterRenderingGbuffer == renderPassInputs.requiresDepthNormalAtEvent || !useRenderPassEnabled))
                    m_DeferredLights.DisableFramebufferFetchInput();

                EnqueueDeferred(ref renderingData, requiresDepthPrepass, renderPassInputs.requiresNormalsTexture, renderingLayerProvidesByDepthNormalPass, mainLightShadows, additionalLightShadows);
            }
            else
            {
                // Optimized store actions are very important on tile based GPUs and have a great impact on performance.
                // if MSAA is enabled and any of the following passes need a copy of the color or depth target, make sure the MSAA'd surface is stored
                // if following passes won't use it then just resolve (the Resolve action will still store the resolved surface, but discard the MSAA'd surface, which is very expensive to store).
                RenderBufferStoreAction opaquePassColorStoreAction = RenderBufferStoreAction.Store;
                if (cameraTargetDescriptor.msaaSamples > 1)
                    opaquePassColorStoreAction = copyColorPass ? RenderBufferStoreAction.StoreAndResolve : RenderBufferStoreAction.Store;


                // make sure we store the depth only if following passes need it.
                RenderBufferStoreAction opaquePassDepthStoreAction = (copyColorPass || requiresDepthCopyPass || !lastCameraInTheStack) ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled && cameraData.xr.copyDepth)
                {
                    opaquePassDepthStoreAction = RenderBufferStoreAction.Store;
                }
#endif

                // handle multisample depth resolve by setting the appropriate store actions if supported
                if (requiresDepthCopyPass && cameraTargetDescriptor.msaaSamples > 1 && RenderingUtils.MultisampleDepthResolveSupported())
                {
                    bool isCopyDepthAfterTransparent = m_CopyDepthPass.renderPassEvent == RenderPassEvent.AfterRenderingTransparents;

                    // we could StoreAndResolve when the depth copy is after opaque, but performance wise doing StoreAndResolve of depth targets is more expensive than a simple Store + following depth copy pass on Apple GPUs,
                    // because of the extra resolve step. So, unless we are copying the depth after the transparent pass, just Store the depth target.
                    if (isCopyDepthAfterTransparent && !copyColorPass)
                    {
                        if (opaquePassDepthStoreAction == RenderBufferStoreAction.Store)
                            opaquePassDepthStoreAction = RenderBufferStoreAction.StoreAndResolve;
                        else if (opaquePassDepthStoreAction == RenderBufferStoreAction.DontCare)
                            opaquePassDepthStoreAction = RenderBufferStoreAction.Resolve;
                    }
                }

                DrawObjectsPass renderOpaqueForwardPass = null;
                if (renderingLayerProvidesRenderObjectPass)
                {
                    renderOpaqueForwardPass = m_RenderOpaqueForwardWithRenderingLayersPass;
                    m_RenderOpaqueForwardWithRenderingLayersPass.Setup(m_ActiveCameraColorAttachment, m_DecalLayersTexture, m_ActiveCameraDepthAttachment);
                }
                else
                    renderOpaqueForwardPass = m_RenderOpaqueForwardPass;

                renderOpaqueForwardPass.ConfigureColorStoreAction(opaquePassColorStoreAction);
                renderOpaqueForwardPass.ConfigureDepthStoreAction(opaquePassDepthStoreAction);

                // If there is any custom render pass renders to opaque pass' target before opaque pass,
                // we can't clear color as it contains the valid rendering output.
                bool hasPassesBeforeOpaque = activeRenderPassQueue.Find(x => (x.renderPassEvent <= RenderPassEvent.BeforeRenderingOpaques) && !x.overrideCameraTarget) != null;
                ClearFlag opaqueForwardPassClearFlag = (hasPassesBeforeOpaque || cameraData.renderType != CameraRenderType.Base)
                                                    ? ClearFlag.None
                                                    : ClearFlag.Color;
#if ENABLE_VR && ENABLE_XR_MODULE
                // workaround for DX11 and DX12 XR test failures.
                // XRTODO: investigate DX XR clear issues.
                if (SystemInfo.usesLoadStoreActions)
#endif
                renderOpaqueForwardPass.ConfigureClear(opaqueForwardPassClearFlag, Color.black);

                EnqueuePass(renderOpaqueForwardPass);
            }

            if (camera.clearFlags == CameraClearFlags.Skybox && cameraData.renderType != CameraRenderType.Overlay)
            {
                if (RenderSettings.skybox != null || (camera.TryGetComponent(out Skybox cameraSkybox) && cameraSkybox.material != null))
                    EnqueuePass(m_DrawSkyboxPass);
            }

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer.
            // Also skip if Deferred+RenderPass as CameraDepthTexture is used and filled by the GBufferPass
            // however we might need the depth texture with Forward-only pass rendered to it, so enable the copy depth in that case
            if (requiresDepthCopyPass && !(this.renderingModeActual == RenderingMode.Deferred && useRenderPassEnabled && !renderPassInputs.requiresDepthTexture))
            {
                m_CopyDepthPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
                EnqueuePass(m_CopyDepthPass);
            }

            // Set the depth texture to the far Z if we do not have a depth prepass or copy depth
            // Don't do this for Overlay cameras to not lose depth data in between cameras (as Base is guaranteed to be first)
            if (cameraData.renderType == CameraRenderType.Base && !requiresDepthPrepass && !requiresDepthCopyPass)
                Shader.SetGlobalTexture("_CameraDepthTexture", SystemInfo.usesReversedZBuffer ? Texture2D.blackTexture : Texture2D.whiteTexture);

            if (copyColorPass)
            {
                // TODO: Downsampling method should be stored in the renderer instead of in the asset.
                // We need to migrate this data to renderer. For now, we query the method in the active asset.
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                var descriptor = cameraTargetDescriptor;
                CopyColorPass.ConfigureDescriptor(downsamplingMethod, ref descriptor, out var filterMode);

                RenderingUtils.ReAllocateIfNeeded(ref m_OpaqueColor, descriptor, filterMode, TextureWrapMode.Clamp, name: "_CameraOpaqueTexture");
                m_CopyColorPass.Setup(m_ActiveCameraColorAttachment, m_OpaqueColor, downsamplingMethod);
                EnqueuePass(m_CopyColorPass);
            }

            // Motion vectors
            if (renderPassInputs.requiresMotionVectors)
            {
                var colorDesc = cameraTargetDescriptor;
                colorDesc.graphicsFormat = MotionVectorRenderPass.k_TargetFormat;
                colorDesc.depthBufferBits = (int)DepthBits.None;
                colorDesc.msaaSamples = 1;  // Disable MSAA, consider a pixel resolve for half left velocity and half right velocity --> no velocity, which is untrue.
                RenderingUtils.ReAllocateIfNeeded(ref m_MotionVectorColor, colorDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_MotionVectorTexture");

                var depthDescriptor = cameraTargetDescriptor;
                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref m_MotionVectorDepth, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_MotionVectorDepthTexture");

                m_MotionVectorPass.Setup(m_MotionVectorColor, m_MotionVectorDepth);
                EnqueuePass(m_MotionVectorPass);
            }

#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                if (transparentsNeedSettingsPass)
                {
                    EnqueuePass(m_TransparentSettingsPass);
                }

                // if this is not lastCameraInTheStack we still need to Store, since the MSAA buffer might be needed by the Overlay cameras
                RenderBufferStoreAction transparentPassColorStoreAction = cameraTargetDescriptor.msaaSamples > 1 && lastCameraInTheStack ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
                RenderBufferStoreAction transparentPassDepthStoreAction = lastCameraInTheStack ? RenderBufferStoreAction.DontCare : RenderBufferStoreAction.Store;

                // If CopyDepthPass pass event is scheduled on or after AfterRenderingTransparent, we will need to store the depth buffer or resolve (store for now until latest trunk has depth resolve support) it for MSAA case
                if (requiresDepthCopyPass && m_CopyDepthPass.renderPassEvent >= RenderPassEvent.AfterRenderingTransparents)
                {
                    transparentPassDepthStoreAction = RenderBufferStoreAction.Store;

                    // handle depth resolve on platforms supporting it
                    if (cameraTargetDescriptor.msaaSamples > 1 && RenderingUtils.MultisampleDepthResolveSupported())
                        transparentPassDepthStoreAction = RenderBufferStoreAction.Resolve;
                }

                m_RenderTransparentForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
                m_RenderTransparentForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);
                EnqueuePass(m_RenderTransparentForwardPass);
            }
            EnqueuePass(m_OnRenderObjectCallbackPass);

            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && outputToHDR)
            {
                m_DrawOffscreenUIPass.Setup(ref cameraData, k_DepthBufferBits);
                EnqueuePass(m_DrawOffscreenUIPass);
            }

            bool hasCaptureActions = renderingData.cameraData.captureActions != null && lastCameraInTheStack;

            // When FXAA or scaling is active, we must perform an additional pass at the end of the frame for the following reasons:
            // 1. FXAA expects to be the last shader running on the image before it's presented to the screen. Since users are allowed
            //    to add additional render passes after post processing occurs, we can't run FXAA until all of those passes complete as well.
            //    The FinalPost pass is guaranteed to execute after user authored passes so FXAA is always run inside of it.
            // 2. UberPost can only handle upscaling with linear filtering. All other filtering methods require the FinalPost pass.
            // 3. TAA sharpening using standalone RCAS pass is required. (When upscaling is not enabled).
            bool applyFinalPostProcessing = anyPostProcessing && lastCameraInTheStack &&
                ((renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) ||
                 ((renderingData.cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (renderingData.cameraData.upscalingFilter != ImageUpscalingFilter.Linear)) ||
                 (renderingData.cameraData.IsTemporalAAEnabled() && renderingData.cameraData.taaSettings.contrastAdaptiveSharpening > 0.0f));

            // When post-processing is enabled we can use the stack to resolve rendering to camera target (screen or RT).
            // However when there are render passes executing after post we avoid resolving to screen so rendering continues (before sRGBConversion etc)
            bool resolvePostProcessingToCameraTarget = !hasCaptureActions && !hasPassesAfterPostProcessing && !applyFinalPostProcessing;
            bool needsColorEncoding = DebugHandler == null || !DebugHandler.HDRDebugViewIsActive(ref cameraData);

            if (applyPostProcessing)
            {
                var desc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat, DepthBits.None);
                RenderingUtils.ReAllocateIfNeeded(ref m_PostProcessPasses.m_AfterPostProcessColor, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_AfterPostProcessTexture");
            }

            if (lastCameraInTheStack)
            {
                SetupFinalPassDebug(ref cameraData);

                // Post-processing will resolve to final target. No need for final blit pass.
                if (applyPostProcessing)
                {
                    // if resolving to screen we need to be able to perform sRGBConversion in post-processing if necessary
                    bool doSRGBEncoding = resolvePostProcessingToCameraTarget && needsColorEncoding;
                    postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, resolvePostProcessingToCameraTarget, m_ActiveCameraDepthAttachment, colorGradingLut, m_MotionVectorColor, applyFinalPostProcessing, doSRGBEncoding);
                    EnqueuePass(postProcessPass);
                }

                var sourceForFinalPass = m_ActiveCameraColorAttachment;

                // Do FXAA or any other final post-processing effect that might need to run after AA.
                if (applyFinalPostProcessing)
                {
                    finalPostProcessPass.SetupFinalPass(sourceForFinalPass, true, needsColorEncoding);
                    EnqueuePass(finalPostProcessPass);
                }

                if (renderingData.cameraData.captureActions != null)
                {
                    EnqueuePass(m_CapturePass);
                }

                // if post-processing then we already resolved to camera target while doing post.
                // Also only do final blit if camera is not rendering to RT.
                bool cameraTargetResolved =
                    // final PP always blit to camera target
                    applyFinalPostProcessing ||
                    // no final PP but we have PP stack. In that case it blit unless there are render pass after PP
                    (applyPostProcessing && !hasPassesAfterPostProcessing && !hasCaptureActions) ||
                    // offscreen camera rendering to a texture, we don't need a blit pass to resolve to screen
                    m_ActiveCameraColorAttachment.nameID == m_XRTargetHandleAlias.nameID;

                // We need final blit to resolve to screen
                if (!cameraTargetResolved)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, sourceForFinalPass);
                    EnqueuePass(m_FinalBlitPass);
                }

                if (shouldRenderUI && !outputToHDR)
                {
                    EnqueuePass(m_DrawOverlayUIPass);
                }

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    // active depth is depth target, we don't need a blit pass to resolve
                    bool depthTargetResolved = m_ActiveCameraDepthAttachment.nameID == cameraData.xr.renderTarget;

                    if (!depthTargetResolved && cameraData.xr.copyDepth)
                    {
                        m_XRCopyDepthPass.Setup(m_ActiveCameraDepthAttachment, m_XRTargetHandleAlias);
                        m_XRCopyDepthPass.CopyToDepth = true;
                        EnqueuePass(m_XRCopyDepthPass);
                    }
                }
#endif
            }
            // stay in RT so we resume rendering on stack after post-processing
            else if (applyPostProcessing)
            {
                postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, false, m_ActiveCameraDepthAttachment, colorGradingLut, m_MotionVectorColor, false, false);
                EnqueuePass(postProcessPass);
            }

#if UNITY_EDITOR
            if (isSceneViewOrPreviewCamera || (isGizmosEnabled && lastCameraInTheStack))
            {
                // Scene view camera should always resolve target (not stacked)
                m_FinalDepthCopyPass.Setup(m_DepthTexture, k_CameraTarget);
                m_FinalDepthCopyPass.CopyToDepth = true;
                m_FinalDepthCopyPass.MssaSamples = 0;
                EnqueuePass(m_FinalDepthCopyPass);
            }
#endif
        }

        /// <inheritdoc />
        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);

            if (this.renderingModeActual == RenderingMode.Deferred)
                m_DeferredLights.SetupLights(context, ref renderingData);
        }

        /// <inheritdoc />
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {
            // TODO: PerObjectCulling also affect reflection probes. Enabling it for now.
            // if (asset.additionalLightsRenderingMode == LightRenderingMode.Disabled ||
            //     asset.maxAdditionalLightsCount == 0)
            if (renderingModeActual == RenderingMode.ForwardPlus)
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

            if (this.renderingModeActual == RenderingMode.Deferred)
                cullingParameters.maximumVisibleLights = 0xFFFF;
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
            m_ColorBufferSystem.Clear();
            m_ActiveCameraColorAttachment = null;
            m_ActiveCameraDepthAttachment = null;
        }

        void EnqueueDeferred(ref RenderingData renderingData, bool hasDepthPrepass, bool hasNormalPrepass, bool hasRenderingLayerPrepass, bool applyMainShadow, bool applyAdditionalShadow)
        {
            m_DeferredLights.Setup(
                ref renderingData,
                applyAdditionalShadow ? m_AdditionalLightsShadowCasterPass : null,
                hasDepthPrepass,
                hasNormalPrepass,
                hasRenderingLayerPrepass,
                m_DepthTexture,
                m_ActiveCameraDepthAttachment,
                m_ActiveCameraColorAttachment
            );
            // Need to call Configure for both of these passes to setup input attachments as first frame otherwise will raise errors
            if (useRenderPassEnabled && m_DeferredLights.UseRenderPass)
            {
                m_GBufferPass.Configure(null, renderingData.cameraData.cameraTargetDescriptor);
                m_DeferredPass.Configure(null, renderingData.cameraData.cameraTargetDescriptor);
            }

            EnqueuePass(m_GBufferPass);

            //Must copy depth for deferred shading: TODO wait for API fix to bind depth texture as read-only resource.
            if (!useRenderPassEnabled || !m_DeferredLights.UseRenderPass)
            {
                m_GBufferCopyDepthPass.Setup(m_CameraDepthAttachment, m_DepthTexture);
                EnqueuePass(m_GBufferCopyDepthPass);
            }

            EnqueuePass(m_DeferredPass);

            EnqueuePass(m_RenderOpaqueForwardOnlyPass);
        }

        private struct RenderPassInputSummary
        {
            internal bool requiresDepthTexture;
            internal bool requiresDepthPrepass;
            internal bool requiresNormalsTexture;
            internal bool requiresColorTexture;
            internal bool requiresColorTextureCreated;
            internal bool requiresMotionVectors;
            internal RenderPassEvent requiresDepthNormalAtEvent;
            internal RenderPassEvent requiresDepthTextureEarliestEvent;
        }

        private RenderPassInputSummary GetRenderPassInputs(ref RenderingData renderingData)
        {
            RenderPassEvent beforeMainRenderingEvent = m_RenderingMode == RenderingMode.Deferred ? RenderPassEvent.BeforeRenderingGbuffer : RenderPassEvent.BeforeRenderingOpaques;

            RenderPassInputSummary inputSummary = new RenderPassInputSummary();
            inputSummary.requiresDepthNormalAtEvent = RenderPassEvent.BeforeRenderingOpaques;
            inputSummary.requiresDepthTextureEarliestEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            for (int i = 0; i < activeRenderPassQueue.Count; ++i)
            {
                ScriptableRenderPass pass = activeRenderPassQueue[i];
                bool needsDepth = (pass.input & ScriptableRenderPassInput.Depth) != ScriptableRenderPassInput.None;
                bool needsNormals = (pass.input & ScriptableRenderPassInput.Normal) != ScriptableRenderPassInput.None;
                bool needsColor = (pass.input & ScriptableRenderPassInput.Color) != ScriptableRenderPassInput.None;
                bool needsMotion = (pass.input & ScriptableRenderPassInput.Motion) != ScriptableRenderPassInput.None;
                bool eventBeforeMainRendering = pass.renderPassEvent <= beforeMainRenderingEvent;

                // TODO: Need a better way to handle this, probably worth to recheck after render graph
                // DBuffer requires color texture created as it does not handle y flip correctly
                if (pass is DBufferRenderPass dBufferRenderPass)
                {
                    inputSummary.requiresColorTextureCreated = true;
                }

                inputSummary.requiresDepthTexture |= needsDepth;
                inputSummary.requiresDepthPrepass |= needsNormals || needsDepth && eventBeforeMainRendering;
                inputSummary.requiresNormalsTexture |= needsNormals;
                inputSummary.requiresColorTexture |= needsColor;
                inputSummary.requiresMotionVectors |= needsMotion;
                if (needsDepth)
                    inputSummary.requiresDepthTextureEarliestEvent = (RenderPassEvent)Mathf.Min((int)pass.renderPassEvent, (int)inputSummary.requiresDepthTextureEarliestEvent);
                if (needsNormals || needsDepth)
                    inputSummary.requiresDepthNormalAtEvent = (RenderPassEvent)Mathf.Min((int)pass.renderPassEvent, (int)inputSummary.requiresDepthNormalAtEvent);
            }

            // NOTE: TAA and motion vector dependencies added here to share between Execute and Render (Graph) paths.
            // TAA in postprocess requires motion to function.
            if (renderingData.cameraData.IsTemporalAAEnabled())
                inputSummary.requiresMotionVectors = true;

            // Motion vectors imply depth
            if (inputSummary.requiresMotionVectors)
                inputSummary.requiresDepthTexture = true;

            return inputSummary;
        }

        void CreateCameraRenderTarget(ScriptableRenderContext context, ref RenderTextureDescriptor descriptor, bool primedDepth, CommandBuffer cmd, ref CameraData cameraData)
        {
            using (new ProfilingScope(null, Profiling.createCameraRenderTarget))
            {
                if (m_ColorBufferSystem.PeekBackBuffer() == null || m_ColorBufferSystem.PeekBackBuffer().nameID != BuiltinRenderTextureType.CameraTarget)
                {
                    m_ActiveCameraColorAttachment = m_ColorBufferSystem.GetBackBuffer(cmd);
                    ConfigureCameraColorTarget(m_ActiveCameraColorAttachment);
                    cmd.SetGlobalTexture("_CameraColorTexture", m_ActiveCameraColorAttachment.nameID);
                    //Set _AfterPostProcessTexture, users might still rely on this although it is now always the cameratarget due to swapbuffer
                    cmd.SetGlobalTexture("_AfterPostProcessTexture", m_ActiveCameraColorAttachment.nameID);
                }

                if (m_CameraDepthAttachment == null || m_CameraDepthAttachment.nameID != BuiltinRenderTextureType.CameraTarget)
                {
                    var depthDescriptor = descriptor;
                    depthDescriptor.useMipMap = false;
                    depthDescriptor.autoGenerateMips = false;
                    depthDescriptor.bindMS = false;

                    bool hasMSAA = depthDescriptor.msaaSamples > 1 && (SystemInfo.supportsMultisampledTextures != 0);

                    // if MSAA is enabled and we are not resolving depth, which we only do if the CopyDepthPass is AfterTransparents,
                    // then we want to bind the multisampled surface.
                    if (hasMSAA)
                    {
                        // if depth priming is enabled the copy depth primed pass is meant to do the MSAA resolve, so we want to bind the MS surface
                        if (IsDepthPrimingEnabled(ref cameraData))
                            depthDescriptor.bindMS = true;
                        else
                            depthDescriptor.bindMS = !(RenderingUtils.MultisampleDepthResolveSupported() && m_CopyDepthMode == CopyDepthMode.AfterTransparents);
                    }

                    // binding MS surfaces is not supported by the GLES backend, and it won't be fixed after investigating
                    // the high performance impact of potential fixes, which would make it more expensive than depth prepass (fogbugz 1339401 for more info)
                    if (IsGLESDevice())
                        depthDescriptor.bindMS = false;

                    depthDescriptor.graphicsFormat = GraphicsFormat.None;
                    depthDescriptor.depthStencilFormat = k_DepthStencilFormat;
                    RenderingUtils.ReAllocateIfNeeded(ref m_CameraDepthAttachment, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
                    cmd.SetGlobalTexture(m_CameraDepthAttachment.name, m_CameraDepthAttachment.nameID);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        bool PlatformRequiresExplicitMsaaResolve()
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
        bool RequiresIntermediateColorTexture(ref CameraData cameraData)
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
            if (this.renderingModeActual == RenderingMode.Deferred)
                return true;

            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            int msaaSamples = cameraTargetDescriptor.msaaSamples;
            bool isScaledRender = cameraData.imageScalingMode != ImageScalingMode.None;
            bool isCompatibleBackbufferTextureDimension = cameraTargetDescriptor.dimension == TextureDimension.Tex2D;
            bool requiresExplicitMsaaResolve = msaaSamples > 1 && PlatformRequiresExplicitMsaaResolve();
            bool isOffscreenRender = cameraData.targetTexture != null && !isSceneViewCamera;
            bool isCapturing = cameraData.captureActions != null;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                isScaledRender = false;
                isCompatibleBackbufferTextureDimension = cameraData.xr.renderTargetDesc.dimension == cameraTargetDescriptor.dimension;
            }
#endif
            bool postProcessEnabled = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool requiresBlitForOffscreenCamera = postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve || !cameraData.isDefaultViewport;
            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                !isCompatibleBackbufferTextureDimension || isCapturing || cameraData.requireSrgbConversion;
        }

        bool CanCopyDepth(ref CameraData cameraData)
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

        internal override void SwapColorBuffer(CommandBuffer cmd)
        {
            m_ColorBufferSystem.Swap();

            //Check if we are using the depth that is attached to color buffer
            if (m_ActiveCameraDepthAttachment.nameID != BuiltinRenderTextureType.CameraTarget)
                ConfigureCameraTarget(m_ColorBufferSystem.GetBackBuffer(cmd), m_ColorBufferSystem.GetBufferA());
            else
                ConfigureCameraColorTarget(m_ColorBufferSystem.GetBackBuffer(cmd));

            m_ActiveCameraColorAttachment = m_ColorBufferSystem.GetBackBuffer(cmd);
            cmd.SetGlobalTexture("_CameraColorTexture", m_ActiveCameraColorAttachment.nameID);
            //Set _AfterPostProcessTexture, users might still rely on this although it is now always the cameratarget due to swapbuffer
            cmd.SetGlobalTexture("_AfterPostProcessTexture", m_ActiveCameraColorAttachment.nameID);
        }

        internal override RTHandle GetCameraColorFrontBuffer(CommandBuffer cmd)
        {
            return m_ColorBufferSystem.GetFrontBuffer(cmd);
        }

        internal override RTHandle GetCameraColorBackBuffer(CommandBuffer cmd)
        {
            return m_ColorBufferSystem.GetBackBuffer(cmd);
        }

        internal override void EnableSwapBufferMSAA(bool enable)
        {
            m_ColorBufferSystem.EnableMSAA(enable);
        }
    }
}
