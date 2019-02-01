using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public abstract class RendererSetup
    {
        protected List<RenderPassFeature> m_RenderPassFeatures = new List<RenderPassFeature>(10);

        public abstract void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);

        protected void EnqueuePasses(RenderPassFeature.InjectionPoint injectionCallback, RenderPassFeature.InjectionPoint injectionCallbackMask,
            ScriptableRenderer renderer, RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
        {
            if (CoreUtils.HasFlag(injectionCallbackMask, injectionCallback))
            {
                foreach (var renderPassFeature in m_RenderPassFeatures)
                {
                    var renderPass = renderPassFeature.GetPassToEnqueue(injectionCallback, baseDescriptor, colorHandle, depthHandle);
                    if (renderPass != null)
                        renderer.EnqueuePass(renderPass);
                }
            }
        }
    }
}
