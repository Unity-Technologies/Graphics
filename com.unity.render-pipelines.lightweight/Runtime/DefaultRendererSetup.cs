using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    internal class DefaultRendererSetup : IRendererSetup
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
        private CreateColorRenderTexturesPass m_CreatePostOpaqueColorPass;
        private PostProcessPass m_OpaquePostProcessPass;
        private DrawSkyboxPass m_DrawSkyboxPass;
        private CopyDepthPass m_CopyDepthPass;
        private CopyColorPass m_CopyColorPass;
        private CreateColorRenderTexturesPass m_CreatePostTransparentColorPass;
        private RenderTransparentForwardPass m_RenderTransparentForwardPass;
        private PostProcessPass m_PostProcessPass;
        private CreateColorRenderTexturesPass m_createColorPass;
        private FinalBlitPass m_FinalBlitPass;
        private EndXRRenderingPass m_EndXrRenderingPass;

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

        [NonSerialized]
        private bool m_Initialized = false;

        private void Init()
        {
            if (m_Initialized)
                return;

            m_DepthOnlyPass = new DepthOnlyPass();
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass();
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass();
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass();
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass();
            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
            m_BeginXrRenderingPass = new BeginXRRenderingPass();
            m_SetupLightweightConstants = new SetupLightweightConstanstPass();
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
            m_CreatePostOpaqueColorPass = new CreateColorRenderTexturesPass();
            m_OpaquePostProcessPass = new PostProcessPass();
            m_DrawSkyboxPass = new DrawSkyboxPass();
            m_CopyDepthPass = new CopyDepthPass();
            m_CopyColorPass = new CopyColorPass();
            m_CreatePostTransparentColorPass = new CreateColorRenderTexturesPass();
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass();
            m_PostProcessPass = new PostProcessPass();
            m_FinalBlitPass = new FinalBlitPass();
            m_EndXrRenderingPass = new EndXRRenderingPass();

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass();
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

            m_Initialized = true;
        }
        
        public static bool RequiresIntermediateColorTexture(ref CameraData cameraData, RenderTextureDescriptor baseDescriptor)
        {
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isTargetTexture2DArray = baseDescriptor.dimension == TextureDimension.Tex2DArray;
            bool noAutoResolveMsaa = cameraData.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve;
            return noAutoResolveMsaa || cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || isTargetTexture2DArray || !cameraData.isDefaultViewport;
        }


        List<IAfterDepthPrePass> m_AfterDepthpasses = new List<IAfterDepthPrePass>(10);
        List<IAfterOpaquePass> m_AfterOpaquePasses = new List<IAfterOpaquePass>(10);
        List<IAfterOpaquePostProcess> m_AfterOpaquePostProcessPasses = new List<IAfterOpaquePostProcess>(10);
        List<IAfterSkyboxPass> m_AfterSkyboxPasses = new List<IAfterSkyboxPass>(10);
        List<IAfterTransparentPass> m_AfterTransparentPasses = new List<IAfterTransparentPass>(10);
        List<IAfterRender> m_AfterRenderPasses = new List<IAfterRender>(10);

        public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Init();

            Camera camera = renderingData.cameraData.camera;

            renderer.SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
            RenderTextureDescriptor baseDescriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
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
            bool requiresDepthPrepass = resolveShadowsInScreenSpace || renderingData.cameraData.isSceneViewCamera ||
                                        (renderingData.cameraData.requiresDepthTexture && (!CanCopyDepth(ref renderingData.cameraData)));

            // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
            requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            camera.GetComponents(m_AfterDepthpasses);
            camera.GetComponents(m_AfterOpaquePasses);
            camera.GetComponents(m_AfterOpaquePostProcessPasses);
            camera.GetComponents(m_AfterSkyboxPasses);
            camera.GetComponents(m_AfterTransparentPasses);
            camera.GetComponents(m_AfterRenderPasses);

            if (requiresDepthPrepass)
            {
                m_DepthOnlyPass.Setup(baseDescriptor, m_DepthTexture, SampleCount.One);
                renderer.EnqueuePass(m_DepthOnlyPass);

                foreach (var pass in m_AfterDepthpasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(m_DepthOnlyPass.descriptor, m_DepthTexture));
            }

            if (resolveShadowsInScreenSpace)
            {
                m_ScreenSpaceShadowResolvePass.Setup(baseDescriptor, m_ScreenSpaceShadowmap);
                renderer.EnqueuePass(m_ScreenSpaceShadowResolvePass);
            }

            bool requiresRenderToTexture = RequiresIntermediateColorTexture(ref renderingData.cameraData, baseDescriptor)
                    || m_AfterDepthpasses.Count != 0
                    || m_AfterOpaquePasses.Count != 0
                    || m_AfterOpaquePostProcessPasses.Count != 0
                    || m_AfterSkyboxPasses.Count != 0
                    || m_AfterTransparentPasses.Count != 0
                    || m_AfterRenderPasses.Count != 0;

            RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
            RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;


            var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
            if (requiresRenderToTexture)
            {
                colorHandle = m_ColorAttachment;
                depthHandle = m_DepthAttachment;

                m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
                renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);
            }

            if (renderingData.cameraData.isStereoEnabled)
                renderer.EnqueuePass(m_BeginXrRenderingPass);

            var rendererConfiguration = ScriptableRenderer.GetRendererConfiguration(renderingData.lightData.additionalLightsCount);

            m_SetupLightweightConstants.Setup(renderer.maxVisibleAdditionalLights, renderer.perObjectLightIndices);
            renderer.EnqueuePass(m_SetupLightweightConstants);

            m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, ScriptableRenderer.GetCameraClearFlag(camera), camera.backgroundColor, rendererConfiguration);
            renderer.EnqueuePass(m_RenderOpaqueForwardPass);
            foreach (var pass in m_AfterOpaquePasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessingContext))
            {
                m_CreatePostOpaqueColorPass.Setup(baseDescriptor, m_ColorAttachmentAfterOpaquePost, sampleCount);
                renderer.EnqueuePass(m_CreatePostOpaqueColorPass);
                m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle, m_ColorAttachmentAfterOpaquePost, true, false);
                renderer.EnqueuePass(m_OpaquePostProcessPass);

                colorHandle = m_ColorAttachmentAfterOpaquePost;

                foreach (var pass in m_AfterOpaquePostProcessPasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
            }

            if (camera.clearFlags == CameraClearFlags.Skybox)
            {
                m_DrawSkyboxPass.Setup(colorHandle, depthHandle);
                renderer.EnqueuePass(m_DrawSkyboxPass);
            }

            foreach (var pass in m_AfterSkyboxPasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            if (renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass)
            {
                m_CopyDepthPass.Setup(depthHandle, m_DepthTexture);
                renderer.EnqueuePass(m_CopyDepthPass);
            }

            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                m_CopyColorPass.Setup(colorHandle, m_OpaqueColor);
                renderer.EnqueuePass(m_CopyColorPass);
            }

            m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, rendererConfiguration);
            renderer.EnqueuePass(m_RenderTransparentForwardPass);

            foreach (var pass in m_AfterTransparentPasses)
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            bool afterRenderExists = m_AfterRenderPasses.Count != 0;

            // if we have additional filters
            // we need to stay in a RT
            if (afterRenderExists)
            {
                // perform post with src / dest the same
                if (!renderingData.cameraData.isStereoEnabled && renderingData.cameraData.postProcessEnabled)
                {
                    m_CreatePostTransparentColorPass.Setup(baseDescriptor, m_ColorAttachmentAfterTransparentPost, sampleCount);
                    renderer.EnqueuePass(m_CreatePostTransparentColorPass);

                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, m_ColorAttachmentAfterTransparentPost, false, false);
                    renderer.EnqueuePass(m_PostProcessPass);

                    colorHandle = m_ColorAttachmentAfterTransparentPost;
                }

                //execute after passes
                foreach (var pass in m_AfterRenderPasses)
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

                //now blit into the final target
                if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                    renderer.EnqueuePass(m_FinalBlitPass);
                }
            }
            else
            {
                if (!renderingData.cameraData.isStereoEnabled && renderingData.cameraData.postProcessEnabled)
                {
                    m_PostProcessPass.Setup(baseDescriptor, colorHandle, RenderTargetHandle.CameraTarget, false, (!renderingData.cameraData.isStereoEnabled && renderingData.cameraData.camera.targetTexture == null));
                    renderer.EnqueuePass(m_PostProcessPass);
                }
                else if (colorHandle != RenderTargetHandle.CameraTarget)
                {
                    m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                    renderer.EnqueuePass(m_FinalBlitPass);
                }
            }
            
            if (renderingData.cameraData.isStereoEnabled)
            {
                renderer.EnqueuePass(m_EndXrRenderingPass);
            }

#if UNITY_EDITOR
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
