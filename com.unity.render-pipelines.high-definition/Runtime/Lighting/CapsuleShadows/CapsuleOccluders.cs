using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct CapsuleOccluderList
        {
            public List<OrientedBBox>            bounds;
            public List<EllipsoidOccluderData>   occluders;
        }
        
        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        ComputeBuffer                 m_VisibleCapsuleOccludersBuffer           = null;
        ComputeBuffer                 m_VisibleCapsuleOccludersDataBuffer       = null;
        
        private const int k_MaxCapsuleOccludersCount                            = 256;
        List<OrientedBBox>            m_VisibleCapsuleOccludersBounds           = null;
        List<EllipsoidOccluderData>   m_VisibleCapsuleOccludersData             = null;
        
        
        CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CommandBuffer cmd, float time)
        {
            CapsuleOccluderList capsuleOccluderVolumes = new CapsuleOccluderList();
            //if (!Fog.IsVolumetricFogEnabled(hdCamera))
            //    return capsuleOccluderVolumes; 
            //TODO: add a flag to enable and disable this.

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareVisibleCapsuleOccludersList))) 
            {
                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset   = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleCapsuleOccludersBounds.Clear();
                m_VisibleCapsuleOccludersData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var occluders = EllipsoidOccluderManager.manager.PrepareEllipsoidOccludersData(cmd, hdCamera, time);

                for (int i = 0; i < Math.Min(occluders.Count, k_MaxCapsuleOccludersCount); i++)
                {
                    EllipsoidOccluder occluder = occluders[i];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(occluder.transform.position, occluder.transform.rotation, Vector3.one * occluder.radius));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        // TODO: cache these?
                        var data = occluder.ConvertToEngineData(camOffset);

                        m_VisibleCapsuleOccludersBounds.Add(obb);
                        m_VisibleCapsuleOccludersData.Add(data);
                    }
                }

                m_VisibleCapsuleOccludersBuffer.SetData(m_VisibleCapsuleOccludersBounds);
                m_VisibleCapsuleOccludersDataBuffer.SetData(m_VisibleCapsuleOccludersData);

                // Fill the struct with pointers in order to share the data with the light loop.
                capsuleOccluderVolumes.bounds  = m_VisibleCapsuleOccludersBounds;
                capsuleOccluderVolumes.occluders = m_VisibleCapsuleOccludersData;

                return capsuleOccluderVolumes;
            }
        }
        
        void InitializeCapsuleOccluders()
        {
            /*m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return
                */

            //m_VolumeVoxelizationCS = defaultResources.shaders.volumeVoxelizationCS;
            CreateCapsuleOccluderBuffers();
        }
        
        void CleanupCapsuleOccluders()
        {
            DestroyCapsuleOccluderBuffers();
        }

        internal void CreateCapsuleOccluderBuffers()
        {
            m_VisibleCapsuleOccludersBounds       = new List<OrientedBBox>();
            m_VisibleCapsuleOccludersData         = new List<EllipsoidOccluderData>();
            m_VisibleCapsuleOccludersBuffer       = new ComputeBuffer(k_MaxCapsuleOccludersCount, Marshal.SizeOf(typeof(OrientedBBox)));
            m_VisibleCapsuleOccludersDataBuffer   = new ComputeBuffer(k_MaxCapsuleOccludersCount, Marshal.SizeOf(typeof(EllipsoidOccluderData)));
        }

        internal void DestroyCapsuleOccluderBuffers()
        {
            CoreUtils.SafeRelease(m_VisibleCapsuleOccludersBuffer);
            CoreUtils.SafeRelease(m_VisibleCapsuleOccludersDataBuffer);

            m_VisibleCapsuleOccludersData   = null; // free()
            m_VisibleCapsuleOccludersBounds = null; // free()
        }
    }
}