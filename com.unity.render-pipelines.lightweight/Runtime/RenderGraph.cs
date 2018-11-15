namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class RenderGraph
    {
        public abstract void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);
    }
}
