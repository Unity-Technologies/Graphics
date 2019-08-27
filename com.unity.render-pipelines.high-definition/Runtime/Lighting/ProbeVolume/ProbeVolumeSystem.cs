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

        public static ProbeVolumeEngineData GetNeutralValues()
        {
            ProbeVolumeEngineData data;

            data.debugColor = Vector3.zero;
            data.payloadIndex  = -1;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpDistFadeLen = 0;
            data.endTimesRcpDistFadeLen = 1;

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

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, uint frameIndex)
        {
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
                ProbeVolume[] volumes = ProbeVolumeManager.manager.PrepareProbeVolumeData(cmd, hdCamera.camera);

                for (int i = 0; i < Math.Min(volumes.Length, k_MaxVisibleProbeVolumeCount); i++)
                {
                    ProbeVolume volume = volumes[i];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum))
                    {
                        // TODO: cache these?
                        var data = volume.parameters.ConvertToEngineData();

                        m_VisibleProbeVolumeBounds.Add(obb);
                        m_VisibleProbeVolumeData.Add(data);
                    }
                }

                s_VisibleProbeVolumeBoundsBuffer.SetData(m_VisibleProbeVolumeBounds);
                s_VisibleProbeVolumeDataBuffer.SetData(m_VisibleProbeVolumeData);

                // Fill the struct with pointers in order to share the data with the light loop.
                probeVolumes.bounds  = m_VisibleProbeVolumeBounds;
                probeVolumes.data = m_VisibleProbeVolumeData;

                return probeVolumes;
            }
        }

    } // class ProbeVolumeSystem
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
