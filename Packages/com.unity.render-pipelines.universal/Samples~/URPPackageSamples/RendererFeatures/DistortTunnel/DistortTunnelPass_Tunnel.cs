using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This pass renders a certain object (an object called "Tunnel" in this sample) to an RTHandle set by the Renderer Feature.
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

    public void SetRTHandles(ref RTHandle dest)
    {
        m_OutputHandle = dest;
    }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

    // Unity calls the Configure method in the Compatibility mode (non-RenderGraph path)
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
    {
        ConfigureTarget(m_OutputHandle);
    }

    // Unity calls the Execute method in the Compatibility mode
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Get the "Tunnel" renderer object from the scene (example-specific code)
        SetTunnelObject();
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

#pragma warning restore 618, 672

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
            // Set RTHandle as a texture resource used by this render graph instance
            TextureHandle destination = renderGraph.ImportTexture(m_OutputHandle);
            texRefData.tunnelTexHandle = destination;

            if (!destination.IsValid())
                return;

            passData.tunnelObject = m_TunnelObject;
            passData.tunnelMaterial = m_TunnelObject.sharedMaterial;
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write); // Set the RTHandle as the output
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRenderer(data.tunnelObject, data.tunnelMaterial,0,0);
            });
        }
    }
}
