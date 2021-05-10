namespace UnityEngine.Rendering
{
    /// <summary>
    /// Helper static class used by render pipelines to setup stereo constants accessed by builtin shaders.
    /// </summary>
    public static class XRBuiltinShaderConstants
    {
        // References to builtin shader constants
        static readonly int k_StereoCameraProjection    = Shader.PropertyToID("unity_StereoCameraProjection");
        static readonly int k_StereoCameraInvProjection = Shader.PropertyToID("unity_StereoCameraInvProjection");
        static readonly int k_StereoMatrixV             = Shader.PropertyToID("unity_StereoMatrixV");
        static readonly int k_StereoMatrixInvV          = Shader.PropertyToID("unity_StereoMatrixInvV");
        static readonly int k_StereoMatrixP             = Shader.PropertyToID("unity_StereoMatrixP");
        static readonly int k_StereoMatrixInvP          = Shader.PropertyToID("unity_StereoMatrixInvP");
        static readonly int k_StereoMatrixVP            = Shader.PropertyToID("unity_StereoMatrixVP");
        static readonly int k_StereoMatrixInvVP         = Shader.PropertyToID("unity_StereoMatrixInvVP");
        static readonly int k_StereoWorldSpaceCameraPos = Shader.PropertyToID("unity_StereoWorldSpaceCameraPos");

        // Pre-allocate arrays to avoid GC
        static Matrix4x4[] s_cameraProjMatrix       = new Matrix4x4[2];
        static Matrix4x4[] s_invCameraProjMatrix    = new Matrix4x4[2];
        static Matrix4x4[] s_viewMatrix             = new Matrix4x4[2];
        static Matrix4x4[] s_invViewMatrix          = new Matrix4x4[2];
        static Matrix4x4[] s_projMatrix             = new Matrix4x4[2];
        static Matrix4x4[] s_invProjMatrix          = new Matrix4x4[2];
        static Matrix4x4[] s_viewProjMatrix         = new Matrix4x4[2];
        static Matrix4x4[] s_invViewProjMatrix      = new Matrix4x4[2];
        static Vector4[]   s_worldSpaceCameraPos    = new Vector4[2];

        /// <summary>
        /// Populate and upload shader constants used by the C++ builtin renderer.
        /// This is required to maintain compatibility with legacy code and shaders.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="renderIntoTexture"></param>
        public static void Update(XRPass xrPass, CommandBuffer cmd, bool renderIntoTexture)
        {
            if (xrPass.enabled)
            {
                cmd.SetViewProjectionMatrices(xrPass.GetViewMatrix(), xrPass.GetProjMatrix());

                if (xrPass.singlePassEnabled)
                {
                    for (int viewIndex = 0; viewIndex < 2; ++viewIndex)
                    {
                        s_cameraProjMatrix[viewIndex]     = xrPass.GetProjMatrix(viewIndex);
                        s_viewMatrix[viewIndex]           = xrPass.GetViewMatrix(viewIndex);
                        s_projMatrix[viewIndex]           = GL.GetGPUProjectionMatrix(s_cameraProjMatrix[viewIndex], renderIntoTexture);
                        s_viewProjMatrix[viewIndex]       = s_projMatrix[viewIndex] * s_viewMatrix[viewIndex];

                        s_invCameraProjMatrix[viewIndex]  = Matrix4x4.Inverse(s_cameraProjMatrix[viewIndex]);
                        s_invViewMatrix[viewIndex]        = Matrix4x4.Inverse(s_viewMatrix[viewIndex]);
                        s_invProjMatrix[viewIndex]        = Matrix4x4.Inverse(s_projMatrix[viewIndex]);
                        s_invViewProjMatrix[viewIndex]    = Matrix4x4.Inverse(s_viewProjMatrix[viewIndex]);

                        s_worldSpaceCameraPos[viewIndex]  = s_invViewMatrix[viewIndex].GetColumn(3);
                    }

                    cmd.SetGlobalMatrixArray(k_StereoCameraProjection, s_cameraProjMatrix);
                    cmd.SetGlobalMatrixArray(k_StereoCameraInvProjection, s_invCameraProjMatrix);
                    cmd.SetGlobalMatrixArray(k_StereoMatrixV, s_viewMatrix);
                    cmd.SetGlobalMatrixArray(k_StereoMatrixInvV, s_invViewMatrix);
                    cmd.SetGlobalMatrixArray(k_StereoMatrixP, s_projMatrix);
                    cmd.SetGlobalMatrixArray(k_StereoMatrixInvP, s_invProjMatrix);
                    cmd.SetGlobalMatrixArray(k_StereoMatrixVP, s_viewProjMatrix);
                    cmd.SetGlobalMatrixArray(k_StereoMatrixInvVP, s_invViewProjMatrix);
                    cmd.SetGlobalVectorArray(k_StereoWorldSpaceCameraPos, s_worldSpaceCameraPos);
                }
            }
        }
    };
}
