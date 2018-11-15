namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class LightweightRendererSetup : ScriptableObject, IRendererSetup
    {
        public abstract void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);
    }
}