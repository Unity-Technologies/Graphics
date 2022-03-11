namespace UnityEngine.Rendering.Universal
{
    internal class NormalPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

        private readonly Renderer2DData m_Renderer2DData;
        private bool m_NeedsDepth;

        Renderer2DData IRenderPass2D.rendererData
        {
            get { return m_Renderer2DData; }
        }

        public NormalPass(Renderer2DData rendererData)
        {
            profilingSampler = new ProfilingSampler(nameof(NormalPass));
            m_Renderer2DData = rendererData;
        }

        public void Setup(bool needsDepth)
        {
            m_NeedsDepth = needsDepth;
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //this.ReleaseNormalMap(cmd);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                ref LayerBatch layerBatch = ref m_Renderer2DData.layerBatches[m_Renderer2DData.currLayerBatch];
            
                if (layerBatch.lightStats.totalNormalMapUsage > 0)
                {
                    var filterSettings = new FilteringSettings();
                    filterSettings.renderQueueRange = RenderQueueRange.all;
                    filterSettings.layerMask = -1;
                    filterSettings.renderingLayerMask = 0xFFFFFFFF;
                    filterSettings.sortingLayerRange = layerBatch.layerRange;

                    var camera = renderingData.cameraData.camera;
                    var normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);
                    LayerUtility.SetTransparencySortingMode(camera, m_Renderer2DData, ref normalsDrawSettings);

                    var depthTarget = m_NeedsDepth ? depthAttachmentHandle.nameID : BuiltinRenderTextureType.None;
                    this.RenderNormals(context, renderingData, normalsDrawSettings, filterSettings, depthTarget, layerBatch.lightStats);
                }
            }
        }
    }
}
