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

    public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        ConfigureCameraTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

        foreach (var feature in rendererFeatures)
            feature.AddRenderPasses(this, ref renderingData);
        EnqueuePass(m_RenderOpaqueForwardPass);
    }

    public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        m_ForwardLights.Setup(context, ref renderingData);
    }
}
