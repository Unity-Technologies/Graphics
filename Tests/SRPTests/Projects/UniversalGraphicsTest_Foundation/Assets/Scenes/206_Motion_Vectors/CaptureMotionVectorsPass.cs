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

            ExecutePass(renderingData.cameraData.renderer.cameraColorTargetHandle, cmd, renderingData.cameraData.camera, m_Material, m_intensity);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }


        static void ExecutePass(RTHandle targetHandle, CommandBuffer cmd, Camera camera, Material material, float motionIntensity)
        {
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
            internal Camera camera;
            internal Material material;
            internal float intensity;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // TODO: Make this use a raster pass it likely doesn't need LowLevel. On the other hand probably ok as-is for the tests.
            using (var builder = renderGraph.AddLowLevelPass<PassData>("Capture Motion Vector Pass", out var passData, s_ProfilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                TextureHandle color = resourceData.activeColorTexture;
                passData.target = builder.UseTexture(color, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                passData.camera = cameraData.camera;
                passData.material = m_Material;
                passData.intensity = m_intensity;

                builder.SetRenderFunc((PassData data,  LowLevelGraphContext rgContext) =>
                {
                    ExecutePass(data.target, rgContext.legacyCmd, data.camera, data.material, data.intensity);
                });
            }
        }
    }
}
