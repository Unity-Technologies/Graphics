using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideScriptableRenderFeature : ScriptableRendererFeature
{
    class ScreenCoordOverrideFullScreenPass : ScriptableRenderPass
    {
        Material m_Material;

        public ScreenCoordOverrideFullScreenPass(Material material)
        {
            m_Material = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Test Screen Coord Override");
            Blit(cmd, ref renderingData, m_Material);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    const string k_ShaderPath = "Hidden/Test/ScreenCoordOverrideFullScreen";

    ScreenCoordOverrideFullScreenPass m_ScriptablePass;
    Material m_Material;

    public override void Create()
    {
        if (m_Material == null)
        {
            m_Material = CoreUtils.CreateEngineMaterial(k_ShaderPath);
        }

        m_ScriptablePass = new ScreenCoordOverrideFullScreenPass(m_Material);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}
