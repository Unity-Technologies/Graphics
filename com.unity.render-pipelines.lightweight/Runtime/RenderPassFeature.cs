using System;

namespace UnityEngine.Rendering.LWRP
{
    public abstract class RenderPassFeature : ScriptableObject
    {
        [Flags]
        public enum InjectionPoint
        {
            None = 0,
            BeforeRenderPasses = 1 << 0,
            AfterOpaqueRenderPasses = 1 << 1,
            AfterOpaquePostProcessPasses = 1 << 2,
            AfterSkyboxPasses = 1 << 3,
            AfterTransparentPasses = 1 << 4,
            AfterRenderPasses = 1 << 5,
        }

        public abstract InjectionPoint injectionPoints { get; }
        
        public abstract ScriptableRenderPass GetPassToEnqueue(
            InjectionPoint injection,
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle);
    }
}
