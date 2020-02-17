using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Deferred renderer for Universal RP.
    /// This renderer is supported on all Universal RP supported platforms.
    /// In the default mode, lights volumes are rendered using stencil masks.
    /// </summary>
    public sealed class DeferredRenderer : ScriptableRenderer
    {
        const int k_DepthStencilBufferBits = 32;
        const string k_CreateCameraTextures = "Create Camera Texture";

        ColorGradingLutPass m_ColorGradingLutPass;
        DepthOnlyPass m_DepthPrepass;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
        GBufferPass m_GBufferPass;
        TileDepthRangePass m_TileDepthRangePass;
        TileDepthRangePass m_TileDepthRangeExtraPass; // TODO use subpass API to hide this pass
        DeferredPass m_DeferredPass;
        DrawObjectsPass m_RenderOpaqueForwardOnlyPass;
        DrawSkyboxPass m_DrawSkyboxPass;
        CopyDepthPass m_CopyDepthTempPass;
        CopyDepthPass m_CopyDepthPass;
        CopyColorPass m_CopyColorPass;
        TransparentSettingsPass m_TransparentSettingsPass;
        DrawObjectsPass m_RenderTransparentForwardPass;
        InvokeOnRenderObjectCallbackPass m_OnRenderObjectCallbackPass;
        PostProcessPass m_PostProcessPass;
        PostProcessPass m_FinalPostProcessPass;
        FinalBlitPass m_FinalBlitPass;
        CapturePass m_CapturePass;

#if UNITY_EDITOR
        SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif

        public const int GBufferSlicesCount = 3;

        // Attachments are like "binding points", internally they identify the texture shader properties declared with the same names
        RenderTargetHandle m_ActiveCameraColorAttachment;
        RenderTargetHandle m_ActiveCameraDepthAttachment;
        RenderTargetHandle m_CameraColorAttachment;
        RenderTargetHandle m_CameraDepthAttachment;
        RenderTargetHandle m_DepthTexture;
        RenderTargetHandle[] m_GBufferAttachments = new RenderTargetHandle[GBufferSlicesCount];
        RenderTargetHandle m_DepthCopyTexture;
        RenderTargetHandle m_OpaqueColor;
        RenderTargetHandle m_AfterPostProcessColor;
        RenderTargetHandle m_ColorGradingLut;
        RenderTargetHandle m_DepthInfoTexture;
        RenderTargetHandle m_TileDepthInfoTexture;

        ForwardLights m_ForwardLights; // Required for transparent pass
        DeferredLights m_DeferredLights;
        StencilState m_DefaultStencilState;

        Material m_BlitMaterial;
        Material m_CopyDepthMaterial;
        Material m_SamplingMaterial;
        Material m_ScreenspaceShadowsMaterial;
        Material m_TileDepthInfoMaterial;
        Material m_TileDeferredMaterial;
        Material m_StencilDeferredMaterial;

        public DeferredRenderer(DeferredRendererData data) : base(data)
        {
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyDepthPS);
            m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.shaders.samplingPS);
            m_ScreenspaceShadowsMaterial = CoreUtils.CreateEngineMaterial(data.shaders.screenSpaceShadowPS);
            m_TileDepthInfoMaterial = CoreUtils.CreateEngineMaterial(data.shaders.tileDepthInfoPS);
            m_TileDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.tileDeferredPS);
            m_StencilDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.stencilDeferredPS);

            StencilStateData stencilData = data.defaultStencilState;
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

            m_ForwardLights = new ForwardLights();
            m_DeferredLights = new DeferredLights(m_TileDepthInfoMaterial, m_TileDeferredMaterial, m_StencilDeferredMaterial);
            m_DeferredLights.tiledDeferredShading = data.tiledDeferredShading;

            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrepasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass(RenderPassEvent.BeforeRenderingPrepasses, m_ScreenspaceShadowsMaterial);
            m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingOpaques, data.postProcessData);
            m_GBufferPass = new GBufferPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_CopyDepthTempPass = new CopyDepthPass(RenderPassEvent.BeforeRenderingOpaques + 1, m_CopyDepthMaterial);
            m_TileDepthRangePass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingOpaques + 2, m_DeferredLights, 0);
            m_TileDepthRangeExtraPass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingOpaques + 3, m_DeferredLights, 1);
            m_DeferredPass = new DeferredPass(RenderPassEvent.BeforeRenderingOpaques + 4, m_DeferredLights);
            m_RenderOpaqueForwardOnlyPass = new DrawObjectsPass("Render Opaques Forward Only", new ShaderTagId("UniversalForwardOnly"), true, RenderPassEvent.BeforeRenderingOpaques + 5, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingSkybox, m_CopyDepthMaterial);
            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
            m_CopyColorPass = new CopyColorPass(RenderPassEvent.BeforeRenderingTransparents, m_SamplingMaterial);
            m_TransparentSettingsPass = new TransparentSettingsPass(RenderPassEvent.BeforeRenderingTransparents, data.shadowTransparentReceive);
            m_RenderTransparentForwardPass = new DrawObjectsPass("Render Transparents", false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, data.transparentLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_OnRenderObjectCallbackPass = new InvokeOnRenderObjectCallbackPass(RenderPassEvent.BeforeRenderingPostProcessing);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data.postProcessData);
            m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data.postProcessData);
            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, m_BlitMaterial);

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(RenderPassEvent.AfterRendering + 9, m_CopyDepthMaterial);
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            // string shaderProperty passed to Init() is use to refer to a texture from shader code
            m_CameraColorAttachment.Init("_CameraColorTexture");
            m_CameraDepthAttachment.Init("_CameraDepthAttachment");
            m_DepthTexture.Init("_CameraDepthTexture");
            m_DepthTexture.InitDescriptor(RenderTextureFormat.Depth);
            m_GBufferAttachments[0].Init("_GBuffer0");
            m_GBufferAttachments[1].Init("_GBuffer1");
            m_GBufferAttachments[2].Init("_GBuffer2");
            //m_GBufferAttachments[3].Init("_GBuffer3"); // RenderTarget bound as output #3 during the GBuffer pass is the LightingGBuffer m_ActiveCameraColorAttachment, initialized as m_CameraColorAttachment above
            m_DepthCopyTexture.Init("_CameraDepthCopyTexture");
            m_DepthCopyTexture.InitDescriptor(RenderTextureFormat.R8);
            m_OpaqueColor.Init("_CameraOpaqueTexture");
            m_AfterPostProcessColor.Init("_AfterPostProcessTexture");
            m_ColorGradingLut.Init("_InternalGradingLut");
            m_DepthInfoTexture.Init("_DepthInfoTexture");
            m_TileDepthInfoTexture.Init("_TileDepthInfoTexture");

            supportedRenderingFeatures = new RenderingFeatures()
            {
                cameraStacking = true,
            };
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            m_PostProcessPass.Cleanup();
            // m_FinalPostProcessPass.Cleanup() ?
            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);
            CoreUtils.Destroy(m_ScreenspaceShadowsMaterial);
            CoreUtils.Destroy(m_TileDepthInfoMaterial);
            CoreUtils.Destroy(m_TileDeferredMaterial);
            CoreUtils.Destroy(m_StencilDeferredMaterial);
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            ref CameraData cameraData = ref renderingData.cameraData;
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            // Depth prepass is generated in the following cases:
            // - Scene view camera always requires a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            bool requiresDepthPrepass = cameraData.isSceneViewCamera;
            requiresDepthPrepass |= (cameraData.requiresDepthTexture && !CanCopyDepth(ref renderingData.cameraData)) || m_DeferredLights.tiledDeferredShading;
            // TODO: There's an issue in multiview and depth copy pass. Atm forcing a depth prepass on XR until we have a proper fix.
            if (cameraData.isStereoEnabled && cameraData.requiresDepthTexture)
                requiresDepthPrepass = true;
            CommandBuffer cmd = CommandBufferPool.Get(k_CreateCameraTextures);
            // Special path for depth only offscreen cameras. Only write opaques + transparents.
            bool isOffscreenDepthTexture = cameraData.targetTexture != null && cameraData.targetTexture.format == RenderTextureFormat.Depth;
            if (isOffscreenDepthTexture)
            {
                ConfigureCameraTarget(RenderTargetHandle.CameraTarget, RenderTargetHandle.CameraTarget);

                for (int i = 0; i < rendererFeatures.Count; ++i)
                    rendererFeatures[i].AddRenderPasses(this, ref renderingData);

                EnqueueDeferred(ref renderingData, requiresDepthPrepass, context, ref cmd);

                // Previous pass configured different CameraTargets, restore main color and depth to be used as targets by the DrawSkybox pass:
                m_DrawSkyboxPass.ConfigureTarget(m_CameraColorAttachment, m_DepthTexture);
                EnqueuePass(m_DrawSkyboxPass);
                // Must explicitely set correct depth target to the transparent pass (it will bind a different depth target otherwise).
                m_RenderTransparentForwardPass.ConfigureTarget(m_CameraColorAttachment, m_DepthTexture);
                EnqueuePass(m_RenderTransparentForwardPass);
                return;
            }

            // We only apply post-processing at the end of the stack, i.e, when we are rendering a camera that resolves rendering to camera target.
            bool applyPostProcessing = cameraData.postProcessEnabled && renderingData.resolveFinalTarget;

            // We generate color LUT in the base camera only. This allows us to not break render pass execution for overlay cameras.
            bool generateColorGradingLUT = cameraData.postProcessEnabled && cameraData.renderType == CameraRenderType.Base;

            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);
            bool transparentsNeedSettingsPass = m_TransparentSettingsPass.Setup(ref renderingData);

            // The copying of depth should normally happen after rendering opaques only.
            // But if we only require it for post processing or the scene camera then we do it after rendering transparent objects
            m_CopyDepthPass.renderPassEvent = (!cameraData.requiresDepthTexture && (applyPostProcessing || cameraData.isSceneViewCamera)) ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingOpaques;

            //bool createColorTexture = RequiresIntermediateColorTexture(ref renderingData, cameraTargetDescriptor) || camera.forceIntoRenderTexture;
            bool createColorTexture = true; // we always create a new colorTexture in this pass: it is used as the 3rd GBuffer slice used in the GBuffer pass, and as lightingBuffer RenderTarget in the Deferred pass

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read later by effect requiring it.
            bool createDepthTexture = cameraData.requiresDepthTexture && !requiresDepthPrepass;
            createDepthTexture |= (renderingData.cameraData.renderType == CameraRenderType.Base && !renderingData.resolveFinalTarget);

            // Configure all settings require to start a new camera stack (base camera only)
            if (cameraData.renderType == CameraRenderType.Base)
            {
                m_ActiveCameraColorAttachment = (createColorTexture) ? m_CameraColorAttachment : RenderTargetHandle.CameraTarget;
                m_ActiveCameraDepthAttachment = (createDepthTexture) ? m_CameraDepthAttachment : RenderTargetHandle.CameraTarget;
                m_ActiveCameraColorAttachment.InitDescriptor(cameraTargetDescriptor.graphicsFormat);
                m_ActiveCameraDepthAttachment.InitDescriptor(RenderTextureFormat.Depth);
                bool intermediateRenderTexture = createColorTexture || createDepthTexture;

                // Doesn't create texture for Overlay cameras as they are already overlaying on top of created textures.
                bool createTextures = intermediateRenderTexture;
                if (createTextures)
                    CreateCameraRenderTarget(context, ref renderingData.cameraData);

                // if rendering to intermediate render texture we don't have to create msaa backbuffer
                int backbufferMsaaSamples = (intermediateRenderTexture) ? 1 : cameraTargetDescriptor.msaaSamples;

                if (Camera.main == camera && camera.cameraType == CameraType.Game && cameraData.targetTexture == null)
                SetupBackbufferFormat(backbufferMsaaSamples, cameraData.isStereoEnabled);
            }
            ConfigureCameraTarget(m_ActiveCameraColorAttachment, m_ActiveCameraDepthAttachment);
            m_CameraColorAttachment.InitDescriptor(cameraTargetDescriptor.graphicsFormat);
            m_ActiveCameraColorAttachment.InitDescriptor(cameraTargetDescriptor.graphicsFormat);
            for (int i = 0; i < rendererFeatures.Count; ++i)
            {
                rendererFeatures[i].AddRenderPasses(this, ref renderingData);
            }

            int count = activeRenderPassQueue.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if(activeRenderPassQueue[i] == null)
                    activeRenderPassQueue.RemoveAt(i);
            }
            bool hasAfterRendering = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRendering) != null;


            if (mainLightShadows)
            {
                m_MainLightShadowCasterPass.Configure(cmd, cameraTargetDescriptor);
                EnqueuePass(m_MainLightShadowCasterPass);
            }

            if (additionalLightShadows)
            {
                m_AdditionalLightsShadowCasterPass.Configure(cmd, cameraTargetDescriptor);
                EnqueuePass(m_AdditionalLightsShadowCasterPass);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (requiresDepthPrepass)
            {
                m_DepthPrepass.Setup(cameraTargetDescriptor, m_DepthTexture);
                EnqueuePass(m_DepthPrepass);
            }

            if (generateColorGradingLUT)
            {
                m_ColorGradingLutPass.Setup(m_ColorGradingLut);
                EnqueuePass(m_ColorGradingLutPass);
            }

            EnqueueDeferred(ref renderingData, requiresDepthPrepass, context, ref cmd);

			bool isOverlayCamera = cameraData.renderType == CameraRenderType.Overlay;
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null && !isOverlayCamera)
            {
                // Previous pass configured different CameraTargets, restore main color and depth to be used as targets by the DrawSkybox pass:
                //m_DrawSkyboxPass.ConfigureTarget(m_ActiveCameraColorAttachment, m_DepthTexture);
//                m_DrawSkyboxPass.colorAttachments.Clear();
//                m_DrawSkyboxPass.ConfigureColorAttachment(m_CameraColorAttachment, true, false);
//                m_DrawSkyboxPass.ConfigureDepthAttachment(m_DepthTexture, true, false);
//                m_DrawSkyboxPass.ConfigureRenderPassDescriptor(cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.msaaSamples);
//                EnqueuePass(m_DrawSkyboxPass);
            }

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer
            if (!requiresDepthPrepass && renderingData.cameraData.requiresDepthTexture && createDepthTexture)
            {
                m_CopyDepthPass.Setup(m_DepthTexture, m_DepthCopyTexture);
                EnqueuePass(m_CopyDepthPass);
            }

            // This is useful for refraction effects (particle system).
            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                // TODO: Downsampling method should be store in the renderer instead of in the asset.
                // We need to migrate this data to renderer. For now, we query the method in the active asset.
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                m_CopyColorPass.Setup(m_ActiveCameraColorAttachment, m_OpaqueColor, downsamplingMethod);
                EnqueuePass(m_CopyColorPass);
            }

            if (transparentsNeedSettingsPass)
            {
                EnqueuePass(m_TransparentSettingsPass); // Only toggle shader keywords for shadow receivers
            }

            // Must explicitely set correct depth target to the transparent pass (it will bind a different depth target otherwise).
            //m_RenderTransparentForwardPass.ConfigureTarget(m_CameraColorAttachment, m_DepthTexture);
            m_RenderTransparentForwardPass.colorAttachments.Clear();
            m_RenderTransparentForwardPass.ConfigureColorAttachment(m_CameraColorAttachment, true, true);
            m_RenderTransparentForwardPass.ConfigureDepthAttachment(m_DepthTexture, true, false);
