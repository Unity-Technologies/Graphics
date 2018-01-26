using System;
using UnityEngine.Rendering;
using System.Collections.Generic;

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
    public float  asymmetry;    // Single global parameter for all volumes. TODO: UX

    public VolumeParameters()
    {
        bounds       = new Bounds(Vector3.zero, Vector3.positiveInfinity);
        albedo       = new Color(0.5f, 0.5f, 0.5f);
        meanFreePath = 10.0f;
        asymmetry    = 0.0f;
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

        asymmetry = Mathf.Clamp(asymmetry, -1.0f, 1.0f);
    }

    public VolumeProperties GetProperties()
    {
        VolumeProperties properties = new VolumeProperties();

        properties.scattering = GetScatteringCoefficient();
        properties.extinction = GetExtinctionCoefficient();

        return properties;
    }
} // class VolumeParameters

public class VolumetricLightingModule
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
        public int                      viewID       =   -1; // -1 is invalid; positive for Game Views, 0 otherwise
        public RenderTexture[]          lightingRTEX = null;
        public RenderTargetIdentifier[] lightingRTID = null;

        public RenderTargetIdentifier GetLightingIntegralBuffer() // Of the current frame
        {
            Debug.Assert(viewID >= 0);
            return lightingRTID[0];
        }

        public RenderTargetIdentifier GetLightingHistoryBuffer() // From the previous frame
        {
            Debug.Assert(viewID > 0); // Game View only
            return lightingRTID[1 + ((Time.renderedFrameCount + 0) & 1)];
        }

        public RenderTargetIdentifier GetLightingFeedbackBuffer() // For the next frame
        {
            Debug.Assert(viewID > 0); // Game View only
            return lightingRTID[1 + ((Time.renderedFrameCount + 1) & 1)];
        }

        public void Create(int viewID, int w, int h, int d)
        {
            Debug.Assert(viewID >= 0);
            Debug.Assert(w > 0 && h > 0 && d > 0);

            // Clean up first.
            Destroy();

            // The required number of buffers depends on the view type.
            bool isGameView = viewID > 0;
            int  n = isGameView ? 3 : 1;

            this.viewID       = viewID;
            this.lightingRTEX = new RenderTexture[n];
            this.lightingRTID = new RenderTargetIdentifier[n];

            for (int i = 0; i < n; i++)
            {
                this.lightingRTEX[i] = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                this.lightingRTEX[i].filterMode        = FilterMode.Trilinear;   // Custom
                this.lightingRTEX[i].dimension         = TextureDimension.Tex3D; // TODO: request the thick 3D tiling layout
                this.lightingRTEX[i].volumeDepth       = d;
                this.lightingRTEX[i].enableRandomWrite = true;
                this.lightingRTEX[i].Create();

                this.lightingRTID[i] = new RenderTargetIdentifier(this.lightingRTEX[i]);
            }
        }

        public void Destroy()
        {
            if (this.lightingRTEX != null)
            {
                for (int i = 0, n = this.lightingRTEX.Length; i < n; i++)
                {
                    this.lightingRTEX[i].Release();
                }
            }

            this.viewID       =   -1;
            this.lightingRTEX = null;
            this.lightingRTID = null;
        }
    } // class VBuffer

    public VolumetricLightingPreset preset { get { return (VolumetricLightingPreset)Math.Min(ShaderConfig.s_VolumetricLightingPreset, (int)VolumetricLightingPreset.Count); } }

    ComputeShader m_VolumetricLightingCS = null;

    List<VBuffer> m_VBuffers         = null;
    float         m_VBufferNearPlane = 0.5f;  // Distance in meters; dynamic modifications not handled by reprojection
    float         m_VBufferFarPlane  = 64.0f; // Distance in meters; dynamic modifications not handled by reprojection

    public void Build(HDRenderPipelineAsset asset)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        m_VolumetricLightingCS = asset.renderPipelineResources.volumetricLightingCS;
        m_VBuffers = new List<VBuffer>(1);
    }

    public void Cleanup()
    {
        if (preset == VolumetricLightingPreset.Off) return;

        m_VolumetricLightingCS = null;

        for (int i = 0, n = m_VBuffers.Count; i < n; i++)
        {
            m_VBuffers[i].Destroy();
        }

        m_VBuffers = null;
    }

    public void ResizeVBuffer(HDCamera camera, int screenWidth, int screenHeight)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        int viewID = camera.GetViewID();

        Debug.Assert(viewID >= 0);

        int w = 0, h = 0, d = 0;
        ComputeVBufferResolutionAndScale(preset, screenWidth, screenHeight, ref w, ref h, ref d);

        VBuffer vBuffer = FindVBuffer(viewID);

        if (vBuffer != null)
        {
            Debug.Assert(vBuffer.lightingRTEX    != null);
            Debug.Assert(vBuffer.lightingRTEX[0] != null);
            Debug.Assert(vBuffer.lightingRTID    != null);

            // Found, check resolution.
            if (w == vBuffer.lightingRTEX[0].width  &&
                h == vBuffer.lightingRTEX[0].height &&
                d == vBuffer.lightingRTEX[0].volumeDepth)
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

    VBuffer FindVBuffer(int viewID)
    {
        Debug.Assert(viewID >= 0);

        VBuffer vBuffer = null;

        if (m_VBuffers != null)
        {
            int n = m_VBuffers.Count;

            for (int i = 0; i < n; i++)
            {
                if (viewID == m_VBuffers[i].viewID)
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

    // Uses a logarithmic depth encoding.
    // Near plane: depth = 0; far plane: depth = 1.
    // x = n, y = log2(f/n), z = 1/n, w = 1/log2(f/n).
    static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane)
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

    public void PushGlobalParams(HDCamera camera, CommandBuffer cmd)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        HomogeneousFog globalFogComponent = HomogeneousFog.GetGlobalFogComponent();

        // TODO: may want to cache these results somewhere.
        VolumeProperties globalFogProperties = (globalFogComponent != null) ? globalFogComponent.volumeParameters.GetProperties()
                                                                            : VolumeProperties.GetNeutralVolumeProperties();

        cmd.SetGlobalVector(HDShaderIDs._GlobalFog_Scattering, globalFogProperties.scattering);
        cmd.SetGlobalFloat( HDShaderIDs._GlobalFog_Extinction, globalFogProperties.extinction);
        cmd.SetGlobalFloat( HDShaderIDs._GlobalFog_Asymmetry,  globalFogComponent != null ? globalFogComponent.volumeParameters.asymmetry : 0);

        int w = 0, h = 0, d = 0;
        Vector2 scale = ComputeVBufferResolutionAndScale(preset, (int)camera.screenSize.x, (int)camera.screenSize.y, ref w, ref h, ref d);

        VBuffer vBuffer = FindVBuffer(camera.GetViewID());
        Debug.Assert(vBuffer != null);

        cmd.SetGlobalVector( HDShaderIDs._VBufferResolution,          new Vector4(w, h, 1.0f / w, 1.0f / h));
        cmd.SetGlobalVector( HDShaderIDs._VBufferScaleAndSliceCount,  new Vector4(scale.x, scale.y, d, 1.0f / d));
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthEncodingParams, ComputeLogarithmicDepthEncodingParams(m_VBufferNearPlane, m_VBufferFarPlane));
        cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,            vBuffer.GetLightingIntegralBuffer());
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

    public void VolumetricLightingPass(HDCamera camera, CommandBuffer cmd, FrameSettings frameSettings)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        using (new ProfilingSample(cmd, "Volumetric Lighting"))
        {
            VBuffer vBuffer = FindVBuffer(camera.GetViewID());
            Debug.Assert(vBuffer != null);

            if (HomogeneousFog.GetGlobalFogComponent() == null)
            {
                // Clear the render target instead of running the shader.
                // CoreUtils.SetRenderTarget(cmd, GetVBufferLightingIntegral(viewOffset), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                // return;

                // Clearing 3D textures does not seem to work!
                // Use the workaround by running the full shader with no volume.
            }

            bool enableClustered    = frameSettings.lightLoopSettings.enableTileAndCluster;
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
            Vector2 scale = ComputeVBufferResolutionAndScale(preset, (int)camera.screenSize.x, (int)camera.screenSize.y, ref w, ref h, ref d);
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
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingIntegral, vBuffer.GetLightingIntegralBuffer()); // Write
            if (enableReprojection)
            {
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingFeedback, vBuffer.GetLightingFeedbackBuffer()); // Write
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingHistory,  vBuffer.GetLightingHistoryBuffer());  // Read
            }

            // The shader defines GROUP_SIZE_1D = 16.
            cmd.DispatchCompute(m_VolumetricLightingCS, kernel, (w + 15) / 16, (h + 15) / 16, 1);
        }
    }
} // class VolumetricLightingModule
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
