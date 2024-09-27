namespace UnityEngine.Rendering
{
    /// <summary>
    /// By implementing this interface, a render pipeline can indicate its usage of the STP upscaler
    /// </summary>
    public interface ISTPEnabledRenderPipeline
    {
        /// <summary>
        /// Indicates if this render pipeline instance uses STP.
        /// </summary>
        bool isStpUsed { get; }
    }
}
