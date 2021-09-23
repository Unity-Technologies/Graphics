using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class UpscalePass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingScope = new ProfilingSampler("Upscale Pass");

        RTHandle m_Source;
        RTHandle m_UpscaleHandle;

        public UpscalePass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(RTHandle colorTargetHandle, int width, int height, FilterMode mode, ref RenderingData renderingData, out RTHandle upscaleHandle)
        {
            m_Source = colorTargetHandle;

            ref CameraData cameraData = ref renderingData.cameraData;
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width = width;
            desc.height = height;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref m_UpscaleHandle, desc, mode, TextureWrapMode.Clamp, name: "_UpscaleTexture");

            upscaleHandle = m_UpscaleHandle;
        }

        public void Dispose()
        {
            m_UpscaleHandle?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingScope))
            {
                CoreUtils.SetRenderTarget(cmd, m_UpscaleHandle,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    ClearFlag.None, Color.clear);
                Blit(cmd, m_Source, m_UpscaleHandle);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
