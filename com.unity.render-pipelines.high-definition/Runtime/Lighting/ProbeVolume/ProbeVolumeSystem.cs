using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'ProbeVolumeArtistParameters'.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    public struct ProbeVolumeEngineData
    {
        public Vector3 debugColor;
        public int     payloadIndex;
        public Vector3 rcpPosFaceFade;
        public Vector3 rcpNegFaceFade;
        public float   rcpDistFadeLen;
        public float   endTimesRcpDistFadeLen;
        public Vector4 scaleBias;
        public Vector4 octahedralDepthScaleBias;
        public Vector3 resolution;
        public Vector3 resolutionInverse;
        public int     volumeBlendMode;

        public static ProbeVolumeEngineData GetNeutralValues()
        {
            ProbeVolumeEngineData data;

            data.debugColor = Vector3.zero;
            data.payloadIndex  = -1;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpDistFadeLen = 0;
            data.endTimesRcpDistFadeLen = 1;
            data.scaleBias = Vector4.zero;
            data.octahedralDepthScaleBias = Vector4.zero;
            data.resolution = Vector3.zero;
            data.resolutionInverse = Vector3.zero;
            data.volumeBlendMode = 0;

            return data;
        }
    }

    [Serializable]
    [GenerateHLSL]
    public struct SphericalHarmonicsL1
    {
        public Vector4 shAr;
        public Vector4 shAg;
        public Vector4 shAb;

        public static SphericalHarmonicsL1 GetNeutralValues()
        {
            SphericalHarmonicsL1 sh;
            sh.shAr = Vector4.zero;
            sh.shAg = Vector4.zero;
            sh.shAb = Vector4.zero;
            return sh;
        }
    }

    [GenerateHLSL]
    public enum LeakMitigationMode
    {
        NormalBias = 0,
        GeometricFilter,
        ProbeValidityFilter
    }

    public struct ProbeVolumeList
    {
        public List<OrientedBBox> bounds;
        public List<ProbeVolumeEngineData> data;
    }

    public class ProbeVolumeSystem
    {
        public enum ProbeVolumeSystemPreset
        {
            Off,
            On,
            Count
        }

        public ProbeVolumeSystemPreset preset = ProbeVolumeSystemPreset.Off;

        List<OrientedBBox> m_VisibleProbeVolumeBounds = null;
        List<ProbeVolumeEngineData> m_VisibleProbeVolumeData = null;
        public const int k_MaxVisibleProbeVolumeCount = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        static ComputeBuffer s_VisibleProbeVolumeBoundsBuffer = null;
        static ComputeBuffer s_VisibleProbeVolumeDataBuffer = null;
        static ComputeBuffer s_VisibleProbeVolumeBoundsBufferDefault = null;
        static ComputeBuffer s_VisibleProbeVolumeDataBufferDefault = null;

        // Is the feature globally disabled?
        bool m_SupportProbeVolume = false;

        // Pre-allocate sort keys array to max size to avoid creating allocations / garbage at runtime.
        uint[] m_SortKeys = new uint[k_MaxVisibleProbeVolumeCount];

        static ComputeShader s_ProbeVolumeAtlasBlitCS = null;
        static ComputeShader s_ProbeVolumeAtlasOctahedralDepthBlitCS = null;
        static int s_ProbeVolumeAtlasBlitKernel = -1;
        static int s_ProbeVolumeAtlasOctahedralDepthBlitKernel = -1;
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataBuffer = null;
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataValidityBuffer = null;
        static ComputeBuffer s_ProbeVolumeAtlasOctahedralDepthBuffer = null;
        static int s_ProbeVolumeAtlasWidth = 1024;
        static int s_ProbeVolumeAtlasHeight = 1024;
        static int s_ProbeVolumeAtlasOctahedralDepthWidth = 2048;
        static int s_ProbeVolumeAtlasOctahedralDepthHeight = 2048;
        static int k_MaxProbeVolumeAtlasOctahedralDepthProbeCount = (s_ProbeVolumeAtlasOctahedralDepthWidth / 8) * (s_ProbeVolumeAtlasOctahedralDepthHeight / 8);
        public const int k_ProbeOctahedralDepthWidth = 8;
        public const int k_ProbeOctahedralDepthHeight = 8;

        // TODO: Preallocating compute buffer for this worst case of a single probe volume that consumes the whole atlas is a memory hog.
        // May want to look at dynamic resizing of compute buffer based on use, or more simply, slicing it up across multiple dispatches for massive volumes.
        // With current settings this compute buffer will take  1024 * 1024 * sizeof(float) * coefficientCount (12) bytes ~= 50.3 MB.
        static int s_MaxProbeVolumeProbeCount = 1024 * 1024;
        RTHandle m_ProbeVolumeAtlasSHRTHandle;
        int m_ProbeVolumeAtlasSHRTDepthSliceCount = 4; // one texture per [RGB] SH coefficients + one texture for float4(validity, unassigned, unassigned, unassigned).
        Texture2DAtlasDynamic probeVolumeAtlas = null;

        RTHandle m_ProbeVolumeAtlasOctahedralDepthRTHandle;
        Texture2DAtlasDynamic probeVolumeAtlasOctahedralDepth = null;
        bool isClearProbeVolumeAtlasRequested = false;

        public void Build(HDRenderPipelineAsset asset)
        {
            m_SupportProbeVolume = asset.currentPlatformRenderPipelineSettings.supportProbeVolume;

            s_ProbeVolumeAtlasWidth = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasWidth;
            s_ProbeVolumeAtlasHeight = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasHeight;
            s_MaxProbeVolumeProbeCount = s_ProbeVolumeAtlasWidth * s_ProbeVolumeAtlasHeight;

            s_ProbeVolumeAtlasOctahedralDepthWidth = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthWidth;
            s_ProbeVolumeAtlasOctahedralDepthHeight = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasOctahedralDepthHeight;
            k_MaxProbeVolumeAtlasOctahedralDepthProbeCount = (s_ProbeVolumeAtlasOctahedralDepthWidth / 8) * (s_ProbeVolumeAtlasOctahedralDepthHeight / 8);

            preset = m_SupportProbeVolume ? ProbeVolumeSystemPreset.On : ProbeVolumeSystemPreset.Off;

            if (preset != ProbeVolumeSystemPreset.Off)
            {
                CreateBuffers();

                s_ProbeVolumeAtlasBlitCS = asset.renderPipelineResources.shaders.probeVolumeAtlasBlitCS;
                s_ProbeVolumeAtlasBlitKernel = s_ProbeVolumeAtlasBlitCS.FindKernel("ProbeVolumeAtlasBlitKernel");

                s_ProbeVolumeAtlasOctahedralDepthBlitCS = asset.renderPipelineResources.shaders.probeVolumeAtlasOctahedralDepthBlitCS;
                s_ProbeVolumeAtlasOctahedralDepthBlitKernel = s_ProbeVolumeAtlasOctahedralDepthBlitCS.FindKernel("ProbeVolumeAtlasOctahedralDepthBlitKernel");
            }
            else
            {
                CreateBuffersDefault();
            }

        #if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared += OnLightingDataCleared;
        #endif
        }

        void CreateBuffersDefault()
        {
            s_VisibleProbeVolumeBoundsBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleProbeVolumeDataBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(ProbeVolumeEngineData)));
        }

        void CreateBuffers()
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
                slices: m_ProbeVolumeAtlasSHRTDepthSliceCount,
                dimension: TextureDimension.Tex2DArray,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,//GraphicsFormat.B10G11R11_UFloatPack32,
                enableRandomWrite: true,
                useMipMap: false,
                name: "ProbeVolumeAtlasSH"
            );

            probeVolumeAtlas = new Texture2DAtlasDynamic(s_ProbeVolumeAtlasWidth, s_ProbeVolumeAtlasHeight, k_MaxVisibleProbeVolumeCount, m_ProbeVolumeAtlasSHRTHandle);

            // TODO: (Nick): Might be able drop precision down to half-floats, since we only need to encode depth data up to one probe spacing distance away. Could rescale depth data to this range before encoding.
            m_ProbeVolumeAtlasOctahedralDepthRTHandle = RTHandles.Alloc(
                width: s_ProbeVolumeAtlasOctahedralDepthWidth,
                height: s_ProbeVolumeAtlasOctahedralDepthHeight,
                slices: 1,
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
                enableRandomWrite: true,
                useMipMap: false,
                name: "ProbeVolumeAtlasOctahedralDepth"
            );

            probeVolumeAtlasOctahedralDepth = new Texture2DAtlasDynamic(
                s_ProbeVolumeAtlasOctahedralDepthWidth,
                s_ProbeVolumeAtlasOctahedralDepthHeight,
                k_MaxVisibleProbeVolumeCount,
                m_ProbeVolumeAtlasOctahedralDepthRTHandle
            );
        }

        void DestroyBuffers()
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

        public void Cleanup()
        {
            DestroyBuffers();

        #if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared -= OnLightingDataCleared;
        #endif
        }

        protected void OnLightingDataCleared()
        {
            // User requested all lighting data to be cleared.
            // Clear out all block allocations in atlas, and clear out texture data.
            // Clearing out texture data is not strictly necessary,
            // but it makes the display atlas debug view more readable.
            // Note: We do this lazily, in order to trigger the clear during the
            // next frame's render loop on the command buffer.
            isClearProbeVolumeAtlasRequested = true;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            if (!m_SupportProbeVolume)
            {
                ProbeVolumeSystem.PushGlobalParamsDefault(hdCamera, cmd, frameIndex);
                return;
            }

            cmd.SetGlobalInt(HDShaderIDs._EnableProbeVolumes, hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) ? 1 : 0);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBuffer);
            cmd.SetGlobalInt(HDShaderIDs._ProbeVolumeCount, m_VisibleProbeVolumeBounds.Count);
            cmd.SetGlobalTexture("_ProbeVolumeAtlasSH", m_ProbeVolumeAtlasSHRTHandle);
            cmd.SetGlobalVector("_ProbeVolumeAtlasResolutionAndInverse", new Vector4(
                    m_ProbeVolumeAtlasSHRTHandle.rt.width,
                    m_ProbeVolumeAtlasSHRTHandle.rt.height,
                    1.0f / (float)m_ProbeVolumeAtlasSHRTHandle.rt.width,
                    1.0f / (float)m_ProbeVolumeAtlasSHRTHandle.rt.height
            ));
            cmd.SetGlobalTexture("_ProbeVolumeAtlasOctahedralDepth", m_ProbeVolumeAtlasOctahedralDepthRTHandle);
            cmd.SetGlobalVector("_ProbeVolumeAtlasOctahedralDepthResolutionAndInverse", new Vector4(
                m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height,
                1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height
            ));

            var settings = VolumeManager.instance.stack.GetComponent<ProbeVolumeController>();
            LeakMitigationMode leakMitigationMode = (settings == null)
                ? LeakMitigationMode.NormalBias
                : settings.leakMitigationMode.value;
            float normalBiasWS = (settings == null) ? 0.0f : settings.normalBiasWS.value;
            float bilateralFilterWeight = (settings == null) ? 0.0f : settings.bilateralFilterWeight.value;
            if (leakMitigationMode != LeakMitigationMode.NormalBias)
            {
                normalBiasWS = 0.0f;

                if (bilateralFilterWeight < 1e-5f)
                {
                    // If bilateralFilterWeight is effectively zero, then we are simply doing trilinear filtering.
                    // In this case we can avoid the performance cost of computing our bilateral filter entirely.
                    leakMitigationMode = LeakMitigationMode.NormalBias;
                }
            }

            cmd.SetGlobalInt("_ProbeVolumeLeakMitigationMode", (int)leakMitigationMode);
            cmd.SetGlobalFloat("_ProbeVolumeNormalBiasWS", normalBiasWS);
            cmd.SetGlobalFloat("_ProbeVolumeBilateralFilterWeightMin", 1e-5f);
            cmd.SetGlobalFloat("_ProbeVolumeBilateralFilterWeight", bilateralFilterWeight);
        }

        private static void PushGlobalParamsDefault(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            cmd.SetGlobalInt(HDShaderIDs._EnableProbeVolumes, 0);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBufferDefault);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBufferDefault);
            cmd.SetGlobalInt(HDShaderIDs._ProbeVolumeCount, 0);
            cmd.SetGlobalTexture("_ProbeVolumeAtlasSH", TextureXR.GetBlackTexture());
            cmd.SetGlobalTexture("_ProbeVolumeAtlasOctahedralDepth", TextureXR.GetBlackTexture());
            cmd.SetGlobalInt("_ProbeVolumeLeakMitigationMode", (int)LeakMitigationMode.NormalBias);
            cmd.SetGlobalFloat("_ProbeVolumeNormalBiasWS", 0.0f);
            cmd.SetGlobalFloat("_ProbeVolumeBilateralFilterWeightMin", 0.0f);
            cmd.SetGlobalFloat("_ProbeVolumeBilateralFilterWeight", 0.0f);
        }

        public void ReleaseProbeVolumeFromAtlas(ProbeVolume volume)
        {
            int key = volume.GetID();
            // Debug.Log(probeVolumeAtlas.DebugStringFromRoot());
            probeVolumeAtlas.ReleaseTextureSlot(key);
            // Debug.Log(probeVolumeAtlas.DebugStringFromRoot());

            probeVolumeAtlasOctahedralDepth.ReleaseTextureSlot(key);
        }

        private bool EnsureProbeVolumeInAtlas(ScriptableRenderContext renderContext, CommandBuffer cmd, ProbeVolume volume)
        {
            int key = volume.GetID();
            int width = volume.parameters.resolutionX * volume.parameters.resolutionZ;
            int height = volume.parameters.resolutionY;
            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ;
            Debug.Assert(size > 0, "Error: ProbeVolumeSystem: Encountered probe volume with resolution set to zero on all three axes.");

            // TODO: Store volume resolution inside the atlas's key->bias dictionary.
            // If resolution has changed since upload, need to free previous allocation from atlas,
            // and attempt to allocate a new chunk from the atlas for the new resolution settings.
            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = probeVolumeAtlas.EnsureTextureSlot(out bool isUploadNeeded, out volume.parameters.scaleBias, key, width, height);

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
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, "_ProbeVolumeResolution", new Vector3(
                        volume.parameters.resolutionX,
                        volume.parameters.resolutionY,
                        volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, "_ProbeVolumeResolutionInverse", new Vector3(
                        1.0f / (float)volume.parameters.resolutionX,
                        1.0f / (float)volume.parameters.resolutionY,
                        1.0f / (float)volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, "_ProbeVolumeAtlasScaleBias",
                        volume.parameters.scaleBias
                    );
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasBlitCS, "_ProbeVolumeAtlasResolutionAndInverse", new Vector4(
                        m_ProbeVolumeAtlasSHRTHandle.rt.width,
                        m_ProbeVolumeAtlasSHRTHandle.rt.height,
                        1.0f / (float)m_ProbeVolumeAtlasSHRTHandle.rt.width,
                        1.0f / (float)m_ProbeVolumeAtlasSHRTHandle.rt.height
                    ));

                    Debug.Assert(data.Length == size, "ProbeVolumeSystem: The probe volume baked data and its resolution are out of sync! Volume data length is " + data.Length + ", but resolution size is " + size + ".");

                    s_ProbeVolumeAtlasBlitDataBuffer.SetData(data);
                    s_ProbeVolumeAtlasBlitDataValidityBuffer.SetData(dataValidity);
                    cmd.SetComputeIntParam(s_ProbeVolumeAtlasBlitCS, "_ProbeVolumeAtlasReadBufferCount", size);
                    cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, "_ProbeVolumeAtlasReadBuffer", s_ProbeVolumeAtlasBlitDataBuffer);
                    cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, "_ProbeVolumeAtlasReadValidityBuffer", s_ProbeVolumeAtlasBlitDataValidityBuffer);
                    cmd.SetComputeTextureParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, "_ProbeVolumeAtlasWriteTextureSH", m_ProbeVolumeAtlasSHRTHandle);

                    // TODO: Determine optimal batch size.
                    const int kBatchSize = 256;
                    int numThreadGroups = Mathf.CeilToInt((float)size / (float)kBatchSize);
                    cmd.DispatchCompute(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, numThreadGroups, 1, 1);
                    return true;

                }
                return false;
            }

            Debug.Assert(isSlotAllocated, "ProbeVolumeSystem: Texture Atlas failed to allocate space for texture { key: " + key + "width: " + width + ", height: " + height);
            return false;
        }

        private bool EnsureProbeVolumeInAtlasOctahedralDepth(ScriptableRenderContext renderContext, CommandBuffer cmd, ProbeVolume volume)
        {
            int key = volume.GetID();
            int width = volume.parameters.resolutionX * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth;
            int height = volume.parameters.resolutionY * k_ProbeOctahedralDepthHeight;
            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ * k_ProbeOctahedralDepthWidth * k_ProbeOctahedralDepthHeight;
            Debug.Assert(size > 0, "Error: ProbeVolumeSystem: Encountered probe volume with resolution set to zero on all three axes.");

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

                    //Debug.Log("Uploading Probe Volume Data with key " + key + " at scale bias = " + volume.parameters.scaleBias);
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, "_ProbeVolumeResolution", new Vector3(
                        volume.parameters.resolutionX,
                        volume.parameters.resolutionY,
                        volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, "_ProbeVolumeResolutionInverse", new Vector3(
                        1.0f / (float)volume.parameters.resolutionX,
                        1.0f / (float)volume.parameters.resolutionY,
                        1.0f / (float)volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, "_ProbeVolumeAtlasOctahedralDepthScaleBias",
                        volume.parameters.octahedralDepthScaleBias
                    );
                    cmd.SetComputeVectorParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, "_ProbeVolumeAtlasOctahedralDepthResolutionAndInverse", new Vector4(
                        m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                        m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height,
                        1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.width,
                        1.0f / (float)m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt.height
                    ));

                    Debug.Assert(dataOctahedralDepth.Length == size, "ProbeVolumeSystem: The probe volume baked data and its resolution are out of sync! Volume data length is " + dataOctahedralDepth.Length + ", but resolution size is " + size + ".");

                    s_ProbeVolumeAtlasOctahedralDepthBuffer.SetData(dataOctahedralDepth);
                    cmd.SetComputeIntParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, "_ProbeVolumeAtlasOctahedralDepthReadBufferCount", size);
                    cmd.SetComputeBufferParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, "_ProbeVolumeAtlasOctahedralDepthReadBuffer", s_ProbeVolumeAtlasOctahedralDepthBuffer);
                    cmd.SetComputeTextureParam(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, "_ProbeVolumeAtlasOctahedralDepthWriteTexture", m_ProbeVolumeAtlasOctahedralDepthRTHandle);

                    // TODO: Determine optimal batch size.
                    const int kBatchSize = 256;
                    int numThreadGroups = Mathf.CeilToInt((float)size / (float)kBatchSize);
                    cmd.DispatchCompute(s_ProbeVolumeAtlasOctahedralDepthBlitCS, s_ProbeVolumeAtlasOctahedralDepthBlitKernel, numThreadGroups, 1, 1);
                    return true;

                }
                return false;
            }

            Debug.Assert(isSlotAllocated, "ProbeVolumeSystem: Texture Atlas failed to allocate space for texture { key: " + key + "width: " + width + ", height: " + height);
            return false;
        }

        private void ClearProbeVolumeAtlasIfRequested(CommandBuffer cmd)
        {
            if (!isClearProbeVolumeAtlasRequested) { return; }
            isClearProbeVolumeAtlasRequested = false;

            probeVolumeAtlas.ResetAllocator();
            for (int depthSlice = 0; depthSlice < m_ProbeVolumeAtlasSHRTDepthSliceCount; ++depthSlice)
            {
                cmd.SetRenderTarget(m_ProbeVolumeAtlasSHRTHandle.rt, 0, CubemapFace.Unknown, depthSlice);
                cmd.ClearRenderTarget(false, true, Color.black, 0.0f);
            }

            probeVolumeAtlasOctahedralDepth.ResetAllocator();
            cmd.SetRenderTarget(m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt, 0, CubemapFace.Unknown, 0);
            cmd.ClearRenderTarget(false, true, Color.black, 0.0f);
        }

        public ProbeVolumeList PrepareVisibleProbeVolumeList(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd)
        {
            ProbeVolumeList probeVolumes = new ProbeVolumeList();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return probeVolumes;

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

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, hdCamera.frustum.planes.Length, hdCamera.frustum.corners.Length))
                    {
                        var logVolume = CalculateProbeVolumeLogVolume(volume.parameters.size);

                        m_SortKeys[sortCount++] = PackProbeVolumeSortKey(logVolume, probeVolumesIndex);
                    }
                }

                CoreUnsafeUtils.QuickSort(m_SortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    // In 1. we have already classify and sorted the probe volume, we need to use this sorted order here
                    uint sortKey = m_SortKeys[sortIndex];
                    int probeVolumesIndex;
                    UnpackProbeVolumeSortKey(sortKey, out probeVolumesIndex);

                    ProbeVolume volume = volumes[probeVolumesIndex];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // TODO: cache these?
                    var data = volume.parameters.ConvertToEngineData();

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
                const int volumeUploadedToAtlasOctahedralDepthCapacity = 1;
                int volumeUploadedToAtlasSHCount = 0;
                int volumeUploadedToAtlasOctahedralDepthCount = 0;

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    uint sortKey = m_SortKeys[sortIndex];
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

                return probeVolumes;
            }
        }

        static float CalculateProbeVolumeLogVolume(Vector3 size)
        {
            //Notes:
            // - 1+ term is to prevent having negative values in the log result
            // - 1000* is too keep 3 digit after the dot while we truncate the result later
            // - 1048575 is 2^20-1 as we pack the result on 20bit later
            float boxVolume = 8f* size.x * size.y * size.z;
            float logVolume = Mathf.Clamp(Mathf.Log(1 + boxVolume, 1.05f)*1000, 0, 1048575);
            return logVolume;
        }

        static void UnpackProbeVolumeSortKey(uint sortKey, out int probeIndex)
        {
            const uint PROBE_VOLUME_MASK = (1 << 12) - 1;
            probeIndex = (int)(sortKey & PROBE_VOLUME_MASK);
        }

        static uint PackProbeVolumeSortKey(float logVolume, int probeVolumeIndex)
        {
            // 20 bit volume, 12 bit index
            Debug.Assert((uint)logVolume < (1 << 20));
            Debug.Assert((uint)probeVolumeIndex < (1 << 12));
            const uint PROBE_VOLUME_MASK = (1 << 12) - 1;
            return (uint)logVolume << 12 | ((uint)probeVolumeIndex & PROBE_VOLUME_MASK);
        }

        public void DisplayProbeVolumeAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, int sliceMode)
        {
            if (!m_SupportProbeVolume) { return; }
            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            Vector4 scaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
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
                    if (probeVolumeAtlas.TryGetScaleBias(out Vector4 selectedProbeVolumeScaleBias, selectedProbeVolumeKey))
                    {
                        scaleBias = selectedProbeVolumeScaleBias;
                    }
                    if (probeVolumeAtlasOctahedralDepth.TryGetScaleBias(out Vector4 selectedProbeVolumeOctahedralDepthScaleBias, selectedProbeVolumeKey))
                    {
                        atlasTextureOctahedralDepthScaleBias = selectedProbeVolumeOctahedralDepthScaleBias;
                    }
                }
            }
        #endif

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_AtlasTextureSH", m_ProbeVolumeAtlasSHRTHandle.rt);
            propertyBlock.SetVector("_TextureScaleBias", scaleBias);
            propertyBlock.SetTexture("_AtlasTextureOctahedralDepth", m_ProbeVolumeAtlasOctahedralDepthRTHandle.rt);
            propertyBlock.SetVector("_AtlasTextureOctahedralDepthScaleBias", atlasTextureOctahedralDepthScaleBias);
            propertyBlock.SetVector("_ValidRange", validRange);
            propertyBlock.SetInt("_ProbeVolumeAtlasSliceMode", sliceMode);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("ProbeVolume"), MeshTopology.Triangles, 3, 1, propertyBlock);
        }

    } // class ProbeVolumeSystem
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
