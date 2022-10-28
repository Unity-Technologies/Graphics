using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
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

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var target = renderingData.cameraData.renderer.cameraColorTargetHandle;
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_TempTex, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_TempTex");

        var cmd = CommandBufferPool.Get(k_CommandBufferName);

        CoreUtils.SetRenderTarget(cmd, m_TempTex);
        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        Blitter.BlitTexture(cmd, target, new Vector4(1, 1, 0, 0), m_Material, 0);
        Blitter.BlitCameraTexture(cmd, m_TempTex, target);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
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

    public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
    {
        var camDesc = renderingData.cameraData.cameraTargetDescriptor;
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

        using (var builder = renderGraph.AddRenderPass<PassData>("Blit to TempTex", out var passData, null))
        {
            var target = UniversalRenderer.m_ActiveRenderGraphColor;
            passData.tempTex = builder.UseColorBuffer(tempTex, 0);
            passData.targetTex = builder.ReadTexture(target);

            builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
            {
                Blitter.BlitTexture(rgContext.cmd, data.targetTex, new Vector4(1, 1, 0, 0), m_Material, 0);
            });
        }
        using (var builder = renderGraph.AddRenderPass<PassData>("Blit to TargetTex", out var passData, null))
        {
            var target = UniversalRenderer.m_ActiveRenderGraphColor;
            passData.targetTex = builder.UseColorBuffer(target, 0);
            passData.tempTex = builder.ReadTexture(tempTex);

            builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
            {
                Blitter.BlitTexture(rgContext.cmd, data.tempTex, new Vector4(1, 1, 0, 0), 0.0f, false);
            });
        }
    }
}
