using System;
using UnityEngine.Rendering;

#if UNITY_EDITOR
    using UnityEditor;
#endif

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
}

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
}

public partial class HDRenderPipeline : RenderPipeline
{
    bool m_VolumetricLightingEnabled = false; // Must be able to change this dynamically
    int  m_VolumetricBufferTileSize  = 4;     // In pixels, must be a power of 2

    RenderTexture          m_VolumetricLightingBufferCurrentFrame = null;
    RenderTexture          m_VolumetricLightingBufferAccumulated  = null;
    RenderTargetIdentifier m_VolumetricLightingBufferCurrentFrameRT;
   // RenderTargetIdentifier m_VolumetricLightingBufferAccumulatedRT;

    ComputeShader m_VolumetricLightingCS { get { return m_Asset.renderPipelineResources.volumetricLightingCS; } }

    void CreateVolumetricLightingBuffers(int width, int height)
    {
        if (m_VolumetricLightingBufferAccumulated != null)
        {
            m_VolumetricLightingBufferAccumulated.Release();
            m_VolumetricLightingBufferCurrentFrame.Release();
        }

        int s = m_VolumetricBufferTileSize;
        Debug.Assert((s & (s - 1)) == 0, "m_VolumetricBufferTileSize must be a power of 2.");

        int w = (width  + s - 1) / s;
        int h = (height + s - 1) / s;
        int d = 64;

        m_VolumetricLightingBufferCurrentFrame = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        m_VolumetricLightingBufferCurrentFrame.filterMode        = FilterMode.Bilinear;    // Custom trilinear
        m_VolumetricLightingBufferCurrentFrame.dimension         = TextureDimension.Tex3D; // Prefer 3D Thick tiling layout
        m_VolumetricLightingBufferCurrentFrame.volumeDepth       = d;
        m_VolumetricLightingBufferCurrentFrame.enableRandomWrite = true;
        m_VolumetricLightingBufferCurrentFrame.Create();
        m_VolumetricLightingBufferCurrentFrameRT = new RenderTargetIdentifier(m_VolumetricLightingBufferCurrentFrame);

        m_VolumetricLightingBufferAccumulated = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        m_VolumetricLightingBufferAccumulated.filterMode        = FilterMode.Bilinear;    // Custom trilinear
        m_VolumetricLightingBufferAccumulated.dimension         = TextureDimension.Tex3D; // Prefer 3D Thick tiling layout
        m_VolumetricLightingBufferAccumulated.volumeDepth       = d;
        m_VolumetricLightingBufferAccumulated.enableRandomWrite = true;
        m_VolumetricLightingBufferAccumulated.Create();
       // m_VolumetricLightingBufferAccumulatedRT = new RenderTargetIdentifier(m_VolumetricLightingBufferAccumulated);
    }

    void ClearVolumetricLightingBuffers(CommandBuffer cmd, bool isFirstFrame)
    {
        using (new ProfilingSample("Clear volumetric lighting buffers", cmd))
        {
            CoreUtils.SetRenderTarget(cmd, m_VolumetricLightingBufferCurrentFrameRT, ClearFlag.Color, Color.black);

            if (isFirstFrame)
            {
                CoreUtils.SetRenderTarget(cmd, m_VolumetricLightingBufferAccumulated, ClearFlag.Color, Color.black);
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

    void VolumetricLightingPass(HDCamera hdCamera, CommandBuffer cmd)
    {
        if (!SetGlobalVolumeProperties(m_VolumetricLightingEnabled, cmd, m_VolumetricLightingCS)) { return; }

        using (new ProfilingSample("VolumetricLighting", cmd))
        {
            bool enableClustered = m_Asset.tileSettings.enableClustered && m_Asset.tileSettings.enableTileAndCluster;

            int volumetricLightingKernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClustered"
                                                                                             : "VolumetricLightingAllLights");
            hdCamera.SetupComputeShader(m_VolumetricLightingCS, cmd);

            cmd.SetComputeTextureParam(m_VolumetricLightingCS, volumetricLightingKernel, HDShaderIDs._CameraColorTexture, m_CameraColorBufferRT);
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, volumetricLightingKernel, HDShaderIDs._DepthTexture,       GetDepthTexture());
            cmd.SetComputeVectorParam( m_VolumetricLightingCS, HDShaderIDs._Time,        Shader.GetGlobalVector(HDShaderIDs._Time));

            // Pass clustered light data (if present) into the compute shader.
            m_LightLoop.PushGlobalParams(hdCamera.camera, cmd, m_VolumetricLightingCS, volumetricLightingKernel, true);
            cmd.SetComputeIntParam(m_VolumetricLightingCS, HDShaderIDs._UseTileLightList, 0);

            cmd.DispatchCompute(m_VolumetricLightingCS, volumetricLightingKernel, ((int)hdCamera.screenSize.x + 15) / 16, ((int)hdCamera.screenSize.y + 15) / 16, 1);
        }
    }
} // class HDRenderPipeline

} // namespace UnityEngine.Experimental.Rendering.HDPipeline
