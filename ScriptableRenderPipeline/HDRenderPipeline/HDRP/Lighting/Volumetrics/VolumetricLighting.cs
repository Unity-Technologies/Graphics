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
        public long                     viewID       =   -1; // -1 is invalid; positive for Game Views, 0 otherwise
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

        public void Create(long viewID, int w, int h, int d)
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
                    if (this.lightingRTEX[i] != null)
                    {
                        this.lightingRTEX[i].Release();
                    }
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
    const float   k_LogScale         = 0.5f;

    public void Build(HDRenderPipelineAsset asset)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        m_VolumetricLightingCS = asset.renderPipelineResources.volumetricLightingCS;
        m_VBuffers = new List<VBuffer>(0);
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

        long viewID = camera.GetViewID();

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
                if (viewID == m_VBuffers[i].viewID && m_VBuffers[i].lightingRTEX != null && m_VBuffers[i].lightingRTEX[0] != null)
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

    public unsafe struct ZonalHarmonicsL2
    {
        public fixed float coeffs[3];
    };

    public static unsafe ZonalHarmonicsL2 GetHenyeyGreensteinPhaseFunction(float asymmetry)
    {
        float g = asymmetry;

        ZonalHarmonicsL2 zh = new ZonalHarmonicsL2();

        zh.coeffs[0] = 0.5f * Mathf.Sqrt(1.0f / Mathf.PI);
        zh.coeffs[1] = 0.5f * Mathf.Sqrt(3.0f / Mathf.PI) * g;
        zh.coeffs[2] = 0.5f * Mathf.Sqrt(5.0f / Mathf.PI) * g * g;

        return zh;
    }

    public static unsafe ZonalHarmonicsL2 GetCornetteShanksPhaseFunction(float asymmetry)
    {
        float g = asymmetry;

        ZonalHarmonicsL2 zh = new ZonalHarmonicsL2();

        zh.coeffs[0] = 0.282095f;
        zh.coeffs[1] = 0.293162f * g * (4.0f + (g * g)) / (2.0f + (g * g));
        zh.coeffs[2] = (0.126157f + 1.44179f * (g * g) + 0.324403f * (g * g) * (g * g)) / (2.0f + (g * g));

        return zh;
    }

    // Ref: "Stupid Spherical Harmonics Tricks", p. 6.
    public static unsafe SphericalHarmonicsL2 Convolve(SphericalHarmonicsL2 sh, ZonalHarmonicsL2 zh)
    {
        for (int l = 0; l <= 2; l++)
        {
            float n = Mathf.Sqrt((4.0f * Mathf.PI) / (2 * l + 1));
            float k = zh.coeffs[l];
            float p = n * k;

            for (int m = -l; m <= l; m++)
            {
                int i = l * (l + 1) + m;

                for (int c = 0; c < 3; c++)
                {
                    sh[c, i] *= p;
                }
            }
        }

        return sh;
    }

    // Undoes coefficient normalization to obtain the canonical values of SH.
    public static SphericalHarmonicsL2 DenormalizeSH(SphericalHarmonicsL2 sh)
    {    
        float sqrtPi = Mathf.Sqrt(Mathf.PI);

        const float c0 = 0.28209479177387814347f; // 1/2  * sqrt(1/Pi)
        const float c1 = 0.32573500793527994772f; // 1/3  * sqrt(3/Pi)
        const float c2 = 0.27313710764801976764f; // 1/8  * sqrt(15/Pi)
        const float c3 = 0.07884789131313000151f; // 1/16 * sqrt(5/Pi)
        const float c4 = 0.13656855382400988382f; // 1/16 * sqrt(15/Pi)

        // Compute the inverse of SphericalHarmonicsL2::kNormalizationConstants.
        // See SetSHEMapConstants() in "Stupid Spherical Harmonics Tricks". Note that we do not multiply by 3 here.
        float[] invNormConsts = { 1.0f / c0, -1.0f / c1, 1.0f / c1, -1.0f / c1, 1.0f / c2, -1.0f / c2, 1.0f / c3, -1.0f / c2, 1.0f / c4 };

        for (int c = 0; c < 3; c++)
        {
            for (int i = 0; i < 9; i++)
            {
                sh[c, i] *= invNormConsts[i];
            }
        }

        return sh;
    }

    // Premultiplies the SH with the polynomial coefficients of SH basis functions,
    // which avoids using any constants during SH evaluation.
    // The resulting evaluation takes the form:
    // c_0 + c_1 y + c_2 z + c_3 x + c_4 x y + c_5 y z + c_6 (3 z^2 - 1) + c_7 x z + c_8 (x^2 - y^2)
    public static SphericalHarmonicsL2 PremultiplySH(SphericalHarmonicsL2 sh)
    {
        const float k0 = 0.28209479177387814347f; // {0, 0} : 1/2 * sqrt(1/Pi)
        const float k1 = 0.48860251190291992159f; // {1, 0} : 1/2 * sqrt(3/Pi)
        const float k2 = 1.09254843059207907054f; // {2,-2} : 1/2 * sqrt(15/Pi)
        const float k3 = 0.31539156525252000603f; // {2, 0} : 1/4 * sqrt(5/Pi)
        const float k4 = 0.54627421529603953527f; // {2, 2} : 1/4 * sqrt(15/Pi)

        float[] ks = { k0, -k1, k1, -k1, k2, -k2, k3, -k2, k4 };

        for (int c = 0; c < 3; c++)
        {
            for (int i = 0; i < 9; i++)
            {
                sh[c, i] *= ks[i];
            }
        }

        return sh;
    }

    void SetPreconvolvedAmbientLightProbe(CommandBuffer cmd, float asymmetry)
    {
        SphericalHarmonicsL2 probeSH = DenormalizeSH(RenderSettings.ambientProbe);
        ZonalHarmonicsL2     phaseZH = GetCornetteShanksPhaseFunction(asymmetry);
        SphericalHarmonicsL2 finalSH = PremultiplySH(Convolve(probeSH, phaseZH));

        // Reorder coefficients in the MAD form:
        // HornerForm[c_0 + c_1 y + c_2 z + c_3 x + c_4 x y + c_5 y z + c_6 (3 z^2 - 1) + c_7 x z + c_8 (x^2 - y^2)]
        // = z (3 c_6 z + c_7 x + c_2) + y (-c_8 y + c_5 z + c_4 x + c_1) + x (c_8 x + c_3) + (c_0 - c_6)
        Vector4[] coeffs = new Vector4[9];

        const int r = 0, g = 1, b = 2;

        coeffs[0] = new Vector3(finalSH[r, 0], finalSH[g, 0], finalSH[b, 0])
                  - new Vector3(finalSH[r, 6], finalSH[g, 6], finalSH[b, 6]);
        coeffs[1] = new Vector3(finalSH[r, 3], finalSH[g, 3], finalSH[b, 3]);
        coeffs[2] = new Vector3(finalSH[r, 8], finalSH[g, 8], finalSH[b, 8]);
        coeffs[3] = new Vector3(finalSH[r, 1], finalSH[g, 1], finalSH[b, 1]);
        coeffs[4] = new Vector3(finalSH[r, 4], finalSH[g, 4], finalSH[b, 4]);
        coeffs[5] = new Vector3(finalSH[r, 5], finalSH[g, 5], finalSH[b, 5]);
        // Avoid reduplicating c_8.
        coeffs[6] = new Vector3(finalSH[r, 2], finalSH[g, 2], finalSH[b, 2]);
        coeffs[7] = new Vector3(finalSH[r, 7], finalSH[g, 7], finalSH[b, 7]);
        coeffs[8] = new Vector3(finalSH[r, 6], finalSH[g, 6], finalSH[b, 6]) * 3.0f;

        cmd.SetGlobalVectorArray(HDShaderIDs._AmbientProbeCoeffs, coeffs);
    }

    float CornetteShanksPhasePartConstant(float asymmetry)
    {
        float g = asymmetry;

        return (1.0f / (4.0f * Mathf.PI)) * 1.5f * (1.0f - g * g) / (2.0f + g * g);
    }

    public void PushGlobalParams(HDCamera camera, CommandBuffer cmd)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        HomogeneousFog globalFogComponent = HomogeneousFog.GetGlobalFogComponent();

        // TODO: may want to cache these results somewhere.
        VolumeProperties globalFogProperties = (globalFogComponent != null) ? globalFogComponent.volumeParameters.GetProperties()
                                                                            : VolumeProperties.GetNeutralVolumeProperties();

        float asymmetry = globalFogComponent != null ? globalFogComponent.volumeParameters.asymmetry : 0;
        cmd.SetGlobalVector(HDShaderIDs._GlobalFog_Scattering, globalFogProperties.scattering);
        cmd.SetGlobalFloat( HDShaderIDs._GlobalFog_Extinction, globalFogProperties.extinction);
        cmd.SetGlobalFloat( HDShaderIDs._GlobalFog_Asymmetry,  asymmetry);

        int w = 0, h = 0, d = 0;
        Vector2 scale = ComputeVBufferResolutionAndScale(preset, (int)camera.screenSize.x, (int)camera.screenSize.y, ref w, ref h, ref d);

        VBuffer vBuffer = FindVBuffer(camera.GetViewID());
        Debug.Assert(vBuffer != null);

        SetPreconvolvedAmbientLightProbe(cmd, asymmetry);
        cmd.SetGlobalVector( HDShaderIDs._VBufferResolution,          new Vector4(w, h, 1.0f / w, 1.0f / h));
        cmd.SetGlobalVector( HDShaderIDs._VBufferScaleAndSliceCount,  new Vector4(scale.x, scale.y, d, 1.0f / d));
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthEncodingParams, ComputeLogarithmicDepthEncodingParams(m_VBufferNearPlane, m_VBufferFarPlane, k_LogScale));
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthDecodingParams, ComputeLogarithmicDepthDecodingParams(m_VBufferNearPlane, m_VBufferFarPlane, k_LogScale));
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

            HomogeneousFog globalFogComponent = HomogeneousFog.GetGlobalFogComponent();

            // TODO: may want to cache these results somewhere.
            VolumeProperties globalFogProperties = (globalFogComponent != null) ? globalFogComponent.volumeParameters.GetProperties()
                                                                                : VolumeProperties.GetNeutralVolumeProperties();

            float asymmetry = globalFogComponent != null ? globalFogComponent.volumeParameters.asymmetry : 0;

            if (globalFogComponent == null)
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
            cmd.SetComputeFloatParam(  m_VolumetricLightingCS,         HDShaderIDs._CornetteShanksConstant,  CornetteShanksPhasePartConstant(asymmetry));
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
