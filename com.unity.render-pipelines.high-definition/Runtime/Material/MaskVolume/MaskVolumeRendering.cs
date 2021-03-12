using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'MaskVolumeArtistParameters'.
    // Currently 128-bytes.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    internal struct MaskVolumeEngineData
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
        public int blendMode;
        public Vector3 resolution;
        public uint lightLayers;
        public Vector3 resolutionInverse;
        public float normalBiasWS;

        public static MaskVolumeEngineData GetNeutralValues()
        {
            MaskVolumeEngineData data;

            data.debugColor = Vector3.zero;
            data.weight = 0.0f;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpDistFadeLen = 0;
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.endTimesRcpDistFadeLen = 1;
            data.scale = Vector3.zero;
            data.payloadIndex  = -1;
            data.bias = Vector3.zero;
            data.blendMode = 0;
            data.resolution = Vector3.zero;
            data.lightLayers = 0;
            data.resolutionInverse = Vector3.zero;
            data.normalBiasWS = 0.0f;

            return data;
        }
    }

    struct MaskVolumeList
    {
        public List<OrientedBBox> bounds;
        public List<MaskVolumeEngineData> data;
    }

    public partial class HDRenderPipeline
    {
        List<OrientedBBox> m_VisibleMaskVolumeBounds = null;
        List<MaskVolumeEngineData> m_VisibleMaskVolumeData = null;
        internal const int k_MaxVisibleMaskVolumeCount = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        static ComputeBuffer s_VisibleMaskVolumeBoundsBuffer = null;
        static ComputeBuffer s_VisibleMaskVolumeDataBuffer = null;
        static ComputeBuffer s_VisibleMaskVolumeBoundsBufferDefault = null;
        static ComputeBuffer s_VisibleMaskVolumeDataBufferDefault = null;

        // Is the feature globally disabled?
        bool m_SupportMaskVolume = false;

        // Pre-allocate sort keys array to max size to avoid creating allocations / garbage at runtime.
        uint[] m_MaskVolumeSortKeys = new uint[k_MaxVisibleMaskVolumeCount];

        static ComputeShader s_MaskVolumeAtlasBlitCS = null;
        static int s_MaskVolumeAtlasBlitKernel = -1;
        static ComputeBuffer s_MaskVolumeAtlasBlitDataSHL0Buffer = null;
        static int s_MaskVolumeAtlasResolution;
        // TODO: Use SNorm for L1 and L2 encoding when they are supported again.
        internal const Experimental.Rendering.GraphicsFormat k_MaskVolumeAtlasFormat = Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

        static int s_MaxMaskVolumeMaskCount;
        RTHandle m_MaskVolumeAtlasSHRTHandle;

        Texture3DAtlasDynamic maskVolumeAtlas = null;

        bool isClearMaskVolumeAtlasRequested = false;

        // Preallocated scratch memory for storing ambient mask packed SH coefficients, which are used as a fallback when mask volume weight < 1.0.
        static Vector4[] s_AmbientMaskFallbackPackedCoeffs = new Vector4[7];

        void InitializeMaskVolumes()
        {
            m_SupportMaskVolume = asset.currentPlatformRenderPipelineSettings.supportMaskVolume;

            s_MaskVolumeAtlasResolution = asset.currentPlatformRenderPipelineSettings.maskVolumeSettings.atlasResolution;
            if (GetApproxMaskVolumeAtlasSizeInByte(s_MaskVolumeAtlasResolution) > HDRenderPipeline.k_MaxCacheSize)
            {
                s_MaskVolumeAtlasResolution = GetMaxMaskVolumeAtlasSizeForWeightInByte(HDRenderPipeline.k_MaxCacheSize);
            }

            // TODO: Preallocating compute buffer for this worst case of a single mask volume that consumes the whole atlas is a memory hog.
            // May want to look at dynamic resizing of compute buffer based on use, or more simply, slicing it up across multiple dispatches for massive volumes.
            s_MaxMaskVolumeMaskCount = s_MaskVolumeAtlasResolution * s_MaskVolumeAtlasResolution * s_MaskVolumeAtlasResolution;

            if (m_SupportMaskVolume)
            {
                CreateMaskVolumeBuffers();

                s_MaskVolumeAtlasBlitCS = asset.renderPipelineResources.shaders.maskVolumeAtlasBlitCS;
                s_MaskVolumeAtlasBlitKernel = s_MaskVolumeAtlasBlitCS.FindKernel("MaskVolumeAtlasBlitKernel");
            }

            // Need Default / Fallback buffers for binding in case when ShaderConfig has activated mask volume code,
            // and mask volumes has been enabled in the HDRenderPipelineAsset,
            // but mask volumes is disabled in the current camera's frame settings.
            // This can go away if we add a global keyword for using / completely stripping mask volume code per camera.
            CreateMaskVolumeBuffersDefault();
        }

        internal void CreateMaskVolumeBuffersDefault()
        {
            s_VisibleMaskVolumeBoundsBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleMaskVolumeDataBufferDefault = new ComputeBuffer(1, Marshal.SizeOf(typeof(MaskVolumeEngineData)));
        }

        // Used for displaying memory cost in HDRenderPipelineAsset UI.
        internal static long GetApproxMaskVolumeAtlasSizeInByte(int resolution)
        {
            return (long)(resolution * resolution * resolution) * (long)HDUtils.GetFormatSizeInBytes(k_MaskVolumeAtlasFormat);
        }

        internal static int GetMaxMaskVolumeAtlasSizeForWeightInByte(long weight)
        {
            int theoricalResult = Mathf.FloorToInt(Mathf.Pow(weight / (long)HDUtils.GetFormatSizeInBytes(k_MaskVolumeAtlasFormat), 1.0f / 3.0f));
            return Mathf.Clamp(theoricalResult, 1, SystemInfo.maxTextureSize);
        }

        internal void CreateMaskVolumeBuffers()
        {
            m_VisibleMaskVolumeBounds = new List<OrientedBBox>();
            m_VisibleMaskVolumeData = new List<MaskVolumeEngineData>();
            s_VisibleMaskVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleMaskVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleMaskVolumeDataBuffer = new ComputeBuffer(k_MaxVisibleMaskVolumeCount, Marshal.SizeOf(typeof(MaskVolumeEngineData)));
            s_MaskVolumeAtlasBlitDataSHL0Buffer = new ComputeBuffer(s_MaxMaskVolumeMaskCount * MaskVolumePayload.GetDataSHL0Stride() / 4, Marshal.SizeOf(4));

            m_MaskVolumeAtlasSHRTHandle = RTHandles.Alloc(
                width: s_MaskVolumeAtlasResolution,
                height: s_MaskVolumeAtlasResolution,
                slices: s_MaskVolumeAtlasResolution,
                dimension:         TextureDimension.Tex3D,
                colorFormat:       k_MaskVolumeAtlasFormat,
                enableRandomWrite: true,
                useMipMap:         false,
                name:              "MaskVolumeAtlasSH"
            );

            maskVolumeAtlas = new Texture3DAtlasDynamic(s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, k_MaxVisibleMaskVolumeCount, m_MaskVolumeAtlasSHRTHandle);
        }

        internal void DestroyMaskVolumeBuffers()
        {
            CoreUtils.SafeRelease(s_VisibleMaskVolumeBoundsBufferDefault);
            CoreUtils.SafeRelease(s_VisibleMaskVolumeDataBufferDefault);
            CoreUtils.SafeRelease(s_VisibleMaskVolumeBoundsBuffer);
            CoreUtils.SafeRelease(s_VisibleMaskVolumeDataBuffer);
            CoreUtils.SafeRelease(s_MaskVolumeAtlasBlitDataSHL0Buffer);

            if (m_MaskVolumeAtlasSHRTHandle != null)
                RTHandles.Release(m_MaskVolumeAtlasSHRTHandle);

            if (maskVolumeAtlas != null)
                maskVolumeAtlas.Release();

            m_VisibleMaskVolumeBounds = null;
            m_VisibleMaskVolumeData = null;
        }

        void CleanupMaskVolumes()
        {
            DestroyMaskVolumeBuffers();
        }

        unsafe void UpdateShaderVariablesGlobalMaskVolumesDefault(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            cb._EnableMaskVolumes = 0;
            cb._MaskVolumeCount = 0;
        }

        unsafe void UpdateShaderVariablesGlobalMaskVolumes(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            if (!m_SupportMaskVolume)
            {
                UpdateShaderVariablesGlobalMaskVolumesDefault(ref cb, hdCamera);
                return;
            }

            cb._EnableMaskVolumes = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MaskVolume) ? 1u : 0u;
            cb._MaskVolumeCount = (uint)m_VisibleMaskVolumeBounds.Count;
            cb._MaskVolumeAtlasResolutionAndSliceCount = new Vector4(
                    s_MaskVolumeAtlasResolution,
                    s_MaskVolumeAtlasResolution,
                    s_MaskVolumeAtlasResolution,
                    1.0f
            );
            cb._MaskVolumeAtlasResolutionAndSliceCountInverse = new Vector4(
                    1.0f / (float)s_MaskVolumeAtlasResolution,
                    1.0f / (float)s_MaskVolumeAtlasResolution,
                    1.0f / (float)s_MaskVolumeAtlasResolution,
                    1.0f
            );
        }

        void PushMaskVolumesGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            Debug.Assert(m_SupportMaskVolume);

            cmd.SetGlobalBuffer(HDShaderIDs._MaskVolumeBounds, s_VisibleMaskVolumeBoundsBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._MaskVolumeDatas, s_VisibleMaskVolumeDataBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._MaskVolumeAtlasSH, m_MaskVolumeAtlasSHRTHandle);
        }

        internal void PushMaskVolumesGlobalParamsDefault(HDCamera hdCamera, CommandBuffer cmd)
        {
            Debug.Assert(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MaskVolume) == false);

            cmd.SetGlobalBuffer(HDShaderIDs._MaskVolumeBounds, s_VisibleMaskVolumeBoundsBufferDefault);
            cmd.SetGlobalBuffer(HDShaderIDs._MaskVolumeDatas, s_VisibleMaskVolumeDataBufferDefault);
            cmd.SetGlobalTexture(HDShaderIDs._MaskVolumeAtlasSH, TextureXR.GetBlackTexture3D());
        }

        internal void ReleaseMaskVolumeFromAtlas(MaskVolume volume)
        {
            if (!m_SupportMaskVolume)
                return;

            int key = volume.GetID();

            maskVolumeAtlas.ReleaseTextureSlot(key);
        }

        internal bool EnsureMaskVolumeInAtlas(ScriptableRenderContext renderContext, CommandBuffer cmd, MaskVolume volume)
        {
            int key = volume.GetID();
            int width = volume.parameters.resolutionX;
            int height = volume.parameters.resolutionY;
            int depth = volume.parameters.resolutionZ;

            int size = volume.parameters.resolutionX * volume.parameters.resolutionY * volume.parameters.resolutionZ;
            Debug.Assert(size > 0, "MaskVolume: Encountered mask volume with resolution set to zero on all three axes.");

            // TODO: Store volume resolution inside the atlas's key->bias dictionary.
            // If resolution has changed since upload, need to free previous allocation from atlas,
            // and attempt to allocate a new chunk from the atlas for the new resolution settings.
            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = maskVolumeAtlas.EnsureTextureSlot(out bool isUploadNeeded, out volume.parameters.scale, out volume.parameters.bias, key, width, height, depth);

            if (isSlotAllocated)
            {
                if (isUploadNeeded || volume.dataUpdated)
                {
                    MaskVolumePayload payload = volume.GetPayload();

                    if (MaskVolumePayload.IsNull(ref payload) || !volume.IsAssetCompatible())
                    {
                        ReleaseMaskVolumeFromAtlas(volume);
                        return false;
                    }

                    int sizeSHCoefficientsL0 = size * MaskVolumePayload.GetDataSHL0Stride();

                    Debug.Assert(payload.dataSHL0.Length == sizeSHCoefficientsL0, "MaskVolume: The mask volume data and its resolution are out of sync! Volume data length is " + payload.dataSHL0.Length + ", but resolution * SH stride size is " + sizeSHCoefficientsL0 + ".");

                    if (size > s_MaxMaskVolumeMaskCount)
                    {
                        Debug.LogWarning("MaskVolume: mask volume data size exceeds the currently max supported blitable size. Volume data size is " + size + ", but s_MaxMaskVolumeMaskCount is " + s_MaxMaskVolumeMaskCount + ". Please decrease MaskVolume resolution, or increase MaskVolumeRendering.s_MaxMaskVolumeMaskCount.");
                        return false;
                    }

                    //Debug.Log("Uploading Mask Volume Data with key " + key + " at scale bias = " + volume.parameters.scaleBias);
                    cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeResolution, new Vector3(
                        volume.parameters.resolutionX,
                        volume.parameters.resolutionY,
                        volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeResolutionInverse, new Vector3(
                        1.0f / (float)volume.parameters.resolutionX,
                        1.0f / (float)volume.parameters.resolutionY,
                        1.0f / (float)volume.parameters.resolutionZ
                    ));
                    cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeAtlasScale,
                        volume.parameters.scale
                    );
                    cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeAtlasBias,
                        volume.parameters.bias
                    );
                    cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeAtlasResolutionAndSliceCount, new Vector4(
                        s_MaskVolumeAtlasResolution,
                        s_MaskVolumeAtlasResolution,
                        s_MaskVolumeAtlasResolution,
                        1.0f
                    ));
                    cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeAtlasResolutionAndSliceCountInverse, new Vector4(
                        1.0f / (float)s_MaskVolumeAtlasResolution,
                        1.0f / (float)s_MaskVolumeAtlasResolution,
                        1.0f / (float)s_MaskVolumeAtlasResolution,
                        1.0f
                    ));

                    s_MaskVolumeAtlasBlitDataSHL0Buffer.SetData(payload.dataSHL0);
                    cmd.SetComputeIntParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeAtlasReadBufferCount, size);
                    cmd.SetComputeBufferParam(s_MaskVolumeAtlasBlitCS, s_MaskVolumeAtlasBlitKernel, HDShaderIDs._MaskVolumeAtlasReadSHL0Buffer, s_MaskVolumeAtlasBlitDataSHL0Buffer);
                    cmd.SetComputeTextureParam(s_MaskVolumeAtlasBlitCS, s_MaskVolumeAtlasBlitKernel, HDShaderIDs._MaskVolumeAtlasWriteTextureSH, m_MaskVolumeAtlasSHRTHandle);

                    // TODO: Determine optimal batch size.
                    const int kBatchSize = 256;
                    int numThreadGroups = Mathf.CeilToInt((float)size / (float)kBatchSize);
                    cmd.DispatchCompute(s_MaskVolumeAtlasBlitCS, s_MaskVolumeAtlasBlitKernel, numThreadGroups, 1, 1);
                    return true;

                }
                return false;
            }

            if (!isSlotAllocated)
            {
                Debug.LogWarning("MaskVolume: Texture Atlas failed to allocate space for texture { key: " + key + "width: " + width + ", height: " + height + ", depth: " + depth + "}");
            }

            return false;
        }

        internal void ClearMaskVolumeAtlasIfRequested(CommandBuffer cmd)
        {
            if (!isClearMaskVolumeAtlasRequested) { return; }
            isClearMaskVolumeAtlasRequested = false;

            maskVolumeAtlas.ResetAllocator();
            cmd.SetRenderTarget(m_MaskVolumeAtlasSHRTHandle.rt, 0, CubemapFace.Unknown, 0);
            cmd.ClearRenderTarget(false, true, Color.black, 0.0f);
        }

        MaskVolumeList PrepareVisibleMaskVolumeList(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd)
        {
            MaskVolumeList maskVolumes = new MaskVolumeList();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MaskVolume))
            {
                PushMaskVolumesGlobalParamsDefault(hdCamera, cmd);
            }
            else
            {
                PrepareVisibleMaskVolumeListBuffers(renderContext, hdCamera, cmd, ref maskVolumes);
                PushMaskVolumesGlobalParams(hdCamera, cmd);
            }

            return maskVolumes;
        }

        void PrepareVisibleMaskVolumeListBuffers(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd, ref MaskVolumeList maskVolumes)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareMaskVolumeList)))
            {
                ClearMaskVolumeAtlasIfRequested(cmd);

                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleMaskVolumeBounds.Clear();
                m_VisibleMaskVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                List<MaskVolume> volumes = MaskVolume.GetVolumes();

                int maskVolumesCount = Math.Min(volumes.Count, k_MaxVisibleMaskVolumeCount);
                int sortCount = 0;

                // Sort mask volumes smallest from smallest to largest volume.
                // Same as is done with reflection masks.
                // See LightLoop.cs::PrepareLightsForGPU() for original example of this.
                for (int maskVolumesIndex = 0; (maskVolumesIndex < volumes.Count) && (sortCount < maskVolumesCount); maskVolumesIndex++)
                {
                    MaskVolume volume = volumes[maskVolumesIndex];

#if UNITY_EDITOR
                    if (!volume.IsAssetCompatible())
                        continue;
#endif

                    if (volume.maskVolumeAsset == null || !volume.maskVolumeAsset.IsDataAssigned())
                        continue;

                    /*
                    if (ShaderConfig.s_MaskVolumesAdditiveBlending == 0 && volume.parameters.volumeBlendMode != VolumeBlendMode.Normal)
                    {
                        // Non-normal blend mode volumes are not supported. Skip.
                        continue;
                    }
                    */

                    float maskVolumeDepthFromCameraWS = Vector3.Dot(hdCamera.camera.transform.forward, volume.transform.position - camPosition);
                    if (maskVolumeDepthFromCameraWS >= volume.parameters.distanceFadeEnd)
                    {
                        // Mask volume is completely faded out from distance fade optimization.
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
                        var logVolume = CalculateMaskVolumeLogVolume(volume.parameters.size);

                        m_MaskVolumeSortKeys[sortCount++] = PackMaskVolumeSortKey(volume.parameters.blendMode, logVolume, maskVolumesIndex);
                    }
                }

                CoreUnsafeUtils.QuickSort(m_MaskVolumeSortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    // In 1. we have already classify and sorted the mask volume, we need to use this sorted order here
                    uint sortKey = m_MaskVolumeSortKeys[sortIndex];
                    int maskVolumesIndex;
                    UnpackMaskVolumeSortKey(sortKey, out maskVolumesIndex);

                    MaskVolume volume = volumes[maskVolumesIndex];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // TODO: cache these?
                    var data = volume.parameters.ConvertToEngineData();

                    m_VisibleMaskVolumeBounds.Add(obb);
                    m_VisibleMaskVolumeData.Add(data);
                }

                s_VisibleMaskVolumeBoundsBuffer.SetData(m_VisibleMaskVolumeBounds);
                s_VisibleMaskVolumeDataBuffer.SetData(m_VisibleMaskVolumeData);

                // Fill the struct with pointers in order to share the data with the light loop.
                maskVolumes.bounds = m_VisibleMaskVolumeBounds;
                maskVolumes.data = m_VisibleMaskVolumeData;

                // For now, only upload one volume per frame.
                // This is done:
                // 1) To timeslice upload cost across N frames for N volumes.
                // 2) To avoid creating a sync point between compute buffer upload and each volume upload.
                const int volumeUploadedToAtlasSHCapacity = 1;
                int volumeUploadedToAtlasSHCount = 0;

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    uint sortKey = m_MaskVolumeSortKeys[sortIndex];
                    int maskVolumesIndex;
                    UnpackMaskVolumeSortKey(sortKey, out maskVolumesIndex);

                    MaskVolume volume = volumes[maskVolumesIndex];

                    if (volumeUploadedToAtlasSHCount < volumeUploadedToAtlasSHCapacity)
                    {
                        bool volumeWasUploaded = EnsureMaskVolumeInAtlas(renderContext, cmd, volume);
                        if (volumeWasUploaded)
                            ++volumeUploadedToAtlasSHCount;
                    }

                    if (volumeUploadedToAtlasSHCount == volumeUploadedToAtlasSHCapacity)
                    {
                        // Met our capacity this frame. Early out.
                        break;
                    }
                }

                return;
            }
        }

        internal static float CalculateMaskVolumeLogVolume(Vector3 size)
        {
            //Notes:
            // - 1+ term is to prevent having negative values in the log result
            // - 1000* is too keep 3 digit after the dot while we truncate the result later
            // - 1048575 is 2^20-1 as we pack the result on 20bit later
            float boxVolume = 8f* size.x * size.y * size.z;
            float logVolume = Mathf.Clamp(Mathf.Log(1 + boxVolume, 1.05f)*1000, 0, 1048575);
            return logVolume;
        }

        internal static void UnpackMaskVolumeSortKey(uint sortKey, out int maskIndex)
        {
            const uint MASK_VOLUME_MASK = (1 << 11) - 1;
            maskIndex = (int)(sortKey & MASK_VOLUME_MASK);
        }

        internal static uint PackMaskVolumeSortKey(MaskVolumeBlendMode maskVolumeBlendMode, float logVolume, int maskVolumeIndex)
        {
            // 1 bit blendMode, 20 bit volume, 11 bit index
            Debug.Assert(logVolume >= 0.0f && (uint)logVolume < (1 << 20));
            Debug.Assert(maskVolumeIndex >= 0 && (uint)maskVolumeIndex < (1 << 11));
            const uint VOLUME_MASK = (1 << 20) - 1;
            const uint INDEX_MASK = (1 << 11) - 1;

            // Sort mask volumes primarily by blend mode, and secondarily by size.
            // In the lightloop, this means we will evaluate all Additive and Subtractive blending volumes first,
            // and finally our Normal (over) blending volumes.
            // This allows us to early out during the Normal blend volumes if opacity has reached 1.0 across all threads.
            uint blendModeBits = ((maskVolumeBlendMode != MaskVolumeBlendMode.Normal) ? 0u : 1u) << 31;
            uint logVolumeBits = ((uint)logVolume & VOLUME_MASK) << 11;
            uint indexBits = (uint)maskVolumeIndex & INDEX_MASK;

            return blendModeBits | logVolumeBits | indexBits;
        }

        // TODO: Debug views
        /*
        void RenderMaskVolumeDebugOverlay(in DebugParameters debugParameters, CommandBuffer cmd)
        {
            if (!m_SupportMaskVolume)
                return;

            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.maskVolumeDebugMode != MaskVolumeDebugMode.None)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MaskVolumeDebug)))
                {
                    if (lightingDebug.maskVolumeDebugMode == MaskVolumeDebugMode.VisualizeAtlas)
                    {
                        DisplayMaskVolumeAtlas(cmd, debugParameters.maskVolumeOverlayParameters, debugParameters.debugOverlay);
                    }
                }
            }
        }

        struct MaskVolumeDebugOverlayParameters
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
            public RTHandle maskVolumeAtlas;
            public RTHandle maskVolumeAtlasOctahedralDepth;
        }

        MaskVolumeDebugOverlayParameters PrepareMaskVolumeOverlayParameters(LightingDebugSettings lightingDebug)
        {
            MaskVolumeDebugOverlayParameters parameters = new MaskVolumeDebugOverlayParameters();

            parameters.material = m_DebugDisplayMaskVolumeMaterial;

            parameters.sliceMode = (int)lightingDebug.maskVolumeAtlasSliceMode;
            parameters.validRange = new Vector4(lightingDebug.maskVolumeMinValue, 1.0f / (lightingDebug.maskVolumeMaxValue - lightingDebug.maskVolumeMinValue));
            parameters.textureViewScale = new Vector3(1.0f, 1.0f, 1.0f);
            parameters.textureViewBias = new Vector3(0.0f, 0.0f, 0.0f);
            parameters.textureViewResolution = new Vector3(s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution);
            parameters.atlasTextureOctahedralDepthScaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);

#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != null)
            {
                var selectedMaskVolume = UnityEditor.Selection.activeGameObject.GetComponent<MaskVolume>();
                if (selectedMaskVolume != null)
                {
                    // User currently has a mask volume selected.
                    // Compute a scaleBias term so that atlas view automatically zooms into selected mask volume.
                    int selectedMaskVolumeKey = selectedMaskVolume.GetID();
                    if (maskVolumeAtlas.TryGetScaleBias(out Vector3 selectedMaskVolumeScale, out Vector3 selectedMaskVolumeBias, selectedMaskVolumeKey))
                    {
                        parameters.textureViewScale = selectedMaskVolumeScale;
                        parameters.textureViewBias = selectedMaskVolumeBias;
                        parameters.textureViewResolution = new Vector3(
                            selectedMaskVolume.parameters.resolutionX,
                            selectedMaskVolume.parameters.resolutionY,
                            selectedMaskVolume.parameters.resolutionZ
                        );
                    }

                    if (ShaderConfig.s_MaskVolumesBilateralFilteringMode == MaskVolumesBilateralFilteringModes.OctahedralDepth)
                    {
                        if (maskVolumeAtlasOctahedralDepth.TryGetScaleBias(out Vector4 selectedMaskVolumeOctahedralDepthScaleBias, selectedMaskVolumeKey))
                        {
                            parameters.atlasTextureOctahedralDepthScaleBias = selectedMaskVolumeOctahedralDepthScaleBias;
                        }
                    }
                }
            }
#endif

            // Note: The system is not aware of slice packing in Z.
            // Need to modify scale and bias terms just before uploading to GPU.
            // TODO: Should we make it aware earlier up the chain?
            parameters.textureViewScale.z = parameters.textureViewScale.z / m_MaskVolumeAtlasSHRTDepthSliceCount;
            parameters.textureViewBias.z = parameters.textureViewBias.z / m_MaskVolumeAtlasSHRTDepthSliceCount;

            parameters.atlasResolutionAndSliceCount = new Vector4(s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, m_MaskVolumeAtlasSHRTDepthSliceCount);
            parameters.atlasResolutionAndSliceCountInverse = new Vector4(1.0f / s_MaskVolumeAtlasResolution, 1.0f / s_MaskVolumeAtlasResolution, 1.0f / s_MaskVolumeAtlasResolution, 1.0f / m_MaskVolumeAtlasSHRTDepthSliceCount);

            parameters.maskVolumeAtlas = m_MaskVolumeAtlasSHRTHandle;
            parameters.maskVolumeAtlasOctahedralDepth = m_MaskVolumeAtlasOctahedralDepthRTHandle;

            return parameters;
        }

        static void DisplayMaskVolumeAtlas(CommandBuffer cmd, in MaskVolumeDebugOverlayParameters parameters, DebugOverlay debugOverlay)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture(HDShaderIDs._AtlasTextureSH, parameters.maskVolumeAtlas);
            propertyBlock.SetVector(HDShaderIDs._TextureViewScale, parameters.textureViewScale);
            propertyBlock.SetVector(HDShaderIDs._TextureViewBias, parameters.textureViewBias);
            propertyBlock.SetVector(HDShaderIDs._TextureViewResolution, parameters.textureViewResolution);
            cmd.SetGlobalVector(HDShaderIDs._MaskVolumeAtlasResolutionAndSliceCount, parameters.atlasResolutionAndSliceCount);
            cmd.SetGlobalVector(HDShaderIDs._MaskVolumeAtlasResolutionAndSliceCountInverse, parameters.atlasResolutionAndSliceCountInverse);

            propertyBlock.SetVector(HDShaderIDs._ValidRange, parameters.validRange);
            propertyBlock.SetInt(HDShaderIDs._MaskVolumeAtlasSliceMode, parameters.sliceMode);

            debugOverlay.SetViewport(cmd);
            cmd.DrawProcedural(Matrix4x4.identity, parameters.material, parameters.material.FindPass("MaskVolume"), MeshTopology.Triangles, 3, 1, propertyBlock);
            debugOverlay.Next();
        }
        */
    } // class MaskVolumeRendering
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
