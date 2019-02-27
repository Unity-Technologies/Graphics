using UnityEngine;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

public class CustomLWPipe : ScriptableRenderer
{
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;

    ForwardLights m_ForwardLights;

    public CustomLWPipe(CustomRenderGraphData data) : base(data)
    {
        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, -1);
        m_ForwardLights = new ForwardLights();
    }

    public override void Setup(ref RenderingData renderingData)
    {
        RenderTextureDescriptor baseDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        cameraColorHandle = RenderTargetHandle.CameraTarget;
        cameraDepthHandle = RenderTargetHandle.CameraTarget;
        
        Camera camera = renderingData.cameraData.camera;

        for (int i = 0; i < m_RendererFeatures.Count; ++i)
        {
            m_RendererFeatures[i].AddRenderPasses(m_ActiveRenderPassQueue, baseDescriptor, cameraColorHandle, cameraDepthHandle);
        }
        m_ActiveRenderPassQueue.Sort();
        EnqueuePass(m_RenderOpaqueForwardPass);
    }

    public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        m_ForwardLights.Setup(context, ref renderingData);
    }
}
