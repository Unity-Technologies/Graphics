namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IAfterDepthPrePass
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle);
    }
}
