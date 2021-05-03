using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal.Internal
{
    sealed class MotionVectorRendering
    {
        #region Fields
        static MotionVectorRendering s_Instance;

        Dictionary<Camera, MotionData> m_MotionDatas;
        uint  m_FrameCount;
        float m_LastTime;
        float m_Time;
        #endregion

        #region Constructors
        private MotionVectorRendering()
        {
            // Set data
            m_MotionDatas = new Dictionary<Camera, MotionData>();
        }

        public static MotionVectorRendering instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new MotionVectorRendering();
                return s_Instance;
            }
        }
        #endregion

        #region RenderPass

        public void Clear()
        {
            m_MotionDatas.Clear();
        }

        public MotionData GetMotionDataForCamera(Camera camera)
        {
            // Get MotionData
            MotionData motionData;
            if (!m_MotionDatas.TryGetValue(camera, out motionData))
            {
                motionData = new MotionData();
                m_MotionDatas.Add(camera, motionData);
            }

            // Calculate motion data
            CalculateTime();
            UpdateMotionData(camera, motionData);
            return motionData;
        }

        #endregion

        void CalculateTime()
        {
            // Get data
            float t = Time.realtimeSinceStartup;
            uint  c = (uint)Time.frameCount;

            // SRP.Render() can be called several times per frame.
            // Also, most Time variables do not consistently update in the Scene View.
            // This makes reliable detection of the start of the new frame VERY hard.
            // One of the exceptions is 'Time.realtimeSinceStartup'.
            // Therefore, outside of the Play Mode we update the time at 60 fps,
            // and in the Play Mode we rely on 'Time.frameCount'.
            bool newFrame;
            if (Application.isPlaying)
            {
                newFrame = m_FrameCount != c;
                m_FrameCount = c;
            }
            else
            {
                newFrame = (t - m_Time) > 0.0166f;
                m_FrameCount += newFrame ? (uint)1 : (uint)0;
            }

            if (newFrame)
            {
                // Make sure both are never 0.
                m_LastTime = (m_Time > 0) ? m_Time : t;
                m_Time  = t;
            }
        }

        void UpdateMotionData(Camera camera, MotionData motionData)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true); // Had to change this from 'false'
            var gpuView = camera.worldToCameraMatrix;
            var gpuVP = gpuProj * gpuView;

            var vp = camera.projectionMatrix * camera.worldToCameraMatrix;

            // Set last frame data
            // A camera could be rendered multiple times per frame, only updates the previous view proj & pos if needed
            if (motionData.lastFrameActive != Time.frameCount)
            {
                motionData.isFirstFrame = false;
                motionData.previousGPUViewProjectionMatrix = motionData.isFirstFrame ?
                    gpuVP : motionData.gpuViewProjectionMatrix;
                motionData.previousViewProjectionMatrix = motionData.isFirstFrame ?
                    vp : motionData.viewProjectionMatrix;
            }

            // Set current frame data
            motionData.gpuViewProjectionMatrix = gpuVP;
            motionData.viewProjectionMatrix = vp;
            motionData.lastFrameActive = Time.frameCount;
        }
    }
}
