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

            using (var builder = renderGraph.AddUnsafePass<PassData>("Capture Motion Vector Pass", out var passData, s_ProfilingSampler))
            {
                passData.target = resourceData.activeColorTexture;
                builder.SetRenderAttachment(passData.target, 0);
                passData.isGameCamera = cameraData.camera.cameraType == CameraType.Game;
                passData.material = m_Material;
                passData.intensity = m_intensity;

                builder.SetRenderFunc((PassData data, UnsafeGraphContext rgContext) =>
                {
                    var nativeCmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
                    ExecutePass(data.target, nativeCmd, data.isGameCamera, data.material, data.intensity);
                });
            }
        }
    }
}
