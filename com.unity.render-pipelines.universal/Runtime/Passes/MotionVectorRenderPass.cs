using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace kTools.Motion
{
    sealed class MotionVectorRenderPass : ScriptableRenderPass
    {
#region Fields
        const string kCameraShader = "Hidden/kMotion/CameraMotionVectors";
        const string kObjectShader = "Hidden/kMotion/ObjectMotionVectors";
        const string kPreviousViewProjectionMatrix = "_PrevViewProjMatrix";
        const string kMotionVectorTexture = "_MotionVectorTexture";
      //  const string kProfilingTag = "Motion Vectors";

        static readonly string[] s_ShaderTags = new string[]
        {
            "MotionVectors",
            "MotionVectorsOnlyTransformMoved"
        };

        RenderTargetHandle m_MotionVectorHandle;
        Material m_CameraMaterial;
        Material m_ObjectMaterial;
        MotionData m_MotionData;
#endregion

#region Constructors
        internal MotionVectorRenderPass()
        {
            // Set data
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
#endregion

#region State
        internal void Setup(MotionData motionData)
        {
            // Set data
            m_MotionData = motionData;
            m_CameraMaterial = new Material(Shader.Find(kCameraShader));
            m_ObjectMaterial = new Material(Shader.Find(kObjectShader));
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var rtd = cameraTextureDescriptor;
            rtd.graphicsFormat = GraphicsFormat.R16G16_SFloat;
            // Configure Render Target
            m_MotionVectorHandle.Init(kMotionVectorTexture);
            cmd.GetTemporaryRT(m_MotionVectorHandle.id, rtd, FilterMode.Point);
            ConfigureTarget(m_MotionVectorHandle.Identifier(), m_MotionVectorHandle.Identifier());
            cmd.SetRenderTarget(m_MotionVectorHandle.Identifier(), m_MotionVectorHandle.Identifier());

            // TODO: Why do I have to clear here?
            cmd.ClearRenderTarget(true, true, Color.black, 1.0f);
        }
#endregion

#region Execution
    //private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(kProfilingTag);

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get data
            var camera = renderingData.cameraData.camera;

            // Never draw in Preview
            if(camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            CommandBuffer cmd = CommandBufferPool.Get();
           // using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                ExecuteCommand(context, cmd);

                // Shader uniforms
                Shader.SetGlobalMatrix(kPreviousViewProjectionMatrix, m_MotionData.previousGPUViewProjectionMatrix);

                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                // Drawing
                DrawCameraMotionVectors(context, cmd, camera);
                DrawObjectMotionVectors(context, ref renderingData, cmd, camera);
            }
            ExecuteCommand(context, cmd);
        }

        DrawingSettings GetDrawingSettings(ref RenderingData renderingData)
        {
            // Drawing Settings
            var camera = renderingData.cameraData.camera;
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(ShaderTagId.none, sortingSettings)
            {
                perObjectData = PerObjectData.MotionVectors,
                enableDynamicBatching = renderingData.supportsDynamicBatching,
                enableInstancing = true,
            };

            // Shader Tags
            for (int i = 0; i < s_ShaderTags.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, new ShaderTagId(s_ShaderTags[i]));
            }

            // Material
            drawingSettings.fallbackMaterial = m_ObjectMaterial;
            return drawingSettings;
        }

        void DrawCameraMotionVectors(ScriptableRenderContext context, CommandBuffer cmd, Camera camera)
        {
            // Draw fullscreen quad
            cmd.DrawProcedural(Matrix4x4.identity, m_CameraMaterial, 0, MeshTopology.Triangles, 3, 1);
            ExecuteCommand(context, cmd);
        }

        void DrawObjectMotionVectors(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer cmd, Camera camera)
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
