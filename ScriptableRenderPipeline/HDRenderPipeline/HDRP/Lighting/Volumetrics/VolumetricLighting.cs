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

    VolumetricLightingPreset m_VolumetricLightingPreset
    { get { return (VolumetricLightingPreset)Math.Min(ShaderConfig.s_VolumetricLightingPreset, (int)VolumetricLightingPreset.Count); } }

    ComputeShader            m_VolumetricLightingCS { get { return m_Asset.renderPipelineResources.volumetricLightingCS; } }

    float                    m_VBufferNearPlane  = 0.5f;  // Distance in meters; dynamic modifications not handled by reprojection
    float                    m_VBufferFarPlane   = 64.0f; // Distance in meters; dynamic modifications not handled by reprojection
    const int                k_VBufferCount      = 3;     // 0 and 1 - history (prev) and feedback (next), 2 - integral (curr)

    RenderTexture[]          m_VBufferLighting   = null;
    RenderTargetIdentifier[] m_VBufferLightingRT = null;

    int                      m_ViewCount         = 0;
    int[]                    m_ViewIdArray       = new int[8]; // TODO: account for the CameraType

    int ViewOffsetFromViewId(int viewId)
    {
        int viewOffset = -1;

        Debug.Assert(m_ViewCount == 0 || m_ViewIdArray != null);

        for (int i = 0; i < m_ViewCount; i++)
        {
            if (m_ViewIdArray[i] == viewId)
            {
                viewOffset = i;
            }
        }

        return viewOffset;
    }

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

    void ResizeVBuffer(int viewId, int screenWidth, int screenHeight)
    {
        int viewOffset = ViewOffsetFromViewId(viewId);

        if (viewOffset >= 0)
        {
            // Found, check resolution.
            int w = 0, h = 0, d = 0;
            ComputeVBufferResolutionAndScale(screenWidth, screenHeight, ref w, ref h, ref d);

            Debug.Assert(m_VBufferLighting != null);
            Debug.Assert(m_VBufferLighting.Length >= (viewOffset + 1) * k_VBufferCount);
            Debug.Assert(m_VBufferLighting[viewOffset * k_VBufferCount] != null);

            if (w == m_VBufferLighting[viewOffset * k_VBufferCount].width  &&
                h == m_VBufferLighting[viewOffset * k_VBufferCount].height &&
                d == m_VBufferLighting[viewOffset * k_VBufferCount].volumeDepth)
            {
                // Everything matches, nothing to do here.
                return;
            }
        }

        // Otherwise, we have to recreate the VBuffer.
        CreateVBuffer(viewId, screenWidth, screenHeight);
    }

    void CreateVBuffer(int viewId, int screenWidth, int screenHeight)
    {
        // Clean up first.
        DestroyVBuffer(viewId);

        int viewOffset = ViewOffsetFromViewId(viewId);

        if (viewOffset < 0)
        {
            // Not found. Push back.
            viewOffset = m_ViewCount++;
            Debug.Assert(viewOffset < 8);
            m_ViewIdArray[viewOffset] = viewId;

            if (m_VBufferLighting == null)
            {
                // Lazy initialize.
                m_VBufferLighting   = new RenderTexture[k_VBufferCount];
                m_VBufferLightingRT = new RenderTargetIdentifier[k_VBufferCount];
            }
            else if (m_VBufferLighting.Length < m_ViewCount * k_VBufferCount)
            {
                // Grow by reallocation and copy.
                RenderTexture[]          newArray   = new RenderTexture[m_ViewCount * k_VBufferCount];
                RenderTargetIdentifier[] newArrayRT = new RenderTargetIdentifier[m_ViewCount * k_VBufferCount];
                
                for (int i = 0, n = m_VBufferLighting.Length; i < n; i++)
                {
                    newArray[i]   = m_VBufferLighting[i];
                    newArrayRT[i] = m_VBufferLightingRT[i];
                }

                // Reassign and release memory.
                m_VBufferLighting   = newArray;
                m_VBufferLightingRT = newArrayRT;
            }
        }

        Debug.Assert(m_VBufferLighting != null);

        int w = 0, h = 0, d = 0;
        ComputeVBufferResolutionAndScale(screenWidth, screenHeight, ref w, ref h, ref d);

        for (int i = viewOffset * k_VBufferCount,
                 n = viewOffset * k_VBufferCount + k_VBufferCount; i < n; i++)
        {
            m_VBufferLighting[i] = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            m_VBufferLighting[i].filterMode        = FilterMode.Trilinear;   // Custom
            m_VBufferLighting[i].dimension         = TextureDimension.Tex3D; // TODO: request the thick 3D tiling layout
            m_VBufferLighting[i].volumeDepth       = d;
            m_VBufferLighting[i].enableRandomWrite = true;
            m_VBufferLighting[i].Create();

            m_VBufferLightingRT[i] = new RenderTargetIdentifier(m_VBufferLighting[i]);
        }
    }

    void DestroyVBuffer(int viewId)
    {
        int viewOffset = ViewOffsetFromViewId(viewId);

        if (viewOffset < 0)
        {
            // Not found.
            return;
        }

        int lastOffset = m_ViewCount - 1;
        Debug.Assert(lastOffset >= 0);

        if (m_VBufferLighting != null)
        {
            Debug.Assert(m_VBufferLighting.Length >= m_ViewCount * k_VBufferCount);

            for (int i = 0; i < k_VBufferCount; i++)
            {
                int viewBuffer = viewOffset * k_VBufferCount + i;
                int lastBuffer = lastOffset * k_VBufferCount + i;

                // Release the memory.
                if (m_VBufferLighting[viewBuffer] != null)
                {
                    m_VBufferLighting[viewBuffer].Release();
                }

                // Swap with the last element.
                m_VBufferLighting[viewBuffer]   = m_VBufferLighting[lastBuffer];
                m_VBufferLightingRT[viewBuffer] = m_VBufferLightingRT[lastBuffer];
            }
        }

        // Swap with the last element and shrink the array.
        m_ViewIdArray[viewOffset] = m_ViewIdArray[lastOffset];
        m_ViewCount--;
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

    RenderTargetIdentifier GetVBufferLightingHistory(int viewOffset) // From the previous frame
    {
        return m_VBufferLightingRT[viewOffset * k_VBufferCount + ((Time.renderedFrameCount + 0) & 1)]; // Does not work in the Scene view
    }

    RenderTargetIdentifier GetVBufferLightingFeedback(int viewOffset) // For the next frame
    {
        return m_VBufferLightingRT[viewOffset * k_VBufferCount + ((Time.renderedFrameCount + 1) & 1)]; // Does not work in the Scene view
    }

    RenderTargetIdentifier GetVBufferLightingIntegral(int viewOffset) // Of the current frame
    {
        return m_VBufferLightingRT[viewOffset * k_VBufferCount + 2];
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

        int viewId     = camera.camera.GetInstanceID();
        int viewOffset = ViewOffsetFromViewId(viewId);

        Debug.Assert(viewOffset >= 0 && viewOffset < 8);

        cmd.SetGlobalVector( HDShaderIDs._VBufferResolution,          new Vector4(w, h, 1.0f / w, 1.0f / h));
        cmd.SetGlobalVector( HDShaderIDs._VBufferScaleAndSliceCount,  new Vector4(scale.x, scale.y, d, 1.0f / d));
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthEncodingParams, ComputeLogarithmicDepthEncodingParams(m_VBufferNearPlane, m_VBufferFarPlane));
        cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,            GetVBufferLightingIntegral(viewOffset));
    }

    // Ref: https://en.wikipedia.org/wiki/Close-packing_of_equal_spheres
    // The returned {x, y} coordinates (and all spheres) are all within the (-0.5, 0.5)^2 range.
    // The pattern has been rotated by 15 degrees to maximize the resolution along X and Y:
    // https://www.desmos.com/calculator/kcpfvltz7c
    Vector2[] GetHexagonalClosePackedSpheres7()
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

    void VolumetricLightingPass(HDCamera camera, CommandBuffer cmd)
    {
        if (m_VolumetricLightingPreset == VolumetricLightingPreset.Off) return;

        using (new ProfilingSample(cmd, "Volumetric Lighting"))
        {
            int viewId     = camera.camera.GetInstanceID(); // Warning: different views can use the same camera
            int viewOffset = ViewOffsetFromViewId(viewId);

            Debug.Assert(viewOffset >= 0 && viewOffset < 8);

            if (GetGlobalFogComponent() == null)
            {
                // Clear the render target instead of running the shader.
                // CoreUtils.SetRenderTarget(cmd, GetVBufferLightingIntegral(viewOffset), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                // return;

                // Clearing 3D textures does not seem to work!
                // Use the workaround by running the full shader with no volume.
            }

            bool enableClustered    = m_FrameSettings.lightLoopSettings.enableTileAndCluster;
            bool enableReprojection = Application.isPlaying && camera.camera.cameraType == CameraType.Game;

            int kernel;

            if (enableReprojection)
            {
                // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
                kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClusteredReproj"
                                                                           : "VolumetricLightingAllLightsReproj");
            }
            else
            {
                kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClustered"
                                                                           : "VolumetricLightingAllLights");

            }

            int w = 0, h = 0, d = 0;
            Vector2 scale = ComputeVBufferResolutionAndScale(camera.screenSize.x, camera.screenSize.y, ref w, ref h, ref d);
            float   vFoV  = camera.camera.fieldOfView * Mathf.Deg2Rad;

            // Compose the matrix which allows us to compute the world space view direction.
            // Compute it using the scaled resolution to account for the visible area of the VBuffer.
            Vector4   scaledRes = new Vector4(w * scale.x, h * scale.y, 1.0f / (w * scale.x), 1.0f / (h * scale.y));
            Matrix4x4 transform = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, scaledRes, camera.viewMatrix, false);

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
            cmd.SetComputeVectorParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferSampleOffset,     offset);
            cmd.SetComputeMatrixParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferCoordToViewDirWS, transform);
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingHistory,  GetVBufferLightingHistory(viewOffset));  // Read
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingFeedback, GetVBufferLightingFeedback(viewOffset)); // Write
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingIntegral, GetVBufferLightingIntegral(viewOffset)); // Write

            // The shader defines GROUP_SIZE_1D = 16.
            cmd.DispatchCompute(m_VolumetricLightingCS, kernel, (w + 15) / 16, (h + 15) / 16, 1);
        }
    }
} // class HDRenderPipeline
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
