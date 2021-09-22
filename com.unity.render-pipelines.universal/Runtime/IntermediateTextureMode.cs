namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Controls when URP renders via an intermediate texture.
    /// </summary>
    public enum IntermediateTextureMode
    {
        /// <summary>
        /// Uses information declared by active Renderer Features to automatically determine whether to render through an intermediate texture or not.
        /// </summary>
        Auto,
        /// <summary>
        /// Forces rendering via an intermediate texture, enabling compatibility with renderer features that do not declare their needed inputs, but can have a significant performance impact on some platforms.
        /// </summary>
        Always
    }
}
