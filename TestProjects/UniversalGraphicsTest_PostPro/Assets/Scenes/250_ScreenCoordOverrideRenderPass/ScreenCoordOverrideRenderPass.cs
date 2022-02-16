using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideRenderPass : ScriptableRenderPass
{
    const string k_CommandBufferName = "Screen Coord Override";

    RTHandle m_TempTex;
    Material m_Material;

    public ScreenCoordOverrideRenderPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
        m_Material = CoreUtils.CreateEngineMaterial(ScreenCoordOverrideResources.GetInstance().FullScreenShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // Why can't we rely on the API implied lifecycle?
        if (m_Material == null)
        {
            return;
        }

        var target = renderingData.cameraData.renderer.cameraColorTargetHandle;
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_TempTex, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_TempTex");

        var cmd = CommandBufferPool.Get(k_CommandBufferName);

        Blitter.BlitCameraTexture(cmd, target, m_TempTex, m_Material, 0);
        Blitter.BlitCameraTexture(cmd, m_TempTex, target);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
        m_TempTex?.Release();
        CoreUtils.Destroy(m_Material);
    }
}
