namespace UnityEngine.Experimental.Rendering.LWRP
{
    public interface IAfterDepthPrePass
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle);
    }
}
