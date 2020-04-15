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

        bool m_UseDepthStencilBuffer = true;
        bool m_CreateColorTexture;
        bool m_CreateDepthTexture;

        readonly RenderTargetHandle k_ColorTextureHandle;
        readonly RenderTargetHandle k_DepthTextureHandle;
        readonly RenderTargetHandle k_AfterPostProcessColorHandle;
        readonly RenderTargetHandle k_ColorGradingLutHandle;

        Material m_BlitMaterial;

        Renderer2DData m_Renderer2DData;

        public Renderer2D(Renderer2DData data) : base(data)
        {
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);

            m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingOpaques, data.postProcessData);
            m_Render2DLightingPass = new Render2DLightingPass(data);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
            m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);

            m_UseDepthStencilBuffer = data.useDepthStencilBuffer;

            k_ColorTextureHandle.Init("_CameraColorTexture");
            k_DepthTextureHandle.Init("_CameraDepthAttachment");
            k_AfterPostProcessColorHandle.Init("_AfterPostProcessTexture");
            k_ColorGradingLutHandle.Init("_InternalGradingLut");

            m_Renderer2DData = data;

            supportedRenderingFeatures = new RenderingFeatures()
            {
                cameraStacking = true,
            };
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
            bool cameraHasPostProcess = renderingData.cameraData.postProcessEnabled;
            bool stackHasPostProcess = renderingData.postProcessingEnabled;
            bool lastCameraInStack = cameraData.resolveFinalTarget;

            PixelPerfectCamera ppc = null;
            RenderTargetHandle colorTargetHandle;
            RenderTargetHandle depthTargetHandle;

            if (cameraData.renderType == CameraRenderType.Base)
            {
                cameraData.camera.TryGetComponent(out ppc);
                Vector2Int ppcOffscreenRTSize = ppc != null ? ppc.offscreenRTSize : Vector2Int.zero;
                bool ppcUsesOffscreenRT = ppcOffscreenRTSize != Vector2Int.zero;
                
                m_CreateColorTexture = ppcUsesOffscreenRT || cameraHasPostProcess || cameraData.isHdrEnabled || cameraData.isSceneViewCamera || !cameraData.isDefaultViewport
                    || !m_UseDepthStencilBuffer || !lastCameraInStack;

                m_CreateDepthTexture = !lastCameraInStack && m_UseDepthStencilBuffer;

                // Pixel Perfect Camera may request a different RT size than camera VP size.
                // In that case we need to modify cameraTargetDescriptor here so that all the passes would use the same size.
                if (ppcUsesOffscreenRT)
                {
                    cameraTargetDescriptor.width = ppcOffscreenRTSize.x;
                    cameraTargetDescriptor.height = ppcOffscreenRTSize.y;
                }

                colorTargetHandle = RenderTargetHandle.CameraTarget;
                depthTargetHandle = RenderTargetHandle.CameraTarget;

                if (m_CreateColorTexture || m_CreateDepthTexture)
                {
                    CommandBuffer cmd = CommandBufferPool.Get("Create Camera Textures");

                    if (m_CreateColorTexture)
                    {
                        var filterMode = ppc != null ? ppc.finalBlitFilterMode : FilterMode.Bilinear;
                        var colorDescriptor = cameraTargetDescriptor;
                        colorDescriptor.depthBufferBits = m_CreateDepthTexture || !m_UseDepthStencilBuffer ? 0 : 32;
                        cmd.GetTemporaryRT(k_ColorTextureHandle.id, colorDescriptor, filterMode);

                        colorTargetHandle = k_ColorTextureHandle;
                    }

                    if (m_CreateDepthTexture)
                    {
                        var depthDescriptor = cameraTargetDescriptor;
                        depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                        depthDescriptor.depthBufferBits = 32;
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                        cmd.GetTemporaryRT(k_DepthTextureHandle.id, depthDescriptor, FilterMode.Point);

                        depthTargetHandle = k_DepthTextureHandle;
                    }
                    else
                        depthTargetHandle = colorTargetHandle;

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
            else    // Overlay camera
            {
                m_CreateColorTexture = false;
                m_CreateDepthTexture = false;
                colorTargetHandle = k_ColorTextureHandle;
                depthTargetHandle = k_DepthTextureHandle;
            }

            ConfigureCameraTarget(colorTargetHandle.Identifier(), depthTargetHandle.Identifier());

            // We generate color LUT in the base camera only. This allows us to not break render pass execution for overlay cameras.
            if (stackHasPostProcess && cameraData.renderType == CameraRenderType.Base)
            {
                m_ColorGradingLutPass.Setup(k_ColorGradingLutHandle);
                EnqueuePass(m_ColorGradingLutPass);
            }

            m_Render2DLightingPass.ConfigureTarget(colorTargetHandle.Identifier(), depthTargetHandle.Identifier());
            EnqueuePass(m_Render2DLightingPass);

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target.
            bool ppcUpscaleRT = ppc != null && ppc.upscaleRT && ppc.isRunning;

            bool requireFinalPostProcessPass =
                lastCameraInStack && !ppcUpscaleRT && stackHasPostProcess && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            RenderTargetHandle currentTargetHandle = colorTargetHandle;

            if (cameraHasPostProcess)
            {
                RenderTargetHandle postProcessDestHandle =
                    lastCameraInStack && !ppcUpscaleRT && !requireFinalPostProcessPass ? RenderTargetHandle.CameraTarget : k_AfterPostProcessColorHandle;

                m_PostProcessPass.Setup(
                    cameraTargetDescriptor,
                    colorTargetHandle,
                    postProcessDestHandle,
                    depthTargetHandle,
                    k_ColorGradingLutHandle,
                    requireFinalPostProcessPass,
                    postProcessDestHandle == RenderTargetHandle.CameraTarget);

                EnqueuePass(m_PostProcessPass);
                currentTargetHandle = postProcessDestHandle;
            }

            if (requireFinalPostProcessPass)
            {
                m_FinalPostProcessPass.SetupFinalPass(currentTargetHandle);
                EnqueuePass(m_FinalPostProcessPass);
                currentTargetHandle = RenderTargetHandle.CameraTarget;
            }
            else if (lastCameraInStack && currentTargetHandle != RenderTargetHandle.CameraTarget)
            {
                m_FinalBlitPass.Setup(cameraTargetDescriptor, currentTargetHandle);
                EnqueuePass(m_FinalBlitPass);
            }
        }
        
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            if (m_CreateColorTexture)
                cmd.ReleaseTemporaryRT(k_ColorTextureHandle.id);

            if (m_CreateDepthTexture)
                cmd.ReleaseTemporaryRT(k_DepthTextureHandle.id);
        }
    }
}
