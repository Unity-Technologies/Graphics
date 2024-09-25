using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This Renderer Feature creates an RTHandle which stores a Texture2DArray with 2 slices for the final effect.
// The RTHandle is being set as input or output for the 3 passes:
// 1. CopyColor pass: Blit the screen color texture to the RTHandle's first slice.
// 2. Tunnel pass: Render a "tunnel" object from the scene to the RTHandle's second slice.
// 3. Distort pass: Uses the two slices to create the final effect, and blits the result back to screen.
public class DistortTunnelRendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingSkybox;

    public Shader distortShader;
    private Material m_DistortMaterial;

    private DistortTunnelPass_CopyColor m_CopyColorPass;
    private DistortTunnelPass_Tunnel m_TunnelPass;
    private DistortTunnelPass_Distort m_DistortPass;

    private RTHandle m_DistortTunnelTexHandle;
    private const string k_DistortTunnelTexName = "_DistortTunnelTexture";

    // This class stores TextureHandle references in the frame data so that they can be shared with multiple passes in the render graph system.
    public class TexRefData : ContextItem
    {
        public TextureHandle distortTunnelTexHandle = TextureHandle.nullHandle;

        public override void Reset()
        {
            distortTunnelTexHandle = TextureHandle.nullHandle;
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

        // Create RTHandle with 2 slices
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        desc.dimension = TextureDimension.Tex2DArray;
        desc.volumeDepth = 2;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_DistortTunnelTexHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_DistortTunnelTexName );

        // Provide the necessary RTHandles to the passes
        m_CopyColorPass.SetRTHandles(ref m_DistortTunnelTexHandle,0);
        m_TunnelPass.SetRTHandles(ref m_DistortTunnelTexHandle,1);
        m_DistortPass.SetRTHandles(ref m_DistortTunnelTexHandle);

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

        m_DistortTunnelTexHandle?.Release();
    }
}
