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
        private LayerBatch layerBatch;
        private RTHandle[] gbuffers;
        private bool[] transients = {false, false, false, false};

        public DrawRenderer2DPass(Renderer2DData rendererData, bool isNative)
        {
            this.rendererData = rendererData;
            useNativeRenderPass = isNative;
            gbuffers = new RTHandle[5];
            transients = new bool[gbuffers.Length];
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            var filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound,
                layerBatch.layerRange.upperBound);

            var drawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
            var blendStylesCount = rendererData.lightBlendStyles.Length;

            cmd.SetGlobalFloat(k_HDREmulationScaleID, rendererData.hdrEmulationScale);
            cmd.SetGlobalFloat(k_UseSceneLightingID, true ? 1.0f : 0.0f);
            cmd.SetGlobalColor(k_RendererColorID, Color.white);
            this.SetShapeLightShaderGlobals(cmd);

            if (layerBatch.lightStats.totalLights > 0)
            {
                for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                {
                    var blendStyleMask = (uint)(1 << blendStyleIndex);
                    var blendStyleUsed = (layerBatch.lightStats.blendStylesUsed & blendStyleMask) > 0;

                    if (blendStyleUsed)
                    {
                        var gBuffer = gbuffers[blendStyleIndex];
                        cmd.SetGlobalTexture(gBuffer.name, gBuffer.nameID);
                    }

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

            cmd.SetGlobalColor("_GlobalLight0", layerBatch.clearColors[0]);
            cmd.SetGlobalColor("_GlobalLight1", layerBatch.clearColors[1]);
            cmd.SetGlobalColor("_GlobalLight2", layerBatch.clearColors[2]);
            cmd.SetGlobalColor("_GlobalLight3", layerBatch.clearColors[3]);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);

            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if(useNativeRenderPass)
                ConfigureInputAttachments(gbuffers, transients);
        }

        public void Setup(LayerBatch layerBatch, RTHandle[] gbuffers)
        {
            this.layerBatch = layerBatch;
            for (var i = 0; i < this.gbuffers.Length; i++)
            {
                transients[i] = true;
                this.gbuffers[i] = gbuffers[i];
            }
        }
    }
}