//            // m_DrawSkyboxPass.ConfigureDepthAttachment(m_CameraDepthAttachment, true, true);
            m_RenderTransparentForwardPass.ConfigureRenderPassDescriptor(cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.msaaSamples);
            EnqueuePass(m_RenderTransparentForwardPass);
            EnqueuePass(m_OnRenderObjectCallbackPass);

            bool resolveFinalTarget = renderingData.resolveFinalTarget;
            bool hasCaptureActions = renderingData.cameraData.captureActions != null && resolveFinalTarget;
            bool afterRenderExists = hasCaptureActions || hasAfterRendering;

            bool requiresFinalPostProcessPass = applyPostProcessing &&
                                     renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            // if we have additional filters
            // we need to stay in a RT
            if (afterRenderExists)
            {
                bool willRenderFinalPass = (m_ActiveCameraColorAttachment != RenderTargetHandle.CameraTarget);
                // perform post with src / dest the same
                if (applyPostProcessing)
                {
                    m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, m_AfterPostProcessColor, m_ActiveCameraDepthAttachment, m_ColorGradingLut, requiresFinalPostProcessPass, !willRenderFinalPass);
                    EnqueuePass(m_PostProcessPass);
                }

                //now blit into the final target
                if (m_ActiveCameraColorAttachment != RenderTargetHandle.CameraTarget)
                {
                    if (renderingData.cameraData.captureActions != null)
                    {
                        m_CapturePass.Setup(m_ActiveCameraColorAttachment);
                        EnqueuePass(m_CapturePass);
                    }

                    if (requiresFinalPostProcessPass)
                    {
                        m_FinalPostProcessPass.SetupFinalPass(m_AfterPostProcessColor);
                        EnqueuePass(m_FinalPostProcessPass);
                    }
                    else
                    {
                        m_FinalBlitPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment);
                        m_FinalBlitPass.colorAttachments.Clear();

                        m_FinalBlitPass.ConfigureRenderPassDescriptor(cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.msaaSamples);
                        m_FinalBlitPass.ConfigureColorAttachment(m_ActiveCameraColorAttachment, true, false);
                        EnqueuePass(m_FinalBlitPass);
                    }
                }
            }
            else
            {
                if (applyPostProcessing)
                {
                    if (requiresFinalPostProcessPass)
                    {
                        m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, m_AfterPostProcessColor, m_ActiveCameraDepthAttachment, m_ColorGradingLut, true, false);
                        EnqueuePass(m_PostProcessPass);
                        m_FinalPostProcessPass.SetupFinalPass(m_AfterPostProcessColor);
                        EnqueuePass(m_FinalPostProcessPass);
                    }
                    else
                    {
                        m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, RenderTargetHandle.CameraTarget, m_ActiveCameraDepthAttachment, m_ColorGradingLut, false, true);
                        EnqueuePass(m_PostProcessPass);
                    }
                }
                else if (m_ActiveCameraColorAttachment != RenderTargetHandle.CameraTarget && resolveFinalTarget)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment);
                    m_FinalBlitPass.colorAttachments.Clear();
                    m_FinalBlitPass.ConfigureColorAttachment(m_ActiveCameraColorAttachment, true, false);
                    m_FinalBlitPass.ConfigureRenderPassDescriptor(cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.msaaSamples);
                    EnqueuePass(m_FinalBlitPass);
                }
            }

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                // Scene view camera should always resolve target (not stacked)
                Assertions.Assert.IsTrue(resolveFinalTarget, "Editor camera must resolve target upon finish rendering.");
                m_SceneViewDepthCopyPass.Setup(m_DepthTexture);
                EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
        }

        /// <inheritdoc />
        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);

            // Perform per-tile light culling on CPU
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

            cullingParameters.shadowDistance = cameraData.maxShadowDistance;
        }

        /// <inheritdoc />
        public override void FinishRendering(CommandBuffer cmd)
        {
            if (m_ActiveCameraColorAttachment != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(m_ActiveCameraColorAttachment.id);
                m_ActiveCameraColorAttachment = RenderTargetHandle.CameraTarget;
            }

            if (m_ActiveCameraDepthAttachment != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(m_ActiveCameraDepthAttachment.id);
                m_ActiveCameraDepthAttachment = RenderTargetHandle.CameraTarget;
            }
        }

        void EnqueueDeferred(ref RenderingData renderingData, bool requiresDepthPrepass, ScriptableRenderContext context, ref CommandBuffer cmd)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            if (requiresDepthPrepass)
            {
                m_DepthPrepass.Setup(desc, m_DepthTexture);
                m_DepthPrepass.Configure(cmd, desc);
                EnqueuePass(m_DepthPrepass);
            }

            RenderTargetHandle[] gbufferColorAttachments = new RenderTargetHandle[GBufferSlicesCount + 2];


            for (int gbufferIndex = 0; gbufferIndex < GBufferSlicesCount; ++gbufferIndex)
            {
                gbufferColorAttachments[gbufferIndex] = m_GBufferAttachments[gbufferIndex];
                gbufferColorAttachments[gbufferIndex].InitDescriptor(renderingData.cameraData.cameraTargetDescriptor.graphicsFormat);

            }

            gbufferColorAttachments[0].InitDescriptor(Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            gbufferColorAttachments[1].InitDescriptor(Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            gbufferColorAttachments[2].InitDescriptor(Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
            gbufferColorAttachments[GBufferSlicesCount] = m_ActiveCameraColorAttachment; // the last slice is the lighting buffer created in DeferredRenderer.cs
//            gbufferColorAttachments[GBufferSlicesCount + 1] = new RenderTargetHandle(); // the last slice is the lighting buffer created in DeferredRenderer.cs
            gbufferColorAttachments[GBufferSlicesCount + 1].InitDescriptor(GraphicsFormat.R32_SFloat); // the last slice is the lighting buffer created in DeferredRenderer.cs
            //gbufferColorAttachments[GBufferSlicesCount].InitDescriptor(renderingData.cameraData.cameraTargetDescriptor.colorFormat);
            m_GBufferPass.Setup(ref renderingData, m_DepthTexture, gbufferColorAttachments, requiresDepthPrepass);
            m_GBufferPass.Configure(cmd, desc);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            m_GBufferPass.m_ColorAttachments.Clear();
            m_GBufferPass.ConfigureColorAttachment(gbufferColorAttachments[0], false, false, true, 0);
            m_GBufferPass.ConfigureColorAttachment(gbufferColorAttachments[1], false, false, true, 1);
            m_GBufferPass.ConfigureColorAttachment(gbufferColorAttachments[2], false, false, true, 2);
            m_GBufferPass.ConfigureColorAttachment(gbufferColorAttachments[3], true, true, false, 3);
            #if UNITY_IOS && !UNITY_EDITOR
            m_GBufferPass.ConfigureColorAttachment(gbufferColorAttachments[4], false, false, true, 4);
            #endif

            m_GBufferPass.ConfigureDepthAttachment(m_DepthTexture, false, true, true);
            m_GBufferPass.ConfigureRenderPassDescriptor(desc.width, desc.height, desc.msaaSamples);

            EnqueuePass(m_GBufferPass);

//            gbufferColorAttachments[0] = m_GBufferPass.colorAttachments[0];
//            gbufferColorAttachments[1] = m_GBufferPass.colorAttachments[1];
//            gbufferColorAttachments[2] = m_GBufferPass.colorAttachments[2];
//            gbufferColorAttachments[3] = m_GBufferPass.colorAttachments[3];
//            m_CopyDepthTempPass.m_ColorAttachments.Clear();
//            m_CopyDepthTempPass.Setup(m_DepthTexture, m_DepthCopyTexture);
//            m_CopyDepthTempPass.Configure(cmd, desc);
//            context.ExecuteCommandBuffer(cmd);
//            cmd.Clear();
           // m_CopyDepthTempPass.ConfigureInputAttachment(m_DepthTexture, 0);
            //m_CopyDepthTempPass.ConfigureColorAttachment(m_DepthCopyTexture, false, true, true);
            //m_CopyDepthTempPass.ConfigureRenderPassDescriptor(desc.width, desc.height, desc.msaaSamples);
            //EnqueuePass(m_CopyDepthTempPass);

            m_DeferredLights.Setup(ref renderingData, m_AdditionalLightsShadowCasterPass, m_DepthCopyTexture, m_DepthInfoTexture, m_TileDepthInfoTexture, m_DepthTexture, gbufferColorAttachments);
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
            m_DeferredPass.Configure(cmd, desc);
            m_DeferredPass.ConfigureColorAttachment(m_DeferredLights.m_GbufferColorAttachments[3], true, true, false, 0);

            m_DeferredPass.ConfigureInputAttachment(m_GBufferPass.colorAttachments[0], 0);
            m_DeferredPass.ConfigureInputAttachment(m_GBufferPass.colorAttachments[1], 1);
            m_DeferredPass.ConfigureInputAttachment(m_GBufferPass.colorAttachments[2], 2);
#if UNITY_IOS && !UNITY_EDITOR
            m_DeferredPass.ConfigureInputAttachment(m_GBufferPass.colorAttachments[4], 3);
#else
            m_DeferredPass.ConfigureInputAttachment(m_GBufferPass.depthAttachment, 3);
#endif
            //m_DeferredPass.ConfigureDepthAttachment(m_GBufferPass.depthAttachment, true, true, false);
            EnqueuePass(m_DeferredPass);
            //EnqueuePass(m_RenderTransparentForwardPass);

            // Must explicitely set correct depth target to the transparent pass (it will bind a different depth target otherwise).
            //m_RenderOpaqueForwardOnlyPass.ConfigureTarget(m_CameraColorAttachment, m_DepthTexture);

//            m_RenderOpaqueForwardOnlyPass.ConfigureColorAttachment(m_CameraColorAttachment, true, true);
//            m_RenderOpaqueForwardOnlyPass.ConfigureDepthAttachment(m_DepthTexture, true, true);
//            m_RenderTransparentForwardPass.ConfigureRenderPassDescriptor(desc.width, desc.height, desc.msaaSamples);
            //EnqueuePass(m_RenderOpaqueForwardOnlyPass);

        }

        void CreateCameraRenderTarget(ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_CreateCameraTextures);
            var descriptor = cameraData.cameraTargetDescriptor;
            int msaaSamples = descriptor.msaaSamples;
            if (m_ActiveCameraColorAttachment != RenderTargetHandle.CameraTarget)
            {
                bool useDepthRenderBuffer = m_ActiveCameraDepthAttachment == RenderTargetHandle.CameraTarget;
                var colorDescriptor = descriptor; // Camera decides if HDR format is needed. ScriptableRenderPipelineCore.cs decides between FP16 and R11G11B10.
                colorDescriptor.depthBufferBits = (useDepthRenderBuffer) ? k_DepthStencilBufferBits : 0;
                cmd.GetTemporaryRT(m_ActiveCameraColorAttachment.id, colorDescriptor, FilterMode.Bilinear);
            }

            if (m_ActiveCameraDepthAttachment != RenderTargetHandle.CameraTarget)
            {
                var depthDescriptor = descriptor;
                depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                depthDescriptor.depthBufferBits = k_DepthStencilBufferBits;
                depthDescriptor.bindMS = msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                cmd.GetTemporaryRT(m_ActiveCameraDepthAttachment.id, depthDescriptor, FilterMode.Point);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupBackbufferFormat(int msaaSamples, bool stereo)
        {
#if ENABLE_VR && ENABLE_VR_MODULE
            bool msaaSampleCountHasChanged = false;
            int currentQualitySettingsSampleCount = QualitySettings.antiAliasing;
            if (currentQualitySettingsSampleCount != msaaSamples &&
                !(currentQualitySettingsSampleCount == 0 && msaaSamples == 1))
            {
                msaaSampleCountHasChanged = true;
            }

            // There's no exposed API to control how a backbuffer is created with MSAA
            // By settings antiAliasing we match what the amount of samples in camera data with backbuffer
            // We only do this for the main camera and this only takes effect in the beginning of next frame.
            // This settings should not be changed on a frame basis so that's fine.
            QualitySettings.antiAliasing = msaaSamples;

            if (stereo && msaaSampleCountHasChanged)
                XR.XRDevice.UpdateEyeTextureMSAASetting();
#else
            QualitySettings.antiAliasing = msaaSamples;
#endif
        }
        
        bool RequiresIntermediateColorTexture(ref RenderingData renderingData, RenderTextureDescriptor baseDescriptor)
        {
            // When rendering a camera stack we always create an intermediate render texture to composite camera results.
            // We create it upon rendering the Base camera.
            if (renderingData.cameraData.renderType == CameraRenderType.Base && !renderingData.resolveFinalTarget)
                return true;

            // Only base cameras create working intermediate render texture
            // Overlay cameras will composite on top of the working texture provided by base camera.
            if (renderingData.cameraData.renderType != CameraRenderType.Base)
                return false;

            ref CameraData cameraData = ref renderingData.cameraData;
            int msaaSamples = cameraData.cameraTargetDescriptor.msaaSamples;
            bool isStereoEnabled = renderingData.cameraData.isStereoEnabled;
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isCompatibleBackbufferTextureDimension = baseDescriptor.dimension == TextureDimension.Tex2D;
            bool requiresExplicitMsaaResolve = msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve;
            bool isOffscreenRender = cameraData.targetTexture != null && !cameraData.isSceneViewCamera;
            bool isCapturing = cameraData.captureActions != null;

#if ENABLE_VR && ENABLE_VR_MODULE
            if (isStereoEnabled)
                isCompatibleBackbufferTextureDimension = UnityEngine.XR.XRSettings.deviceEyeTextureDimension == baseDescriptor.dimension;
#endif

            bool requiresBlitForOffscreenCamera = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve;
            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                   !isCompatibleBackbufferTextureDimension || !cameraData.isDefaultViewport || isCapturing || Display.main.requiresBlitToBackbuffer;
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
    }
}
