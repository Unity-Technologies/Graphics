using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    sealed class MotionVectorRenderPass : ScriptableRenderPass
    {
        #region Fields
        const string kPreviousViewProjectionMatrix = "_PrevViewProjMatrix";
#if ENABLE_VR && ENABLE_XR_MODULE
        const string kPreviousViewProjectionMatrixStero = "_PrevViewProjMStereo";
#endif
        const string kMotionVectorTexture = "_MotionVectorTexture";
        const GraphicsFormat m_TargetFormat = GraphicsFormat.R16G16_SFloat;

        static readonly string[] s_ShaderTags = new string[] { "MotionVectors" };

        RenderTargetHandle m_MotionVectorHandle; //Move to UniversalRenderer like other passes?
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
            // Configure Render Target
            m_MotionVectorHandle.Init(kMotionVectorTexture);
            cmd.GetTemporaryRT(m_MotionVectorHandle.id, rtd, FilterMode.Point);
            ConfigureTarget(m_MotionVectorHandle.Identifier(), m_MotionVectorHandle.Identifier());
        }

        #endregion

        #region Execution

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CameraMaterial == null || m_ObjectMaterial == null)
                return;

            // Get data
            ref var cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            MotionVectorsPersistentData motionData = null;

            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                motionData = additionalCameraData.motionVectorsPersistentData;

            if (motionData == null)
                return;

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                int passID = motionData.GetXRMultiPassId(ref cameraData);

                ExecuteCommand(context, cmd);
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                {
                    m_CameraMaterial.SetMatrixArray(kPreviousViewProjectionMatrixStero, m_MotionData.previousViewProjectionMatrixStereo);
                    m_ObjectMaterial.SetMatrixArray(kPreviousViewProjectionMatrixStero, m_MotionData.previousViewProjectionMatrixStereo);
                }
                else
#endif
                {
                    Shader.SetGlobalMatrix(kPreviousViewProjectionMatrix, m_MotionData.previousViewProjectionMatrix);
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

            // Reset Render Target
            if (m_MotionVectorHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(m_MotionVectorHandle.id);
                m_MotionVectorHandle = RenderTargetHandle.CameraTarget;
            }
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
