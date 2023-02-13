using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Same as CopyColorPass but downsampling is not supported, while viewport can be specified
internal class CopyToViewportRenderPass : ScriptableRenderPass
{
    const string m_ProfilerTag = "Copy To Viewport";

    static readonly int m_SrcTexShaderPropertyId = Shader.PropertyToID("_SrcTex");

    RTHandle m_Source;
    RTHandle m_Destination;

    Material m_CopyToViewportMaterial;

    Rect m_Viewport;

    public CopyToViewportRenderPass(Material copyToViewportMaterial)
    {
        m_CopyToViewportMaterial = copyToViewportMaterial;
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public void Setup(RTHandle source, RTHandle destination, Rect viewport)
    {
        m_Source = source;
        m_Destination = destination;
        m_Viewport = viewport;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderingUtils.ReAllocateIfNeeded(ref m_Destination, cameraTextureDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: m_Destination.name);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

        cmd.SetRenderTarget(m_Destination.nameID);
        cmd.SetGlobalTexture(m_SrcTexShaderPropertyId, m_Source.nameID);
        cmd.SetViewport(m_Viewport); // cmd.Blit would override SetViewport with the Rect of m_Destination
        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyToViewportMaterial);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
