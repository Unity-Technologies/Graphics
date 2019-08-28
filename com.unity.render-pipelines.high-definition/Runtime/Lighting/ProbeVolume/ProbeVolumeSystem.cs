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

            return data;
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

        public struct ProbeVolumeSystemParameters
        {
            // TODO: Create Probe Volume Rendering global parameters here.

            public void ZeroInitialize()
            {
                // TODO: Clear all parameters to neutral values.
            }
        }

        public ProbeVolumeSystemPreset preset = ProbeVolumeSystemPreset.Off;

        List<OrientedBBox> m_VisibleProbeVolumeBounds = null;
        List<ProbeVolumeEngineData> m_VisibleProbeVolumeData = null;
        public const int k_MaxVisibleProbeVolumeCount = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        static ComputeBuffer s_VisibleProbeVolumeBoundsBuffer = null;
        static ComputeBuffer s_VisibleProbeVolumeDataBuffer = null;

        // Is the feature globally disabled?
        bool m_SupportProbeVolume = false;

        // Pre-allocate sort keys array to max size to avoid creating allocations / garbage at runtime.
        uint[] m_SortKeys = new uint[k_MaxVisibleProbeVolumeCount];

        public void Build(HDRenderPipelineAsset asset)
        {
            m_SupportProbeVolume = asset.currentPlatformRenderPipelineSettings.supportProbeVolume;

            preset = m_SupportProbeVolume ? ProbeVolumeSystemPreset.On : ProbeVolumeSystemPreset.Off;

            if (preset != ProbeVolumeSystemPreset.Off)
            {
                CreateBuffers();
            }
        }

        void CreateBuffers()
        {
            m_VisibleProbeVolumeBounds = new List<OrientedBBox>();
            m_VisibleProbeVolumeData = new List<ProbeVolumeEngineData>();
            s_VisibleProbeVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleProbeVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleProbeVolumeDataBuffer = new ComputeBuffer(k_MaxVisibleProbeVolumeCount, Marshal.SizeOf(typeof(ProbeVolumeEngineData)));
        }

        // For the initial allocation, no suballocation happens (the texture is full size).
        ProbeVolumeSystemParameters ComputeParameters(HDCamera hdCamera)
        {
            var controller = VolumeManager.instance.stack.GetComponent<ProbeVolumeController>();

            // TODO: Assign probe volume rendering parameters from values returned from ProbeVolumeController (camera activated volume).
            return new ProbeVolumeSystemParameters();
        }

        public void InitializePerCameraData(HDCamera hdCamera)
        {
            // Note: Here we can't test framesettings as they are not initialize yet
            if (!m_SupportProbeVolume)
                return;

            hdCamera.probeVolumeSystemParams = ComputeParameters(hdCamera);
        }

        public void DeinitializePerCameraData(HDCamera hdCamera)
        {
            if (!m_SupportProbeVolume)
                return;

            hdCamera.probeVolumeSystemParams.ZeroInitialize();
        }

        // This function relies on being called once per camera per frame.
        // The results are undefined otherwise.
        public void UpdatePerCameraData(HDCamera hdCamera)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return;

            // Currently the same as initialize. We just compute the settings.
            this.InitializePerCameraData(hdCamera);
        }

        void DestroyBuffers()
        {
            CoreUtils.SafeRelease(s_VisibleProbeVolumeBoundsBuffer);
            CoreUtils.SafeRelease(s_VisibleProbeVolumeDataBuffer);

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
                return;

            var currFrameParams = hdCamera.probeVolumeSystemParams;

            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeBounds, s_VisibleProbeVolumeBoundsBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._ProbeVolumeDatas, s_VisibleProbeVolumeDataBuffer);
            cmd.SetGlobalInt(HDShaderIDs._ProbeVolumeCount, m_VisibleProbeVolumeBounds.Count);
        }

        public ProbeVolumeList PrepareVisibleProbeVolumeList(HDCamera hdCamera, CommandBuffer cmd)
        {
            ProbeVolumeList probeVolumes = new ProbeVolumeList();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return probeVolumes;

            using (new ProfilingSample(cmd, "Prepare Probe Volume List"))
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
                List<ProbeVolume> volumes = ProbeVolumeManager.manager.PrepareProbeVolumeData(cmd, hdCamera.camera);

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
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum))
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

                return probeVolumes;
            }
        }

        public void ProbeVolumeLightingPass(HDCamera hdCamera, CommandBuffer cmd, uint frameIndex)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return;
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

    } // class ProbeVolumeSystem
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
