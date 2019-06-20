using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public interface IBeforeRender
    {
        ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorHandle, RenderTargetHandle depthHandle, ClearFlag clearFlag);
    }
}
