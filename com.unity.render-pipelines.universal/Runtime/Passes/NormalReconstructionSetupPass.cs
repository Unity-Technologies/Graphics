namespace UnityEngine.Rendering.Universal
{
    public class NormalReconstructionSetupPass : ScriptableRenderPass
    {
        private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
        private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
        private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");

        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";

        private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
        private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
        private Vector4[] m_CameraXExtent = new Vector4[2];
        private Vector4[] m_CameraYExtent = new Vector4[2];
        private Vector4[] m_CameraZExtent = new Vector4[2];

        private ProfilingSampler m_ProfilingSampler;

        public NormalReconstructionSetupPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;

            m_ProfilingSampler = new ProfilingSampler("Normal Reconstruction Setup");
        }

        private void SetupNormalReconstructProperties(CommandBuffer cmd, ref RenderingData renderingData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = renderingData.cameraData.xr.enabled && renderingData.cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif
            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = renderingData.cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix(eyeIndex);
                m_CameraViewProjections[eyeIndex] = proj * view;

                // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                Matrix4x4 cview = view;
                cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                Matrix4x4 cviewProj = proj * cview;
                Matrix4x4 cviewProjInv = cviewProj.inverse;

                Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                m_CameraTopLeftCorner[eyeIndex] = topLeftCorner;
                m_CameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                m_CameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                m_CameraZExtent[eyeIndex] = farCentre;
            }

            cmd.SetGlobalVector(s_ProjectionParams2ID, new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalMatrixArray(s_CameraViewProjectionsID, m_CameraViewProjections);
            cmd.SetGlobalVectorArray(s_CameraViewTopLeftCornerID, m_CameraTopLeftCorner);
            cmd.SetGlobalVectorArray(s_CameraViewXExtentID, m_CameraXExtent);
            cmd.SetGlobalVectorArray(s_CameraViewYExtentID, m_CameraYExtent);
            cmd.SetGlobalVectorArray(s_CameraViewZExtentID, m_CameraZExtent);

            PostProcessUtils.SetSourceSize(cmd, renderingData.cameraData.cameraTargetDescriptor);

            // Update keywords
            CoreUtils.SetKeyword(cmd, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SetupNormalReconstructProperties(cmd, ref renderingData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
