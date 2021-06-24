using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class SDFCameraData
{
    public static readonly int InvViewProjectionMatrixId = Shader.PropertyToID("InvViewProjectionMatrix");
    public static readonly int TexelSizeId = Shader.PropertyToID("TexelSize");
    public static readonly int CameraPosId = Shader.PropertyToID("CameraPos");



    // Internal camera data as we are not yet sure how to expose View in stereo context.
    // We might change this API soon.
    Matrix4x4   m_ViewMatrix;
    Matrix4x4   m_ProjectionMatrix;
    Matrix4x4   m_InvViewProjectionMatrix;

    Vector3 m_CameraPos;

    Vector4 m_TexelSize; // x = 1 / width, y = 1 / height, z = width, w = height

    RenderTexture m_RenderTarget;


    public void InitializeCameraData(Camera camera)
    {
        m_ViewMatrix = camera.worldToCameraMatrix;
        m_ProjectionMatrix = camera.projectionMatrix;

        m_InvViewProjectionMatrix = (m_ViewMatrix * m_ProjectionMatrix).inverse;
        m_CameraPos = camera.cameraToWorldMatrix.MultiplyPoint(new Vector3(0, 0, 0));

        m_TexelSize.x = 1.0f / camera.scaledPixelWidth;
        m_TexelSize.y = 1.0f / camera.scaledPixelHeight;
        m_TexelSize.z = camera.scaledPixelWidth;
        m_TexelSize.w = camera.scaledPixelHeight;

        m_RenderTarget = camera.targetTexture;
    }

    public void UpdateComputeShaderVariables(CommandBuffer cmd, ComputeShader computeShader)
    {
        cmd.SetComputeMatrixParam(computeShader, InvViewProjectionMatrixId, m_InvViewProjectionMatrix);
        cmd.SetComputeVectorParam(computeShader, TexelSizeId, m_TexelSize);
        cmd.SetComputeVectorParam(computeShader, CameraPosId, m_CameraPos);

    }

    public void UpdateGlobalShaderVariables(CommandBuffer cmd)
    {
        cmd.SetGlobalVector(TexelSizeId, m_TexelSize);
        cmd.SetGlobalVector(CameraPosId, m_CameraPos);
    }
}
