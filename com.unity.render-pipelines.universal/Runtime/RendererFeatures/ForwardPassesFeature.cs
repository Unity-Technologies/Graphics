using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class ForwardPassesFeature : ScriptableRendererFeature
    {
        const int k_DepthStencilBufferBits = 32;

        /*private static class Profiling
        {
            private const string k_Name = nameof(ForwardRenderer);

            public static readonly ProfilingSampler createCameraRenderTarget =
                new ProfilingSampler($"{k_Name}.{nameof(CreateCameraRenderTarget)}");
        }*/

        // Rendering mode setup from UI.
        internal RenderingMode renderingMode
        {
            get { return m_RenderingMode; }
        }

        // Actual rendering mode, which may be different (ex: wireframe rendering, harware not capable of deferred rendering).
        internal RenderingMode actualRenderingMode
        {
            get
            {
                return GL.wireframe || m_DeferredLights == null || !m_DeferredLights.IsRuntimeSupportedThisFrame()
                    ? RenderingMode.Forward
                    : this.renderingMode;
            }
        }

        internal bool accurateGbufferNormals
        {
            get { return m_DeferredLights != null ? m_DeferredLights.AccurateGbufferNormals : false; }
        }

        //ColorGradingLutPass m_ColorGradingLutPass;
        //DepthOnlyPass m_DepthPrepass;
        //DepthNormalOnlyPass m_DepthNormalPrepass;
        //MainLightShadowCasterPass m_MainLightShadowCasterPass;
        //AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        GBufferPass m_GBufferPass;
        CopyDepthPass m_GBufferCopyDepthPass;
        TileDepthRangePass m_TileDepthRangePass;
        TileDepthRangePass m_TileDepthRangeExtraPass; // TODO use subpass API to hide this pass
        DeferredPass m_DeferredPass;
        DrawObjectsPass m_RenderOpaqueForwardOnlyPass;
        DrawObjectsPass m_RenderOpaqueForwardPass;
        DrawSkyboxPass m_DrawSkyboxPass;
        //CopyDepthPass m_CopyDepthPass;
        CopyColorPass m_CopyColorPass;
        TransparentSettingsPass m_TransparentSettingsPass;
        DrawObjectsPass m_RenderTransparentForwardPass;
        InvokeOnRenderObjectCallbackPass m_OnRenderObjectCallbackPass;
        //PostProcessPass m_PostProcessPass;
        //PostProcessPass m_FinalPostProcessPass;
        FinalBlitPass m_FinalBlitPass;
        CapturePass m_CapturePass;
#if ENABLE_VR && ENABLE_XR_MODULE
        XROcclusionMeshPass m_XROcclusionMeshPass;
        CopyDepthPass m_XRCopyDepthPass;
#endif
#if UNITY_EDITOR
        SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif

        RenderTargetHandle m_ActiveCameraColorAttachment;
        RenderTargetHandle m_ActiveCameraDepthAttachment;
        RenderTargetHandle m_CameraColorAttachment;
        RenderTargetHandle m_CameraDepthAttachment;
        RenderTargetHandle m_DepthTexture;
        RenderTargetHandle m_NormalsTexture;
        RenderTargetHandle[] m_GBufferHandles;
        RenderTargetHandle m_OpaqueColor;
        RenderTargetHandle m_AfterPostProcessColor;

        RenderTargetHandle m_ColorGradingLut;

        // For tiled-deferred shading.
        RenderTargetHandle m_DepthInfoTexture;
        RenderTargetHandle m_TileDepthInfoTexture;

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

        private bool createColorTexture = true;
        private bool createDepthTexture = true;
        private bool requiresOpaqueTexture = true;

        /// <inheritdoc/>
        public override void Create()
        {
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            ForwardRendererData data = ScriptableObject.CreateInstance<ForwardRendererData>();
            data.ReloadAllNullProperties();

            #if ENABLE_VR && ENABLE_XR_MODULE
                UniversalRenderPipeline.m_XRSystem.InitializeXRSystemData(data.xrSystemData);
            #endif

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

            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            //m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            //m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
#if ENABLE_VR && ENABLE_XR_MODULE
            m_XROcclusionMeshPass = new XROcclusionMeshPass(RenderPassEvent.BeforeRenderingOpaques);
            // Schedule XR copydepth right after m_FinalBlitPass(AfterRendering + 1)
            m_XRCopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRendering + 2, m_CopyDepthMaterial);
#endif
            //m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            //m_DepthNormalPrepass = new DepthNormalOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, data.opaqueLayerMask);

            if (this.renderingMode == RenderingMode.Deferred)
            {
                m_DeferredLights = new DeferredLights(m_TileDepthInfoMaterial, m_TileDeferredMaterial, m_StencilDeferredMaterial);
                m_DeferredLights.AccurateGbufferNormals = data.accurateGbufferNormals;
                //m_DeferredLights.TiledDeferredShading = data.tiledDeferredShading;
                m_DeferredLights.TiledDeferredShading = false;

                m_GBufferPass = new GBufferPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference, m_DeferredLights);
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
                m_RenderOpaqueForwardOnlyPass = new DrawObjectsPass("Render Opaques Forward Only", forwardOnlyShaderTagIds, true, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, data.opaqueLayerMask, forwardOnlyStencilState, forwardOnlyStencilRef);
                m_GBufferCopyDepthPass = new CopyDepthPass(RenderPassEvent.BeforeRenderingOpaques + 2, m_CopyDepthMaterial);
                m_TileDepthRangePass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingOpaques + 3, m_DeferredLights, 0);
                m_TileDepthRangeExtraPass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingOpaques + 4, m_DeferredLights, 1);
                m_DeferredPass = new DeferredPass(RenderPassEvent.BeforeRenderingOpaques + 5, m_DeferredLights);
            }

            // Always create this pass even in deferred because we use it for wireframe rendering in the Editor or offscreen depth texture rendering.
            m_RenderOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawOpaqueObjects, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);

            //m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingSkybox, m_CopyDepthMaterial);
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
/*#pragma warning disable 618 // Obsolete warning
            PostProcessData postProcessData = data.postProcessData ? data.postProcessData : urpAsset.postProcessData;
#pragma warning restore 618 // Obsolete warning
            if (postProcessData != null)
            {
                m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, postProcessData);
                m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, postProcessData, m_BlitMaterial);
                m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRendering + 1, postProcessData, m_BlitMaterial);
                m_AfterPostProcessColor.Init("_AfterPostProcessTexture");
                m_ColorGradingLut.Init("_InternalGradingLut");
            }*/

            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(RenderPassEvent.AfterRendering + 9, m_CopyDepthMaterial);
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_CameraColorAttachment.Init("_CameraColorTexture");
            m_CameraDepthAttachment.Init("_CameraDepthAttachment");
            m_DepthTexture.Init("_CameraDepthTexture");
            m_NormalsTexture.Init("_CameraNormalsTexture");
            if (this.renderingMode == RenderingMode.Deferred)
            {
                m_GBufferHandles = new RenderTargetHandle[(int)DeferredLights.GBufferHandles.Count];
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.DepthAsColor].Init("_GBufferDepthAsColor");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.Albedo].Init("_GBuffer0");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.SpecularMetallic].Init("_GBuffer1");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.NormalSmoothness].Init("_GBuffer2");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.Lighting] = new RenderTargetHandle();
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.ShadowMask].Init("_GBuffer4");
            }
            m_OpaqueColor.Init("_CameraOpaqueTexture");
            m_DepthInfoTexture.Init("_DepthInfoTexture");
            m_TileDepthInfoTexture.Init("_TileDepthInfoTexture");

            /*supportedRenderingFeatures = new RenderingFeatures()
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
            }*/
        }

        public override void Setup(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            bool needTransparencyPass = !UniversalRenderPipeline.asset.useAdaptivePerformance || !AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipTransparentObjects;
#endif
            Camera camera = renderingData.cameraData.camera;
            ref CameraData cameraData = ref renderingData.cameraData;
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            // Special path for depth only offscreen cameras. Only write opaques + transparents.
            bool isOffscreenDepthTexture = cameraData.targetTexture != null && cameraData.targetTexture.format == RenderTextureFormat.Depth;
            if (isOffscreenDepthTexture)
            {
                renderer.ConfigureCameraTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                //AddRenderPasses(ref renderingData);
                renderer.EnqueuePass(m_RenderOpaqueForwardPass);

                // TODO: Do we need to inject transparents and skybox when rendering depth only camera? They don't write to depth.
                renderer.EnqueuePass(m_DrawSkyboxPass);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
                if (!needTransparencyPass)
                    return;
#endif
                renderer.EnqueuePass(m_RenderTransparentForwardPass);
                return;
            }

            if (m_DeferredLights != null)
                m_DeferredLights.ResolveMixedLightingMode(ref renderingData);

            // Assign the camera color target early in case it is needed during AddRenderPasses.
            bool isPreviewCamera = cameraData.isPreviewCamera;
            //var createColorTexture = rendererFeatures.Count != 0 && !isPreviewCamera;
            //if (createColorTexture)
            {
                m_ActiveCameraColorAttachment = m_CameraColorAttachment;
                var activeColorRenderTargetId = m_ActiveCameraColorAttachment.Identifier();
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled) activeColorRenderTargetId = new RenderTargetIdentifier(activeColorRenderTargetId, 0, CubemapFace.Unknown, -1);
#endif
                renderer.ConfigureCameraColorTarget(activeColorRenderTargetId);
            }

            // Add render passes and gather the input requirements
            /*isCameraColorTargetValid = true;
            AddRenderPasses(ref renderingData);
            isCameraColorTargetValid = false;
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            // Should apply post-processing after rendering this camera?
*/
            //bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPass != null;
            bool applyPostProcessing = cameraData.postProcessEnabled;

            /*// There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = renderingData.postProcessingEnabled && m_FinalPostProcessPass != null;
            // TODO: We could cache and generate the LUT before rendering the stack
            bool generateColorGradingLUT = cameraData.postProcessEnabled && m_ColorGradingLutPass != null;*/
            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            //bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || this.actualRenderingMode == RenderingMode.Deferred;
            bool requiresDepthTexture = cameraData.requiresDepthTexture || this.actualRenderingMode == RenderingMode.Deferred;

            //bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            //bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);
            bool transparentsNeedSettingsPass = m_TransparentSettingsPass.Setup(ref renderingData);
