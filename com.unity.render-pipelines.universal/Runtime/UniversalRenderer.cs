using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;

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
    /// Default renderer for Universal RP.
    /// This renderer is supported on all Universal RP supported platforms.
    /// It uses a classic forward rendering strategy with per-object light culling.
    /// </summary>
    public sealed class UniversalRenderer : ScriptableRenderer
    {
        const DepthBits k_DepthStencilBufferBits = DepthBits.Depth32;
        static readonly string k_DepthNormalsOnly = "DepthNormalsOnly";

        private static class Profiling
        {
            private const string k_Name = nameof(UniversalRenderer);
            public static readonly ProfilingSampler createCameraRenderTarget = new ProfilingSampler($"{k_Name}.{nameof(UpdateCameraAttachments)}");
        }

        // Rendering mode setup from UI.
        internal RenderingMode renderingMode { get { return m_RenderingMode;  } }
        // Actual rendering mode, which may be different (ex: wireframe rendering, harware not capable of deferred rendering).
        internal RenderingMode actualRenderingMode { get { return GL.wireframe || m_DeferredLights == null || !m_DeferredLights.IsRuntimeSupportedThisFrame() || m_DeferredLights.IsOverlay ? RenderingMode.Forward : this.renderingMode; } }
        internal bool accurateGbufferNormals { get { return m_DeferredLights != null ? m_DeferredLights.AccurateGbufferNormals : false; } }
        internal bool usesRenderPass;
        DepthOnlyPass m_DepthPrepass;
        DepthNormalOnlyPass m_DepthNormalPrepass;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        GBufferPass m_GBufferPass;
        CopyDepthPass m_GBufferCopyDepthPass;
        TileDepthRangePass m_TileDepthRangePass;
        TileDepthRangePass m_TileDepthRangeExtraPass; // TODO use subpass API to hide this pass
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
        struct CameraAttachments
        {
            public RTHandle color;
            public RTHandle depth;
        }
        struct CameraProperties
        {
            public int width, height;
            public MSAASamples msaaSamples;
        }

        CameraAttachments m_ActiveCameraAttachments;
        CameraAttachments m_CameraAttachments;

#if ENABLE_VR && ENABLE_XR_MODULE
        RTHandle m_XRCameraTarget;
#endif

        BufferedRTHandleSystem m_ColorRTBufferSystem;
        CameraProperties m_CurrentCameraProperties;

        RTHandle m_DepthTexture;
        RTHandle m_NormalsTexture;
        RTHandle[] m_GBufferHandles;
        RTHandle m_OpaqueColor;
        // For tiled-deferred shading.
        RTHandle m_DepthInfoTexture;
        RTHandle m_TileDepthInfoTexture;

        ForwardLights m_ForwardLights;
        DeferredLights m_DeferredLights;
        RenderingMode m_RenderingMode;
        StencilState m_DefaultStencilState;

        // Materials used in URP Scriptable Render Passes
        Material m_BlitMaterial = null;
        Material m_CopyDepthMaterial = null;
        Material m_SamplingMaterial = null;
        Material m_TileDepthInfoMaterial = null;
        Material m_TileDeferredMaterial = null;
        Material m_StencilDeferredMaterial = null;

        PostProcessPasses m_PostProcessPasses;
        internal ColorGradingLutPass colorGradingLutPass { get => m_PostProcessPasses.colorGradingLutPass; }
        internal PostProcessPass postProcessPass { get => m_PostProcessPasses.postProcessPass; }
        internal PostProcessPass finalPostProcessPass { get => m_PostProcessPasses.finalPostProcessPass; }
        internal RTHandle afterPostProcessColor { get => m_PostProcessPasses.afterPostProcessColor; }
        internal RTHandle colorGradingLut { get => m_PostProcessPasses.colorGradingLut; }

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
            //m_TileDepthInfoMaterial = CoreUtils.CreateEngineMaterial(data.shaders.tileDepthInfoPS);
            //m_TileDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.tileDeferredPS);
            m_StencilDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.stencilDeferredPS);

            StencilStateData stencilData = data.defaultStencilState;
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

            m_ForwardLights = new ForwardLights();
            //m_DeferredLights.LightCulling = data.lightCulling;
            this.m_RenderingMode = data.renderingMode;
            this.usesRenderPass = data.useNativeRenderPass;

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

            if (this.renderingMode == RenderingMode.Deferred)
            {
                m_DeferredLights = new DeferredLights(m_TileDepthInfoMaterial, m_TileDeferredMaterial, m_StencilDeferredMaterial);
                m_DeferredLights.AccurateGbufferNormals = data.accurateGbufferNormals;
                //m_DeferredLights.TiledDeferredShading = data.tiledDeferredShading;
                m_DeferredLights.TiledDeferredShading = false;

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
                m_GBufferCopyDepthPass = new CopyDepthPass(RenderPassEvent.BeforeRenderingGbuffer + 1, m_CopyDepthMaterial);
                m_TileDepthRangePass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingGbuffer + 2, m_DeferredLights, 0);
                m_TileDepthRangeExtraPass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingGbuffer + 3, m_DeferredLights, 1);
                m_DeferredPass = new DeferredPass(RenderPassEvent.BeforeRenderingDeferredLights, m_DeferredLights);
                m_RenderOpaqueForwardOnlyPass = new DrawObjectsPass("Render Opaques Forward Only", forwardOnlyShaderTagIds, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, forwardOnlyStencilState, forwardOnlyStencilRef);
            }

            // Always create this pass even in deferred because we use it for wireframe rendering in the Editor or offscreen depth texture rendering.
            m_RenderOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawOpaqueObjects, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);

            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingSkybox, m_CopyDepthMaterial);
            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
            m_CopyColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, m_SamplingMaterial, m_BlitMaterial);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (!UniversalRenderPipeline.asset.useAdaptivePerformance || AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipTransparentObjects == false)
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

            AllocColorRT(0, BufferedRTAllocator, 2, GraphicsFormat.B10G11R11_UFloatPack32, MSAASamples.None);
       
            bool bindMS = false;
