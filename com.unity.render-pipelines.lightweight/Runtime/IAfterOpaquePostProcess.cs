namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IAfterOpaquePostProcess
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle,
            RenderTargetHandle depthHandle);
    }
}
