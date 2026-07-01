namespace UnityEngine.Rendering.Universal
{
    internal class Universal2DRenderingData : ContextItem
    {
        internal Renderer2DData renderingData;

        internal LayerBatch[] layerBatches;

        internal int batchCount;

        public override void Reset()
        {
            renderingData = null;
            layerBatches = null;
            batchCount = 0;
        }
    }
}
