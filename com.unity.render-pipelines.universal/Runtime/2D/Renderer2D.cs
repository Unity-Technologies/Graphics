using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Renderer2D : ScriptableRenderer
    {
        ColorGradingLutPass m_ColorGradingLutPass;
        Render2DLightingPass m_Render2DLightingPass;
        PostProcessPass m_PostProcessPass;
        FinalBlitPass m_FinalBlitPass;
        PostProcessPass m_FinalPostProcessPass;

#if POST_PROCESSING_STACK_2_0_0_OR_NEWER
        PostProcessPassCompat m_PostProcessPassCompat;
#endif

        bool m_UseDepthStencilBuffer = true;
        RenderTargetHandle m_ColorTargetHandle;
        RenderTargetHandle m_AfterPostProcessColorHandle;
        RenderTargetHandle m_ColorGradingLutHandle;

        Material m_BlitMaterial;

        Renderer2DData m_Renderer2DData;

        public Renderer2D(Renderer2DData data) : base(data)
        {
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);

            m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingOpaques, data.postProcessData);
            m_Render2DLightingPass = new Render2DLightingPass(data);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
            m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, m_BlitMaterial);

#if POST_PROCESSING_STACK_2_0_0_OR_NEWER
            m_PostProcessPassCompat = new PostProcessPassCompat(RenderPassEvent.BeforeRenderingPostProcessing);
#endif

            m_UseDepthStencilBuffer = data.useDepthStencilBuffer;

            m_AfterPostProcessColorHandle.Init("_AfterPostProcessTexture");
            m_ColorGradingLutHandle.Init("_InternalGradingLut");

            m_Renderer2DData = data;
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_BlitMaterial);
        }

        public Renderer2DData GetRenderer2DData()
        {
            return m_Renderer2DData;
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;
            PixelPerfectCamera ppc;
            cameraData.camera.TryGetComponent<PixelPerfectCamera>(out ppc);

            Vector2Int ppcOffscreenRTSize = ppc != null ? ppc.offscreenRTSize : Vector2Int.zero;
            bool ppcUsesOffscreenRT = ppcOffscreenRTSize != Vector2Int.zero;
            var postProcessFeatureSet = UniversalRenderPipeline.asset.postProcessingFeatureSet;
            bool postProcessEnabled = renderingData.cameraData.postProcessEnabled;
            bool useOffscreenColorTexture =
                ppcUsesOffscreenRT || postProcessEnabled || cameraData.isHdrEnabled || cameraData.isSceneViewCamera || !cameraData.isDefaultViewport || !m_UseDepthStencilBuffer;

            // Pixel Perfect Camera may request a different RT size than camera VP size.
            // In that case we need to modify cameraTargetDescriptor here so that all the passes would use the same size.
            if (ppcUsesOffscreenRT)
            {
                cameraTargetDescriptor.width = ppcOffscreenRTSize.x;
                cameraTargetDescriptor.height = ppcOffscreenRTSize.y;
            }

            if (useOffscreenColorTexture)
            {
                var filterMode = ppc != null ? ppc.finalBlitFilterMode : FilterMode.Bilinear;
                m_ColorTargetHandle = CreateOffscreenColorTexture(context, ref cameraTargetDescriptor, filterMode);
            }
            else
                m_ColorTargetHandle = RenderTargetHandle.CameraTarget;

            ConfigureCameraTarget(m_ColorTargetHandle.Identifier(), BuiltinRenderTextureType.CameraTarget);

            m_Render2DLightingPass.ConfigureTarget(m_ColorTargetHandle.Identifier());
            EnqueuePass(m_Render2DLightingPass);

            bool requireFinalBlitPass = useOffscreenColorTexture;
            var finalBlitSourceHandle = m_ColorTargetHandle;

            if (postProcessEnabled && postProcessFeatureSet == PostProcessingFeatureSet.Integrated)
            {
                m_ColorGradingLutPass.Setup(m_ColorGradingLutHandle);
                EnqueuePass(m_ColorGradingLutPass);

                // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
                // and only upscale the low-res RT to fullscreen when blitting it to camera target.
                if (ppc != null && ppc.upscaleRT && ppc.isRunning)
                {
                    m_PostProcessPass.Setup(
                        cameraTargetDescriptor,
                        m_ColorTargetHandle,
                        m_AfterPostProcessColorHandle,
                        RenderTargetHandle.CameraTarget,
                        m_ColorGradingLutHandle,
                        false,
                        false
                    );
                    EnqueuePass(m_PostProcessPass);

                    requireFinalBlitPass = true;
                    finalBlitSourceHandle = m_AfterPostProcessColorHandle;
                }
                else if (renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing)
                {
                    m_PostProcessPass.Setup(
                        cameraTargetDescriptor,
                        m_ColorTargetHandle,
                        m_AfterPostProcessColorHandle,
                        RenderTargetHandle.CameraTarget,
                        m_ColorGradingLutHandle,
                        true,
                        false
                    );
                    EnqueuePass(m_PostProcessPass);

                    m_FinalPostProcessPass.SetupFinalPass(m_AfterPostProcessColorHandle);
                    EnqueuePass(m_FinalPostProcessPass);

                    requireFinalBlitPass = false;
                }
                else
                {
                    m_PostProcessPass.Setup(
                        cameraTargetDescriptor,
                        m_ColorTargetHandle,
                        RenderTargetHandle.CameraTarget,
                        RenderTargetHandle.CameraTarget,
                        m_ColorGradingLutHandle,
                        false,
                        true
                    );
                    EnqueuePass(m_PostProcessPass);

                    requireFinalBlitPass = false;
                }
            }
#if POST_PROCESSING_STACK_2_0_0_OR_NEWER
            else if (postProcessEnabled && postProcessFeatureSet == PostProcessingFeatureSet.PostProcessingV2)
            {
                m_PostProcessPassCompat.Setup(cameraData.cameraTargetDescriptor, m_ColorTargetHandle, m_ColorTargetHandle);
                EnqueuePass(m_PostProcessPassCompat);
            }
#endif

            if (requireFinalBlitPass)
            {
                m_FinalBlitPass.Setup(cameraTargetDescriptor, finalBlitSourceHandle);
                EnqueuePass(m_FinalBlitPass);
            }
        }
        
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
        }

        RenderTargetHandle CreateOffscreenColorTexture(ScriptableRenderContext context, ref RenderTextureDescriptor cameraTargetDescriptor, FilterMode filterMode)
        {
            RenderTargetHandle colorTextureHandle = new RenderTargetHandle();
            colorTextureHandle.Init("_CameraColorTexture");

            var colorDescriptor = cameraTargetDescriptor;
            colorDescriptor.depthBufferBits = m_UseDepthStencilBuffer ? 32 : 0;

            CommandBuffer cmd = CommandBufferPool.Get("Create Camera Textures");
            cmd.GetTemporaryRT(colorTextureHandle.id, colorDescriptor, filterMode);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            return colorTextureHandle;
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            if (m_ColorTargetHandle != RenderTargetHandle.CameraTarget)
                cmd.ReleaseTemporaryRT(m_ColorTargetHandle.id);
        }
    }
}
