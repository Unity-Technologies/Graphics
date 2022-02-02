using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class UpscalePass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingScope = new ProfilingSampler("Upscale Pass");

        RenderTargetHandle m_Source;
        RenderTargetHandle m_UpscaleHandle;
        int m_UpscaleWidth;
        int m_UpscaleHeight;
        FilterMode m_filterMode;

        public UpscalePass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(RenderTargetHandle colorTargetHandle, int width, int height, FilterMode mode, out RenderTargetHandle upscaleHandle)
        {
            m_Source = colorTargetHandle;
            m_UpscaleWidth = width;
            m_UpscaleHeight = height;
            m_filterMode = mode;

            m_UpscaleHandle.Init("_UpscaleTexture");
            upscaleHandle = m_UpscaleHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingScope))
            {
                RenderTextureDescriptor upscaleDesc = cameraData.cameraTargetDescriptor;
                upscaleDesc.width = m_UpscaleWidth;
                upscaleDesc.height = m_UpscaleHeight;
                cmd.GetTemporaryRT(m_UpscaleHandle.id, upscaleDesc, m_filterMode);

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
