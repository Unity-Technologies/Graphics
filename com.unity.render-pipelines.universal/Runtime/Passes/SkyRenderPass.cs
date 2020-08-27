 namespace UnityEngine.Rendering.Universal
{
    public class SkyRenderPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "Sky Render";

        public SkyRenderPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            SkyManager.RenderSky(ref renderingData.cameraData, cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
