using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    internal class CaptureMotionVectorsPass : ScriptableRenderPass
    {
        static ProfilingSampler s_ProfilingSampler = new ProfilingSampler("MotionVecTest");
        Material m_Material;
        float m_intensity;

        static readonly int s_MotionVectorTexture = Shader.PropertyToID("_MotionVectorTexture");
        static readonly int s_MotionVectorDepthTexture = Shader.PropertyToID("_MotionVectorDepthTexture");

        public CaptureMotionVectorsPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing + 1;
        }

        public void SetIntensity(float intensity)
        {
            m_intensity = intensity;
        }

#if URP_COMPATIBILITY_MODE
        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer rawcmd = CommandBufferPool.Get();
            var cmd = CommandBufferHelpers.GetRasterCommandBuffer(rawcmd);

            ExecutePass(renderingData.cameraData.renderer.cameraColorTargetHandle, cmd, renderingData.cameraData.camera, m_Material, m_intensity);

            context.ExecuteCommandBuffer(rawcmd);
            rawcmd.Clear();

            CommandBufferPool.Release(rawcmd);
        }
#endif

        static void ExecutePass(RTHandle targetHandle, RasterCommandBuffer cmd, Camera camera, Material material, float motionIntensity)
        {
            if (camera.cameraType != CameraType.Game)
                return;

            if (material == null)
                return;

            using (new ProfilingScope(cmd, s_ProfilingSampler))
            {
                material.SetFloat("_Intensity", motionIntensity);
                Blitter.BlitTexture(cmd, targetHandle, Vector2.one, material, 0);
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
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Capture Motion Vector Pass", out var passData, s_ProfilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.motionVectorColor.IsValid())
                    builder.UseGlobalTexture(s_MotionVectorTexture);
                if (resourceData.motionVectorDepth.IsValid())
                    builder.UseGlobalTexture(s_MotionVectorDepthTexture);

                TextureHandle color = resourceData.activeColorTexture;
                passData.target = color;
                builder.SetRenderAttachment(color, 0, AccessFlags.Write);
                passData.camera = cameraData.camera;
                passData.material = m_Material;
                passData.intensity = m_intensity;

                builder.SetRenderFunc((PassData data,  RasterGraphContext rgContext) =>
                {
                    ExecutePass(data.target, rgContext.cmd, data.camera, data.material, data.intensity);
                });
            }
        }
    }
}
