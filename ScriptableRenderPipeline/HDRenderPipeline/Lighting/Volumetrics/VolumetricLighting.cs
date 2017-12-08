using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{

[GenerateHLSL]
public struct VolumeProperties
{
    public Vector3 scattering; // [0, 1], prefer sRGB
    public float   extinction; // [0, 1], prefer sRGB

    public static VolumeProperties GetNeutralVolumeProperties()
    {
        VolumeProperties properties = new VolumeProperties();

        properties.scattering = Vector3.zero;
        properties.extinction = 0;

        return properties;
    }
} // struct VolumeProperties

[Serializable]
public class VolumeParameters
{
    public Bounds bounds;       // Position and dimensions in meters
    public Color  albedo;       // Single scattering albedo [0, 1]
    public float  meanFreePath; // In meters [1, inf]. Should be chromatic - this is an optimization!

    public VolumeParameters()
    {
        bounds       = new Bounds(Vector3.zero, Vector3.positiveInfinity);
        albedo       = new Color(0.5f, 0.5f, 0.5f);
        meanFreePath = 10.0f;
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
    }

    public VolumeProperties GetProperties()
    {
        VolumeProperties properties = new VolumeProperties();

        properties.scattering = GetScatteringCoefficient();
        properties.extinction = GetExtinctionCoefficient();

        return properties;
    }
} // class VolumeParameters

public partial class HDRenderPipeline : RenderPipeline
{
    public enum VolumetricLightingPreset
    {
        Off,
        Normal,
        Ultra,
        Count
    };

    VolumetricLightingPreset m_VolumetricLightingPreset = VolumetricLightingPreset.Normal;
    ComputeShader            m_VolumetricLightingCS { get { return m_Asset.renderPipelineResources.volumetricLightingCS; } }

    float                    m_VBufferNearPlane  =  0.5f; // Distance in meters
    float                    m_VBufferFarPlane   = 64.0f; // Distance in meters

    RenderTexture[]          m_VBufferLighting   = null;  // Used for even / odd frames
    RenderTargetIdentifier[] m_VBufferLightingRT = null;

    public static int ComputeVBufferTileSize(VolumetricLightingPreset preset)
    {
        switch (preset)
        {
            case VolumetricLightingPreset.Normal:
                return 8;
            case VolumetricLightingPreset.Ultra:
                return 4;
            case VolumetricLightingPreset.Off:
                return 0;
            default:
                Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                return 0;
        }
    }

    public static int ComputeVBufferSliceCount(VolumetricLightingPreset preset)
    {
        switch (preset)
        {
            case VolumetricLightingPreset.Normal:
                return 128;
            case VolumetricLightingPreset.Ultra:
                return 256;
            case VolumetricLightingPreset.Off:
                return 0;
            default:
                Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                return 0;
        }
    }

    // Since a single voxel corresponds to a tile (e.g. 8x8) of pixels,
    // the VBuffer can potentially extend past the boundaries of the viewport.
    // The function returns the fraction of the {width, height} of the VBuffer visible on screen.
    Vector2 ComputeVBufferResolutionAndScale(float screenWidth, float screenHeight,
                                             ref int w, ref int h, ref int d)
    {
        int t = ComputeVBufferTileSize(m_VolumetricLightingPreset);

        // Ceil(ScreenSize / TileSize).
        w = ((int)screenWidth  + t - 1) / t;
        h = ((int)screenHeight + t - 1) / t;
        d = ComputeVBufferSliceCount(m_VolumetricLightingPreset);

        return new Vector2(screenWidth / (w * t), screenHeight / (h * t));
    }

    void CreateVBuffer(int screenWidth, int screenHeight)
    {
        DestroyVBuffer();

        m_VBufferLighting   = new RenderTexture[2];
        m_VBufferLightingRT = new RenderTargetIdentifier[2];

        int w = 0, h = 0, d = 0;
        ComputeVBufferResolutionAndScale(screenWidth, screenHeight, ref w, ref h, ref d);

        for (int i = 0; i < 2; i++)
        {
            m_VBufferLighting[i] = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear); // UAV with ARGBHalf appears to be broken...
            m_VBufferLighting[i].filterMode        = FilterMode.Bilinear;    // Custom trilinear
            m_VBufferLighting[i].dimension         = TextureDimension.Tex3D; // TODO: request the thick 3D tiling layout
            m_VBufferLighting[i].volumeDepth       = d;
            m_VBufferLighting[i].enableRandomWrite = true;
            m_VBufferLighting[i].Create();

            m_VBufferLightingRT[i] = new RenderTargetIdentifier(m_VBufferLighting[i]);
        }
    }

    void DestroyVBuffer()
    {
        if (m_VBufferLighting != null)
        {
            if (m_VBufferLighting[0] != null) m_VBufferLighting[0].Release();
            if (m_VBufferLighting[1] != null) m_VBufferLighting[1].Release();

            m_VBufferLighting   = null;
            m_VBufferLightingRT = null;
        }
    }

    // Uses a logarithmic depth encoding.
    // Near plane: depth = 0; far plane: depth = 1.
    // x = n, y = log2(f/n), z = 1/n, w = 1/log2(f/n).
    public static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane)
    {
        Vector4 depthParams = new Vector4();

        float n = nearPlane;
        float f = farPlane;

        depthParams.x = n;
        depthParams.y = Mathf.Log(f / n, 2);
        depthParams.z = 1.0f / depthParams.x;
        depthParams.w = 1.0f / depthParams.y;

        return depthParams;
    }
    
    // Returns NULL if a global fog component does not exist, or is not enabled.
    public static HomogeneousFog GetGlobalFogComponent()
    {
        HomogeneousFog globalFogComponent = null;

        HomogeneousFog[] fogComponents = Object.FindObjectsOfType(typeof(HomogeneousFog)) as HomogeneousFog[];

        foreach (HomogeneousFog fogComponent in fogComponents)
        {
            if (fogComponent.enabled && fogComponent.volumeParameters.IsVolumeUnbounded())
            {
                globalFogComponent = fogComponent;
                break;
            }
        }

        return globalFogComponent;
    }

    public void SetVolumetricLightingData(HDCamera camera, CommandBuffer cmd)
    {
        HomogeneousFog globalFogComponent = GetGlobalFogComponent();

        // TODO: may want to cache these results somewhere.
        VolumeProperties globalFogProperties = (globalFogComponent != null) ? globalFogComponent.volumeParameters.GetProperties()
                                                                            : VolumeProperties.GetNeutralVolumeProperties();

        cmd.SetGlobalVector(HDShaderIDs._GlobalFog_Scattering, globalFogProperties.scattering);
        cmd.SetGlobalFloat( HDShaderIDs._GlobalFog_Extinction, globalFogProperties.extinction);

        int w = 0, h = 0, d = 0;
        Vector2 scale = ComputeVBufferResolutionAndScale(camera.screenSize.x, camera.screenSize.y, ref w, ref h, ref d);

        Vector4 resAndScale = new Vector4(w, h, scale.x, scale.y);
        Vector4 depthParams = ComputeLogarithmicDepthEncodingParams(m_VBufferNearPlane, m_VBufferFarPlane);

        cmd.SetGlobalVector( HDShaderIDs._VBufferResolutionAndScale,  resAndScale);
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthEncodingParams, depthParams);
        cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,            m_VBufferLightingRT[0]);
    }

    void VolumetricLightingPass(HDCamera camera, CommandBuffer cmd)
    {
        if (m_VolumetricLightingPreset == VolumetricLightingPreset.Off) return;

        using (new ProfilingSample(cmd, "Volumetric Lighting"))
        {
            if (GetGlobalFogComponent() == null)
            {
                // Clear the render target instead of running the shader.
                CoreUtils.SetRenderTarget(cmd, m_VBufferLightingRT[0], ClearFlag.Color, CoreUtils.clearColorAllBlack);
                return;
            }

            camera.SetupComputeShader(m_VolumetricLightingCS, cmd);

            bool enableClustered = m_Asset.lightLoopSettings.enableTileAndCluster;
            int  kernel          = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClustered"
                                                                                     : "VolumetricLightingAllLights");
            int w = 0, h = 0, d = 0;
            Vector2 scale = ComputeVBufferResolutionAndScale(camera.screenSize.x, camera.screenSize.y, ref w, ref h, ref d);
            float   vFoV  = camera.camera.fieldOfView * Mathf.Deg2Rad;

            // Compose the matrix which allows us to compute the world space view direction.
            // Compute it using the scaled resolution to account for the visible area of the VBuffer.
            Vector4   scaledRes = new Vector4(w * scale.x, h * scale.y, 1.0f / (w * scale.x), 1.0f / (h * scale.y));
            Matrix4x4 transform = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, scaledRes, camera.viewMatrix, false);

            // TODO: set 'm_VolumetricLightingPreset'.
            cmd.SetComputeMatrixParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferCoordToViewDirWS, transform);
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLighting,         m_VBufferLightingRT[0]);
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingPrev,     m_VBufferLightingRT[1]);

            // The shader defines GROUP_SIZE_1D = 16.
            cmd.DispatchCompute(m_VolumetricLightingCS, kernel, (w + 15) / 16, (h + 15) / 16, 1);
        }
    }
} // class HDRenderPipeline
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