#if ENABLE_VR && ENABLE_XR_MODULE
            bindMS = !SystemInfo.supportsMultisampleAutoResolve && SystemInfo.supportsMultisampledTextures != 0;
#endif
            m_CameraAttachments.depth = RTHandles.Alloc(
                Vector2.one,
                depthBufferBits: k_DepthStencilBufferBits,
                colorFormat: GraphicsFormat.DepthAuto,
                filterMode: FilterMode.Point,
                dimension: TextureDimension.Tex2D,
                useMipMap: false,
                autoGenerateMips: false,
                bindTextureMS: bindMS,
                name: "_CameraDepthAttachment");

            if (renderingMode == RenderingMode.Deferred)
            {
                m_GBufferHandles = new RTHandle[(int)DeferredLights.GBufferHandles.Count];
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.DepthAsColor] = RTHandles.Alloc(
                    Vector2.one,
                    colorFormat: DeferredLights.GetGBufferFormat(DeferredLights.GBufferHandles.DepthAsColor),
                    name: "_CameraOpaqueTexture");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.Albedo] = RTHandles.Alloc(
                    Vector2.one,
                    colorFormat: DeferredLights.GetGBufferFormat(DeferredLights.GBufferHandles.Albedo),
                    name: "_GBuffer0");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.SpecularMetallic] = RTHandles.Alloc(
                    Vector2.one,
                    colorFormat: DeferredLights.GetGBufferFormat(DeferredLights.GBufferHandles.SpecularMetallic),
                    name: "_GBuffer1");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.NormalSmoothness] = RTHandles.Alloc(
                    Vector2.one,
                    colorFormat: DeferredLights.GetGBufferFormat(DeferredLights.GBufferHandles.NormalSmoothness, accurateGbufferNormals),
                    name: "_GBuffer2");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.Lighting] = null; // Set to be m_ActiveCameraAttachments during runtime
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.ShadowMask] = RTHandles.Alloc(
                    Vector2.one,
                    colorFormat: DeferredLights.GetGBufferFormat(DeferredLights.GBufferHandles.ShadowMask),
                    name: "_GBuffer4");
            }
            m_DepthInfoTexture = RTHandles.Alloc(Shader.PropertyToID("_DepthInfoTexture"), "_DepthInfoTexture");
            m_TileDepthInfoTexture = RTHandles.Alloc(Shader.PropertyToID("_TileDepthInfoTexture"), "_TileDepthInfoTexture");

            supportedRenderingFeatures = new RenderingFeatures()
            {
                cameraStacking = true,
            };

            if (this.renderingMode == RenderingMode.Deferred)
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

            // MSAA is temporary disabled when using the RenderPass API. TODO: enable it back once the handling of resolving to implicit resolve textures and Vulkan backbuffer is fixed in trunk!
            if (useRenderPassEnabled)
                this.supportedRenderingFeatures.msaa = false;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            m_PostProcessPasses.Dispose();
            m_MainLightShadowCasterPass.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();

            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);
            CoreUtils.Destroy(m_TileDepthInfoMaterial);
            CoreUtils.Destroy(m_TileDeferredMaterial);
            CoreUtils.Destroy(m_StencilDeferredMaterial);

            m_CameraAttachments.color.Release();
            m_CameraAttachments.depth.Release();
            m_OpaqueColor?.Release();
            m_DepthTexture?.Release();
            m_NormalsTexture?.Release();
            if (m_GBufferHandles != null)
            {
                m_GBufferHandles[(int) DeferredLights.GBufferHandles.DepthAsColor]?.Release();
                m_GBufferHandles[(int) DeferredLights.GBufferHandles.Albedo]?.Release();
                m_GBufferHandles[(int) DeferredLights.GBufferHandles.SpecularMetallic]?.Release();
                m_GBufferHandles[(int) DeferredLights.GBufferHandles.NormalSmoothness]?.Release();
                m_GBufferHandles[(int) DeferredLights.GBufferHandles.ShadowMask]?.Release();
            }

            Blitter.Cleanup();
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            bool needTransparencyPass = !UniversalRenderPipeline.asset.useAdaptivePerformance || !AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipTransparentObjects;
#endif
            Camera camera = renderingData.cameraData.camera;
            ref CameraData cameraData = ref renderingData.cameraData;
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            RTHandle cameraTarget = k_CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                if (m_XRCameraTarget?.nameID != cameraData.xr.renderTarget)
                {
                    m_XRCameraTarget?.Release();
                    m_XRCameraTarget = RTHandles.Alloc(cameraData.xr.renderTarget);
                }
                cameraTarget = m_XRCameraTarget;
            }
