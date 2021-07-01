using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'ProbeVolumeArtistParameters'.
    // Currently 144-bytes.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    internal struct ProbeVolumeEngineData
    {
        public Vector3 debugColor;
        public float weight;
        public Vector3 rcpPosFaceFade;
        public float rcpDistFadeLen;
        public Vector3 rcpNegFaceFade;
        public float endTimesRcpDistFadeLen;
        public Vector3 scale;
        public float padding0;
        public Vector3 bias;
        public int volumeBlendMode;
        public Vector4 octahedralDepthScaleBias;
        public Vector3 resolution;
        public uint lightLayers;
        public Vector3 resolutionInverse;
        public float normalBiasWS;
        public float viewBiasWS;
        public Vector3 padding1;

        public static ProbeVolumeEngineData GetNeutralValues()
        {
            ProbeVolumeEngineData data;

            data.debugColor = Vector3.zero;
            data.weight = 0.0f;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpDistFadeLen = 0;
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.endTimesRcpDistFadeLen = 1;
            data.scale = Vector3.zero;
            data.padding0 = 0.0f;
            data.bias = Vector3.zero;
            data.volumeBlendMode = 0;
            data.octahedralDepthScaleBias = Vector4.zero;
            data.resolution = Vector3.zero;
            data.lightLayers = 0;
            data.resolutionInverse = Vector3.zero;
            data.normalBiasWS = 0.0f;
            data.viewBiasWS = 0.0f;
            data.padding1 = Vector3.zero;

            return data;
        }
    }

    [GenerateHLSL]
    internal enum LeakMitigationMode
    {
        NormalBias = 0,
        GeometricFilter,
        ProbeValidityFilter,
        OctahedralDepthOcclusionFilter
    }

    struct ProbeVolumeList
    {
        public List<OrientedBBox> bounds;
        public List<ProbeVolumeEngineData> data;
    }

    public partial class HDRenderPipeline
    {
        List<OrientedBBox> m_VisibleProbeVolumeBounds = null;
        List<ProbeVolumeEngineData> m_VisibleProbeVolumeData = null;
        internal const int k_MaxVisibleProbeVolumeCount = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        static ComputeBuffer s_VisibleProbeVolumeBoundsBuffer = null;
        static ComputeBuffer s_VisibleProbeVolumeDataBuffer = null;
        static ComputeBuffer s_VisibleProbeVolumeBoundsBufferDefault = null;
        static ComputeBuffer s_VisibleProbeVolumeDataBufferDefault = null;

        // Is the feature globally disabled?
        bool m_SupportProbeVolume = false;

        // Pre-allocate sort keys array to max size to avoid creating allocations / garbage at runtime.
        uint[] m_ProbeVolumeSortKeys = new uint[k_MaxVisibleProbeVolumeCount];

        static ComputeShader s_ProbeVolumeAtlasBlitCS = null;
        static ComputeShader s_ProbeVolumeAtlasOctahedralDepthBlitCS = null;
        static ComputeShader s_ProbeVolumeAtlasOctahedralDepthConvolveCS = null;
        static int s_ProbeVolumeAtlasBlitKernel = -1;
        static int s_ProbeVolumeAtlasOctahedralDepthBlitKernel = -1;
        static int s_ProbeVolumeAtlasOctahedralDepthConvolveKernel = -1;
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataSHL01Buffer = null;
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataSHL2Buffer = null;
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataValidityBuffer = null;
        static ComputeBuffer s_ProbeVolumeAtlasOctahedralDepthBuffer = null;
        static int s_ProbeVolumeAtlasResolution;
        static int s_ProbeVolumeAtlasOctahedralDepthResolution;
        static int k_MaxProbeVolumeAtlasOctahedralDepthProbeCount;
        internal const int k_ProbeOctahedralDepthWidth = 8;
        internal const int k_ProbeOctahedralDepthHeight = 8;
        internal const UnityEngine.Experimental.Rendering.GraphicsFormat k_ProbeVolumeAtlasFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
        internal const UnityEngine.Experimental.Rendering.GraphicsFormat k_ProbeVolumeOctahedralDepthAtlasFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat; // float2(mean, variance)

        static int s_MaxProbeVolumeProbeCount;
        static int s_MaxProbeVolumeProbeOctahedralDepthCount;
        RTHandle m_ProbeVolumeAtlasSHRTHandle;

        int m_ProbeVolumeAtlasSHRTDepthSliceCount;
        Texture3DAtlasDynamic<ProbeVolume.ProbeVolumeAtlasKey> probeVolumeAtlas = null;

        RTHandle m_ProbeVolumeAtlasOctahedralDepthRTHandle;
        Texture2DAtlasDynamic probeVolumeAtlasOctahedralDepth = null;
        bool isClearProbeVolumeAtlasRequested = false;

        // Preallocated scratch memory for storing ambient probe packed SH coefficients, which are used as a fallback when probe volume weight < 1.0.
        static Vector4[] s_AmbientProbeFallbackPackedCoeffs = new Vector4[7];

        void InitializeProbeVolumes()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            m_SupportProbeVolume = asset.currentPlatformRenderPipelineSettings.supportProbeVolume && (ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled);

            s_ProbeVolumeAtlasResolution = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasResolution;
            if (GetApproxProbeVolumeAtlasSizeInByte(s_ProbeVolumeAtlasResolution) > HDRenderPipeline.k_MaxCacheSize)
            {
                s_ProbeVolumeAtlasResolution = GetMaxProbeVolumeAtlasSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize);
            }

            // TODO: Preallocating compute buffer for this worst case of a single probe volume that consumes the whole atlas is a memory hog.
            // May want to look at dynamic resizing of compute buffer based on use, or more simply, slicing it up across multiple dispatches for massive volumes.
            s_MaxProbeVolumeProbeCount = s_ProbeVolumeAtlasResolution * s_ProbeVolumeAtlasResolution * s_ProbeVolumeAtlasResolution;
            s_MaxProbeVolumeProbeOctahedralDepthCount = s_MaxProbeVolumeProbeCount * k_ProbeOctahedralDepthWidth * k_ProbeOctahedralDepthHeight;

            s_ProbeVolumeAtlasOctahedralDepthResolution = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthResolution;
            if (GetApproxProbeVolumeOctahedralDepthAtlasSizeInByte(s_ProbeVolumeAtlasOctahedralDepthResolution) > HDRenderPipeline.k_MaxCacheSize)
            {
                s_ProbeVolumeAtlasOctahedralDepthResolution = GetMaxProbeVolumeOctahedralDepthAtlasSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize);
            }

            k_MaxProbeVolumeAtlasOctahedralDepthProbeCount = (s_ProbeVolumeAtlasOctahedralDepthResolution / k_ProbeOctahedralDepthWidth) * (s_ProbeVolumeAtlasOctahedralDepthResolution / k_ProbeOctahedralDepthWidth);

            if (m_SupportProbeVolume)
            {
                CreateProbeVolumeBuffers();

                s_ProbeVolumeAtlasBlitCS = asset.renderPipelineResources.shaders.probeVolumeAtlasBlitCS;
                s_ProbeVolumeAtlasBlitKernel = s_ProbeVolumeAtlasBlitCS.FindKernel("ProbeVolumeAtlasBlitKernel");

                s_ProbeVolumeAtlasOctahedralDepthBlitCS = asset.renderPipelineResources.shaders.probeVolumeAtlasOctahedralDepthBlitCS;
                s_ProbeVolumeAtlasOctahedralDepthBlitKernel = s_ProbeVolumeAtlasOctahedralDepthBlitCS.FindKernel("ProbeVolumeAtlasOctahedralDepthBlitKernel");
                s_ProbeVolumeAtlasOctahedralDepthConvolveCS = asset.renderPipelineResources.shaders.probeVolumeAtlasOctahedralDepthConvolveCS;
                s_ProbeVolumeAtlasOctahedralDepthConvolveKernel = s_ProbeVolumeAtlasOctahedralDepthConvolveCS.FindKernel("ProbeVolumeAtlasOctahedralDepthConvolveKernel");
            }

            // Need Default / Fallback buffers for binding in case when ShaderConfig has activated probe volume code,
            // and probe volumes has been enabled in the HDRenderPipelineAsset,
            // but probe volumes is disabled in the current camera's frame settings.
            // This can go away if we add a global keyword for using / completely stripping probe volume code per camera.
            CreateProbeVolumeBuffersDefault();

        #if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared += OnLightingDataCleared;
        #endif
        }

        internal void CreateProbeVolumeBuffersDefault()
        {
            s_VisibleProbeVolumeBoundsBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleProbeVolumeDataBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(ProbeVolumeEngineData)));
        }

        static internal int GetDepthSliceCountFromEncodingMode(ProbeVolumesEncodingModes encodingMode)
        {
            switch (encodingMode)
            {
                case ProbeVolumesEncodingModes.SphericalHarmonicsL0:
                {
                    // One "texture slice" for our single RGB SH DC term. Validity is placed in the alpha channel.
                    return 1;
                }

                case ProbeVolumesEncodingModes.SphericalHarmonicsL1:
                {
                    // One "texture slice" per [R, G, and B] SH 4x float coefficients + one "texture slice" for float4(validity, unassigned, unassigned, unassigned).
                    return 4;
                }

                case ProbeVolumesEncodingModes.SphericalHarmonicsL2:
                {
                    // One "texture slice" per 4x float coefficients, with the Validity term placed in the alpha channel of the last slice.
                    return 7;
                }

                default:
                {
                    Debug.Assert(false, "Error: Encountered unsupported probe volumes encoding mode in ShaderConfig.cs. Please set a valid enum value for ShaderOptions.ProbeVolumesEncodingMode.");
                    return 0;
                }
            }
        }

        // Used for displaying memory cost in HDRenderPipelineAsset UI.
        internal static long GetApproxProbeVolumeAtlasSizeInByte(int resolution)
        {
            int depthSliceCount = GetDepthSliceCountFromEncodingMode(ShaderConfig.s_ProbeVolumesEncodingMode);
            return (long)(resolution * resolution * resolution * depthSliceCount) * (long)HDUtils.GetFormatSizeInBytes(k_ProbeVolumeAtlasFormat);
        }

        internal static int GetMaxProbeVolumeAtlasSizeForWeightInByte(long weight)
        {
            int depthSliceCount = GetDepthSliceCountFromEncodingMode(ShaderConfig.s_ProbeVolumesEncodingMode);
            int theoricalResult = Mathf.FloorToInt(Mathf.Pow(weight / ((long)depthSliceCount * (long)HDUtils.GetFormatSizeInBytes(k_ProbeVolumeAtlasFormat)), 1.0f / 3.0f));
            return Mathf.Clamp(theoricalResult, 1, SystemInfo.maxTextureSize);
        }

        internal static long GetApproxProbeVolumeOctahedralDepthAtlasSizeInByte(int resolution)
        {
            return (long)(resolution * resolution) * (long)HDUtils.GetFormatSizeInBytes(k_ProbeVolumeOctahedralDepthAtlasFormat);
        }

        internal static int GetMaxProbeVolumeOctahedralDepthAtlasSizeForWeightInByte(long weight)
        {
            int theoricalResult = Mathf.FloorToInt(Mathf.Pow(weight / (long)HDUtils.GetFormatSizeInBytes(k_ProbeVolumeAtlasFormat), 1.0f / 2.0f));
            return Mathf.Clamp(theoricalResult, 1, SystemInfo.maxTextureSize);
        }

        internal void CreateProbeVolumeBuffers()
        {
            m_VisibleProbeVolumeBounds = new List<OrientedBBox>(32);
            m_VisibleProbeVolumeData = new List<ProbeVolumeEngineData>(32);
            s_VisibleProbeVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleProbeVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleProbeVolumeDataBuffer = new ComputeBuffer(k_MaxVisibleProbeVolumeCount, Marshal.SizeOf(typeof(ProbeVolumeEngineData)));
            s_ProbeVolumeAtlasBlitDataSHL01Buffer = new ComputeBuffer(s_MaxProbeVolumeProbeCount * ProbeVolumePayload.GetDataSHL01Stride(), Marshal.SizeOf(typeof(float)));
            if (ShaderConfig.s_ProbeVolumesEncodingMode == ProbeVolumesEncodingModes.SphericalHarmonicsL2)
            {
                s_ProbeVolumeAtlasBlitDataSHL2Buffer = new ComputeBuffer(s_MaxProbeVolumeProbeCount * ProbeVolumePayload.GetDataSHL2Stride(), Marshal.SizeOf(typeof(float)));
            }
            s_ProbeVolumeAtlasBlitDataValidityBuffer = new ComputeBuffer(s_MaxProbeVolumeProbeCount, Marshal.SizeOf(typeof(float)));

            m_ProbeVolumeAtlasSHRTDepthSliceCount = GetDepthSliceCountFromEncodingMode(ShaderConfig.s_ProbeVolumesEncodingMode);

            m_ProbeVolumeAtlasSHRTHandle = RTHandles.Alloc(
                width: s_ProbeVolumeAtlasResolution,
                height: s_ProbeVolumeAtlasResolution,
                slices: s_ProbeVolumeAtlasResolution * m_ProbeVolumeAtlasSHRTDepthSliceCount,
                dimension:         TextureDimension.Tex3D,
                colorFormat:       k_ProbeVolumeAtlasFormat,
                enableRandomWrite: true,
                useMipMap:         false,
                name:              "ProbeVolumeAtlasSH"
            );

            probeVolumeAtlas = new Texture3DAtlasDynamic<ProbeVolume.ProbeVolumeAtlasKey>(s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution, k_MaxVisibleProbeVolumeCount, m_ProbeVolumeAtlasSHRTHandle);

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                s_ProbeVolumeAtlasOctahedralDepthBuffer = new ComputeBuffer(s_MaxProbeVolumeProbeOctahedralDepthCount, Marshal.SizeOf(typeof(float)));

                // TODO: (Nick): Might be able drop precision down to half-floats, since we only need to encode depth data up to one probe spacing distance away. Could rescale depth data to this range before encoding.
                m_ProbeVolumeAtlasOctahedralDepthRTHandle = RTHandles.Alloc(
                    width: s_ProbeVolumeAtlasOctahedralDepthResolution,
                    height: s_ProbeVolumeAtlasOctahedralDepthResolution,
                    slices: 1,
                    dimension: TextureDimension.Tex2D,
                    colorFormat: k_ProbeVolumeOctahedralDepthAtlasFormat,
                    enableRandomWrite: true,
                    useMipMap: false,
                    name: "ProbeVolumeAtlasOctahedralDepthMeanAndVariance"
                );

                probeVolumeAtlasOctahedralDepth = new Texture2DAtlasDynamic(
                    s_ProbeVolumeAtlasOctahedralDepthResolution,
                    s_ProbeVolumeAtlasOctahedralDepthResolution,
                    k_MaxVisibleProbeVolumeCount,
                    m_ProbeVolumeAtlasOctahedralDepthRTHandle
                );
            }
        }

        internal void DestroyProbeVolumeBuffers()
        {
            CoreUtils.SafeRelease(s_VisibleProbeVolumeBoundsBufferDefault);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeDataBufferDefault);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeBoundsBuffer);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeDataBuffer);
            CoreUtils.SafeRelease(s_ProbeVolumeAtlasBlitDataSHL01Buffer);
            CoreUtils.SafeRelease(s_ProbeVolumeAtlasBlitDataSHL2Buffer);
            CoreUtils.SafeRelease(s_ProbeVolumeAtlasBlitDataValidityBuffer);
            CoreUtils.SafeRelease(s_ProbeVolumeAtlasOctahedralDepthBuffer);

            if (m_ProbeVolumeAtlasSHRTHandle != null)
                RTHandles.Release(m_ProbeVolumeAtlasSHRTHandle);

            if (probeVolumeAtlas != null)
                probeVolumeAtlas.Release();

            if (m_ProbeVolumeAtlasOctahedralDepthRTHandle != null)
                RTHandles.Release(m_ProbeVolumeAtlasOctahedralDepthRTHandle);

            if (probeVolumeAtlasOctahedralDepth != null)
                probeVolumeAtlasOctahedralDepth.Release();

            m_VisibleProbeVolumeBounds = null;
            m_VisibleProbeVolumeData = null;
        }

        void CleanupProbeVolumes()
        {
            DestroyProbeVolumeBuffers();

        #if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared -= OnLightingDataCleared;
        #endif
        }

        internal void OnLightingDataCleared()
        {
            // User requested all lighting data to be cleared.
            // Clear out all block allocations in atlas, and clear out texture data.
            // Clearing out texture data is not strictly necessary,
            // but it makes the display atlas debug view more readable.
            // Note: We do this lazily, in order to trigger the clear during the
            // next frame's render loop on the command buffer.
            isClearProbeVolumeAtlasRequested = true;
        }

        unsafe void UpdateShaderVariablesGlobalProbeVolumesDefault(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            cb._EnableProbeVolumes = 0;
            cb._ProbeVolumeCount = 0;
            cb._ProbeVolumeLeakMitigationMode = (int)LeakMitigationMode.NormalBias;
            cb._ProbeVolumeBilateralFilterWeightMin = 0.0f;
            cb._ProbeVolumeBilateralFilterWeight = 0.0f;
            cb._ProbeVolumeBilateralFilterOctahedralDepthParameters = Vector2.zero;

            bool probeVolumeReflectionProbeNormalizationEnabled = false;
            float probeVolumeReflectionProbeNormalizationDirectionality = 0.0f;
            float probeVolumeReflectionProbeNormalizationMin = 1.0f;
            float probeVolumeReflectionProbeNormalizationMax = 1.0f;

            cb._ProbeVolumeReflectionProbeNormalizationParameters = new Vector4(
                probeVolumeReflectionProbeNormalizationEnabled ? 1.0f : 0.0f,
                probeVolumeReflectionProbeNormalizationDirectionality,
                probeVolumeReflectionProbeNormalizationMin,
                probeVolumeReflectionProbeNormalizationMax
            );

            // Need to populate ambient probe fallback even in the default case,
            // As if the feature is enabled in the ShaderConfig, but disabled in the HDRenderPipelineAsset, we need to fallback to ambient probe only.
            SphericalHarmonicsL2 ambientProbeFallbackSH = m_SkyManager.GetAmbientProbe(hdCamera);
            SphericalHarmonicMath.PackCoefficients(s_AmbientProbeFallbackPackedCoeffs, ambientProbeFallbackSH);
            for (int i = 0; i < 7; ++i)
                for (int j = 0; j < 4; ++j)
                    cb._ProbeVolumeAmbientProbeFallbackPackedCoeffs[i * 4 + j] = s_AmbientProbeFallbackPackedCoeffs[i][j];
        }

        unsafe void UpdateShaderVariablesGlobalProbeVolumes(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            if (!m_SupportProbeVolume)
            {
                UpdateShaderVariablesGlobalProbeVolumesDefault(ref cb, hdCamera);
                return;
            }

            cb._EnableProbeVolumes = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) ? 1u : 0u;
            cb._ProbeVolumeCount = (uint)m_VisibleProbeVolumeBounds.Count;
            cb._ProbeVolumeAtlasResolutionAndSliceCount = new Vector4(
                    s_ProbeVolumeAtlasResolution,
                    s_ProbeVolumeAtlasResolution,
                    s_ProbeVolumeAtlasResolution,
                    m_ProbeVolumeAtlasSHRTDepthSliceCount
            );
            cb._ProbeVolumeAtlasResolutionAndSliceCountInverse = new Vector4(
                    1.0f / (float)s_ProbeVolumeAtlasResolution,
                    1.0f / (float)s_ProbeVolumeAtlasResolution,
                    1.0f / (float)s_ProbeVolumeAtlasResolution,
                    1.0f / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount
            );

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                cb._ProbeVolumeAtlasOctahedralDepthResolutionAndInverse = new Vector4(
                    m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                    m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height,
                    1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                    1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height
                );
            }
            else
            {
                cb._ProbeVolumeAtlasOctahedralDepthResolutionAndInverse = Vector4.zero;
            }


            var settings = hdCamera.volumeStack.GetComponent<ProbeVolumeController>();
            LeakMitigationMode leakMitigationMode = (settings == null)
                ? LeakMitigationMode.NormalBias
                : settings.leakMitigationMode.value;
            float bilateralFilterWeight = (settings == null) ? 0.0f : settings.bilateralFilterWeight.value;
            if (leakMitigationMode != LeakMitigationMode.NormalBias)
            {
                if (bilateralFilterWeight < 1e-5f)
                {
                    // If bilateralFilterWeight is effectively zero, then we are simply doing trilinear filtering.
                    // In this case we can avoid the performance cost of computing our bilateral filter entirely.
                    leakMitigationMode = LeakMitigationMode.NormalBias;
                }
            }

            cb._ProbeVolumeLeakMitigationMode = (int)leakMitigationMode;
            cb._ProbeVolumeBilateralFilterWeightMin = 1e-5f;
            cb._ProbeVolumeBilateralFilterWeight = bilateralFilterWeight;
            cb._ProbeVolumeBilateralFilterOctahedralDepthParameters = Vector2.zero;
            if (leakMitigationMode == LeakMitigationMode.OctahedralDepthOcclusionFilter)
            {
                cb._ProbeVolumeBilateralFilterOctahedralDepthParameters = new Vector2(
                    settings.octahedralDepthWeightMin.value,
                    settings.octahedralDepthLightBleedReductionThreshold.value
                );
            }

            bool probeVolumeReflectionProbeNormalizationEnabled = settings.reflectionProbeNormalizationWeight.value >= 1e-5f;
            float probeVolumeReflectionProbeNormalizationDirectionality = settings.reflectionProbeNormalizationDirectionality.value;
            float probeVolumeReflectionProbeNormalizationMin = probeVolumeReflectionProbeNormalizationEnabled ? 0.0f : 1.0f;
            float probeVolumeReflectionProbeNormalizationMax = (probeVolumeReflectionProbeNormalizationEnabled && !settings.reflectionProbeNormalizationDarkenOnly.value) ? float.MaxValue : 1.0f;
            probeVolumeReflectionProbeNormalizationEnabled = ((Mathf.Abs(probeVolumeReflectionProbeNormalizationMin - 1.0f) < 1e-5f) && (Mathf.Abs(probeVolumeReflectionProbeNormalizationMax - 1.0f) < 1e-5f))
                ? false
                : probeVolumeReflectionProbeNormalizationEnabled;

            cb._ProbeVolumeReflectionProbeNormalizationParameters = new Vector4(
                settings.reflectionProbeNormalizationWeight.value,
                probeVolumeReflectionProbeNormalizationDirectionality,
                probeVolumeReflectionProbeNormalizationMin,
                probeVolumeReflectionProbeNormalizationMax
            );

            SphericalHarmonicsL2 ambientProbeFallbackSH = m_SkyManager.GetAmbientProbe(hdCamera);
            SphericalHarmonicMath.PackCoefficients(s_AmbientProbeFallbackPackedCoeffs, ambientProbeFallbackSH);
            for (int i = 0; i < 7; ++i)
                for (int j = 0; j < 4; ++j)
                    cb._ProbeVolumeAmbientProbeFallbackPackedCoeffs[i * 4 + j] = s_AmbientProbeFallbackPackedCoeffs[i][j];
        }

        void PushProbeVolumesGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            Debug.Assert(ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled);
            Debug.Assert(m_SupportProbeVolume);

            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasSH, m_ProbeVolumeAtlasSHRTHandle);

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasOctahedralDepth, m_ProbeVolumeAtlasOctahedralDepthRTHandle);
            }
        }

        internal void PushProbeVolumesGlobalParamsDefault(HDCamera hdCamera, CommandBuffer cmd)
        {
            Debug.Assert(ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled);
            Debug.Assert(hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) == false);

            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBufferDefault);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBufferDefault);
            cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasSH, TextureXR.GetBlackTexture3D());

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasOctahedralDepth, Texture2D.blackTexture);
            }
        }

        internal void ReleaseProbeVolumeFromAtlas(ProbeVolumeHandle volume)
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            if (!m_SupportProbeVolume)
                return;

            int id = volume.GetBakeID();
            ProbeVolume.ProbeVolumeAtlasKey key = volume.ComputeProbeVolumeAtlasKey();
            ProbeVolume.ProbeVolumeAtlasKey keyPrevious = volume.GetProbeVolumeAtlasKeyPrevious();

            // TODO: Currently, this means that if there are multiple probe volumes that point to the same payload,
            // if any of them are disabled, that payload will be evicted from the atlas.
            // If will get added back to the atlas the next frame any of the remaining enabled probe volumes are seen,
            // so functionally, this is fine. It does however put additional pressure on the atlas allocator + blitting.
            // Could add reference counting to atlas keys, or could track key use timestamps and evict based on least recently used as needed.
            if (probeVolumeAtlas.IsTextureSlotAllocated(key)) { probeVolumeAtlas.ReleaseTextureSlot(key); }
            if (probeVolumeAtlas.IsTextureSlotAllocated(keyPrevious)) { probeVolumeAtlas.ReleaseTextureSlot(keyPrevious); }

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                probeVolumeAtlasOctahedralDepth.ReleaseTextureSlot(id);
            }
        }

        internal bool EnsureProbeVolumeInAtlas(ScriptableRenderContext renderContext, CommandBuffer cmd, ProbeVolumeHandle volume)
        {
            int id = volume.GetAtlasID();
            int width = volume.parameters.resolutionX;
            int height = volume.parameters.resolutionY;
            int depth = volume.parameters.resolutionZ;

            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ;
            Debug.Assert(size > 0, "ProbeVolume: Encountered probe volume with resolution set to zero on all three axes.");

            ProbeVolume.ProbeVolumeAtlasKey key = volume.ComputeProbeVolumeAtlasKey();
            ProbeVolume.ProbeVolumeAtlasKey keyPrevious = volume.GetProbeVolumeAtlasKeyPrevious();
            if (!key.Equals(keyPrevious))
            {
                if (probeVolumeAtlas.IsTextureSlotAllocated(keyPrevious))
                {
                    probeVolumeAtlas.ReleaseTextureSlot(keyPrevious);
                }
                volume.SetProbeVolumeAtlasKeyPrevious(key);
            }

            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = probeVolumeAtlas.EnsureTextureSlot(out bool isUploadNeeded, out volume.parameters.scale, out volume.parameters.bias, key, width, height, depth);

            if (isSlotAllocated)
            {
                if (isUploadNeeded || volume.IsDataUpdated())
                {
                    if (!volume.IsDataAssigned() || !volume.IsAssetCompatible())
                    {
                        ReleaseProbeVolumeFromAtlas(volume);
                        return false;
                    }

                    // Debug.Log("Uploading probe volume to atlas: " + volume.gameObject.name + ", because: " + (isUploadNeeded ? "atlas slot allocated." : "data was updated."));

                    int sizeSHCoefficientsL01 = size * ProbeVolumePayload.GetDataSHL01Stride();
                    int sizeSHCoefficientsL2 = size * ProbeVolumePayload.GetDataSHL2Stride();

                    Debug.AssertFormat(volume.DataSHL01Length == sizeSHCoefficientsL01, "ProbeVolume: The probe volume baked data and its resolution are out of sync! Volume data length is {0}, but resolution * SH stride size is {1}.", volume.DataSHL01Length, sizeSHCoefficientsL01);
                    if (ShaderConfig.s_ProbeVolumesEncodingMode == ProbeVolumesEncodingModes.SphericalHarmonicsL2)
                    {
                        Debug.AssertFormat(volume.DataSHL2Length == sizeSHCoefficientsL2, "ProbeVolume: The probe volume baked data and its resolution are out of sync! Volume data length is {0}, but resolution * SH stride size is {1}.", volume.DataSHL2Length, sizeSHCoefficientsL2);
                    }

                    if (size > s_MaxProbeVolumeProbeCount)
                    {
                        Debug.LogWarningFormat("ProbeVolume: probe volume baked data size exceeds the currently max supported blitable size. Volume data size is {0}, but s_MaxProbeVolumeProbeCount is {1}. Please decrease ProbeVolume resolution, or increase ProbeVolumeLighting.s_MaxProbeVolumeProbeCount.", size, s_MaxProbeVolumeProbeCount);
                        return false;
                    }

                    //Debug.Log("Uploading Probe Volume Data with key " + key + " at scale bias = " + volume.parameters.scaleBias);
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeResolution, new Vector3(
                        volume.parameters.resolutionX,
                        volume.parameters.resolutionY,
                        volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeResolutionInverse, new Vector3(
                        1.0f / (float)volume.parameters.resolutionX,
                        1.0f / (float)volume.parameters.resolutionY,
                        1.0f / (float)volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasScale,
                        volume.parameters.scale
                    );
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasBias,
                        volume.parameters.bias
                    );
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, new Vector4(
                        s_ProbeVolumeAtlasResolution,
                        s_ProbeVolumeAtlasResolution,
                        s_ProbeVolumeAtlasResolution,
                        m_ProbeVolumeAtlasSHRTDepthSliceCount
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, new Vector4(
                        1.0f / (float)s_ProbeVolumeAtlasResolution,
                        1.0f / (float)s_ProbeVolumeAtlasResolution,
                        1.0f / (float)s_ProbeVolumeAtlasResolution,
                        1.0f / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasSHRotateRight, key.rotation * new Vector3(1.0f, 0.0f, 0.0f));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasSHRotateUp, key.rotation * new Vector3(0.0f, 1.0f, 0.0f));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasSHRotateForward, key.rotation * new Vector3(0.0f, 0.0f, 1.0f));

                    volume.SetDataSHL01(s_ProbeVolumeAtlasBlitDataSHL01Buffer);
                    volume.SetDataValidity(s_ProbeVolumeAtlasBlitDataValidityBuffer);
                    cmd.SetComputeIntParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasReadBufferCount, size);
                    cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasReadSHL01Buffer, s_ProbeVolumeAtlasBlitDataSHL01Buffer);
                    if (ShaderConfig.s_ProbeVolumesEncodingMode == ProbeVolumesEncodingModes.SphericalHarmonicsL2)
                    {
                        volume.SetDataSHL2(s_ProbeVolumeAtlasBlitDataSHL2Buffer);
                        cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasReadSHL2Buffer, s_ProbeVolumeAtlasBlitDataSHL2Buffer);
                    }
                    cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasReadValidityBuffer, s_ProbeVolumeAtlasBlitDataValidityBuffer);
                    cmd.SetComputeTextureParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasWriteTextureSH, m_ProbeVolumeAtlasSHRTHandle);

                    // TODO: Determine optimal batch size.
                    const int kBatchSize = 256;
                    int numThreadGroups = Mathf.CeilToInt((float)size / (float)kBatchSize);
                    cmd.DispatchCompute(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, numThreadGroups, 1, 1);
                    return true;

                }
                return false;
            }

            if (!isSlotAllocated)
            {
                Debug.LogWarning($"ProbeVolume: Texture Atlas failed to allocate space for texture id: {id}, width: {width}, height: {height}, depth: {depth}, rotation: {key.rotation.eulerAngles}");
            }

            return false;
        }

        internal bool EnsureProbeVolumeInAtlasOctahedralDepth(ScriptableRenderContext renderContext, CommandBuffer cmd, ProbeVolumeHandle volume)
        {
            int key = volume.GetAtlasID();
            int width = volume.parameters.resolutionX * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth;
            int height = volume.parameters.resolutionY * k_ProbeOctahedralDepthHeight;
            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth * k_ProbeOctahedralDepthHeight * 2; // * 2 for [mean, mean^2]
            Debug.Assert(size > 0, "ProbeVolume: Encountered probe volume with resolution set to zero on all three axes.");

            // TODO: Store volume resolution inside the atlas's key->bias dictionary.
            // If resolution has changed since upload, need to free previous allocation from atlas,
            // and attempt to allocate a new chunk from the atlas for the new resolution settings.
            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = probeVolumeAtlasOctahedralDepth.EnsureTextureSlot(out bool isUploadNeeded, out volume.parameters.octahedralDepthScaleBias, key, width, height);

            if (isSlotAllocated)
            {
                // TODO: FIXME: volume.GetDataIsUpdated() will return false after the standard SH atlas calls volume.GetPayload()
                // This means that even though the octahedral depth data was updated, it will not be updated.
                // Need to either add a second dirty flag specifically for octahedral depth data, or we need to switch over to a timestamp.
                if (isUploadNeeded || volume.IsDataUpdated())
                {
                    if (!volume.IsDataAssigned() || !volume.IsAssetCompatible())
                    {
                        ReleaseProbeVolumeFromAtlas(volume);
                        return false;
                    }

                    // Blit:
                    {
                        Debug.AssertFormat(volume.DataOctahedralDepthLength == size, "ProbeVolume: The probe volume baked data and its resolution are out of sync! Volume data length is {0}, but resolution size is {1}.", volume.DataOctahedralDepthLength, size);

                        if (size > s_MaxProbeVolumeProbeOctahedralDepthCount)
                        {
                            Debug.LogWarningFormat("ProbeVolume: probe volume octahedral depth baked data size exceeds the currently max supported blitable size. Volume data size is {0}, but s_MaxProbeVolumeProbeCount is {1}. Please decrease ProbeVolume resolution, or increase ProbeVolumeLighting.s_MaxProbeVolumeProbeCount.", size, s_MaxProbeVolumeProbeOctahedralDepthCount);
                            return false;
                        }

                        //Debug.Log("Uploading Probe Volume Data with key " + key + " at scale bias = " + volume.parameters.scaleBias);
                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeResolution, new Vector3(
                            volume.parameters.resolutionX,
                            volume.parameters.resolutionY,
                            volume.parameters.resolutionZ
                        ));
                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeResolutionInverse, new Vector3(
                            1.0f / (float)volume.parameters.resolutionX,
                            1.0f / (float)volume.parameters.resolutionY,
                            1.0f / (float)volume.parameters.resolutionZ
                        ));
                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthScaleBias,
                            volume.parameters.octahedralDepthScaleBias
                        );
                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthResolutionAndInverse, new Vector4(
                            m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                            m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height,
                            1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                            1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height
                        ));
                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, new Vector4(
                            s_ProbeVolumeAtlasResolution,
                            s_ProbeVolumeAtlasResolution,
                            s_ProbeVolumeAtlasResolution,
                            m_ProbeVolumeAtlasSHRTDepthSliceCount
                        ));
                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, new Vector4(
                            1.0f / (float)s_ProbeVolumeAtlasResolution,
                            1.0f / (float)s_ProbeVolumeAtlasResolution,
                            1.0f / (float)s_ProbeVolumeAtlasResolution,
                            1.0f / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount
                        ));

                        volume.SetDataOctahedralDepth(s_ProbeVolumeAtlasOctahedralDepthBuffer);
                        cmd.SetComputeIntParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthReadBufferCount, size / 2);
                        cmd.SetComputeBufferParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthReadBuffer, s_ProbeVolumeAtlasOctahedralDepthBuffer);
                        cmd.SetComputeTextureParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthWriteTexture, m_ProbeVolumeAtlasOctahedralDepthRTHandle);

                        // TODO: Determine optimal batch size.
                        const int kBatchSize = 256;
                        int numThreadGroups = Mathf.CeilToInt((float)(size / 2) / (float)kBatchSize); // / 2 for [mean, mean^2] (will be written to a float2 RT)
                        cmd.DispatchCompute(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, numThreadGroups, 1, 1);
                    }
                    return true;

                }
                return false;
            }

            if (!isSlotAllocated)
            {
                Debug.LogWarningFormat("ProbeVolume: Texture Atlas failed to allocate space for octahedral depth texture { key: {0}, width: {1}, height: {2} }", key, width, height);
            }

            return false;
        }

        internal void ClearProbeVolumeAtlasIfRequested(CommandBuffer cmd)
        {
            if (!isClearProbeVolumeAtlasRequested) { return; }
            isClearProbeVolumeAtlasRequested = false;

            probeVolumeAtlas.ResetAllocator();
            cmd.SetRenderTarget(m_ProbeVolumeAtlasSHRTHandle.rt, 0, CubemapFace.Unknown, 0);
            cmd.ClearRenderTarget(false, true, Color.black, 0.0f);

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                probeVolumeAtlasOctahedralDepth.ResetAllocator();
                cmd.SetRenderTarget(m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt, 0, CubemapFace.Unknown, 0);
                cmd.ClearRenderTarget(false, true, Color.black, 0.0f);
            }
        }

        ProbeVolumeList PrepareVisibleProbeVolumeList(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd)
        {
            ProbeVolumeList probeVolumes = new ProbeVolumeList();

            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return probeVolumes;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
            {
                PushProbeVolumesGlobalParamsDefault(hdCamera, cmd);
            }
            else
            {
                PrepareVisibleProbeVolumeListBuffers(renderContext, hdCamera, cmd, ref probeVolumes);
                PushProbeVolumesGlobalParams(hdCamera, cmd);
            }

            return probeVolumes;
        }

        void PrepareVisibleProbeVolumeListBuffers(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd, ref ProbeVolumeList probeVolumes)
        {
            var settings = hdCamera.volumeStack.GetComponent<ProbeVolumeController>();
            bool octahedralDepthOcclusionFilterIsEnabled =
                ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth
                && settings.leakMitigationMode.value == LeakMitigationMode.OctahedralDepthOcclusionFilter;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareProbeVolumeList)))
            {
                ClearProbeVolumeAtlasIfRequested(cmd);
                probeVolumeAtlas.UpdateTimestamp();

                //Debug.Log("Probe Volume Atlas: allocationCount: " + probeVolumeAtlas.GetAllocationCount() + ", allocationRatio: " + probeVolumeAtlas.GetAllocationRatio());

                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleProbeVolumeBounds.Clear();
                m_VisibleProbeVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                List<ProbeVolumeHandle> volumes = ProbeVolumeManager.manager.CollectVolumesToRender();

                int probeVolumesCount = Math.Min(volumes.Count, k_MaxVisibleProbeVolumeCount);
                int sortCount = 0;

                // Sort probe volumes smallest from smallest to largest volume.
                // Same as is done with reflection probes.
                // See LightLoop.cs::PrepareLightsForGPU() for original example of this.
                for (int probeVolumesIndex = 0; (probeVolumesIndex < volumes.Count) && (sortCount < probeVolumesCount); probeVolumesIndex++)
                {
                    ProbeVolumeHandle volume = volumes[probeVolumesIndex];

#if UNITY_EDITOR
                    if (!volume.IsAssetCompatible())
                        continue;

                    if (volume.IsHiddesInScene())
                        continue;
#endif

                    if (!volume.IsDataAssigned())
                        continue;

                    if (ShaderConfig.s_ProbeVolumesAdditiveBlending == 0 && volume.parameters.volumeBlendMode != VolumeBlendMode.Normal)
                    {
                        // Non-normal blend mode volumes are not supported. Skip.
                        continue;
                    }

                    float probeVolumeDepthFromCameraWS = Vector3.Dot(hdCamera.camera.transform.forward, volume.position - camPosition);
                    if (probeVolumeDepthFromCameraWS >= volume.parameters.distanceFadeEnd)
                    {
                        // Probe volume is completely faded out from distance fade optimization.
                        // Do not bother adding it to the list, it would evaluate to zero weight.
                        continue;
                    }

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.position, volume.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, hdCamera.frustum.planes.Length, hdCamera.frustum.corners.Length))
                    {
                        var logVolume = CalculateProbeVolumeLogVolume(volume.parameters.size);

                        m_ProbeVolumeSortKeys[sortCount++] = PackProbeVolumeSortKey(volume.parameters.volumeBlendMode, logVolume, probeVolumesIndex);
                    }
                }

                CoreUnsafeUtils.QuickSort(m_ProbeVolumeSortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    // In 1. we have already classify and sorted the probe volume, we need to use this sorted order here
                    uint sortKey = m_ProbeVolumeSortKeys[sortIndex];
                    int probeVolumesIndex;
                    UnpackProbeVolumeSortKey(sortKey, out probeVolumesIndex);

                    ProbeVolumeHandle volume = volumes[probeVolumesIndex];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.position, volume.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // TODO: cache these?
                    var data = volume.parameters.ConvertToEngineData();

                    // Note: The system is not aware of slice packing in Z.
                    // Need to modify scale and bias terms just before uploading to GPU.
                    // TODO: Should we make it aware earlier up the chain?
                    data.scale.z = data.scale.z / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount;
                    data.bias.z = data.bias.z / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount;

                    m_VisibleProbeVolumeBounds.Add(obb);
                    m_VisibleProbeVolumeData.Add(data);
                }

                s_VisibleProbeVolumeBoundsBuffer.SetData(m_VisibleProbeVolumeBounds);
                s_VisibleProbeVolumeDataBuffer.SetData(m_VisibleProbeVolumeData);

                // Fill the struct with pointers in order to share the data with the light loop.
                probeVolumes.bounds = m_VisibleProbeVolumeBounds;
                probeVolumes.data = m_VisibleProbeVolumeData;

                // For now, only upload one volume per frame.
                // This is done:
                // 1) To timeslice upload cost across N frames for N volumes.
                // 2) To avoid creating a sync point between compute buffer upload and each volume upload.
                const int volumeUploadedToAtlasSHCapacity = 1;
                int volumeUploadedToAtlasOctahedralDepthCapacity = octahedralDepthOcclusionFilterIsEnabled ? 1 : 0;
                int volumeUploadedToAtlasSHCount = 0;
                int volumeUploadedToAtlasOctahedralDepthCount = 0;

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    uint sortKey = m_ProbeVolumeSortKeys[sortIndex];
                    int probeVolumesIndex;
                    UnpackProbeVolumeSortKey(sortKey, out probeVolumesIndex);

                    ProbeVolumeHandle volume = volumes[probeVolumesIndex];

                    if (volumeUploadedToAtlasSHCount < volumeUploadedToAtlasSHCapacity)
                    {
                        bool volumeWasUploaded = EnsureProbeVolumeInAtlas(renderContext, cmd, volume);
                        if (volumeWasUploaded)
                            ++volumeUploadedToAtlasSHCount;
                    }

                    if (volumeUploadedToAtlasOctahedralDepthCount < volumeUploadedToAtlasOctahedralDepthCapacity)
                    {
                        bool volumeWasUploaded = EnsureProbeVolumeInAtlasOctahedralDepth(renderContext, cmd, volume);
                        if (volumeWasUploaded)
                            ++volumeUploadedToAtlasOctahedralDepthCount;
                    }

                    if (volumeUploadedToAtlasSHCount == volumeUploadedToAtlasSHCapacity
                        && volumeUploadedToAtlasOctahedralDepthCount == volumeUploadedToAtlasOctahedralDepthCapacity)
                    {
                        // Met our capacity this frame. Early out.
                        break;
                    }
                }

                return;
            }
        }

        internal static float CalculateProbeVolumeLogVolume(Vector3 size)
        {
            //Notes:
            // - 1+ term is to prevent having negative values in the log result
            // - 1000* is too keep 3 digit after the dot while we truncate the result later
            // - 1048575 is 2^20-1 as we pack the result on 20bit later
            float boxVolume = 8f* size.x * size.y * size.z;
            float logVolume = Mathf.Clamp(Mathf.Log(1 + boxVolume, 1.05f)*1000, 0, 1048575);
            return logVolume;
        }

        internal static void UnpackProbeVolumeSortKey(uint sortKey, out int probeIndex)
        {
            const uint PROBE_VOLUME_MASK = (1 << 11) - 1;
            probeIndex = (int)(sortKey & PROBE_VOLUME_MASK);
        }

        internal static uint PackProbeVolumeSortKey(VolumeBlendMode volumeBlendMode, float logVolume, int probeVolumeIndex)
        {
            // 1 bit blendMode, 20 bit volume, 11 bit index
            Debug.Assert(logVolume >= 0.0f && (uint)logVolume < (1 << 20));
            Debug.Assert(probeVolumeIndex >= 0 && (uint)probeVolumeIndex < (1 << 11));
            const uint VOLUME_MASK = (1 << 20) - 1;
            const uint INDEX_MASK = (1 << 11) - 1;

            // Sort probe volumes primarily by blend mode, and secondarily by size.
            // In the lightloop, this means we will evaluate all Additive and Subtractive blending volumes first,
            // and finally our Normal (over) blending volumes.
            // This allows us to early out during the Normal blend volumes if opacity has reached 1.0 across all threads.
            uint blendModeBits = ((volumeBlendMode != VolumeBlendMode.Normal) ? 0u : 1u) << 31;
            uint logVolumeBits = ((uint)logVolume & VOLUME_MASK) << 11;
            uint indexBits = (uint)probeVolumeIndex & INDEX_MASK;

            return blendModeBits | logVolumeBits | indexBits;
        }

        void RenderProbeVolumeDebugOverlay(in DebugParameters debugParameters, CommandBuffer cmd)
        {
            if (!m_SupportProbeVolume)
                return;

            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.probeVolumeDebugMode != ProbeVolumeDebugMode.None)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDebug)))
                {
                    if (lightingDebug.probeVolumeDebugMode == ProbeVolumeDebugMode.VisualizeAtlas)
                    {
                        DisplayProbeVolumeAtlas(cmd, debugParameters.probeVolumeOverlayParameters, debugParameters.debugOverlay);
                    }
                }
            }
        }

        struct ProbeVolumeDebugOverlayParameters
        {
            public Material material;
            public Vector4 validRange;
            public Vector4 textureViewScale;
            public Vector4 textureViewBias;
            public Vector3 textureViewResolution;
            public Vector4 atlasResolutionAndSliceCount;
            public Vector4 atlasResolutionAndSliceCountInverse;
            public Vector4 atlasTextureOctahedralDepthScaleBias;
            public int sliceMode;
            public RTHandle probeVolumeAtlas;
            public RTHandle probeVolumeAtlasOctahedralDepth;
        }

        ProbeVolumeDebugOverlayParameters PrepareProbeVolumeOverlayParameters(LightingDebugSettings lightingDebug)
        {
            ProbeVolumeDebugOverlayParameters parameters = new ProbeVolumeDebugOverlayParameters();

            parameters.material = m_DebugDisplayProbeVolumeMaterial;

            parameters.sliceMode = (int)lightingDebug.probeVolumeAtlasSliceMode;
            parameters.validRange = new Vector4(lightingDebug.probeVolumeMinValue, 1.0f / (lightingDebug.probeVolumeMaxValue - lightingDebug.probeVolumeMinValue));
            parameters.textureViewScale = new Vector3(1.0f, 1.0f, 1.0f);
            parameters.textureViewBias = new Vector3(0.0f, 0.0f, 0.0f);
            parameters.textureViewResolution = new Vector3(s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution);
            parameters.atlasTextureOctahedralDepthScaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);

