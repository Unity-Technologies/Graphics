using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideScriptableRenderFeature : ScriptableRendererFeature
{
    ScreenCoordOverrideRenderPass m_ScriptablePass;
    Material m_Material;

    public override void Create()
    {
        m_ScriptablePass = new ScreenCoordOverrideRenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (camera.cameraType != CameraType.Game)
        {
            return;
        }

        var shader = ScreenCoordOverrideResources.GetInstance().FullScreenShader;
        if (shader == null)
        {
            return;
        }

        if (m_Material == null)
        {
            m_Material = CoreUtils.CreateEngineMaterial(shader);
        }

        m_ScriptablePass.Setup(RenderPassEvent.AfterRenderingPostProcessing, m_Material);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Cleanup();
        CoreUtils.Destroy(m_Material);
    }
}
