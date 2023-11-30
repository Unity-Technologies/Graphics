using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class CaptureMotionVectorsRendererFeature : ScriptableRendererFeature
{
    public Shader m_Shader;
    public float m_Intensity;

    Material m_Material;

    CaptureMotionVectorsPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Motion);
            m_RenderPass.SetIntensity(m_Intensity);
            #pragma warning disable CS0618 // Type or member is obsolete
            renderer.EnqueuePass(m_RenderPass);
            #pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    public override void Create()
    {
        if (m_Shader != null)
            m_Material = new Material(m_Shader);

        m_RenderPass = new CaptureMotionVectorsPass(m_Material);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}
