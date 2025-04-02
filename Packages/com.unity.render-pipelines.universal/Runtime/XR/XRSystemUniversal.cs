using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal static class XRSystemUniversal
    {
        // Prevent GC by keeping an array pre-allocated
        static Matrix4x4[] s_projMatrix = new Matrix4x4[2];

#if ENABLE_VR && ENABLE_XR_MODULE
        static MaterialPropertyBlock s_XRSharedPropertyBlock = new MaterialPropertyBlock();
        internal static MaterialPropertyBlock GetMaterialPropertyBlock()
        {
            return s_XRSharedPropertyBlock;
        }
#endif

        internal static void BeginLateLatching(Camera camera, XRPassUniversal xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XR.XRDisplaySubsystem xrDisplay = XRSystem.GetActiveDisplay();

            if (xrDisplay != null && xrPass.viewCount == 2) // multiview only
            {
                xrDisplay.BeginRecordingIfLateLatched(camera);
                xrPass.isLateLatchEnabled = true;
            }
#endif
        }

        internal static void EndLateLatching(Camera camera, XRPassUniversal xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XR.XRDisplaySubsystem xrDisplay = XRSystem.GetActiveDisplay();

            if (xrDisplay != null && xrPass.isLateLatchEnabled)
            {
                xrDisplay.EndRecordingIfLateLatched(camera);
                xrPass.isLateLatchEnabled = false;
            }
#endif
        }

        internal static void UnmarkShaderProperties(RasterCommandBuffer cmd, XRPassUniversal xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass.isLateLatchEnabled && xrPass.hasMarkedLateLatch)
            {
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.View);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.InverseView);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.ViewProjection);
                cmd.UnmarkLateLatchMatrix(CameraLateLatchMatrixType.InverseViewProjection);
                xrPass.hasMarkedLateLatch = false;
            }
#endif
        }

        internal static void MarkShaderProperties(RasterCommandBuffer cmd, XRPassUniversal xrPass, bool renderIntoTexture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass.isLateLatchEnabled && xrPass.canMarkLateLatch)
            {
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.View, XRBuiltinShaderConstants.unity_StereoMatrixV);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.InverseView, XRBuiltinShaderConstants.unity_StereoMatrixInvV);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.ViewProjection, XRBuiltinShaderConstants.unity_StereoMatrixVP);
                cmd.MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType.InverseViewProjection, XRBuiltinShaderConstants.unity_StereoMatrixInvVP);

                for (int viewIndex = 0; viewIndex < 2; ++viewIndex)
                    s_projMatrix[viewIndex] = GL.GetGPUProjectionMatrix(xrPass.GetProjMatrix(viewIndex), renderIntoTexture);

                cmd.SetLateLatchProjectionMatrices(s_projMatrix);
                xrPass.hasMarkedLateLatch = true;
            }
#endif
        }
    }
}