#endif

            m_CurrentCameraProperties.width = cameraData.cameraTargetDescriptor.width;
            m_CurrentCameraProperties.height = cameraData.cameraTargetDescriptor.height;
            m_CurrentCameraProperties.msaaSamples = (MSAASamples)cameraData.cameraTargetDescriptor.msaaSamples;

            m_ColorRTBufferSystem.SetReferenceSize(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
            UpdateCameraAttachments(ref cameraTargetDescriptor);

            // Special path for depth only offscreen cameras. Only write opaques + transparents.
            bool isOffscreenDepthTexture = cameraData.targetTexture != null && cameraData.targetTexture.format == RenderTextureFormat.Depth;
            if (isOffscreenDepthTexture)
            {
                ConfigureCameraTarget(k_CameraTarget, k_CameraTarget);
                AddRenderPasses(ref renderingData);
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
            var createColorTexture = rendererFeatures.Count != 0 && !isPreviewCamera;
            if (createColorTexture)
            {
                ConfigureCameraColorTarget(m_ColorRTBufferSystem.GetFrameRT(0, 0));
            }

            // Add render passes and gather the input requirements
            isCameraColorTargetValid = true;
            AddRenderPasses(ref renderingData);
            isCameraColorTargetValid = false;
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            // Should apply post-processing after rendering this camera?
            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            // There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = renderingData.postProcessingEnabled && m_PostProcessPasses.isCreated;

            // TODO: We could cache and generate the LUT before rendering the stack
            bool generateColorGradingLUT = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || this.actualRenderingMode == RenderingMode.Deferred;

#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
#else
            bool isGizmosEnabled = false;
#endif

            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);
            bool transparentsNeedSettingsPass = m_TransparentSettingsPass.Setup(ref renderingData);

            // Depth prepass is generated in the following cases:
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            // - Scene or preview cameras always require a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - Render passes require it
            bool requiresDepthPrepass = requiresDepthTexture && !CanCopyDepth(ref renderingData.cameraData);
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
            if (requiresDepthPrepass && this.actualRenderingMode == RenderingMode.Deferred && !renderPassInputs.requiresNormalsTexture)
                requiresDepthPrepass = false;

            // The copying of depth should normally happen after rendering opaques.
            // But if we only require it for post processing or the scene camera then we do it after rendering transparent objects
            m_CopyDepthPass.renderPassEvent = (!requiresDepthTexture && (applyPostProcessing || isSceneViewCamera || isGizmosEnabled)) ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingOpaques;
            createColorTexture |= RequiresIntermediateColorTexture(ref cameraData);
            createColorTexture |= renderPassInputs.requiresColorTexture;
            createColorTexture &= !isPreviewCamera;

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read later by effect requiring it.
            // When deferred renderer is enabled, we must always create a depth texture and CANNOT use BuiltinRenderTextureType.CameraTarget. This is to get
            // around a bug where during gbuffer pass (MRT pass), the camera depth attachment is correctly bound, but during
            // deferred pass ("camera color" + "camera depth"), the implicit depth surface of "camera color" is used instead of "camera depth",
            // because BuiltinRenderTextureType.CameraTarget for depth means there is no explicit depth attachment...
            bool createDepthTexture = cameraData.requiresDepthTexture && !requiresDepthPrepass;
            createDepthTexture |= (cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget);
            // Deferred renderer always need to access depth buffer.
            createDepthTexture |= this.actualRenderingMode == RenderingMode.Deferred;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // URP can't handle msaa/size mismatch between depth RT and color RT(for now we create intermediate textures to ensure they match)
                createDepthTexture |= createColorTexture;
                createColorTexture = createDepthTexture;
            }
