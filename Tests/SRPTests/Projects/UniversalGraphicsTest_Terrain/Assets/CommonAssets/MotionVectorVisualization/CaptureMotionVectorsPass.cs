using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class CaptureMotionVectorsPass : ScriptableRenderPass
    {
        static ProfilingSampler s_ProfilingSampler = new ProfilingSampler("MotionVecTest");
        Material m_Material;
        float m_intensity;

        public CaptureMotionVectorsPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing + 1;
        }

        public void SetIntensity(float intensity)
        {
            m_intensity = intensity;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //Todo: test code is not working for XR
            CommandBuffer cmd = CommandBufferPool.Get();

            ExecutePass(renderingData.cameraData.renderer.cameraColorTargetHandle, cmd, renderingData.cameraData, m_Material, m_intensity);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }


        static void ExecutePass(RTHandle targetHandle, CommandBuffer cmd, CameraData cameraData, Material material, float motionIntensity)
        {
            var camera = cameraData.camera;
            if (camera.cameraType != CameraType.Game)
                return;

            if (material == null)
                return;

            using (new ProfilingScope(cmd, s_ProfilingSampler))
            {
                material.SetFloat("_Intensity", motionIntensity);
                Blitter.BlitCameraTexture(cmd, targetHandle, targetHandle, material, 0);
            }
        }
        internal class PassData
        {
            internal TextureHandle target;
            internal CameraData cameraData;
            internal Material material;
            internal float intensity;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Capture Motion Vector Pass", out var passData, s_ProfilingSampler))
            {
                UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;

                TextureHandle color = renderer.activeColorTexture;
                passData.target = builder.UseColorBuffer(color, 0);
                passData.cameraData = renderingData.cameraData;
                passData.material = m_Material;
                passData.intensity = m_intensity;

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    ExecutePass(data.target, rgContext.cmd, data.cameraData, data.material, data.intensity);
                });
            }
        }
    }
}
