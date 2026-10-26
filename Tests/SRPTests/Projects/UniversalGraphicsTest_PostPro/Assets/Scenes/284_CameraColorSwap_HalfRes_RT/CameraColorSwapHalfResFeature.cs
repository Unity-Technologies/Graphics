using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Swaps UniversalResourceData.cameraColor to a texture with half the resolution before post processing.
// Regression setup for UUM-145833: the post-processing viewports must follow the actual render graph
// texture sizes instead of cameraTargetDescriptor.
public class CameraColorSwapHalfResFeature : ScriptableRendererFeature
{
    class CameraColorSwapHalfResPass : ScriptableRenderPass
    {
        const string k_PassName = "CameraColorSwapHalfRes";

        public CameraColorSwapHalfResPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.cameraColor;

            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = k_PassName;
            destinationDesc.clearBuffer = false;
            destinationDesc.width = Mathf.Max(1, destinationDesc.width / 2);
            destinationDesc.height = Mathf.Max(1, destinationDesc.height / 2);
            var destination = renderGraph.CreateTexture(destinationDesc);

            renderGraph.AddBlitPass(source, destination, Vector2.one, Vector2.zero, passName: k_PassName);

            resourceData.cameraColor = destination;
        }
    }

    CameraColorSwapHalfResPass m_Pass;

    public override void Create()
    {
        m_Pass = new CameraColorSwapHalfResPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_Pass);
    }
}