/*
            // Depth prepass is generated in the following cases:
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            // - Scene or preview cameras always require a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - Render passes require it
            bool requiresDepthPrepass = requiresDepthTexture && !CanCopyDepth(ref renderingData.cameraData);
            requiresDepthPrepass |= isSceneViewCamera;
            requiresDepthPrepass |= isPreviewCamera;
            requiresDepthPrepass |= renderPassInputs.requiresDepthPrepass;
            requiresDepthPrepass |= renderPassInputs.requiresNormalsTexture;

            // The copying of depth should normally happen after rendering opaques.
            // But if we only require it for post processing or the scene camera then we do it after rendering transparent objects
            m_CopyDepthPass.renderPassEvent = (!requiresDepthTexture && (applyPostProcessing || isSceneViewCamera)) ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingOpaques;
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
*/

#if UNITY_ANDROID || UNITY_WEBGL
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
            {
                // GLES can not use render texture's depth buffer with the color buffer of the backbuffer
                // in such case we create a color texture for it too.
                createColorTexture |= createDepthTexture;
            }
#endif

            // Configure all settings require to start a new camera stack (base camera only)
            if (cameraData.renderType == CameraRenderType.Base)
            {
                RenderTargetHandle cameraTargetHandle = RenderTargetHandle.GetCameraTarget(cameraData.xr);

                m_ActiveCameraColorAttachment = (createColorTexture) ? m_CameraColorAttachment : cameraTargetHandle;
                m_ActiveCameraDepthAttachment = (createDepthTexture) ? m_CameraDepthAttachment : cameraTargetHandle;

                bool intermediateRenderTexture = createColorTexture || createDepthTexture;

                // Doesn't create texture for Overlay cameras as they are already overlaying on top of created textures.
                if (intermediateRenderTexture)
                    CreateCameraRenderTarget(context, ref cameraTargetDescriptor, createColorTexture, createDepthTexture);
            }
            else
            {
                m_ActiveCameraColorAttachment = m_CameraColorAttachment;
                m_ActiveCameraDepthAttachment = m_CameraDepthAttachment;
            }

            // Assign camera targets (color and depth)
            {
                var activeColorRenderTargetId = m_ActiveCameraColorAttachment.Identifier();
                var activeDepthRenderTargetId = m_ActiveCameraDepthAttachment.Identifier();

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    activeColorRenderTargetId = new RenderTargetIdentifier(activeColorRenderTargetId, 0, CubemapFace.Unknown, -1);
                    activeDepthRenderTargetId = new RenderTargetIdentifier(activeDepthRenderTargetId, 0, CubemapFace.Unknown, -1);
                }
