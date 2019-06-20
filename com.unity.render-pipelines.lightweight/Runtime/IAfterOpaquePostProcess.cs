namespace UnityEngine.Experimental.Rendering.LWRP
{
    public interface IAfterOpaquePostProcess
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle,
            RenderTargetHandle depthHandle);
    }
}
