namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Interface to add/manage Render Graph related parameters across different types of RenderPipelineAssets.
    /// </summary>
    public interface IRenderGraphEnabledRenderPipeline
    {
        /// <summary>
        /// Indicates if this render pipeline instance supports ImmediateMode when debugging the render graph.
        /// </summary>
        bool isImmediateModeSupported { get; }
    }
}
