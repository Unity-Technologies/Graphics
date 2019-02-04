using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using System.Linq;

public class CustomLWPipe : RendererSetup
{  
    private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
    private SetupLightweightConstanstPass m_SetupLightweightConstants;
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;

    public CustomLWPipe(CustomRenderGraphData data)
    {
        m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
        m_SetupLightweightConstants = new SetupLightweightConstanstPass();
        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
        m_RenderPassFeatures.AddRange(data.renderPassFeatures.Where(x => x != null));
    }

    public override void Setup(ref RenderingData renderingData)
    {
        SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
        RenderTextureDescriptor baseDescriptor = ScriptableRenderPass.CreateRenderTextureDescriptor(ref renderingData.cameraData);
        RenderTextureDescriptor shadowDescriptor = baseDescriptor;
        shadowDescriptor.dimension = TextureDimension.Tex2D;

        RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
        RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;
        
        var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
        m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
        EnqueuePass(RenderPassBlock.MainRender, m_CreateLightweightRenderTexturesPass);

        Camera camera = renderingData.cameraData.camera;

        RenderPassFeature.InjectionPoint injectionPoints = 0;
        foreach (var pass in m_RenderPassFeatures)
        {
            injectionPoints |= pass.injectionPoints;
        }

        EnqueuePass(RenderPassBlock.MainRender, m_SetupLightweightConstants);

        m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, GetCameraClearFlag(camera), camera.backgroundColor);
        EnqueuePass(RenderPassBlock.MainRender, m_RenderOpaqueForwardPass);
        
        EnqueuePasses(RenderPassBlock.MainRender, RenderPassFeature.InjectionPoint.AfterOpaqueRenderPasses, injectionPoints,
            baseDescriptor, colorHandle, depthHandle);
    }
}
