using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class ForwardRendererSetup : IRendererSetup
    {
        private DepthOnlyPass m_DepthOnlyPass;
        private MainLightShadowCasterPass m_MainLightShadowCasterPass;
        private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        private SetupForwardRenderingPass m_SetupForwardRenderingPass;
        private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
        private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
        private BeginXRRenderingPass m_BeginXrRenderingPass;
        private SetupLightweightConstanstPass m_SetupLightweightConstants;
        private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;
        private PostProcessPass m_OpaquePostProcessPass;
        private DrawSkyboxPass m_DrawSkyboxPass;
        private CopyDepthPass m_CopyDepthPass;
        private CopyColorPass m_CopyColorPass;
        private RenderTransparentForwardPass m_RenderTransparentForwardPass;
        private PostProcessPass m_PostProcessPass;
        private CreateColorRenderTexturesPass m_createColorPass;
        private FinalBlitPass m_FinalBlitPass;
        private CapturePass m_CapturePass;
        private EndXRRenderingPass m_EndXrRenderingPass;

#if UNITY_EDITOR
        private GizmoRenderingPass m_LitGizmoRenderingPass;
        private GizmoRenderingPass m_UnlitGizmoRenderingPass;
        private SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif

        private RenderTargetHandle m_ColorAttachment;
        private RenderTargetHandle m_ColorAttachmentAfterOpaquePost;
        private RenderTargetHandle m_ColorAttachmentAfterTransparentPost;
        private RenderTargetHandle m_DepthAttachment;
        private RenderTargetHandle m_DepthTexture;
        private RenderTargetHandle m_OpaqueColor;
        private RenderTargetHandle m_MainLightShadowmap;
        private RenderTargetHandle m_AdditionalLightsShadowmap;
        private RenderTargetHandle m_ScreenSpaceShadowmap;

        public ForwardRendererSetup(ForwardRendererData data)
        {
            Material blitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            Material copyDepthMaterial = CoreUtils.CreateEngineMaterial(data.copyDepthShader);
            Material samplingMaterial = CoreUtils.CreateEngineMaterial(data.samplingShader);
            Material screenspaceShadowsMaterial = CoreUtils.CreateEngineMaterial(data.screenSpaceShadowShader);

            m_DepthOnlyPass = new DepthOnlyPass();
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass();
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass();
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass();
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass(screenspaceShadowsMaterial);
            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
            m_BeginXrRenderingPass = new BeginXRRenderingPass();
            m_SetupLightweightConstants = new SetupLightweightConstanstPass();
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
            m_OpaquePostProcessPass = new PostProcessPass();
            m_DrawSkyboxPass = new DrawSkyboxPass();
            m_CopyDepthPass = new CopyDepthPass(copyDepthMaterial);
            m_CopyColorPass = new CopyColorPass(samplingMaterial);
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass();
            m_PostProcessPass = new PostProcessPass();
            m_FinalBlitPass = new FinalBlitPass(blitMaterial);
            m_CapturePass = new CapturePass();
            m_EndXrRenderingPass = new EndXRRenderingPass();

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(copyDepthMaterial);
            m_LitGizmoRenderingPass = new GizmoRenderingPass();
            m_UnlitGizmoRenderingPass = new GizmoRenderingPass();
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_ColorAttachment.Init("_CameraColorTexture");
            m_ColorAttachmentAfterOpaquePost.Init("_CameraColorTextureAfterOpaquePost");
            m_ColorAttachmentAfterTransparentPost.Init("_CameraColorTextureAfterTransparentPost");
            m_DepthAttachment.Init("_CameraDepthAttachment");
            m_DepthTexture.Init("_CameraDepthTexture");
            m_OpaqueColor.Init("_CameraOpaqueTexture");
            m_MainLightShadowmap.Init("_MainLightShadowmapTexture");
            m_AdditionalLightsShadowmap.Init("_AdditionalLightsShadowmapTexture");
            m_ScreenSpaceShadowmap.Init("_ScreenSpaceShadowmapTexture");
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

        List<IBeforeRender> m_BeforeRenderPasses = new List<IBeforeRender>(10);
        List<IAfterOpaquePass> m_AfterOpaquePasses = new List<IAfterOpaquePass>(10);
        List<IAfterOpaquePostProcess> m_AfterOpaquePostProcessPasses = new List<IAfterOpaquePostProcess>(10);
        List<IAfterSkyboxPass> m_AfterSkyboxPasses = new List<IAfterSkyboxPass>(10);
        List<IAfterTransparentPass> m_AfterTransparentPasses = new List<IAfterTransparentPass>(10);
        List<IAfterRender> m_AfterRenderPasses = new List<IAfterRender>(10);

        public override void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            renderer.SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
            RenderTextureDescriptor baseDescriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            ClearFlag clearFlag = ScriptableRenderer.GetCameraClearFlag(renderingData.cameraData.camera);
            RenderTextureDescriptor shadowDescriptor = baseDescriptor;
            shadowDescriptor.dimension = TextureDimension.Tex2D;

            bool mainLightShadows = false;
            if (renderingData.shadowData.supportsMainLightShadows)
            {
                mainLightShadows = m_MainLightShadowCasterPass.Setup(m_MainLightShadowmap, ref renderingData);
                if (mainLightShadows)
                    renderer.EnqueuePass(m_MainLightShadowCasterPass);
            }

            if (renderingData.shadowData.supportsAdditionalLightShadows)
            {
                bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(m_AdditionalLightsShadowmap, ref renderingData, renderer.maxVisibleAdditionalLights);
                if (additionalLightShadows)
                    renderer.EnqueuePass(m_AdditionalLightsShadowCasterPass);
            }

            bool resolveShadowsInScreenSpace = mainLightShadows && renderingData.shadowData.requiresScreenSpaceShadowResolve;

            // Depth prepass is generated in the following cases:
            // - We resolve shadows in screen space
            // - Scene view camera always requires a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            bool requiresDepthPrepass = resolveShadowsInScreenSpace ||
                                        renderingData.cameraData.isSceneViewCamera ||
                                        (renderingData.cameraData.requiresDepthTexture && (!CanCopyDepth(ref renderingData.cameraData)));

            // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
            requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            camera.GetComponents(m_BeforeRenderPasses);
            camera.GetComponents(m_AfterOpaquePasses);
            camera.GetComponents(m_AfterOpaquePostProcessPasses);
            camera.GetComponents(m_AfterSkyboxPasses);
            camera.GetComponents(m_AfterTransparentPasses);
            camera.GetComponents(m_AfterRenderPasses);

            bool createColorTexture = RequiresIntermediateColorTexture(ref renderingData, baseDescriptor)
                    || m_BeforeRenderPasses.Count != 0
                    || m_AfterOpaquePasses.Count != 0
                    || m_AfterOpaquePostProcessPasses.Count != 0
                    || m_AfterSkyboxPasses.Count != 0
                    || m_AfterTransparentPasses.Count != 0
                    || m_AfterRenderPasses.Count != 0;

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read
            // later by effect requiring it.
            bool createDepthTexture = renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass;

            RenderTargetHandle colorHandle = (createColorTexture) ? m_ColorAttachment : RenderTargetHandle.CameraTarget;
            RenderTargetHandle depthHandle = (createDepthTexture) ? m_DepthAttachment : RenderTargetHandle.CameraTarget;

            var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
            if (createColorTexture || createDepthTexture)
            {
                m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
                renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);
            }

            foreach (var pass in m_BeforeRenderPasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle, clearFlag));

            if (requiresDepthPrepass)
            {
                m_DepthOnlyPass.Setup(baseDescriptor, m_DepthTexture);
                renderer.EnqueuePass(m_DepthOnlyPass);
            }

            if (resolveShadowsInScreenSpace)
            {
                m_ScreenSpaceShadowResolvePass.Setup(baseDescriptor, m_ScreenSpaceShadowmap);
                renderer.EnqueuePass(m_ScreenSpaceShadowResolvePass);
            }

            if (renderingData.cameraData.isStereoEnabled)
                renderer.EnqueuePass(m_BeginXrRenderingPass);

            var perObjectFlags = ScriptableRenderer.GetPerObjectLightFlags(renderingData.lightData.mainLightIndex, renderingData.lightData.additionalLightsCount);

            m_SetupLightweightConstants.Setup(renderer.maxVisibleAdditionalLights, renderer.perObjectLightIndices);
            renderer.EnqueuePass(m_SetupLightweightConstants);

            // If a before all render pass executed we expect it to clear the color render target
            if (m_BeforeRenderPasses.Count != 0)
                clearFlag = ClearFlag.None;

            m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, clearFlag, camera.backgroundColor, perObjectFlags);
            renderer.EnqueuePass(m_RenderOpaqueForwardPass);
            foreach (var pass in m_AfterOpaquePasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessingContext))
            {
                m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle, colorHandle, true, false);
                renderer.EnqueuePass(m_OpaquePostProcessPass);

                foreach (var pass in m_AfterOpaquePostProcessPasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
            }

            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                // We can't combine skybox and render opaques passes if there's a custom render pass in between
                // them. Ideally we need a render graph here that each render pass declares inputs and output
                // attachments and their Load/Store action so we figure out properly if we can combine passes
                // and move to interleaved rendering with RenderPass API. 
                bool combineWithRenderOpaquesPass = m_AfterOpaquePostProcessPasses.Count == 0;
                m_DrawSkyboxPass.Setup(baseDescriptor, colorHandle, depthHandle, combineWithRenderOpaquesPass);
                renderer.EnqueuePass(m_DrawSkyboxPass);
            }

            foreach (var pass in m_AfterSkyboxPasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer
            if (createDepthTexture)
            {
                m_CopyDepthPass.Setup(depthHandle, m_DepthTexture);
                renderer.EnqueuePass(m_CopyDepthPass);
            }

            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                m_CopyColorPass.Setup(colorHandle, m_OpaqueColor);
                renderer.EnqueuePass(m_CopyColorPass);
            }

            m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, perObjectFlags);
            renderer.EnqueuePass(m_RenderTransparentForwardPass);

            foreach (var pass in m_AfterTransparentPasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

#if UNITY_EDITOR
            m_LitGizmoRenderingPass.Setup(true);
            renderer.EnqueuePass(m_LitGizmoRenderingPass);
#endif

            bool afterRenderExists = m_AfterRenderPasses.Count != 0;

            // if we have additional filters
            // we need to stay in a RT
            if (afterRenderExists)
            {
                // perform post with src / dest the same
                if (renderingData.cameraData.postProcessEnabled)
                {
                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, colorHandle, false, false);
                    renderer.EnqueuePass(m_PostProcessPass);
                }

                //execute after passes
                foreach (var pass in m_AfterRenderPasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

                //now blit into the final target
                if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    if (m_CapturePass.Setup(colorHandle, renderingData.cameraData.captureActions))
                        renderer.EnqueuePass(m_CapturePass);

                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle, Display.main.requiresSrgbBlitToBackbuffer, renderingData.killAlphaInFinalBlit);
                    renderer.EnqueuePass(m_FinalBlitPass);
                }
            }
            else
            {
                if (renderingData.cameraData.postProcessEnabled)
                {
                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, RenderTargetHandle.CameraTarget, false, renderingData.cameraData.camera.targetTexture == null);
                    renderer.EnqueuePass(m_PostProcessPass);
                }
                else if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    if (m_CapturePass.Setup(colorHandle, renderingData.cameraData.captureActions))
                        renderer.EnqueuePass(m_CapturePass);

                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle, Display.main.requiresSrgbBlitToBackbuffer, renderingData.killAlphaInFinalBlit);
                    renderer.EnqueuePass(m_FinalBlitPass);
                }
            }

            if (renderingData.cameraData.isStereoEnabled)
            {
                renderer.EnqueuePass(m_EndXrRenderingPass);
            }

#if UNITY_EDITOR
            m_UnlitGizmoRenderingPass.Setup(false);
            renderer.EnqueuePass(m_UnlitGizmoRenderingPass);

            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_SceneViewDepthCopyPass.Setup(m_DepthTexture);
                renderer.EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = (int)cameraData.msaaSamples > 1;
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
