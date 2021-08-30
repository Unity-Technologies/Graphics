using UnityEngine;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal sealed class PreviousFrameData
    {
        #region Fields
        bool m_IsFirstFrame;
        int m_LastFrameActive;
        Matrix4x4 m_viewProjectionMatrix;
        Matrix4x4 m_PreviousViewProjectionMatrix;

#if ENABLE_VR && ENABLE_XR_MODULE
        Matrix4x4[] m_ViewProjectionMatrixStereo = new Matrix4x4[2];
        Matrix4x4[] m_PreviousViewProjectionMatrixStereo = new Matrix4x4[2];
#endif
        #endregion

        #region Constructors
        internal PreviousFrameData()
        {
            // Set data
            m_IsFirstFrame = true;
            m_LastFrameActive = -1;
            m_viewProjectionMatrix = Matrix4x4.identity;
            m_PreviousViewProjectionMatrix = Matrix4x4.identity;
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

        internal Matrix4x4 viewProjectionMatrix
        {
            get => m_viewProjectionMatrix;
            set => m_viewProjectionMatrix = value;
        }

        internal Matrix4x4 previousViewProjectionMatrix
        {
            get => m_PreviousViewProjectionMatrix;
            set => m_PreviousViewProjectionMatrix = value;
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal Matrix4x4[] previousViewProjectionMatrixStereo
        {
            get => m_PreviousViewProjectionMatrixStereo;
            set => m_PreviousViewProjectionMatrixStereo = value;
        }

        internal Matrix4x4[] viewProjectionMatrixStereo
        {
            get => m_ViewProjectionMatrixStereo;
            set => m_ViewProjectionMatrixStereo = value;
        }
#endif
    }
    #endregion
}
