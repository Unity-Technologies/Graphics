namespace UnityEngine.Experimental.Rendering.LWRP
{
    public interface IAfterRender
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle);
    }
}
