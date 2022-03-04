using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    sealed class MotionVectorRenderPass : ScriptableRenderPass
    {
        #region Fields
        const string kPreviousViewProjectionMatrix = "_PrevViewProjMatrix";
#if ENABLE_VR && ENABLE_XR_MODULE
        const string kPreviousViewProjectionMatrixStero = "_PrevViewProjMStereo";
#endif
        internal static readonly GraphicsFormat m_TargetFormat = GraphicsFormat.R16G16_SFloat;

        static readonly string[] s_ShaderTags = new string[] { "MotionVectors" };

        RTHandle m_Color;
        RTHandle m_Depth;
        readonly Material m_CameraMaterial;
        readonly Material m_ObjectMaterial;

        PreviousFrameData m_MotionData;
        ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(URPProfileId.MotionVectors);
        private PassData m_PassData;
        #endregion

        #region Constructors
        internal MotionVectorRenderPass(Material cameraMaterial, Material objectMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            m_CameraMaterial = cameraMaterial;
            m_ObjectMaterial = objectMaterial;
            m_PassData = new PassData();
            base.profilingSampler = new ProfilingSampler("Motion Vector Pass");

        }

        #endregion

        #region State
        internal void Setup(RTHandle color, RTHandle depth, PreviousFrameData frameData)
        {
            m_MotionData = frameData;
            m_Color = color;
            m_Depth = depth;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.SetGlobalTexture(m_Color.name, m_Color.nameID);
            cmd.SetGlobalTexture(m_Depth.name, m_Depth.nameID);
            ConfigureTarget(m_Color, m_Depth);
        }

        #endregion

        #region Execution
        private static void ExecutePass(ScriptableRenderContext context, PassData passData)
        {
            var cameraMaterial = passData.cameraMaterial;
            var objectMaterial = passData.objectMaterial;
            var motionData = passData.motionData;
            var renderingData = passData.renderingData;

            if (cameraMaterial == null || objectMaterial == null)
                return;

            // Get data
            var camera = renderingData.cameraData.camera;

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.MotionVectors)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                var cameraData = renderingData.cameraData;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                {
                    cameraMaterial.SetMatrixArray(kPreviousViewProjectionMatrixStero, motionData.previousViewProjectionMatrixStereo);
                    objectMaterial.SetMatrixArray(kPreviousViewProjectionMatrixStero, motionData.previousViewProjectionMatrixStereo);
                }
                else
#endif
                {
                    Shader.SetGlobalMatrix(kPreviousViewProjectionMatrix, motionData.previousViewProjectionMatrix);
                }

                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                // TODO: add option to only draw either one?
                DrawCameraMotionVectors(context, cmd, camera, cameraMaterial);
                DrawObjectMotionVectors(context, ref renderingData, camera, objectMaterial);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.renderingData = renderingData;
            m_PassData.cameraMaterial = m_CameraMaterial;
            m_PassData.objectMaterial = m_ObjectMaterial;
            m_PassData.motionData = m_MotionData;

            ExecutePass(context, m_PassData);
        }

        private static DrawingSettings GetDrawingSettings(ref RenderingData renderingData, Material objectMaterial)
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
            drawingSettings.fallbackMaterial = objectMaterial;

            return drawingSettings;
        }

        private static void DrawCameraMotionVectors(ScriptableRenderContext context, CommandBuffer cmd, Camera camera, Material cameraMaterial)
        {
            // Draw fullscreen quad
            cmd.DrawProcedural(Matrix4x4.identity, cameraMaterial, 0, MeshTopology.Triangles, 3, 1);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private static void DrawObjectMotionVectors(ScriptableRenderContext context, ref RenderingData renderingData, Camera camera, Material objectMaterial)
        {
            var drawingSettings = GetDrawingSettings(ref renderingData, objectMaterial);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, camera.cullingMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            // Draw Renderers
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
        }
        #endregion

        class PassData
        {
            public TextureHandle motionVectorColor;
            public TextureHandle motionVectorDepth;
            public RenderingData renderingData;
            public Material cameraMaterial;
            public Material objectMaterial;
            public PreviousFrameData motionData;
        }

        public void Render(in TextureHandle motionVectorColor, in TextureHandle motionVectorDepth, PreviousFrameData motionData, ref RenderingData renderingData)
        {
            RenderGraph graph = renderingData.renderGraph;

            using (var builder = graph.AddRenderPass<PassData>("Motion Vector Pass", out var passData, base.profilingSampler))
            {
                //  TODO RENDERGRAPH: culling? force culluing off for testing
                builder.AllowPassCulling(false);
                passData.motionVectorColor = builder.UseColorBuffer(motionVectorColor, 0);
                passData.motionVectorDepth = builder.UseDepthBuffer(motionVectorDepth, DepthAccess.Write);
                passData.renderingData = renderingData;
                passData.cameraMaterial = m_CameraMaterial;
                passData.objectMaterial = m_ObjectMaterial;
                passData.motionData = motionData;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data);
                    data.renderingData.commandBuffer.SetGlobalTexture("_MotionVectorTexture", data.motionVectorColor);
                });

                return;
            }
        }
    }
}
