using System;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
[GenerateHLSL]
public struct DensityVolumeProperties
{
    public Vector3 scattering; // [0, 1], prefer sRGB
    public float   extinction; // [0, 1], prefer sRGB

    public static DensityVolumeProperties GetNeutralProperties()
    {
        DensityVolumeProperties properties = new DensityVolumeProperties();

        properties.scattering = Vector3.zero;
        properties.extinction = 0;

        return properties;
    }
} // struct VolumeProperties

[Serializable]
public class DensityVolumeParameters
{
    public bool  isLocal;      // Enables voxelization
    public Color albedo;       // Single scattering albedo [0, 1]
    public float meanFreePath; // In meters [1, inf]. Should be chromatic - this is an optimization!
    public float asymmetry;    // Only used if (isLocal == false)

    public DensityVolumeParameters()
    {
        isLocal      = true;
        albedo       = new Color(0.5f, 0.5f, 0.5f);
        meanFreePath = 10.0f;
        asymmetry    = 0.0f;
    }

    public bool IsLocalVolume()
    {
        return isLocal;
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
        albedo.r = Mathf.Clamp01(albedo.r);
        albedo.g = Mathf.Clamp01(albedo.g);
        albedo.b = Mathf.Clamp01(albedo.b);

        meanFreePath = Mathf.Max(meanFreePath, 1.0f);

        asymmetry = Mathf.Clamp(asymmetry, -1.0f, 1.0f);
    }

    public DensityVolumeProperties GetProperties()
    {
        DensityVolumeProperties properties = new DensityVolumeProperties();

        properties.scattering = GetScatteringCoefficient();
        properties.extinction = GetExtinctionCoefficient();

        return properties;
    }
} // class VolumeParameters

public struct DensityVolumeList
{
    public List<OrientedBBox>            bounds;
    public List<DensityVolumeProperties> properties;
}

public class VolumetricLightingSystem
{
    public enum VolumetricLightingPreset
    {
        Off,
        Normal,
        Ultra,
        Count
    }
    class VBuffer
    {
        const int k_IndexDensity  = 0;
        const int k_IndexIntegral = 1;
        const int k_IndexHistory  = 2; // Depends on frame ID
        const int k_IndexFeedback = 3; // Depends on frame ID

        long                     m_ViewID       =   -1; // -1 is invalid; positive for Game Views, 0 otherwise
        RenderTexture[]          m_Textures     = null;
        RenderTargetIdentifier[] m_Identifiers  = null;

        public RenderTargetIdentifier GetDensityBuffer()
        {
            Debug.Assert(m_ViewID >= 0);
            return m_Identifiers[k_IndexDensity];
        }

        public RenderTargetIdentifier GetLightingIntegralBuffer() // Of the current frame
        {
            Debug.Assert(m_ViewID >= 0);
            return m_Identifiers[k_IndexIntegral];
        }

        public RenderTargetIdentifier GetLightingHistoryBuffer() // From the previous frame
        {
            Debug.Assert(m_ViewID > 0); // Game View only
            return m_Identifiers[k_IndexHistory + (Time.renderedFrameCount & 1)];
        }

        public RenderTargetIdentifier GetLightingFeedbackBuffer() // For the next frame
        {
            Debug.Assert(m_ViewID > 0); // Game View only
            return m_Identifiers[k_IndexFeedback - (Time.renderedFrameCount & 1)];
        }

