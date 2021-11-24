using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct CapsuleOccluderList
    {
        public List<OrientedBBox> bounds;
        public List<CapsuleOccluderData> occluders;
    };

    public partial class HDRenderPipeline
    {
        private const int k_MaxVisibleCapsuleOccluders = 256;

        List<OrientedBBox> m_VisibleCapsuleOccluderBounds = null;
        List<CapsuleOccluderData> m_VisibleCapsuleOccluderData = null;
        ComputeBuffer m_VisibleCapsuleOccluderDataBuffer = null;

        internal void InitializeCapsuleShadows()
        {
            m_VisibleCapsuleOccluderBounds = new List<OrientedBBox>();
            m_VisibleCapsuleOccluderData = new List<CapsuleOccluderData>();
            m_VisibleCapsuleOccluderDataBuffer = new ComputeBuffer(k_MaxVisibleCapsuleOccluders, Marshal.SizeOf(typeof(CapsuleOccluderData)));
        }

        internal void CleanupCapsuleShadows()
        {
            CoreUtils.SafeRelease(m_VisibleCapsuleOccluderDataBuffer);
            m_VisibleCapsuleOccluderData = null;
            m_VisibleCapsuleOccluderBounds = null;
        }

        internal CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CommandBuffer cmd)
        {
            Vector3 originWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                originWS = hdCamera.camera.transform.position;
            }

            m_VisibleCapsuleOccluderBounds.Clear();
            m_VisibleCapsuleOccluderData.Clear();

            var occluders = CapsuleOccluderManager.instance.occluders;
            foreach (CapsuleOccluder occluder in occluders)
            {
                if (m_VisibleCapsuleOccluderData.Count >= k_MaxVisibleCapsuleOccluders)
                {
                    break;
                }

                CapsuleOccluderData data = occluder.GetOccluderData(originWS);
                // TODO: visibility check range vs frustum

                Vector3 centre = new Vector3(data.centerRWS_radius.x, data.centerRWS_radius.y, data.centerRWS_radius.z);
                float length = 2.0f * data.directionWS_range.w;
                OrientedBBox bbox = new OrientedBBox(
                    Matrix4x4.TRS(centre, Quaternion.identity, new Vector3(length, length, length)));

                m_VisibleCapsuleOccluderBounds.Add(bbox);
                m_VisibleCapsuleOccluderData.Add(data);
            }

            m_VisibleCapsuleOccluderDataBuffer.SetData(m_VisibleCapsuleOccluderData);

            return new CapsuleOccluderList
            {
                bounds = m_VisibleCapsuleOccluderBounds,
                occluders = m_VisibleCapsuleOccluderData,
            };
        }

        internal void BindGlobalCapsuleShadowBuffers(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._CapsuleOccluderDatas, m_VisibleCapsuleOccluderDataBuffer);
        }
    }
}
