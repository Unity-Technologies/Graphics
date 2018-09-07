namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IRendererSetup
    {
        void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);
    }
}
