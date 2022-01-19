namespace UnityEngine.Rendering
{
    /// <summary>
    /// Specifies the logging level for shader variants
    /// </summary>
    public enum ShaderVariantLogLevel
    {
        /// <summary>
        /// All logging of shader variants is disabled
        /// </summary>
        Disabled,
        /// <summary>
        /// Only log the processed shaders of the current pipeline
        /// </summary>
        OnlySRPShaders,
        /// <summary>
        /// Everything that is processed will be logged
        /// </summary>
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
