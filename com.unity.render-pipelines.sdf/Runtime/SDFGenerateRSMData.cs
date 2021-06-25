using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.SDFRP;

public class SDFGenerateRSMData
{
    public static readonly int LightPosId = Shader.PropertyToID("LightPos");
    public static readonly int LightColorId = Shader.PropertyToID("LightColor");
    public static readonly int TexelSizeId = Shader.PropertyToID("TexelSize");

    public static readonly int InvViewMatrixId = Shader.PropertyToID("InvViewMatrix");
    public static readonly int InvProjectionMatrixId = Shader.PropertyToID("InvProjectionMatrix");


    public static readonly int DistanceId = Shader.PropertyToID("Distance");
    public static readonly int NormalId = Shader.PropertyToID("Normal");
    public static readonly int FluxId = Shader.PropertyToID("Flux");

    public RenderTexture m_RSMDistanceTexture;
    public RenderTexture m_RSMNormalTexture;
    public RenderTexture m_RSMFluxTexture;
    public Matrix4x4 m_RSMProjectionMatrix;

    protected Vector3 m_CameraPos;
    protected Vector3 m_LightPos;
    protected Vector3 m_LightColor;
    protected Vector4 m_TexelSize;
    protected Matrix4x4 m_InvViewMatrix;
    protected Matrix4x4 m_InvProjectionMatrix;

    public void SetupGenerateRSMData(Camera camera, Light light)
    {
        // Calculate "camera" parameters based on light
        m_InvViewMatrix = light.transform.worldToLocalMatrix.inverse;
        // m_InvViewMatrix = camera.worldToCameraMatrix.inverse;

        // TODO: Projection matrix and width / height currently set to same values as for normal SDF pass to help with validation
        int rsmPixelWidth = camera.scaledPixelWidth;
        int rsmPixelHeight = camera.scaledPixelHeight;
        m_RSMProjectionMatrix = camera.projectionMatrix;
        m_InvProjectionMatrix = camera.projectionMatrix.inverse;
        // Matrix4x4 invProjectionMatrix = GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-rsmPixelWidth / 2, rsmPixelWidth / 2, -rsmPixelHeight / 2, rsmPixelHeight / 2, 1, 1000)).inverse;
        m_LightPos = light.transform.localToWorldMatrix.MultiplyPoint(new Vector3(0, 0, 0));

        m_LightColor = new Vector3(light.color.linear.r, light.color.linear.g, light.color.linear.b);
        // m_LightColor = new Vector3(0.2f, 0.2f, 0.2f);

        m_TexelSize.x = 1.0f / rsmPixelWidth;
        m_TexelSize.y = 1.0f / rsmPixelHeight;
        m_TexelSize.z = rsmPixelWidth;
        m_TexelSize.w = rsmPixelHeight;

        if (!m_RSMDistanceTexture)
        {
            m_RSMDistanceTexture = new RenderTexture(rsmPixelWidth, rsmPixelHeight, 0, RenderTextureFormat.ARGBHalf);
            m_RSMDistanceTexture.enableRandomWrite = true;
            m_RSMDistanceTexture.Create();
        }
        if (!m_RSMNormalTexture)
        {
            m_RSMNormalTexture = new RenderTexture(rsmPixelWidth, rsmPixelHeight, 0, RenderTextureFormat.ARGBHalf);
            m_RSMNormalTexture.enableRandomWrite = true;
            m_RSMNormalTexture.Create();
        }
        if (!m_RSMFluxTexture)
        {
            m_RSMFluxTexture = new RenderTexture(rsmPixelWidth, rsmPixelHeight, 0, RenderTextureFormat.ARGB32);
            m_RSMFluxTexture.enableRandomWrite = true;
            m_RSMFluxTexture.Create();
        }

        m_CameraPos = camera.cameraToWorldMatrix.MultiplyPoint(new Vector3(0, 0, 0));
    }

    public void UpdateComputeShaderVariables(CommandBuffer cmd, ComputeShader computeShader)
    {
        // int kernelIndex = computeShader.FindKernel("GenerateRSMKernel");
        int kernelIndex = 0;

        cmd.SetComputeTextureParam(computeShader, kernelIndex, DistanceId,  m_RSMDistanceTexture);
        cmd.SetComputeTextureParam(computeShader, kernelIndex, NormalId,  m_RSMNormalTexture);
        cmd.SetComputeTextureParam(computeShader, kernelIndex, FluxId,  m_RSMFluxTexture);

        cmd.SetComputeVectorParam(computeShader, LightPosId, m_CameraPos);
        cmd.SetComputeVectorParam(computeShader, LightColorId, m_LightColor);
        cmd.SetComputeVectorParam(computeShader, TexelSizeId, m_TexelSize);
        cmd.SetComputeMatrixParam(computeShader, InvViewMatrixId, m_InvViewMatrix);
        cmd.SetComputeMatrixParam(computeShader, InvProjectionMatrixId, m_InvProjectionMatrix);
    }
}
