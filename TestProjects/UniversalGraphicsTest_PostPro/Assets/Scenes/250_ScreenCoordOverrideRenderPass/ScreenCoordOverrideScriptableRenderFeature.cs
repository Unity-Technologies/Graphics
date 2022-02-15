using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideScriptableRenderFeature : ScriptableRendererFeature
{
    ScreenCoordOverrideRenderPass m_ScriptablePass;

    public override void Create()
    {
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            if (m_ScriptablePass == null)
            {
                m_ScriptablePass = new ScreenCoordOverrideRenderPass(RenderPassEvent.AfterRenderingPostProcessing);
            }
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Cleanup();
    }
}
