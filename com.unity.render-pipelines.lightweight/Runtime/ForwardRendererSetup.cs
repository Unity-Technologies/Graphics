using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class ForwardRendererSetup : RendererSetup
    {
        private DepthOnlyPass m_DepthOnlyPass;
        private MainLightShadowCasterPass m_MainLightShadowCasterPass;
        private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        private SetupLightweightConstanstPass m_SetupLightweightConstants;
        private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
        private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
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
        
#if UNITY_EDITOR
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
            
            m_RenderPassFeatures.AddRange(data.renderPassFeatures.Where(x => x != null));
            
            m_DepthOnlyPass = new DepthOnlyPass();
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass();
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass();
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass(screenspaceShadowsMaterial);
            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
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
            
#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(copyDepthMaterial);
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

        public override void Setup(ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
            RenderTextureDescriptor baseDescriptor = ScriptableRenderPass.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            ClearFlag clearFlag = GetCameraClearFlag(renderingData.cameraData.camera);
            RenderTextureDescriptor shadowDescriptor = baseDescriptor;
            shadowDescriptor.dimension = TextureDimension.Tex2D;

            bool mainLightShadows = false;
            if (renderingData.shadowData.supportsMainLightShadows)
            {
                mainLightShadows = m_MainLightShadowCasterPass.Setup(m_MainLightShadowmap, ref renderingData);
                if (mainLightShadows)
                    EnqueuePass(RenderPassBlock.BeforeMainRender, m_MainLightShadowCasterPass);
            }

            if (renderingData.shadowData.supportsAdditionalLightShadows)
            {
                bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(m_AdditionalLightsShadowmap, ref renderingData);
                if (additionalLightShadows)
                    EnqueuePass(RenderPassBlock.BeforeMainRender, m_AdditionalLightsShadowCasterPass);
            }

            bool resolveShadowsInScreenSpace =
                mainLightShadows && renderingData.shadowData.requiresScreenSpaceShadowResolve;

            // Depth prepass is generated in the following cases:
            // - We resolve shadows in screen space
            // - Scene view camera always requires a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            bool requiresDepthPrepass = resolveShadowsInScreenSpace ||
                                        renderingData.cameraData.isSceneViewCamera ||
                                        (renderingData.cameraData.requiresDepthTexture &&
                                         (!CanCopyDepth(ref renderingData.cameraData)));

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
                EnqueuePass(RenderPassBlock.MainRender, m_CreateLightweightRenderTexturesPass);
            }

            RenderPassFeature.InjectionPoint injectionPoints = 0;

            foreach (var pass in m_RenderPassFeatures)
            {
                injectionPoints |= pass.injectionPoints;
            }

            EnqueuePasses(RenderPassBlock.MainRender, RenderPassFeature.InjectionPoint.BeforeRenderPasses, injectionPoints,
                baseDescriptor, colorHandle, depthHandle);
            
            if (requiresDepthPrepass)
            {
                m_DepthOnlyPass.Setup(baseDescriptor, m_DepthTexture);
                EnqueuePass(RenderPassBlock.MainRender, m_DepthOnlyPass);
            }

            if (resolveShadowsInScreenSpace)
            {
                m_ScreenSpaceShadowResolvePass.Setup(baseDescriptor, m_ScreenSpaceShadowmap);
                EnqueuePass(RenderPassBlock.MainRender, m_ScreenSpaceShadowResolvePass);
            }

            EnqueuePass(RenderPassBlock.MainRender, m_SetupLightweightConstants);

            // If a before all render pass executed we expect it to clear the color render target
            if (CoreUtils.HasFlag(injectionPoints, RenderPassFeature.InjectionPoint.BeforeRenderPasses))
                clearFlag = ClearFlag.None;

            m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, clearFlag, camera.backgroundColor);
            EnqueuePass(RenderPassBlock.MainRender, m_RenderOpaqueForwardPass);

            EnqueuePasses(RenderPassBlock.MainRender, RenderPassFeature.InjectionPoint.AfterOpaqueRenderPasses, injectionPoints,
                baseDescriptor, colorHandle, depthHandle);

            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(ScriptableRenderPass.postProcessRenderContext))
            {
                m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle, colorHandle, true, false);
                EnqueuePass(RenderPassBlock.MainRender, m_OpaquePostProcessPass);

                EnqueuePasses(RenderPassBlock.MainRender, RenderPassFeature.InjectionPoint.AfterOpaquePostProcessPasses, injectionPoints,
                    baseDescriptor, colorHandle, depthHandle);
            }

            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                // We can't combine skybox and render opaques passes if there's a custom render pass in between
                // them. Ideally we need a render graph here that each render pass declares inputs and output
                // attachments and their Load/Store action so we figure out properly if we can combine passes
                // and move to interleaved rendering with RenderPass API. 
                bool combineWithRenderOpaquesPass = !CoreUtils.HasFlag(injectionPoints, RenderPassFeature.InjectionPoint.AfterOpaquePostProcessPasses);
                m_DrawSkyboxPass.Setup(baseDescriptor, colorHandle, depthHandle, combineWithRenderOpaquesPass);
                EnqueuePass(RenderPassBlock.MainRender, m_DrawSkyboxPass);
            }

            EnqueuePasses(RenderPassBlock.MainRender, RenderPassFeature.InjectionPoint.AfterSkyboxPasses, injectionPoints,
                baseDescriptor, colorHandle, depthHandle);

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer
            if (createDepthTexture)
            {
                m_CopyDepthPass.Setup(depthHandle, m_DepthTexture);
                EnqueuePass(RenderPassBlock.MainRender, m_CopyDepthPass);
            }

            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                m_CopyColorPass.Setup(colorHandle, m_OpaqueColor);
                EnqueuePass(RenderPassBlock.MainRender, m_CopyColorPass);
            }

            m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle);
            EnqueuePass(RenderPassBlock.MainRender, m_RenderTransparentForwardPass);

            EnqueuePasses(RenderPassBlock.MainRender, RenderPassFeature.InjectionPoint.AfterTransparentPasses, injectionPoints,
                baseDescriptor, colorHandle, depthHandle);

            bool afterRenderExists = CoreUtils.HasFlag(injectionPoints, RenderPassFeature.InjectionPoint.AfterRenderPasses);

            // if we have additional filters
            // we need to stay in a RT
            if (afterRenderExists)
            {
                // perform post with src / dest the same
                if (renderingData.cameraData.postProcessEnabled)
                {
                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, colorHandle, false, false);
                    EnqueuePass(RenderPassBlock.AfterMainRender, m_PostProcessPass);
                }

                EnqueuePasses(RenderPassBlock.AfterMainRender, RenderPassFeature.InjectionPoint.AfterRenderPasses, injectionPoints,
                    baseDescriptor, colorHandle, depthHandle);

                //now blit into the final target
                if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    if (m_CapturePass.Setup(colorHandle, renderingData.cameraData.captureActions))
                        EnqueuePass(RenderPassBlock.AfterMainRender, m_CapturePass);

                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle, Display.main.requiresSrgbBlitToBackbuffer, renderingData.killAlphaInFinalBlit);
                    EnqueuePass(RenderPassBlock.AfterMainRender, m_FinalBlitPass);
                }
            }
            else
            {
                if (renderingData.cameraData.postProcessEnabled)
                {
                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, RenderTargetHandle.CameraTarget, false, renderingData.cameraData.camera.targetTexture == null);
                    EnqueuePass(RenderPassBlock.AfterMainRender, m_PostProcessPass);
                }
                else if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    if (m_CapturePass.Setup(colorHandle, renderingData.cameraData.captureActions))
                        EnqueuePass(RenderPassBlock.AfterMainRender, m_CapturePass);

                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle, Display.main.requiresSrgbBlitToBackbuffer, renderingData.killAlphaInFinalBlit);
                    EnqueuePass(RenderPassBlock.AfterMainRender, m_FinalBlitPass);
                }
            }

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_SceneViewDepthCopyPass.Setup(m_DepthTexture);
                EnqueuePass(RenderPassBlock.AfterMainRender, m_SceneViewDepthCopyPass);
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
