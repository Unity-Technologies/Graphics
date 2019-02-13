using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    internal class ForwardRendererSetup : RendererSetup
    {
        private DepthOnlyPass m_DepthOnlyPass;
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

        List<ScriptableRenderPass> m_AdditionalRenderPasses = new List<ScriptableRenderPass>(10);

        public ForwardRendererSetup(ForwardRendererData data) : base(data)
        {
            Material blitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            Material copyDepthMaterial = CoreUtils.CreateEngineMaterial(data.copyDepthShader);
            Material samplingMaterial = CoreUtils.CreateEngineMaterial(data.samplingShader);
            Material screenspaceShadowsMaterial = CoreUtils.CreateEngineMaterial(data.screenSpaceShadowShader);

            m_DepthOnlyPass = new DepthOnlyPass(RenderQueueRange.opaque);
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass();
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass();
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass(screenspaceShadowsMaterial);
            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(RenderQueueRange.opaque, data.opaqueLayerMask);
            m_OpaquePostProcessPass = new PostProcessPass(true);
            m_DrawSkyboxPass = new DrawSkyboxPass();
            m_CopyDepthPass = new CopyDepthPass(copyDepthMaterial);
            m_CopyColorPass = new CopyColorPass(samplingMaterial);
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass(RenderQueueRange.transparent, data.transparentLayerMask);
            m_PostProcessPass = new PostProcessPass();
            m_FinalBlitPass = new FinalBlitPass(blitMaterial);
            m_CapturePass = new CapturePass();

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(copyDepthMaterial);
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_ColorAttachment.Init("_CameraColorTexture");
            m_DepthAttachment.Init("_CameraDepthAttachment");
            m_DepthTexture.Init("_CameraDepthTexture");
            m_OpaqueColor.Init("_CameraOpaqueTexture");
            m_ForwardLights = new ForwardLights();
        }

        public static bool RequiresIntermediateColorTexture(ref RenderingData renderingData, RenderTextureDescriptor baseDescriptor)
        {
            CameraData cameraData = renderingData.cameraData;
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isTargetTexture2DArray = baseDescriptor.dimension == TextureDimension.Tex2DArray;
            bool requiresExplicitMsaaResolve = cameraData.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve;
            bool isOffscreenRender = cameraData.camera.targetTexture != null && !cameraData.isSceneViewCamera;
            bool isCapturing = cameraData.captureActions != null;

            bool requiresBlitForOffscreenCamera = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve;
            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                isTargetTexture2DArray || !cameraData.isDefaultViewport || isCapturing || Display.main.requiresBlitToBackbuffer
                    || renderingData.killAlphaInFinalBlit;
        }

        public override void Setup(ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            RenderTextureDescriptor baseDescriptor = ScriptableRenderPass.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            ClearFlag clearFlag = GetCameraClearFlag(renderingData.cameraData.camera);

            bool mainLightShadows = m_MainLightShadowCasterPass.ShouldExecute(ref renderingData);
            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass, RenderPassBlock.BeforeMainRender);

            if (m_MainLightShadowCasterPass.ShouldExecute(ref renderingData))
                EnqueuePass(m_AdditionalLightsShadowCasterPass, RenderPassBlock.BeforeMainRender);

            bool resolveShadowsInScreenSpace = mainLightShadows && m_ScreenSpaceShadowResolvePass.ShouldExecute(ref renderingData);
            bool requiresDepthPrepass = resolveShadowsInScreenSpace || m_DepthOnlyPass.ShouldExecute(ref renderingData);

            // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
            requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;

            bool createColorTexture = RequiresIntermediateColorTexture(ref renderingData, baseDescriptor)
                                      || m_RenderPassFeatures.Count != 0;

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read
            // later by effect requiring it.
            bool createDepthTexture = renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass;

            RenderTargetHandle colorHandle = (createColorTexture) ? m_ColorAttachment : RenderTargetHandle.CameraTarget;
            RenderTargetHandle depthHandle = (createDepthTexture) ? m_DepthAttachment : RenderTargetHandle.CameraTarget;

            var sampleCount = (SampleCount) renderingData.cameraData.msaaSamples;
            if (createColorTexture || createDepthTexture)
            {
                m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
                EnqueuePass(m_CreateLightweightRenderTexturesPass);
            }

            m_AdditionalRenderPasses.Clear();
            for (int i = 0; i < m_RenderPassFeatures.Count; ++i)
            {
                m_RenderPassFeatures[i].AddRenderPasses(m_AdditionalRenderPasses, baseDescriptor, colorHandle, depthHandle);
            }
            m_AdditionalRenderPasses.Sort();
            int customRenderPassIndex = 0;

            bool beforeRenderOpaquesPasses = EnqueuePasses(RenderPassEvent.BeforeRenderingOpaques, m_AdditionalRenderPasses, ref customRenderPassIndex,
                ref renderingData);

            if (requiresDepthPrepass)
            {
                m_DepthOnlyPass.Setup(baseDescriptor, m_DepthTexture);
                EnqueuePass(m_DepthOnlyPass);
            }

            if (resolveShadowsInScreenSpace)
            {
                m_ScreenSpaceShadowResolvePass.Setup(baseDescriptor);
                EnqueuePass(m_ScreenSpaceShadowResolvePass);
            }

            // If a before all render pass executed we expect it to clear the color render target
            if (beforeRenderOpaquesPasses)
                clearFlag = ClearFlag.None;

            m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, clearFlag, camera.backgroundColor);
            EnqueuePass(m_RenderOpaqueForwardPass);

            bool afterOpaques = EnqueuePasses(RenderPassEvent.AfterRenderingOpaques, m_AdditionalRenderPasses, ref customRenderPassIndex,
                ref renderingData);

            if (m_OpaquePostProcessPass.ShouldExecute(ref renderingData))
            {
                m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle, colorHandle);

                afterOpaques |= EnqueuePasses(RenderPassEvent.AfterRenderingOpaquePostProcessing, m_AdditionalRenderPasses, ref customRenderPassIndex,
                    ref renderingData);
            }

            if (m_DrawSkyboxPass.ShouldExecute(ref renderingData))
            {
                // We can't combine skybox and render opaques passes if there's a custom render pass in between
                // them. Ideally we need a render graph here that each render pass declares inputs and output
                // attachments and their Load/Store action so we figure out properly if we can combine passes
                // and move to interleaved rendering with RenderPass API.
                m_DrawSkyboxPass.Setup(baseDescriptor, colorHandle, depthHandle, !afterOpaques);
                EnqueuePass(m_DrawSkyboxPass);
            }

            EnqueuePasses(RenderPassEvent.AfterRenderingSkybox, m_AdditionalRenderPasses, ref customRenderPassIndex,
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

            m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle);
            EnqueuePass(m_RenderTransparentForwardPass);

            EnqueuePasses(RenderPassEvent.AfterRenderingTransparentPasses, m_AdditionalRenderPasses, ref customRenderPassIndex,
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
                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, colorHandle);
                    EnqueuePass(m_PostProcessPass, RenderPassBlock.AfterMainRender);
                }

                EnqueuePasses(RenderPassEvent.AfterRendering, m_AdditionalRenderPasses, ref customRenderPassIndex,
                    ref renderingData);

                //now blit into the final target
                if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    if (m_CapturePass.ShouldExecute(ref renderingData))
                    {
                        m_CapturePass.Setup(colorHandle);
                        EnqueuePass(m_CapturePass, RenderPassBlock.AfterMainRender);
                    }

                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                    EnqueuePass(m_FinalBlitPass, RenderPassBlock.AfterMainRender);
                }
            }
            else
            {
                if (m_PostProcessPass.ShouldExecute(ref renderingData))
                {
                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, RenderTargetHandle.CameraTarget);
                    EnqueuePass(m_PostProcessPass, RenderPassBlock.AfterMainRender);
                }
                else if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                    EnqueuePass(m_FinalBlitPass, RenderPassBlock.AfterMainRender);
                }
            }

#if UNITY_EDITOR
            if (m_SceneViewDepthCopyPass.ShouldExecute(ref renderingData))
            {
                m_SceneViewDepthCopyPass.Setup(m_DepthTexture);
                EnqueuePass(m_SceneViewDepthCopyPass, RenderPassBlock.AfterMainRender);
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
