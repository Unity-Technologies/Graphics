using UnityEngine;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

public class CustomLWPipe : ScriptableRenderer
{
    private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;

    ForwardLights m_ForwardLights;

    public CustomLWPipe(CustomRenderGraphData data) : base(data)
    {
        m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass(RenderPassEvent.BeforeRendering);
        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, -1);
        m_ForwardLights = new ForwardLights();
    }

    public override void Setup(ref RenderingData renderingData)
    {
        RenderTextureDescriptor baseDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderTextureDescriptor shadowDescriptor = baseDescriptor;
        shadowDescriptor.dimension = TextureDimension.Tex2D;

        RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
        RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;

        int sampleCount = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
        m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
        EnqueuePass(m_CreateLightweightRenderTexturesPass);

        Camera camera = renderingData.cameraData.camera;

        for (int i = 0; i < m_RendererFeatures.Count; ++i)
        {
            m_RendererFeatures[i].AddRenderPasses(m_AdditionalRenderPasses, baseDescriptor, colorHandle, depthHandle);
        }
        m_AdditionalRenderPasses.Sort( (lhs, rhs)=>lhs.renderPassEvent.CompareTo(rhs.renderPassEvent));
        int customRenderPassIndex = 0;

        m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, GetCameraClearFlag(camera.clearFlags), camera.backgroundColor);
        EnqueuePass(m_RenderOpaqueForwardPass);

        EnqueueAdditionalRenderPasses(RenderPassEvent.AfterRenderingOpaques, ref customRenderPassIndex,
            ref renderingData);
    }

    public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        m_ForwardLights.Setup(context, ref renderingData);
    }
}
