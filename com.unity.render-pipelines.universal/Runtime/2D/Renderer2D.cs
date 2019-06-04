using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class Renderer2D : ScriptableRenderer
    {
        Render2DLightingPass m_Render2DLightingPass;
        PostProcessPass m_PostProcessPass;
        FinalBlitPass m_FinalBlitPass;
        RenderTargetHandle m_ColorTargetHandle;

        public Renderer2D(Renderer2DData data) : base(data)
        {
            m_Render2DLightingPass = new Render2DLightingPass(data);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, CoreUtils.CreateEngineMaterial(data.blitShader));
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            m_ColorTargetHandle = RenderTargetHandle.CameraTarget;
            PixelPerfectCamera ppc = cameraData.camera.GetComponent<PixelPerfectCamera>();
            bool postProcessEnabled = renderingData.cameraData.postProcessEnabled;
            bool useOffscreenColorTexture = (ppc != null && ppc.useOffscreenRT) || postProcessEnabled || cameraData.isHdrEnabled || cameraData.isSceneViewCamera || !cameraData.isDefaultViewport;

            if (useOffscreenColorTexture)
            {
                var filterMode = ppc != null ? ppc.finalBlitFilterMode : FilterMode.Bilinear;
                m_ColorTargetHandle = CreateOffscreenColorTexture(context, ref cameraData.cameraTargetDescriptor, filterMode);
            }

            ConfigureCameraTarget(m_ColorTargetHandle.Identifier(), BuiltinRenderTextureType.CameraTarget);

            m_Render2DLightingPass.ConfigureTarget(m_ColorTargetHandle.Identifier());
            EnqueuePass(m_Render2DLightingPass);

            if (postProcessEnabled)
            {
                m_PostProcessPass.Setup(cameraData.cameraTargetDescriptor, m_ColorTargetHandle, m_ColorTargetHandle);
                EnqueuePass(m_PostProcessPass);
            }

            if (useOffscreenColorTexture)
            {
                if (ppc != null)
                    m_FinalBlitPass.Setup(cameraData.cameraTargetDescriptor, m_ColorTargetHandle, ppc.useOffscreenRT, ppc.finalBlitPixelRect);
                else
                    m_FinalBlitPass.Setup(cameraData.cameraTargetDescriptor, m_ColorTargetHandle);

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
            colorDescriptor.depthBufferBits = 32;

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