        public void Create(long viewID, int w, int h, int d)
        {
            Debug.Assert(viewID >= 0);
            Debug.Assert(w > 0 && h > 0 && d > 0);

            // Clean up first.
            Destroy();

            // Only Game Views need history and feedback buffers.
            bool isGameView = viewID > 0;
            int  n          = isGameView ? 4 : 2;

            m_ViewID      = viewID;
            m_Textures    = new RenderTexture[n];
            m_Identifiers = new RenderTargetIdentifier[n];

            for (int i = 0; i < n; i++)
            {
                m_Textures[i] = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_Textures[i].hideFlags         = HideFlags.HideAndDontSave;
                m_Textures[i].filterMode        = FilterMode.Trilinear;   // Custom
                m_Textures[i].dimension         = TextureDimension.Tex3D; // TODO: request the thick 3D tiling layout
                m_Textures[i].volumeDepth       = d;
                m_Textures[i].enableRandomWrite = true;
                m_Textures[i].name              = CoreUtils.GetRenderTargetAutoName(w, h, RenderTextureFormat.ARGBHalf, String.Format("VBuffer{0}", i));
                m_Textures[i].Create();

                // TODO: clear the texture. Clearing 3D textures does not appear to work right now.

                m_Identifiers[i] = new RenderTargetIdentifier(m_Textures[i]);
            }
        }

        public void Destroy()
        {
            if (m_Textures != null)
            {
                for (int i = 0, n = m_Textures.Length; i < n; i++)
                {
                    if (m_Textures[i] != null)
                    {
                        m_Textures[i].Release();
                    }
                }
            }

            m_ViewID      =   -1;
            m_Textures    = null;
            m_Identifiers = null;
        }
        public void GetResolution(ref int w, ref int h, ref int d)
        {
            Debug.Assert(m_Textures    != null);
            Debug.Assert(m_Textures[0] != null);
            Debug.Assert(m_Identifiers != null);

            w = m_Textures[0].width;
            h = m_Textures[0].height;
            d = m_Textures[0].volumeDepth;
        }

        public long GetViewID()
        {
            return m_ViewID;
        }

        public bool IsValid()
        {
            return m_ViewID >= 0 && m_Textures != null && m_Textures[0] != null;
        }

    } // class VBuffer

    public VolumetricLightingPreset preset { get { return (VolumetricLightingPreset)Math.Min(ShaderConfig.s_VolumetricLightingPreset, (int)VolumetricLightingPreset.Count); } }

    ComputeShader m_VolumeVoxelizationCS = null;
    ComputeShader m_VolumetricLightingCS = null;

    List<VBuffer>                 m_VBuffers                = null;
    List<OrientedBBox>            m_VisibleVolumeBounds     = null;
    List<DensityVolumeProperties> m_VisibleVolumeProperties = null;
    public const int              k_MaxVisibleVolumeCount   = 512;

    // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
    static ComputeBuffer s_VisibleVolumeBoundsBuffer     = null;
    static ComputeBuffer s_VisibleVolumePropertiesBuffer = null;

    float       m_VBufferNearPlane = 0.5f;  // Distance in meters; dynamic modifications not handled by reprojection
    float       m_VBufferFarPlane  = 64.0f; // Distance in meters; dynamic modifications not handled by reprojection
    const float k_LogScale         = 0.5f;  // Tweak constant, controls the logarithmic depth distribution

