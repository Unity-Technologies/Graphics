namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Invokes OnRenderObject callback
    /// </summary>

    public class InvokeOnRenderObjectCallbackPass : ScriptableRenderPass
    {
        public InvokeOnRenderObjectCallbackPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            context.InvokeOnRenderObjectCallback();
        }
    }
}
