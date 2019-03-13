namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Class <c>ScriptableRendererFeature</c> extends <c>ScriptableRenderer</c> with additional features.
    /// </summary>
    /// <seealso cref="ScriptableRenderer"/>
    public abstract class ScriptableRendererFeature : ScriptableObject
    {
        /// <summary>
        /// Initializes this feature's resources.
        /// </summary>
        public abstract void Create();

        /// <summary>
        /// Injects one or multiple <c>ScriptableRenderPass</c> in the renderer.
        /// </summary>
        /// <param name="renderPasses">List of render passes to add to.</param>
        /// <param name="renderingData">Rendering state. Use this to setup render passes.</param>
        public abstract void AddRenderPasses(ScriptableRenderer renderer,
            ref RenderingData renderingData);

        void OnEnable()
        {
            Create();
        }

        void OnValidate()
        {
            Create();
        }
    }
}
