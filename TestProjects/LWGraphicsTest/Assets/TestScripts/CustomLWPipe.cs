using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


public class CustomLWPipe : MonoBehaviour, IRendererSetup
{
    private SetupForwardRenderingPass m_SetupForwardRenderingPass;
    private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
    private SetupLightweightConstanstPass m_SetupLightweightConstants;
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;

    [NonSerialized]
    private bool m_Initialized = false;

    private void Init(LightweightForwardRenderer renderer)
    {
        if (m_Initialized)
            return;

        m_SetupForwardRenderingPass = new SetupForwardRenderingPass(renderer);
        m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass(renderer);
        m_SetupLightweightConstants = new SetupLightweightConstanstPass(renderer);
        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass(renderer);

        m_Initialized = true;
    }

    public void Setup(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
        ref CullResults cullResults, ref RenderingData renderingData)
    {
        Init(renderer);

        renderer.Clear();

        renderer.SetupPerObjectLightIndices(ref cullResults, ref renderingData.lightData);
        RenderTextureDescriptor baseDescriptor = renderer.CreateRTDesc(ref renderingData.cameraData);
        RenderTextureDescriptor shadowDescriptor = baseDescriptor;
        shadowDescriptor.dimension = TextureDimension.Tex2D;

        renderer.EnqueuePass(m_SetupForwardRenderingPass);

        RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
        RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;
        
        var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
        m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
        renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);

        Camera camera = renderingData.cameraData.camera;
        bool dynamicBatching = renderingData.supportsDynamicBatching;
        RendererConfiguration rendererConfiguration = LightweightForwardRenderer.GetRendererConfiguration(renderingData.lightData.totalAdditionalLightsCount);

        renderer.EnqueuePass(m_SetupLightweightConstants);

        m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, LightweightForwardRenderer.GetCameraClearFlag(camera), camera.backgroundColor, rendererConfiguration, dynamicBatching);
        renderer.EnqueuePass(m_RenderOpaqueForwardPass);
    }
}
