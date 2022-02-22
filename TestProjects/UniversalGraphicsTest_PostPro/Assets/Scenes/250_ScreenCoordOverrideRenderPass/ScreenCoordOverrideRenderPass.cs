using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideRenderPass : ScriptableRenderPass
{
    static readonly int k_SourceTexProp = Shader.PropertyToID("_SourceTex");

    const string k_CommandBufferName = "Screen Coord Override";

    RTHandle m_TempTex;

    public Material material;

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var target = renderingData.cameraData.renderer.cameraColorTargetHandle;
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_TempTex, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_TempTex");

        var cmd = CommandBufferPool.Get(k_CommandBufferName);

        cmd.SetRenderTarget(m_TempTex);
        cmd.SetGlobalTexture(k_SourceTexProp, target);
        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 0);
        cmd.Blit(m_TempTex, target);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
        m_TempTex?.Release();
    }
}
