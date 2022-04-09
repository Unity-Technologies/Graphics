using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    internal class TransparencyPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        private static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_CombinedRenderingPassName };

        private static readonly int[] k_ShapeLightTextureIDs =
        {
            Shader.PropertyToID("_ShapeLightTexture0"),
            Shader.PropertyToID("_ShapeLightTexture1"),
            Shader.PropertyToID("_ShapeLightTexture2"),
            Shader.PropertyToID("_ShapeLightTexture3")
        };

        private readonly Renderer2DData m_Renderer2DData;
        Renderer2DData IRenderPass2D.rendererData
        {
            get { return m_Renderer2DData;}
        }

        int m_BatchCount;

        public TransparencyPass(Renderer2DData rendererData)
        {
            profilingSampler = new ProfilingSampler(nameof(TransparencyPass));
            m_Renderer2DData = rendererData;
        }

        public void Setup(int batchCount)
        {
            m_BatchCount = batchCount;
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            for (int i = 0; i < m_BatchCount; ++i)
            {
                ref var layerBatch = ref m_Renderer2DData.layerBatches[i];
                layerBatch.ReleaseRT(cmd);
            }

            this.DisableAllKeywords(cmd);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                var isSceneLit = m_Renderer2DData.lightCullResult.IsSceneLit();

                for (int i = 0; i < m_BatchCount; ++i)
                {
                    ref var layerBatch = ref m_Renderer2DData.layerBatches[i];

                    var blendStylesCount = m_Renderer2DData.lightBlendStyles.Length;
                    var desc = this.GetBlendStyleRenderTextureDesc(renderingData);

                    if (layerBatch.lightStats.totalLights > 0)
                    {
                        for (var blendStyleIndex = 0; blendStyleIndex < blendStylesCount; blendStyleIndex++)
                        {
                            var blendStyleMask = (uint)(1 << blendStyleIndex);
                            var blendStyleUsed = (layerBatch.lightStats.blendStylesUsed & blendStyleMask) > 0;

                            if (blendStyleUsed)
                            {
                                var identifier = layerBatch.GetRTId(cmd, desc, blendStyleIndex);
                                cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], identifier);
                            }

                            RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleUsed);
                        }
                    }
                    else
                    {
                        for (var blendStyleIndex = 0; blendStyleIndex < k_ShapeLightTextureIDs.Length; blendStyleIndex++)
                        {
                            cmd.SetGlobalTexture(k_ShapeLightTextureIDs[blendStyleIndex], Texture2D.blackTexture);
                            RendererLighting.EnableBlendStyle(cmd, blendStyleIndex, blendStyleIndex == 0);
                        }
                    }

                    if (!isSceneLit)
                        this.DisableAllKeywords(cmd);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var filterSettings = new FilteringSettings();
                    filterSettings.renderQueueRange = RenderQueueRange.all;
                    filterSettings.layerMask = -1;
                    filterSettings.renderingLayerMask = 0xFFFFFFFF;
                    filterSettings.sortingLayerRange = isSceneLit ? new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound) : SortingLayerRange.all;

                    var camera = renderingData.cameraData.camera;
                    var drawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
                    LayerUtility.SetTransparencySortingMode(camera, m_Renderer2DData, ref drawSettings);

                    this.Render(context, cmd, ref renderingData, ref filterSettings, drawSettings);

                    RenderBufferStoreAction storeAction = RenderBufferStoreAction.Store;
                    this.RenderLightVolumes(renderingData, cmd, ref layerBatch, colorAttachmentHandle.nameID, depthAttachmentHandle.nameID,
                              RenderBufferStoreAction.Store, storeAction, false, m_Renderer2DData.lightCullResult.visibleLights);

                    if (!isSceneLit)
                        break;
                }
            }
        }
    }
}
