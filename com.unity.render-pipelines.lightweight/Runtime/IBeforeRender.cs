namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IBeforeRender
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorHandle, RenderTargetHandle depthHandle, ClearFlag clearFlag);
    }
}
