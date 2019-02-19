namespace UnityEngine.Rendering.LWRP
{
    internal class ForwardRenderer : ScriptableRenderer
    {
        private DepthOnlyPass m_DepthPrepass;
        private MainLightShadowCasterPass m_MainLightShadowCasterPass;
        private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
        private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
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

            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass(RenderPassEvent.BeforeRendering);
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRendering);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRendering);
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque);
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass(RenderPassEvent.BeforeRenderingOpaques, screenspaceShadowsMaterial);
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_OpaquePostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingOpaques + 9, true);
            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.AfterRenderingOpaques + 9);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingSkybox, copyDepthMaterial);
            m_CopyColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, samplingMaterial, downsamplingMethod);
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass(RenderPassEvent.AfterRenderingSkybox, RenderQueueRange.transparent, data.transparentLayerMask);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingSkybox);
            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering + 9);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 9, blitMaterial);

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

            bool mainLightShadows = m_MainLightShadowCasterPass.ShouldExecute(ref renderingData);
            bool resolveShadowsInScreenSpace = mainLightShadows && m_ScreenSpaceShadowResolvePass.ShouldExecute(ref renderingData);
            bool requiresDepthPrepass = resolveShadowsInScreenSpace || m_DepthPrepass.ShouldExecute(ref renderingData);
            bool createColorTexture = RequiresIntermediateColorTexture(ref renderingData, cameraTargetDescriptor)
                                      || m_RendererFeatures.Count != 0;

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read
            // later by effect requiring it.
            bool createDepthTexture = renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass;

            RenderTargetHandle colorHandle = (createColorTexture) ? m_ColorAttachment : RenderTargetHandle.CameraTarget;
            RenderTargetHandle depthHandle = (createDepthTexture) ? m_DepthAttachment : RenderTargetHandle.CameraTarget;

            int customRenderPassIndex = 0;
            for (int i = 0; i < m_RendererFeatures.Count; ++i)
            {
                m_RendererFeatures[i].AddRenderPasses(m_AdditionalRenderPasses, cameraTargetDescriptor, colorHandle, depthHandle);
            }
            m_AdditionalRenderPasses.Sort();

            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);

            if (m_MainLightShadowCasterPass.ShouldExecute(ref renderingData))
                EnqueuePass(m_AdditionalLightsShadowCasterPass);

            if (createColorTexture || createDepthTexture)
            {
                int msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
                m_CreateLightweightRenderTexturesPass.Setup(cameraTargetDescriptor, colorHandle, depthHandle, msaaSamples);
                EnqueuePass(m_CreateLightweightRenderTexturesPass);
            }

            bool beforeRenderOpaquesPasses = EnqueueAdditionalRenderPasses(RenderPassEvent.BeforeRenderingOpaques, ref customRenderPassIndex,
                ref renderingData);

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

            // If a before all render pass executed we expect it to clear the color render target
            if (beforeRenderOpaquesPasses)
                clearFlag = ClearFlag.None;

            m_RenderOpaqueForwardPass.Setup(cameraTargetDescriptor, colorHandle, depthHandle, clearFlag, camera.backgroundColor);
            EnqueuePass(m_RenderOpaqueForwardPass);

            bool afterOpaques = EnqueueAdditionalRenderPasses(RenderPassEvent.AfterRenderingOpaques, ref customRenderPassIndex,
                ref renderingData);

            if (m_OpaquePostProcessPass.ShouldExecute(ref renderingData))
                m_OpaquePostProcessPass.Setup(cameraTargetDescriptor, colorHandle, colorHandle);

            if (m_DrawSkyboxPass.ShouldExecute(ref renderingData))
            {
                // We can't combine skybox and render opaques passes if there's a custom render pass in between
                // them. Ideally we need a render graph here that each render pass declares inputs and output
                // attachments and their Load/Store action so we figure out properly if we can combine passes
                // and move to interleaved rendering with RenderPass API.
                m_DrawSkyboxPass.Setup(cameraTargetDescriptor, colorHandle, depthHandle, !afterOpaques);
                EnqueuePass(m_DrawSkyboxPass);
            }

            EnqueueAdditionalRenderPasses(RenderPassEvent.AfterRenderingSkybox, ref customRenderPassIndex,
                ref renderingData);

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer
            if (createDepthTexture)
            {
                m_CopyDepthPass.Setup(depthHandle, m_DepthTexture);
                EnqueuePass(m_CopyDepthPass);
            }

            if (m_CopyColorPass.ShouldExecute(ref renderingData))
            {
                m_CopyColorPass.Setup(colorHandle, m_OpaqueColor);
                EnqueuePass(m_CopyColorPass);
            }

            m_RenderTransparentForwardPass.Setup(cameraTargetDescriptor, colorHandle, depthHandle);
            EnqueuePass(m_RenderTransparentForwardPass);

            EnqueueAdditionalRenderPasses(RenderPassEvent.AfterRenderingTransparentPasses, ref customRenderPassIndex,
                ref renderingData);


            bool afterRenderExists = renderingData.cameraData.captureActions != null ||
                                     AfterRenderExists(customRenderPassIndex);

            // if we have additional filters
            // we need to stay in a RT
            if (afterRenderExists)
            {
                // perform post with src / dest the same
                if (m_PostProcessPass.ShouldExecute(ref renderingData))
                {
                    m_PostProcessPass.Setup(cameraTargetDescriptor, colorHandle, colorHandle);
                    EnqueuePass(m_PostProcessPass);
                }

                EnqueueAdditionalRenderPasses(RenderPassEvent.AfterRendering, ref customRenderPassIndex,
                    ref renderingData);

                //now blit into the final target
                if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    if (m_CapturePass.ShouldExecute(ref renderingData))
                    {
                        m_CapturePass.Setup(colorHandle);
                        EnqueuePass(m_CapturePass);
                    }

                    m_FinalBlitPass.Setup(cameraTargetDescriptor, colorHandle);
                    EnqueuePass(m_FinalBlitPass);
                }
            }
            else
            {
                if (m_PostProcessPass.ShouldExecute(ref renderingData))
                {
                    m_PostProcessPass.Setup(cameraTargetDescriptor, colorHandle, RenderTargetHandle.CameraTarget);
                    EnqueuePass(m_PostProcessPass);
                }
                else if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, colorHandle);
                    EnqueuePass(m_FinalBlitPass);
                }
            }

#if UNITY_EDITOR
            if (m_SceneViewDepthCopyPass.ShouldExecute(ref renderingData))
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
            bool isInstancedStereo = cameraData.isStereoEnabled && (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassInstanced);

            bool requiresBlitForOffscreenCamera = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve;
            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                   (isTargetTexture2DArray && !isInstancedStereo) || !cameraData.isDefaultViewport || isCapturing || Display.main.requiresBlitToBackbuffer
                   || renderingData.killAlphaInFinalBlit;
        }

        bool AfterRenderExists(int currIndex)
        {
            for (int i = currIndex; i < m_AdditionalRenderPasses.Count; ++i)
            {
                if (m_AdditionalRenderPasses[i].renderPassEvent == RenderPassEvent.AfterRendering)
                    return true;
            }

            return false;
        }
    }
}
