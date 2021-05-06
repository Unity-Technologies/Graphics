using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class CaptureMotionVectorsPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("MotionVecDebug");
    Material m_Material;
    RenderTargetIdentifier m_CameraColorTarget;
    float m_intensity;

    public CaptureMotionVectorsPass(Shader shader)
    {
        if (shader != null)
            m_Material = new Material(shader);
        else
            Debug.LogError("Null shader");

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void SetTarget(RenderTargetIdentifier colorHandle, float intensity)
    {
        m_CameraColorTarget = colorHandle;
        m_intensity = intensity;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            m_Material.SetFloat("_Intensity", m_intensity);
            cmd.Blit(m_CameraColorTarget, m_CameraColorTarget, m_Material);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}
