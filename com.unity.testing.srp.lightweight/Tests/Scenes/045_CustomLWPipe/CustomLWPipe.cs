using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


[CreateAssetMenu()]
public class CustomRenderGraphData : RenderGraphData
{
    public override RenderGraph Create()
    {
        return new CustomLWPipe(this);
    }
}

public class CustomLWPipe : RenderGraph
{
    private SetupForwardRenderingPass m_SetupForwardRenderingPass;
    private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
    private SetupLightweightConstanstPass m_SetupLightweightConstants;
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;

    public CustomLWPipe(CustomRenderGraphData data)
    {
        m_SetupForwardRenderingPass = new SetupForwardRenderingPass();
        m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
        m_SetupLightweightConstants = new SetupLightweightConstanstPass();
        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
    }

    public override void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
        RenderTextureDescriptor baseDescriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
        RenderTextureDescriptor shadowDescriptor = baseDescriptor;
        shadowDescriptor.dimension = TextureDimension.Tex2D;

        renderer.EnqueuePass(m_SetupForwardRenderingPass);

        RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
        RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;
        
        var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
        m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
        renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);

        Camera camera = renderingData.cameraData.camera;
        var rendererConfiguration = ScriptableRenderer.GetRendererConfiguration(renderingData.lightData.additionalLightsCount);

        m_SetupLightweightConstants.Setup(renderer.maxVisibleAdditionalLights, renderer.perObjectLightIndices);
        renderer.EnqueuePass(m_SetupLightweightConstants);

        m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, ScriptableRenderer.GetCameraClearFlag(camera), camera.backgroundColor, rendererConfiguration);
        renderer.EnqueuePass(m_RenderOpaqueForwardPass);
    }
}
