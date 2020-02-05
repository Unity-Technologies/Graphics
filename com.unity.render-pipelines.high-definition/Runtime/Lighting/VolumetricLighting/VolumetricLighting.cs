using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'DensityVolumeArtistParameters'.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    struct DensityVolumeEngineData
    {
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

        public static DensityVolumeEngineData GetNeutralValues()
        {
            DensityVolumeEngineData data;

            data.scattering    = Vector3.zero;
            data.extinction    = 0;
            data.textureIndex  = -1;
            data.textureTiling = Vector3.one;
            data.textureScroll = Vector3.zero;
            data.rcpPosFaceFade    = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFaceFade    = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.invertFade    = 0;
            data.rcpDistFadeLen         = 0;
            data.endTimesRcpDistFadeLen = 1;

            return data;
        }
    } // struct VolumeProperties

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
        public List<OrientedBBox>      bounds;
        public List<DensityVolumeEngineData> density;
    }

    enum VolumetricLightingPreset
    {
        Off,
        Medium,
        High,
        Count
    }

    struct VBufferParameters
    {
        public Vector3Int viewportSize;
        public Vector4 depthEncodingParams;
        public Vector4 depthDecodingParams;

        public VBufferParameters(Vector3Int viewportResolution, float depthExtent, float camNear, float camFar, float camVFoV, float sliceDistributionUniformity)
        {
            viewportSize = viewportResolution;

            // The V-Buffer is sphere-capped, while the camera frustum is not.
            // We always start from the near plane of the camera.

            float aspectRatio = viewportResolution.x / (float)viewportResolution.y;
            float farPlaneHeight = 2.0f * Mathf.Tan(0.5f * camVFoV) * camFar;
            float farPlaneWidth = farPlaneHeight * aspectRatio;
            float farPlaneMaxDim = Mathf.Max(farPlaneWidth, farPlaneHeight);
            float farPlaneDist = Mathf.Sqrt(camFar * camFar + 0.25f * farPlaneMaxDim * farPlaneMaxDim);

            float nearDist = camNear;
            float farDist = Math.Min(nearDist + depthExtent, farPlaneDist);

            float c = 2 - 2 * sliceDistributionUniformity; // remap [0, 1] -> [2, 0]
            c = Mathf.Max(c, 0.001f);                // Avoid NaNs

            depthEncodingParams = ComputeLogarithmicDepthEncodingParams(nearDist, farDist, c);
            depthDecodingParams = ComputeLogarithmicDepthDecodingParams(nearDist, farDist, c);
        }

        internal Vector4 ComputeUvScaleAndLimit(Vector2Int bufferSize)
        {
            // The slice count is fixed for now.
            return HDUtils.ComputeUvScaleAndLimit(new Vector2Int(viewportSize.x, viewportSize.y), bufferSize);
        }

        internal float ComputeLastSliceDistance(int sliceCount)
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
        VolumetricLightingPreset      volumetricLightingPreset = VolumetricLightingPreset.Off;

        ComputeShader                 m_VolumeVoxelizationCS      = null;
        ComputeShader                 m_VolumetricLightingCS      = null;

        List<OrientedBBox>            m_VisibleVolumeBounds       = null;
        List<DensityVolumeEngineData> m_VisibleVolumeData         = null;
        const int                     k_MaxVisibleVolumeCount     = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        ComputeBuffer                 m_VisibleVolumeBoundsBuffer = null;
        ComputeBuffer                 m_VisibleVolumeDataBuffer   = null;

        // These two buffers do not depend on the frameID and are therefore shared by all views.
        RTHandle                      m_DensityBufferHandle;
        RTHandle                      m_LightingBufferHandle;

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

        void InitializeVolumetricLighting()
        {
            m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return;

            volumetricLightingPreset = asset.currentPlatformRenderPipelineSettings.increaseResolutionOfVolumetrics
                ? VolumetricLightingPreset.High
                : VolumetricLightingPreset.Medium;

            m_VolumeVoxelizationCS = defaultResources.shaders.volumeVoxelizationCS;
            m_VolumetricLightingCS = defaultResources.shaders.volumetricLightingCS;

            m_PackedCoeffs = new Vector4[7];
            m_PhaseZH = new ZonalHarmonicsL2();
            m_PhaseZH.coeffs = new float[3];

            m_xySeq = new Vector2[7];

            m_PixelCoordToViewDirWS = new Matrix4x4[ShaderConfig.s_XrMaxViews];

            CreateVolumetricLightingBuffers();
        }

        // RTHandleSystem API expects a function that computes the resolution. We define it here.
        // Note that the RTHandleSytem never reduces the size of the render target.
        // Therefore, if this function returns a smaller resolution, the size of the render target will not change.
        Vector2Int ComputeVBufferResolutionXY(Vector2Int screenSize)
        {
            Vector3Int resolution = ComputeVBufferResolution(volumetricLightingPreset, screenSize.x, screenSize.y);

            return new Vector2Int(resolution.x, resolution.y);
        }

        void CreateVolumetricLightingBuffers()
        {
            Debug.Assert(m_VolumetricLightingCS != null);

            m_VisibleVolumeBounds       = new List<OrientedBBox>();
            m_VisibleVolumeData         = new List<DensityVolumeEngineData>();
            m_VisibleVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
            m_VisibleVolumeDataBuffer   = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(DensityVolumeEngineData)));

            int d = ComputeVBufferSliceCount(volumetricLightingPreset);

            m_DensityBufferHandle = RTHandles.Alloc(scaleFunc:         ComputeVBufferResolutionXY,
                    slices:            d,
                    dimension:         TextureDimension.Tex3D,
                    colorFormat:       GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                    enableRandomWrite: true,
                    enableMSAA:        false,
                    /* useDynamicScale: true, // <- TODO */
                    name:              "VBufferDensity");

            m_LightingBufferHandle = RTHandles.Alloc(scaleFunc:         ComputeVBufferResolutionXY,
                    slices:            d,
                    dimension:         TextureDimension.Tex3D,
                    colorFormat:       GraphicsFormat.R16G16B16A16_SFloat,
                    enableRandomWrite: true,
                    enableMSAA:        false,
                    /* useDynamicScale: true, // <- TODO */
                    name:              "VBufferIntegral");
        }

        // For the initial allocation, no suballocation happens (the texture is full size).
        VBufferParameters ComputeVBufferParameters(HDCamera hdCamera)
        {
            Vector3Int viewportResolution = ComputeVBufferResolution(volumetricLightingPreset, hdCamera.actualWidth, hdCamera.actualHeight);

            var controller = hdCamera.volumeStack.GetComponent<Fog>();

            return new VBufferParameters(viewportResolution, controller.depthExtent.value,
                                         hdCamera.camera.nearClipPlane,
                                         hdCamera.camera.farClipPlane,
                                         hdCamera.camera.fieldOfView,
                                         controller.sliceDistributionUniformity.value);
        }

        internal void ReinitializeVolumetricBufferParams(HDCamera hdCamera)
        {
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
                    var parameters = ComputeVBufferParameters(hdCamera);
                    hdCamera.vBufferParams = new VBufferParameters[2];
                    hdCamera.vBufferParams[0] = parameters;
                    hdCamera.vBufferParams[1] = parameters;
                }
            }
        }

        // This function relies on being called once per camera per frame.
        // The results are undefined otherwise.
        internal void UpdateVolumetricBufferParams(HDCamera hdCamera)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            var parameters = ComputeVBufferParameters(hdCamera);

            // Double-buffer. I assume the cost of copying is negligible (don't want to use the frame index).
            // Handle case of first frame. When we are on the first frame, we reuse the value of original frame.
            if (hdCamera.vBufferParams[0].viewportSize.x == 0.0f && hdCamera.vBufferParams[0].viewportSize.y == 0.0f)
            {
                hdCamera.vBufferParams[1] = parameters;
            }
            else
            {
                hdCamera.vBufferParams[1] = hdCamera.vBufferParams[0];
            }
            hdCamera.vBufferParams[0] = parameters;
        }

        internal void AllocateVolumetricHistoryBuffers(HDCamera hdCamera, int bufferCount)
        {
            RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                frameIndex &= 1; // 0 or 1

                int d = ComputeVBufferSliceCount(volumetricLightingPreset);

                return rtHandleSystem.Alloc(scaleFunc: ComputeVBufferResolutionXY,
                    slices: d,
                    dimension: TextureDimension.Tex3D,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    enableRandomWrite: true,
                    enableMSAA: false,
                    /* useDynamicScale: true, // <- TODO */
                    name: string.Format("{0}_VBufferHistory{1}", viewName, frameIndex)
                    );
            }

            hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting, HistoryBufferAllocatorFunction, bufferCount);
        }

        void DestroyVolumetricLightingBuffers()
        {
            if (m_DensityBufferHandle != null)
                RTHandles.Release(m_DensityBufferHandle);
            if (m_LightingBufferHandle != null)
                RTHandles.Release(m_LightingBufferHandle);

            CoreUtils.SafeRelease(m_VisibleVolumeBoundsBuffer);
            CoreUtils.SafeRelease(m_VisibleVolumeDataBuffer);

            m_VisibleVolumeBounds = null;
            m_VisibleVolumeData   = null;
        }

        void CleanupVolumetricLighting()
        {
            // Note: No need to test for support volumetric here, we do saferelease and null assignation
            DestroyVolumetricLightingBuffers();

            m_VolumeVoxelizationCS = null;
            m_VolumetricLightingCS = null;
        }

        static int ComputeVBufferTileSize(VolumetricLightingPreset preset)
        {
            switch (preset)
            {
                case VolumetricLightingPreset.Medium:
                    return 8;
                case VolumetricLightingPreset.High:
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
            var result = 0;
            switch (preset)
            {
                case VolumetricLightingPreset.Medium:
                    result = 64;
                    break;
                case VolumetricLightingPreset.High:
                    result = 128;
                    break;
                case VolumetricLightingPreset.Off:
                    result = 0;
                    break;
                default:
                    Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                    result = 0;
                    break;
            }

            return result;
        }

        static Vector3Int ComputeVBufferResolution(VolumetricLightingPreset preset, int screenWidth, int screenHeight)
        {
            int t = ComputeVBufferTileSize(preset);

            int w = HDUtils.DivRoundUp(screenWidth,  t);
            int h = HDUtils.DivRoundUp(screenHeight, t);
            int d = ComputeVBufferSliceCount(preset);

            return new Vector3Int(w, h, d);
        }

        void SetPreconvolvedAmbientLightProbe(HDCamera hdCamera, CommandBuffer cmd, float dimmer, float anisotropy)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
                                 probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, dimmer);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZH, anisotropy);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZH));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffs, finalSH);
            cmd.SetGlobalVectorArray(HDShaderIDs._AmbientProbeCoeffs, m_PackedCoeffs);
        }

        static float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (3.0f / (8.0f * Mathf.PI)) * (1.0f - g * g) / (2.0f + g * g);
        }

        void PushVolumetricLightingGlobalParams(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
            {
                cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, HDUtils.clearTexture3D);
                return;
            }

            // Get the interpolated anisotropy value.
            var fog = hdCamera.volumeStack.GetComponent<Fog>();

            SetPreconvolvedAmbientLightProbe(hdCamera, cmd, fog.globalLightProbeDimmer.value, fog.anisotropy.value);

            var currFrameParams = hdCamera.vBufferParams[0];
            var prevFrameParams = hdCamera.vBufferParams[1];

            // The lighting & density buffers are shared by all cameras.
            // The history & feedback buffers are specific to the camera.
            // These 2 types of buffers can have different sizes.
            // Additionally, history buffers can have different sizes, since they are not resized at the same time
            // (every frame, we swap the buffers, and resize the feedback buffer but not the history buffer).
            // The viewport size is the same for all of these buffers.
            // All of these buffers may have sub-native-resolution viewports.
            // The 3rd dimension (number of slices) is the same for all of these buffers.
            Vector2Int sharedBufferSize = new Vector2Int(m_LightingBufferHandle.rt.width, m_LightingBufferHandle.rt.height);

            Debug.Assert(m_LightingBufferHandle.rt.width  == m_DensityBufferHandle.rt.width);
            Debug.Assert(m_LightingBufferHandle.rt.height == m_DensityBufferHandle.rt.height);

            Vector2Int historyBufferSize = Vector2Int.zero;

            if (hdCamera.IsVolumetricReprojectionEnabled())
            {
                var historyRT = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting);

                historyBufferSize = new Vector2Int(historyRT.rt.width, historyRT.rt.height);

                // Handle case of first frame. When we are on the first frame, we reuse the value of original frame.
                if (historyBufferSize.x == 0.0f && historyBufferSize.y == 0.0f)
                {
                    historyBufferSize = sharedBufferSize;
                }
            }

            var cvp = currFrameParams.viewportSize;
            var pvp = prevFrameParams.viewportSize;

            // Adjust slices for XR rendering: VBuffer is shared for all single-pass views
            int sliceCount = cvp.z / hdCamera.viewCount;

            cmd.SetGlobalVector(HDShaderIDs._VBufferViewportSize,               new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y));
            cmd.SetGlobalInt(   HDShaderIDs._VBufferSliceCount,                 sliceCount);
            cmd.SetGlobalFloat( HDShaderIDs._VBufferRcpSliceCount,              1.0f / sliceCount);
            cmd.SetGlobalVector(HDShaderIDs._VBufferSharedUvScaleAndLimit,      currFrameParams.ComputeUvScaleAndLimit(sharedBufferSize));
            cmd.SetGlobalVector(HDShaderIDs._VBufferDistanceEncodingParams,     currFrameParams.depthEncodingParams);
            cmd.SetGlobalVector(HDShaderIDs._VBufferDistanceDecodingParams,     currFrameParams.depthDecodingParams);
            cmd.SetGlobalFloat( HDShaderIDs._VBufferLastSliceDist,              currFrameParams.ComputeLastSliceDistance(sliceCount));
            cmd.SetGlobalFloat( HDShaderIDs._VBufferRcpInstancedViewCount,      1.0f / hdCamera.viewCount);

            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevViewportSize,           new Vector4(pvp.x, pvp.y, 1.0f / pvp.x, 1.0f / pvp.y));
            cmd.SetGlobalVector(HDShaderIDs._VBufferHistoryPrevUvScaleAndLimit, prevFrameParams.ComputeUvScaleAndLimit(historyBufferSize));
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevDepthEncodingParams,    prevFrameParams.depthEncodingParams);
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevDepthDecodingParams,    prevFrameParams.depthDecodingParams);

            cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,                  m_LightingBufferHandle);
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

                m_VisibleVolumeBounds.Clear();
                m_VisibleVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var volumes = DensityVolumeManager.manager.PrepareDensityVolumeData(cmd, hdCamera, time);

                for (int i = 0; i < Math.Min(volumes.Count, k_MaxVisibleVolumeCount); i++)
                {
                    DensityVolume volume = volumes[i];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        // TODO: cache these?
                        var data = volume.parameters.ConvertToEngineData();

                        m_VisibleVolumeBounds.Add(obb);
                        m_VisibleVolumeData.Add(data);
                    }
                }

                m_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
                m_VisibleVolumeDataBuffer.SetData(m_VisibleVolumeData);

                // Fill the struct with pointers in order to share the data with the light loop.
                densityVolumes.bounds  = m_VisibleVolumeBounds;
                densityVolumes.density = m_VisibleVolumeData;

                return densityVolumes;
            }
        }

        struct VolumeVoxelizationParameters
        {
            public ComputeShader    voxelizationCS;
            public int              voxelizationKernel;

            public Vector4          resolution;
            public int              numBigTileX, numBigTileY;
            public int              viewCount;
            public bool             tiledLighting;
            public float            unitDepthTexelSpacing;

            public int              numVisibleVolumes;
            public Texture3D        volumeAtlas;
            public Vector4          volumeAtlasDimensions;

            public Matrix4x4[]      pixelCoordToViewDirWS;
        }

        VolumeVoxelizationParameters PrepareVolumeVoxelizationParameters(HDCamera hdCamera)
        {
            var parameters = new VolumeVoxelizationParameters();

            parameters.viewCount = hdCamera.viewCount;
            parameters.numBigTileX = GetNumTileBigTileX(hdCamera);
            parameters.numBigTileY = GetNumTileBigTileY(hdCamera);

            parameters.tiledLighting = HasLightToCull() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
            bool highQuality = volumetricLightingPreset == VolumetricLightingPreset.High;

            parameters.voxelizationCS = m_VolumeVoxelizationCS;
            parameters.voxelizationKernel = (parameters.tiledLighting ? 1 : 0) | (highQuality ? 2 : 0);

            var currFrameParams = hdCamera.vBufferParams[0];
            var cvp = currFrameParams.viewportSize;

            parameters.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            var vFoV = hdCamera.camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            var gpuAspect = HDUtils.ProjectionMatrixAspect(hdCamera.mainViewConstants.projMatrix);

            // Compose the matrix which allows us to compute the world space view direction.
            hdCamera.GetPixelCoordToViewDirWS(parameters.resolution, gpuAspect, ref m_PixelCoordToViewDirWS);
            parameters.pixelCoordToViewDirWS = m_PixelCoordToViewDirWS;

            // Compute texel spacing at the depth of 1 meter.
            parameters.unitDepthTexelSpacing = HDUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, parameters.resolution.y);

            parameters.numVisibleVolumes = m_VisibleVolumeBounds.Count;
            parameters.volumeAtlas = DensityVolumeManager.manager.volumeAtlas.GetAtlas();
            parameters.volumeAtlasDimensions = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            if (parameters.volumeAtlas != null)
            {
                parameters.volumeAtlasDimensions.x = (float)parameters.volumeAtlas.width / parameters.volumeAtlas.depth; // 1 / number of textures
                parameters.volumeAtlasDimensions.y = parameters.volumeAtlas.width;
                parameters.volumeAtlasDimensions.z = parameters.volumeAtlas.depth;
                parameters.volumeAtlasDimensions.w = Mathf.Log(parameters.volumeAtlas.width, 2);              // Max LoD
            }
            else
            {
                parameters.volumeAtlas = CoreUtils.blackVolumeTexture;
            }

            return parameters;
        }

        static void VolumeVoxelizationPass( in VolumeVoxelizationParameters parameters,
                                            RTHandle                        densityBuffer,
                                            ComputeBuffer                   visibleVolumeBoundsBuffer,
                                            ComputeBuffer                   visibleVolumeDataBuffer,
                                            ComputeBuffer                   bigTileLightList,
                                            CommandBuffer                   cmd)
                {
            cmd.SetComputeIntParam(parameters.voxelizationCS, HDShaderIDs._NumTileBigTileX, parameters.numBigTileX);
            cmd.SetComputeIntParam(parameters.voxelizationCS, HDShaderIDs._NumTileBigTileY, parameters.numBigTileY);
            if (parameters.tiledLighting)
                cmd.SetComputeBufferParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs.g_vBigTileLightList, bigTileLightList);

            cmd.SetComputeTextureParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs._VBufferDensity, densityBuffer);
            cmd.SetComputeBufferParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs._VolumeBounds, visibleVolumeBoundsBuffer);
            cmd.SetComputeBufferParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs._VolumeData, visibleVolumeDataBuffer);
            cmd.SetComputeTextureParam(parameters.voxelizationCS, parameters.voxelizationKernel, HDShaderIDs._VolumeMaskAtlas, parameters.volumeAtlas);

                // TODO: set the constant buffer data only once.
            cmd.SetComputeMatrixArrayParam(parameters.voxelizationCS, HDShaderIDs._VBufferCoordToViewDirWS, parameters.pixelCoordToViewDirWS);
            cmd.SetComputeFloatParam(parameters.voxelizationCS, HDShaderIDs._VBufferUnitDepthTexelSpacing, parameters.unitDepthTexelSpacing);
            cmd.SetComputeIntParam(parameters.voxelizationCS, HDShaderIDs._NumVisibleDensityVolumes, parameters.numVisibleVolumes);
            cmd.SetComputeVectorParam(parameters.voxelizationCS, HDShaderIDs._VolumeMaskDimensions, parameters.volumeAtlasDimensions);

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(parameters.voxelizationCS, parameters.voxelizationKernel, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);
        }

        void VolumeVoxelizationPass(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumeVoxelization)))
            {
                var parameters = PrepareVolumeVoxelizationParameters(hdCamera);
                VolumeVoxelizationPass(parameters, m_DensityBufferHandle, m_VisibleVolumeBoundsBuffer, m_VisibleVolumeDataBuffer, m_TileAndClusterData.bigTileLightList, cmd);
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
            public ComputeShader    volumetricLightingCS;
            public int              volumetricLightingKernel;
            public int              volumetricFilteringKernelX;
            public int              volumetricFilteringKernelY;
            public bool             tiledLighting;
            public Vector4          resolution;
            public int              numBigTileX, numBigTileY;
            public float            unitDepthTexelSpacing;
            public float            anisotropy;
            public Vector4          xySeqOffset;
            public bool             enableReprojection;
            public bool             historyIsValid;
            public int              viewCount;
            public bool             filterVolume;

            public Matrix4x4[]      pixelCoordToViewDirWS;
        }

        VolumetricLightingParameters PrepareVolumetricLightingParameters(HDCamera hdCamera, int frameIndex)
        {
            var parameters = new VolumetricLightingParameters();

            // Get the interpolated anisotropy value.
            var fog = hdCamera.volumeStack.GetComponent<Fog>();

            // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
            parameters.tiledLighting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
            parameters.enableReprojection = hdCamera.IsVolumetricReprojectionEnabled();
            bool enableAnisotropy = fog.anisotropy.value != 0;
            bool highQuality = volumetricLightingPreset == VolumetricLightingPreset.High;

            parameters.volumetricLightingCS = m_VolumetricLightingCS;
            parameters.volumetricLightingKernel = (parameters.tiledLighting ? 1 : 0) | (parameters.enableReprojection ? 2 : 0) | (enableAnisotropy ? 4 : 0) | (highQuality ? 8 : 0);
            parameters.volumetricFilteringKernelX = 16;
            parameters.volumetricFilteringKernelY = 17;
            var currFrameParams = hdCamera.vBufferParams[0];
            var cvp = currFrameParams.viewportSize;

            parameters.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            var vFoV = hdCamera.camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            var gpuAspect = HDUtils.ProjectionMatrixAspect(hdCamera.mainViewConstants.projMatrix);

            // Compose the matrix which allows us to compute the world space view direction.
            hdCamera.GetPixelCoordToViewDirWS(parameters.resolution, gpuAspect, ref m_PixelCoordToViewDirWS);
            parameters.pixelCoordToViewDirWS = m_PixelCoordToViewDirWS;

            // Compute texel spacing at the depth of 1 meter.
            parameters.unitDepthTexelSpacing = HDUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, parameters.resolution.y);

            parameters.anisotropy = fog.anisotropy.value;
            parameters.historyIsValid = hdCamera.volumetricHistoryIsValid;
            parameters.viewCount = hdCamera.viewCount;
            parameters.numBigTileX = GetNumTileBigTileX(hdCamera);
            parameters.numBigTileY = GetNumTileBigTileY(hdCamera);
            parameters.filterVolume = fog.filter.value;

            GetHexagonalClosePackedSpheres7(m_xySeq);
            int sampleIndex = frameIndex % 7;
            // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
            // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
            parameters.xySeqOffset.Set(m_xySeq[sampleIndex].x, m_xySeq[sampleIndex].y, m_zSeq[sampleIndex], frameIndex);

            return parameters;
        }

        static void VolumetricLightingPass( in VolumetricLightingParameters parameters,
                                            RTHandle                        densityBuffer,
                                            RTHandle                        lightingBuffer,
                                            RTHandle                        historyRT,
                                            RTHandle                        feedbackRT,
                                            ComputeBuffer                   bigTileLightList,
                                            CommandBuffer                   cmd)
        {
            cmd.SetComputeIntParam(parameters.volumetricLightingCS, HDShaderIDs._NumTileBigTileX, parameters.numBigTileX);
            cmd.SetComputeIntParam(parameters.volumetricLightingCS, HDShaderIDs._NumTileBigTileY, parameters.numBigTileY);
            if (parameters.tiledLighting)
                cmd.SetComputeBufferParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs.g_vBigTileLightList, bigTileLightList);

                // TODO: set 'm_VolumetricLightingPreset'.
                // TODO: set the constant buffer data only once.
            cmd.SetComputeMatrixArrayParam(parameters.volumetricLightingCS, HDShaderIDs._VBufferCoordToViewDirWS, parameters.pixelCoordToViewDirWS);
            cmd.SetComputeFloatParam(parameters.volumetricLightingCS, HDShaderIDs._VBufferUnitDepthTexelSpacing, parameters.unitDepthTexelSpacing);
            cmd.SetComputeFloatParam(parameters.volumetricLightingCS, HDShaderIDs._CornetteShanksConstant, CornetteShanksPhasePartConstant(parameters.anisotropy));
            cmd.SetComputeVectorParam(parameters.volumetricLightingCS, HDShaderIDs._VBufferSampleOffset, parameters.xySeqOffset);
            cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferDensity, densityBuffer);  // Read
            cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferLightingIntegral, lightingBuffer); // Write

            // We set this even when not re-projecting to make sure it stays in a safe state when we switch camera
            cmd.SetComputeIntParam(parameters.volumetricLightingCS, HDShaderIDs._VBufferLightingHistoryIsValid, parameters.historyIsValid ? 1 : 0);

            if (parameters.enableReprojection)
            {
                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferLightingHistory, historyRT);  // Read
                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, HDShaderIDs._VBufferLightingFeedback, feedbackRT); // Write
            }

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(parameters.volumetricLightingCS, parameters.volumetricLightingKernel, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);
        }

        static void FilterVolumetricLighting(in VolumetricLightingParameters parameters, RTHandle outputBuffer, RTHandle inputBuffer, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricLightingFiltering)))
            {
                // The shader defines GROUP_SIZE_1D = 8.
                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricFilteringKernelX, HDShaderIDs._VBufferLightingFeedback, inputBuffer);  // Read
                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricFilteringKernelX, HDShaderIDs._VBufferLightingIntegral, outputBuffer); // Write
                cmd.DispatchCompute(parameters.volumetricLightingCS, parameters.volumetricFilteringKernelX, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);

                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricFilteringKernelY, HDShaderIDs._VBufferLightingFeedback, outputBuffer);  // Read
                cmd.SetComputeTextureParam(parameters.volumetricLightingCS, parameters.volumetricFilteringKernelY, HDShaderIDs._VBufferLightingIntegral, inputBuffer); // Write
                cmd.DispatchCompute(parameters.volumetricLightingCS, parameters.volumetricFilteringKernelY, ((int)parameters.resolution.x + 7) / 8, ((int)parameters.resolution.y + 7) / 8, parameters.viewCount);
            }
        }

        void VolumetricLightingPass(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            var parameters = PrepareVolumetricLightingParameters(hdCamera, frameIndex);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricLighting)))
            {
                // It is safe to request these RTs even if they have not been allocated.
                // The system will return NULL in that case.
                RTHandle historyRT  = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting);
                RTHandle feedbackRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting);

                VolumetricLightingPass(parameters, m_DensityBufferHandle, m_LightingBufferHandle, historyRT, feedbackRT, m_TileAndClusterData.bigTileLightList, cmd);

                if (parameters.enableReprojection)
                    hdCamera.volumetricHistoryIsValid = true; // For the next frame...
            }

            // Let's filter out volumetric buffer
            if (parameters.filterVolume)
                FilterVolumetricLighting(parameters, m_DensityBufferHandle, m_LightingBufferHandle, cmd);
        }
    } // class VolumetricLightingModule
} // namespace UnityEngine.Rendering.HighDefinition
