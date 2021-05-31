namespace UnityEngine.Rendering.Universal
{
    // Only to be used when Pixel Perfect Camera is present and it has Crop Frame X or Y enabled.
    // This pass simply clears BuiltinRenderTextureType.CameraTarget to black, so that the letterbox or pillarbox is black instead of garbage.
    // In the future this can be extended to draw a custom background image instead of just clearing.
    internal class PixelPerfectBackgroundPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingScope = new ProfilingSampler("Pixel Perfect Background Pass");
        private bool m_SavedIsOrthographic;
        private float m_SavedOrthographicSize;

        public PixelPerfectBackgroundPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(bool savedIsOrthographic, float savedOrthographicSize)
        {
            m_SavedIsOrthographic = savedIsOrthographic;
            m_SavedOrthographicSize = savedOrthographicSize;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingScope))
            {
                CoreUtils.SetRenderTarget(
                    cmd,
                    BuiltinRenderTextureType.CameraTarget,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    ClearFlag.Color,
                    Color.black);
            }


            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            renderingData.cameraData.camera.orthographic = m_SavedIsOrthographic;
            renderingData.cameraData.camera.orthographicSize = m_SavedOrthographicSize;
        }
    }
}
