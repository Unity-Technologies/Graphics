using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class CapsuleOcclusionSystem
    {
        // Culling resources
        ComputeBuffer m_VisibleCapsuleOccludersBuffer = null;
        ComputeBuffer m_VisibleCapsuleOccludersDataBuffer = null;

        private const int k_MaxVisibleCapsuleOccludersCount = 256;
        List<OrientedBBox> m_VisibleCapsuleOccludersBounds = null;
        List<EllipsoidOccluderData> m_VisibleCapsuleOccludersData = null;

        internal CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CommandBuffer cmd, float time)
        {
            CapsuleOccluderList capsuleOccluderVolumes = new CapsuleOccluderList();
            if (!CapsuleSoftShadows.IsCapsuleSoftShadowsEnabled(hdCamera))
                return capsuleOccluderVolumes; 

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareVisibleCapsuleOccludersList)))
            {
                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleCapsuleOccludersBounds.Clear();
                m_VisibleCapsuleOccludersData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var occluders = EllipsoidOccluderManager.manager.PrepareEllipsoidOccludersData(cmd, hdCamera, time);

                for (int i = 0; i < Math.Min(occluders.Count, k_MaxVisibleCapsuleOccludersCount); i++)
                {
                    EllipsoidOccluder occluder = occluders[i];

                    // TODO: cache these?
                    EllipsoidOccluderData data = occluder.ConvertToEngineData(camOffset);

                    Vector3 positionRWS = new Vector3(data.positionRWS_radius.x, data.positionRWS_radius.y, data.positionRWS_radius.z);
                    Vector3 directionWS = new Vector3(data.directionWS_influence.x, data.directionWS_influence.y, data.directionWS_influence.z);
                    Quaternion rotationWS = Quaternion.FromToRotation(Vector3.forward, directionWS.normalized);
                    Vector3 scaleWS = Vector3.one * data.directionWS_influence.w * 2.0f;

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(positionRWS, rotationWS, scaleWS));

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        m_VisibleCapsuleOccludersBounds.Add(obb);
                        m_VisibleCapsuleOccludersData.Add(data);
                    }
                }

                m_VisibleCapsuleOccludersBuffer.SetData(m_VisibleCapsuleOccludersBounds);
                m_VisibleCapsuleOccludersDataBuffer.SetData(m_VisibleCapsuleOccludersData);

                // Fill the struct with pointers in order to share the data with the light loop.
                capsuleOccluderVolumes.bounds = m_VisibleCapsuleOccludersBounds;
                capsuleOccluderVolumes.occluders = m_VisibleCapsuleOccludersData;

                return capsuleOccluderVolumes;
            }
        }

        internal void InitializeCapsuleOccluders()
        {
            /*m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return
                */

            //m_VolumeVoxelizationCS = defaultResources.shaders.volumeVoxelizationCS;
            CreateCapsuleOccluderBuffers();
        }

        internal void CleanupCapsuleOccluders()
        {
            DestroyCapsuleOccluderBuffers();
        }

        internal void CreateCapsuleOccluderBuffers()
        {
            m_VisibleCapsuleOccludersBounds = new List<OrientedBBox>();
            m_VisibleCapsuleOccludersData = new List<EllipsoidOccluderData>();
            m_VisibleCapsuleOccludersBuffer = new ComputeBuffer(k_MaxVisibleCapsuleOccludersCount, Marshal.SizeOf(typeof(OrientedBBox)));
            m_VisibleCapsuleOccludersDataBuffer = new ComputeBuffer(k_MaxVisibleCapsuleOccludersCount, Marshal.SizeOf(typeof(EllipsoidOccluderData)));
        }

        internal void DestroyCapsuleOccluderBuffers()
        {
            CoreUtils.SafeRelease(m_VisibleCapsuleOccludersBuffer);
            CoreUtils.SafeRelease(m_VisibleCapsuleOccludersDataBuffer);

            m_VisibleCapsuleOccludersData = null; // free()
            m_VisibleCapsuleOccludersBounds = null; // free()
        }

    }
}
