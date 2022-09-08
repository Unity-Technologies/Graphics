using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideRenderPass : ScriptableRenderPass
{
    const string k_CommandBufferName = "Screen Coord Override";

    RTHandle m_TempTex;
    Material m_Material;

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
        cmd.Blit(m_TempTex, target);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
        m_TempTex?.Release();
    }
}
