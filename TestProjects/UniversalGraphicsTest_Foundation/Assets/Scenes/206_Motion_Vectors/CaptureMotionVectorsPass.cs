using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class CaptureMotionVectorsPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("MotionVecDebug");
    Material m_Material;
    RenderTargetHandle m_MotionVectorHandle;
    RenderTargetIdentifier CameraColorTarget;
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
        CameraColorTarget = colorHandle;
        m_intensity = intensity;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        //var rtd = cameraTextureDescriptor;
        //// Configure Render Target
        //m_MotionVectorHandle.Init("_MotionVecDebug");
        //cmd.GetTemporaryRT(m_MotionVectorHandle.id, rtd, FilterMode.Point);
        //ConfigureTarget(m_MotionVectorHandle.Identifier(), m_MotionVectorHandle.Identifier());
        ////cmd.SetRenderTarget(m_MotionVectorHandle.Identifier(), m_MotionVectorHandle.Identifier());
        //
        //// TODO: Why do clear here?
        //cmd.ClearRenderTarget(true, true, Color.black, 1.0f);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
        {
            Debug.LogError("Material null in motion vec debug");
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            m_Material.SetFloat("_Intensity", m_intensity);
            cmd.Blit(CameraColorTarget, CameraColorTarget, m_Material);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}
