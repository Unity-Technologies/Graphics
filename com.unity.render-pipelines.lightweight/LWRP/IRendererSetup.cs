namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IRendererSetup
    {
        void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData);
    }
}