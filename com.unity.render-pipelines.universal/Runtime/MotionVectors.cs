using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    // Motion vector data that persists over frames. (per camera)
    internal sealed class MotionVectorsPersistentData
    {
        #region Fields

        readonly Matrix4x4[] m_ViewProjection = new Matrix4x4[2];
        readonly Matrix4x4[] m_PreviousViewProjection = new Matrix4x4[2];
        readonly int[] m_LastFrameIndex = new int[2];
        readonly float[] m_PrevAspectRatio = new float[2];

        #endregion

        #region Constructors

        internal MotionVectorsPersistentData()
        {
            for (int i = 0; i < m_ViewProjection.Length; i++)
            {
                m_ViewProjection[i] = Matrix4x4.identity;
                m_PreviousViewProjection[i] = Matrix4x4.identity;
                m_LastFrameIndex[i] = -1;
                m_PrevAspectRatio[i] = -1;
            }
        }

        #endregion

        #region Properties

        internal int lastFrameIndex
        {
            get => m_LastFrameIndex[0];
        }

        internal Matrix4x4 viewProjection
        {
            get => m_ViewProjection[0];
        }

        internal Matrix4x4 previousViewProjection
        {
            get => m_PreviousViewProjection[0];
        }

        internal Matrix4x4[] viewProjectionStereo
        {
            get => m_ViewProjection;
        }

        internal Matrix4x4[] previousViewProjectionStereo
        {
            get => m_PreviousViewProjection;
        }
        #endregion

        internal int GetXRMultiPassId(ref CameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            return cameraData.xr.enabled ? cameraData.xr.multipassId : 0;
#else
            return 0;
#endif
        }

        public void Update(ref CameraData cameraData)
        {
            var camera = cameraData.camera;
            int idx = GetXRMultiPassId(ref cameraData);

            // A camera could be rendered multiple times per frame, only update the view projections if needed
            bool aspectChanged = m_PrevAspectRatio[idx] != cameraData.aspectRatio;
            if (m_LastFrameIndex[idx] != Time.frameCount || aspectChanged)
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                {
                    var gpuVP0 = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(0), true) * cameraData.GetViewMatrix(0);
                    var gpuVP1 = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(1), true) * cameraData.GetViewMatrix(1);
                    m_PreviousViewProjection[0] = aspectChanged ? gpuVP0 : m_ViewProjection[0];
                    m_PreviousViewProjection[1] = aspectChanged ? gpuVP1 : m_ViewProjection[1];
                    m_ViewProjection[0] = gpuVP0;
                    m_ViewProjection[1] = gpuVP1;
                }
                else
#endif
                {
                    var gpuVP = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(0), true) * cameraData.GetViewMatrix(0);
                    m_PreviousViewProjection[idx] = aspectChanged ? gpuVP : m_ViewProjection[idx];
                    m_ViewProjection[idx] = gpuVP;
                }

                m_LastFrameIndex[idx] = Time.frameCount;
                m_PrevAspectRatio[idx] = cameraData.aspectRatio;
            }
        }
    }
}
