namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class RenderGraphData : ScriptableObject
    {
        public abstract RenderGraph Create();
    }
}