#endif

                renderer.ConfigureCameraTarget(activeColorRenderTargetId, activeDepthRenderTargetId);
            }

            bool hasPassesAfterPostProcessing = renderer.activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRendering) != null;

            //if (mainLightShadows)
            //    renderer.EnqueuePass(m_MainLightShadowCasterPass);

            //if (additionalLightShadows)
            //    renderer.EnqueuePass(m_AdditionalLightsShadowCasterPass);

            /*if (requiresDepthPrepass)
            {
                if (requiresNormalsTexture)
                {
                    m_DepthNormalPrepass.Setup(cameraTargetDescriptor, m_DepthTexture, m_NormalsTexture);
                    renderer.EnqueuePass(m_DepthNormalPrepass);
                }
                else
                {
                    m_DepthPrepass.Setup(cameraTargetDescriptor, m_DepthTexture);
                    renderer.EnqueuePass(m_DepthPrepass);
                }
            }*/

            /*if (generateColorGradingLUT)
            {
                m_ColorGradingLutPass.Setup(m_ColorGradingLut);
                renderer.EnqueuePass(m_ColorGradingLutPass);
            }*/

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                renderer.EnqueuePass(m_XROcclusionMeshPass);
#endif

            /*if (this.actualRenderingMode == RenderingMode.Deferred)
                renderer.EnqueueDeferred(ref renderingData, requiresDepthPrepass, mainLightShadows, additionalLightShadows);
            else*/
                renderer.EnqueuePass(m_RenderOpaqueForwardPass);

            Skybox cameraSkybox;
            cameraData.camera.TryGetComponent<Skybox>(out cameraSkybox);
            bool isOverlayCamera = cameraData.renderType == CameraRenderType.Overlay;
            if (camera.clearFlags == CameraClearFlags.Skybox && (RenderSettings.skybox != null || cameraSkybox?.material != null) && !isOverlayCamera)
                renderer.EnqueuePass(m_DrawSkyboxPass);

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer.
            // If deferred rendering path was selected, it has already made a copy.
            /*bool requiresDepthCopyPass = !requiresDepthPrepass
                && renderingData.cameraData.requiresDepthTexture
                && createDepthTexture
                && this.actualRenderingMode != RenderingMode.Deferred;
            if (requiresDepthCopyPass)
            {
                m_CopyDepthPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
                renderer.EnqueuePass(m_CopyDepthPass);
            }

            // For Base Cameras: Set the depth texture to the far Z if we do not have a depth prepass or copy depth
            if (cameraData.renderType == CameraRenderType.Base && !requiresDepthPrepass && !requiresDepthCopyPass)
            {
                Shader.SetGlobalTexture(m_DepthTexture.id, SystemInfo.usesReversedZBuffer ? Texture2D.blackTexture : Texture2D.whiteTexture);
            }
            */

            //if (renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture)
            if (requiresOpaqueTexture)
            {
                // TODO: Downsampling method should be store in the renderer instead of in the asset.
                // We need to migrate this data to renderer. For now, we query the method in the active asset.
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                m_CopyColorPass.Setup(m_ActiveCameraColorAttachment.Identifier(), m_OpaqueColor, downsamplingMethod);
                renderer.EnqueuePass(m_CopyColorPass);
            }
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                if (transparentsNeedSettingsPass)
                {
                    renderer.EnqueuePass(m_TransparentSettingsPass);
                }

                renderer.EnqueuePass(m_RenderTransparentForwardPass);
            }
            renderer.EnqueuePass(m_OnRenderObjectCallbackPass);

            bool lastCameraInTheStack = cameraData.resolveFinalTarget;
            /*bool hasCaptureActions = renderingData.cameraData.captureActions != null && lastCameraInTheStack;
            bool applyFinalPostProcessing = anyPostProcessing && lastCameraInTheStack &&
                renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;*/

            // When post-processing is enabled we can use the stack to resolve rendering to camera target (screen or RT).
            // However when there are render passes executing after post we avoid resolving to screen so rendering continues (before sRGBConvertion etc)
            //bool resolvePostProcessingToCameraTarget = !hasCaptureActions && !hasPassesAfterPostProcessing && !applyFinalPostProcessing;

            if (lastCameraInTheStack)
            {
                // Post-processing will resolve to final target. No need for final blit pass.
                /*if (applyPostProcessing)
                {
                    var destination = resolvePostProcessingToCameraTarget ? RenderTargetHandle.CameraTarget : m_AfterPostProcessColor;

                    // if resolving to screen we need to be able to perform sRGBConvertion in post-processing if necessary
                    bool doSRGBConvertion = resolvePostProcessingToCameraTarget;
                    m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, destination, m_ActiveCameraDepthAttachment, m_ColorGradingLut, applyFinalPostProcessing, doSRGBConvertion);
                    renderer.EnqueuePass(m_PostProcessPass);
                }*/


                // if we applied post-processing for this camera it means current active texture is m_AfterPostProcessColor
                var sourceForFinalPass = (applyPostProcessing) ? m_AfterPostProcessColor : m_ActiveCameraColorAttachment;

                // Do FXAA or any other final post-processing effect that might need to run after AA.
                /*if (applyFinalPostProcessing)
                {
                    m_FinalPostProcessPass.SetupFinalPass(sourceForFinalPass);
                    renderer.EnqueuePass(m_FinalPostProcessPass);
                }*/

                if (renderingData.cameraData.captureActions != null)
                {
                    m_CapturePass.Setup(sourceForFinalPass);
                    renderer.EnqueuePass(m_CapturePass);
                }

                bool hasPassesAfterPostProcessingPlusOne = renderer.activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRendering + 1) != null;

                // if post-processing then we already resolved to camera target while doing post.
                // Also only do final blit if camera is not rendering to RT.
                bool cameraTargetResolved =
                    // final PP always blit to camera target
                    //applyFinalPostProcessing ||
                    // no final PP but we have PP stack. In that case it blit unless there are render pass after PP
                    (applyPostProcessing && !hasPassesAfterPostProcessing && hasPassesAfterPostProcessingPlusOne) ||
                    // offscreen camera rendering to a texture, we don't need a blit pass to resolve to screen
                    m_ActiveCameraColorAttachment == RenderTargetHandle.GetCameraTarget(cameraData.xr);

                // We need final blit to resolve to screen
                if (!cameraTargetResolved)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, sourceForFinalPass);
                    renderer.EnqueuePass(m_FinalBlitPass);
                }