#endif

#if UNITY_ANDROID || UNITY_WEBGL
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
            {
                // GLES can not use render texture's depth buffer with the color buffer of the backbuffer
                // in such case we create a color texture for it too.
                createColorTexture |= createDepthTexture;
            }
#endif

            if (usesRenderPass || RTHandles.rtHandleProperties.rtHandleScale != Vector4.one)
            {
                createDepthTexture |= createColorTexture;
                createColorTexture = createDepthTexture;
            }
            m_CameraAttachments.color = m_ColorRTBufferSystem.GetFrameRT(0, 0);

            // Configure all settings require to start a new camera stack (base camera only)
            if (cameraData.renderType == CameraRenderType.Base)
            {
                m_ActiveCameraAttachments.color = createColorTexture ? m_CameraAttachments.color : cameraTarget;
                m_ActiveCameraAttachments.depth = (createColorTexture || createDepthTexture) ? m_CameraAttachments.depth : cameraTarget;
            }
            else
            {
                m_ActiveCameraAttachments = m_CameraAttachments;
            }

            // Assign camera targets (color and depth)
            {
                ConfigureCameraTarget(m_ActiveCameraAttachments.color, m_ActiveCameraAttachments.depth, m_ColorRTBufferSystem.GetFrameRT(0,1));
            }

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRendering) != null;

            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);

            if (additionalLightShadows)
                EnqueuePass(m_AdditionalLightsShadowCasterPass);

            if (requiresDepthPrepass)
            {
                if (renderPassInputs.requiresNormalsTexture)
                {
                    if (this.actualRenderingMode == RenderingMode.Deferred)
                    {
                        // In deferred mode, depth-normal prepass does really primes the depth and normal buffers, instead of creating a copy.
                        // It is necessary because we need to render depth&normal for forward-only geometry and it is the only way
                        // to get them before the SSAO pass.

                        int gbufferNormalIndex = m_DeferredLights.GBufferNormalSmoothnessIndex;
                        m_DepthNormalPrepass.Setup(context, cameraTargetDescriptor, ref m_ActiveCameraAttachments.depth, ref m_GBufferHandles[(int)DeferredLights.GBufferHandles.NormalSmoothness]);

                        // Change the normal format to the one used by the gbuffer.
                        RenderTextureDescriptor normalDescriptor = m_DepthNormalPrepass.normalDescriptor;
                        normalDescriptor.graphicsFormat = m_DeferredLights.GetGBufferFormat(gbufferNormalIndex);
                        m_DepthNormalPrepass.normalDescriptor = normalDescriptor;
                        // Only render forward-only geometry, as standard geometry will be rendered as normal into the gbuffer.
                        m_DepthNormalPrepass.shaderTagId = new ShaderTagId(k_DepthNormalsOnly);
                    }
                    else
                    {
                        m_DepthNormalPrepass.Setup(context, cameraTargetDescriptor, ref m_DepthTexture, ref m_NormalsTexture);
                    }

                    EnqueuePass(m_DepthNormalPrepass);
                }
                else
                {
                    // Deferred renderer does not require a depth-prepass to generate samplable depth texture.
                    if (this.actualRenderingMode != RenderingMode.Deferred)
                    {
                        m_DepthPrepass.Setup(context, cameraTargetDescriptor, ref m_DepthTexture);
                        EnqueuePass(m_DepthPrepass);
                    }
                }
            }

            if (generateColorGradingLUT)
            {
                colorGradingLutPass.Setup(colorGradingLut);
                EnqueuePass(colorGradingLutPass);
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                EnqueuePass(m_XROcclusionMeshPass);
#endif

            if (this.actualRenderingMode == RenderingMode.Deferred)
                EnqueueDeferred(context, ref renderingData, requiresDepthPrepass, renderPassInputs.requiresNormalsTexture, mainLightShadows, additionalLightShadows);
            else
                EnqueuePass(m_RenderOpaqueForwardPass);

            Skybox cameraSkybox;
            cameraData.camera.TryGetComponent<Skybox>(out cameraSkybox);
            bool isOverlayCamera = cameraData.renderType == CameraRenderType.Overlay;
            if (camera.clearFlags == CameraClearFlags.Skybox && (RenderSettings.skybox != null || cameraSkybox?.material != null) && !isOverlayCamera)
                EnqueuePass(m_DrawSkyboxPass);

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer.
            bool requiresDepthCopyPass = !requiresDepthPrepass
                && renderingData.cameraData.requiresDepthTexture
                && createDepthTexture;
            if (requiresDepthCopyPass)
            {
                m_CopyDepthPass.Setup(context, m_ActiveCameraAttachments.depth, ref m_DepthTexture, renderingData.cameraData);

                EnqueuePass(m_CopyDepthPass);
            }

            // For Base Cameras: Set the depth texture to the far Z if we do not have a depth prepass or copy depth
            if (cameraData.renderType == CameraRenderType.Base && !requiresDepthPrepass && !requiresDepthCopyPass)
            {
                //Shader.SetGlobalTexture(Shader.PropertyToID(m_DepthTexture.name), SystemInfo.usesReversedZBuffer ? Texture2D.blackTexture : Texture2D.whiteTexture);
            }
            else if (requiresDepthPrepass)
            {
                Shader.SetGlobalTexture(Shader.PropertyToID(m_DepthTexture.name), m_DepthTexture);
            }
            else if(!isSceneViewCamera) Shader.SetGlobalTexture(Shader.PropertyToID(m_DepthTexture.name), m_ActiveCameraAttachments.depth);

            if (renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture)
            {
                // TODO: Downsampling method should be store in the renderer instead of in the asset.
                // We need to migrate this data to renderer. For now, we query the method in the active asset.
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                if (m_OpaqueColor == null ||
                    m_OpaqueColor.rt?.graphicsFormat != renderingData.cameraData.cameraTargetDescriptor.graphicsFormat ||
                    downsamplingMethod != m_CopyColorPass.m_DownsamplingMethod)
                {
                    m_OpaqueColor?.Release();
                    m_OpaqueColor = RTHandles.Alloc(size =>
                        {
                            var scaleFactor = Vector2.one;
                            if (UniversalRenderPipeline.asset.opaqueDownsampling == Downsampling._2xBilinear)
                                scaleFactor /= 2;
                            else if (UniversalRenderPipeline.asset.opaqueDownsampling == Downsampling._4xBox ||
                                     UniversalRenderPipeline.asset.opaqueDownsampling == Downsampling._4xBilinear)
                                scaleFactor /= 4;
                            return new Vector2Int(Mathf.Max(Mathf.RoundToInt(scaleFactor.x * RTHandles.maxWidth), 1),
                                Mathf.Max(Mathf.RoundToInt(scaleFactor.y * RTHandles.maxHeight), 1));
                        },
                        depthBufferBits: DepthBits.None,
                        colorFormat: renderingData.cameraData.cameraTargetDescriptor.graphicsFormat,
                        filterMode: UniversalRenderPipeline.asset.opaqueDownsampling == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear,
                        dimension: renderingData.cameraData.cameraTargetDescriptor.dimension,
                        useMipMap: renderingData.cameraData.cameraTargetDescriptor.useMipMap,
                        autoGenerateMips: renderingData.cameraData.cameraTargetDescriptor.autoGenerateMips,
                        wrapMode: TextureWrapMode.Clamp,
                        name: "_CameraOpaqueTexture");
                }
                m_CopyColorPass.Setup(m_ActiveCameraAttachments.color, m_OpaqueColor, downsamplingMethod);
                EnqueuePass(m_CopyColorPass);
            }
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                if (transparentsNeedSettingsPass)
                {
                    EnqueuePass(m_TransparentSettingsPass);
                }

                EnqueuePass(m_RenderTransparentForwardPass);
            }
            EnqueuePass(m_OnRenderObjectCallbackPass);

            bool lastCameraInTheStack = cameraData.resolveFinalTarget;
            bool hasCaptureActions = renderingData.cameraData.captureActions != null && lastCameraInTheStack;
            bool applyFinalPostProcessing = anyPostProcessing && lastCameraInTheStack &&
                renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            // When post-processing is enabled we can use the stack to resolve rendering to camera target (screen or RT).
            // However when there are render passes executing after post we avoid resolving to screen so rendering continues (before sRGBConvertion etc)
            bool resolvePostProcessingToCameraTarget = !hasCaptureActions && !hasPassesAfterPostProcessing;

            m_PostProcessPasses.Setup(cameraTargetDescriptor);

            if (lastCameraInTheStack)
            {
                // Post-processing will resolve to final target. No need for final blit pass.
                if (applyPostProcessing)
                {
                    // if resolving to screen we need to be able to perform sRGBConvertion in post-processing if necessary
                    bool doSRGBConvertion = resolvePostProcessingToCameraTarget;
                    postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraAttachments.depth, colorGradingLut, doSRGBConvertion, resolvePostProcessingToCameraTarget && !applyFinalPostProcessing);
                    EnqueuePass(postProcessPass);
                }

                // Do FXAA or any other final post-processing effect that might need to run after AA.
                if (applyFinalPostProcessing)
                {
                    finalPostProcessPass.SetupFinalPass(m_ActiveCameraAttachments.color, resolvePostProcessingToCameraTarget);
                    EnqueuePass(finalPostProcessPass);
                }

                if (renderingData.cameraData.captureActions != null)
                {
                    m_CapturePass.Setup(m_ActiveCameraAttachments.color);
                    EnqueuePass(m_CapturePass);
                }

                // if post-processing then we already resolved to camera target while doing post.
                // Also only do final blit if camera is not rendering to RT.
                bool cameraTargetResolved =
                    // final PP always blit to camera target
                    applyFinalPostProcessing ||
                    // no final PP but we have PP stack. In that case it blit unless there are render pass after PP
                    (applyPostProcessing && !hasPassesAfterPostProcessing) ||
                    // offscreen camera rendering to a texture, we don't need a blit pass to resolve to screen
                    m_ActiveCameraAttachments.color == cameraTarget;

                // We need final blit to resolve to screen
                if (!cameraTargetResolved)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, m_ActiveCameraAttachments.color, true);
                    EnqueuePass(m_FinalBlitPass);
                }

