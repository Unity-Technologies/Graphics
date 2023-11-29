using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // Motion vector data that persists over frames. (per camera)
    internal sealed class MotionVectorsPersistentData
    {
        #region Fields

        private const int k_EyeCount = 2;

        readonly Matrix4x4[] m_Projection = new Matrix4x4[k_EyeCount];
        readonly Matrix4x4[] m_View = new Matrix4x4[k_EyeCount];
        readonly Matrix4x4[] m_ViewProjection = new Matrix4x4[k_EyeCount];

        readonly Matrix4x4[] m_PreviousProjection = new Matrix4x4[k_EyeCount];
        readonly Matrix4x4[] m_PreviousView = new Matrix4x4[k_EyeCount];
        readonly Matrix4x4[] m_PreviousViewProjection = new Matrix4x4[k_EyeCount];

        readonly Matrix4x4[] m_PreviousPreviousProjection = new Matrix4x4[k_EyeCount];
        readonly Matrix4x4[] m_PreviousPreviousView = new Matrix4x4[k_EyeCount];

        readonly int[] m_LastFrameIndex = new int[k_EyeCount];
        readonly float[] m_PrevAspectRatio = new float[k_EyeCount];

        float m_deltaTime;
        float m_lastDeltaTime;

        Vector3 m_worldSpaceCameraPos;
        Vector3 m_previousWorldSpaceCameraPos;
        Vector3 m_previousPreviousWorldSpaceCameraPos;

        #endregion

        #region Constructors

        internal MotionVectorsPersistentData()
        {
            Reset();
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

        internal Matrix4x4[] projectionStereo
        {
            get => m_Projection;
        }

        internal Matrix4x4[] previousProjectionStereo
        {
            get => m_PreviousProjection;
        }

        internal Matrix4x4[] previousPreviousProjectionStereo
        {
            get => m_PreviousPreviousProjection;
        }

        internal Matrix4x4[] viewStereo
        {
            get => m_View;
        }

        internal Matrix4x4[] previousViewStereo
        {
            get => m_PreviousView;
        }

        internal Matrix4x4[] previousPreviousViewStereo
        {
            get => m_PreviousPreviousView;
        }

        internal float deltaTime
        {
            get => m_deltaTime;
        }

        internal float lastDeltaTime
        {
            get => m_lastDeltaTime;
        }

        internal Vector3 worldSpaceCameraPos
        {
            get => m_worldSpaceCameraPos;
        }

        internal Vector3 previousWorldSpaceCameraPos
        {
            get => m_previousWorldSpaceCameraPos;
        }

        internal Vector3 previousPreviousWorldSpaceCameraPos
        {
            get => m_previousPreviousWorldSpaceCameraPos;
        }
        #endregion

        public void Reset()
        {
            for (int i = 0; i < k_EyeCount; i++)
            {
                m_Projection[i] = Matrix4x4.identity;
                m_View[i] = Matrix4x4.identity;
                m_ViewProjection[i] = Matrix4x4.identity;

                m_PreviousProjection[i] = Matrix4x4.identity;
                m_PreviousView[i] = Matrix4x4.identity;
                m_PreviousViewProjection[i] = Matrix4x4.identity;

                m_PreviousProjection[i] = Matrix4x4.identity;
                m_PreviousView[i] = Matrix4x4.identity;
                m_PreviousViewProjection[i] = Matrix4x4.identity;

                m_LastFrameIndex[i] = -1;
                m_PrevAspectRatio[i] = -1;
            }

            m_deltaTime = 0.0f;
            m_lastDeltaTime = 0.0f;

            m_worldSpaceCameraPos = Vector3.zero;
            m_previousWorldSpaceCameraPos = Vector3.zero;
            m_previousPreviousWorldSpaceCameraPos = Vector3.zero;
        }

        static private int GetXRMultiPassId(XRPass xr)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            return xr.enabled ? xr.multipassId : 0;
#else
            return 0;
#endif
        }

        public void Update(UniversalCameraData cameraData)
        {
            int eyeIndex = GetXRMultiPassId(cameraData.xr);

            // XR multipass renders the frame twice, avoid updating camera history twice.
            bool xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;

            bool isNewFrame = !xrMultipassEnabled || (eyeIndex == 0);

            int frameIndex = Time.frameCount;

            // Update per-frame data once regardless of how many eyes are being rendered
            if (isNewFrame)
            {
                // For per-frame data, we only care if this is the first frame for eye zero because eye zero
                // is always updated once per frame regardless of how XR is configured.
                bool isPreviousFrameDataInvalid = (m_LastFrameIndex[0] == -1);

                float deltaTime = Time.deltaTime;
                Vector3 worldSpaceCameraPos = cameraData.camera.transform.position;

                // Use the current delta time if the previous time is invalid
                if (isPreviousFrameDataInvalid)
                {
                    m_lastDeltaTime = deltaTime;
                    m_deltaTime = deltaTime;

                    m_previousPreviousWorldSpaceCameraPos = worldSpaceCameraPos;
                    m_previousWorldSpaceCameraPos = worldSpaceCameraPos;
                    m_worldSpaceCameraPos = worldSpaceCameraPos;
                }

                m_lastDeltaTime = m_deltaTime;
                m_deltaTime = deltaTime;

                m_previousPreviousWorldSpaceCameraPos = m_previousWorldSpaceCameraPos;
                m_previousWorldSpaceCameraPos = m_worldSpaceCameraPos;
                m_worldSpaceCameraPos = worldSpaceCameraPos;
            }

            // A camera could be rendered multiple times per frame, only update the view projections if needed
            bool aspectChanged = m_PrevAspectRatio[eyeIndex] != cameraData.aspectRatio;
            if (m_LastFrameIndex[eyeIndex] != frameIndex || aspectChanged)
            {
                bool isPreviousFrameDataInvalid = (m_LastFrameIndex[eyeIndex] == -1) || aspectChanged;

                int numActiveViews = cameraData.xr.enabled ? cameraData.xr.viewCount : 1;

                // Make sure we don't try to handle more views than we expect to support
                Debug.Assert(numActiveViews <= k_EyeCount);

                for (int viewIndex = 0; viewIndex < numActiveViews; ++viewIndex)
                {
                    int targetIndex = viewIndex + eyeIndex;

                    var gpuP = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrixNoJitter(viewIndex), true);

                    var gpuV = cameraData.GetViewMatrix(viewIndex);

                    var gpuVP = gpuP * gpuV;

                    // If the data for the previous frame is invalid, we need to set all previous frame data
                    // to the current frame data to avoid generating invalid motion vectors
                    if (isPreviousFrameDataInvalid)
                    {
                        m_PreviousPreviousProjection[targetIndex] = gpuP;
                        m_PreviousProjection[targetIndex] = gpuP;
                        m_Projection[targetIndex] = gpuP;

                        m_PreviousPreviousView[targetIndex] = gpuV;
                        m_PreviousView[targetIndex] = gpuV;
                        m_View[targetIndex] = gpuV;

                        m_ViewProjection[targetIndex] = gpuVP;
                        m_PreviousViewProjection[targetIndex] = gpuVP;
                    }

                    // Shift all matrices to the next position
                    m_PreviousPreviousProjection[targetIndex] = m_PreviousProjection[targetIndex];
                    m_PreviousProjection[targetIndex] = m_Projection[targetIndex];
                    m_Projection[targetIndex] = gpuP;

                    m_PreviousPreviousView[targetIndex] = m_PreviousView[targetIndex];
                    m_PreviousView[targetIndex] = m_View[targetIndex];
                    m_View[targetIndex] = gpuV;

                    m_PreviousViewProjection[targetIndex] = m_ViewProjection[targetIndex];
                    m_ViewProjection[targetIndex] = gpuVP;
                }

                m_LastFrameIndex[eyeIndex] = frameIndex;
                m_PrevAspectRatio[eyeIndex] = cameraData.aspectRatio;
            }
        }

        // Set global motion vector matrix GPU constants.
        public void SetGlobalMotionMatrices(RasterCommandBuffer cmd, XRPass xr)
        {
            var passID = GetXRMultiPassId(xr);
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
            {
                cmd.SetGlobalMatrixArray(ShaderPropertyId.previousViewProjectionNoJitterStereo, previousViewProjectionStereo);
                cmd.SetGlobalMatrixArray(ShaderPropertyId.viewProjectionNoJitterStereo, viewProjectionStereo);
            }
            else
#endif
            {
                cmd.SetGlobalMatrix(ShaderPropertyId.previousViewProjectionNoJitter, previousViewProjectionStereo[passID]);
                cmd.SetGlobalMatrix(ShaderPropertyId.viewProjectionNoJitter, viewProjectionStereo[passID]);
            }
        }
    }
}
