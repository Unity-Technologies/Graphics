namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Interface for resetting the data in a PassData class in the render graph system.
    /// when returning the instance to the <see cref="RenderGraphObjectPool"/>.
    /// </summary>
    /// <remarks>
    /// Unity reuses PassData instances across frames via the <see cref="RenderGraphObjectPool"/>. If you don't set a property every frame,
    /// the property might keep the value from a previous frame. To prevent this, implement this interface and clear those properties
    /// in the <see cref="Reset"/> method. The render graph system calls <see cref="Reset"/> automatically before reusing the data.
    /// </remarks>
    /// <example>
    /// <para>This example shows a PassData class that conditionally sets a material override. The Reset method clears it so stale
    /// references from previous frames do not leak into the next frame's render pass.</para>
    /// <code>
    /// using UnityEngine.Rendering.RenderGraphModule;
    ///
    /// class MyPassData : IRenderGraphPassData
    /// {
    ///     public TextureHandle sourceTexture;
    ///     public Material overrideMaterial; // only set on some frames
    ///
    ///     public void Reset()
    ///     {
    ///         overrideMaterial = null;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="RenderGraphObjectPool"/>
    /// <seealso cref="RenderGraph"/>
    public interface IRenderGraphPassData
    {
        /// <summary>
        /// Resets the PassData object so it can be used as a new instance next time it's created.
        /// </summary>
        /// <remarks>
        /// To avoid memory allocations and generating garbage, the system reuses objects.
        /// This method lets you clear the properties in a `PassData` object in the render graph system, to make sure Unity doesn't keep data across frames.
        /// Note that this is different from a Dispose method or destructor method, as the object is reset, not freed.
        /// </remarks>
        void Reset();
    }
}
