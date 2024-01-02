using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// This pass performs a blit operation with a Material. The input and output textures are set by the Renderer Feature.
public class DepthBlitEdgePass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthBlitEdgePass");
    private RTHandle m_DepthHandle;
    private RTHandle m_OutputHandle; //Camera target
    private Material m_Material;

    public DepthBlitEdgePass(Material mat, RenderPassEvent evt)
    {
        renderPassEvent = evt;
        m_Material = mat;
    }

    public void SetRTHandle(ref RTHandle depthHandle, RTHandle outputHandle)
    {
        m_DepthHandle = depthHandle;
        m_OutputHandle = outputHandle;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blitter.BlitCameraTexture(cmd, m_DepthHandle, m_OutputHandle, m_Material, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }
}