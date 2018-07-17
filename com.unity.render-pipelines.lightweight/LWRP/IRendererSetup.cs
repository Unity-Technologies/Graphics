namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IRendererSetup
    {

        void Setup(LightweightForwardRenderer renderer, ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData);

    }
}