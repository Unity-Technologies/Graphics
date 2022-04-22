using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public struct ProbeVolumeAtlasStats
    {
        public int allocationCount;
        public float allocationRatio;
        public float largestFreeBlockRatio;
        public Vector3Int largestFreeBlockPixels;
    }

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
        public float maxNeighborDistance;
        public Vector3 bias;
        public int volumeBlendMode;
        public Vector4 octahedralDepthScaleBias;
        public Vector3 resolution;
        public uint lightLayers;
        public Vector3 resolutionInverse;
        public float normalBiasWS;
        public float viewBiasWS;
        public uint resolutionX;
        public uint resolutionXY;
        public float padding;

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
            data.maxNeighborDistance = 1;
            data.bias = Vector3.zero;
            data.volumeBlendMode = 0;
            data.octahedralDepthScaleBias = Vector4.zero;
            data.resolution = Vector3.zero;
            data.lightLayers = 0;
            data.resolutionInverse = Vector3.zero;
            data.normalBiasWS = 0.0f;
            data.viewBiasWS = 0.0f;
            data.resolutionX = 0;
            data.resolutionXY = 0;
            data.padding = 0;

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

    struct ProbeVolumesRenderGraphResources
    {
        public ComputeBufferHandle boundsBuffer;
        public ComputeBufferHandle dataBuffer;
        public TextureHandle probeVolumesAtlas;
        public TextureHandle octahedralDepthAtlas;
    }

    struct ProbeVolumeList
    {
        public List<OrientedBBox> bounds;
        public List<ProbeVolumeEngineData> data;
        public ProbeVolumesRenderGraphResources rgResources;
    }

    class ClearProbeVolumeAtlasesPassData
    {
        public TextureHandle probeVolumesAtlas;
        public TextureHandle octahedralDepthAtlas;
    }

    struct UploadProbeVolumeParameters
    {
        public ProbeVolumeHandle volume;

        public int probeVolumeAtlasSize;
        public int probeVolumeAtlasSHRTDepthSliceCount;

        public Vector3 probeVolumeAtlasSHRotateRight;
        public Vector3 probeVolumeAtlasSHRotateUp;
        public Vector3 probeVolumeAtlasSHRotateForward;
    }
    class UploadProbeVolumePassData
    {
        public UploadProbeVolumeParameters parameters;
        public TextureHandle targetAtlas;
        public ComputeBufferHandle uploadBufferSHL01;
        public ComputeBufferHandle uploadBufferSHL2;
        public ComputeBufferHandle uploadBufferValidity;
    }

    struct UploadOctahedralDepthParameters
    {
        public ProbeVolumeHandle volume;

        public int targetAtlasWidth;
        public int targetAtlasHeight;
        public int targetAtlasSize;
    }
    class UploadOctahedralDepthPassData
    {
        public UploadOctahedralDepthParameters parameters;
        public TextureHandle targetAtlas;
        public ComputeBufferHandle uploadBuffer;
    }

    class PushProbeVolumesGlobalParamsPassData
    {
        public ComputeBufferHandle boundsBuffer;
        public ComputeBufferHandle dataBuffer;
        public TextureHandle probeVolumesAtlas;
        public TextureHandle octahedralDepthAtlas;
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
        bool m_SupportDynamicGI = false;
        private bool m_WasProbeVolumeDynamicGIEnabled;

        // Pre-allocate sort keys array to max size to avoid creating allocations / garbage at runtime.
        uint[] m_ProbeVolumeSortKeys = new uint[k_MaxVisibleProbeVolumeCount];

        static ComputeShader s_ProbeVolumeAtlasBlitCS = null;
        static ComputeShader s_ProbeVolumeAtlasOctahedralDepthBlitCS = null;
        static ComputeShader s_ProbeVolumeAtlasOctahedralDepthConvolveCS = null;
        static int s_ProbeVolumeAtlasBlitKernel = -1;
        static int s_ProbeVolumeAtlasOctahedralDepthBlitKernel = -1;
        static int s_ProbeVolumeAtlasOctahedralDepthConvolveKernel = -1;
        static ComputeBuffer s_ProbeVolumeAtlasOctahedralDepthBuffer = null;
        static int s_ProbeVolumeAtlasResolution;
        static int s_ProbeVolumeAtlasOctahedralDepthResolution;
        internal const int k_ProbeOctahedralDepthWidth = 8;
        internal const int k_ProbeOctahedralDepthHeight = 8;
        internal const UnityEngine.Experimental.Rendering.GraphicsFormat k_ProbeVolumeAtlasFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
        internal const UnityEngine.Experimental.Rendering.GraphicsFormat k_ProbeVolumeOctahedralDepthAtlasFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat; // float2(mean, variance)

        static int s_MaxProbeVolumeProbeCount;
        static int s_MaxProbeVolumeProbeOctahedralDepthCount;
        RTHandle m_ProbeVolumeAtlasSHRTHandle;

        int m_ProbeVolumeAtlasSHRTDepthSliceCount;
        Texture3DAtlasDynamic<ProbeVolume.ProbeVolumeAtlasKey> probeVolumeAtlas = null;

        internal int GetProbeVolumeAtlasSHRTDepthSliceCount()
        {
            return m_ProbeVolumeAtlasSHRTDepthSliceCount;
        }

        RTHandle m_ProbeVolumeAtlasOctahedralDepthRTHandle;
        Texture2DAtlasDynamic<ProbeVolume.ProbeVolumeAtlasKey> probeVolumeAtlasOctahedralDepth = null;
        bool isClearProbeVolumeAtlasRequested = false;

        // Preallocated scratch memory for storing ambient probe packed SH coefficients, which are used as a fallback when probe volume weight < 1.0.
        static Vector4[] s_AmbientProbeFallbackPackedCoeffs = new Vector4[7];

#if UNITY_EDITOR
        private static Material s_DebugSHPreviewMaterial = null;
        private static MaterialPropertyBlock s_DebugSHPreviewMaterialPropertyBlock = null;

        private static Material GetDebugSHPreviewMaterial()
        {
            return (s_DebugSHPreviewMaterial != null) ? s_DebugSHPreviewMaterial : new Material(Shader.Find("Hidden/Debug/ProbeVolumeSHPreview"));
        }

        private static MaterialPropertyBlock GetDebugSHPreviewMaterialPropertyBlock()
        {
            return (s_DebugSHPreviewMaterialPropertyBlock != null) ? s_DebugSHPreviewMaterialPropertyBlock : new MaterialPropertyBlock();
        }
#endif

        void InitializeProbeVolumes()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            m_SupportProbeVolume = asset.currentPlatformRenderPipelineSettings.supportProbeVolume && (ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled);
            m_SupportDynamicGI = m_SupportProbeVolume && asset.currentPlatformRenderPipelineSettings.supportProbeVolumeDynamicGI;

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

                probeVolumeAtlasOctahedralDepth = new Texture2DAtlasDynamic<ProbeVolume.ProbeVolumeAtlasKey>(
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
        }

        void PushProbeVolumesGlobalParams(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph, ref ProbeVolumesRenderGraphResources rgResources)
        {
            Debug.Assert(ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled);
            Debug.Assert(m_SupportProbeVolume);

            if (m_EnableRenderGraph)
            {
                using (var builder = renderGraph.AddRenderPass<PushProbeVolumesGlobalParamsPassData>("Push Probe Volumes Global Params", out var passData))
                {
                    passData.boundsBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(s_VisibleProbeVolumeBoundsBuffer));
                    passData.dataBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(s_VisibleProbeVolumeDataBuffer));

                    passData.probeVolumesAtlas = builder.ReadTexture(rgResources.probeVolumesAtlas);
                    if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
                    {
                        passData.octahedralDepthAtlas = builder.ReadTexture(rgResources.octahedralDepthAtlas);
                    }

                    builder.SetRenderFunc((PushProbeVolumesGlobalParamsPassData passData, RenderGraphContext context) =>
                        DoPushProbeVolumesGlobalParams(
                            context.cmd,
                            passData.boundsBuffer,
                            passData.dataBuffer,
                            passData.probeVolumesAtlas,
                            passData.octahedralDepthAtlas));
                }
            }
            else
            {
                DoPushProbeVolumesGlobalParams(
                    immediateCmd,
                    s_VisibleProbeVolumeBoundsBuffer,
                    s_VisibleProbeVolumeDataBuffer,
                    m_ProbeVolumeAtlasSHRTHandle,
                    m_ProbeVolumeAtlasOctahedralDepthRTHandle);
            }
        }

        internal void PushProbeVolumesGlobalParamsDefault(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph)
        {
            Debug.Assert(ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled);
            Debug.Assert(hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) == false);

            if (m_EnableRenderGraph)
            {
                using (var builder = renderGraph.AddRenderPass<PushProbeVolumesGlobalParamsPassData>("Push Probe Volumes Global Params (Default)", out var passData))
                {
                    builder.SetRenderFunc((PushProbeVolumesGlobalParamsPassData passData, RenderGraphContext context) =>
                        DoPushProbeVolumesGlobalParams(
                            context.cmd,
                            s_VisibleProbeVolumeBoundsBufferDefault,
                            s_VisibleProbeVolumeDataBufferDefault,
                            TextureXR.GetBlackTexture3D(),
                            Texture2D.blackTexture));
                }
            }
            else
            {
                DoPushProbeVolumesGlobalParams(
                immediateCmd,
                s_VisibleProbeVolumeBoundsBufferDefault,
                s_VisibleProbeVolumeDataBufferDefault,
                TextureXR.GetBlackTexture3D(),
                Texture2D.blackTexture);
            }
        }

        private static void DoPushProbeVolumesGlobalParams(
            CommandBuffer cmd,
            ComputeBuffer boundsBuffer,
            ComputeBuffer dataBuffer,
            RenderTargetIdentifier probeVolumeAtlas,
            RenderTargetIdentifier octahedralDepthAtlas)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, boundsBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, dataBuffer);

            cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasSH, probeVolumeAtlas);
            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasOctahedralDepth, octahedralDepthAtlas);
            }
        }

        internal void ReleaseProbeVolumeFromAtlas(ProbeVolumeHandle volume)
        {
            if (!m_SupportProbeVolume)
                return;

            ref ProbeVolume.ProbeVolumeAtlasKey usedKey = ref volume.GetPipelineData().UsedAtlasKey;

            // TODO: Currently, this means that if there are multiple probe volumes that point to the same payload,
            // if any of them are disabled, that payload will be evicted from the atlas.
            // If will get added back to the atlas the next frame any of the remaining enabled probe volumes are seen,
            // so functionally, this is fine. It does however put additional pressure on the atlas allocator + blitting.
            // Could add reference counting to atlas keys, or could track key use timestamps and evict based on least recently used as needed.
            if (probeVolumeAtlas.IsTextureSlotAllocated(usedKey)) { probeVolumeAtlas.ReleaseTextureSlot(usedKey); }

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                if (probeVolumeAtlasOctahedralDepth.IsTextureSlotAllocated(usedKey)) { probeVolumeAtlasOctahedralDepth.ReleaseTextureSlot(usedKey); }
            }
            
            usedKey = ProbeVolume.ProbeVolumeAtlasKey.empty;
        }

        internal void EnsureStaleDataIsFlushedFromAtlases(ProbeVolumeHandle volume, bool isOctahedralDepthAtlasEnabled)
        {
            ProbeVolume.ProbeVolumeAtlasKey key = volume.ComputeProbeVolumeAtlasKey();
            ref var usedKey = ref volume.GetPipelineData().UsedAtlasKey;
            if (!key.Equals(usedKey))
            {
                if (probeVolumeAtlas.IsTextureSlotAllocated(usedKey))
                {
                    probeVolumeAtlas.ReleaseTextureSlot(usedKey);
                }

                if (isOctahedralDepthAtlasEnabled && probeVolumeAtlasOctahedralDepth.IsTextureSlotAllocated(usedKey))
                {
                    probeVolumeAtlasOctahedralDepth.ReleaseTextureSlot(usedKey);
                }
                
                usedKey = ProbeVolume.ProbeVolumeAtlasKey.empty;
            }
        }

        internal bool EnsureProbeVolumeInAtlas(CommandBuffer immediateCmd, RenderGraph renderGraph, ref ProbeVolumesRenderGraphResources rgResources, ProbeVolumeHandle volume)
        {
            int width = volume.parameters.resolutionX;
            int height = volume.parameters.resolutionY;
            int depth = volume.parameters.resolutionZ;

            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ;
            Debug.Assert(size > 0, "ProbeVolume: Encountered probe volume with resolution set to zero on all three axes.");

            ProbeVolume.ProbeVolumeAtlasKey key = volume.ComputeProbeVolumeAtlasKey();
            
            ref var pipelineData = ref volume.GetPipelineData();

            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = probeVolumeAtlas.EnsureTextureSlot(out bool isUploadNeeded, out pipelineData.Scale, out pipelineData.Bias, key, width, height, depth);

            if (isSlotAllocated)
            {
                if (isUploadNeeded)
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

                    // Ready to upload: prepare parameters and buffers
                    UploadProbeVolumeParameters parameters = new UploadProbeVolumeParameters()
                    {
                        volume = volume,

                        probeVolumeAtlasSize = size,
                        probeVolumeAtlasSHRTDepthSliceCount = m_ProbeVolumeAtlasSHRTDepthSliceCount,

                        probeVolumeAtlasSHRotateRight = key.rotation * Vector3.right,
                        probeVolumeAtlasSHRotateUp = key.rotation * Vector3.up,
                        probeVolumeAtlasSHRotateForward = key.rotation * Vector3.forward
                    };

                    volume.EnsureVolumeBuffers();

                    if (m_EnableRenderGraph)
                    {
                        using (var builder = renderGraph.AddRenderPass<UploadProbeVolumePassData>("Upload Probe Volume", out var passData))
                        {
                            // Parameters
                            passData.parameters = parameters;

                            // Resources
                            passData.uploadBufferSHL01 = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(pipelineData.SHL01Buffer));
                            passData.uploadBufferSHL2 = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(pipelineData.SHL2Buffer));
                            passData.uploadBufferValidity = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(pipelineData.ValidityBuffer));

                            passData.targetAtlas = builder.WriteTexture(rgResources.probeVolumesAtlas);

                            builder.SetRenderFunc((UploadProbeVolumePassData passData, RenderGraphContext context) =>
                            {
                                var pipelineData = passData.parameters.volume.GetPipelineData();
                                UploadProbeVolumeToAtlas(
                                    passData.parameters,
                                    context.cmd,
                                    new ProbeVolumePipelineData()
                                    {
                                        SHL01Buffer = passData.uploadBufferSHL01,
                                        SHL2Buffer = passData.uploadBufferSHL2,
                                        ValidityBuffer = passData.uploadBufferValidity,
                                        Scale = pipelineData.Scale,
                                        Bias = pipelineData.Bias,
                                    },
                                    passData.targetAtlas);
                            });
                        }
                    }
                    else
                    {
                        UploadProbeVolumeToAtlas(
                            parameters,
                            immediateCmd,
                            pipelineData,
                            m_ProbeVolumeAtlasSHRTHandle);
                    }

                    pipelineData.UsedAtlasKey = key;

                    return true;
                }
                return false;
            }

            if (!isSlotAllocated)
            {
                Debug.LogWarning($"ProbeVolume: Texture Atlas failed to allocate space for texture id: {key.id}, width: {width}, height: {height}, depth: {depth}, rotation: {key.rotation.eulerAngles}");
            }

            return false;
        }

        private static void UploadProbeVolumeToAtlas(
            UploadProbeVolumeParameters parameters,
            CommandBuffer cmd,
            ProbeVolumePipelineData pipelineData,
            RenderTargetIdentifier targetAtlas)
        {
            ProbeVolumeHandle volume = parameters.volume;

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
                pipelineData.Scale
            );

            cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasBias,
                pipelineData.Bias
            );

            cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, new Vector4(
                s_ProbeVolumeAtlasResolution,
                s_ProbeVolumeAtlasResolution,
                s_ProbeVolumeAtlasResolution,
                parameters.probeVolumeAtlasSHRTDepthSliceCount
            ));

            cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, new Vector4(
                1.0f / (float)s_ProbeVolumeAtlasResolution,
                1.0f / (float)s_ProbeVolumeAtlasResolution,
                1.0f / (float)s_ProbeVolumeAtlasResolution,
                1.0f / (float)parameters.probeVolumeAtlasSHRTDepthSliceCount
            ));

            cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasSHRotateRight, parameters.probeVolumeAtlasSHRotateRight);
            cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasSHRotateUp, parameters.probeVolumeAtlasSHRotateUp);
            cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasSHRotateForward, parameters.probeVolumeAtlasSHRotateForward);

            cmd.SetComputeIntParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasReadBufferCount, parameters.probeVolumeAtlasSize);

            cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasReadSHL01Buffer, pipelineData.SHL01Buffer);
            if (ShaderConfig.s_ProbeVolumesEncodingMode == ProbeVolumesEncodingModes.SphericalHarmonicsL2)
            {
                cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasReadSHL2Buffer, pipelineData.SHL2Buffer);
            }
            cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasReadValidityBuffer, pipelineData.ValidityBuffer);

            cmd.SetComputeTextureParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasWriteTextureSH, targetAtlas);

            // TODO: Determine optimal batch size.
            const int kBatchSize = 256;
            int numThreadGroups = (parameters.probeVolumeAtlasSize + kBatchSize - 1) / kBatchSize;
            cmd.DispatchCompute(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, numThreadGroups, 1, 1);

        }

        internal bool EnsureProbeVolumeInAtlasOctahedralDepth(CommandBuffer immediateCmd, RenderGraph renderGraph, ref ProbeVolumesRenderGraphResources rgResources, ProbeVolumeHandle volume)
        {
            int width = volume.parameters.resolutionX * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth;
            int height = volume.parameters.resolutionY * k_ProbeOctahedralDepthHeight;
            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth * k_ProbeOctahedralDepthHeight * 2; // * 2 for [mean, mean^2]
            Debug.Assert(size > 0, "ProbeVolume: Encountered probe volume with resolution set to zero on all three axes.");

            ProbeVolume.ProbeVolumeAtlasKey key = volume.ComputeProbeVolumeAtlasKey();

            // TODO: Store volume resolution inside the atlas's key->bias dictionary.
            // If resolution has changed since upload, need to free previous allocation from atlas,
            // and attempt to allocate a new chunk from the atlas for the new resolution settings.
            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = probeVolumeAtlasOctahedralDepth.EnsureTextureSlot(out bool isUploadNeeded, out volume.GetPipelineData().OctahedralDepthScaleBias, key, width, height);

            if (isSlotAllocated)
            {
                if (isUploadNeeded)
                {
                    if (!volume.IsDataAssigned() || !volume.IsAssetCompatible())
                    {
                        ReleaseProbeVolumeFromAtlas(volume);
                        return false;
                    }

                    Debug.AssertFormat(volume.DataOctahedralDepthLength == size, "ProbeVolume: The probe volume baked data and its resolution are out of sync! Volume data length is {0}, but resolution size is {1}.", volume.DataOctahedralDepthLength, size);

                    if (size > s_MaxProbeVolumeProbeOctahedralDepthCount)
                    {
                        Debug.LogWarningFormat("ProbeVolume: probe volume octahedral depth baked data size exceeds the currently max supported blitable size. Volume data size is {0}, but s_MaxProbeVolumeProbeCount is {1}. Please decrease ProbeVolume resolution, or increase ProbeVolumeLighting.s_MaxProbeVolumeProbeCount.", size, s_MaxProbeVolumeProbeOctahedralDepthCount);
                        return false;
                    }

                    // Ready to upload: prepare parameters and buffers
                    UploadOctahedralDepthParameters parameters = new UploadOctahedralDepthParameters()
                    {
                        volume = volume,

                        targetAtlasWidth = m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                        targetAtlasHeight = m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height,

                        targetAtlasSize = size
                    };

                    volume.SetDataOctahedralDepth(s_ProbeVolumeAtlasOctahedralDepthBuffer);

                    // Execute upload
                    if (m_EnableRenderGraph)
                    {
                        using(var builder = renderGraph.AddRenderPass<UploadOctahedralDepthPassData>("Upload Octahedral Depth", out var passData))
                        {
                            // Parameters
                            passData.parameters = parameters;

                            // Resources
                            passData.uploadBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(s_ProbeVolumeAtlasOctahedralDepthBuffer));

                            passData.targetAtlas = builder.WriteTexture(rgResources.octahedralDepthAtlas);

                            builder.SetRenderFunc((UploadOctahedralDepthPassData passData, RenderGraphContext context) =>
                                UploadOctahedralDepthToAtlas(
                                    passData.parameters,
                                    context.cmd,
                                    passData.uploadBuffer,
                                    passData.targetAtlas));
                        }
                    }
                    else
                    {
                        UploadOctahedralDepthToAtlas(
                            parameters,
                            immediateCmd,
                            s_ProbeVolumeAtlasOctahedralDepthBuffer,
                            m_ProbeVolumeAtlasOctahedralDepthRTHandle);
                    }

                    return true;
                }
                return false;
            }

            if (!isSlotAllocated)
            {
                Debug.LogWarning($"ProbeVolume: Texture Atlas failed to allocate space for octahedral depth texture id: {key.id}, width: {width}, height: {height}, rotation: {key.rotation.eulerAngles}");
            }

            return false;
        }

        private static void UploadOctahedralDepthToAtlas(
            UploadOctahedralDepthParameters parameters,
            CommandBuffer cmd,
            ComputeBuffer uploadBuffer,
            RenderTargetIdentifier targetAtlas)
        {
            ProbeVolumeHandle volume = parameters.volume;

            //Debug.Log("Uploading Probe Volume Data with key " + key + " at scale bias = " + volume.parameters.scaleBias);
            cmd.SetComputeIntParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthReadBufferCount,
                parameters.targetAtlasSize / 2);

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
                volume.GetPipelineData().OctahedralDepthScaleBias
            );

            cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthResolutionAndInverse, new Vector4(
                parameters.targetAtlasWidth,
                parameters.targetAtlasHeight,
                1.0f / (float)parameters.targetAtlasWidth,
                1.0f / (float)parameters.targetAtlasHeight
            ));

            cmd.SetComputeBufferParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthReadBuffer, uploadBuffer);

            cmd.SetComputeTextureParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthWriteTexture, targetAtlas);

            // TODO: Determine optimal batch size.
            const int kBatchSize = 256;
            int numThreadGroups = Mathf.CeilToInt((float)(parameters.targetAtlasSize / 2) / (float)kBatchSize); // / 2 for [mean, mean^2] (will be written to a float2 RT)
            cmd.DispatchCompute(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, numThreadGroups, 1, 1);
        }

        internal void ClearProbeVolumeAtlasIfRequested(CommandBuffer immediateCmd, RenderGraph renderGraph, ref ProbeVolumesRenderGraphResources rgResources)
        {
            if (!isClearProbeVolumeAtlasRequested) { return; }
            isClearProbeVolumeAtlasRequested = false;

            bool usesOctahedralDepth = ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth;

            probeVolumeAtlas.ResetAllocator();
            if (usesOctahedralDepth)
            {
                probeVolumeAtlasOctahedralDepth.ResetAllocator();
            }

            if (m_EnableRenderGraph)
            {
                using (var builder = renderGraph.AddRenderPass<ClearProbeVolumeAtlasesPassData>("Clear Probe Volume Atlases", out var passData))
                {
                    passData.probeVolumesAtlas = builder.WriteTexture(rgResources.probeVolumesAtlas);
                    if (usesOctahedralDepth)
                    {
                        passData.octahedralDepthAtlas = builder.WriteTexture(rgResources.octahedralDepthAtlas);
                    }

                    builder.SetRenderFunc((ClearProbeVolumeAtlasesPassData passData, RenderGraphContext context) =>
                        DoClearProbeVolumeAtlases(context.cmd, passData.probeVolumesAtlas, passData.octahedralDepthAtlas));

                }
            }
            else
            {
                DoClearProbeVolumeAtlases(immediateCmd, m_ProbeVolumeAtlasSHRTHandle, m_ProbeVolumeAtlasOctahedralDepthRTHandle);
            }
        }

        private static void DoClearProbeVolumeAtlases(
            CommandBuffer cmd,
            RenderTargetIdentifier probeVolumeAtlas,
            RenderTargetIdentifier octahedralDepthAtlas)
        {
            cmd.SetRenderTarget(probeVolumeAtlas, 0, CubemapFace.Unknown, 0);
            cmd.ClearRenderTarget(false, true, Color.black, 0.0f);

            if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
            {
                cmd.SetRenderTarget(octahedralDepthAtlas, 0, CubemapFace.Unknown, 0);
                cmd.ClearRenderTarget(false, true, Color.black, 0.0f);
            }
        }

        ProbeVolumeList PrepareVisibleProbeVolumeList(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph)
        {
            ProbeVolumeList probeVolumes = new ProbeVolumeList();

            // In the case where ShaderConfig is setup to fully disable probe volumes, all probe volume variables are stripped, so no work is needed here.
            // However, we can be in a state where ShaderConfig has enabled probe volumes, so the variables are defined, but framesettings disables probe volumes,
            // so in this case we still need to push default parameters.
            // In theory we could expose another keyword to strip out these variables when FrameSettings disables probe volumes, however we do not want to add another
            // keyword and bloat compilation times just for this edge case.
            // This edge case should only happen in practice when users are in the process of enabling probe volumes, but have not fully enabled them.
            // Otherwise, they should just update ShaderConfig to disable probe volumes completely.
            if (ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled)
            {
                if (!m_SupportProbeVolume || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                {
                    PushProbeVolumesGlobalParamsDefault(hdCamera, immediateCmd, renderGraph);
                }
                else
                {
                    PrepareVisibleProbeVolumeListBuffers(hdCamera, immediateCmd, renderGraph, ref probeVolumes);
                }
            }

            return probeVolumes;
        }

        enum ProbeVolumeDynamicGIMode
        {
            None,
            Dispatch,
            Clear
        }

        struct ProbeVolumeDynamicGICommonData
        {
            public ProbeVolumeDynamicGIMode mode;
            public List<ProbeVolumeHandle> volumes;
            public ProbeDynamicGI giSettings;
            public ShaderVariablesGlobal globalCB;
            public SphericalHarmonicsL2 ambientProbe;
            public bool infiniteBounces;
            public int propagationQuality;
            public int maxSimulationsPerFrameOverride;
            public ProbeVolumeDynamicGIMixedLightMode mixedLightMode;
        }

        class ProbeVolumeDynamicGIPassData
        {
            public ProbeVolumeDynamicGICommonData commonData;
            public TextureHandle probeVolumesAtlas;
        }

        ProbeVolumeDynamicGICommonData PrepareProbeVolumeDynamicGIData(HDCamera hdCamera)
        {
            ProbeVolumeDynamicGICommonData data = new ProbeVolumeDynamicGICommonData() { mode = ProbeVolumeDynamicGIMode.None };

            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return data;

            if (hdCamera.camera.cameraType != CameraType.Game && hdCamera.camera.cameraType != CameraType.SceneView)
                return data;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return data;

            if (!m_SupportProbeVolume)
                return data;

            data.volumes = ProbeVolumeManager.manager.GetVolumesToRender();
            data.giSettings = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>();
            data.globalCB = m_ShaderVariablesGlobalCB;
            data.ambientProbe = m_SkyManager.GetAmbientProbe(hdCamera);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolumeDynamicGI) && m_SupportDynamicGI)
            {
                data.infiniteBounces = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolumeDynamicGIInfiniteBounces);
                data.propagationQuality = hdCamera.frameSettings.probeVolumeDynamicGIPropagationQuality;
                data.maxSimulationsPerFrameOverride = hdCamera.frameSettings.probeVolumeDynamicGIMaxSimulationsPerFrame;
                data.mixedLightMode = hdCamera.frameSettings.probeVolumeDynamicGIMixedLightMode;

                data.mode = ProbeVolumeDynamicGIMode.Dispatch;
                m_WasProbeVolumeDynamicGIEnabled = true;
            }
            else if (m_WasProbeVolumeDynamicGIEnabled)
            {
                data.mode = ProbeVolumeDynamicGIMode.Clear;
                m_WasProbeVolumeDynamicGIEnabled = false;
            }

            return data;
        }

        static void ExecuteProbeVolumeDynamicGI(CommandBuffer cmd, ProbeVolumeDynamicGICommonData data, RenderTargetIdentifier probeVolumeAtlas)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProbeVolumeDynamicGI)))
            {
                if (data.mode == ProbeVolumeDynamicGIMode.Dispatch)
                {
                    // Update Probe Volume Data via Dynamic GI Propagation
                    ProbeVolumeDynamicGI.instance.ResetSimulationRequests();
                    float maxRange = Mathf.Max(data.giSettings.rangeBehindCamera.value, data.giSettings.rangeInFrontOfCamera.value);

                    // add simulation requests
                    for (int probeVolumeIndex = 0; probeVolumeIndex < data.volumes.Count; ++probeVolumeIndex)
                    {
                        ProbeVolumeHandle volume = data.volumes[probeVolumeIndex];

                        // basic distance check
                        var obb = volume.GetPipelineData().BoundingBox;
                        float maxExtent = Mathf.Max(obb.extentX, Mathf.Max(obb.extentY, obb.extentZ));
                        if (obb.center.magnitude < (maxRange + maxExtent))
                        {
                            ProbeVolumeDynamicGI.instance.AddSimulationRequest(data.volumes, probeVolumeIndex);
                        }
                    }

                    // dispatch max number of simulation requests this frame
                    var sortedRequests = ProbeVolumeDynamicGI.instance.SortSimulationRequests(data.maxSimulationsPerFrameOverride, out var numSimulationRequests);
                    for (int i = 0; i < numSimulationRequests; ++i)
                    {
                        var simulationRequest = sortedRequests[i];
                        ProbeVolumeHandle volume = data.volumes[simulationRequest.probeVolumeIndex];
                        ProbeVolumeDynamicGI.instance.DispatchProbePropagation(cmd, volume, data.giSettings,
                            in data.globalCB, probeVolumeAtlas, data.infiniteBounces,
                            (ProbeVolumeDynamicGI.PropagationQuality)data.propagationQuality, data.ambientProbe,
                            data.mixedLightMode);
                    }
                }
                else if (data.mode == ProbeVolumeDynamicGIMode.Clear)
                {
                    for (int probeVolumeIndex = 0; probeVolumeIndex < data.volumes.Count; ++probeVolumeIndex)
                    {
                        ProbeVolumeHandle volume = data.volumes[probeVolumeIndex];
                        ProbeVolumeDynamicGI.instance.ClearProbePropagation(volume);
                    }
                }
            }
        }

        void PrepareVisibleProbeVolumeListBuffers(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph, ref ProbeVolumeList probeVolumes)
        {
            var settings = hdCamera.volumeStack.GetComponent<ProbeVolumeController>();
            bool octahedralDepthOcclusionFilterIsEnabled =
                ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth
                && settings.leakMitigationMode.value == LeakMitigationMode.OctahedralDepthOcclusionFilter;

            float globalDistanceFadeStart = settings.distanceFadeStart.value;
            float globalDistanceFadeEnd = settings.distanceFadeEnd.value;

            float offscreenUploadDistance = 0.0f;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolumeDynamicGI) && m_SupportDynamicGI)
            {
                var dynamicGISettings = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>();
                offscreenUploadDistance = (dynamicGISettings.neighborVolumePropagationMode.value == ProbeDynamicGI.DynamicGINeighboringVolumePropagationMode.Disabled)
                    ? 0
                    : Mathf.Min(dynamicGISettings.rangeInFrontOfCamera.value, dynamicGISettings.rangeBehindCamera.value);
            }
            float offscreenUploadDistanceSquared = offscreenUploadDistance * offscreenUploadDistance;

            using (new ProfilingScope(immediateCmd, ProfilingSampler.Get(HDProfileId.PrepareProbeVolumeList)))
            {
                if (m_EnableRenderGraph)
                {
                    probeVolumes.rgResources.boundsBuffer = renderGraph.ImportComputeBuffer(s_VisibleProbeVolumeBoundsBuffer);
                    probeVolumes.rgResources.dataBuffer = renderGraph.ImportComputeBuffer(s_VisibleProbeVolumeDataBuffer);
                    probeVolumes.rgResources.probeVolumesAtlas = renderGraph.ImportTexture(m_ProbeVolumeAtlasSHRTHandle);
                    if (ShaderConfig.s_ProbeVolumesBilateralFilteringMode == ProbeVolumesBilateralFilteringModes.OctahedralDepth)
                    {
                        probeVolumes.rgResources.octahedralDepthAtlas = renderGraph.ImportTexture(m_ProbeVolumeAtlasOctahedralDepthRTHandle);
                    }
                }

                ClearProbeVolumeAtlasIfRequested(immediateCmd, renderGraph, ref probeVolumes.rgResources);
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
                ProbeVolumeManager.manager.UpdateVolumesToRender();
                List<ProbeVolumeHandle> volumes = ProbeVolumeManager.manager.GetVolumesToRender();

                int probeVolumesCount = Math.Min(volumes.Count, k_MaxVisibleProbeVolumeCount);
                int sortCount = 0;

                for (int i = 0; i < volumes.Count; ++i)
                {
                    ProbeVolumeHandle volume = volumes[i];
                    volume.GetPipelineData().EngineDataIndex = -1;
                }

                // Sort probe volumes smallest from smallest to largest volume.
                // Same as is done with reflection probes.
                // See LightLoop.cs::PrepareLightsForGPU() for original example of this.
                for (int probeVolumesIndex = 0; (probeVolumesIndex < volumes.Count) && (sortCount < probeVolumesCount); probeVolumesIndex++)
                {
                    ProbeVolumeHandle volume = volumes[probeVolumesIndex];

                    bool blendModeIsSupported = !((ShaderConfig.s_ProbeVolumesAdditiveBlending == 0) && volume.parameters.volumeBlendMode != VolumeBlendMode.Normal);
                    float probeVolumeDepthFromCameraWS = Vector3.Dot(hdCamera.camera.transform.forward, volume.position - camPosition);
                    bool isVisible = probeVolumeDepthFromCameraWS < Mathf.Min(globalDistanceFadeEnd, volume.parameters.distanceFadeEnd);
                    isVisible &= volume.parameters.weight >= 1e-5f;

                    bool isNeeded = false;
                    if (
#if UNITY_EDITOR
                        volume.IsAssetCompatible() &&
                        !volume.IsHiddesInScene() &&
#endif
                        volume.IsDataAssigned() &&
                        blendModeIsSupported &&
                        isVisible
                    )
                    {
                        // TODO: cache these?
                        var obb = volume.ConstructOBBEngineData(camOffset);

                        Vector3 radialOffset = (ShaderConfig.s_CameraRelativeRendering != 0) ? obb.center : (obb.center - camPosition);
                        float radialDistanceSquared = Vector3.Dot(radialOffset, radialOffset);

                        // Frustum cull on the CPU for now. TODO: do it on the GPU.
                        if (GeometryUtils.Overlap(obb, hdCamera.frustum, hdCamera.frustum.planes.Length, hdCamera.frustum.corners.Length)
                            || (offscreenUploadDistanceSquared > radialDistanceSquared))
                        {
                            var logVolume = CalculateProbeVolumeLogVolume(volume.parameters.size);

                            m_ProbeVolumeSortKeys[sortCount++] = PackProbeVolumeSortKey(volume.parameters.volumeBlendMode, logVolume, probeVolumesIndex);

                            isNeeded = true;
                        }
                    }

                    if (!isNeeded)
                    {
                        ReleaseProbeVolumeFromAtlas(volume);
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

                    ref var pipelineData = ref volume.GetPipelineData();
                    var obb = volume.ConstructOBBEngineData(camOffset);
                    var data = volume.parameters.ConvertToEngineData(pipelineData, m_ProbeVolumeAtlasSHRTDepthSliceCount, globalDistanceFadeStart, globalDistanceFadeEnd);

                    pipelineData.EngineDataIndex = m_VisibleProbeVolumeData.Count;
                    pipelineData.BoundingBox = obb;
                    pipelineData.EngineData = data;

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

                    EnsureStaleDataIsFlushedFromAtlases(volume, octahedralDepthOcclusionFilterIsEnabled);

                    if (volumeUploadedToAtlasSHCount < volumeUploadedToAtlasSHCapacity)
                    {
                        bool volumeWasUploaded = EnsureProbeVolumeInAtlas(immediateCmd, renderGraph, ref probeVolumes.rgResources, volume);
                        if (volumeWasUploaded)
                            ++volumeUploadedToAtlasSHCount;
                    }

                    if (volumeUploadedToAtlasOctahedralDepthCount < volumeUploadedToAtlasOctahedralDepthCapacity)
                    {
                        bool volumeWasUploaded = EnsureProbeVolumeInAtlasOctahedralDepth(immediateCmd, renderGraph, ref probeVolumes.rgResources, volume);
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

                PushProbeVolumesGlobalParams(hdCamera, immediateCmd, renderGraph, ref probeVolumes.rgResources);

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
            public bool supportProbeVolume;
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

            parameters.supportProbeVolume = m_SupportProbeVolume;

            parameters.material = m_DebugDisplayProbeVolumeMaterial;

            parameters.sliceMode = (int)lightingDebug.probeVolumeAtlasSliceMode;
            parameters.validRange = new Vector4(lightingDebug.probeVolumeMinValue, 1.0f / (lightingDebug.probeVolumeMaxValue - lightingDebug.probeVolumeMinValue));
            parameters.textureViewScale = new Vector3(1.0f, 1.0f, 1.0f);
            parameters.textureViewBias = new Vector3(0.0f, 0.0f, 0.0f);
            parameters.textureViewResolution = new Vector3(s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution, s_ProbeVolumeAtlasResolution);
            parameters.atlasTextureOctahedralDepthScaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);

            if (!m_SupportProbeVolume) { return parameters; }

#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != null)
            {
                var selectedProbeVolume = UnityEditor.Selection.activeGameObject.GetComponent<ProbeVolume>();
                if (selectedProbeVolume != null)
                {
                    // User currently has a probe volume selected.
                    // Compute a scaleBias term so that atlas view automatically zooms into selected probe volume.
                    ProbeVolume.ProbeVolumeAtlasKey selectedProbeVolumeKey = selectedProbeVolume.ComputeProbeVolumeAtlasKey();
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
                        if (probeVolumeAtlasOctahedralDepth.TryGetScaleBias(out Vector4 selectedProbeVolumeOctahedralDepthScaleBias, selectedProbeVolumeKey))
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
            if (!parameters.supportProbeVolume) { return; }

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

#if UNITY_EDITOR
        internal void DrawProbeVolumeDebugSHPreview(ProbeVolume probeVolume, Camera camera)
        {
            if (!m_SupportProbeVolume) { return; }

            Material debugMaterial = GetDebugSHPreviewMaterial();
            if (debugMaterial == null) { return; }

            MaterialPropertyBlock debugMaterialPropertyBlock = GetDebugSHPreviewMaterialPropertyBlock();
            debugMaterialPropertyBlock.SetVector("_ProbeVolumeResolution", new Vector3(probeVolume.parameters.resolutionX, probeVolume.parameters.resolutionY, probeVolume.parameters.resolutionZ));
            debugMaterialPropertyBlock.SetMatrix("_ProbeIndex3DToPositionWSMatrix", ProbeVolume.ComputeProbeIndex3DToPositionWSMatrix(probeVolume));
            debugMaterialPropertyBlock.SetFloat("_ProbeVolumeProbeDisplayRadiusWS", Gizmos.probeSize);

            bool probeVolumeIsResidentInAtlas = probeVolumeAtlas.TryGetScaleBias(out Vector3 probeVolumeScaleUnused, out Vector3 probeVolumeBias, probeVolume.ComputeProbeVolumeAtlasKey());
            if (probeVolumeIsResidentInAtlas)
            {
                // Note: The system is not aware of slice packing in Z.
                // Need to modify scale and bias terms just before uploading to GPU.
                // TODO: Should we make it aware earlier up the chain?
                probeVolumeBias.z /= m_ProbeVolumeAtlasSHRTDepthSliceCount;
            }
            else
            {
                probeVolumeBias = Vector3.zero;
            }
            Vector3 probeVolumeBiasTexels = new Vector3(Mathf.Round(probeVolumeBias.x * s_ProbeVolumeAtlasResolution), Mathf.Round(probeVolumeBias.y * s_ProbeVolumeAtlasResolution), Mathf.Round(probeVolumeBias.z * s_ProbeVolumeAtlasResolution * m_ProbeVolumeAtlasSHRTDepthSliceCount));

            debugMaterialPropertyBlock.SetVector("_ProbeVolumeAtlasBiasTexels", probeVolumeBiasTexels);
            debugMaterialPropertyBlock.SetInt("_ProbeVolumeIsResidentInAtlas", probeVolumeIsResidentInAtlas ? 1 : 0);
            debugMaterialPropertyBlock.SetInt("_ProbeVolumeHighlightNegativeRinging", probeVolume.parameters.highlightRinging ? 1 : 0);
            debugMaterialPropertyBlock.SetInt("_ProbeVolumeDrawValidity", probeVolume.parameters.drawValidity ? 1 : 0);
            debugMaterial.SetPass(0);
            Graphics.DrawProcedural(debugMaterial, ProbeVolume.ComputeBoundsWS(probeVolume), MeshTopology.Triangles, 3 * 2 * ProbeVolume.ComputeProbeCount(probeVolume), 1, camera, debugMaterialPropertyBlock, ShadowCastingMode.Off, receiveShadows: false, layer: 0);
        }
#endif

        public ProbeVolumeAtlasStats GetProbeVolumeAtlasStats()
        {
            if (!m_SupportProbeVolume) { return new ProbeVolumeAtlasStats(); }

            return new ProbeVolumeAtlasStats
            {
                allocationCount = (probeVolumeAtlas != null && m_SupportProbeVolume) ? probeVolumeAtlas.GetAllocationCount() : 0,
                allocationRatio = (probeVolumeAtlas != null && m_SupportProbeVolume) ? probeVolumeAtlas.GetAllocationRatio() : 0,
                largestFreeBlockRatio = (probeVolumeAtlas != null && m_SupportProbeVolume) ? probeVolumeAtlas.FindLargestFreeBlockRatio() : 0.0f,
                largestFreeBlockPixels = (probeVolumeAtlas != null && m_SupportProbeVolume) ? probeVolumeAtlas.FindLargestFreeBlockPixels() : Vector3Int.zero
            };
        }

    } // class ProbeVolumeLighting
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
