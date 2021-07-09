using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class UpscalePass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingScope = new ProfilingSampler("Upscale Pass");

        RTHandle m_Source;
        RTHandle m_UpscaleHandle;
        int m_UpscaleWidth;
        int m_UpscaleHeight;
        FilterMode m_filterMode;

        public UpscalePass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(RTHandle colorTargetHandle, int width, int height, FilterMode mode, ref RenderingData renderingData, out RTHandle upscaleHandle)
        {
            m_Source = colorTargetHandle;
            m_UpscaleWidth = width;
            m_UpscaleHeight = height;
            m_filterMode = mode;

            if (m_UpscaleHandle == null)
                m_UpscaleHandle = RTHandles.Alloc("_UpscaleTexture", "_UpscaleTexture");
            upscaleHandle = m_UpscaleHandle;
        }

        public void Dispose()
        {
            m_UpscaleHandle?.Release();
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
                cmd.GetTemporaryRT(Shader.PropertyToID(m_UpscaleHandle.name), upscaleDesc, m_filterMode);

                cmd.SetRenderTarget(m_UpscaleHandle.nameID,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, // color
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth

                cmd.Blit(m_Source.nameID, m_UpscaleHandle.nameID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnFinishCameraStackRendering(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_UpscaleHandle.name));
        }
    }
}
