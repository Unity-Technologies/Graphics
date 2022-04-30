namespace UnityEngine.Rendering
{
    /// <summary>
    /// Specifies the logging level for shader variants
    /// </summary>
    public enum ShaderVariantLogLevel
    {
        /// <summary>Disable all log for Shader Variant</summary>
        [Tooltip("No shader variants are logged")]
        Disabled,
        /// <summary>Only logs SRP Shaders when logging Shader Variant</summary>
        [Tooltip("Only shaders that are compatible with SRPs (e.g., URP, HDRP) are logged")]
        OnlySRPShaders,
        /// <summary>Logs all Shader Variant</summary>
        [Tooltip("All shader variants are logged")]
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
