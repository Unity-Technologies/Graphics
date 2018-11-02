namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IAfterRender
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle);
    }
}
