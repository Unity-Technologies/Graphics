using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// This pass renders a certain object (object called "Tunnel" in this sample) to an RTHandle set by the Renderer Feature.
public class DistortTunnelPass_Tunnel : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DistortTunnelPass_Tunnel");
    private RTHandle m_OutputHandle;
    private Renderer m_TunnelObject;

    public DistortTunnelPass_Tunnel(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        
        // Get the "Tunnel" renderer object from the scene
        SetTunnelObject();
    }

    private void SetTunnelObject()
    {
        if (m_TunnelObject != null) 
            return;
        
        var tunnelGO = GameObject.Find("Tunnel");
        if (tunnelGO != null)
            m_TunnelObject = tunnelGO.GetComponent<Renderer>();
    }
    
    public void SetRTHandles(ref RTHandle dest)
    {
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

        if (!m_TunnelObject)
            return;
        
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            cmd.DrawRenderer(m_TunnelObject, m_TunnelObject.sharedMaterial,0,0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}