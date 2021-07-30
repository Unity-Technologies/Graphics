using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class UpscalePass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingScope = new ProfilingSampler("Upscale Pass");

        RenderTargetHandle m_Source;
        RenderTargetHandle m_UpscaleHandle;
        PixelPerfectCamera m_PixelPerfectCam;

        public UpscalePass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(RenderTargetHandle colorTargetHandle, RenderTargetHandle upscaleHandle, PixelPerfectCamera cam)
        {
            m_Source = colorTargetHandle;
            m_UpscaleHandle = upscaleHandle;
            m_PixelPerfectCam = cam;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingScope))
            {
                RenderTextureDescriptor upscaleDesc = cameraData.cameraTargetDescriptor;
                upscaleDesc.width = m_PixelPerfectCam.refResolutionX * m_PixelPerfectCam.pixelRatio;
                upscaleDesc.height = m_PixelPerfectCam.refResolutionY * m_PixelPerfectCam.pixelRatio;
                cmd.GetTemporaryRT(m_UpscaleHandle.id, upscaleDesc, m_PixelPerfectCam.finalBlitFilterMode);

                cmd.SetRenderTarget(m_UpscaleHandle.id,
                                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, // color
                                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth

                cmd.Blit(m_Source.id, m_UpscaleHandle.id);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnFinishCameraStackRendering(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_UpscaleHandle.id);
        }
    }
}
