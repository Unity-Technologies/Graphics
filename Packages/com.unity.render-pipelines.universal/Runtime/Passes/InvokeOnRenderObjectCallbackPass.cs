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
            base.profilingSampler = new ProfilingSampler(nameof(InvokeOnRenderObjectCallbackPass));
            renderPassEvent = evt;
            //TODO: should we fix and re-enable native render pass for this pass?
            // Currently disabled because when the callback is empty it causes an empty Begin/End RenderPass block, which causes artifacts on Vulkan
            useNativeRenderPass = false;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            renderingData.commandBuffer.InvokeOnRenderObjectCallbacks();
        }

        private class PassData
        {
            internal TextureHandle colorTarget;
            internal TextureHandle depthTarget;
        }

        internal void Render(RenderGraph renderGraph, TextureHandle colorTarget, TextureHandle depthTarget)
        {
            using (var builder = renderGraph.AddUnsafePass<PassData>("OnRenderObject Callback Pass", out var passData,
                base.profilingSampler))
            {
                passData.colorTarget = colorTarget;
                builder.UseTexture(colorTarget, AccessFlags.Write);
                passData.depthTarget = depthTarget;
                builder.UseTexture(depthTarget, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.InvokeOnRenderObjectCallbacks();
                });
            }
        }
    }
}
