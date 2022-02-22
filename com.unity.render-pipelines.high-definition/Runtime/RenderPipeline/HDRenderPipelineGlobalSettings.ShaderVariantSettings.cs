using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IShaderVariantSettings
    {
        [SerializeField, FormerlySerializedAs("shaderVariantLogLevel")]
        internal ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

        /// <summary>
        /// Specifies the level of the logging for shader variants
        /// </summary>
        public ShaderVariantLogLevel shaderVariantLogLevel { get => m_ShaderVariantLogLevel; set => m_ShaderVariantLogLevel = value; }

        [SerializeField]
        internal bool m_ExportShaderVariants = true;

        /// <summary>
        /// Specifies if the stripping of the shaders that variants needs to be exported
        /// </summary>
        public bool exportShaderVariants { get => m_ExportShaderVariants; set => m_ExportShaderVariants = true; }
    }
}
