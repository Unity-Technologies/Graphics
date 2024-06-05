using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawRenderingLayersFeature : ScriptableRendererFeature
{
    private class DrawRenderingLayersPass : ScriptableRenderPass
    {
        private ProfilingSampler m_ProfilingSampler;
        private RTHandle m_TestRenderingLayersTextureHandle;
        private PassData m_PassData;
        public DrawRenderingLayersPass()
        {
            m_ProfilingSampler = new ProfilingSampler("Draw Rendering Layers");
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            m_PassData = new PassData();
        }

        public void Setup(RTHandle renderingLayerTestTextureHandle)
        {
            m_TestRenderingLayersTextureHandle = renderingLayerTestTextureHandle;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.viewportScale = m_TestRenderingLayersTextureHandle.useScaling ? new Vector2(m_TestRenderingLayersTextureHandle.rtHandleProperties.rtHandleScale.x, m_TestRenderingLayersTextureHandle.rtHandleProperties.rtHandleScale.y) : Vector2.one;

            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData);
        }

        private void ExecutePass(RasterCommandBuffer cmd, PassData data)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blitter.BlitTexture(cmd, m_TestRenderingLayersTextureHandle, data.viewportScale, 0, true);
            }
        }

        private class PassData
        {
            internal DrawRenderingLayersPass pass;
            internal TextureHandle color;
            internal Vector2 viewportScale;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Rendering Layers", out var passData, m_ProfilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                passData.color = resourceData.activeColorTexture;
                builder.SetRenderAttachment(passData.color, 0, AccessFlags.Write);
                builder.UseTexture(renderingLayerTexture);
                passData.viewportScale = m_TestRenderingLayersTextureHandle.useScaling ? new Vector2(m_TestRenderingLayersTextureHandle.rtHandleProperties.rtHandleScale.x, m_TestRenderingLayersTextureHandle.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                builder.AllowPassCulling(false);

                passData.pass = this;

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    data.pass.ExecutePass(rgContext.cmd, data);
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
            m_Material = material;
            m_ColoredRenderingLayersTextureHandle = renderingLayerTestTextureHandle;

            for (int i = 0; i < 32; i++)
                m_RenderingLayerColors[i] = Color.HSVToRGB(i / 32f, 1, 1);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_ColoredRenderingLayersTextureHandle);
            ConfigureClear(ClearFlag.ColorStencil, Color.black);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RasterCommandBuffer cmd = CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer);
            ExecutePass(cmd);
        }

        private void ExecutePass(RasterCommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Render(cmd, m_Material);
            }
        }

        private void ExecuteRenderGraphPass(RasterCommandBuffer cmd, Material mat)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Render(cmd, mat);
            }
        }

        private void Render(RasterCommandBuffer cmd, Material material)
        {
            cmd.SetGlobalVectorArray("_RenderingLayersColors", m_RenderingLayerColors);
            cmd.SetGlobalVector(ShaderPropertyId.scaleBias, new Vector4(1, 1, 0, 0));
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1);
        }

        private class PassData
        {
            internal DrawRenderingLayersPrePass pass;
            internal TextureHandle cameraRenderingLayersTexture;
            internal Material mat;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Rendering PrePass", out var passData, m_ProfilingSampler))
            {
                renderingLayerTexture = renderGraph.ImportTexture(m_ColoredRenderingLayersTextureHandle);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                passData.mat = m_Material;

                builder.SetRenderAttachment(renderingLayerTexture, 0, AccessFlags.Write);
                if (renderer.renderingModeActual == RenderingMode.Deferred)
                {
                    builder.UseTexture(resourceData.gBuffer[renderer.deferredLights.GBufferRenderingLayers]);
                    passData.cameraRenderingLayersTexture = resourceData.gBuffer[renderer.deferredLights.GBufferRenderingLayers];
                }
                else
                {
                    builder.UseTexture(resourceData.renderingLayersTexture);
                    passData.cameraRenderingLayersTexture = resourceData.renderingLayersTexture;
                }

                passData.pass = this;

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    if (data.cameraRenderingLayersTexture.IsValid())
                    {
                        data.mat.SetTexture(s_CameraRenderingLayersTextureID,
                            data.cameraRenderingLayersTexture);
                    }

                    data.pass.ExecuteRenderGraphPass(rgContext.cmd, data.mat);
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

    internal override bool RequireRenderingLayers(bool isDeferred, bool needsGBufferAccurateNormals, out RenderingLayerUtils.Event atEvent, out RenderingLayerUtils.MaskSize maskSize)
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
    private static readonly int s_CameraRenderingLayersTextureID = Shader.PropertyToID("_CameraRenderingLayersTexture");

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.msaaSamples = 1;
        desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB;
        desc.depthBufferBits = 0;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_ColoredRenderingLayersTextureHandle, desc, name: "_ColoredRenderingLayersTexture");

        m_DrawRenderingLayerPass.Setup(m_ColoredRenderingLayersTextureHandle, m_Material);
        renderer.EnqueuePass(m_DrawRenderingLayerPass);
        m_RequestRenderingLayerPass.Setup(m_ColoredRenderingLayersTextureHandle);
        renderer.EnqueuePass(m_RequestRenderingLayerPass);
    }
}