#if ENABLE_VR && ENABLE_XR_MODULE
                bool depthTargetResolved =
                    // active depth is depth target, we don't need a blit pass to resolve
                    m_ActiveCameraAttachments.depth == cameraTarget;

                if (!depthTargetResolved && cameraData.xr.copyDepth)
                {
                    m_XRCopyDepthPass.Setup(m_ActiveCameraAttachments.depth);
                    EnqueuePass(m_XRCopyDepthPass);
                }
#endif
            }
            // stay in RT so we resume rendering on stack after post-processing
            else if (applyPostProcessing)
            {
                postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraAttachments.depth, colorGradingLut, false, false);
                EnqueuePass(postProcessPass);
            }

#if UNITY_EDITOR
            if (isSceneViewCamera || isGizmosEnabled)
            {
                // Scene view camera should always resolve target (not stacked)
                Assertions.Assert.IsTrue(lastCameraInTheStack, "Editor camera must resolve target upon finish rendering.");
                m_FinalDepthCopyPass.Setup(m_DepthTexture);
                m_FinalDepthCopyPass.MssaSamples = 0;
                EnqueuePass(m_FinalDepthCopyPass);
            }
#endif
        }

        /// <inheritdoc />
        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);

            // Perform per-tile light culling on CPU
            if (this.actualRenderingMode == RenderingMode.Deferred)
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

            if (this.actualRenderingMode == RenderingMode.Deferred)
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
        }

        /// <inheritdoc />
        public override void FinishRendering(CommandBuffer cmd)
        {
            m_ActiveCameraAttachments.color = null;
            m_ActiveCameraAttachments.depth = null;
        }

        void EnqueueDeferred(ScriptableRenderContext context, ref RenderingData renderingData, bool hasDepthPrepass, bool hasNormalPrepass, bool applyMainShadow, bool applyAdditionalShadow)
        {
            // the last slice is the lighting buffer created in DeferredRenderer.cs
            m_GBufferHandles[(int)DeferredLights.GBufferHandles.Lighting] = m_ActiveCameraAttachments.color;

            m_DeferredLights.Setup(
                ref renderingData,
                applyAdditionalShadow ? m_AdditionalLightsShadowCasterPass : null,
                hasDepthPrepass,
                hasNormalPrepass,
                m_DepthTexture,
                m_DepthInfoTexture,
                m_TileDepthInfoTexture,
                m_ActiveCameraAttachments.depth, m_GBufferHandles
            );

            EnqueuePass(m_GBufferPass);

            //Must copy depth for deferred shading: TODO wait for API fix to bind depth texture as read-only resource.
            m_GBufferCopyDepthPass.Setup(context, m_CameraAttachments.depth, ref m_DepthTexture, renderingData.cameraData);
            EnqueuePass(m_GBufferCopyDepthPass);

            // Note: DeferredRender.Setup is called by UniversalRenderPipeline.RenderSingleCamera (overrides ScriptableRenderer.Setup).
            // At this point, we do not know if m_DeferredLights.m_Tilers[x].m_Tiles actually contain any indices of lights intersecting tiles (If there are no lights intersecting tiles, we could skip several following passes) : this information is computed in DeferredRender.SetupLights, which is called later by UniversalRenderPipeline.RenderSingleCamera (via ScriptableRenderer.Execute).
            // However HasTileLights uses m_HasTileVisLights which is calculated by CheckHasTileLights from all visibleLights. visibleLights is the list of lights that have passed camera culling, so we know they are in front of the camera. So we can assume m_DeferredLights.m_Tilers[x].m_Tiles will not be empty in that case.
            // m_DeferredLights.m_Tilers[x].m_Tiles could be empty if we implemented an algorithm accessing scene depth information on the CPU side, but this (access depth from CPU) will probably not happen.
            if (m_DeferredLights.HasTileLights())
            {
                // Compute for each tile a 32bits bitmask in which a raised bit means "this 1/32th depth slice contains geometry that could intersect with lights".
                // Per-tile bitmasks are obtained by merging together the per-pixel bitmasks computed for each individual pixel of the tile.
                EnqueuePass(m_TileDepthRangePass);

                // On some platform, splitting the bitmasks computation into two passes:
                //   1/ Compute bitmasks for individual or small blocks of pixels
                //   2/ merge those individual bitmasks into per-tile bitmasks
                // provides better performance that doing it in a single above pass.
                if (m_DeferredLights.HasTileDepthRangeExtraPass())
                    EnqueuePass(m_TileDepthRangeExtraPass);
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
        }

        private RenderPassInputSummary GetRenderPassInputs(ref RenderingData renderingData)
        {
            RenderPassInputSummary inputSummary = new RenderPassInputSummary();
            for (int i = 0; i < activeRenderPassQueue.Count; ++i)
            {
                ScriptableRenderPass pass = activeRenderPassQueue[i];
                bool needsDepth   = (pass.input & ScriptableRenderPassInput.Depth) != ScriptableRenderPassInput.None;
                bool needsNormals = (pass.input & ScriptableRenderPassInput.Normal) != ScriptableRenderPassInput.None;
                bool needsColor   = (pass.input & ScriptableRenderPassInput.Color) != ScriptableRenderPassInput.None;
                bool eventBeforeGbuffer = pass.renderPassEvent <= RenderPassEvent.BeforeRenderingGbuffer;

                inputSummary.requiresDepthTexture   |= needsDepth;
                inputSummary.requiresDepthPrepass   |= needsNormals || needsDepth && eventBeforeGbuffer;
                inputSummary.requiresNormalsTexture |= needsNormals;
                inputSummary.requiresColorTexture   |= needsColor;
            }

            return inputSummary;
        }

        public RTHandle AllocColorRT(int id, Func<int, RTHandleSystem, GraphicsFormat, MSAASamples, RTHandle> allocator, int bufferCount, GraphicsFormat format, MSAASamples msaaSamples)
        {
            if (m_ColorRTBufferSystem == null)
            {
                m_ColorRTBufferSystem = new BufferedRTHandleSystem();
                m_ColorRTBufferSystem.Initialize();
                m_ColorRTBufferSystem.SwapAndSetReferenceSize(RTHandles.maxWidth, RTHandles.maxHeight);
            }

            m_ColorRTBufferSystem.AllocBuffer(id, (rts, i) => allocator(i, rts, format, msaaSamples), bufferCount);
            m_CameraAttachments.color = m_ColorRTBufferSystem.GetFrameRT(0, 0);
            m_ActiveCameraAttachments.color = m_CameraAttachments.color;
            ConfigureCameraTarget(m_CameraAttachments.color, m_CameraAttachments.depth, m_ColorRTBufferSystem.GetFrameRT(0, 1));

            return m_ColorRTBufferSystem.GetFrameRT(id, 0);
        }

        // BufferedRTHandleSystem API expects an allocator function. We define it here.
        static RTHandle BufferedRTAllocator(int frameIndex, RTHandleSystem RTsystem, GraphicsFormat format, MSAASamples samples)
        {
            return RTsystem.Alloc(
                    Vector2.one,
                    depthBufferBits: DepthBits.None,
                    colorFormat: format,
                    filterMode: FilterMode.Bilinear,
                    dimension: TextureDimension.Tex2D,
                    enableRandomWrite: false,
                    useMipMap: false,
                    autoGenerateMips: false,
                    bindTextureMS: samples == MSAASamples.None ? false : false,
                    msaaSamples: samples,
                    name: "_CameraColorTexture" + frameIndex);
        }

        void UpdateCameraAttachments(ref RenderTextureDescriptor descriptor)
        {
            using (new ProfilingScope(null, Profiling.createCameraRenderTarget))
            {
                bool bindMS = false;
#if ENABLE_VR && ENABLE_XR_MODULE
                bindMS = descriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && SystemInfo.supportsMultisampledTextures != 0;
#endif

                if (m_CameraAttachments.color.rt.graphicsFormat != descriptor.graphicsFormat ||
                    m_CameraAttachments.color.isMSAAEnabled != bindMS)
                {
                    m_ColorRTBufferSystem.ReleaseAll();
                    m_CameraAttachments.color = AllocColorRT(0, BufferedRTAllocator, 2, descriptor.graphicsFormat, m_CurrentCameraProperties.msaaSamples);
                }

                if (bindMS != m_CameraAttachments.depth.rt.bindTextureMS ||
                    m_CameraAttachments.depth.isMSAAEnabled != bindMS)
                {
                    m_CameraAttachments.depth.Release();
                    m_CameraAttachments.depth = RTHandles.Alloc(
                        Vector2.one,
                        depthBufferBits: k_DepthStencilBufferBits,
                        colorFormat: GraphicsFormat.DepthAuto,
                        filterMode: FilterMode.Point,
                        dimension: TextureDimension.Tex2D,
                        useMipMap: false,
                        autoGenerateMips: false,
                        bindTextureMS: bindMS,
                        msaaSamples: m_CurrentCameraProperties.msaaSamples,
                        name: "_CameraDepthAttachment");
                }
            }
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
            if (this.actualRenderingMode == RenderingMode.Deferred)
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
                isCompatibleBackbufferTextureDimension = cameraData.xr.renderTargetDesc.dimension == cameraTargetDescriptor.dimension;
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

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
        }
       
        internal override void SwapColorBuffer()
        {
            m_ColorRTBufferSystem.SwapAndSetReferenceSize(m_CurrentCameraProperties.width, m_CurrentCameraProperties.height);
            m_ActiveCameraAttachments.color = m_ColorRTBufferSystem.GetFrameRT(0, 0);
            ConfigureCameraTarget(m_ActiveCameraAttachments.color, m_ActiveCameraAttachments.depth, m_ColorRTBufferSystem.GetFrameRT(0, 1));
        }
    }
}
