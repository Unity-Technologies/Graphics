using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class CaptureMotionVectorsPass : ScriptableRenderPass
    {
        static ProfilingSampler s_ProfilingSampler = new ProfilingSampler("MotionVecTest");
        static Material s_Material;
        static float s_intensity;

        public CaptureMotionVectorsPass(Material material)
        {
            s_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing + 1;
        }

        public void SetIntensity(float intensity)
        {
            s_intensity = intensity;
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

            if (s_Material == null)
                return;


            using (new ProfilingScope(cmd, s_ProfilingSampler))
            {
                s_Material.SetFloat("_Intensity", s_intensity);
                Blitter.BlitCameraTexture(cmd, targetHandle, targetHandle, s_Material, 0);
            }
        }
        internal class PassData
        {
            internal TextureHandle target;
            internal CameraData cameraData;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Capture Motion Vector Pass", out var passData, s_ProfilingSampler))
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
