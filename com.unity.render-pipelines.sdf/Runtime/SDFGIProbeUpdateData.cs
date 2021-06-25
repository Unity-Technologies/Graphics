using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.SDFRP;

public class SDFGIProbeUpdateData
{
    public static readonly int rsmFluxIndex = Shader.PropertyToID("RSM_FluxTexture");
    public static readonly int rsmNormalIndex = Shader.PropertyToID("RSM_NormalTexture");
    public static readonly int rsmTValueIndex = Shader.PropertyToID("RSM_tValueTexture");

    public static readonly int rsmProjectionMatrixIndex = Shader.PropertyToID("RSM_ProjectionMatrix");

    public static readonly int rsmSamplePointBufferIndex = Shader.PropertyToID("RSM_SamplePoints");

    public static readonly int outputIndex = Shader.PropertyToID("ProbeAtlasTexture");

    public static readonly int atlasTextureResolutionIndex = Shader.PropertyToID("ProbeAtlasTextureResolution");
    public static readonly int probeResolutionIndex = Shader.PropertyToID("ProbeResolution");

    public static readonly int gridOriginIndex = Shader.PropertyToID("GridOrigin");
    public static readonly int gridSizeIndex = Shader.PropertyToID("GridSize");
    public static readonly int probeDistanceIndex = Shader.PropertyToID("ProbeDistance");

    protected int m_ProbeAtlasTextureResolution;
    protected int m_ProbeResolution;

    protected Vector3 m_GridOrigin;
    protected Vector3 m_GridSize;
    protected Vector3 m_ProbeDistance;

    protected Matrix4x4 m_RSMProjectionMatrix;

    protected RenderTexture m_RSM_FluxTexture;
    protected RenderTexture m_RSM_NormalTexture;
    protected RenderTexture m_RSM_tValueTexture;

    protected RenderTexture m_ProbeAtlasTexture;

    protected ComputeBuffer m_RSMSamplePointsBuffer;


    public void InitializeGIProbeUpdateData(SDFRenderPipelineAsset currentAsset, RenderTexture atlasTexture, ComputeBuffer rsmSamplePointsBuffer)
    {
        m_ProbeAtlasTextureResolution = currentAsset.probeAtlasTextureResolution;
        m_ProbeResolution = currentAsset.probeResolution;

        m_GridOrigin = currentAsset.gridOrigin;
        m_GridSize = currentAsset.gridSize;
        m_ProbeDistance = currentAsset.probeDistance;

        m_ProbeAtlasTexture = atlasTexture;

        m_RSMSamplePointsBuffer = rsmSamplePointsBuffer;
    }

    public void SetupRSMInput(RenderTexture flux, RenderTexture normal, RenderTexture tValue, Matrix4x4 projectionMatrix)
    {
        m_RSM_FluxTexture = flux;
        m_RSM_NormalTexture = normal;
        m_RSM_tValueTexture = tValue;

        m_RSMProjectionMatrix = projectionMatrix;
    }

    public void UpdateComputeShaderVariables(CommandBuffer cmd, ComputeShader computeShader)
    {
        int kernelIndex = computeShader.FindKernel("GatherIrradiance");

        // TODO - uncomment once RSM input have been properly set
        cmd.SetComputeTextureParam(computeShader, kernelIndex, rsmFluxIndex, m_RSM_FluxTexture);
        cmd.SetComputeTextureParam(computeShader, kernelIndex, rsmNormalIndex, m_RSM_NormalTexture);
        cmd.SetComputeTextureParam(computeShader, kernelIndex, rsmTValueIndex, m_RSM_tValueTexture);
        cmd.SetComputeMatrixParam(computeShader, rsmProjectionMatrixIndex, m_RSMProjectionMatrix);

        cmd.SetComputeBufferParam(computeShader, kernelIndex, rsmSamplePointBufferIndex, m_RSMSamplePointsBuffer);

        cmd.SetComputeTextureParam(computeShader, kernelIndex, outputIndex, m_ProbeAtlasTexture);

        cmd.SetComputeIntParam(computeShader, atlasTextureResolutionIndex, m_ProbeAtlasTextureResolution);
        cmd.SetComputeIntParam(computeShader, probeResolutionIndex, m_ProbeResolution);

        cmd.SetComputeVectorParam(computeShader, gridOriginIndex, m_GridOrigin);
        cmd.SetComputeVectorParam(computeShader, gridSizeIndex, m_GridSize);
        cmd.SetComputeVectorParam(computeShader, probeDistanceIndex, m_ProbeDistance);
    }

    public static void GenerateRSMSamplePoints(int sampleCount, float sampleRadius, ComputeBuffer buffer)
    {
        float[] points = new float[sampleCount * 2];

        for (int i = 0; i < sampleCount; ++i)
        {
            float r1 = Random.Range(0.0f, 1.0f);
            float r2 = Random.Range(0.0f, 1.0f);

            points[i] = sampleRadius * r1 * Mathf.Sin(2 * Mathf.PI * r2);
            points[i + 1] = sampleRadius * r1 * Mathf.Cos(2 * Mathf.PI * r2);
        }

        buffer.SetData(points);
    }
}
