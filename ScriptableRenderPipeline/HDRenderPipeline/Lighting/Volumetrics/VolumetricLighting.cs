using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{

[GenerateHLSL]
public struct VolumeProperties
{
    public Vector3 scattering; // [0, 1], prefer sRGB
    public float   extinction; // [0, 1], prefer sRGB
    public float   asymmetry;  // Global (scene) property
    public float   align16_0;
    public float   align16_1;
    public float   align16_2;

    public static VolumeProperties GetNeutralVolumeProperties()
    {
        VolumeProperties properties = new VolumeProperties();

        properties.scattering = Vector3.zero;
        properties.extinction = 0;
        properties.asymmetry  = 0;

        return properties;
    }
} // struct VolumeProperties

[Serializable]
public class VolumeParameters
{
    public Bounds bounds;       // Position and dimensions in meters
    public Color  albedo;       // Single scattering albedo [0, 1]
    public float  meanFreePath; // In meters [1, inf]. Should be chromatic - this is an optimization!
    public float  anisotropy;   // [-1, 1]; 0 = isotropic

    public VolumeParameters()
    {
        bounds       = new Bounds(Vector3.zero, Vector3.positiveInfinity);
        albedo       = new Color(0.5f, 0.5f, 0.5f);
        meanFreePath = 10.0f;
        anisotropy   = 0.0f;
    }

    public bool IsVolumeUnbounded()
    {
        return bounds.size.x == float.PositiveInfinity &&
               bounds.size.y == float.PositiveInfinity &&
               bounds.size.z == float.PositiveInfinity;
    }

    public Vector3 GetAbsorptionCoefficient()
    {
        float   extinction = GetExtinctionCoefficient();
        Vector3 scattering = GetScatteringCoefficient();

        return Vector3.Max(new Vector3(extinction, extinction, extinction) - scattering, Vector3.zero);
    }

    public Vector3 GetScatteringCoefficient()
    {
        float extinction = GetExtinctionCoefficient();

        return new Vector3(albedo.r * extinction, albedo.g * extinction, albedo.b * extinction);
    }

    public float GetExtinctionCoefficient()
    {
        return 1.0f / meanFreePath;
    }

    public void Constrain()
    {
        bounds.size = Vector3.Max(bounds.size, Vector3.zero);

        albedo.r = Mathf.Clamp01(albedo.r);
        albedo.g = Mathf.Clamp01(albedo.g);
        albedo.b = Mathf.Clamp01(albedo.b);

        meanFreePath = Mathf.Max(meanFreePath, 1.0f);

        anisotropy = Mathf.Clamp(anisotropy, -1.0f, 1.0f);
    }

    public VolumeProperties GetProperties()
    {
        VolumeProperties properties = new VolumeProperties();

        properties.scattering = GetScatteringCoefficient();
        properties.extinction = GetExtinctionCoefficient();
        properties.asymmetry  = anisotropy;

        return properties;
    }
} // class VolumeParameters

public partial class HDRenderPipeline : RenderPipeline
{
    bool  m_VolumetricLightingEnabled   = false;
    int   m_VolumetricBufferTileSize    = 4;     // In pixels, must be a power of 2
    float m_VolumetricBufferMaxFarPlane = 32.0f; // Distance in meters

    RenderTexture          m_VolumetricLightingBufferCurrentFrame = null;
    RenderTexture          m_VolumetricLightingBufferAccumulation = null;

    RenderTargetIdentifier m_VolumetricLightingBufferCurrentFrameRT;
    RenderTargetIdentifier m_VolumetricLightingBufferAccumulationRT;

    ComputeShader m_VolumetricLightingCS { get { return m_Asset.renderPipelineResources.volumetricLightingCS; } }

    void ComputeVolumetricBufferResolution(int screenWidth, int screenHeight, ref int w, ref int h, ref int d)
    {
        int t = m_VolumetricBufferTileSize;
        Debug.Assert((t & (t - 1)) == 0, "m_VolumetricBufferTileSize must be a power of 2.");

        // Ceil(ScreenSize / TileSize).
        w = (screenWidth  + t - 1) / t;
        h = (screenHeight + t - 1) / t;
        d = 64;

        // Cell-centered -> node-centered.
        w += 1;
        h += 1;
    }

    // Uses a logarithmic depth encoding.
    // Near plane: depth = 0; far plane: depth = 1.
    // x = n, y = log2(f/n), z = 1/n, w = 1/log2(f/n).
    Vector4 ComputeVolumetricBufferDepthEncodingParams(float cameraNearPlane, float cameraFarPlane, float sliceCount)
    {
        float n = cameraNearPlane;
        float f = Math.Min(cameraFarPlane, m_VolumetricBufferMaxFarPlane);
        float s = sliceCount;

        // Cell-centered -> node-centered.
        // Remap near and far planes s.t. Depth(n) = 0.5/s and Depth(f) = 1-0.5/s.
        float x = Mathf.Pow(n, (s - 0.5f) / (s - 1)) * Mathf.Pow(f, -0.5f / (s - 1));
        f = n * f / x;
        n = x;

        Vector4 depthParams = new Vector4();

        depthParams.x = n;
        depthParams.y = Mathf.Log(f / n, 2);
        depthParams.z = 1 / depthParams.x;
        depthParams.w = 1 / depthParams.y;

        return depthParams;
    }

    void CreateVolumetricLightingBuffers(int width, int height)
    {
        if (m_VolumetricLightingBufferAccumulation != null)
        {
            m_VolumetricLightingBufferAccumulation.Release();
            m_VolumetricLightingBufferCurrentFrame.Release();
        }

        int w = 0, h = 0, d = 0;
        ComputeVolumetricBufferResolution(width, height, ref w, ref h, ref d);

        m_VolumetricLightingBufferCurrentFrame = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear); // UAV with ARGBHalf appears to be broken...
        m_VolumetricLightingBufferCurrentFrame.filterMode        = FilterMode.Bilinear;    // Custom trilinear
        m_VolumetricLightingBufferCurrentFrame.dimension         = TextureDimension.Tex3D; // Prefer 3D Thick tiling layout
        m_VolumetricLightingBufferCurrentFrame.volumeDepth       = d;
        m_VolumetricLightingBufferCurrentFrame.enableRandomWrite = true;
        m_VolumetricLightingBufferCurrentFrame.Create();
        m_VolumetricLightingBufferCurrentFrameRT = new RenderTargetIdentifier(m_VolumetricLightingBufferCurrentFrame);

        m_VolumetricLightingBufferAccumulation = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear); // UAV with ARGBHalf appears to be broken...
        m_VolumetricLightingBufferAccumulation.filterMode        = FilterMode.Bilinear;    // Custom trilinear
        m_VolumetricLightingBufferAccumulation.dimension         = TextureDimension.Tex3D; // Prefer 3D Thick tiling layout
        m_VolumetricLightingBufferAccumulation.volumeDepth       = d;
        m_VolumetricLightingBufferAccumulation.enableRandomWrite = true;
        m_VolumetricLightingBufferAccumulation.Create();
        m_VolumetricLightingBufferAccumulationRT = new RenderTargetIdentifier(m_VolumetricLightingBufferAccumulation);
    }

    void ClearVolumetricLightingBuffers(CommandBuffer cmd, bool isFirstFrame)
    {
        using (new ProfilingSample(cmd, "Clear volumetric lighting buffers"))
        {
            CoreUtils.SetRenderTarget(cmd, m_VolumetricLightingBufferCurrentFrameRT, ClearFlag.Color, Color.black);

            if (isFirstFrame)
            {
                CoreUtils.SetRenderTarget(cmd, m_VolumetricLightingBufferAccumulation, ClearFlag.Color, Color.black);
            }
        }
    }

    // Returns 'true' if the global fog is enabled, 'false' otherwise.
    public static bool SetGlobalVolumeProperties(bool volumetricLightingEnabled, CommandBuffer cmd, ComputeShader cs = null)
    {
        HomogeneousFog globalFogComponent = null;

        if (volumetricLightingEnabled)
        {
            HomogeneousFog[] fogComponents = Object.FindObjectsOfType(typeof(HomogeneousFog)) as HomogeneousFog[];

            foreach (HomogeneousFog fogComponent in fogComponents)
            {
                if (fogComponent.enabled && fogComponent.volumeParameters.IsVolumeUnbounded())
                {
                    globalFogComponent = fogComponent;
                    break;
                }
            }
        }

        // TODO: may want to cache these results somewhere.
        VolumeProperties globalFogProperties = (globalFogComponent != null) ? globalFogComponent.volumeParameters.GetProperties()
                                                                            : VolumeProperties.GetNeutralVolumeProperties();
        if (cs)
        {
            cmd.SetComputeVectorParam(cs, HDShaderIDs._GlobalFog_Scattering, globalFogProperties.scattering);
            cmd.SetComputeFloatParam( cs, HDShaderIDs._GlobalFog_Extinction, globalFogProperties.extinction);
            cmd.SetComputeFloatParam( cs, HDShaderIDs._GlobalFog_Asymmetry,  globalFogProperties.asymmetry);
        }
        else
        {
            cmd.SetGlobalVector(HDShaderIDs._GlobalFog_Scattering, globalFogProperties.scattering);
            cmd.SetGlobalFloat( HDShaderIDs._GlobalFog_Extinction, globalFogProperties.extinction);
            cmd.SetGlobalFloat( HDShaderIDs._GlobalFog_Asymmetry,  globalFogProperties.asymmetry);
        }

        return (globalFogComponent != null);
    }

    public void SetVolumetricLightingData(bool volumetricLightingEnabled, CommandBuffer cmd, HDCamera camera, ComputeShader cs = null, int kernel = 0)
    {
        SetGlobalVolumeProperties(volumetricLightingEnabled, cmd, cs);

        // Compute dimensions of the buffer.
        int w = 0, h = 0, d = 0;
        ComputeVolumetricBufferResolution((int)camera.screenSize.x, (int)camera.screenSize.y, ref w, ref h, ref d);
        Vector4 resolution = new Vector4(w, h, d, Mathf.Log(m_VolumetricBufferTileSize, 2));

        // Compute custom near and far flipping planes of the volumetric lighting buffer.
        Vector4 depthParams = ComputeVolumetricBufferDepthEncodingParams(camera.camera.nearClipPlane, camera.camera.farClipPlane, d);

        if (cs)
        {
            cmd.SetComputeVectorParam( cs,         HDShaderIDs._vBufferResolution,          resolution);
            cmd.SetComputeVectorParam( cs,         HDShaderIDs._vBufferDepthEncodingParams, depthParams);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VolumetricLightingBuffer,   m_VolumetricLightingBufferCurrentFrameRT);
        }
        else
        {
            cmd.SetGlobalVector( HDShaderIDs._vBufferResolution,          resolution);
            cmd.SetGlobalVector( HDShaderIDs._vBufferDepthEncodingParams, depthParams);
            cmd.SetGlobalTexture(HDShaderIDs._VolumetricLightingBuffer,   m_VolumetricLightingBufferCurrentFrameRT);
        }
    }

    void VolumetricLightingPass(HDCamera camera, CommandBuffer cmd)
    {
        if (!SetGlobalVolumeProperties(m_VolumetricLightingEnabled, cmd, m_VolumetricLightingCS)) { return; }

        using (new ProfilingSample(cmd, "VolumetricLighting"))
        {
            bool enableClustered = m_Asset.tileSettings.enableClustered && m_Asset.tileSettings.enableTileAndCluster;

            int volumetricLightingKernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClustered"
                                                                                             : "VolumetricLightingAllLights");
            camera.SetupComputeShader(m_VolumetricLightingCS, cmd);

            // Compute dimensions of the buffer.
            int w = 0, h = 0, d = 0;
            ComputeVolumetricBufferResolution((int)camera.screenSize.x, (int)camera.screenSize.y, ref w, ref h, ref d);
            Vector4 resolution = new Vector4(w, h, d, Mathf.Log(m_VolumetricBufferTileSize, 2));

            // Compute custom near and far flipping planes of the volumetric lighting buffer.
            Vector4 depthParams = ComputeVolumetricBufferDepthEncodingParams(camera.camera.nearClipPlane, camera.camera.farClipPlane, d);

            // Cell-centered -> node-centered.
            float vFoV = camera.camera.fieldOfView * Mathf.Deg2Rad;
            vFoV = 2 * Mathf.Atan(Mathf.Tan(0.5f * vFoV) * h / (h - 1));

            // Compose the matrix which allows us to compute the world space view direction.
            Matrix4x4 transform = SkyManager.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, new Vector4(w, h, 1.0f / w, 1.0f / h), camera.viewMatrix, false);

            cmd.SetComputeVectorParam( m_VolumetricLightingCS, HDShaderIDs._vBufferResolution,          resolution);
            cmd.SetComputeVectorParam( m_VolumetricLightingCS, HDShaderIDs._vBufferDepthEncodingParams, depthParams);
            cmd.SetComputeMatrixParam( m_VolumetricLightingCS, HDShaderIDs._vBufferCoordToViewDirWS,    transform);
            cmd.SetComputeVectorParam( m_VolumetricLightingCS, HDShaderIDs._Time,                       Shader.GetGlobalVector(HDShaderIDs._Time));
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, volumetricLightingKernel, HDShaderIDs._VolumetricLightingBufferCurrentFrame, m_VolumetricLightingBufferCurrentFrameRT);
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, volumetricLightingKernel, HDShaderIDs._VolumetricLightingBufferAccumulation, m_VolumetricLightingBufferAccumulationRT);

            // Pass clustered light data (if present) to the compute shader.
            m_LightLoop.PushGlobalParams(camera.camera, cmd, m_VolumetricLightingCS, volumetricLightingKernel, true);
            cmd.SetComputeIntParam(m_VolumetricLightingCS, HDShaderIDs._UseTileLightList, 0);

            cmd.DispatchCompute(m_VolumetricLightingCS, volumetricLightingKernel, (w + 15) / 16, (h + 15) / 16, 1);
        }
    }
} // class HDRenderPipeline
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
