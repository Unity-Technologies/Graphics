namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IAfterRender
    {
        ScriptableRenderPass GetPassToEnqueue();
    }
}
