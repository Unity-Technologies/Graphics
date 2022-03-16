using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawRenderer2DPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");

        private static readonly List<ShaderTagId> k_ShaderTags =
            new List<ShaderTagId>() {k_LegacyPassName, k_CombinedRenderingPassName};

        private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");
        private static readonly int k_UseSceneLightingID = Shader.PropertyToID("_UseSceneLighting");
        private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");

        public Renderer2DData rendererData { get; }
        private LayerBatch m_LayerBatch;
        private UniversalRenderer2D.GBuffers m_GBuffers;
        private bool m_NeedsClear;

        public DrawRenderer2DPass(Renderer2DData rendererData, LayerBatch layerBatch, UniversalRenderer2D.GBuffers buffers, bool needsClear)
        {
            this.rendererData = rendererData;
            useNativeRenderPass = true;
            m_GBuffers = buffers;
            m_LayerBatch = layerBatch;
            m_NeedsClear = needsClear;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            var filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = new SortingLayerRange(m_LayerBatch.layerRange.lowerBound,
                m_LayerBatch.layerRange.upperBound);

            var drawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
            var blendStylesCount = rendererData.lightBlendStyles.Length;

            cmd.SetGlobalFloat(k_HDREmulationScaleID, rendererData.hdrEmulationScale);
            cmd.SetGlobalFloat(k_UseSceneLightingID, true ? 1.0f : 0.0f);
            cmd.SetGlobalColor(k_RendererColorID, Color.white);
            this.SetShapeLightShaderGlobals(cmd);

            if (m_LayerBatch.lightStats.totalLights > 0)
            {
                for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                {
                    var blendStyleMask = (uint)(1 << blendStyleIndex);
                    var blendStyleUsed = (m_LayerBatch.lightStats.blendStylesUsed & blendStyleMask) > 0;
                    RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);
                }
            }
            else
            {
                for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                {
                    cmd.SetGlobalTexture(Render2DLightingPass.k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);
                    RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, false);
                }
            }

            cmd.SetGlobalColor("_GlobalLight0", m_LayerBatch.clearColors[0]);
            cmd.SetGlobalColor("_GlobalLight1", m_LayerBatch.clearColors[1]);
            cmd.SetGlobalColor("_GlobalLight2", m_LayerBatch.clearColors[2]);
            cmd.SetGlobalColor("_GlobalLight3", m_LayerBatch.clearColors[3]);

            if(m_NeedsClear)
                cmd.ClearRenderTarget(true, true, Color.black);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);

            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // THIS LINE IS CRUCIAL, WITHOUT IT, MEMORYLESS TEXTURES WON'T GET MADE PROPERLY
            ConfigureInputAttachments(m_GBuffers.lightBuffers, m_GBuffers.transients);

            ConfigureTarget(m_GBuffers.colorAttachment, m_GBuffers.depthAttachment);
            ConfigureClear(ClearFlag.None, Color.black);
        }
    }
}
