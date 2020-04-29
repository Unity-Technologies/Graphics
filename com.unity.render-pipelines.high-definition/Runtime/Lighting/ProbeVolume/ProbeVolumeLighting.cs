using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'ProbeVolumeArtistParameters'.
    // Currently 128-bytes.
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
        public int payloadIndex;
        public Vector3 bias;
        public int volumeBlendMode;
        public Vector4 octahedralDepthScaleBias;
        public Vector3 resolution;
        public uint lightLayers;
        public Vector3 resolutionInverse;
        public float unused;

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
            data.payloadIndex  = -1;
            data.bias = Vector3.zero;
            data.volumeBlendMode = 0;
            data.octahedralDepthScaleBias = Vector4.zero;
            data.resolution = Vector3.zero;
            data.lightLayers = 0;
            data.resolutionInverse = Vector3.zero;
            data.unused = 0.0f;

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
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataBuffer = null;
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataValidityBuffer = null;
        static ComputeBuffer s_ProbeVolumeAtlasOctahedralDepthBuffer = null;
        static int s_ProbeVolumeAtlasWidth;
        static int s_ProbeVolumeAtlasHeight;
        static int s_ProbeVolumeAtlasDepth;
        static int s_ProbeVolumeAtlasOctahedralDepthWidth;
        static int s_ProbeVolumeAtlasOctahedralDepthHeight;
        static int k_MaxProbeVolumeAtlasOctahedralDepthProbeCount;
        internal const int k_ProbeOctahedralDepthWidth = 8;
        internal const int k_ProbeOctahedralDepthHeight = 8;

        // TODO: Preallocating compute buffer for this worst case of a single probe volume that consumes the whole atlas is a memory hog.
        // May want to look at dynamic resizing of compute buffer based on use, or more simply, slicing it up across multiple dispatches for massive volumes.
        // With current settings this compute buffer will take  1024 * 1024 * sizeof(float) * coefficientCount (12) bytes ~= 50.3 MB.
        static int s_MaxProbeVolumeProbeCount = 1024 * 1024;
        RTHandle m_ProbeVolumeAtlasSHRTHandle;
        int m_ProbeVolumeAtlasSHRTDepthSliceCount = 4; // one texture per [RGB] SH coefficients + one texture for float4(validity, unassigned, unassigned, unassigned).
        Texture3DAtlasDynamic probeVolumeAtlas = null;

        RTHandle m_ProbeVolumeAtlasOctahedralDepthRTHandle;
        Texture2DAtlasDynamic probeVolumeAtlasOctahedralDepth = null;
        bool isClearProbeVolumeAtlasRequested = false;

        // Preallocated scratch memory for storing ambient probe packed SH coefficients, which are used as a fallback when probe volume weight < 1.0.
        static Vector4[] s_AmbientProbeFallbackPackedCoeffs = new Vector4[7];

        void InitializeProbeVolumes()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            m_SupportProbeVolume = asset.currentPlatformRenderPipelineSettings.supportProbeVolume;

            s_ProbeVolumeAtlasWidth = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasWidth;
            s_ProbeVolumeAtlasHeight = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasHeight;
            s_ProbeVolumeAtlasDepth = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasDepth;
            s_MaxProbeVolumeProbeCount = s_ProbeVolumeAtlasWidth * s_ProbeVolumeAtlasHeight * s_ProbeVolumeAtlasDepth;

            s_ProbeVolumeAtlasOctahedralDepthWidth = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthWidth;
            s_ProbeVolumeAtlasOctahedralDepthHeight = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthHeight;
            k_MaxProbeVolumeAtlasOctahedralDepthProbeCount = (s_ProbeVolumeAtlasOctahedralDepthWidth / k_ProbeOctahedralDepthWidth) * (s_ProbeVolumeAtlasOctahedralDepthHeight / k_ProbeOctahedralDepthWidth);

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
            else
            {
                CreateProbeVolumeBuffersDefault();
            }

        #if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared += OnLightingDataCleared;
        #endif
        }

        internal void CreateProbeVolumeBuffersDefault()
        {
            s_VisibleProbeVolumeBoundsBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleProbeVolumeDataBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(ProbeVolumeEngineData)));
        }

        internal void CreateProbeVolumeBuffers()
        {
            m_VisibleProbeVolumeBounds = new List<OrientedBBox>();
            m_VisibleProbeVolumeData = new List<ProbeVolumeEngineData>();
            s_VisibleProbeVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleProbeVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleProbeVolumeDataBuffer = new ComputeBuffer(k_MaxVisibleProbeVolumeCount, Marshal.SizeOf(typeof(ProbeVolumeEngineData)));
            s_ProbeVolumeAtlasBlitDataBuffer = new ComputeBuffer(s_MaxProbeVolumeProbeCount, Marshal.SizeOf(typeof(SphericalHarmonicsL1)));
            s_ProbeVolumeAtlasBlitDataValidityBuffer = new ComputeBuffer(s_MaxProbeVolumeProbeCount, Marshal.SizeOf(typeof(float)));
            s_ProbeVolumeAtlasOctahedralDepthBuffer = new ComputeBuffer(s_MaxProbeVolumeProbeCount, Marshal.SizeOf(typeof(float)));

            m_ProbeVolumeAtlasSHRTHandle = RTHandles.Alloc(
                width: s_ProbeVolumeAtlasWidth,
                height: s_ProbeVolumeAtlasHeight,
                slices: s_ProbeVolumeAtlasDepth * m_ProbeVolumeAtlasSHRTDepthSliceCount,
                dimension:         TextureDimension.Tex3D,
                colorFormat:       UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,//GraphicsFormat.B10G11R11_UFloatPack32,
                enableRandomWrite: true,
                useMipMap:         false,
                name:              "ProbeVolumeAtlasSH"
            );

            probeVolumeAtlas = new Texture3DAtlasDynamic(s_ProbeVolumeAtlasWidth, s_ProbeVolumeAtlasHeight, s_ProbeVolumeAtlasDepth, k_MaxVisibleProbeVolumeCount, m_ProbeVolumeAtlasSHRTHandle);

            // TODO: (Nick): Might be able drop precision down to half-floats, since we only need to encode depth data up to one probe spacing distance away. Could rescale depth data to this range before encoding.
            m_ProbeVolumeAtlasOctahedralDepthRTHandle = RTHandles.Alloc(
                width: s_ProbeVolumeAtlasOctahedralDepthWidth,
                height: s_ProbeVolumeAtlasOctahedralDepthHeight,
                slices: 1,
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat, // float2(mean, variance)
                enableRandomWrite: true,
                useMipMap: false,
                name: "ProbeVolumeAtlasOctahedralDepthMeanAndVariance"
            );

            probeVolumeAtlasOctahedralDepth = new Texture2DAtlasDynamic(
                s_ProbeVolumeAtlasOctahedralDepthWidth,
                s_ProbeVolumeAtlasOctahedralDepthHeight,
                k_MaxVisibleProbeVolumeCount,
                m_ProbeVolumeAtlasOctahedralDepthRTHandle
            );
        }

        internal void DestroyProbeVolumeBuffers()
        {
            CoreUtils.SafeRelease(s_VisibleProbeVolumeBoundsBufferDefault);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeDataBufferDefault);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeBoundsBuffer);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeDataBuffer);
            CoreUtils.SafeRelease(s_ProbeVolumeAtlasBlitDataBuffer);
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
            cb._ProbeVolumeNormalBiasWS = 0.0f;
            cb._ProbeVolumeBilateralFilterWeightMin = 0.0f;
            cb._ProbeVolumeBilateralFilterWeight = 0.0f;

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
                    s_ProbeVolumeAtlasWidth,
                    s_ProbeVolumeAtlasHeight,
                    s_ProbeVolumeAtlasDepth,
                    m_ProbeVolumeAtlasSHRTDepthSliceCount
            );
            cb._ProbeVolumeAtlasResolutionAndSliceCountInverse = new Vector4(
                    1.0f / (float)s_ProbeVolumeAtlasWidth,
                    1.0f / (float)s_ProbeVolumeAtlasHeight,
                    1.0f / (float)s_ProbeVolumeAtlasDepth,
                    1.0f / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount
            );
            cb._ProbeVolumeAtlasOctahedralDepthResolutionAndInverse = new Vector4(
                m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height,
                1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height
            );

            var settings = hdCamera.volumeStack.GetComponent<ProbeVolumeController>();
            LeakMitigationMode leakMitigationMode = (settings == null)
                ? LeakMitigationMode.NormalBias
                : settings.leakMitigationMode.value;
            float normalBiasWS = (settings == null) ? 0.0f : settings.normalBiasWS.value;
            float bilateralFilterWeight = (settings == null) ? 0.0f : settings.bilateralFilterWeight.value;
            if (leakMitigationMode != LeakMitigationMode.NormalBias)
            {
                if (leakMitigationMode != LeakMitigationMode.OctahedralDepthOcclusionFilter)
                {
                    normalBiasWS = 0.0f;
                }

                if (bilateralFilterWeight < 1e-5f)
                {
                    // If bilateralFilterWeight is effectively zero, then we are simply doing trilinear filtering.
                    // In this case we can avoid the performance cost of computing our bilateral filter entirely.
                    leakMitigationMode = LeakMitigationMode.NormalBias;
                }
            }

            cb._ProbeVolumeLeakMitigationMode = (int)leakMitigationMode;
            cb._ProbeVolumeNormalBiasWS = normalBiasWS;
            cb._ProbeVolumeBilateralFilterWeightMin = 1e-5f;
            cb._ProbeVolumeBilateralFilterWeight = bilateralFilterWeight;

            SphericalHarmonicsL2 ambientProbeFallbackSH = m_SkyManager.GetAmbientProbe(hdCamera);
            SphericalHarmonicMath.PackCoefficients(s_AmbientProbeFallbackPackedCoeffs, ambientProbeFallbackSH);
            for (int i = 0; i < 7; ++i)
                for (int j = 0; j < 4; ++j)
                    cb._ProbeVolumeAmbientProbeFallbackPackedCoeffs[i * 4 + j] = s_AmbientProbeFallbackPackedCoeffs[i][j];
        }

        void PushProbeVolumesGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            if (!m_SupportProbeVolume)
            {
                PushProbeVolumesGlobalParamsDefault(hdCamera, cmd);
                return;
            }

            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasSH, m_ProbeVolumeAtlasSHRTHandle);
            cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasOctahedralDepth, m_ProbeVolumeAtlasOctahedralDepthRTHandle);
        }

        internal void PushProbeVolumesGlobalParamsDefault(HDCamera hdCamera, CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBufferDefault);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBufferDefault);
            cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasSH, TextureXR.GetBlackTexture3D());
            cmd.SetGlobalTexture(HDShaderIDs._ProbeVolumeAtlasOctahedralDepth, Texture2D.blackTexture);
        }

        internal void ReleaseProbeVolumeFromAtlas(ProbeVolume volume)
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            if (!m_SupportProbeVolume)
                return;

            int key = volume.GetID();

            probeVolumeAtlas.ReleaseTextureSlot(key);
            probeVolumeAtlasOctahedralDepth.ReleaseTextureSlot(key);
        }

        internal bool EnsureProbeVolumeInAtlas(ScriptableRenderContext renderContext, CommandBuffer cmd, ProbeVolume volume)
        {
            int key = volume.GetID();
            int width = volume.parameters.resolutionX;
            int height = volume.parameters.resolutionY;
            int depth = volume.parameters.resolutionZ;

            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ;
            Debug.Assert(size > 0, "ProbeVolume: Encountered probe volume with resolution set to zero on all three axes.");

            // TODO: Store volume resolution inside the atlas's key->bias dictionary.
            // If resolution has changed since upload, need to free previous allocation from atlas,
            // and attempt to allocate a new chunk from the atlas for the new resolution settings.
            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = probeVolumeAtlas.EnsureTextureSlot(out bool isUploadNeeded, out volume.parameters.scale, out volume.parameters.bias, key, width, height, depth);

            if (isSlotAllocated)
            {
                if (isUploadNeeded || volume.dataUpdated)
                {
                    (var data, var dataValidity, var dataOctahedralDepth) = volume.GetData();

                    if (data == null || data.Length == 0 || !volume.IsAssetCompatible())
                    {
                        ReleaseProbeVolumeFromAtlas(volume);
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
                        s_ProbeVolumeAtlasWidth,
                        s_ProbeVolumeAtlasHeight,
                        s_ProbeVolumeAtlasDepth,
                        m_ProbeVolumeAtlasSHRTDepthSliceCount
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, new Vector4(
                        1.0f / (float)s_ProbeVolumeAtlasWidth,
                        1.0f / (float)s_ProbeVolumeAtlasHeight,
                        1.0f / (float)s_ProbeVolumeAtlasDepth,
                        1.0f / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount
                    ));
                    Debug.Assert(data.Length == size, "ProbeVolume: The probe volume baked data and its resolution are out of sync! Volume data length is " + data.Length + ", but resolution size is " + size + ".");
                    Debug.Assert(size < s_MaxProbeVolumeProbeCount, "ProbeVolume: probe volume baked data size exceeds the currently max supported blitable size. Volume data size is " + size + ", but s_MaxProbeVolumeProbeCount is " + s_MaxProbeVolumeProbeCount + ". Please decrease ProbeVolume resolution, or increase ProbeVolumeLighting.s_MaxProbeVolumeProbeCount.");

                    s_ProbeVolumeAtlasBlitDataBuffer.SetData(data);
                    s_ProbeVolumeAtlasBlitDataValidityBuffer.SetData(dataValidity);
                    cmd.SetComputeIntParam(s_ProbeVolumeAtlasBlitCS, HDShaderIDs._ProbeVolumeAtlasReadBufferCount, size);
                    cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, HDShaderIDs._ProbeVolumeAtlasReadBuffer, s_ProbeVolumeAtlasBlitDataBuffer);
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

            Debug.Assert(isSlotAllocated, "ProbeVolume: Texture Atlas failed to allocate space for texture { key: " + key + "width: " + width + ", height: " + height + ", depth: " + depth + "}");
            return false;
        }

        internal bool EnsureProbeVolumeInAtlasOctahedralDepth(ScriptableRenderContext renderContext, CommandBuffer cmd, ProbeVolume volume)
        {
            int key = volume.GetID();
            int width = volume.parameters.resolutionX * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth;
            int height = volume.parameters.resolutionY * k_ProbeOctahedralDepthHeight;
            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth * k_ProbeOctahedralDepthHeight;
            Debug.Assert(size > 0, "ProbeVolume: Encountered probe volume with resolution set to zero on all three axes.");

            // TODO: Store volume resolution inside the atlas's key->bias dictionary.
            // If resolution has changed since upload, need to free previous allocation from atlas,
            // and attempt to allocate a new chunk from the atlas for the new resolution settings.
            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = probeVolumeAtlasOctahedralDepth.EnsureTextureSlot(out bool isUploadNeeded, out volume.parameters.octahedralDepthScaleBias, key, width, height);

            if (isSlotAllocated)
            {
                if (isUploadNeeded || volume.dataUpdated)
                {
                    (var data, var dataValidity, var dataOctahedralDepth) = volume.GetData();

                    if (data == null || data.Length == 0 || !volume.IsAssetCompatible())
                    {
                        ReleaseProbeVolumeFromAtlas(volume);
                        return false;
                    }

                    // Blit:
                    {
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
                            s_ProbeVolumeAtlasWidth,
                            s_ProbeVolumeAtlasHeight,
                            s_ProbeVolumeAtlasDepth,
                            m_ProbeVolumeAtlasSHRTDepthSliceCount
                        ));
                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, new Vector4(
                            1.0f / (float)s_ProbeVolumeAtlasWidth,
                            1.0f / (float)s_ProbeVolumeAtlasHeight,
                            1.0f / (float)s_ProbeVolumeAtlasDepth,
                            1.0f / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount
                        ));
                        Debug.Assert(dataOctahedralDepth.Length == size, "ProbeVolume: The probe volume baked data and its resolution are out of sync! Volume data length is " + dataOctahedralDepth.Length + ", but resolution size is " + size + ".");

                        s_ProbeVolumeAtlasOctahedralDepthBuffer.SetData(dataOctahedralDepth);
                        cmd.SetComputeIntParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthReadBufferCount, size);
                        cmd.SetComputeBufferParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthReadBuffer, s_ProbeVolumeAtlasOctahedralDepthBuffer);
                        cmd.SetComputeTextureParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthWriteTexture, m_ProbeVolumeAtlasOctahedralDepthRTHandle);

                        // TODO: Determine optimal batch size.
                        const int kBatchSize = 256;
                        int numThreadGroups = Mathf.CeilToInt((float)size / (float)kBatchSize);
                        cmd.DispatchCompute(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, numThreadGroups, 1, 1);
                    }

                    // Convolve:
                    {
                        Vector4 probeVolumeAtlasOctahedralDepthScaleBiasTexels = new Vector4(
                            Mathf.Floor(volume.parameters.octahedralDepthScaleBias.x * s_ProbeVolumeAtlasOctahedralDepthWidth + 0.5f),
                            Mathf.Floor(volume.parameters.octahedralDepthScaleBias.y * s_ProbeVolumeAtlasOctahedralDepthHeight + 0.5f),
                            Mathf.Floor(volume.parameters.octahedralDepthScaleBias.z * s_ProbeVolumeAtlasOctahedralDepthWidth + 0.5f),
                            Mathf.Floor(volume.parameters.octahedralDepthScaleBias.w * s_ProbeVolumeAtlasOctahedralDepthHeight + 0.5f)
                        );

                        cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthConvolveCS, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthScaleBiasTexels,
                            probeVolumeAtlasOctahedralDepthScaleBiasTexels
                        );

                        cmd.SetComputeTextureParam(s_ProbeVolumeAtlasOctahedralDepthConvolveCS, s_ProbeVolumeAtlasOctahedralDepthConvolveKernel, HDShaderIDs._ProbeVolumeAtlasOctahedralDepthRWTexture, m_ProbeVolumeAtlasOctahedralDepthRTHandle);

                        cmd.SetComputeIntParam(s_ProbeVolumeAtlasOctahedralDepthConvolveCS, HDShaderIDs._FilterSampleCount, 16); // TODO: Expose
                        cmd.SetComputeFloatParam(s_ProbeVolumeAtlasOctahedralDepthConvolveCS, HDShaderIDs._FilterSharpness, 6.0f); // TODO: Expose

                        // Warning: This kernel numthreads must be an integer multiple of OCTAHEDRAL_DEPTH_RESOLUTION
                        // We read + write from the same texture, so any partial work would pollute / cause feedback into neighboring chunks.
                        int widthPixels = (int)(volume.parameters.octahedralDepthScaleBias.x * (float)s_ProbeVolumeAtlasOctahedralDepthWidth);
                        int heightPixels = (int)(volume.parameters.octahedralDepthScaleBias.y * (float)s_ProbeVolumeAtlasOctahedralDepthHeight);
                        int probeCountX = widthPixels / k_ProbeOctahedralDepthWidth;
                        int probeCountY = heightPixels / k_ProbeOctahedralDepthHeight;
                        Debug.Assert((k_ProbeOctahedralDepthWidth == k_ProbeOctahedralDepthHeight) && (k_ProbeOctahedralDepthWidth == 8));
                        Debug.Assert((probeCountX * k_ProbeOctahedralDepthWidth) == widthPixels);
                        Debug.Assert((probeCountY * k_ProbeOctahedralDepthHeight) == heightPixels);
                        cmd.DispatchCompute(s_ProbeVolumeAtlasOctahedralDepthConvolveCS, s_ProbeVolumeAtlasOctahedralDepthConvolveKernel, probeCountX, probeCountY, 1);
                    }
                    return true;

                }
                return false;
            }

            Debug.Assert(isSlotAllocated, "ProbeVolume: Texture Atlas failed to allocate space for octahedral depth texture { key: " + key + " width: " + width + ", height: " + height);
            return false;
        }

        internal void ClearProbeVolumeAtlasIfRequested(CommandBuffer cmd)
        {
            if (!isClearProbeVolumeAtlasRequested) { return; }
            isClearProbeVolumeAtlasRequested = false;

            probeVolumeAtlas.ResetAllocator();
            cmd.SetRenderTarget(m_ProbeVolumeAtlasSHRTHandle.rt, 0, CubemapFace.Unknown, 0);
            cmd.ClearRenderTarget(false, true, Color.black, 0.0f);

            probeVolumeAtlasOctahedralDepth.ResetAllocator();
            cmd.SetRenderTarget(m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt, 0, CubemapFace.Unknown, 0);
            cmd.ClearRenderTarget(false, true, Color.black, 0.0f);
        }

        ProbeVolumeList PrepareVisibleProbeVolumeList(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd)
        {
            ProbeVolumeList probeVolumes = new ProbeVolumeList();

            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return probeVolumes;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return probeVolumes;

            var settings = hdCamera.volumeStack.GetComponent<ProbeVolumeController>();
            bool octahedralDepthOcclusionFilterIsEnabled = settings.leakMitigationMode.value == LeakMitigationMode.OctahedralDepthOcclusionFilter;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareProbeVolumeList)))
            {
                ClearProbeVolumeAtlasIfRequested(cmd);

                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleProbeVolumeBounds.Clear();
                m_VisibleProbeVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                List<ProbeVolume> volumes = ProbeVolumeManager.manager.volumes;

                int probeVolumesCount = Math.Min(volumes.Count, k_MaxVisibleProbeVolumeCount);
                int sortCount = 0;

                // Sort probe volumes smallest from smallest to largest volume.
                // Same as is done with reflection probes.
                // See LightLoop.cs::PrepareLightsForGPU() for original example of this.
                for (int probeVolumesIndex = 0; (probeVolumesIndex < volumes.Count) && (sortCount < probeVolumesCount); probeVolumesIndex++)
                {
                    ProbeVolume volume = volumes[probeVolumesIndex];

#if UNITY_EDITOR
                    if (!volume.IsAssetCompatible())
                        continue;
#endif

                    if (ShaderConfig.s_ProbeVolumesAdditiveBlending == 0 && volume.parameters.volumeBlendMode != VolumeBlendMode.Normal)
                    {
                        // Non-normal blend mode volumes are not supported. Skip.
                        continue;
                    }

                    float probeVolumeDepthFromCameraWS = Vector3.Dot(hdCamera.camera.transform.forward, volume.transform.position - camPosition);
                    if (probeVolumeDepthFromCameraWS >= volume.parameters.distanceFadeEnd)
                    {
                        // Probe volume is completely faded out from distance fade optimization.
                        // Do not bother adding it to the list, it would evaluate to zero weight.
                        continue;
                    }

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

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

                    ProbeVolume volume = volumes[probeVolumesIndex];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

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

                    ProbeVolume volume = volumes[probeVolumesIndex];

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

                PushProbeVolumesGlobalParams(hdCamera, cmd);

                return probeVolumes;
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

        void DisplayProbeVolumeAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, int sliceMode)
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            if (!m_SupportProbeVolume)
                return;

            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            Vector3 textureViewScale = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 textureViewBias = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 textureViewResolution = new Vector3(s_ProbeVolumeAtlasWidth, s_ProbeVolumeAtlasHeight, s_ProbeVolumeAtlasDepth);
            Vector4 atlasTextureOctahedralDepthScaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);

        #if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != null)
            {
                var selectedProbeVolume = UnityEditor.Selection.activeGameObject.GetComponent<ProbeVolume>();
                if (selectedProbeVolume != null)
                {
                    // User currently has a probe volume selected.
                    // Compute a scaleBias term so that atlas view automatically zooms into selected probe volume.
                    int selectedProbeVolumeKey = selectedProbeVolume.GetID();
                    if (probeVolumeAtlas.TryGetScaleBias(out Vector3 selectedProbeVolumeScale, out Vector3 selectedProbeVolumeBias, selectedProbeVolumeKey))
                    {
                        textureViewScale = selectedProbeVolumeScale;
                        textureViewBias = selectedProbeVolumeBias;
                        textureViewResolution = new Vector3(
                            selectedProbeVolume.parameters.resolutionX,
                            selectedProbeVolume.parameters.resolutionY,
                            selectedProbeVolume.parameters.resolutionZ
                        );
                    }
                    if (probeVolumeAtlasOctahedralDepth.TryGetScaleBias(out Vector4 selectedProbeVolumeOctahedralDepthScaleBias, selectedProbeVolumeKey))
                    {
                        atlasTextureOctahedralDepthScaleBias = selectedProbeVolumeOctahedralDepthScaleBias;
                    }
                }
            }
        #endif

            // Note: The system is not aware of slice packing in Z.
            // Need to modify scale and bias terms just before uploading to GPU.
            // TODO: Should we make it aware earlier up the chain?
            textureViewScale.z = textureViewScale.z / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount;
            textureViewBias.z = textureViewBias.z / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount;

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture(HDShaderIDs._AtlasTextureSH, m_ProbeVolumeAtlasSHRTHandle.rt);
            propertyBlock.SetVector(HDShaderIDs._TextureViewScale, textureViewScale);
            propertyBlock.SetVector(HDShaderIDs._TextureViewBias, textureViewBias);
            propertyBlock.SetVector(HDShaderIDs._TextureViewResolution, textureViewResolution);
            cmd.SetGlobalVector(HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCount, new Vector4(
                s_ProbeVolumeAtlasWidth,
                        s_ProbeVolumeAtlasHeight,
                        s_ProbeVolumeAtlasDepth,
                        m_ProbeVolumeAtlasSHRTDepthSliceCount
            ));
            cmd.SetGlobalVector(HDShaderIDs._ProbeVolumeAtlasResolutionAndSliceCountInverse, new Vector4(
                    1.0f / (float)s_ProbeVolumeAtlasWidth,
                    1.0f / (float)s_ProbeVolumeAtlasHeight,
                    1.0f / (float)s_ProbeVolumeAtlasDepth,
                    1.0f / (float)m_ProbeVolumeAtlasSHRTDepthSliceCount
            ));

            propertyBlock.SetTexture(HDShaderIDs._AtlasTextureOctahedralDepth, m_ProbeVolumeAtlasOctahedralDepthRTHandle);
            propertyBlock.SetVector(HDShaderIDs._AtlasTextureOctahedralDepthScaleBias, atlasTextureOctahedralDepthScaleBias);
            propertyBlock.SetVector(HDShaderIDs._ValidRange, validRange);
            propertyBlock.SetInt(HDShaderIDs._ProbeVolumeAtlasSliceMode, sliceMode);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("ProbeVolume"), MeshTopology.Triangles, 3, 1, propertyBlock);
        }

    } // class ProbeVolumeLighting
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
