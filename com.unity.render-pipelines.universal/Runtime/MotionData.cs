using UnityEngine;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal sealed class MotionData
    {
        #region Fields
        bool m_IsFirstFrame;
        int m_LastFrameActive;
        Matrix4x4 m_GpuViewProjectionMatrix;
        Matrix4x4 m_PreviousGpuViewProjectionMatrix;

        Matrix4x4 m_ViewProjectionMatrix;
        Matrix4x4 m_PreviousViewProjectionMatrix;
        #endregion

        #region Constructors
        internal MotionData()
        {
            // Set data
            m_IsFirstFrame = true;
            m_LastFrameActive = -1;
            m_GpuViewProjectionMatrix = Matrix4x4.identity;
            m_PreviousGpuViewProjectionMatrix = Matrix4x4.identity;
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

        internal Matrix4x4 gpuViewProjectionMatrix
        {
            get => m_GpuViewProjectionMatrix;
            set => m_GpuViewProjectionMatrix = value;
        }

        internal Matrix4x4 previousGPUViewProjectionMatrix
        {
            get => m_PreviousGpuViewProjectionMatrix;
            set => m_PreviousGpuViewProjectionMatrix = value;
        }

        internal Matrix4x4 viewProjectionMatrix
        {
            get => m_ViewProjectionMatrix;
            set => m_ViewProjectionMatrix = value;
        }

        internal Matrix4x4 previousViewProjectionMatrix
        {
            get => m_PreviousViewProjectionMatrix;
            set => m_PreviousViewProjectionMatrix = value;
        }
        #endregion
    }
}
