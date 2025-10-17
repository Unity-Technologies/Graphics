using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Experimental.Rendering;

public class CaptureDepthFeature : ScriptableRendererFeature
{
    private class CaptureDepthPass : ScriptableRenderPass
    {
        private static Material m_Material;

        internal RTHandle m_CapturedDepthRT;
        internal TextureHandle m_CapturedDepthHandle;

        public CaptureDepthPass(RenderPassEvent injectionPoint)
        {
            ScriptableRenderPassInput inputs = ScriptableRenderPassInput.Depth;

            // Request access to normals if the injection point is in a place that could trigger a partial prepass
            // This helps improve coverage of edge cases within URP's deferred renderer
            if (injectionPoint >= RenderPassEvent.AfterRenderingGbuffer && injectionPoint < RenderPassEvent.BeforeRenderingOpaques)
            {
                inputs |= ScriptableRenderPassInput.Normal;
            }

            ConfigureInput(inputs);

            renderPassEvent = injectionPoint;
        }

        public void Setup(Material material)
        {
            m_Material = material;
        }

        class CaptureDepthPassData
        {
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            bool xrReady = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;

            ref var cameraDesc = ref cameraData.cameraTargetDescriptor;

            int width = cameraDesc.width;
            int height = cameraDesc.height;

            m_CapturedDepthHandle = renderGraph.CreateTexture(new TextureDesc(width, height, false, xrReady) { format = GraphicsFormat.R32_SFloat, name = "_CapturedDepthTexture" });

            using (var builder = renderGraph.AddRasterRenderPass<CaptureDepthPassData>("Capture Depth Blit", out var passData))
            {
                passData.material = m_Material;

                builder.UseTexture(resourceData.cameraDepthTexture);

                builder.SetRenderAttachment(m_CapturedDepthHandle, 0);

                builder.SetRenderFunc((CaptureDepthPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, Vector2.one, data.material, 0);
                });
            }
        }
    }

    private class DrawDepthPass : ScriptableRenderPass
    {
        private CaptureDepthPass m_CapturePass;

        public DrawDepthPass(CaptureDepthPass capturePass)
        {
            m_CapturePass = capturePass;

            renderPassEvent = RenderPassEvent.AfterRendering;
        }

        class DrawDepthPassData
        {
            public TextureHandle source;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass<DrawDepthPassData>("Draw Depth Blit", out var passData))
            {
                passData.source = m_CapturePass.m_CapturedDepthHandle;
                builder.UseTexture(passData.source);

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                builder.SetRenderFunc((DrawDepthPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, Vector2.one, 0, false);
                });
            }
        }
    }

    public Material m_Material;

    public enum InjectionPoint
    {
        AfterPrePasses = RenderPassEvent.AfterRenderingPrePasses,
        AfterGbuffer = RenderPassEvent.AfterRenderingGbuffer,
        AfterSkybox = RenderPassEvent.AfterRenderingSkybox,
        AfterTransparents = RenderPassEvent.AfterRenderingTransparents,
    }

    public InjectionPoint m_InjectionPoint = InjectionPoint.AfterTransparents;

    private CaptureDepthPass m_CaptureDepthPass;
    private DrawDepthPass m_DrawDepthPass;

    public override void Create()
    {
        m_CaptureDepthPass = new CaptureDepthPass((RenderPassEvent)m_InjectionPoint);
        m_DrawDepthPass = new DrawDepthPass(m_CaptureDepthPass);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Assert.IsNotNull(m_Material);

        m_CaptureDepthPass.Setup(m_Material);
        renderer.EnqueuePass(m_CaptureDepthPass);
        renderer.EnqueuePass(m_DrawDepthPass);
    }
}
