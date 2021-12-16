using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideRenderPass : ScriptableRenderPass
{
    static class ShaderProperties
    {
        public static readonly int _SourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int _TempTex = Shader.PropertyToID("_TempTex");
    }

    const string k_CommandBufferName = "Screen Coord Override";

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

        var cmd = CommandBufferPool.Get(k_CommandBufferName);

        cmd.GetTemporaryRT(ShaderProperties._TempTex, descriptor);
        cmd.SetRenderTarget(ShaderProperties._TempTex);
        cmd.SetGlobalTexture(ShaderProperties._SourceTex, target);
        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material, 0, 0);
        cmd.Blit(ShaderProperties._TempTex, target);
        cmd.ReleaseTemporaryRT(ShaderProperties._TempTex);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
