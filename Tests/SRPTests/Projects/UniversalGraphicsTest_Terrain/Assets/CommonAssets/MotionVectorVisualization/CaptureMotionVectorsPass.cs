using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
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

        static void ExecutePass(RTHandle targetHandle, CommandBuffer cmd, bool isGameCamera, Material material, float motionIntensity)
        {
            if (!isGameCamera)
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
            internal bool isGameCamera;
            internal Material material;
            internal float intensity;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddRenderPass<PassData>("Capture Motion Vector Pass", out var passData, s_ProfilingSampler))
            {
                TextureHandle color = resourceData.activeColorTexture;
                passData.target = builder.UseColorBuffer(color, 0);
                passData.isGameCamera = cameraData.camera.cameraType == CameraType.Game;
                passData.material = m_Material;
                passData.intensity = m_intensity;

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    ExecutePass(data.target, rgContext.cmd, data.isGameCamera, data.material, data.intensity);
                });
            }
        }
    }
}
