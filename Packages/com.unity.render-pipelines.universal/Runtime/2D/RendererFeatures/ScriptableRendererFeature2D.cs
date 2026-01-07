namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// You can add a <c>ScriptableRendererFeature2D</c> to the <c>ScriptableRenderer</c>. Use this scriptable renderer feature to inject render passes into the 2D renderer.
    /// </summary>
    /// <seealso cref="ScriptableRenderer"/>
    /// <seealso cref="ScriptableRenderPass2D"/>
    [ExcludeFromPreset]
    [SupportedOnRenderer(typeof(Renderer2DData))]
    public abstract partial class ScriptableRendererFeature2D : ScriptableRendererFeature
    {
        /// <summary>
        /// Specifies at which injection point the pass will be rendered.
        /// </summary>
        [HideInInspector]
        public RenderPassEvent2D injectionPoint2D = RenderPassEvent2D.BeforeRendering;

        /// <summary>
        /// Specifies the sorting layer in which Unity injects the pass.
        /// </summary>
        [HideInInspector]
        public int sortingLayerID = 0;
    }
}
