using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class CaptureMotionVectorsPass : ScriptableRenderPass
    {
        static ProfilingSampler m_ProfilingSampler = new ProfilingSampler("MotionVecDebug");
        static Material m_Material;
        static float m_intensity;

        public CaptureMotionVectorsPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public void SetIntensity(float intensity)
        {
            m_intensity = intensity;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //Todo: test code is not working for XR
            CommandBuffer cmd = CommandBufferPool.Get();

            ExecutePass(renderingData.cameraData.renderer.cameraColorTargetHandle, cmd, renderingData.cameraData);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        static void ExecutePass(RTHandle targetHandle, CommandBuffer cmd, CameraData cameraData)
        {
            var camera = cameraData.camera;
            if (camera.cameraType != CameraType.Game)
                return;

            if (m_Material == null)
                return;


            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                m_Material.SetFloat("_Intensity", m_intensity);
                Blitter.BlitCameraTexture(cmd, targetHandle, targetHandle, m_Material, 0);
            }
        }
        internal class PassData
        {
            internal TextureHandle target;
            internal CameraData cameraData;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Capture Motion Vector Pass", out var passData, m_ProfilingSampler))
            {
                TextureHandle color = UniversalRenderer.m_ActiveRenderGraphColor;
                passData.target = builder.UseColorBuffer(color, 0);
                passData.cameraData = renderingData.cameraData;

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    ExecutePass(data.target, rgContext.cmd, data.cameraData);
                });
            }
        }
    }
}
