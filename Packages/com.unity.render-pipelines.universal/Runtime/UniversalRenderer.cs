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
        Forward,
        /// <summary>Render all objects first in a g-buffer pass, then apply all lighting in a separate pass using deferred shading.</summary>
        Deferred
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
    public sealed class UniversalRenderer : ScriptableRenderer
    {
        #if UNITY_SWITCH
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
        const int k_DepthBufferBits = 24;
        #else
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
        const int k_DepthBufferBits = 32;
        #endif

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
        internal RenderingMode renderingModeActual => (GL.wireframe || (DebugHandler != null && DebugHandler.IsActiveModeUnsupportedForDeferred) || m_DeferredLights == null || !m_DeferredLights.IsRuntimeSupportedThisFrame() || m_DeferredLights.IsOverlay)
        ? RenderingMode.Forward
        : this.renderingModeRequested;

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

        internal RenderTargetBufferSystem m_ColorBufferSystem;

        internal RTHandle m_ActiveCameraColorAttachment;
        RTHandle m_ColorFrontBuffer;
        internal RTHandle m_ActiveCameraDepthAttachment;
        internal RTHandle m_CameraDepthAttachment;
        RTHandle m_XRTargetHandleAlias;
        internal RTHandle m_DepthTexture;
        RTHandle m_NormalsTexture;
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

        public UniversalRenderer(UniversalRendererData data) : base(data)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            UniversalRenderPipeline.m_XRSystem.InitializeXRSystemData(data.xrSystemData);
#endif
            // TODO: should merge shaders with HDRP into core, XR dependency for now.
            // TODO: replace/merge URP blit into core blitter.
            Blitter.Initialize(data.shaders.coreBlitPS, data.shaders.coreBlitColorAndDepthPS);

            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
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

            {
                var settings = LightCookieManager.Settings.GetDefault();
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

            ForwardLights.InitParams forwardInitParams;
            forwardInitParams.lightCookieManager = m_LightCookieManager;
            forwardInitParams.clusteredRendering = data.clusteredRendering;
            forwardInitParams.tileSize = (int)data.tileSize;
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
            // Schedule XR copydepth right after m_FinalBlitPass(AfterRendering + 1)
            m_XRCopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRendering + 2, m_CopyDepthMaterial);
#endif
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_DepthNormalPrepass = new DepthNormalOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_MotionVectorPass = new MotionVectorRenderPass(m_CameraMotionVecMaterial, m_ObjectMotionVecMaterial);

            if (this.renderingModeRequested == RenderingMode.Forward)
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

            bool copyDepthAfterTransparents = m_CopyDepthMode == CopyDepthMode.AfterTransparents;

            m_CopyDepthPass = new CopyDepthPass(copyDepthAfterTransparents ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingSkybox, m_CopyDepthMaterial, true);
            m_CopyDepthPass.m_CopyResolvedDepth = RenderingUtils.MultisampleDepthResolveSupported() && copyDepthAfterTransparents;
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

            m_PostProcessPasses = new PostProcessPasses(data.postProcessData, m_BlitMaterial);

            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);

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
            m_ColorBufferSystem.Dispose();
            m_MainLightShadowCasterPass?.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();

            m_CameraDepthAttachment?.Release();
            m_XRTargetHandleAlias?.Release();
            m_DepthTexture?.Release();
            m_NormalsTexture?.Release();
            m_OpaqueColor?.Release();
            m_MotionVectorColor?.Release();
            m_MotionVectorDepth?.Release();

            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);
            CoreUtils.Destroy(m_StencilDeferredMaterial);
            CoreUtils.Destroy(m_CameraMotionVecMaterial);
            CoreUtils.Destroy(m_ObjectMotionVecMaterial);

            Blitter.Cleanup();

            LensFlareCommonSRP.Dispose();
        }

        private void SetupFinalPassDebug(ref CameraData cameraData)
        {
            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(ref cameraData))
            {
                if (DebugHandler.TryGetFullscreenDebugMode(out DebugFullScreenMode fullScreenDebugMode, out int textureHeightPercent))
                {
                    Camera camera = cameraData.camera;
                    float screenWidth = camera.pixelWidth;
                    float screenHeight = camera.pixelHeight;
                    float height = Mathf.Clamp01(textureHeightPercent / 100f) * screenHeight;
                    float width = height * (screenWidth / screenHeight);
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

        bool IsDepthPrimingEnabled()
        {
            return (m_DepthPrimingRecommended && m_DepthPrimingMode == DepthPrimingMode.Auto) || (m_DepthPrimingMode == DepthPrimingMode.Forced);
        }

        bool IsGLESDevice()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.ProcessLights(ref renderingData);

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;

            DebugHandler?.Setup(context, ref cameraData);

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

            if (m_DeferredLights != null)
            {
                m_DeferredLights.ResolveMixedLightingMode(ref renderingData);
                m_DeferredLights.IsOverlay = cameraData.renderType == CameraRenderType.Overlay;
            }

            // Assign the camera color target early in case it is needed during AddRenderPasses.
            bool isPreviewCamera = cameraData.isPreviewCamera;
            var createColorTexture = m_IntermediateTextureMode == IntermediateTextureMode.Always && !isPreviewCamera;

            // Gather render passe input requirements
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            // Should apply post-processing after rendering this camera?
            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            // There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = renderingData.postProcessingEnabled && m_PostProcessPasses.isCreated;

            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = applyPostProcessing && cameraData.postProcessingRequiresDepthTexture;

            // TODO: We could cache and generate the LUT before rendering the stack
            bool generateColorGradingLUT = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;
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
            requiresDepthPrepass |= isSceneViewCamera;
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

            requiresDepthPrepass |= m_DepthPrimingMode == DepthPrimingMode.Forced;

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
            else if (cameraHasPostProcessingWithDepth || isSceneViewCamera || isGizmosEnabled)
            {
                // If only post process requires depth texture, we can re-use depth buffer from main geometry pass instead of enqueuing a depth copy pass, but no proper API to do that for now, so resort to depth copy pass for now
                m_CopyDepthPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }


            createColorTexture |= RequiresIntermediateColorTexture(ref cameraData);
            createColorTexture |= renderPassInputs.requiresColorTexture;
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
            createDepthTexture |= m_DepthPrimingMode == DepthPrimingMode.Forced;

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
            bool useDepthPriming = IsDepthPrimingEnabled();
            useDepthPriming &= requiresDepthPrepass && (createDepthTexture || createColorTexture) && m_RenderingMode == RenderingMode.Forward && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth);

            if (useRenderPassEnabled || useDepthPriming)
                createColorTexture |= createDepthTexture;

            // Configure all settings require to start a new camera stack (base camera only)
            if (cameraData.renderType == CameraRenderType.Base)
            {
                //Scene filtering redraws the objects on top of the resulting frame. It has to draw directly to the sceneview buffer.
                bool sceneViewFilterEnabled = camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered;
                bool intermediateRenderTexture = (createColorTexture || createDepthTexture) && !sceneViewFilterEnabled;

                // RTHandles do not support combining color and depth in the same texture so we create them separately
                createDepthTexture = intermediateRenderTexture;

                RenderTargetIdentifier targetId = BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                    targetId = cameraData.xr.renderTarget;
#endif

                if (m_XRTargetHandleAlias == null || m_XRTargetHandleAlias.nameID != targetId)
                {
                    m_XRTargetHandleAlias?.Release();
                    m_XRTargetHandleAlias = RTHandles.Alloc(targetId);
                }

                // Doesn't create texture for Overlay cameras as they are already overlaying on top of created textures.
                if (intermediateRenderTexture)
                    CreateCameraRenderTarget(context, ref cameraTargetDescriptor, useDepthPriming);

                m_ActiveCameraColorAttachment = createColorTexture && !sceneViewFilterEnabled ? m_ColorBufferSystem.PeekBackBuffer() : m_XRTargetHandleAlias;
                m_ActiveCameraDepthAttachment = createDepthTexture && !sceneViewFilterEnabled ? m_CameraDepthAttachment : m_XRTargetHandleAlias;
            }
            else
            {
                cameraData.baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraData);
                var baseRenderer = (UniversalRenderer)baseCameraData.scriptableRenderer;
                m_ColorBufferSystem = baseRenderer.m_ColorBufferSystem;
                m_ActiveCameraColorAttachment = m_ColorBufferSystem.PeekBackBuffer();
                m_ActiveCameraDepthAttachment = baseRenderer.m_ActiveCameraDepthAttachment;
                m_XRTargetHandleAlias = baseRenderer.m_XRTargetHandleAlias;
            }

            if (rendererFeatures.Count != 0 && !isPreviewCamera)
                ConfigureCameraColorTarget(m_ColorBufferSystem.PeekBackBuffer());

            cameraData.renderer.useDepthPriming = useDepthPriming;

            bool copyColorPass = renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture;

            // Assign camera targets (color and depth)
            ConfigureCameraTarget(m_ActiveCameraColorAttachment, m_ActiveCameraDepthAttachment);

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;

            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);

            if (additionalLightShadows)
                EnqueuePass(m_AdditionalLightsShadowCasterPass);

            bool requiresDepthCopyPass = !requiresDepthPrepass
                && (renderingData.cameraData.requiresDepthTexture || cameraHasPostProcessingWithDepth)
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

                    if (!isSceneViewCamera)
                    {
                        requiresDepthPrepass = false;
                        generateColorGradingLUT = false;
                        copyColorPass = false;
                        requiresDepthCopyPass = false;
                    }
                }

                if (useRenderPassEnabled)
                    useRenderPassEnabled = DebugHandler.IsRenderPassSupported;
            }
            if (this.renderingModeActual == RenderingMode.Deferred)
            {
                if (m_DeferredLights.UseRenderPass && (RenderPassEvent.AfterRenderingGbuffer == renderPassInputs.requiresDepthNormalAtEvent || !useRenderPassEnabled))
                    m_DeferredLights.DisableFramebufferFetchInput();
            }

            // Allocate m_DepthTexture if used
            if ((this.renderingModeActual == RenderingMode.Deferred && !this.useRenderPassEnabled) || requiresDepthPrepass || requiresDepthCopyPass)
            {
                var depthDescriptor = cameraTargetDescriptor;
                if (requiresDepthPrepass)
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

                CommandBuffer cmd = CommandBufferPool.Get();
                cmd.SetGlobalTexture(m_DepthTexture.name, m_DepthTexture.nameID);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
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

                RenderingUtils.ReAllocateIfNeeded(ref normalsTexture, normalDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: normalsTextureName);

                CommandBuffer cmd = CommandBufferPool.Get();
                cmd.SetGlobalTexture(normalsTexture.name, normalsTexture.nameID);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
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
                        m_DepthNormalPrepass.Setup(m_ActiveCameraDepthAttachment, m_DeferredLights.GbufferAttachments[gbufferNormalIndex]);

                        // Only render forward-only geometry, as standard geometry will be rendered as normal into the gbuffer.
                        if (RenderPassEvent.AfterRenderingGbuffer <= renderPassInputs.requiresDepthNormalAtEvent &&
                            renderPassInputs.requiresDepthNormalAtEvent <= RenderPassEvent.BeforeRenderingOpaques)
                            m_DepthNormalPrepass.shaderTagIds = k_DepthNormalsOnly;
                    }
                    else
                    {
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

            // Depth priming requires a manual resolve of MSAA depth right after the depth prepass. If autoresolve is supported but MSAA is 1x then a copy is still required.
            if (useDepthPriming && (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan || cameraTargetDescriptor.msaaSamples == 1))
            {
                m_PrimedDepthCopyPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
                EnqueuePass(m_PrimedDepthCopyPass);
            }

            if (generateColorGradingLUT)
            {
                colorGradingLutPass.ConfigureDescriptor(in renderingData.postProcessingData, out var desc, out var filterMode);
                RenderingUtils.ReAllocateIfNeeded(ref m_PostProcessPasses.m_ColorGradingLut, desc, filterMode, TextureWrapMode.Clamp, name: "_InternalGradingLut");
                colorGradingLutPass.Setup(colorGradingLut);
                EnqueuePass(colorGradingLutPass);
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                EnqueuePass(m_XROcclusionMeshPass);
#endif

            if (this.renderingModeActual == RenderingMode.Deferred)
            {
                if (m_DeferredLights.UseRenderPass && (RenderPassEvent.AfterRenderingGbuffer == renderPassInputs.requiresDepthNormalAtEvent || !useRenderPassEnabled))
                    m_DeferredLights.DisableFramebufferFetchInput();

                EnqueueDeferred(ref renderingData, requiresDepthPrepass, renderPassInputs.requiresNormalsTexture, mainLightShadows, additionalLightShadows);
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
                RenderBufferStoreAction opaquePassDepthStoreAction = (copyColorPass || requiresDepthCopyPass) ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
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

                m_RenderOpaqueForwardPass.ConfigureColorStoreAction(opaquePassColorStoreAction);
                m_RenderOpaqueForwardPass.ConfigureDepthStoreAction(opaquePassDepthStoreAction);

                // If there is any custom render pass renders to opaque pass' target before opaque pass, we could not clear color as it contains the valid rendering output.
                bool hasPassesBeforeOpaque = activeRenderPassQueue.Find(x => (x.renderPassEvent <= RenderPassEvent.BeforeRenderingOpaques) && !x.overrideCameraTarget) != null;
#if ENABLE_VR && ENABLE_XR_MODULE
                // workaround for DX11 and DX12 XR test failures.
                // XRTODO: investigate DX XR clear issues.
                if (SystemInfo.usesLoadStoreActions && !hasPassesBeforeOpaque)
#endif
                    m_RenderOpaqueForwardPass.ConfigureClear((cameraData.renderType == CameraRenderType.Base) ? ClearFlag.Color : ClearFlag.None, Color.black);

                EnqueuePass(m_RenderOpaqueForwardPass);
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

            if (renderPassInputs.requiresMotionVectors && !cameraData.xr.enabled)
            {
                SupportedRenderingFeatures.active.motionVectors = true; // hack for enabling UI

                var colorDescriptor = cameraTargetDescriptor;
                colorDescriptor.graphicsFormat = MotionVectorRenderPass.m_TargetFormat;
                colorDescriptor.depthBufferBits = (int)DepthBits.None;
                RenderingUtils.ReAllocateIfNeeded(ref m_MotionVectorColor, colorDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_MotionVectorTexture");

                var depthDescriptor = cameraTargetDescriptor;
                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                RenderingUtils.ReAllocateIfNeeded(ref m_MotionVectorDepth, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_MotionVectorDepthTexture");

                var data = MotionVectorRendering.instance.GetMotionDataForCamera(camera, cameraData);
                m_MotionVectorPass.Setup(m_MotionVectorColor, m_MotionVectorDepth, data);
                EnqueuePass(m_MotionVectorPass);
            }

            bool lastCameraInTheStack = cameraData.resolveFinalTarget;

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
                RenderBufferStoreAction transparentPassDepthStoreAction = RenderBufferStoreAction.DontCare;

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


            bool hasCaptureActions = renderingData.cameraData.captureActions != null && lastCameraInTheStack;
            bool applyFinalPostProcessing = anyPostProcessing && lastCameraInTheStack &&
                renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            // When post-processing is enabled we can use the stack to resolve rendering to camera target (screen or RT).
            // However when there are render passes executing after post we avoid resolving to screen so rendering continues (before sRGBConversion etc)
            bool resolvePostProcessingToCameraTarget = !hasCaptureActions && !hasPassesAfterPostProcessing && !applyFinalPostProcessing;

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
                    bool doSRGBConversion = resolvePostProcessingToCameraTarget;
                    postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, resolvePostProcessingToCameraTarget, m_ActiveCameraDepthAttachment, colorGradingLut, applyFinalPostProcessing, doSRGBConversion);
                    EnqueuePass(postProcessPass);
                }

                var sourceForFinalPass = m_ActiveCameraColorAttachment;

                // Do FXAA or any other final post-processing effect that might need to run after AA.
                if (applyFinalPostProcessing)
                {
                    finalPostProcessPass.SetupFinalPass(sourceForFinalPass, true);
                    EnqueuePass(finalPostProcessPass);
                }

                if (renderingData.cameraData.captureActions != null)
                {
                    m_CapturePass.Setup(sourceForFinalPass);
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
                postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, false, m_ActiveCameraDepthAttachment, colorGradingLut, false, false);
                EnqueuePass(postProcessPass);
            }

#if UNITY_EDITOR
            if (isSceneViewCamera || (isGizmosEnabled && lastCameraInTheStack))
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
            // {
            //     cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            // }

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
            m_ColorBufferSystem.Clear(cmd);
            m_ActiveCameraColorAttachment = null;
            m_ActiveCameraDepthAttachment = null;
        }

        void EnqueueDeferred(ref RenderingData renderingData, bool hasDepthPrepass, bool hasNormalPrepass, bool applyMainShadow, bool applyAdditionalShadow)
        {
            m_DeferredLights.Setup(
                ref renderingData,
                applyAdditionalShadow ? m_AdditionalLightsShadowCasterPass : null,
                hasDepthPrepass,
                hasNormalPrepass,
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

            return inputSummary;
        }

        void CreateCameraRenderTarget(ScriptableRenderContext context, ref RenderTextureDescriptor descriptor, bool primedDepth)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(null, Profiling.createCameraRenderTarget))
            {
                if (m_ColorBufferSystem.PeekBackBuffer() == null || m_ColorBufferSystem.PeekBackBuffer().nameID != BuiltinRenderTextureType.CameraTarget)
                {
                    var colorDescriptor = descriptor;
                    colorDescriptor.useMipMap = false;
                    colorDescriptor.autoGenerateMips = false;
                    colorDescriptor.depthBufferBits = (int)DepthBits.None;
                    m_ColorBufferSystem.SetCameraSettings(cmd, colorDescriptor, FilterMode.Bilinear);
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
                        if (IsDepthPrimingEnabled())
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
            CommandBufferPool.Release(cmd);
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
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
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

            bool requiresBlitForOffscreenCamera = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve || !cameraData.isDefaultViewport;
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

            // copying depth on GLES3 is giving invalid results. This won't be fixed by the driver team because it would introduce performance issues (more info in the Fogbugz issue 1339401 comments)
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
