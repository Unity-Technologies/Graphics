namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Controls when URP renders via an intermediate texture.
    /// </summary>
    public enum IntermediateTextureMode
    {
        /// <summary>
        /// Uses information declared by active Renderer Features to automatically determine whether to render via an intermediate texture or not. <seealso cref="ScriptableRenderPass.ConfigureInput"/>.
        /// </summary>
        Auto,
        /// <summary>
        /// Forces rendering via an intermediate texture. Use this option only for compatibility with Renderer Features that do not declare their inputs with <see cref="ScriptableRenderPass.ConfigureInput"/>. Using this option might have a significant performance impact on some platforms.
        /// </summary>
        Always
    }
}
