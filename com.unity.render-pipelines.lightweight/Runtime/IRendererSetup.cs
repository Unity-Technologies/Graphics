namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class IRendererSetup
    {
        public abstract void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);
    }
}
