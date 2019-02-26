namespace UnityEngine.Rendering.LWRP
{
    internal class ForwardRenderer : ScriptableRenderer
    {
        private DepthOnlyPass m_DepthPrepass;
        private MainLightShadowCasterPass m_MainLightShadowCasterPass;
        private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
        private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;
        private PostProcessPass m_OpaquePostProcessPass;
        private DrawSkyboxPass m_DrawSkyboxPass;
        private CopyDepthPass m_CopyDepthPass;
        private CopyColorPass m_CopyColorPass;
        private RenderTransparentForwardPass m_RenderTransparentForwardPass;
        private PostProcessPass m_PostProcessPass;
        private FinalBlitPass m_FinalBlitPass;
        private CapturePass m_CapturePass;

#if UNITY_EDITOR
        private SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif

        RenderTargetHandle m_ColorAttachment;
        RenderTargetHandle m_DepthAttachment;
        RenderTargetHandle m_DepthTexture;
        RenderTargetHandle m_OpaqueColor;

        ForwardLights m_ForwardLights;

        public ForwardRenderer(ForwardRendererData data) : base(data)
        {
            Downsampling downsamplingMethod = LightweightRenderPipeline.asset.opaqueDownsampling;

            Material blitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            Material copyDepthMaterial = CoreUtils.CreateEngineMaterial(data.copyDepthShader);
            Material samplingMaterial = CoreUtils.CreateEngineMaterial(data.samplingShader);
            Material screenspaceShadowsMaterial = CoreUtils.CreateEngineMaterial(data.screenSpaceShadowShader);

            // Note: Why the +1 in render block? Because if use these events for the built-in passes as well.
            // F.ex the main light shadow map pass. We set it to inject at BeforeRenderingShadows + 1 so it render
            // between before and after shadows.
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows + 1);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows + 1);
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrepasses + 1, RenderQueueRange.opaque);
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass(RenderPassEvent.BeforeRenderingPrepasses + 1, screenspaceShadowsMaterial);
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.BeforeRenderingOpaques + 1, copyDepthMaterial);
            m_OpaquePostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingOpaques + 1, true);
            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox + 1);
            m_CopyColorPass = new CopyColorPass(RenderPassEvent.BeforeRenderingSkybox + 1, samplingMaterial, downsamplingMethod);
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass(RenderPassEvent.BeforeRenderingTransparents + 1, RenderQueueRange.transparent, data.transparentLayerMask);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing + 1);
            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering + 1);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, blitMaterial);

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(RenderPassEvent.AfterRendering + 9, copyDepthMaterial);
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_ColorAttachment.Init("_CameraColorTexture");
            m_DepthAttachment.Init("_CameraDepthAttachment");
            m_DepthTexture.Init("_CameraDepthTexture");
            m_OpaqueColor.Init("_CameraOpaqueTexture");
            m_ForwardLights = new ForwardLights();
        }

        public override void Setup(ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            ClearFlag clearFlag = GetCameraClearFlag(camera.clearFlags);

            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);
            bool resolveShadowsInScreenSpace = mainLightShadows && renderingData.shadowData.requiresScreenSpaceShadowResolve;
            
            // Depth prepass is generated in the following cases:
            // - We resolve shadows in screen space
            // - Scene view camera always requires a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            bool requiresDepthPrepass = renderingData.cameraData.isSceneViewCamera ||
                (renderingData.cameraData.requiresDepthTexture && (!CanCopyDepth(ref renderingData.cameraData)));
            requiresDepthPrepass |= resolveShadowsInScreenSpace;
            bool createColorTexture = RequiresIntermediateColorTexture(ref renderingData, cameraTargetDescriptor)
                                      || m_RendererFeatures.Count != 0;

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read
            // later by effect requiring it.
            bool createDepthTexture = renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass;
            bool postProcessEnabled = renderingData.cameraData.postProcessEnabled;
            bool hasOpaquePostProcess = postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(RenderingUtils.postProcessRenderContext);

            cameraColorHandle = (createColorTexture) ? m_ColorAttachment : RenderTargetHandle.CameraTarget;
            cameraDepthHandle = (createDepthTexture) ? m_DepthAttachment : RenderTargetHandle.CameraTarget;

            for (int i = 0; i < m_RendererFeatures.Count; ++i)
            {
                m_RendererFeatures[i].AddRenderPasses(m_ActiveRenderPassQueue, cameraTargetDescriptor, cameraColorHandle, cameraDepthHandle);
            }
            m_ActiveRenderPassQueue.Sort();

            bool hasAfterRendering = m_ActiveRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRendering) != null;

            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);

            if (additionalLightShadows)
                EnqueuePass(m_AdditionalLightsShadowCasterPass);

            if (requiresDepthPrepass)
            {
                m_DepthPrepass.Setup(cameraTargetDescriptor, m_DepthTexture);
                EnqueuePass(m_DepthPrepass);
            }

            if (resolveShadowsInScreenSpace)
            {
                m_ScreenSpaceShadowResolvePass.Setup(cameraTargetDescriptor);
                EnqueuePass(m_ScreenSpaceShadowResolvePass);
            }

            EnqueuePass(m_RenderOpaqueForwardPass);

            if (hasOpaquePostProcess)
                m_OpaquePostProcessPass.Setup(cameraTargetDescriptor, cameraColorHandle, cameraColorHandle);

            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
                EnqueuePass(m_DrawSkyboxPass);

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer
            if (createDepthTexture)
            {
                m_CopyDepthPass.Setup(cameraDepthHandle, m_DepthTexture);
                EnqueuePass(m_CopyDepthPass);
            }

            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                m_CopyColorPass.Setup(cameraColorHandle, m_OpaqueColor);
                EnqueuePass(m_CopyColorPass);
            }

            EnqueuePass(m_RenderTransparentForwardPass);

            bool afterRenderExists = renderingData.cameraData.captureActions != null ||
                                     hasAfterRendering;

            // if we have additional filters
            // we need to stay in a RT
            if (afterRenderExists)
            {
                // perform post with src / dest the same
                if (postProcessEnabled)
                {
                    m_PostProcessPass.Setup(cameraTargetDescriptor, cameraColorHandle, cameraColorHandle);
                    EnqueuePass(m_PostProcessPass);
                }

                //now blit into the final target
                if (cameraColorHandle != RenderTargetHandle.CameraTarget)
                {
                    if (renderingData.cameraData.captureActions != null)
                    {
                        m_CapturePass.Setup(cameraColorHandle);
                        EnqueuePass(m_CapturePass);
                    }

                    m_FinalBlitPass.Setup(cameraTargetDescriptor, cameraColorHandle);
                    EnqueuePass(m_FinalBlitPass);
                }
            }
            else
            {
                if (postProcessEnabled)
                {
                    m_PostProcessPass.Setup(cameraTargetDescriptor, cameraColorHandle, RenderTargetHandle.CameraTarget);
                    EnqueuePass(m_PostProcessPass);
                }
                else if (cameraColorHandle != RenderTargetHandle.CameraTarget)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, cameraColorHandle);
                    EnqueuePass(m_FinalBlitPass);
                }
            }

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_SceneViewDepthCopyPass.Setup(m_DepthTexture);
                EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {
            Camera camera = cameraData.camera;
            // TODO: PerObjectCulling also affect reflection probes. Enabling it for now.
            // if (asset.additionalLightsRenderingMode == LightRenderingMode.Disabled ||
            //     asset.maxAdditionalLightsCount == 0)
            // {
            //     cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            // }

            cullingParameters.shadowDistance = Mathf.Min(cameraData.maxShadowDistance, camera.farClipPlane);
        }

        bool RequiresIntermediateColorTexture(ref RenderingData renderingData, RenderTextureDescriptor baseDescriptor)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            int msaaSamples = cameraData.cameraTargetDescriptor.msaaSamples;
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isTargetTexture2DArray = baseDescriptor.dimension == TextureDimension.Tex2DArray;
            bool requiresExplicitMsaaResolve = msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve;
            bool isOffscreenRender = cameraData.camera.targetTexture != null && !cameraData.isSceneViewCamera;
            bool isCapturing = cameraData.captureActions != null;

            bool requiresBlitForOffscreenCamera = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve;
            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                   isTargetTexture2DArray || !cameraData.isDefaultViewport || isCapturing || Display.main.requiresBlitToBackbuffer
                   || renderingData.killAlphaInFinalBlit;
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = cameraData.cameraTargetDescriptor.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
        }
    }
}
