namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CreateAssetMenu()]
    public class CustomRenderGraphData : RenderGraphData
    {
        public override RenderGraph Create()
        {
            return new CustomLWPipe(this);
        }
    }
}

