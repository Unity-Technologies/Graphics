namespace UnityEngine.Rendering.Universal
{
    partial class UniversalRenderPipelineGlobalSettings : IShaderVariantSettings
    {
        [SerializeField]
        internal Rendering.ShaderVariantLogLevel m_ShaderVariantLogLevel = Rendering.ShaderVariantLogLevel.Disabled;

        /// <summary>
        /// Specifies the level of the logging for shader variants
        /// </summary>
        public Rendering.ShaderVariantLogLevel shaderVariantLogLevel { get => m_ShaderVariantLogLevel; set => m_ShaderVariantLogLevel = value; }

        [SerializeField]
        internal bool m_ExportShaderVariants = true;

        /// <summary>
        /// Specifies if the stripping of the shaders that variants needs to be exported
        /// </summary>
        public bool exportShaderVariants { get => m_ExportShaderVariants; set => m_ExportShaderVariants = true; }
    }
}
