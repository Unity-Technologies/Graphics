using UnityEngine;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal sealed class PreviousFrameData
    {
        #region Fields
        bool m_IsFirstFrame;
        int m_LastFrameActive;
        Matrix4x4 m_ViewProjection;
        Matrix4x4 m_PreviousViewProjection;

#if ENABLE_VR && ENABLE_XR_MODULE
        Matrix4x4[] m_ViewProjectionStereo = new Matrix4x4[2];
        Matrix4x4[] m_PreviousViewProjectionStereo = new Matrix4x4[2];
#endif
        #endregion

        #region Constructors
        internal PreviousFrameData()
        {
            // Set data
            m_IsFirstFrame = true;
            m_LastFrameActive = -1;
            m_ViewProjection = Matrix4x4.identity;
            m_PreviousViewProjection = Matrix4x4.identity;
        }

        #endregion

        #region Properties
        internal bool isFirstFrame
        {
            get => m_IsFirstFrame;
            set => m_IsFirstFrame = value;
        }

        internal int lastFrameActive
        {
            get => m_LastFrameActive;
            set => m_LastFrameActive = value;
        }

        internal Matrix4x4 viewProjection
        {
            get => m_ViewProjection;
            set => m_ViewProjection = value;
        }

        internal Matrix4x4 previousViewProjection
        {
            get => m_PreviousViewProjection;
            set => m_PreviousViewProjection = value;
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal Matrix4x4[] previousViewProjectionStereo
        {
            get => m_PreviousViewProjectionStereo;
            set => m_PreviousViewProjectionStereo = value;
        }

        internal Matrix4x4[] viewProjectionStereo
        {
            get => m_ViewProjectionStereo;
            set => m_ViewProjectionStereo = value;
        }
#endif
    }
    #endregion
}
