using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.SDFRP;

public class SDFGIShadingData
{
    public static readonly int atlasTextureIndex = Shader.PropertyToID("ProbeAtlasTexture");
    public static readonly int atlasTextureResolutionIndex = Shader.PropertyToID("ProbeAtlasTextureResolution");
    public static readonly int probeResolutionIndex = Shader.PropertyToID("ProbeResolution");

    public static readonly int gridOriginIndex = Shader.PropertyToID("GridOrigin");
    public static readonly int gridSizeIndex = Shader.PropertyToID("GridSize");
    public static readonly int probeDistanceIndex = Shader.PropertyToID("ProbeDistance");

    public static readonly int tValueTextureIndex = Shader.PropertyToID("tValueTexture");
    public static readonly int normalTextureIndex = Shader.PropertyToID("NormalTexture");

    public static readonly int outputIndex = Shader.PropertyToID("ColorResult");

    protected int m_ProbeAtlasTextureResolution;
    protected int m_ProbeResolution;

    protected Vector3 m_GridOrigin;
    protected Vector3 m_GridSize;
    protected Vector3 m_ProbeDistance;

    protected RenderTexture m_ProbeAtlasTexture;
    protected RenderTexture m_RenderTarget; // Color input and output

    protected Texture2D m_tValueTexture;
    protected Texture2D m_normalTexture;


    public void InitializeGIShadingData(SDFRenderPipelineAsset currentAsset, RenderTexture atlasTexture)
    {
        m_ProbeAtlasTextureResolution = currentAsset.probeAtlasTextureResolution;
        m_ProbeResolution = currentAsset.probeResolution;

        m_GridOrigin = currentAsset.gridOrigin;
        m_GridSize = currentAsset.gridSize;
        m_ProbeDistance = currentAsset.probeDistance;

        m_ProbeAtlasTexture = atlasTexture;
    }

    public void SetupScreenSpaceInput(Texture2D tValue, Texture2D normal, RenderTexture renderTarget)
    {
        m_tValueTexture = tValue;
        m_normalTexture = normal;

        m_RenderTarget = renderTarget;
    }

    public void UpdateComputeShaderVariables(CommandBuffer cmd, ComputeShader computeShader)
    {
        int kernelIndex = computeShader.FindKernel("CompositeGI");

        cmd.SetComputeTextureParam(computeShader, kernelIndex, atlasTextureIndex, m_ProbeAtlasTexture);
        cmd.SetComputeIntParam(computeShader, atlasTextureResolutionIndex, m_ProbeAtlasTextureResolution);
        cmd.SetComputeIntParam(computeShader, probeResolutionIndex, m_ProbeResolution);

        cmd.SetComputeVectorParam(computeShader, gridOriginIndex, m_GridOrigin);
        cmd.SetComputeVectorParam(computeShader, gridSizeIndex, m_GridSize);
        cmd.SetComputeVectorParam(computeShader, probeDistanceIndex, m_ProbeDistance);

        // TODO - uncomment once the correct input is hooked up
        //cmd.SetComputeTextureParam(computeShader, kernelIndex, tValueTextureIndex, m_tValueTexture);
        //cmd.SetComputeTextureParam(computeShader, kernelIndex, normalTextureIndex, m_normalTexture);

        cmd.SetComputeTextureParam(computeShader, kernelIndex, outputIndex, m_RenderTarget);
    }
}
