using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    sealed class MotionVectorRenderPass : ScriptableRenderPass
    {
        #region Fields
        const string kPreviousViewProjectionMatrix = "_PrevViewProjMatrix";
        const string kViewProjection = "_ViewProjMatrix";
#if ENABLE_VR && ENABLE_XR_MODULE
        const string kPreviousViewProjectionStereo = "_PrevViewProjMatrixStereo";
        const string kViewProjectionStereo = "_ViewProjMatrixStereo";
#endif
        const GraphicsFormat m_TargetFormat = GraphicsFormat.R16G16_SFloat;

        static readonly string[] s_ShaderTags = new string[] { "MotionVectors" };
        static readonly int kTargetID = Shader.PropertyToID("_MotionVectorTexture");
        readonly RenderTargetIdentifier m_MotionVectorTarget;
        readonly Material m_CameraMaterial;
        readonly Material m_ObjectMaterial;

        PreviousFrameData m_MotionData;
        ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(URPProfileId.MotionVectors);
        #endregion

        #region Constructors
        internal MotionVectorRenderPass(Material cameraMaterial, Material objectMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            m_CameraMaterial = cameraMaterial;
            m_ObjectMaterial = objectMaterial;
            m_MotionVectorTarget = new RenderTargetIdentifier(kTargetID, 0, CubemapFace.Unknown, -1);
        }

        #endregion

        #region State
        internal void Setup(PreviousFrameData frameData)
        {
            m_MotionData = frameData;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var rtd = cameraTextureDescriptor;
            rtd.graphicsFormat = m_TargetFormat;

            cmd.GetTemporaryRT(kTargetID, rtd, FilterMode.Point);
            ConfigureTarget(m_MotionVectorTarget, m_MotionVectorTarget);
            ConfigureClear(ClearFlag.Color, Color.black);
        }

        #endregion

        #region Execution

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CameraMaterial == null || m_ObjectMaterial == null)
                return;

            // Get data
            var camera = renderingData.cameraData.camera;

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                ExecuteCommand(context, cmd);
                var cameraData = renderingData.cameraData;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                {
                    cmd.SetGlobalMatrixArray(kPreviousViewProjectionStereo, m_MotionData.previousViewProjectionStereo);
                    cmd.SetGlobalMatrixArray(kViewProjectionStereo, m_MotionData.viewProjectionStereo);
                }
                else if (cameraData.xr.enabled)
                {
                    int passID = cameraData.xr.multipassId;
                    cmd.SetGlobalMatrix(kPreviousViewProjectionMatrix, m_MotionData.previousViewProjectionStereo[passID]);
                    cmd.SetGlobalMatrix(kViewProjection, m_MotionData.viewProjectionStereo[passID]);
                }
                else
#endif
                {
                    cmd.SetGlobalMatrix(kPreviousViewProjectionMatrix, m_MotionData.previousViewProjection);
                    cmd.SetGlobalMatrix(kViewProjection, m_MotionData.viewProjection);
                }

                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                // TODO: add option to only draw either one?
                DrawCameraMotionVectors(context, cmd, camera);
                DrawObjectMotionVectors(context, ref renderingData, camera);
            }
            ExecuteCommand(context, cmd);
            CommandBufferPool.Release(cmd);
        }

        DrawingSettings GetDrawingSettings(ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(ShaderTagId.none, sortingSettings)
            {
                perObjectData = PerObjectData.MotionVectors,
                enableDynamicBatching = renderingData.supportsDynamicBatching,
                enableInstancing = true,
            };

            for (int i = 0; i < s_ShaderTags.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, new ShaderTagId(s_ShaderTags[i]));
            }

            // Material that will be used if shader tags cannot be found
            drawingSettings.fallbackMaterial = m_ObjectMaterial;

            return drawingSettings;
        }

        void DrawCameraMotionVectors(ScriptableRenderContext context, CommandBuffer cmd, Camera camera)
        {
            // Draw fullscreen quad
            cmd.DrawProcedural(Matrix4x4.identity, m_CameraMaterial, 0, MeshTopology.Triangles, 3, 1);
            ExecuteCommand(context, cmd);
        }

        void DrawObjectMotionVectors(ScriptableRenderContext context, ref RenderingData renderingData, Camera camera)
        {
            var drawingSettings = GetDrawingSettings(ref renderingData);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, camera.cullingMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            // Draw Renderers
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
        }

        #endregion

        #region Cleanup
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(kTargetID);
        }

        #endregion

        #region CommandBufer
        void ExecuteCommand(ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        #endregion
    }
}
