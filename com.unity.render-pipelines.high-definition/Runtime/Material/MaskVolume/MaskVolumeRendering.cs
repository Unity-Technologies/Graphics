using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    public struct MaskVolumeAtlasStats
    {
        public int allocationCount;
        public float allocationRatio;
        public float largestFreeBlockRatio;
        public Vector3Int largestFreeBlockPixels;
    }

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
        public int padding;
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
            data.payloadIndex = -1;
            data.bias = Vector3.zero;
            data.padding = 0;
            data.resolution = Vector3.zero;
            data.lightLayers = 0;
            data.resolutionInverse = Vector3.zero;
            data.normalBiasWS = 0.0f;

            return data;
        }
    }

    struct MaskVolumesResources
    {
        public ComputeBufferHandle boundsBuffer;
        public ComputeBufferHandle dataBuffer;
        public TextureHandle maskVolumesAtlas;
    }

    struct MaskVolumeList
    {
        public List<OrientedBBox> bounds;
        public List<MaskVolumeEngineData> data;
        public MaskVolumesResources resources;
    }

    class ClearMaskVolumesAtlasPassData
    {
        public TextureHandle maskVolumesAtlas;
    }

    struct UploadMaskVolumeParameters
    {
        public MaskVolumeHandle volume;
        public int maskVolumeAtlasSize;
    }
    class UploadMaskVolumePassData
    {
        public UploadMaskVolumeParameters parameters;
        public TextureHandle maskVolumesAtlas;
        public ComputeBufferHandle uploadBufferSHL01;
    }

    class PushMaskVolumesGlobalParamsPassData
    {
        public MaskVolumesResources resources;
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
        internal const Experimental.Rendering.GraphicsFormat k_MaskVolumeAtlasFormat = Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

        static int s_MaxMaskVolumeMaskCount;
        RTHandle m_MaskVolumeAtlasSHRTHandle;

        Texture3DAtlasDynamic<MaskVolume.MaskVolumeAtlasKey> maskVolumeAtlas = null;

        bool isClearMaskVolumeAtlasRequested = false;

#if UNITY_EDITOR
        private static Material s_DebugSamplePreviewMaterial = null;
        private static MaterialPropertyBlock s_DebugSamplePreviewMaterialPropertyBlock = null;

        private static Material GetDebugSamplePreviewMaterial()
        {
            return (s_DebugSamplePreviewMaterial != null) ? s_DebugSamplePreviewMaterial : new Material(Shader.Find("Hidden/Debug/MaskVolumeSamplePreview"));
        }

        private static MaterialPropertyBlock GetDebugSamplePreviewMaterialPropertyBlock()
        {
            return (s_DebugSamplePreviewMaterialPropertyBlock != null) ? s_DebugSamplePreviewMaterialPropertyBlock : new MaterialPropertyBlock();
        }
#endif

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
            m_VisibleMaskVolumeBounds = new List<OrientedBBox>(32);
            m_VisibleMaskVolumeData = new List<MaskVolumeEngineData>(32);
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

            maskVolumeAtlas = new Texture3DAtlasDynamic<MaskVolume.MaskVolumeAtlasKey>(s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, k_MaxVisibleMaskVolumeCount, m_MaskVolumeAtlasSHRTHandle);
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

        void UpdateShaderVariablesGlobalMaskVolumesDefault(ref ShaderVariablesGlobal cb)
        {
            cb._EnableMaskVolumes = 0;
            cb._MaskVolumeCount = 0;
        }

        void UpdateShaderVariablesGlobalMaskVolumes(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            var enableMaskVolumes = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MaskVolume);
            UpdateShaderVariablesGlobalMaskVolumes(ref cb, enableMaskVolumes);
        }

        void UpdateShaderVariablesGlobalMaskVolumes(ref ShaderVariablesGlobal cb, bool enableMaskVolumes)
        {
            if (!m_SupportMaskVolume)
            {
                UpdateShaderVariablesGlobalMaskVolumesDefault(ref cb);
                return;
            }

            cb._EnableMaskVolumes = enableMaskVolumes ? 1u : 0u;
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

        void PushMaskVolumesGlobalParams(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph, ref MaskVolumesResources resources)
        {
            Debug.Assert(m_SupportMaskVolume);

            if (renderGraph != null)
            {
                using (var builder = renderGraph.AddRenderPass<PushMaskVolumesGlobalParamsPassData>("Push Mask Volumes Global Params", out var passData))
                {
                    passData.resources.boundsBuffer = builder.ReadComputeBuffer(resources.boundsBuffer);
                    passData.resources.dataBuffer = builder.ReadComputeBuffer(resources.dataBuffer);
                    passData.resources.maskVolumesAtlas = builder.ReadTexture(resources.maskVolumesAtlas);

                    builder.SetRenderFunc((PushMaskVolumesGlobalParamsPassData passData, RenderGraphContext context) => DoPushMaskVolumesGlobalParams(
                        context.cmd,
                        passData.resources.boundsBuffer,
                        passData.resources.dataBuffer,
                        passData.resources.maskVolumesAtlas));
                }
            }
            else
            {
                DoPushMaskVolumesGlobalParams(immediateCmd,
                    s_VisibleMaskVolumeBoundsBuffer,
                    s_VisibleMaskVolumeDataBuffer,
                    m_MaskVolumeAtlasSHRTHandle);
            }
        }

        internal void PushMaskVolumesGlobalParamsDefault(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph)
        {
            Debug.Assert(hdCamera == null || hdCamera.frameSettings.IsEnabled(FrameSettingsField.MaskVolume) == false);

            if (renderGraph != null)
            {
                using (var builder = renderGraph.AddRenderPass<PushMaskVolumesGlobalParamsPassData>("Push Mask Volumes Global Params", out var passData))
                {
                    builder.SetRenderFunc((PushMaskVolumesGlobalParamsPassData passData, RenderGraphContext context) => DoPushMaskVolumesGlobalParams(
                        context.cmd,
                    s_VisibleMaskVolumeBoundsBufferDefault,
                    s_VisibleMaskVolumeDataBufferDefault,
                    TextureXR.GetBlackTexture3D()));
                }
            }
            else
            {
                DoPushMaskVolumesGlobalParams(immediateCmd,
                    s_VisibleMaskVolumeBoundsBufferDefault,
                    s_VisibleMaskVolumeDataBufferDefault,
                    TextureXR.GetBlackTexture3D());
            }
        }

        private static void DoPushMaskVolumesGlobalParams(
            CommandBuffer cmd,
            ComputeBuffer boundsBuffer,
            ComputeBuffer dataBuffer,
            RenderTargetIdentifier maskVolumeAtlas)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._MaskVolumeBounds, boundsBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._MaskVolumeDatas, dataBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._MaskVolumeAtlasSH, maskVolumeAtlas);
        }

        internal void ReleaseMaskVolumeFromAtlas(MaskVolumeHandle volume)
        {
            if (!m_SupportMaskVolume)
                return;

            MaskVolume.MaskVolumeAtlasKey key = volume.ComputeMaskVolumeAtlasKey();
            MaskVolume.MaskVolumeAtlasKey keyPrevious = volume.GetMaskVolumeAtlasKeyPrevious();

            if (maskVolumeAtlas.IsTextureSlotAllocated(key)) { maskVolumeAtlas.ReleaseTextureSlot(key); }
            if (maskVolumeAtlas.IsTextureSlotAllocated(keyPrevious)) { maskVolumeAtlas.ReleaseTextureSlot(keyPrevious); }
        }

        internal void EnsureStaleDataIsFlushedFromAtlases(MaskVolumeHandle volume)
        {
            MaskVolume.MaskVolumeAtlasKey key = volume.ComputeMaskVolumeAtlasKey();
            MaskVolume.MaskVolumeAtlasKey keyPrevious = volume.GetMaskVolumeAtlasKeyPrevious();
            if (!key.Equals(keyPrevious))
            {
                if (maskVolumeAtlas.IsTextureSlotAllocated(keyPrevious))
                {
                    maskVolumeAtlas.ReleaseTextureSlot(keyPrevious);
                }

                volume.SetMaskVolumeAtlasKeyPrevious(key);
            }
        }

        internal bool EnsureMaskVolumeInAtlas(CommandBuffer immediateCmd, RenderGraph renderGraph, ref MaskVolumesResources resources, MaskVolumeHandle volume)
        {
            VolumeGlobalUniqueID id = volume.GetAtlasID();
            var resolution = volume.GetResolution();
            int size = resolution.x * resolution.y * resolution.z;
            Debug.Assert(size > 0, "MaskVolume: Encountered mask volume with resolution set to zero on all three axes.");

            MaskVolume.MaskVolumeAtlasKey key = volume.ComputeMaskVolumeAtlasKey();

            // TODO: Store volume resolution inside the atlas's key->bias dictionary.
            // If resolution has changed since upload, need to free previous allocation from atlas,
            // and attempt to allocate a new chunk from the atlas for the new resolution settings.
            // Currently atlas allocator only handles splitting. Need to add merging of neighboring, empty chunks to avoid fragmentation.
            bool isSlotAllocated = maskVolumeAtlas.EnsureTextureSlot(out bool isUploadNeeded, out volume.parameters.scale, out volume.parameters.bias, key, resolution.x, resolution.y, resolution.z);

            if (isSlotAllocated)
            {
                if (isUploadNeeded || volume.IsDataUpdated())
                {
                    if (!volume.IsDataAssigned())
                    {
                        ReleaseMaskVolumeFromAtlas(volume);
                        return false;
                    }

                    int sizeSHCoefficientsL0 = size * MaskVolumePayload.GetDataSHL0Stride();
                    Debug.AssertFormat(volume.DataSHL0Length == sizeSHCoefficientsL0, "MaskVolume: The mask volume data and its resolution are out of sync! Volume data length is {0}, but resolution * SH stride size is {1}.", volume.DataSHL0Length, sizeSHCoefficientsL0);

                    if (size > s_MaxMaskVolumeMaskCount)
                    {
                        Debug.LogWarningFormat("MaskVolume: mask volume data size exceeds the currently max supported blitable size. Volume data size is {0}, but s_MaxMaskVolumeMaskCount is {1}. Please decrease MaskVolume resolution, or increase MaskVolumeRendering.s_MaxMaskVolumeMaskCount.", size, s_MaxMaskVolumeMaskCount);
                        return false;
                    }

                    // Ready to upload: prepare parameters and data
                    UploadMaskVolumeParameters parameters = new UploadMaskVolumeParameters()
                    {
                        volume = volume,
                        maskVolumeAtlasSize = size,
                    };

                    volume.SetDataSHL0(immediateCmd, s_MaskVolumeAtlasBlitDataSHL0Buffer);

                    // Execute upload
                    if (renderGraph != null)
                    {
                        using (var builder = renderGraph.AddRenderPass<UploadMaskVolumePassData>("Upload Mask Volume", out var passData))
                        {
                            // Parameters
                            passData.parameters = parameters;

                            // Resources
                            passData.uploadBufferSHL01 = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(s_MaskVolumeAtlasBlitDataSHL0Buffer));
                            passData.maskVolumesAtlas = builder.WriteTexture(resources.maskVolumesAtlas);

                            // RenderFunc
                            builder.SetRenderFunc((UploadMaskVolumePassData passData, RenderGraphContext context) => UploadMaskVolumeToAtlas(
                                passData.parameters,
                                context.cmd,
                                passData.uploadBufferSHL01,
                                passData.maskVolumesAtlas));
                        }
                    }
                    else
                    {
                        UploadMaskVolumeToAtlas(
                            parameters,
                            immediateCmd,
                            s_MaskVolumeAtlasBlitDataSHL0Buffer,
                            m_MaskVolumeAtlasSHRTHandle);
                    }

                    return true;

                }
                return false;
            }

            if (!isSlotAllocated)
            {
                Debug.LogWarningFormat("MaskVolume: Texture Atlas failed to allocate space for texture (id: {0}, width: {1}, height: {2}, depth: {3})", id, resolution.x, resolution.y, resolution.z);
            }

            return false;
        }

        private static void UploadMaskVolumeToAtlas(
            UploadMaskVolumeParameters parameters,
            CommandBuffer cmd,
            ComputeBuffer uploadBufferSHL01,
            RenderTargetIdentifier targetAtlas)
        {
            MaskVolumeHandle volume = parameters.volume;
            var resolution = volume.GetResolution();

            cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeResolution, (Vector3)resolution);

            cmd.SetComputeVectorParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeResolutionInverse, new Vector3(
                1.0f / resolution.x,
                1.0f / resolution.y,
                1.0f / resolution.z
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

            cmd.SetComputeIntParam(s_MaskVolumeAtlasBlitCS, HDShaderIDs._MaskVolumeAtlasReadBufferCount, parameters.maskVolumeAtlasSize);

            cmd.SetComputeBufferParam(s_MaskVolumeAtlasBlitCS, s_MaskVolumeAtlasBlitKernel, HDShaderIDs._MaskVolumeAtlasReadSHL0Buffer, uploadBufferSHL01);
            cmd.SetComputeTextureParam(s_MaskVolumeAtlasBlitCS, s_MaskVolumeAtlasBlitKernel, HDShaderIDs._MaskVolumeAtlasWriteTextureSH, targetAtlas);

            // TODO: Determine optimal batch size.
            const int kBatchSize = 256;
            int numThreadGroups = (parameters.maskVolumeAtlasSize + kBatchSize - 1) / kBatchSize;
            cmd.DispatchCompute(s_MaskVolumeAtlasBlitCS, s_MaskVolumeAtlasBlitKernel, numThreadGroups, 1, 1);
        }

        internal void ClearMaskVolumeAtlasIfRequested(CommandBuffer immediateCmd, RenderGraph renderGraph, ref MaskVolumesResources resources)
        {
            if (!isClearMaskVolumeAtlasRequested) { return; }
            isClearMaskVolumeAtlasRequested = false;

            maskVolumeAtlas.ResetAllocator();

            if (renderGraph != null)
            {
                using (var builder = renderGraph.AddRenderPass<ClearMaskVolumesAtlasPassData>("Clear Mask Volume Atlas", out var passData))
                {
                    passData.maskVolumesAtlas = builder.WriteTexture(resources.maskVolumesAtlas);

                    builder.SetRenderFunc((ClearMaskVolumesAtlasPassData passData, RenderGraphContext context) =>
                        DoClearMaskVolumeAtlas(context.cmd, passData.maskVolumesAtlas));
                }
            }
            else
            {
                DoClearMaskVolumeAtlas(immediateCmd, m_MaskVolumeAtlasSHRTHandle);
            }
        }

        private void DoClearMaskVolumeAtlas(CommandBuffer cmd, RenderTargetIdentifier atlas)
        {
            cmd.SetRenderTarget(atlas, 0, CubemapFace.Unknown, 0);
            cmd.ClearRenderTarget(false, true, Color.black, 0.0f);
        }

        MaskVolumeList PrepareVisibleMaskVolumeList(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph)
        {
            MaskVolumeList maskVolumes = new MaskVolumeList();

            if (!m_SupportMaskVolume)
                return maskVolumes;

            if (!m_EnableRenderGraph)
                renderGraph = null;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MaskVolume))
            {
                PushMaskVolumesGlobalParamsDefault(hdCamera, immediateCmd, renderGraph);
            }
            else
            {
                PrepareVisibleMaskVolumeListBuffers(hdCamera, immediateCmd, renderGraph, ref maskVolumes);
                PushMaskVolumesGlobalParams(hdCamera, immediateCmd, renderGraph, ref maskVolumes.resources);

                // Fill the struct with pointers in order to share the data with the light loop.
                maskVolumes.bounds = m_VisibleMaskVolumeBounds;
                maskVolumes.data = m_VisibleMaskVolumeData;
            }

            return maskVolumes;
        }

        internal void PrepareGlobalMaskVolumeList(CommandBuffer immediateCmd)
        {
            MaskVolumeList maskVolumes = new MaskVolumeList();

            if (!currentAsset.GetDefaultFrameSettings(FrameSettingsRenderType.CustomOrBakedReflection).IsEnabled(FrameSettingsField.MaskVolume))
            {
                PushMaskVolumesGlobalParamsDefault(null, immediateCmd, null);
            }
            else
            {
                PrepareVisibleMaskVolumeListBuffers(null, immediateCmd, null, ref maskVolumes);
                PushMaskVolumesGlobalParams(null, immediateCmd, null, ref maskVolumes.resources);
                
                m_ShaderVariablesGlobalCB._WorldSpaceCameraPos_Internal = Vector4.zero;
                UpdateShaderVariablesGlobalMaskVolumes(ref m_ShaderVariablesGlobalCB, true);
                ConstantBuffer.PushGlobal(immediateCmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
            }
        }
        
        void PrepareVisibleMaskVolumeListBuffers(HDCamera hdCamera, CommandBuffer immediateCmd, RenderGraph renderGraph, ref MaskVolumeList maskVolumes)
        {
            using (new ProfilingScope(immediateCmd, ProfilingSampler.Get(HDProfileId.PrepareMaskVolumeList)))
            {
                if (renderGraph != null)
                {
                    maskVolumes.resources.boundsBuffer = renderGraph.ImportComputeBuffer(s_VisibleMaskVolumeBoundsBuffer);
                    maskVolumes.resources.dataBuffer = renderGraph.ImportComputeBuffer(s_VisibleMaskVolumeDataBuffer);
                    maskVolumes.resources.maskVolumesAtlas = renderGraph.ImportTexture(m_MaskVolumeAtlasSHRTHandle);
                }

                ClearMaskVolumeAtlasIfRequested(immediateCmd, renderGraph, ref maskVolumes.resources);

                var isViewDependent = hdCamera != null;

                float globalDistanceFadeStart = default;
                float globalDistanceFadeEnd = default;
                Vector3 camPosition = Vector3.zero;
                Vector3 camOffset = Vector3.zero; // World-origin-relative
                if (isViewDependent)
                {
                    var settings = hdCamera.volumeStack.GetComponent<MaskVolumeController>();

                    globalDistanceFadeStart = settings.distanceFadeStart.value;
                    globalDistanceFadeEnd = settings.distanceFadeEnd.value;
                    
                    camPosition = hdCamera.camera.transform.position;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                        camOffset = camPosition; // Camera-relative
                }

                m_VisibleMaskVolumeBounds.Clear();
                m_VisibleMaskVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                List<MaskVolumeHandle> volumes = MaskVolumeManager.manager.CollectVolumesToRender();

                int maskVolumesCount = Math.Min(volumes.Count, k_MaxVisibleMaskVolumeCount);
                int sortCount = 0;

                // Sort mask volumes smallest from smallest to largest volume.
                // Same as is done with reflection masks.
                // See LightLoop.cs::PrepareLightsForGPU() for original example of this.
                for (int maskVolumesIndex = 0; (maskVolumesIndex < volumes.Count) && (sortCount < maskVolumesCount); maskVolumesIndex++)
                {
                    MaskVolumeHandle volume = volumes[maskVolumesIndex];

                    var isVisible = volume.parameters.weight >= 1e-5f && volume.IsDataAssigned();

                    // When hdCamera is null we are preparing for some view-independent baking, so we consider all valid volumes visible.
                    if (isViewDependent && isVisible)
                    {
                        float maskVolumeDepthFromCameraWS = Vector3.Dot(hdCamera.camera.transform.forward, volume.position - camPosition);
                        isVisible = maskVolumeDepthFromCameraWS < Mathf.Min(globalDistanceFadeEnd, volume.parameters.distanceFadeEnd);

                        if (isVisible)
                        {
                            // TODO: cache these?
                            var obb = new OrientedBBox(Matrix4x4.TRS(volume.position, volume.rotation, volume.parameters.size));

                            // Handle camera-relative rendering.
                            obb.center -= camOffset;

                            // Frustum cull on the CPU for now. TODO: do it on the GPU.
                            isVisible = GeometryUtils.Overlap(obb, hdCamera.frustum, hdCamera.frustum.planes.Length, hdCamera.frustum.corners.Length);
                        }
                    }

                    if (isVisible)
                    {
                        var logVolume = CalculateMaskVolumeLogVolume(volume.parameters.size);
                        m_MaskVolumeSortKeys[sortCount++] = PackMaskVolumeSortKey(logVolume, maskVolumesIndex);
                    }
                    else
                    {
                        ReleaseMaskVolumeFromAtlas(volume);
                    }
                }

                CoreUnsafeUtils.QuickSort(m_MaskVolumeSortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    // In 1. we have already classify and sorted the mask volume, we need to use this sorted order here
                    uint sortKey = m_MaskVolumeSortKeys[sortIndex];
                    int maskVolumesIndex;
                    UnpackMaskVolumeSortKey(sortKey, out maskVolumesIndex);

                    MaskVolumeHandle volume = volumes[maskVolumesIndex];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.position, volume.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // TODO: cache these?
                    var resolution = volume.GetResolution();
                    var data = ConvertToEngineData(volume.parameters, resolution, isViewDependent, globalDistanceFadeStart, globalDistanceFadeEnd);

                    m_VisibleMaskVolumeBounds.Add(obb);
                    m_VisibleMaskVolumeData.Add(data);
                }

                s_VisibleMaskVolumeBoundsBuffer.SetData(m_VisibleMaskVolumeBounds);
                s_VisibleMaskVolumeDataBuffer.SetData(m_VisibleMaskVolumeData);

                // For now, only upload one volume per frame.
                // This is done to timeslice upload cost across N frames for N volumes.
                // Uncap upload capacity when baking.
                int volumeUploadedToAtlasCapacity = isViewDependent ? 1 : int.MaxValue;
                int volumeUploadedToAtlasSHCount = 0;

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    uint sortKey = m_MaskVolumeSortKeys[sortIndex];
                    int maskVolumesIndex;
                    UnpackMaskVolumeSortKey(sortKey, out maskVolumesIndex);

                    MaskVolumeHandle volume = volumes[maskVolumesIndex];

                    EnsureStaleDataIsFlushedFromAtlases(volume);

                    if (volumeUploadedToAtlasSHCount < volumeUploadedToAtlasCapacity)
                    {
                        bool volumeWasUploaded = EnsureMaskVolumeInAtlas(immediateCmd, renderGraph, ref maskVolumes.resources, volume);
                        if (volumeWasUploaded)
                            ++volumeUploadedToAtlasSHCount;
                    }

                    if (volumeUploadedToAtlasSHCount == volumeUploadedToAtlasCapacity)
                    {
                        // Met our capacity this frame. Early out.
                        break;
                    }
                }
            }
        }
        
        MaskVolumeEngineData ConvertToEngineData(MaskVolumeArtistParameters parameters, Vector3Int resolution, bool useDistanceFade, float globalDistanceFadeStart, float globalDistanceFadeEnd)
        {
            MaskVolumeEngineData data = new MaskVolumeEngineData();

            data.weight = parameters.weight;
            data.normalBiasWS = parameters.normalBiasWS;

            data.debugColor.x = parameters.debugColor.r;
            data.debugColor.y = parameters.debugColor.g;
            data.debugColor.z = parameters.debugColor.b;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = Vector3.Max(parameters.positiveFade, new Vector3(1e-5f, 1e-5f, 1e-5f));
            Vector3 negativeFade = Vector3.Max(parameters.negativeFade, new Vector3(1e-5f, 1e-5f, 1e-5f));

            data.rcpPosFaceFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFaceFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFaceFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFaceFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFaceFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFaceFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            if (useDistanceFade)
            {
                float distanceFadeStart = Mathf.Min(globalDistanceFadeStart, parameters.distanceFadeStart);
                float distanceFadeEnd = Mathf.Min(globalDistanceFadeEnd, parameters.distanceFadeEnd);

                float distFadeLen = Mathf.Max(distanceFadeEnd - distanceFadeStart, 0.00001526f);
                data.rcpDistFadeLen = 1.0f / distFadeLen;
                data.endTimesRcpDistFadeLen = distanceFadeEnd * data.rcpDistFadeLen;
            }
            else
            {
                data.rcpDistFadeLen = 1.0f;
                data.endTimesRcpDistFadeLen = float.MaxValue;
            }

            data.scale = parameters.scale;
            data.bias = parameters.bias;

            data.resolution = resolution;
            data.resolutionInverse = new Vector3(1.0f / resolution.x, 1.0f / resolution.y, 1.0f / resolution.z);

            data.lightLayers = (uint)parameters.lightLayers;

            return data;
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

        internal static uint PackMaskVolumeSortKey(float logVolume, int maskVolumeIndex)
        {
            // 20 bit volume, 12 bit index
            Debug.Assert(logVolume >= 0.0f && (uint)logVolume < (1 << 20));
            Debug.Assert(maskVolumeIndex >= 0 && (uint)maskVolumeIndex < (1 << 12));
            const uint VOLUME_MASK = (1 << 20) - 1;
            const uint INDEX_MASK = (1 << 12) - 1;

            // Sort mask volumes primarily by blend mode, and secondarily by size.
            // In the lightloop, this means we will evaluate all Additive and Subtractive blending volumes first,
            // and finally our Normal (over) blending volumes.
            // This allows us to early out during the Normal blend volumes if opacity has reached 1.0 across all threads.
            uint logVolumeBits = ((uint)logVolume & VOLUME_MASK) << 11;
            uint indexBits = (uint)maskVolumeIndex & INDEX_MASK;

            return logVolumeBits | indexBits;
        }

        void RenderMaskVolumeDebugOverlay(in DebugParameters debugParameters, CommandBuffer cmd)
        {
            if (!m_SupportProbeVolume)
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
            public bool supportMaskVolume;
            public Material material;
            public Vector4 textureViewScale;
            public Vector4 textureViewBias;
            public Vector3 textureViewResolution;
            public Vector4 atlasResolutionAndSliceCount;
            public Vector4 atlasResolutionAndSliceCountInverse;
            public RTHandle maskVolumeAtlas;
        }

        MaskVolumeDebugOverlayParameters PrepareMaskVolumeOverlayParameters(LightingDebugSettings lightingDebug)
        {
            MaskVolumeDebugOverlayParameters parameters = new MaskVolumeDebugOverlayParameters();

            parameters.supportMaskVolume = m_SupportMaskVolume;

            parameters.material = m_DebugDisplayMaskVolumeMaterial;
            parameters.textureViewScale = new Vector3(1.0f, 1.0f, 1.0f);
            parameters.textureViewBias = new Vector3(0.0f, 0.0f, 0.0f);
            parameters.textureViewResolution = new Vector3(s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution);

#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != null)
            {
                var selectedMaskVolume = UnityEditor.Selection.activeGameObject.GetComponent<MaskVolume>();
                if (selectedMaskVolume != null)
                {
                    // User currently has a probe volume selected.
                    // Compute a scaleBias term so that atlas view automatically zooms into selected probe volume.
                    MaskVolume.MaskVolumeAtlasKey selectedMaskVolumeKey = selectedMaskVolume.ComputeMaskVolumeAtlasKey();
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
                }
            }
#endif
            parameters.atlasResolutionAndSliceCount = new Vector4(s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, s_MaskVolumeAtlasResolution, 1.0f);
            parameters.atlasResolutionAndSliceCountInverse = new Vector4(1.0f / s_MaskVolumeAtlasResolution, 1.0f / s_MaskVolumeAtlasResolution, 1.0f / s_MaskVolumeAtlasResolution, 1.0f / 1.0f);

            parameters.maskVolumeAtlas = m_MaskVolumeAtlasSHRTHandle;

            return parameters;
        }

        static void DisplayMaskVolumeAtlas(CommandBuffer cmd, in MaskVolumeDebugOverlayParameters parameters, DebugOverlay debugOverlay)
        {
            if (!parameters.supportMaskVolume) { return; }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetVector(HDShaderIDs._TextureViewScale, parameters.textureViewScale);
            propertyBlock.SetVector(HDShaderIDs._TextureViewBias, parameters.textureViewBias);
            propertyBlock.SetVector(HDShaderIDs._TextureViewResolution, parameters.textureViewResolution);
            cmd.SetGlobalVector(HDShaderIDs._MaskVolumeAtlasResolutionAndSliceCount, parameters.atlasResolutionAndSliceCount);
            cmd.SetGlobalVector(HDShaderIDs._MaskVolumeAtlasResolutionAndSliceCountInverse, parameters.atlasResolutionAndSliceCountInverse);

            debugOverlay.SetViewport(cmd);
            cmd.DrawProcedural(Matrix4x4.identity, parameters.material, parameters.material.FindPass("MaskVolume"), MeshTopology.Triangles, 3, 1, propertyBlock);
            debugOverlay.Next();
        }

#if UNITY_EDITOR
        internal void DrawMaskVolumeDebugSamplePreview(MaskVolume maskVolume, Camera camera)
        {
            if (!m_SupportMaskVolume) { return; }

            Material debugMaterial = GetDebugSamplePreviewMaterial();
            if (debugMaterial == null) { return; }

            MaterialPropertyBlock debugMaterialPropertyBlock = GetDebugSamplePreviewMaterialPropertyBlock();
            debugMaterialPropertyBlock.SetVector("_MaskVolumeResolution", new Vector3(maskVolume.parameters.resolutionX, maskVolume.parameters.resolutionY, maskVolume.parameters.resolutionZ));
            debugMaterialPropertyBlock.SetMatrix("_ProbeIndex3DToPositionWSMatrix", MaskVolume.ComputeProbeIndex3DToPositionWSMatrix(maskVolume));
            debugMaterialPropertyBlock.SetFloat("_MaskVolumeProbeDisplayRadiusWS", Gizmos.probeSize);

            bool maskVolumeIsResidentInAtlas = maskVolumeAtlas.TryGetScaleBias(out Vector3 maskVolumeScaleUnused, out Vector3 maskVolumeBias, maskVolume.ComputeMaskVolumeAtlasKey());
            if (!maskVolumeIsResidentInAtlas)
            {
                maskVolumeBias = Vector3.zero;
            }
            Vector3 maskVolumeBiasTexels = new Vector3(Mathf.Round(maskVolumeBias.x * s_MaskVolumeAtlasResolution), Mathf.Round(maskVolumeBias.y * s_MaskVolumeAtlasResolution), Mathf.Round(maskVolumeBias.z * s_MaskVolumeAtlasResolution));

            debugMaterialPropertyBlock.SetVector("_MaskVolumeAtlasBiasTexels", maskVolumeBiasTexels);
            debugMaterialPropertyBlock.SetInt("_MaskVolumeIsResidentInAtlas", maskVolumeIsResidentInAtlas ? 1 : 0);
            debugMaterialPropertyBlock.SetFloat("_MaskVolumeDrawWeightThresholdSquared", maskVolume.parameters.drawWeightThreshold * maskVolume.parameters.drawWeightThreshold);
            debugMaterial.SetPass(0);
            Graphics.DrawProcedural(debugMaterial, MaskVolume.ComputeBoundsWS(maskVolume), MeshTopology.Triangles, 3 * 2 * MaskVolume.ComputeProbeCount(maskVolume), 1, camera, debugMaterialPropertyBlock, ShadowCastingMode.Off, receiveShadows: false, layer: 0);
        }
#endif

        public MaskVolumeAtlasStats GetMaskVolumeAtlasStats()
        {
            if (!m_SupportMaskVolume) { return new MaskVolumeAtlasStats(); }

            return new MaskVolumeAtlasStats
            {
                allocationCount = (maskVolumeAtlas != null && m_SupportMaskVolume) ? maskVolumeAtlas.GetAllocationCount() : 0,
                allocationRatio = (maskVolumeAtlas != null && m_SupportMaskVolume) ? maskVolumeAtlas.GetAllocationRatio() : 0,
                largestFreeBlockRatio = (maskVolumeAtlas != null && m_SupportMaskVolume) ? maskVolumeAtlas.FindLargestFreeBlockRatio() : 0.0f,
                largestFreeBlockPixels = (maskVolumeAtlas != null && m_SupportMaskVolume) ? maskVolumeAtlas.FindLargestFreeBlockPixels() : Vector3Int.zero
            };
        }
    } // class MaskVolumeRendering
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
