using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Same as CopyColorPass but downsampling is not supported, while viewport can be specified
internal class CopyToViewportRenderPass : ScriptableRenderPass
{
    const string m_ProfilerTag = "Copy To Viewport";

    static readonly int m_SrcTexShaderPropertyId = Shader.PropertyToID("_SrcTex");

    RenderTargetIdentifier m_Source;
    RenderTargetHandle m_Destination;

    Material m_CopyToViewportMaterial;

    Rect m_Viewport;

    public CopyToViewportRenderPass(Material copyToViewportMaterial)
    {
        m_CopyToViewportMaterial = copyToViewportMaterial;
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Rect viewport)
    {
        m_Source = source;
        m_Destination = destination;
        m_Viewport = viewport;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        cmd.GetTemporaryRT(m_Destination.id, cameraTextureDescriptor, FilterMode.Point);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

        cmd.SetRenderTarget(m_Destination.Identifier());
        cmd.SetGlobalTexture(m_SrcTexShaderPropertyId, m_Source);
        cmd.SetViewport(m_Viewport); // cmd.Blit would override SetViewport with the Rect of m_Destination
        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyToViewportMaterial);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(m_Destination.id);
    }
}
