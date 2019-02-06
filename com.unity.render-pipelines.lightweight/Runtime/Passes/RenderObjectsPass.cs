namespace UnityEngine.Rendering.LWRP
{
    public class RenderObjectsPass : ScriptableRenderPass
    {
        RenderQueueType renderQueueType;
        Material overrideMaterial;
        int overrideMaterialPassIndex;
        FilteringSettings m_FilteringSettings;

        public RenderObjectsPass(string[] shaderTags, RenderQueueType renderQueueType, Material overrideMaterial,
            int overrideMaterialPassIndex, int layerMask)
        {
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = overrideMaterial;
            this.overrideMaterialPassIndex = overrideMaterialPassIndex;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            foreach (var passName in shaderTags)
                RegisterShaderPassName(passName);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            Camera camera = renderingData.cameraData.camera;
            DrawingSettings drawingSettings = CreateDrawingSettings(camera, sortingCriteria,
                renderingData.perObjectData, renderingData.supportsDynamicBatching);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
        }
    }
}
