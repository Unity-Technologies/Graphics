using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// This pass performs a blit from a source texture to a destination texture set up by the RendererFeature.
public class DistortTunnelPass_CopyColor : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DistortTunnelPass_CopyColor");
    private RTHandle m_Source;
    private RTHandle m_OutputHandle;

    public DistortTunnelPass_CopyColor(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }
    
    public void SetRTHandles(RTHandle src, ref RTHandle dest)
    {
        m_Source = src;
        m_OutputHandle = dest;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
    {
        ConfigureTarget(m_OutputHandle);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;
        
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blitter.BlitCameraTexture(cmd, m_Source, m_OutputHandle, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}