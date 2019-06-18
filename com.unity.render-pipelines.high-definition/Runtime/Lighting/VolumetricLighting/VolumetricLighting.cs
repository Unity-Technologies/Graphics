using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Optimized version of 'DensityVolumeArtistParameters'.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    public struct DensityVolumeEngineData
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

    public class VolumeRenderingUtils
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

    public struct DensityVolumeList
    {
        public List<OrientedBBox>      bounds;
        public List<DensityVolumeEngineData> density;
    }

        public enum VolumetricLightingPreset
        {
            Off,
            Medium,
            High,
            Count
    }

        public struct VBufferParameters
        {
            public Vector3Int viewportSize;
            public Vector4    depthEncodingParams;
            public Vector4    depthDecodingParams;

            public VBufferParameters(Vector3Int viewportResolution, float depthExtent, float camNear, float camFar, float camVFoV, float sliceDistributionUniformity)
            {
                viewportSize = viewportResolution;

                // The V-Buffer is sphere-capped, while the camera frustum is not.
                // We always start from the near plane of the camera.

                float aspectRatio    = viewportResolution.x / (float)viewportResolution.y;
                float farPlaneHeight = 2.0f * Mathf.Tan(0.5f * camVFoV) * camFar;
                float farPlaneWidth  = farPlaneHeight * aspectRatio;
                float farPlaneMaxDim = Mathf.Max(farPlaneWidth, farPlaneHeight);
                float farPlaneDist   = Mathf.Sqrt(camFar * camFar + 0.25f * farPlaneMaxDim * farPlaneMaxDim);

                float nearDist = camNear;
                float farDist  = Math.Min(nearDist + depthExtent, farPlaneDist);

                float c = 2 - 2 * sliceDistributionUniformity; // remap [0, 1] -> [2, 0]
                      c = Mathf.Max(c, 0.001f);                // Avoid NaNs

                depthEncodingParams = ComputeLogarithmicDepthEncodingParams(nearDist, farDist, c);
                depthDecodingParams = ComputeLogarithmicDepthDecodingParams(nearDist, farDist, c);
            }

            public Vector4 ComputeUvScaleAndLimit(Vector2Int bufferSize)
            {
                // The slice count is fixed for now.
                return HDUtils.ComputeUvScaleAndLimit(new Vector2Int(viewportSize.x, viewportSize.y), bufferSize);
            }

            public float ComputeLastSliceDistance()
            {
                float d   = 1.0f - 0.5f / viewportSize.z;
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
        public VolumetricLightingPreset preset = VolumetricLightingPreset.Off;

        static ComputeShader          m_VolumeVoxelizationCS      = null;
        static ComputeShader          m_VolumetricLightingCS      = null;

        List<OrientedBBox>            m_VisibleVolumeBounds       = null;
        List<DensityVolumeEngineData> m_VisibleVolumeData         = null;
        public const int              k_MaxVisibleVolumeCount     = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        static ComputeBuffer          s_VisibleVolumeBoundsBuffer = null;
        static ComputeBuffer          s_VisibleVolumeDataBuffer   = null;

        // These two buffers do not depend on the frameID and are therefore shared by all views.
        RTHandleSystem.RTHandle       m_DensityBufferHandle;
        RTHandleSystem.RTHandle       m_LightingBufferHandle;

        // Is the feature globally disabled?
        bool m_SupportVolumetrics = false;

        Vector4[] m_PackedCoeffs;
        ZonalHarmonicsL2 m_PhaseZH;
        Vector2[] m_xySeq;
        Vector4 m_xySeqOffset;
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

        public void InitializeVolumetricLighting()
        {
            m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return;

            preset = asset.currentPlatformRenderPipelineSettings.increaseResolutionOfVolumetrics
                ? VolumetricLightingPreset.High
                : VolumetricLightingPreset.Medium;

            m_VolumeVoxelizationCS = asset.renderPipelineResources.shaders.volumeVoxelizationCS;
            m_VolumetricLightingCS = asset.renderPipelineResources.shaders.volumetricLightingCS;

            m_PackedCoeffs = new Vector4[7];
            m_PhaseZH = new ZonalHarmonicsL2();
            m_PhaseZH.coeffs = new float[3];

            m_xySeq = new Vector2[7];
            m_xySeqOffset = new Vector4();

            m_PixelCoordToViewDirWS = new Matrix4x4[TextureXR.kMaxSlices];

            CreateVolumetricLightingBuffers();
        }

        // RTHandleSystem API expects a function which computes the resolution. We define it here.
        Vector2Int ComputeVBufferResolutionXY(Vector2Int screenSize)
        {
            Vector3Int resolution = ComputeVBufferResolution(preset, screenSize.x, screenSize.y);

            return new Vector2Int(resolution.x, resolution.y);
        }

        // RTHandleSystem API expects a function which computes the resolution. We define it here.
        Vector2Int ComputeHistoryVBufferResolutionXY(Vector2Int screenSize)
        {
            Vector2Int resolution = ComputeVBufferResolutionXY(screenSize);

            // Since the buffers owned by the VolumetricLightingSystem may have different lifetimes compared
            // to those owned by the HDCamera, we need to make sure that the buffer resolution is the same
            // (in order to share the UV scale and the UV limit).
            if (m_LightingBufferHandle != null)
            {
                resolution.x = Math.Max(resolution.x, m_LightingBufferHandle.rt.width);
                resolution.y = Math.Max(resolution.y, m_LightingBufferHandle.rt.height);
            }

            return resolution;
        }

        // BufferedRTHandleSystem API expects an allocator function. We define it here.
        RTHandleSystem.RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            frameIndex &= 1;

            int d = ComputeVBufferSliceCount(preset);

            // With stereo instancing, the VBuffer is doubled and split into 2 compartments for each eye
            if (TextureXR.useTexArray && XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassInstanced)
                d = d * 2;

            return rtHandleSystem.Alloc(scaleFunc:         ComputeHistoryVBufferResolutionXY,
                slices:            d,
                dimension:         TextureDimension.Tex3D,
                colorFormat:       GraphicsFormat.R16G16B16A16_SFloat,
                enableRandomWrite: true,
                enableMSAA:        false,
                /* useDynamicScale: true, // <- TODO */
                name: string.Format("{0}_VBufferHistory{1}", viewName, frameIndex)
                );
        }

        void CreateVolumetricLightingBuffers()
        {
            Debug.Assert(m_VolumetricLightingCS != null);

            m_VisibleVolumeBounds       = new List<OrientedBBox>();
            m_VisibleVolumeData         = new List<DensityVolumeEngineData>();
            s_VisibleVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleVolumeDataBuffer   = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(DensityVolumeEngineData)));

            int d = ComputeVBufferSliceCount(preset);

            // With stereo instancing, the VBuffer is doubled and split into 2 compartments for each eye
            if (TextureXR.useTexArray && XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassInstanced)
                d = d * 2;

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
            Vector3Int viewportResolution = ComputeVBufferResolution(preset, hdCamera.actualWidth, hdCamera.actualHeight);

            var controller = VolumeManager.instance.stack.GetComponent<VolumetricLightingController>();

            return new VBufferParameters(viewportResolution, controller.depthExtent.value,
                                         hdCamera.camera.nearClipPlane,
                                         hdCamera.camera.farClipPlane,
                                         hdCamera.camera.fieldOfView,
                                         controller.sliceDistributionUniformity.value);
        }

        public void InitializeVolumetricLightingPerCameraData(HDCamera hdCamera, int bufferCount)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics))
                return;

            // Start with the same parameters for both frames. Then update them one by one every frame.
            var parameters            = ComputeVBufferParameters(hdCamera);
            hdCamera.vBufferParams    = new VBufferParameters[2];
            hdCamera.vBufferParams[0] = parameters;
            hdCamera.vBufferParams[1] = parameters;

            hdCamera.volumetricHistoryIsValid = false;

            if (bufferCount != 0)
            {
                hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting, HistoryBufferAllocatorFunction, bufferCount);
            }
        }

        public void DeinitializeVolumetricLightingPerCameraData(HDCamera hdCamera)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics))
                return;

            hdCamera.vBufferParams = null;

            hdCamera.volumetricHistoryIsValid = false;

            // Cannot free the history buffer from within this function.
        }

        // This function relies on being called once per camera per frame.
        // The results are undefined otherwise.
        public void UpdateVolumetricLightingPerCameraData(HDCamera hdCamera)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics))
                return;

            var parameters = ComputeVBufferParameters(hdCamera);

            // Double-buffer. I assume the cost of copying is negligible (don't want to use the frame index).
            hdCamera.vBufferParams[1] = hdCamera.vBufferParams[0];
            hdCamera.vBufferParams[0] = parameters;

            // Note: resizing of history buffer is automatic (handled by the BufferedRTHandleSystem).
        }

        void DestroyVolumetricLightingBuffers()
        {
            if (m_DensityBufferHandle != null)
                RTHandles.Release(m_DensityBufferHandle);
            if (m_LightingBufferHandle != null)
                RTHandles.Release(m_LightingBufferHandle);

            CoreUtils.SafeRelease(s_VisibleVolumeBoundsBuffer);
            CoreUtils.SafeRelease(s_VisibleVolumeDataBuffer);

            m_VisibleVolumeBounds = null;
            m_VisibleVolumeData   = null;
        }

        public void CleanupVolumetricLighting()
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
            switch (preset)
            {
                case VolumetricLightingPreset.Medium:
                    return 64;
                case VolumetricLightingPreset.High:
                    return 128;
                case VolumetricLightingPreset.Off:
                    return 0;
                default:
                    Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                    return 0;
            }
        }

        static Vector3Int ComputeVBufferResolution(VolumetricLightingPreset preset, int screenWidth, int screenHeight)
        {
            int t = ComputeVBufferTileSize(preset);

            int w = HDUtils.DivRoundUp(screenWidth,  t);
            int h = HDUtils.DivRoundUp(screenHeight, t);
            int d = ComputeVBufferSliceCount(preset);

            return new Vector3Int(w, h, d);
        }

        void SetPreconvolvedAmbientLightProbe(CommandBuffer cmd, float dimmer, float anisotropy)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(RenderSettings.ambientProbe);
                                 probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, dimmer);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZH, anisotropy);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZH));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffs, finalSH);
            cmd.SetGlobalVectorArray(HDShaderIDs._AmbientProbeCoeffs, m_PackedCoeffs);
        }

        float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (1.0f / (4.0f * Mathf.PI)) * 1.5f * (1.0f - g * g) / (2.0f + g * g);
        }

        public void PushVolumetricLightingGlobalParams(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();

            // VisualEnvironment sets global fog parameters

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics) || visualEnvironment.fogType.value != FogType.Volumetric)
            {
                cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, HDUtils.clearTexture3D);
                return;
            }

            // Get the interpolated anisotropy value.
            var fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();

            SetPreconvolvedAmbientLightProbe(cmd, fog.globalLightProbeDimmer.value, fog.anisotropy.value);

            var currFrameParams = hdCamera.vBufferParams[0];
            var prevFrameParams = hdCamera.vBufferParams[1];

            // All buffers are of the same size, and are resized at the same time, at the beginning of the frame.
            Vector2Int bufferSize = new Vector2Int(m_LightingBufferHandle.rt.width, m_LightingBufferHandle.rt.height);

            var cvp = currFrameParams.viewportSize;
            var pvp = prevFrameParams.viewportSize;

            cmd.SetGlobalVector(HDShaderIDs._VBufferResolution,              new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y));
            cmd.SetGlobalInt(   HDShaderIDs._VBufferSliceCount,              cvp.z);
            cmd.SetGlobalFloat( HDShaderIDs._VBufferRcpSliceCount,           1.0f / cvp.z);
            cmd.SetGlobalVector(HDShaderIDs._VBufferUvScaleAndLimit,         currFrameParams.ComputeUvScaleAndLimit(bufferSize));
            cmd.SetGlobalVector(HDShaderIDs._VBufferDistanceEncodingParams,  currFrameParams.depthEncodingParams);
            cmd.SetGlobalVector(HDShaderIDs._VBufferDistanceDecodingParams,  currFrameParams.depthDecodingParams);
            cmd.SetGlobalFloat( HDShaderIDs._VBufferLastSliceDist,           currFrameParams.ComputeLastSliceDistance());
            cmd.SetGlobalFloat( HDShaderIDs._VBufferRcpInstancedViewCount,   1.0f / hdCamera.viewCount);

            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevResolution,          new Vector4(pvp.x, pvp.y, 1.0f / pvp.x, 1.0f / pvp.y));
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevUvScaleAndLimit,     prevFrameParams.ComputeUvScaleAndLimit(bufferSize));
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevDepthEncodingParams, prevFrameParams.depthEncodingParams);
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevDepthDecodingParams, prevFrameParams.depthDecodingParams);

            cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,               m_LightingBufferHandle);
        }

        public DensityVolumeList PrepareVisibleDensityVolumeList(HDCamera hdCamera, CommandBuffer cmd, float time)
        {
            DensityVolumeList densityVolumes = new DensityVolumeList();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics))
                return densityVolumes;

            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            if (visualEnvironment.fogType.value != FogType.Volumetric)
                return densityVolumes;

            using (new ProfilingSample(cmd, "Prepare Visible Density Volume List"))
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
                DensityVolume[] volumes = DensityVolumeManager.manager.PrepareDensityVolumeData(cmd, hdCamera.camera, time);

                for (int i = 0; i < Math.Min(volumes.Length, k_MaxVisibleVolumeCount); i++)
                {
                    DensityVolume volume = volumes[i];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    // XRTODO: fix combined frustum culling
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8) || hdCamera.xr.instancingEnabled)
                    {
                        // TODO: cache these?
                        var data = volume.parameters.ConvertToEngineData();

                        m_VisibleVolumeBounds.Add(obb);
                        m_VisibleVolumeData.Add(data);
                    }
                }

                s_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
                s_VisibleVolumeDataBuffer.SetData(m_VisibleVolumeData);

                // Fill the struct with pointers in order to share the data with the light loop.
                densityVolumes.bounds  = m_VisibleVolumeBounds;
                densityVolumes.density = m_VisibleVolumeData;

                return densityVolumes;
            }
        }

        public void VolumeVoxelizationPass(HDCamera hdCamera, CommandBuffer cmd, int frameIndex, DensityVolumeList densityVolumes)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics))
                return;

            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            if (visualEnvironment.fogType.value != FogType.Volumetric)
                return;

            using (new ProfilingSample(cmd, "Volume Voxelization"))
            {
                int  numVisibleVolumes = m_VisibleVolumeBounds.Count;
                bool tiledLighting     = HasLightToCull() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
                bool highQuality       = preset == VolumetricLightingPreset.High;

                int kernel = (tiledLighting ? 1 : 0) | (highQuality ? 2 : 0);

                var currFrameParams = hdCamera.vBufferParams[0];
                var cvp = currFrameParams.viewportSize;

                Vector4 resolution  = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
#if UNITY_2019_1_OR_NEWER
                var vFoV        = hdCamera.camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
#else
                var vFoV        = hdCamera.camera.fieldOfView * Mathf.Deg2Rad;
#endif
                // Compose the matrix which allows us to compute the world space view direction.
                hdCamera.GetPixelCoordToViewDirWS(resolution, ref m_PixelCoordToViewDirWS);

                // Compute texel spacing at the depth of 1 meter.
                float unitDepthTexelSpacing = HDUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, resolution.y);

                Texture3D volumeAtlas = DensityVolumeManager.manager.volumeAtlas.GetAtlas();
                Vector4 volumeAtlasDimensions = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                if (volumeAtlas != null)
                {
                    volumeAtlasDimensions.x = (float)volumeAtlas.width / volumeAtlas.depth; // 1 / number of textures
                    volumeAtlasDimensions.y = volumeAtlas.width;
                    volumeAtlasDimensions.z = volumeAtlas.depth;
                    volumeAtlasDimensions.w = Mathf.Log(volumeAtlas.width, 2);              // Max LoD
                }
                else
                {
                    volumeAtlas = CoreUtils.blackVolumeTexture;
                }

                if(hdCamera.frameSettings.VolumeVoxelizationRunsAsync())
                {
                    // We explicitly set the big tile info even though it is set globally, since this could be running async before the PushGlobalParams
                    cmd.SetComputeIntParam(m_VolumeVoxelizationCS, HDShaderIDs._NumTileBigTileX, GetNumTileBigTileX(hdCamera));
                    cmd.SetComputeIntParam(m_VolumeVoxelizationCS, HDShaderIDs._NumTileBigTileY, GetNumTileBigTileY(hdCamera));
                    if (tiledLighting)
                        cmd.SetComputeBufferParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs.g_vBigTileLightList, m_TileAndClusterData.bigTileLightList);
                }

                cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VBufferDensity,  m_DensityBufferHandle);
                cmd.SetComputeBufferParam( m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeBounds,    s_VisibleVolumeBoundsBuffer);
                cmd.SetComputeBufferParam( m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeData,      s_VisibleVolumeDataBuffer);
                cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeMaskAtlas, volumeAtlas);

                // TODO: set the constant buffer data only once.
                cmd.SetComputeMatrixArrayParam(m_VolumeVoxelizationCS, HDShaderIDs._VBufferCoordToViewDirWS,      m_PixelCoordToViewDirWS);
                cmd.SetComputeFloatParam( m_VolumeVoxelizationCS, HDShaderIDs._VBufferUnitDepthTexelSpacing, unitDepthTexelSpacing);
                cmd.SetComputeIntParam(   m_VolumeVoxelizationCS, HDShaderIDs._NumVisibleDensityVolumes,     numVisibleVolumes);
                cmd.SetComputeVectorParam(m_VolumeVoxelizationCS, HDShaderIDs._VolumeMaskDimensions,         volumeAtlasDimensions);

                int w = (int)resolution.x;
                int h = (int)resolution.y;

                // The shader defines GROUP_SIZE_1D = 8.
                cmd.DispatchCompute(m_VolumeVoxelizationCS, kernel, (w + 7) / 8, (h + 7) / 8, hdCamera.viewCount);
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

        public void VolumetricLightingPass(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics))
                return;

            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            if (visualEnvironment.fogType.value != FogType.Volumetric)
                return;

            using (new ProfilingSample(cmd, "Volumetric Lighting"))
            {
                // Get the interpolated anisotropy value.
                var fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();

                // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
                bool tiledLighting      = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
                bool enableReprojection = Application.isPlaying && hdCamera.camera.cameraType == CameraType.Game &&
                                          hdCamera.frameSettings.IsEnabled(FrameSettingsField.ReprojectionForVolumetrics);
                bool enableAnisotropy   = fog.anisotropy.value != 0;
                bool highQuality        = preset == VolumetricLightingPreset.High;

                int kernel = (tiledLighting ? 1 : 0) | (enableReprojection ? 2 : 0) | (enableAnisotropy ? 4 : 0) | (highQuality ? 8 : 0);

                var currFrameParams = hdCamera.vBufferParams[0];
                var cvp = currFrameParams.viewportSize;

                Vector4 resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
#if UNITY_2019_1_OR_NEWER
                var vFoV = hdCamera.camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
#else
                var vFoV        = hdCamera.camera.fieldOfView * Mathf.Deg2Rad;
#endif
                // Compose the matrix which allows us to compute the world space view direction.
                hdCamera.GetPixelCoordToViewDirWS(resolution, ref m_PixelCoordToViewDirWS);

                // Compute texel spacing at the depth of 1 meter.
                float unitDepthTexelSpacing = HDUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, resolution.y);

                GetHexagonalClosePackedSpheres7(m_xySeq);

                int sampleIndex = frameIndex % 7;

                // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
                // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
                m_xySeqOffset.Set(m_xySeq[sampleIndex].x, m_xySeq[sampleIndex].y, m_zSeq[sampleIndex], frameIndex);


                // TODO: set 'm_VolumetricLightingPreset'.
                // TODO: set the constant buffer data only once.
                cmd.SetComputeMatrixArrayParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferCoordToViewDirWS,      m_PixelCoordToViewDirWS);
                cmd.SetComputeFloatParam(  m_VolumetricLightingCS,         HDShaderIDs._VBufferUnitDepthTexelSpacing, unitDepthTexelSpacing);
                cmd.SetComputeFloatParam(  m_VolumetricLightingCS,         HDShaderIDs._CornetteShanksConstant,       CornetteShanksPhasePartConstant(fog.anisotropy.value));
                cmd.SetComputeVectorParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferSampleOffset,          m_xySeqOffset);
                cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferDensity,               m_DensityBufferHandle);  // Read
                cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingIntegral,      m_LightingBufferHandle); // Write

                if (enableReprojection)
                {
                    var historyRT  = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting);
                    var feedbackRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting);

                    cmd.SetComputeIntParam(    m_VolumetricLightingCS,         HDShaderIDs._VBufferLightingHistoryIsValid, hdCamera.volumetricHistoryIsValid ? 1 : 0);
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingHistory,        historyRT);  // Read
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingFeedback,       feedbackRT); // Write

                    hdCamera.volumetricHistoryIsValid = true; // For the next frame...
                }

                int w = (int)resolution.x;
                int h = (int)resolution.y;

                // The shader defines GROUP_SIZE_1D = 8.
                cmd.DispatchCompute(m_VolumetricLightingCS, kernel, (w + 7) / 8, (h + 7) / 8, hdCamera.viewCount);
            }
        }
    } // class VolumetricLightingModule
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
