using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideRenderPass : ScriptableRenderPass
{
    const string k_CommandBufferName = "Screen Coord Override";

    RTHandle m_TempTex;
    static Material m_Material;

    public void Setup(RenderPassEvent renderPassEvent, Material material)
    {
        this.renderPassEvent = renderPassEvent;
        m_Material = material;
    }

    public void Cleanup()
    {
        m_TempTex?.Release();
    }

    private class PassData
    {
        internal TextureHandle tempTex;
        internal TextureHandle targetTex;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        var camDesc = cameraData.cameraTargetDescriptor;
        TextureDesc desc = new TextureDesc(camDesc.width, camDesc.height);//renderingData.cameraData.cameraTargetDescriptor;

        desc.dimension = camDesc.dimension;
        desc.clearBuffer = true;
        desc.bindTextureMS = camDesc.bindMS;
        desc.colorFormat = camDesc.graphicsFormat;
        desc.depthBufferBits = 0;
        desc.slices = camDesc.volumeDepth;
        desc.msaaSamples = (MSAASamples)camDesc.msaaSamples;
        desc.name = "_TempTex";
        desc.enableRandomWrite = false;

        TextureHandle tempTex = renderGraph.CreateTexture(desc);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Blit to TempTex", out var passData))
        {
            var target = resourceData.activeColorTexture;
            passData.tempTex = tempTex;
            builder.SetRenderAttachment(tempTex, 0, AccessFlags.Write);
            passData.targetTex = target;
            builder.UseTexture(target, AccessFlags.Read);

            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
            {
                Blitter.BlitTexture(rgContext.cmd, data.targetTex, new Vector4(1, 1, 0, 0), m_Material, 0);
            });
        }
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Blit to TargetTex", out var passData))
        {
            var target = resourceData.activeColorTexture;
            passData.targetTex = target;
            builder.SetRenderAttachment(target, 0, AccessFlags.Write);
            passData.tempTex = tempTex;
            builder.UseTexture(tempTex, AccessFlags.Read);

            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
            {
                Blitter.BlitTexture(rgContext.cmd, data.tempTex, new Vector4(1, 1, 0, 0), 0.0f, false);
            });
        }
    }
}