#if ENABLE_VR && ENABLE_XR_MODULE
                bool depthTargetResolved =
                    // active depth is depth target, we don't need a blit pass to resolve
                    m_ActiveCameraDepthAttachment == RenderTargetHandle.GetCameraTarget(cameraData.xr);

                if (!depthTargetResolved && cameraData.xr.copyDepth)
                {
                    m_XRCopyDepthPass.Setup(m_ActiveCameraDepthAttachment, RenderTargetHandle.GetCameraTarget(cameraData.xr));
                    renderer.EnqueuePass(m_XRCopyDepthPass);
                }
#endif
            }
            // stay in RT so we resume rendering on stack after post-processing
            /*else if (applyPostProcessing)
            {
                m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, m_AfterPostProcessColor, m_ActiveCameraDepthAttachment, m_ColorGradingLut, false, false);
                renderer.EnqueuePass(m_PostProcessPass);
            }*/

#if UNITY_EDITOR
            if (isSceneViewCamera)
            {
                // Scene view camera should always resolve target (not stacked)
                Assertions.Assert.IsTrue(lastCameraInTheStack, "Editor camera must resolve target upon finish rendering.");
                m_SceneViewDepthCopyPass.Setup(m_DepthTexture);
                renderer.EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);

            // Perform per-tile light culling on CPU
            if (this.actualRenderingMode == RenderingMode.Deferred)
                m_DeferredLights.SetupLights(context, ref renderingData);
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
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

        void CreateCameraRenderTarget(ScriptableRenderContext context, ref RenderTextureDescriptor descriptor, bool createColor, bool createDepth)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            //using (new ProfilingScope(cmd, Profiling.createCameraRenderTarget))
            {
                if (createColor)
                {
                    bool useDepthRenderBuffer = m_ActiveCameraDepthAttachment == RenderTargetHandle.CameraTarget;
                    var colorDescriptor = descriptor;
                    colorDescriptor.useMipMap = false;
                    colorDescriptor.autoGenerateMips = false;
                    colorDescriptor.depthBufferBits = (useDepthRenderBuffer) ? k_DepthStencilBufferBits : 0;
                    cmd.GetTemporaryRT(m_ActiveCameraColorAttachment.id, colorDescriptor, FilterMode.Bilinear);
                }

                if (createDepth)
                {
                    var depthDescriptor = descriptor;
                    depthDescriptor.useMipMap = false;
                    depthDescriptor.autoGenerateMips = false;
#if ENABLE_VR && ENABLE_XR_MODULE
                    // XRTODO: Enabled this line for non-XR pass? URP copy depth pass is already capable of handling MSAA.
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
#endif
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = k_DepthStencilBufferBits;
                    cmd.GetTemporaryRT(m_ActiveCameraDepthAttachment.id, depthDescriptor, FilterMode.Point);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}

