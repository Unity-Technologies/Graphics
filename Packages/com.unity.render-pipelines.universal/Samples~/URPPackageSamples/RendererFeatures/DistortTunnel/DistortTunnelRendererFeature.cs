using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This Renderer Feature creates and stores all the RTHandles for the final effect.
// The RTHandles are being set as input or output for the 3 passes:
// 1. CopyColor pass: Blit the screen color texture to an RTHandle.
// 2. Tunnel pass: Render a "tunnel" object from the scene to another RTHandle.
// 3. Distort pass: Uses the two RTHandles to create the final effect, and blits the result back to screen.
public class DistortTunnelRendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingSkybox;

    public Shader distortShader;
    private Material m_DistortMaterial;

    private DistortTunnelPass_CopyColor m_CopyColorPass;
    private DistortTunnelPass_Tunnel m_TunnelPass;
    private DistortTunnelPass_Distort m_DistortPass;

    private RTHandle m_CopyColorTexHandle;
    private const string k_CopyColorTexName = "_TunnelDistortBgTexture";
    private RTHandle m_TunnelTexHandle;
    private const string k_TunnelTexName = "_TunnelDistortTexture";

    // This class stores TextureHandle references in the frame data so that they can be shared with multiple passes in the render graph system.
    public class TexRefData : ContextItem
    {
        public TextureHandle copyColorTexHandle = TextureHandle.nullHandle;
        public TextureHandle tunnelTexHandle = TextureHandle.nullHandle;

        public override void Reset()
        {
            copyColorTexHandle = TextureHandle.nullHandle;
            tunnelTexHandle = TextureHandle.nullHandle;
        }
    }

    public override void Create()
    {
        m_CopyColorPass = new DistortTunnelPass_CopyColor(passEvent);
        m_TunnelPass = new DistortTunnelPass_Tunnel(passEvent);
        m_DistortMaterial = CoreUtils.CreateEngineMaterial(distortShader);
        m_DistortPass = new DistortTunnelPass_Distort(m_DistortMaterial, passEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // Create RTHandles
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_CopyColorTexHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_CopyColorTexName );
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_TunnelTexHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_TunnelTexName );

        // Provide the necessary RTHandles to the passes
        m_CopyColorPass.SetRTHandles(ref m_CopyColorTexHandle);
        m_TunnelPass.SetRTHandles(ref m_TunnelTexHandle);
        m_DistortPass.SetRTHandles(ref m_CopyColorTexHandle,ref m_TunnelTexHandle);

        renderer.EnqueuePass(m_CopyColorPass);
        renderer.EnqueuePass(m_TunnelPass);
        renderer.EnqueuePass(m_DistortPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_DistortMaterial);

        m_DistortPass = null;
        m_TunnelPass = null;
        m_CopyColorPass = null;

        m_CopyColorTexHandle?.Release();
        m_TunnelTexHandle?.Release();
    }
}
