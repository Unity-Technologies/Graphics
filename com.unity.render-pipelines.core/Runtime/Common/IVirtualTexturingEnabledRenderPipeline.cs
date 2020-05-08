namespace UnityEngine.Rendering
{
    /// <summary>
    /// By implementing this interface, a render pipeline can indicate to external code it supports virtual texturing.
    /// </summary>
    public interface IVirtualTexturingEnabledRenderPipeline
    {
        /// <summary>
        /// Indicates if virtual texturing is currently enabled for this render pipeline instance.
        /// </summary>
        bool virtualTexturingEnabled { get; }
    }
}
