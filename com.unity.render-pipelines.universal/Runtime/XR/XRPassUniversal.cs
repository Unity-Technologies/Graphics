using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal class XRPassUniversal : XRPass
    {
        public static XRPass Create(XRPassCreateInfo createInfo)
        {
            XRPassUniversal pass = GenericPool<XRPassUniversal>.Get();
            pass.InitBase(createInfo);

            // Initialize fields specific to Universal
            pass.isLateLatchEnabled = false;
            pass.canMarkLateLatch = false;
            pass.hasMarkedLateLatch = false;

            return pass;
        }

        override public void Release()
        {
            GenericPool<XRPassUniversal>.Release(this);
        }

        /// If true, late latching mechanism is available for the frame.
        internal bool isLateLatchEnabled { get; set; }

        /// Used by the render pipeline to control the granularity of late latching.
        internal bool canMarkLateLatch { get; set; }

        /// Track the state of the late latching system.
        internal bool hasMarkedLateLatch { get; set; }

        // Prevent GC by keeping an array pre-allocated
        static Matrix4x4[] s_projMatrix = new Matrix4x4[2];

        internal void BeginLateLatching(Camera camera)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XR.XRDisplaySubsystem xrDisplay = XRSystem.GetActiveDisplay();

            if (xrDisplay != null && viewCount == 2) // multiview only
            {
                xrDisplay.BeginRecordingIfLateLatched(camera);
                isLateLatchEnabled = true;
            }
#endif
        }

        internal void EndLateLatching(Camera camera)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XR.XRDisplaySubsystem xrDisplay = XRSystem.GetActiveDisplay();

            if (xrDisplay != null && isLateLatchEnabled)
            {
                xrDisplay.EndRecordingIfLateLatched(camera);
                isLateLatchEnabled = false;
            }
#endif
        }

        internal void UnmarkShaderProperties(CommandBuffer cmd)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (isLateLatchEnabled && hasMarkedLateLatch)
            {
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.View);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.InverseView);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.ViewProjection);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.InverseViewProjection);
                hasMarkedLateLatch = false;
            }
#endif
        }

        internal void MarkShaderProperties(CommandBuffer cmd, bool renderIntoTexture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (isLateLatchEnabled && canMarkLateLatch)
            {
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.View, XRBuiltinShaderConstants.unity_StereoMatrixV);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.InverseView, XRBuiltinShaderConstants.unity_StereoMatrixInvV);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.ViewProjection, XRBuiltinShaderConstants.unity_StereoMatrixVP);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.InverseViewProjection, XRBuiltinShaderConstants.unity_StereoMatrixInvVP);

                for (int viewIndex = 0; viewIndex < 2; ++viewIndex)
                    s_projMatrix[viewIndex] = GL.GetGPUProjectionMatrix(GetProjMatrix(viewIndex), renderIntoTexture);

                cmd.SetLateLatchProjectionMatrices(s_projMatrix);
                hasMarkedLateLatch = true;
            }
#endif
        }
    }
}