#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != null)
            {
                var selectedProbeVolume = UnityEditor.Selection.activeGameObject.GetComponent<ProbeVolume>();
                if (selectedProbeVolume != null)
                {
                    // User currently has a probe volume selected.
                    // Compute a scaleBias term so that atlas view automatically zooms into selected probe volume.
                    ProbeVolume.ProbeVolumeAtlasKey selectedProbeVolumeKey = selectedProbeVolume.ComputeProbeVolumeAtlasKey();
                    int id = selectedProbeVolume.GetBakeID();
                    if (probeVolumeAtlas.TryGetScaleBias(out Vector3 selectedProbeVolumeScale, out Vector3 selectedProbeVolumeBias, selectedProbeVolumeKey))
                    {
                        parameters.textureViewScale = selectedProbeVolumeScale;
                        parameters.textureViewBias = selectedProbeVolumeBias;
                        parameters.textureViewResolution = new Vector3(
                            selectedProbeVolume.parameters.resolutionX,
                            selectedProbeVolume.parameters.resolutionY,
                            selectedProbeVolume.parameters.resolutionZ
                        );
                    }

                    if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
                    {
                        if (probeVolumeAtlasOctahedralDepth.TryGetScaleBias(out Vector4 selectedProbeVolumeOctahedralDepthScaleBias, id))
                        {
                            parameters.atlasTextureOctahedralDepthScaleBias = selectedProbeVolumeOctahedralDepthScaleBias;

                            if (selectedProbeVolume.parameters.drawOctahedralDepthRays)
                            {
                                // Zoom all the way in to the specific octahedral depth probe that is being inspected.
                                Vector4 probeOctahedralDepthScaleBias2D = ProbeVolume.ComputeProbeOctahedralDepthScaleBias2D(
                                    selectedProbeVolume,
                                    selectedProbeVolume.parameters.drawOctahedralDepthRayIndexX,
                                    selectedProbeVolume.parameters.drawOctahedralDepthRayIndexY,
                                    selectedProbeVolume.parameters.drawOctahedralDepthRayIndexZ
                                );

                                parameters.atlasTextureOctahedralDepthScaleBias = new Vector4(
                                   parameters.atlasTextureOctahedralDepthScaleBias.x * probeOctahedralDepthScaleBias2D.x,
                                   parameters.atlasTextureOctahedralDepthScaleBias.y * probeOctahedralDepthScaleBias2D.y,
                                   parameters.atlasTextureOctahedralDepthScaleBias.z + (probeOctahedralDepthScaleBias2D.z * parameters.atlasTextureOctahedralDepthScaleBias.x * probeOctahedralDepthScaleBias2D.x),
                                   parameters.atlasTextureOctahedralDepthScaleBias.w + (probeOctahedralDepthScaleBias2D.w * parameters.atlasTextureOctahedralDepthScaleBias.y * probeOctahedralDepthScaleBias2D.y)
                                );
                            }
                        }
                    }
                }
            }
