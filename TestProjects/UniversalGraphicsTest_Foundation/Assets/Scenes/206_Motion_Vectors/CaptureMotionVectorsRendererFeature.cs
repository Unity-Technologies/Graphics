using UnityEngine;
using UnityEngine.Rendering.Universal;

internal class CaptureMotionVectorsRendererFeature : ScriptableRendererFeature
{
    public Shader m_Shader;
    public float m_Intensity;

    CaptureMotionVectorsPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Motion);
            m_RenderPass.SetTarget(renderer.cameraColorTarget, m_Intensity);
            renderer.EnqueuePass(m_RenderPass);
        }
    }

    public override void Create()
    {
        m_RenderPass = new CaptureMotionVectorsPass(m_Shader);
    }
}
