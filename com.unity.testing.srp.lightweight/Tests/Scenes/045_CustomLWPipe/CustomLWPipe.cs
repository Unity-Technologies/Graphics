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

        ConfigureCameraTarget(RenderTargetHandle.CameraTarget, RenderTargetHandle.CameraTarget);

        foreach (var feature in rendererFeatures)
            feature.AddRenderPasses(this, baseDescriptor, cameraColorHandle, cameraDepthHandle);
        EnqueuePass(m_RenderOpaqueForwardPass);
    }

    public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        m_ForwardLights.Setup(context, ref renderingData);
    }
}
