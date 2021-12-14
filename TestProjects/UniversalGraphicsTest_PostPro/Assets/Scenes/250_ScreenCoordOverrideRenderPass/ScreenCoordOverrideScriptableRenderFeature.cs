using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScreenCoordOverrideScriptableRenderFeature : ScriptableRendererFeature
{
    ScreenCoordOverrideRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new ScreenCoordOverrideRenderPass(RenderPassEvent.AfterRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            Assert.IsNotNull(m_ScriptablePass);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        Assert.IsNotNull(m_ScriptablePass);
        m_ScriptablePass.Cleanup();
    }
}
