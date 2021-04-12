namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Util class for normal reconstruction.
    /// </summary>
    public static class NormalReconstruction
    {
        private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
        private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
        private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");

        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";

        private static Matrix4x4[] s_CameraViewProjections = new Matrix4x4[2];
        private static Vector4[] s_CameraTopLeftCorner = new Vector4[2];
        private static Vector4[] s_CameraXExtent = new Vector4[2];
        private static Vector4[] s_CameraYExtent = new Vector4[2];
        private static Vector4[] s_CameraZExtent = new Vector4[2];

        /// <summary>
        /// Setup properties needed for normal reconstruction from depth using shader functions in NormalReconstruction.hlsl
        /// </summary>
        /// <param name="cmd">Command Buffer used for properties setup.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        public static void SetupProperties(CommandBuffer cmd, in CameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif
            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = cameraData.GetProjectionMatrix(eyeIndex);
                s_CameraViewProjections[eyeIndex] = proj * view;

                // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                Matrix4x4 cview = view;
                cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                Matrix4x4 cviewProj = proj * cview;
                Matrix4x4 cviewProjInv = cviewProj.inverse;

                Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                s_CameraTopLeftCorner[eyeIndex] = topLeftCorner;
                s_CameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                s_CameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                s_CameraZExtent[eyeIndex] = farCentre;
            }

            cmd.SetGlobalVector(s_ProjectionParams2ID, new Vector4(1.0f / cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalMatrixArray(s_CameraViewProjectionsID, s_CameraViewProjections);
            cmd.SetGlobalVectorArray(s_CameraViewTopLeftCornerID, s_CameraTopLeftCorner);
            cmd.SetGlobalVectorArray(s_CameraViewXExtentID, s_CameraXExtent);
            cmd.SetGlobalVectorArray(s_CameraViewYExtentID, s_CameraYExtent);
            cmd.SetGlobalVectorArray(s_CameraViewZExtentID, s_CameraZExtent);

            PostProcessUtils.SetSourceSize(cmd, cameraData.cameraTargetDescriptor);

            // Update keywords
            CoreUtils.SetKeyword(cmd, k_OrthographicCameraKeyword, cameraData.camera.orthographic);
        }
    }
}
