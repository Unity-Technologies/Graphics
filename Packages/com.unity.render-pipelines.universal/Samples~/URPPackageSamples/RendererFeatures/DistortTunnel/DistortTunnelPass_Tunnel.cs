using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This pass renders a certain object (an object called "Tunnel" in this sample) to the second slice of the RTHandle set by the Renderer Feature.
public class DistortTunnelPass_Tunnel : ScriptableRenderPass
{
    class PassData
    {
        public Renderer tunnelObject;
        public Material tunnelMaterial;
    }

    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DistortTunnelPass_Tunnel");
    private RTHandle m_OutputHandle;
    private Renderer m_TunnelObject;
    private int m_Slice;

    public DistortTunnelPass_Tunnel(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }

    private void SetTunnelObject()
    {
        if (m_TunnelObject != null)
            return;

        var tunnelGO = GameObject.Find("Tunnel");
        if (tunnelGO != null)
            m_TunnelObject = tunnelGO.GetComponent<Renderer>();
    }

    public void SetRTHandles(ref RTHandle dest, int slice)
    {
        m_OutputHandle = dest;
        m_Slice = slice;
    }

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        DistortTunnelRendererFeature.TexRefData texRefData = frameData.GetOrCreate<DistortTunnelRendererFeature.TexRefData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Get the "Tunnel" renderer object from the scene (example-specific code)
        SetTunnelObject();
        if (!m_TunnelObject)
            return;

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("DistortTunnelPass_Tunnel", out var passData))
        {
            // Get the texture resource that is passed to this render graph instance from the previous pass
            TextureHandle destination = texRefData.distortTunnelTexHandle;

            if (!destination.IsValid())
                return;

            passData.tunnelObject = m_TunnelObject;
            passData.tunnelMaterial = m_TunnelObject.sharedMaterial;
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write, 0, depthSlice: m_Slice); // Set the TextureHandle as the output
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRenderer(data.tunnelObject, data.tunnelMaterial,0,0);
            });
        }
    }
}
