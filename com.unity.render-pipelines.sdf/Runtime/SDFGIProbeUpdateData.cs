using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.SDFRP;

public class SDFGIProbeUpdateData
{
    public static readonly int rsmFluxIndex = Shader.PropertyToID("RSM_FluxTexture");
    public static readonly int rsmNormalIndex = Shader.PropertyToID("RSM_NormalTexture");
    public static readonly int rsmTValueIndex = Shader.PropertyToID("RSM_tValueTexture");

    public static readonly int rsmProjectionMatrixIndex = Shader.PropertyToID("RSM_ProjectionMatrix");

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

    protected Texture2D m_RSM_FluxTexture;
    protected Texture2D m_RSM_NormalTexture;
    protected Texture2D m_RSM_tValueTexture;

    protected RenderTexture m_ProbeAtlasTexture;

    public void InitializeGIProbeUpdateData(SDFRenderPipelineAsset currentAsset, RenderTexture atlasTexture)
    {
        m_ProbeAtlasTextureResolution = currentAsset.probeAtlasTextureResolution;
        m_ProbeResolution = currentAsset.probeResolution;

        m_GridOrigin = currentAsset.gridOrigin;
        m_GridSize = currentAsset.gridSize;
        m_ProbeDistance = currentAsset.probeDistance;

        m_ProbeAtlasTexture = atlasTexture;
    }

    public void SetupRSMInput(Texture2D flux, Texture2D normal, Texture2D tValue, Matrix4x4 projectionMatrix)
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
        //cmd.SetComputeTextureParam(computeShader, kernelIndex, rsmFluxIndex, m_RSM_FluxTexture);
        //cmd.SetComputeTextureParam(computeShader, kernelIndex, rsmNormalIndex, m_RSM_NormalTexture);
        //cmd.SetComputeTextureParam(computeShader, kernelIndex, rsmTValueIndex, m_RSM_tValueTexture);
        //cmd.SetComputeMatrixParam(computeShader, rsmProjectionMatrixIndex, m_RSMProjectionMatrix);

        cmd.SetComputeTextureParam(computeShader, kernelIndex, outputIndex, m_ProbeAtlasTexture);

        cmd.SetComputeIntParam(computeShader, atlasTextureResolutionIndex, m_ProbeAtlasTextureResolution);
        cmd.SetComputeIntParam(computeShader, probeResolutionIndex, m_ProbeResolution);

        cmd.SetComputeVectorParam(computeShader, gridOriginIndex, m_GridOrigin);
        cmd.SetComputeVectorParam(computeShader, gridSizeIndex, m_GridSize);
        cmd.SetComputeVectorParam(computeShader, probeDistanceIndex, m_ProbeDistance);
    }
}
