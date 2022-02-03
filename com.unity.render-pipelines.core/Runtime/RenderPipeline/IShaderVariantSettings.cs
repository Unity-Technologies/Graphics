namespace UnityEngine.Rendering
{
    /// <summary>
    /// Specifies the logging level for shader variants
    /// </summary>
    public enum ShaderVariantLogLevel
    {
        /// <summary>Disable all log for Shader Variant</summary>
        [Tooltip("Logging of shader variants is disabled")]
        Disabled,
        /// <summary>Only logs Shaders with a Subshader containing the tag RenderPipeline</summary>
        [Tooltip("Logging of shader variants is enabled and filtering them by the tag RenderPipeline")]
        OnlySRPShaders,
        /// <summary>No filter is applied to the logging of shader variants</summary>
        [Tooltip("Logging of shader variants is enabled and without filters")]
        AllShaders,
    }

    /// <summary>
    /// Exposes settings for shader variants
    /// </summary>
    public interface IShaderVariantSettings
    {
        /// <summary>
        /// Specifies the level of the logging for shader variants
        /// </summary>
        ShaderVariantLogLevel shaderVariantLogLevel { get; set; }

        /// <summary>
        /// Specifies if the stripping of the shaders variants needs to be exported
        /// </summary>
        bool exportShaderVariants { get; set; }
    }
}
