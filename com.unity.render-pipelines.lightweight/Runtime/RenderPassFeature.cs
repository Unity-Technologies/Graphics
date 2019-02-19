using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    public abstract class RenderPassFeature : ScriptableObject
    {
        public abstract void AddRenderPasses(List<ScriptableRenderPass> renderPasses,
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle);
    }
}
