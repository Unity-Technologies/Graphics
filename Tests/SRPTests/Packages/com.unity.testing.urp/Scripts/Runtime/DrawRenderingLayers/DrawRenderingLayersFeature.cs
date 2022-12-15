using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawRenderingLayersFeature : ScriptableRendererFeature
{
    private class DrawRenderingLayersPass : ScriptableRenderPass
    {
        private ProfilingSampler m_ProfilingSampler;
        private RTHandle m_TestRenderingLayersTextureHandle;

        public DrawRenderingLayersPass()
        {
            m_ProfilingSampler = new ProfilingSampler("Draw Rendering Layers");
            this.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }

        public void Setup(RTHandle renderingLayerTestTextureHandle)
        {
            m_TestRenderingLayersTextureHandle = renderingLayerTestTextureHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecutePass(renderingData.commandBuffer, renderingData.cameraData.renderer.cameraColorTargetHandle);
        }

        public void ExecutePass(CommandBuffer cmd, RTHandle color)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, m_TestRenderingLayersTextureHandle, color);
            }
        }

        private class PassData
        {
            internal DrawRenderingLayersPass pass;
            internal TextureHandle color;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Rendering Layers", out var passData, m_ProfilingSampler))
            {
                UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;

                passData.color = renderer.activeColorTexture;
                builder.UseColorBuffer(passData.color, 0);
                builder.ReadTexture(renderingLayerTexture);

                builder.AllowPassCulling(false);

                passData.pass = this;

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    data.pass.ExecutePass(rgContext.cmd, data.color);
                });
            }
        }
    }

    private class DrawRenderingLayersPrePass : ScriptableRenderPass
    {
        private static class ShaderPropertyId
        {
            public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");
        }

        private Material m_Material;
        private ProfilingSampler m_ProfilingSampler;
        private RTHandle m_ColoredRenderingLayersTextureHandle;
        private Vector4[] m_RenderingLayerColors = new Vector4[32];

        public DrawRenderingLayersPrePass(RenderPassEvent renderPassEvent)
        {
            m_ProfilingSampler = new ProfilingSampler("Rendering Layers PrePass");
            this.renderPassEvent = renderPassEvent;
        }

        public void Setup(RTHandle renderingLayerTestTextureHandle, Material material)
        {
            m_ColoredRenderingLayersTextureHandle = renderingLayerTestTextureHandle;

            m_Material = material;

            for (int i = 0; i < 32; i++)
                m_RenderingLayerColors[i] = Color.HSVToRGB(i / 32f, 1, 1);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_ColoredRenderingLayersTextureHandle);
            ConfigureClear(ClearFlag.ColorStencil, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Render(cmd, renderingData.cameraData);
            }
        }

        private void Render(CommandBuffer cmd, in CameraData cameraData)
        {
            cmd.SetGlobalVectorArray("_RenderingLayersColors", m_RenderingLayerColors);
            cmd.SetGlobalVector(ShaderPropertyId.scaleBias, new Vector4(1, 1, 0, 0));
            Blitter.BlitCameraTexture(cmd, m_ColoredRenderingLayersTextureHandle, m_ColoredRenderingLayersTextureHandle, m_Material, 0);
        }

        private class PassData
        {
            internal DrawRenderingLayersPrePass pass;
            internal RenderingData renderingData;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Rendering PrePass", out var passData, m_ProfilingSampler))
            {
                renderingLayerTexture = renderGraph.ImportTexture(m_ColoredRenderingLayersTextureHandle);
                builder.UseColorBuffer(renderingLayerTexture, 0);

                builder.AllowPassCulling(false);

                passData.pass = this;
                passData.renderingData = renderingData;

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    data.pass.Execute(rgContext.renderContext, ref data.renderingData);
                });
            }
        }
    }

    private const string k_ShaderName = "Hidden/Universal Render Pipeline/DrawRenderingLayers";

    [SerializeField]
    private Material m_Material;

    [SerializeField]
    private RenderPassEvent m_Event = RenderPassEvent.AfterRenderingPrePasses;

    [SerializeField]
    internal RenderingLayerUtils.MaskSize m_MaskSize = RenderingLayerUtils.MaskSize.Bits8;

    private DrawRenderingLayersPrePass m_DrawRenderingLayerPass;
    private DrawRenderingLayersPass m_RequestRenderingLayerPass;

    private RTHandle m_ColoredRenderingLayersTextureHandle;

    internal override bool RequireRenderingLayers(bool isDeferred, out RenderingLayerUtils.Event atEvent, out RenderingLayerUtils.MaskSize maskSize)
    {
        if (m_Event < RenderPassEvent.AfterRenderingGbuffer)
            atEvent = RenderingLayerUtils.Event.DepthNormalPrePass;
        else
            atEvent = RenderingLayerUtils.Event.Opaque;
        maskSize = m_MaskSize;
        return true;
    }

    /// <inheritdoc/>
    public override void Create()
    {
        m_DrawRenderingLayerPass = new DrawRenderingLayersPrePass(m_Event);
        m_RequestRenderingLayerPass = new DrawRenderingLayersPass();
    }

    protected override void Dispose(bool disposing)
    {
        m_ColoredRenderingLayersTextureHandle?.Release();
    }

    internal static TextureHandle renderingLayerTexture;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.msaaSamples = 1;
        desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB;
        desc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_ColoredRenderingLayersTextureHandle, desc, name: "_ColoredRenderingLayersTexture");

        m_DrawRenderingLayerPass.Setup(m_ColoredRenderingLayersTextureHandle, m_Material);
        renderer.EnqueuePass(m_DrawRenderingLayerPass);
        m_RequestRenderingLayerPass.Setup(m_ColoredRenderingLayersTextureHandle);
        renderer.EnqueuePass(m_RequestRenderingLayerPass);
    }
}
