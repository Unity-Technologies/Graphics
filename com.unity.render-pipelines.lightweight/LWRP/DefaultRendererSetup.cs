using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DefaultRendererSetup : IRendererSetup
    {
        private DepthOnlyPass m_DepthOnlyPass;
        private DirectionalShadowsPass m_DirectionalShadowPass;
        private LocalShadowsPass m_LocalShadowPass;
        private SetupForwardRenderingPass m_SetupForwardRenderingPass;
        private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResovePass;
        private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
        private BeginXRRenderingPass m_BeginXrRenderingPass;
        private SetupLightweightConstanstPass m_SetupLightweightConstants;
        private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;
        private OpaquePostProcessPass m_OpaquePostProcessPass;
        private DrawSkyboxPass m_DrawSkyboxPass;
        private CopyDepthPass m_CopyDepthPass;
        private CopyColorPass m_CopyColorPass;
        private RenderTransparentForwardPass m_RenderTransparentForwardPass;
        private TransparentPostProcessPass m_TransparentPostProcessPass;
        private FinalBlitPass m_FinalBlitPass;
        private EndXRRenderingPass m_EndXrRenderingPass;
        
        #if UNITY_EDITOR
        private SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
        #endif


        private RenderTargetHandle Color;
        private RenderTargetHandle DepthAttachment;
        private RenderTargetHandle DepthTexture;
        private RenderTargetHandle OpaqueColor;
        private RenderTargetHandle DirectionalShadowmap;
        private RenderTargetHandle LocalShadowmap;
        private RenderTargetHandle ScreenSpaceShadowmap;

        [NonSerialized]
        private bool m_Initialized = false;

        private void Init(LightweightForwardRenderer renderer)
        {
            if (m_Initialized)
                return;

            m_DepthOnlyPass = new DepthOnlyPass(renderer);
            m_DirectionalShadowPass = new DirectionalShadowsPass(renderer);
            m_LocalShadowPass = new LocalShadowsPass(renderer);
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass(renderer);
            m_ScreenSpaceShadowResovePass = new ScreenSpaceShadowResolvePass(renderer);
            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass(renderer);
            m_BeginXrRenderingPass = new BeginXRRenderingPass(renderer);
            m_SetupLightweightConstants = new SetupLightweightConstanstPass(renderer);
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(renderer);
            m_OpaquePostProcessPass = new OpaquePostProcessPass(renderer);
            m_DrawSkyboxPass = new DrawSkyboxPass(renderer);
            m_CopyDepthPass = new CopyDepthPass(renderer);
            m_CopyColorPass = new CopyColorPass(renderer);
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass(renderer);
            m_TransparentPostProcessPass = new TransparentPostProcessPass(renderer);
            m_FinalBlitPass = new FinalBlitPass(renderer);
            m_EndXrRenderingPass = new EndXRRenderingPass(renderer);
            
            #if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(renderer);
            #endif
            
            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            Color.Init("_CameraColorTexture");
            DepthAttachment.Init("_CameraDepthAttachment");
            DepthTexture.Init("_CameraDepthTexture");
            OpaqueColor.Init("_CameraOpaqueTexture");
            DirectionalShadowmap.Init("_DirectionalShadowmapTexture");
            LocalShadowmap.Init("_LocalShadowmapTexture");
            ScreenSpaceShadowmap.Init("_ScreenSpaceShadowMapTexture");

            m_Initialized = true;
        }

        public void Setup(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults, ref RenderingData renderingData)
        {
            Init(renderer);

            renderer.Clear();

            renderer.SetupPerObjectLightIndices(ref cullResults, ref renderingData.lightData);
            RenderTextureDescriptor baseDescriptor = renderer.CreateRTDesc(ref renderingData.cameraData);
            RenderTextureDescriptor shadowDescriptor = baseDescriptor;
            shadowDescriptor.dimension = TextureDimension.Tex2D;

            bool requiresCameraDepth = renderingData.cameraData.requiresDepthTexture;
            bool requiresDepthPrepass = renderingData.shadowData.requiresScreenSpaceShadowResolve ||
                                        renderingData.cameraData.isSceneViewCamera ||
                                        (requiresCameraDepth &&
                                         !LightweightForwardRenderer.CanCopyDepth(ref renderingData.cameraData));

            // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
            requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;

            if (renderingData.shadowData.renderDirectionalShadows)
            {
                m_DirectionalShadowPass.Setup(DirectionalShadowmap);
                renderer.EnqueuePass(m_DirectionalShadowPass);
            }

            if (renderingData.shadowData.renderLocalShadows)
            {
                
                m_LocalShadowPass.Setup(LocalShadowmap);
                renderer.EnqueuePass(m_LocalShadowPass);
            }

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            if (requiresDepthPrepass)
            {
                m_DepthOnlyPass.Setup(baseDescriptor, DepthTexture, SampleCount.One);
                renderer.EnqueuePass(m_DepthOnlyPass);
            }

            if (renderingData.shadowData.renderDirectionalShadows &&
                renderingData.shadowData.requiresScreenSpaceShadowResolve)
            {
                m_ScreenSpaceShadowResovePass.Setup(baseDescriptor, ScreenSpaceShadowmap);
                renderer.EnqueuePass(m_ScreenSpaceShadowResovePass);
            }

            bool requiresDepthAttachment = requiresCameraDepth && !requiresDepthPrepass;
            bool requiresColorAttachment =
                LightweightForwardRenderer.RequiresIntermediateColorTexture(
                    ref renderingData.cameraData,
                    baseDescriptor,
                    requiresDepthAttachment);
            RenderTargetHandle colorHandle = (requiresColorAttachment) ? Color : RenderTargetHandle.CameraTarget;
            RenderTargetHandle depthHandle = (requiresDepthAttachment) ? DepthAttachment : RenderTargetHandle.CameraTarget;

            var sampleCount = (SampleCount) renderingData.cameraData.msaaSamples;
            m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
            renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);

            if (renderingData.cameraData.isStereoEnabled)
                renderer.EnqueuePass(m_BeginXrRenderingPass);

            Camera camera = renderingData.cameraData.camera;
            bool dynamicBatching = renderingData.supportsDynamicBatching;
            RendererConfiguration rendererConfiguration = LightweightForwardRenderer.GetRendererConfiguration(renderingData.lightData.totalAdditionalLightsCount);

            renderer.EnqueuePass(m_SetupLightweightConstants);

            m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, LightweightForwardRenderer.GetCameraClearFlag(camera), camera.backgroundColor, rendererConfiguration,dynamicBatching);
            renderer.EnqueuePass(m_RenderOpaqueForwardPass);

            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessRenderContext))
            {
                m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle);
                renderer.EnqueuePass(m_OpaquePostProcessPass);
            }

            if (camera.clearFlags == CameraClearFlags.Skybox)
                renderer.EnqueuePass(m_DrawSkyboxPass);

            if (depthHandle != RenderTargetHandle.CameraTarget)
            {
                m_CopyDepthPass.Setup(depthHandle, DepthTexture);
                renderer.EnqueuePass(m_CopyDepthPass);
            }

            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                m_CopyColorPass.Setup(colorHandle, OpaqueColor);
                renderer.EnqueuePass(m_CopyColorPass);
            }

            m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, ClearFlag.None, camera.backgroundColor, rendererConfiguration, dynamicBatching);
            renderer.EnqueuePass(m_RenderTransparentForwardPass);

            if (renderingData.cameraData.postProcessEnabled)
            {
                m_TransparentPostProcessPass.Setup(baseDescriptor, colorHandle);
                renderer.EnqueuePass(m_TransparentPostProcessPass);
            }
            else if (!renderingData.cameraData.isOffscreenRender && colorHandle != RenderTargetHandle.CameraTarget)
            {
                m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                renderer.EnqueuePass(m_FinalBlitPass);
            }

            if (renderingData.cameraData.isStereoEnabled)
            {
                renderer.EnqueuePass(m_EndXrRenderingPass);
            }
            
#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_SceneViewDepthCopyPass.Setup(DepthTexture);
                renderer.EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
        }
    }
}
