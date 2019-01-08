namespace UnityEngine.Experimental.Rendering.LWRP
{
    public interface IAfterOpaquePass
    {
        ScriptableRenderPass GetPassToEnqueue(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle);
    }
}
