using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class _2DRenderer : ScriptableRenderer
    {
        Render2DLightingPass m_Render2DLightingPass;
        FinalBlitPass m_FinalBlitPass;
        RenderTargetHandle m_ColorTargetHandle;

        public _2DRenderer(_2DRendererData data) : base(data)
        {
            m_Render2DLightingPass = new Render2DLightingPass(data);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, CoreUtils.CreateEngineMaterial(data.blitShader));
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            m_ColorTargetHandle = RenderTargetHandle.CameraTarget;
            PixelPerfectCamera ppc = cameraData.camera.GetComponent<PixelPerfectCamera>();
            bool useOffscreenColorTexture = ppc != null ? ppc.useOffscreenRT : false;

            if (useOffscreenColorTexture)
            {
                var filterMode = ppc != null ? ppc.finalBlitFilterMode : FilterMode.Bilinear;
                m_ColorTargetHandle = CreateOffscreenColorTexture(context, ref cameraData.cameraTargetDescriptor, filterMode);
            }

            ConfigureCameraTarget(m_ColorTargetHandle.Identifier(), BuiltinRenderTextureType.CameraTarget);

            m_Render2DLightingPass.ConfigureTarget(m_ColorTargetHandle.Identifier());
            EnqueuePass(m_Render2DLightingPass);

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
