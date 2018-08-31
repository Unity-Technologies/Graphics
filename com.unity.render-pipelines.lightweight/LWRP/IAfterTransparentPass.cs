namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IAfterTransparentPass
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle);
    }
}
