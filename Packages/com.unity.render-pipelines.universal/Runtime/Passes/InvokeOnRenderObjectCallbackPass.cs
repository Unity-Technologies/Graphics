using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Invokes OnRenderObject callback
    /// </summary>

    internal class InvokeOnRenderObjectCallbackPass : ScriptableRenderPass
    {
        public InvokeOnRenderObjectCallbackPass(RenderPassEvent evt)
        {
            profilingSampler = new ProfilingSampler("Invoke OnRenderObject Callback");
            renderPassEvent = evt;
        }

        private class PassData
        {
            internal TextureHandle colorTarget;
            internal TextureHandle depthTarget;
        }

        internal void Render(RenderGraph renderGraph, TextureHandle colorTarget, TextureHandle depthTarget)
        {
            using (var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.colorTarget = colorTarget;
                builder.UseTexture(colorTarget, AccessFlags.Write);
                passData.depthTarget = depthTarget;
                builder.UseTexture(depthTarget, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.colorTarget, data.depthTarget);
                    context.cmd.InvokeOnRenderObjectCallbacks();
                });
            }
        }
    }
}