    public void Build(HDRenderPipelineAsset asset)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        m_VolumeVoxelizationCS          = asset.renderPipelineResources.volumeVoxelizationCS;
        m_VolumetricLightingCS          = asset.renderPipelineResources.volumetricLightingCS;
        m_VBuffers                      = new List<VBuffer>();
        m_VisibleVolumeBounds           = new List<OrientedBBox>();
        m_VisibleVolumeProperties       = new List<DensityVolumeProperties>();
        s_VisibleVolumeBoundsBuffer     = new ComputeBuffer(k_MaxVisibleVolumeCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(OrientedBBox)));
        s_VisibleVolumePropertiesBuffer = new ComputeBuffer(k_MaxVisibleVolumeCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DensityVolumeProperties)));
    }

    public void Cleanup()
    {
        if (preset == VolumetricLightingPreset.Off) return;

        m_VolumeVoxelizationCS = null;
        m_VolumetricLightingCS = null;

        for (int i = 0, n = m_VBuffers.Count; i < n; i++)
        {
            m_VBuffers[i].Destroy();
        }

        m_VBuffers                = null;
        m_VisibleVolumeBounds     = null;
        m_VisibleVolumeProperties = null;

        CoreUtils.SafeRelease(s_VisibleVolumeBoundsBuffer);
        CoreUtils.SafeRelease(s_VisibleVolumePropertiesBuffer);
    }

    public void ResizeVBuffer(HDCamera camera, int screenWidth, int screenHeight)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        long viewID = camera.GetViewID();

        Debug.Assert(viewID >= 0);

        int w = 0, h = 0, d = 0;
        ComputeVBufferResolutionAndScale(preset, screenWidth, screenHeight, ref w, ref h, ref d);

        VBuffer vBuffer = FindVBuffer(viewID);

        if (vBuffer != null)
        {
            int width = 0, height = 0, depth = 0;
            vBuffer.GetResolution(ref width, ref height, ref depth);

            // Found, check resolution.
            if (w == width && h == height && d == depth)
            {
                // Everything matches, nothing to do here.
                return;
            }
        }
        else
        {
            // Not found - grow the array.
            vBuffer = new VBuffer();
            m_VBuffers.Add(vBuffer);
        }

        vBuffer.Create(viewID, w, h, d);
    }

    VBuffer FindVBuffer(long viewID)
    {
        Debug.Assert(viewID >= 0);

        VBuffer vBuffer = null;

        if (m_VBuffers != null)
        {
            int n = m_VBuffers.Count;

            for (int i = 0; i < n; i++)
            {
                // Check whether domain reload killed it...
                if (viewID == m_VBuffers[i].GetViewID() && m_VBuffers[i].IsValid())
                {
                    vBuffer = m_VBuffers[i];
                }
            }
        }

        return vBuffer;
    }

    static int ComputeVBufferTileSize(VolumetricLightingPreset preset)
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

    static int ComputeVBufferSliceCount(VolumetricLightingPreset preset)
    {
        switch (preset)
        {
            case VolumetricLightingPreset.Normal:
                return 64;
            case VolumetricLightingPreset.Ultra:
                return 128;
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
    // Note: for performance reasons, scale is unused (implicitly 1). The error is typically under 1%.
    static Vector2 ComputeVBufferResolutionAndScale(VolumetricLightingPreset preset,
                                                    int screenWidth, int screenHeight,
                                                    ref int w, ref int h, ref int d)
    {
        int t = ComputeVBufferTileSize(preset);

        // Ceil(ScreenSize / TileSize).
        w = (screenWidth  + t - 1) / t;
        h = (screenHeight + t - 1) / t;
        d = ComputeVBufferSliceCount(preset);

        return new Vector2((float)screenWidth / (float)(w * t), (float)screenHeight / (float)(h * t));
    }

    // See EncodeLogarithmicDepthGeneralized().
    static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane, float c)
    {
        Vector4 depthParams = new Vector4();

        float n = nearPlane;
        float f = farPlane;

        depthParams.x = Mathf.Log(c, 2) * (1.0f / Mathf.Log(c * (f - n) + 1, 2));
        depthParams.y = 1.0f / Mathf.Log(c * (f - n) + 1, 2);
        depthParams.z = n - 1.0f / c; // Same
        depthParams.w = 0.0f;

        return depthParams;
    }

    // See DecodeLogarithmicDepthGeneralized().
    static Vector4 ComputeLogarithmicDepthDecodingParams(float nearPlane, float farPlane, float c)
    {
        Vector4 depthParams = new Vector4();

        float n = nearPlane;
        float f = farPlane;

        depthParams.x = 1.0f / c;
        depthParams.y = c * (f - n) + 1;
        depthParams.z = n - 1.0f / c; // Same
        depthParams.w = 0.0f;

        return depthParams;
    }

    void SetPreconvolvedAmbientLightProbe(CommandBuffer cmd, float asymmetry)
    {
        SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(RenderSettings.ambientProbe);
        ZonalHarmonicsL2     phaseZH = ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(asymmetry);
        SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, phaseZH));

        cmd.SetGlobalVectorArray(HDShaderIDs._AmbientProbeCoeffs, SphericalHarmonicMath.PackCoefficients(finalSH));
    }

    float CornetteShanksPhasePartConstant(float asymmetry)
    {
        float g = asymmetry;

        return (1.0f / (4.0f * Mathf.PI)) * 1.5f * (1.0f - g * g) / (2.0f + g * g);
    }

    public void PushGlobalParams(HDCamera camera, CommandBuffer cmd)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        HomogeneousDensityVolume globalVolume = HomogeneousDensityVolume.GetGlobalHomogeneousDensityVolume();

        // TODO: may want to cache these results somewhere.
        DensityVolumeProperties globalVolumeProperties = (globalVolume != null) ? globalVolume.parameters.GetProperties()
                                                                                : DensityVolumeProperties.GetNeutralProperties();

        float asymmetry = globalVolume != null ? globalVolume.parameters.asymmetry : 0;
        cmd.SetGlobalVector(HDShaderIDs._GlobalScattering, globalVolumeProperties.scattering);
        cmd.SetGlobalFloat( HDShaderIDs._GlobalExtinction, globalVolumeProperties.extinction);
        cmd.SetGlobalFloat( HDShaderIDs._GlobalAsymmetry,  asymmetry);

        VBuffer vBuffer = FindVBuffer(camera.GetViewID());
        Debug.Assert(vBuffer != null);

        int w = 0, h = 0, d = 0;
        vBuffer.GetResolution(ref w, ref h, ref d);

        SetPreconvolvedAmbientLightProbe(cmd, asymmetry);
        cmd.SetGlobalVector( HDShaderIDs._VBufferResolution,          new Vector4(w, h, 1.0f / w, 1.0f / h));
        cmd.SetGlobalVector( HDShaderIDs._VBufferSliceCount,          new Vector4(d, 1.0f / d));
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthEncodingParams, ComputeLogarithmicDepthEncodingParams(m_VBufferNearPlane, m_VBufferFarPlane, k_LogScale));
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthDecodingParams, ComputeLogarithmicDepthDecodingParams(m_VBufferNearPlane, m_VBufferFarPlane, k_LogScale));
        cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,            vBuffer.GetLightingIntegralBuffer());
    }

    public DensityVolumeList PrepareVisibleDensityVolumeList(HDCamera camera, CommandBuffer cmd)
    {
        DensityVolumeList densityVolumes = new DensityVolumeList();

        if (preset == VolumetricLightingPreset.Off) return densityVolumes;

        using (new ProfilingSample(cmd, "Prepare Visible Density Volume List"))
        {
            Vector3 camPosition = camera.camera.transform.position;
            Vector3 camOffset   = Vector3.zero; // World-origin-relative

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                camOffset = camPosition; // Camera-relative
            }

            m_VisibleVolumeBounds.Clear();
            m_VisibleVolumeProperties.Clear();

            // Collect all visible finite volume data, and upload it to the GPU.
            HomogeneousDensityVolume[] volumes = Object.FindObjectsOfType(typeof(HomogeneousDensityVolume)) as HomogeneousDensityVolume[];

            for (int i = 0; i < Math.Min(volumes.Length, k_MaxVisibleVolumeCount); i++)
            {
                HomogeneousDensityVolume volume = volumes[i];

                // Only test active finite volumes.
                if (volume.enabled && volume.parameters.IsLocalVolume())
                {
                    // TODO: cache these?
                    var obb = OrientedBBox.Create(volume.transform);

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    if (GeometryUtils.Overlap(obb, camera.frustum, 6, 8))
                    {
                        // TODO: cache these?
                        var properties = volume.parameters.GetProperties();

                        m_VisibleVolumeBounds.Add(obb);
                        m_VisibleVolumeProperties.Add(properties);
                    }
                }
            }

            s_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
            s_VisibleVolumePropertiesBuffer.SetData(m_VisibleVolumeProperties);

            // Fill the struct with pointers in order to share the data with the light loop.
            densityVolumes.bounds     = m_VisibleVolumeBounds;
            densityVolumes.properties = m_VisibleVolumeProperties;

            return densityVolumes;
        }
    }

    public void VolumeVoxelizationPass(DensityVolumeList densityVolumes, HDCamera camera, CommandBuffer cmd, FrameSettings settings)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        using (new ProfilingSample(cmd, "Volume Voxelization"))
        {
            int numVisibleVolumes = m_VisibleVolumeBounds.Count;

            if (numVisibleVolumes == 0)
            {
                // Clear the render target instead of running the shader.
                // CoreUtils.SetRenderTarget(cmd, vBuffer.GetDensityBuffer(), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                // return;

                // Clearing 3D textures does not seem to work!
                // Use the workaround by running the full shader with 0 density.
            }

            VBuffer vBuffer = FindVBuffer(camera.GetViewID());
            Debug.Assert(vBuffer != null);

            int w = 0, h = 0, d = 0;
            vBuffer.GetResolution(ref w, ref h, ref d);

            bool enableClustered = settings.lightLoopSettings.enableTileAndCluster;

            int kernel = m_VolumeVoxelizationCS.FindKernel(enableClustered ? "VolumeVoxelizationClustered"
                                                                           : "VolumeVoxelizationBruteforce");

            float     vFoV       = camera.camera.fieldOfView * Mathf.Deg2Rad;
            Vector4   resolution = new Vector4(w, h, 1.0f / w, 1.0f / h);
            Matrix4x4 transform  = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, resolution, camera.viewMatrix, false);
                                   
            camera.SetupComputeShader( m_VolumeVoxelizationCS, cmd);
            cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VBufferDensity,   vBuffer.GetDensityBuffer());
            cmd.SetComputeBufferParam( m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeBounds,     s_VisibleVolumeBoundsBuffer);
            cmd.SetComputeBufferParam( m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeProperties, s_VisibleVolumePropertiesBuffer);

            // TODO: set the constant buffer data only once.
            cmd.SetComputeMatrixParam( m_VolumeVoxelizationCS, HDShaderIDs._VBufferCoordToViewDirWS,  transform);
            cmd.SetComputeIntParam(    m_VolumeVoxelizationCS, HDShaderIDs._NumVisibleDensityVolumes, numVisibleVolumes);

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(m_VolumeVoxelizationCS, kernel, (w + 7) / 8, (h + 7) / 8, 1);
        }
    }

    // Ref: https://en.wikipedia.org/wiki/Close-packing_of_equal_spheres
    // The returned {x, y} coordinates (and all spheres) are all within the (-0.5, 0.5)^2 range.
    // The pattern has been rotated by 15 degrees to maximize the resolution along X and Y:
    // https://www.desmos.com/calculator/kcpfvltz7c
    static Vector2[] GetHexagonalClosePackedSpheres7()
    {
        Vector2[] coords = new Vector2[7];

        float r = 0.17054068870105443882f;
        float d = 2 * r;
        float s = r * Mathf.Sqrt(3);

        // Try to keep the weighted average as close to the center (0.5) as possible.
        //  (7)(5)    ( )( )    ( )( )    ( )( )    ( )( )    ( )(o)    ( )(x)    (o)(x)    (x)(x)
        // (2)(1)(3) ( )(o)( ) (o)(x)( ) (x)(x)(o) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x)
        //  (4)(6)    ( )( )    ( )( )    ( )( )    (o)( )    (x)( )    (x)(o)    (x)(x)    (x)(x)
        coords[0] = new Vector2( 0,  0);
        coords[1] = new Vector2(-d,  0);
        coords[2] = new Vector2( d,  0);
        coords[3] = new Vector2(-r, -s);
        coords[4] = new Vector2( r,  s);
        coords[5] = new Vector2( r, -s);
        coords[6] = new Vector2(-r,  s);

        // Rotate the sampling pattern by 15 degrees.
        const float cos15 = 0.96592582628906828675f;
        const float sin15 = 0.25881904510252076235f;

        for (int i = 0; i < 7; i++)
        {
            Vector2 coord = coords[i];

            coords[i].x = coord.x * cos15 - coord.y * sin15;
            coords[i].y = coord.x * sin15 + coord.y * cos15;
        }

        return coords;
    }

    public void VolumetricLightingPass(HDCamera camera, CommandBuffer cmd, FrameSettings settings)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        using (new ProfilingSample(cmd, "Volumetric Lighting"))
        {
            VBuffer vBuffer = FindVBuffer(camera.GetViewID());
            Debug.Assert(vBuffer != null);

            HomogeneousDensityVolume globalVolume = HomogeneousDensityVolume.GetGlobalHomogeneousDensityVolume();
            float asymmetry = globalVolume != null ? globalVolume.parameters.asymmetry : 0;

            if (globalVolume == null)
            {
                // Clear the render target instead of running the shader.
                // CoreUtils.SetRenderTarget(cmd, vBuffer.GetLightingIntegralBuffer(), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                // CoreUtils.SetRenderTarget(cmd, vBuffer.GetLightingFeedbackBuffer(), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                // return;

                // Clearing 3D textures does not seem to work!
                // Use the workaround by running the full shader with 0 density.
            }

            // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
            bool enableClustered    = settings.lightLoopSettings.enableTileAndCluster;
            bool enableReprojection = Application.isPlaying && camera.camera.cameraType == CameraType.Game;

            int kernel;

            if (enableReprojection)
            {
                kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClusteredReproj"
                                                                           : "VolumetricLightingBruteforceReproj");
            }
            else
            {
                kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClustered"
                                                                           : "VolumetricLightingBruteforce");
            }

            int w = 0, h = 0, d = 0;
            vBuffer.GetResolution(ref w, ref h, ref d);

            // Compose the matrix which allows us to compute the world space view direction.
            float     vFoV       = camera.camera.fieldOfView * Mathf.Deg2Rad;
            Vector4   resolution = new Vector4(w, h, 1.0f / w, 1.0f / h);
            Matrix4x4 transform  = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, resolution, camera.viewMatrix, false);

            camera.SetupComputeShader(m_VolumetricLightingCS, cmd);

            Vector2[] xySeq = GetHexagonalClosePackedSpheres7();

            // This is a sequence of 7 equidistant numbers from 1/14 to 13/14.
            // Each of them is the centroid of the interval of length 2/14.
            // They've been rearranged in a sequence of pairs {small, large}, s.t. (small + large) = 1.
            // That way, the running average position is close to 0.5.
            // | 6 | 2 | 4 | 1 | 5 | 3 | 7 |
            // |   |   |   | o |   |   |   |
            // |   | o |   | x |   |   |   |
            // |   | x |   | x |   | o |   |
            // |   | x | o | x |   | x |   |
            // |   | x | x | x | o | x |   |
            // | o | x | x | x | x | x |   |
            // | x | x | x | x | x | x | o |
            // | x | x | x | x | x | x | x |
            float[] zSeq = {7.0f/14.0f, 3.0f/14.0f, 11.0f/14.0f, 5.0f/14.0f, 9.0f/14.0f, 1.0f/14.0f, 13.0f/14.0f};

            int rfc = Time.renderedFrameCount;
            int sampleIndex = rfc % 7;
            Vector4 offset = new Vector4(xySeq[sampleIndex].x, xySeq[sampleIndex].y, zSeq[sampleIndex], rfc);

            // TODO: set 'm_VolumetricLightingPreset'.
            // TODO: set the constant buffer data only once.
            cmd.SetComputeMatrixParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferCoordToViewDirWS, transform);
            cmd.SetComputeVectorParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferSampleOffset,     offset);
            cmd.SetComputeFloatParam(  m_VolumetricLightingCS,         HDShaderIDs._CornetteShanksConstant,  CornetteShanksPhasePartConstant(asymmetry));
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferDensity,          vBuffer.GetDensityBuffer());          // Read
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingIntegral, vBuffer.GetLightingIntegralBuffer()); // Write
            if (enableReprojection)
            {
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingFeedback, vBuffer.GetLightingFeedbackBuffer()); // Write
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingHistory,  vBuffer.GetLightingHistoryBuffer());  // Read
            }

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(m_VolumetricLightingCS, kernel, (w + 7) / 8, (h + 7) / 8, 1);
        }
    }
} // class VolumetricLightingModule
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
