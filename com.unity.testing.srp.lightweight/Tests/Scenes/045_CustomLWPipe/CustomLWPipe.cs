using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

public class CustomLWPipe : RendererSetup
{
    private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;

    ForwardLights m_ForwardLights;
    List<ScriptableRenderPass> m_AdditionalRenderPasses = new List<ScriptableRenderPass>(10);

    public CustomLWPipe(CustomRenderGraphData data) : base(data)
    {
        m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(RenderQueueRange.opaque, -1);
        m_ForwardLights = new ForwardLights();
    }

    public override void Setup(ref RenderingData renderingData)
    {
        RenderTextureDescriptor baseDescriptor = ScriptableRenderPass.CreateRenderTextureDescriptor(ref renderingData.cameraData);
        RenderTextureDescriptor shadowDescriptor = baseDescriptor;
        shadowDescriptor.dimension = TextureDimension.Tex2D;

        RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
        RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;

        var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
        m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
        EnqueuePass(m_CreateLightweightRenderTexturesPass);

        Camera camera = renderingData.cameraData.camera;

        m_AdditionalRenderPasses.Clear();
        for (int i = 0; i < m_RenderPassFeatures.Count; ++i)
        {
            m_RenderPassFeatures[i].AddRenderPasses(m_AdditionalRenderPasses, baseDescriptor, colorHandle, depthHandle);
        }
        m_AdditionalRenderPasses.Sort( (lhs, rhs)=>lhs.renderPassEvent.CompareTo(rhs.renderPassEvent));
        int customRenderPassIndex = 0;

        m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, GetCameraClearFlag(camera), camera.backgroundColor);
        EnqueuePass(m_RenderOpaqueForwardPass);

        EnqueuePasses(RenderPassEvent.AfterRenderingOpaques, m_AdditionalRenderPasses, ref customRenderPassIndex,
            ref renderingData);
    }

    public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        m_ForwardLights.Setup(context, ref renderingData);
    }
}