#endif

            // Note: The system is not aware of slice packing in Z.
            // Need to modify scale and bias terms just before uploading to GPU.
            // TODO: Should we make it aware earlier up the chain?
            parameters.textureViewScale.z = parameters.textureViewScale.z / m_ProbeVolumeAtlasSHRTDepthSliceCount;
            parameters.textureViewBias.z = parameters.textureViewBias.z / m_ProbeVolumeAtlasSHRTDepthSliceCount;

            parameters.atlasResolutionAndSliceCount = new Vector4(s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution, m_ProbeVolumeAtlasSHRTDepthSliceCount);
            parameters.atlasResolutionAndSliceCountInverse = new Vector4(1.0f / s_ProbeVolumeAtlasResolution, 1.0f / s_ProbeVolumeAtlasResolution, 1.0f / s_ProbeVolumeAtlasResolution, 1.0f / m_ProbeVolumeAtlasSHRTDepthSliceCount);

            parameters.probeVolumeAtlas = m_ProbeVolumeAtlasSHRTHandle;
            parameters.probeVolumeAtlasOctahedralDepth = m_ProbeVolumeAtlasOctahedralDepthRTHandle;

            return parameters;
        }

        static void DisplayProbeVolumeAtlas(CommandBuffer cmd, in ProbeVolumeDebugOverlayParameters parameters, DebugOverlay debugOverlay)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetVector(HDShaderIDs._TextureViewScale, parameters.textureViewScale);
            propertyBlock.SetVector(HDShaderIDs._TextureViewBias, parameters.textureViewBias);
            propertyBlock.SetVector(HDShaderIDs._TextureViewResolution, parameters.textureViewResolution);
            cmd.SetGlobalVector(HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, parameters.atlasResolutionAndSliceCount);
            cmd.SetGlobalVector(HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, parameters.atlasResolutionAndSliceCountInverse);

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                propertyBlock.SetVector(HDShaderIDs._AtlasTextureOctahedralDepthScaleBias, parameters.atlasTextureOctahedralDepthScaleBias);
            }

            propertyBlock.SetVector(HDShaderIDs._ValidRange, parameters.validRange);
            propertyBlock.SetInt(HDShaderIDs._ProbeVolumeAtlasSliceMode, parameters.sliceMode);

            debugOverlay.SetViewport(cmd);
            cmd.DrawProcedural(Matrix4x4.identity, parameters.material, parameters.material.FindPass("ProbeVolume"), MeshTopology.Triangles, 3, 1, propertyBlock);
            debugOverlay.Next();
        }

    } // class ProbeVolumeLighting
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
