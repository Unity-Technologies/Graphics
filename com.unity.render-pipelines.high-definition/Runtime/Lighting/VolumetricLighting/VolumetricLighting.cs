using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'DensityVolumeArtistParameters'.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    struct DensityVolumeData
    {
        public Vector3 right;
        public float   extentX;
        public Vector3 up;
        public float   extentY;
        public Vector3 center;
        public float   extentZ;

        public Vector3 scattering;    // [0, 1]
        public float   extinction;    // [0, 1]
        public Vector3 textureTiling;
        public int     textureIndex;
        public Vector3 textureScroll;
        public int     invertFade;    // bool...
        public Vector3 rcpPosFaceFade;
        public float   rcpDistFadeLen;
        public Vector3 rcpNegFaceFade;
        public float   endTimesRcpDistFadeLen;
    } // struct VolumeProperties

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesVolumetric
    {
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float _VBufferCoordToViewDirWS[ShaderConfig.k_XRMaxViewsForCBuffer * 16];

        public float _VBufferUnitDepthTexelSpacing;
        public uint _Pad2_SVV;
        public float _CornetteShanksConstant;
        public uint _VBufferHistoryIsValid;

        public Vector4 _VBufferSampleOffset;
        public Vector4 _VolumeMaskDimensions;

        [HLSLArray(7, typeof(Vector4))]
        public fixed float _AmbientProbeCoeffs[7 * 4];  // 3 bands of SH, packed, rescaled and convolved with the phase function

        public float _VBufferVoxelSize;
        public float _HaveToPad;
        public float _OtherwiseTheBuffer;
        public float _IsFilledWithGarbage;
        public Vector4 _VBufferPrevViewportSize;
        public Vector4 _VBufferHistoryViewportScale;
        public Vector4 _VBufferHistoryViewportLimit;
        public Vector4 _VBufferPrevDistanceEncodingParams;
        public Vector4 _VBufferPrevDistanceDecodingParams;

        // TODO: Remove if equals to the ones in global CB?
        public uint _NumTileBigTileX;
        public uint _NumTileBigTileY;
        public uint _Pad0_SVV;
        public uint _Pad1_SVV;
    }

    class VolumeRenderingUtils
    {
        public static float MeanFreePathFromExtinction(float extinction)
        {
            return 1.0f / extinction;
        }

        public static float ExtinctionFromMeanFreePath(float meanFreePath)
        {
            return 1.0f / meanFreePath;
        }

        public static Vector3 AbsorptionFromExtinctionAndScattering(float extinction, Vector3 scattering)
        {
            return new Vector3(extinction, extinction, extinction) - scattering;
        }

        public static Vector3 ScatteringFromExtinctionAndAlbedo(float extinction, Vector3 albedo)
        {
            return extinction * albedo;
        }

        public static Vector3 AlbedoFromMeanFreePathAndScattering(float meanFreePath, Vector3 scattering)
        {
            return meanFreePath * scattering;
        }
    }

    struct DensityVolumeList
    {
        public List<DensityVolumeData> data;
    }

    struct VBufferParameters
    {
        public Vector3Int viewportSize;
        public float      voxelSize;
        public Vector4    depthEncodingParams;
        public Vector4    depthDecodingParams;

        public VBufferParameters(Vector3Int viewportSize, float depthExtent, float camNear, float camFar, float camVFoV,
                                 float sliceDistributionUniformity, float voxelSize)
        {
            this.viewportSize = viewportSize;
            this.voxelSize    = voxelSize;

            // The V-Buffer is sphere-capped, while the camera frustum is not.
            // We always start from the near plane of the camera.

            float aspectRatio    = viewportSize.x / (float)viewportSize.y;
            float farPlaneHeight = 2.0f * Mathf.Tan(0.5f * camVFoV) * camFar;
            float farPlaneWidth  = farPlaneHeight * aspectRatio;
            float farPlaneMaxDim = Mathf.Max(farPlaneWidth, farPlaneHeight);
            float farPlaneDist   = Mathf.Sqrt(camFar * camFar + 0.25f * farPlaneMaxDim * farPlaneMaxDim);

            float nearDist = camNear;
            float farDist = Math.Min(nearDist + depthExtent, farPlaneDist);

            float c = 2 - 2 * sliceDistributionUniformity; // remap [0, 1] -> [2, 0]
            c = Mathf.Max(c, 0.001f);                // Avoid NaNs

            depthEncodingParams = ComputeLogarithmicDepthEncodingParams(nearDist, farDist, c);
            depthDecodingParams = ComputeLogarithmicDepthDecodingParams(nearDist, farDist, c);
        }

        internal Vector3 ComputeViewportScale(Vector3Int bufferSize)
        {
            return new Vector3(HDUtils.ComputeViewportScale(viewportSize.x, bufferSize.x),
                               HDUtils.ComputeViewportScale(viewportSize.y, bufferSize.y),
                               HDUtils.ComputeViewportScale(viewportSize.z, bufferSize.z));
        }

        internal Vector3 ComputeViewportLimit(Vector3Int bufferSize)
        {
            return new Vector3(HDUtils.ComputeViewportLimit(viewportSize.x, bufferSize.x),
                               HDUtils.ComputeViewportLimit(viewportSize.y, bufferSize.y),
                               HDUtils.ComputeViewportLimit(viewportSize.z, bufferSize.z));
        }

        internal float ComputeLastSliceDistance(uint sliceCount)
        {
            float d = 1.0f - 0.5f / sliceCount;
            float ln2 = 0.69314718f;

            // DecodeLogarithmicDepthGeneralized(1 - 0.5 / sliceCount)
            return depthDecodingParams.x * Mathf.Exp(ln2 * d * depthDecodingParams.y) + depthDecodingParams.z;
        }

        // See EncodeLogarithmicDepthGeneralized().
        static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane, float c)
        {
            Vector4 depthParams = new Vector4();

            float n = nearPlane;
            float f = farPlane;

            depthParams.y = 1.0f / Mathf.Log(c * (f - n) + 1, 2);
            depthParams.x = Mathf.Log(c, 2) * depthParams.y;
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
            depthParams.y = Mathf.Log(c * (f - n) + 1, 2);
            depthParams.z = n - 1.0f / c; // Same
            depthParams.w = 0.0f;

            return depthParams;
        }
    }

    public partial class HDRenderPipeline
    {
        ComputeShader             m_VolumeVoxelizationCS          = null;
        ComputeShader             m_VolumetricLightingCS          = null;
        ComputeShader             m_VolumetricLightingFilteringCS = null;

        List<DensityVolumeData>   m_VisibleVolumeData             = null;
        const int                 k_MaxVisibleVolumeCount         = 512;

        // These two buffers do not depend on the frameID and are therefore shared by all views.
        RTHandle                  m_DensityBuffer;
        RTHandle                  m_LightingBuffer;
        Vector3Int                m_CurrentVolumetricBufferSize;

        ShaderVariablesVolumetric m_ShaderVariablesVolumetricCB = new ShaderVariablesVolumetric();

        // Is the feature globally disabled?
        bool m_SupportVolumetrics = false;

        Vector4[] m_PackedCoeffs;
        ZonalHarmonicsL2 m_PhaseZH;
        Vector2[] m_xySeq;

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
        float[] m_zSeq = { 7.0f / 14.0f, 3.0f / 14.0f, 11.0f / 14.0f, 5.0f / 14.0f, 9.0f / 14.0f, 1.0f / 14.0f, 13.0f / 14.0f };

        Matrix4x4[] m_PixelCoordToViewDirWS;

        static internal void SafeDestroy(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release(); // The texture itself is not destroyed: https://docs.unity3d.com/ScriptReference/RenderTexture.Release.html
                Object.DestroyImmediate(rt); // Destroy() may not be called from the Edit mode
            }
        }

        static internal Vector3Int ComputeVolumetricViewportSize(HDCamera hdCamera, ref float voxelSize)
        {
            var controller = hdCamera.volumeStack.GetComponent<Fog>();
            Debug.Assert(controller != null);

            int   viewportWidth  = hdCamera.actualWidth;
            int   viewportHeight = hdCamera.actualHeight;
            float screenFraction = controller.screenResolutionPercentage.value * 0.01f;
            int   sliceCount     = controller.volumeSliceCount.value;

            int w = Mathf.RoundToInt(viewportWidth  * screenFraction);
            int h = Mathf.RoundToInt(viewportHeight * screenFraction);
            int d = sliceCount;

            if (controller.screenResolutionPercentage.value == (1.0f/8.0f) * 100)
                voxelSize = 8;
            else
                voxelSize = 1.0f / screenFraction; // Does not account for rounding (same function, above)

            return new Vector3Int(w, h, d);
        }

        static internal VBufferParameters ComputeVolumetricBufferParameters(HDCamera hdCamera)
        {
            var controller = hdCamera.volumeStack.GetComponent<Fog>();
            Debug.Assert(controller != null);

            float voxelSize = 0;
            Vector3Int viewportSize = ComputeVolumetricViewportSize(hdCamera, ref voxelSize);

            return new VBufferParameters(viewportSize, controller.depthExtent.value,
                                         hdCamera.camera.nearClipPlane,
                                         hdCamera.camera.farClipPlane,
                                         hdCamera.camera.fieldOfView,
                                         controller.sliceDistributionUniformity.value,
                                         voxelSize);
        }

        static internal void ReinitializeVolumetricBufferParams(HDCamera hdCamera)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            bool fog  = Fog.IsVolumetricFogEnabled(hdCamera);
            bool init = hdCamera.vBufferParams != null;

            if (fog ^ init)
            {
                if (init)
                {
                    // Deinitialize.
                    hdCamera.vBufferParams = null;
                }
                else
                {
                    // Initialize.
                    // Start with the same parameters for both frames. Then update them one by one every frame.
                    var parameters = ComputeVolumetricBufferParameters(hdCamera);
                    hdCamera.vBufferParams = new VBufferParameters[2];
                    hdCamera.vBufferParams[0] = parameters;
                    hdCamera.vBufferParams[1] = parameters;
                }
            }
        }

        // This function relies on being called once per camera per frame.
        // The results are undefined otherwise.
        static internal void UpdateVolumetricBufferParams(HDCamera hdCamera, int frameIndex)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            Debug.Assert(hdCamera.vBufferParams != null);
            Debug.Assert(hdCamera.vBufferParams.Length == 2);

            var currentParams = ComputeVolumetricBufferParameters(hdCamera);

            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            hdCamera.vBufferParams[currIdx] = currentParams;

            // Handle case of first frame. When we are on the first frame, we reuse the value of original frame.
            if (hdCamera.vBufferParams[prevIdx].viewportSize.x == 0.0f && hdCamera.vBufferParams[prevIdx].viewportSize.y == 0.0f)
            {
                hdCamera.vBufferParams[prevIdx] = currentParams;
            }
        }

        // Do not access 'rt.name', it allocates memory every time...
        // Have to manually cache and pass the name.
        static internal void ResizeVolumetricBuffer(ref RTHandle rt, string name, int viewportWidth, int viewportHeight, int viewportDepth)
        {
            Debug.Assert(rt != null);

            int width  = rt.rt.width;
            int height = rt.rt.height;
            int depth  = rt.rt.volumeDepth;

            bool realloc = (width < viewportWidth) || (height < viewportHeight) || (depth < viewportDepth);

            if (realloc)
            {
                RTHandles.Release(rt);

                width  = Math.Max(width,  viewportWidth);
                height = Math.Max(height, viewportHeight);
                depth  = Math.Max(depth,  viewportDepth);

                rt = RTHandles.Alloc(width, height, depth, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                                     dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: name);
            }
        }

        static internal void CreateVolumetricHistoryBuffers(HDCamera hdCamera, int bufferCount)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            Debug.Assert(hdCamera.volumetricHistoryBuffers == null);

            hdCamera.volumetricHistoryBuffers = new RTHandle[bufferCount];

            // Allocation happens early in the frame. So we shouldn't rely on 'hdCamera.vBufferParams'.
            // Allocate the smallest possible 3D texture.
            // We will perform rescaling manually, in a custom manner, based on volume parameters.
            const int minSize = 4;

            for (int i = 0; i < bufferCount; i++)
            {
                hdCamera.volumetricHistoryBuffers[i] = RTHandles.Alloc(minSize, minSize, minSize, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                                                                       dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: string.Format("VBufferHistory{0}", i));
            }

            hdCamera.volumetricHistoryIsValid = false;
        }

        static internal void DestroyVolumetricHistoryBuffers(HDCamera hdCamera)
        {
            if (hdCamera.volumetricHistoryBuffers == null)
                return;

            int bufferCount = hdCamera.volumetricHistoryBuffers.Length;

            for (int i = 0; i < bufferCount; i++)
            {
                RTHandles.Release(hdCamera.volumetricHistoryBuffers[i]);
            }

            hdCamera.volumetricHistoryBuffers = null;
            hdCamera.volumetricHistoryIsValid = false;
        }

        // Must be called AFTER UpdateVolumetricBufferParams.
        static readonly string[] volumetricHistoryBufferNames = new string[2]{ "VBufferHistory0", "VBufferHistory1" };
        static internal void ResizeVolumetricHistoryBuffers(HDCamera hdCamera, int frameIndex)
        {
            if (!hdCamera.IsVolumetricReprojectionEnabled())
                return;

            Debug.Assert(hdCamera.vBufferParams != null);
            Debug.Assert(hdCamera.vBufferParams.Length == 2);
            Debug.Assert(hdCamera.volumetricHistoryBuffers != null);

            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currentParams = hdCamera.vBufferParams[currIdx];

            // Render texture contents can become "lost" on certain events, like loading a new level,
            // system going to a screensaver mode, in and out of fullscreen and so on.
            // https://docs.unity3d.com/ScriptReference/RenderTexture.html
            if (hdCamera.volumetricHistoryBuffers[0] == null || hdCamera.volumetricHistoryBuffers[1] == null)
            {
                DestroyVolumetricHistoryBuffers(hdCamera);
                CreateVolumetricHistoryBuffers(hdCamera, hdCamera.vBufferParams.Length); // Basically, assume it's 2
            }

            // We only resize the feedback buffer (#0), not the history buffer (#1).
            // We must NOT resize the buffer from the previous frame (#1), as that would invalidate its contents.
            ResizeVolumetricBuffer(ref hdCamera.volumetricHistoryBuffers[currIdx], volumetricHistoryBufferNames[currIdx], currentParams.viewportSize.x,
                                                                                                   currentParams.viewportSize.y,
                                                                                                   currentParams.viewportSize.z);
        }

        internal void CreateVolumetricLightingBuffers()
        {
            Debug.Assert(m_VolumetricLightingCS != null);
            Debug.Assert(m_DensityBuffer  == null);
            Debug.Assert(m_LightingBuffer == null);

            m_VisibleVolumeData = new List<DensityVolumeData>();

            // Allocate the smallest possible 3D texture.
            // We will perform rescaling manually, in a custom manner, based on volume parameters.
            const int minSize = 4;

            m_DensityBuffer = RTHandles.Alloc(minSize, minSize, minSize, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                                               dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "VBufferDensity");

            m_LightingBuffer = RTHandles.Alloc(minSize, minSize, minSize, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                                               dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "VBufferLighting");
        }

        internal void DestroyVolumetricLightingBuffers()
        {
            RTHandles.Release(m_LightingBuffer);
            RTHandles.Release(m_DensityBuffer);

            m_VisibleVolumeData = null; // free()
        }

        // Must be called AFTER UpdateVolumetricBufferParams.
        internal void ResizeVolumetricLightingBuffers(HDCamera hdCamera, int frameIndex)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            Debug.Assert(hdCamera.vBufferParams != null);

            // Render texture contents can become "lost" on certain events, like loading a new level,
            // system going to a screensaver mode, in and out of fullscreen and so on.
            // https://docs.unity3d.com/ScriptReference/RenderTexture.html
            if (m_DensityBuffer == null || m_LightingBuffer == null)
            {
                DestroyVolumetricLightingBuffers();
                CreateVolumetricLightingBuffers();
            }

            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currentParams = hdCamera.vBufferParams[currIdx];

            ResizeVolumetricBuffer(ref m_DensityBuffer, "VBufferDensity", currentParams.viewportSize.x,
                                                                            currentParams.viewportSize.y,
                                                                            currentParams.viewportSize.z);
            ResizeVolumetricBuffer(ref m_LightingBuffer, "VBufferLighting", currentParams.viewportSize.x,
                                                                            currentParams.viewportSize.y,
                                                                            currentParams.viewportSize.z);

            // TODO RENDERGRAPH: For now those texture are not handled by render graph.
            // When they are we won't have the m_DensityBuffer handy for getting the current size in UpdateShaderVariablesGlobalVolumetrics
            // So we store the size here and in time we'll fill this vector differently.
            m_CurrentVolumetricBufferSize = new Vector3Int(m_DensityBuffer.rt.width, m_DensityBuffer.rt.height, m_DensityBuffer.rt.volumeDepth);
        }

        void InitializeVolumetricLighting()
        {
            m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return;

            m_VolumeVoxelizationCS = defaultResources.shaders.volumeVoxelizationCS;
            m_VolumetricLightingCS = defaultResources.shaders.volumetricLightingCS;
            m_VolumetricLightingFilteringCS = defaultResources.shaders.volumetricLightingFilteringCS;

            m_PackedCoeffs = new Vector4[7];
            m_PhaseZH = new ZonalHarmonicsL2();
            m_PhaseZH.coeffs = new float[3];

            m_xySeq = new Vector2[7];

            m_PixelCoordToViewDirWS = new Matrix4x4[ShaderConfig.s_XrMaxViews];

            CreateVolumetricLightingBuffers();
        }

        void CleanupVolumetricLighting()
        {
            // Note: No need to test for support volumetric here, we do saferelease and null assignation
            DestroyVolumetricLightingBuffers();

            m_VolumeVoxelizationCS = null;
            m_VolumetricLightingCS = null;
            m_VolumetricLightingFilteringCS = null;
        }

        static float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (3.0f / (8.0f * Mathf.PI)) * (1.0f - g * g) / (2.0f + g * g);
        }

        void UpdateShaderVariablesGlobalVolumetrics(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
            {
                return;
            }

            // Get the interpolated anisotropy value.
            var fog = hdCamera.volumeStack.GetComponent<Fog>();
            int frameIndex = m_FrameCount;
            int currIdx = (frameIndex + 0) & 1;

            var currParams = hdCamera.vBufferParams[currIdx];

            // The lighting & density buffers are shared by all cameras.
            // The history & feedback buffers are specific to the camera.
            // These 2 types of buffers can have different sizes.
            // Additionally, history buffers can have different sizes, since they are not resized at the same time.
            var cvp = currParams.viewportSize;

            // Adjust slices for XR rendering: VBuffer is shared for all single-pass views
            uint sliceCount = (uint)(cvp.z / hdCamera.viewCount);

            cb._VBufferViewportSize = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            cb._VBufferSliceCount = sliceCount;
            cb._VBufferRcpSliceCount = 1.0f / sliceCount;
            cb._VBufferLightingViewportScale = currParams.ComputeViewportScale(m_CurrentVolumetricBufferSize);
            cb._VBufferLightingViewportLimit = currParams.ComputeViewportLimit(m_CurrentVolumetricBufferSize);
            cb._VBufferDistanceEncodingParams = currParams.depthEncodingParams;
            cb._VBufferDistanceDecodingParams = currParams.depthDecodingParams;
            cb._VBufferLastSliceDist = currParams.ComputeLastSliceDistance(sliceCount);
            cb._VBufferRcpInstancedViewCount = 1.0f / hdCamera.viewCount;
        }

        DensityVolumeList PrepareVisibleDensityVolumeList(HDCamera hdCamera, CommandBuffer cmd, float time)
        {
            DensityVolumeList densityVolumes = new DensityVolumeList();

            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return densityVolumes;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareVisibleDensityVolumeList)))
            {
                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset   = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var volumes = DensityVolumeManager.manager.PrepareDensityVolumeData(cmd, hdCamera, time);

                for (int i = 0; i < Math.Min(volumes.Count, k_MaxVisibleVolumeCount); i++)
                {
                    DensityVolume volume = volumes[i];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Make the OBB camera-relative.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        // TODO: cache these?
                        var data = volume.parameters.ConvertToEngineData(obb);

                        m_VisibleVolumeData.Add(data);
                    }
                }

                // Fill the struct with pointers in order to share the data with the light loop.
                densityVolumes.data = m_VisibleVolumeData;

                return densityVolumes;
            }
        }

        struct VolumeVoxelizationParameters
        {
            public ComputeShader                voxelizationCS;
            public int                          voxelizationKernel;

            public Vector4                      resolution;
            public int                          viewCount;
            public bool                         tiledLighting;

            public Texture3D                    volumeAtlas;

            public ShaderVariablesVolumetric    volumetricCB;
            public ShaderVariablesLightList     lightListCB;
        }

        unsafe void SetPreconvolvedAmbientLightProbe(ref ShaderVariablesVolumetric cb, HDCamera hdCamera, Fog fog)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, fog.globalLightProbeDimmer.value);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZH, fog.anisotropy.value);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZH));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffs, finalSH);
            for (int i = 0; i < 7; i++)
                for (int j = 0; j < 4; ++j)
                    cb._AmbientProbeCoeffs[i * 4 + j] = m_PackedCoeffs[i][j];
        }

        unsafe void UpdateShaderVariableslVolumetrics(ref ShaderVariablesVolumetric cb, HDCamera hdCamera, in Vector4 resolution, int frameIndex)
        {
            var fog = hdCamera.volumeStack.GetComponent<Fog>();
            var vFoV = hdCamera.camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            var gpuAspect = HDUtils.ProjectionMatrixAspect(hdCamera.mainViewConstants.projMatrix);

            // Compose the matrix which allows us to compute the world space view direction.
            hdCamera.GetPixelCoordToViewDirWS(resolution, gpuAspect, ref m_PixelCoordToViewDirWS);

            for (int i = 0; i < m_PixelCoordToViewDirWS.Length; ++i)
                for (int j = 0; j < 16; ++j)
                    cb._VBufferCoordToViewDirWS[i * 16 + j] = m_PixelCoordToViewDirWS[i][j];
            cb._VBufferUnitDepthTexelSpacing = HDUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, resolution.y);
            cb._CornetteShanksConstant = CornetteShanksPhasePartConstant(fog.anisotropy.value);
            cb._VBufferHistoryIsValid = hdCamera.volumetricHistoryIsValid ? 1u : 0u;

            GetHexagonalClosePackedSpheres7(m_xySeq);
            int sampleIndex = frameIndex % 7;
            Vector4 xySeqOffset = new Vector4();
            // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
            // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
            xySeqOffset.Set(m_xySeq[sampleIndex].x, m_xySeq[sampleIndex].y, m_zSeq[sampleIndex], frameIndex);
            cb._VBufferSampleOffset = xySeqOffset;

            var volumeAtlas = DensityVolumeManager.manager.volumeAtlas.GetAtlas();
            cb._VolumeMaskDimensions = Vector4.zero;
            if (DensityVolumeManager.manager.volumeAtlas.GetAtlas() != null)
            {
                cb._VolumeMaskDimensions.x = (float)volumeAtlas.width / volumeAtlas.depth; // 1 / number of textures
                cb._VolumeMaskDimensions.y = volumeAtlas.width;
                cb._VolumeMaskDimensions.z = volumeAtlas.depth;
                cb._VolumeMaskDimensions.w = Mathf.Log(volumeAtlas.width, 2); // Max LoD
            }

            SetPreconvolvedAmbientLightProbe(ref cb, hdCamera, fog);

            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currParams = hdCamera.vBufferParams[currIdx];
            var prevParams = hdCamera.vBufferParams[prevIdx];

            var pvp = prevParams.viewportSize;

            // The lighting & density buffers are shared by all cameras.
            // The history & feedback buffers are specific to the camera.
            // These 2 types of buffers can have different sizes.
            // Additionally, history buffers can have different sizes, since they are not resized at the same time.
            Vector3Int historyBufferSize = Vector3Int.zero;

            if (hdCamera.IsVolumetricReprojectionEnabled())
            {
                RTHandle historyRT = hdCamera.volumetricHistoryBuffers[prevIdx];
                historyBufferSize = new Vector3Int(historyRT.rt.width, historyRT.rt.height, historyRT.rt.volumeDepth);
            }

            cb._VBufferVoxelSize = currParams.voxelSize;
            cb._VBufferPrevViewportSize = new Vector4(pvp.x, pvp.y, 1.0f / pvp.x, 1.0f / pvp.y);
            cb._VBufferHistoryViewportScale = prevParams.ComputeViewportScale(historyBufferSize);
            cb._VBufferHistoryViewportLimit = prevParams.ComputeViewportLimit(historyBufferSize);
            cb._VBufferPrevDistanceEncodingParams = prevParams.depthEncodingParams;
            cb._VBufferPrevDistanceDecodingParams = prevParams.depthDecodingParams;
            cb._NumTileBigTileX = (uint)GetNumTileBigTileX(hdCamera);
            cb._NumTileBigTileY = (uint)GetNumTileBigTileY(hdCamera);
        }

        VolumeVoxelizationParameters PrepareVolumeVoxelizationParameters(HDCamera hdCamera, int frameIndex)
        {
            var parameters = new VolumeVoxelizationParameters();

            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currParams = hdCamera.vBufferParams[currIdx];

            parameters.viewCount = hdCamera.viewCount;
            parameters.tiledLighting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BinnedLighting) && m_BoundedEntityCollection.GetEntityCount(BoundedEntityCategory.DensityVolume) > 0;

            bool optimal = currParams.voxelSize == 8;

            parameters.voxelizationCS = m_VolumeVoxelizationCS;
            parameters.voxelizationKernel = (parameters.tiledLighting ? 1 : 0) | (!optimal ? 2 : 0);

            var cvp = currParams.viewportSize;

            parameters.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            parameters.volumeAtlas = DensityVolumeManager.manager.volumeAtlas.GetAtlas();

            if (parameters.volumeAtlas == null)
            {
                parameters.volumeAtlas = CoreUtils.blackVolumeTexture;
            }

            UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetricCB, hdCamera, parameters.resolution, frameIndex);
            parameters.volumetricCB = m_ShaderVariablesVolumetricCB;
            parameters.lightListCB = m_ShaderVariablesLightListCB;

            return parameters;
        }

        static void VolumeVoxelizationPass( in VolumeVoxelizationParameters parameters,
                                            RTHandle                        densityBuffer,
                                            ComputeBuffer                   bigTileLightList,
                                            CommandBuffer                   cmd)
        {
            if (parameters.tiledLighting)
                cmd.SetComputeBufferParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs.g_vBigTileLightList, bigTileLightList);

            cmd.SetComputeTextureParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs._VBufferDensity,  densityBuffer);
            cmd.SetComputeTextureParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs._VolumeMaskAtlas, parameters.volumeAtlas);

            ConstantBuffer.Push(cmd, parameters.volumetricCB, parameters.voxelizationCS, HDShaderIDs._ShaderVariablesVolumetric);
            ConstantBuffer.Set<ShaderVariablesLightList>(cmd, parameters.voxelizationCS, HDShaderIDs._ShaderVariablesLightList);

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(parameters.voxelizationCS, parameters.voxelizationKernel, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);
        }

        void VolumeVoxelizationPass(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumeVoxelization)))
            {
                var parameters = PrepareVolumeVoxelizationParameters(hdCamera, frameIndex);
                VolumeVoxelizationPass(parameters, m_DensityBuffer, m_TileAndClusterData.bigTileLightList, cmd);
            }
        }

        // Ref: https://en.wikipedia.org/wiki/Close-packing_of_equal_spheres
        // The returned {x, y} coordinates (and all spheres) are all within the (-0.5, 0.5)^2 range.
        // The pattern has been rotated by 15 degrees to maximize the resolution along X and Y:
        // https://www.desmos.com/calculator/kcpfvltz7c
        static void GetHexagonalClosePackedSpheres7(Vector2[] coords)
        {

            float r = 0.17054068870105443882f;
            float d = 2 * r;
            float s = r * Mathf.Sqrt(3);

            // Try to keep the weighted average as close to the center (0.5) as possible.
            //  (7)(5)    ( )( )    ( )( )    ( )( )    ( )( )    ( )(o)    ( )(x)    (o)(x)    (x)(x)
            // (2)(1)(3) ( )(o)( ) (o)(x)( ) (x)(x)(o) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x)
            //  (4)(6)    ( )( )    ( )( )    ( )( )    (o)( )    (x)( )    (x)(o)    (x)(x)    (x)(x)
            coords[0] = new Vector2(0,  0);
            coords[1] = new Vector2(-d,  0);
            coords[2] = new Vector2(d,  0);
            coords[3] = new Vector2(-r, -s);
            coords[4] = new Vector2(r,  s);
            coords[5] = new Vector2(r, -s);
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
        }

        struct VolumetricLightingParameters
        {
            public ComputeShader                volumetricLightingCS;
            public ComputeShader                volumetricLightingFilteringCS;
            public int                          volumetricLightingKernel;
            public int                          volumetricFilteringKernelX;
            public int                          volumetricFilteringKernelY;
            public bool                         tiledLighting;
            public Vector4                      resolution;
            public bool                         enableReprojection;
            public int                          viewCount;
            public bool                         filterVolume;
            public ShaderVariablesVolumetric    volumetricCB;
            public ShaderVariablesLightList     lightListCB;
        }

        VolumetricLightingParameters PrepareVolumetricLightingParameters(HDCamera hdCamera, int frameIndex)
        {
            var parameters = new VolumetricLightingParameters();

            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currParams = hdCamera.vBufferParams[currIdx];

            // Get the interpolated anisotropy value.
            var fog = hdCamera.volumeStack.GetComponent<Fog>();

            // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
            parameters.tiledLighting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BinnedLighting);
            parameters.enableReprojection = hdCamera.IsVolumetricReprojectionEnabled();
            bool enableAnisotropy = fog.anisotropy.value != 0;
            bool optimal = currParams.voxelSize == 8;

            parameters.volumetricLightingCS = m_VolumetricLightingCS;
            parameters.volumetricLightingCS.shaderKeywords = null;

            if(parameters.tiledLighting)
            {
                parameters.volumetricLightingCS.EnableKeyword("COARSE_BINNING");
            }

            if(parameters.enableReprojection)
            {
                parameters.volumetricLightingCS.EnableKeyword("ENABLE_REPROJECTION");
            }

            if(enableAnisotropy)
            {
                parameters.volumetricLightingCS.EnableKeyword("ENABLE_ANISOTROPY");
            }

            if (optimal)
            {
                parameters.volumetricLightingCS.EnableKeyword("VL_PRESET_OPTIMAL");
            }
            else
            {
                parameters.volumetricLightingCS.DisableKeyword("VL_PRESET_OPTIMAL");
            }

            parameters.volumetricLightingKernel = parameters.volumetricLightingCS.FindKernel("VolumetricLighting");


            parameters.volumetricLightingFilteringCS = m_VolumetricLightingFilteringCS;
            parameters.volumetricFilteringKernelX = parameters.volumetricLightingFilteringCS.FindKernel("FilterVolumetricLightingX");
            parameters.volumetricFilteringKernelY = parameters.volumetricLightingFilteringCS.FindKernel("FilterVolumetricLightingY");

            var cvp = currParams.viewportSize;

            parameters.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            parameters.viewCount = hdCamera.viewCount;
            parameters.filterVolume = fog.filter.value;

            UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetricCB, hdCamera, parameters.resolution, frameIndex);
            parameters.volumetricCB = m_ShaderVariablesVolumetricCB;
            parameters.lightListCB = m_ShaderVariablesLightListCB;

            return parameters;
        }

        static void VolumetricLightingPass( in VolumetricLightingParameters parameters,
                                            RTHandle                        depthTexture,
                                            RTHandle                        densityBuffer,
                                            RTHandle                        lightingBuffer,
                                            RTHandle                        historyRT,
                                            RTHandle                        feedbackRT,
                                            ComputeBuffer                   bigTileLightList,
                                            CommandBuffer                   cmd)
        {
            if (parameters.tiledLighting)
                cmd.SetComputeBufferParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs.g_vBigTileLightList, bigTileLightList);

            cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._CameraDepthTexture, depthTexture);  // Read
            cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferDensity,  densityBuffer);  // Read
            cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferLighting, lightingBuffer); // Write

            if (parameters.enableReprojection)
            {
                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferHistory,  historyRT);  // Read
                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferFeedback, feedbackRT); // Write
            }

            ConstantBuffer.Push(cmd, parameters.volumetricCB, parameters.volumetricLightingCS, HDShaderIDs._ShaderVariablesVolumetric);
            ConstantBuffer.Set<ShaderVariablesLightList>(cmd, parameters.volumetricLightingCS, HDShaderIDs._ShaderVariablesLightList);

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);
        }

        static void FilterVolumetricLighting(in VolumetricLightingParameters parameters, RTHandle outputBuffer, RTHandle inputBuffer, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricLightingFiltering)))
            {
                ConstantBuffer.Push(cmd, parameters.volumetricCB, parameters.volumetricLightingCS, HDShaderIDs._ShaderVariablesVolumetric);

                // The shader defines GROUP_SIZE_1D = 8.
                cmd.SetComputeTextureParam(parameters.volumetricLightingFilteringCS, parameters.volumetricFilteringKernelX, HDShaderIDs._VBufferFilteringInput,  inputBuffer);  // Read
                cmd.SetComputeTextureParam(parameters.volumetricLightingFilteringCS, parameters.volumetricFilteringKernelX, HDShaderIDs._VBufferFilteringOutput, outputBuffer); // Write
                cmd.DispatchCompute(parameters.volumetricLightingFilteringCS, parameters.volumetricFilteringKernelX, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);

                cmd.SetComputeTextureParam(parameters.volumetricLightingFilteringCS, parameters.volumetricFilteringKernelY, HDShaderIDs._VBufferFilteringInput,  outputBuffer); // Read
                cmd.SetComputeTextureParam(parameters.volumetricLightingFilteringCS, parameters.volumetricFilteringKernelY, HDShaderIDs._VBufferFilteringOutput, inputBuffer);  // Write
                cmd.DispatchCompute(parameters.volumetricLightingFilteringCS, parameters.volumetricFilteringKernelY, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);
            }
        }

        void VolumetricLightingPass(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
            {
                cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, HDUtils.clearTexture3D);
                return;
            }

            var parameters = PrepareVolumetricLightingParameters(hdCamera, frameIndex);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricLighting)))
            {
                RTHandle feedbackRT = null, historyRT = null;

                if (parameters.enableReprojection)
                {
                    var currIdx = (frameIndex + 0) & 1;
                    var prevIdx = (frameIndex + 1) & 1;

                    feedbackRT = hdCamera.volumetricHistoryBuffers[currIdx];
                    historyRT  = hdCamera.volumetricHistoryBuffers[prevIdx];
                }

                VolumetricLightingPass(parameters, m_SharedRTManager.GetDepthTexture(), m_DensityBuffer, m_LightingBuffer, historyRT, feedbackRT, m_TileAndClusterData.bigTileLightList, cmd);

                if (parameters.enableReprojection)
                    hdCamera.volumetricHistoryIsValid = true; // For the next frame...
            }

            // Let's filter out volumetric buffer
            if (parameters.filterVolume)
                FilterVolumetricLighting(parameters, m_DensityBuffer, m_LightingBuffer, cmd);

            cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, m_LightingBuffer);
        }
    } // class VolumetricLightingModule
} // namespace UnityEngine.Rendering.HighDefinition
