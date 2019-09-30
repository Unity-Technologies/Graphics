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
        public Vector3 resolution;
        public Vector3 resolutionInverse;

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
            data.resolution = Vector3.zero;
            data.resolutionInverse = Vector3.zero;

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
        static int s_ProbeVolumeAtlasBlitKernel = -1;
        static ComputeBuffer s_ProbeVolumeAtlasBlitDataBuffer = null;
        static int s_ProbeVolumeAtlasWidth = 1024;
        static int s_ProbeVolumeAtlasHeight = 1024;

        // TODO: Preallocating compute buffer for this worst case of a single probe volume that consumes the whole atlas is a memory hog.
        // May want to look at dynamic resizing of compute buffer based on use, or more simply, slicing it up across multiple dispatches for massive volumes.
        // With current settings this compute buffer will take  1024 * 1024 * sizeof(float) * coefficientCount (12) bytes ~= 50.3 MB.
        static int s_MaxProbeVolumeProbeCount = 1024 * 1024;
        RTHandle m_ProbeVolumeAtlasSHRTHandle;
        Texture2DAtlas probeVolumeAtlas = null; // TODO(Nicholas): it was marked as public, but Texture2DAtlas is not publicly accessible anymore.

        // Note: These max resolution dimensions are implicitly defined from the way probe volumes are laid out in our 2D atlas.
        // If this layout changes, these resolution constraints should be updated to reflect the actual constraint.
        // Currently, the constraint is defined as: the maximum resolution that could possibly be allocated given the atlas size and only one probe volume resident.
        public static void ComputeProbeVolumeMaxResolutionFromConstraintX(out int maxX, out int maxY, out int maxZ, int requestedX)
        {
            maxY = s_ProbeVolumeAtlasHeight;
            maxX = s_ProbeVolumeAtlasWidth;
            maxZ = s_ProbeVolumeAtlasWidth / Mathf.Min(maxX, requestedX);
        }

        public void Build(HDRenderPipelineAsset asset)
        {
            m_SupportProbeVolume = asset.currentPlatformRenderPipelineSettings.supportProbeVolume;

            s_ProbeVolumeAtlasWidth = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasWidth;
            s_ProbeVolumeAtlasHeight = asset.currentPlatformRenderPipelineSettings.probeVolumeSettings.atlasHeight;
            s_MaxProbeVolumeProbeCount = s_ProbeVolumeAtlasWidth * s_ProbeVolumeAtlasHeight;

            preset = m_SupportProbeVolume ? ProbeVolumeSystemPreset.On : ProbeVolumeSystemPreset.Off;

            if (preset != ProbeVolumeSystemPreset.Off)
            {
                CreateBuffers();

                s_ProbeVolumeAtlasBlitCS = asset.renderPipelineResources.shaders.probeVolumeAtlasBlitCS;
                s_ProbeVolumeAtlasBlitKernel = s_ProbeVolumeAtlasBlitCS.FindKernel("ProbeVolumeAtlasBlitKernel");
            }
            else
            {
                CreateBuffersDefault();
            }
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

            m_ProbeVolumeAtlasSHRTHandle = RTHandles.Alloc(
                width: s_ProbeVolumeAtlasWidth,
                height: s_ProbeVolumeAtlasHeight,
                slices: 3, // one texture per [RGB] SH coefficients
                dimension: TextureDimension.Tex2DArray,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,//GraphicsFormat.B10G11R11_UFloatPack32,
                enableRandomWrite: true,
                useMipMap: false,
                name: "ProbeVolumeAtlasSH"
            );

            probeVolumeAtlas = new Texture2DAtlas(s_ProbeVolumeAtlasWidth, s_ProbeVolumeAtlasHeight, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
        }

        void DestroyBuffers()
        {
            CoreUtils.SafeRelease(s_VisibleProbeVolumeBoundsBufferDefault);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeDataBufferDefault);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeBoundsBuffer);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeDataBuffer);
            CoreUtils.SafeRelease(s_ProbeVolumeAtlasBlitDataBuffer);

            if (m_ProbeVolumeAtlasSHRTHandle != null)
                RTHandles.Release(m_ProbeVolumeAtlasSHRTHandle);

            if (probeVolumeAtlas != null)
                probeVolumeAtlas.Release();

            m_VisibleProbeVolumeBounds = null;
            m_VisibleProbeVolumeData = null;
        }

        public void Cleanup()
        {
            DestroyBuffers();
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            if (!m_SupportProbeVolume)
            {
                ProbeVolumeSystem.PushGlobalParamsDefault(hdCamera, cmd, frameIndex);
                return;
            }

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

            var settings = VolumeManager.instance.stack.GetComponent<ProbeVolumeController>();
            float normalBiasWS = (settings == null) ? 0.0f : settings.normalBiasWS.value;
            cmd.SetGlobalFloat("_ProbeVolumeNormalBiasWS", normalBiasWS);
        }

        private static void PushGlobalParamsDefault(HDCamera hdCamera, CommandBuffer cmd, int frameIndex)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBufferDefault);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBufferDefault);
            cmd.SetGlobalInt(HDShaderIDs._ProbeVolumeCount, 0);
            cmd.SetGlobalTexture("_ProbeVolumeAtlasSH", Texture2D.blackTexture);
            cmd.SetGlobalFloat("_ProbeVolumeNormalBiasWS", 0.0f);
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
            bool isSlotAllocated = probeVolumeAtlas.EnsureTextureSlot(out bool isUploadNeeded, ref volume.parameters.scaleBias, key, width, height);

            if (isSlotAllocated)
            {
                if (isUploadNeeded || volume.dataUpdated)
                {
                    var data = volume.GetData();

                    if (data == null || data.Length == 0)
                        return false;

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

                    //Debug.Log("data[0] = " + data[0]);
                    Debug.Assert(data.Length == size, "Error: ProbeVolumeSystem: volume data length = " + data.Length + ", resolution size = " + size);
                    s_ProbeVolumeAtlasBlitDataBuffer.SetData(data);
                    cmd.SetComputeIntParam(s_ProbeVolumeAtlasBlitCS, "_ProbeVolumeAtlasReadBufferCount", size);
                    cmd.SetComputeBufferParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, "_ProbeVolumeAtlasReadBuffer", s_ProbeVolumeAtlasBlitDataBuffer);
                    cmd.SetComputeTextureParam(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, "_ProbeVolumeAtlasWriteTextureSH", m_ProbeVolumeAtlasSHRTHandle);

                    // TODO: Determine optimal batch size.
                    const int kBatchSize = 256;
                    int numThreadGroups = Mathf.CeilToInt((float)size / (float)kBatchSize);
                    cmd.DispatchCompute(s_ProbeVolumeAtlasBlitCS, s_ProbeVolumeAtlasBlitKernel, numThreadGroups, 1, 1);
                    return true;

                }
                return false;
            }

            Debug.Assert(isSlotAllocated, "Warning: ProbeVolumeSystem: Texture Atlas failed to allocate space for texture { key: " + key + "width: " + width + ", height: " + height);
            return false;
        }

        public ProbeVolumeList PrepareVisibleProbeVolumeList(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd)
        {
            ProbeVolumeList probeVolumes = new ProbeVolumeList();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return probeVolumes;

            using (new ProfilingSample(cmd, "Prepare Probe Volume List", CustomSamplerId.PrepareProbeVolumeList.GetSampler()))
            {
                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset   = Vector3.zero;// World-origin-relative

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
                probeVolumes.bounds  = m_VisibleProbeVolumeBounds;
                probeVolumes.data = m_VisibleProbeVolumeData;

                for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                {
                    uint sortKey = m_SortKeys[sortIndex];
                    int probeVolumesIndex;
                    UnpackProbeVolumeSortKey(sortKey, out probeVolumesIndex);

                    ProbeVolume volume = volumes[probeVolumesIndex];

                    bool volumeWasUploaded = EnsureProbeVolumeInAtlas(renderContext, cmd, volume);
                    if (volumeWasUploaded)
                    {
                        // For now, only upload one volume per frame.
                        // This is done:
                        // 1) To timeslice upload cost across N frames for N volumes.
                        // 2) To avoid creating a sync point between compute buffer upload and each volume upload.
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

        public void DisplayProbeVolumeAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue)
        {
            if (!m_SupportProbeVolume) { return; }
            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            float rWidth = 1.0f / m_ProbeVolumeAtlasSHRTHandle.rt.width;
            float rHeight = 1.0f / m_ProbeVolumeAtlasSHRTHandle.rt.height;
            Vector4 scaleBias = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), new Vector4(m_ProbeVolumeAtlasSHRTHandle.rt.width, m_ProbeVolumeAtlasSHRTHandle.rt.height, 0, 0));

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_AtlasTextureSH", m_ProbeVolumeAtlasSHRTHandle.rt);
            propertyBlock.SetVector("_TextureScaleBias", scaleBias);
            propertyBlock.SetVector("_ValidRange", validRange);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("ProbeVolume"), MeshTopology.Triangles, 3, 1, propertyBlock);
        }

    } // class ProbeVolumeSystem
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
