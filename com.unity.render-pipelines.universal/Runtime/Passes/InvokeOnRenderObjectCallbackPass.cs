using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
            context.InvokeOnRenderObjectCallback();
        }

        class PassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
        }

        public void Render(TextureHandle colorTarget, TextureHandle depthTarget, ref RenderingData renderingData)
        {
            var graph = renderingData.renderGraph;

            using (var builder = graph.AddRenderPass<PassData>("OnRenderObject Callback Pass", out var passData,
                base.profilingSampler))
            {
                passData.colorTarget = builder.UseColorBuffer(colorTarget, 0);
                passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.renderContext.InvokeOnRenderObjectCallback();
                });
            }
        }
    }
}
