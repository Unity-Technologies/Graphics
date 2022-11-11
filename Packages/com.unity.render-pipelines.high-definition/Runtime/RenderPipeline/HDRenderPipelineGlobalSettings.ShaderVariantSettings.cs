using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IShaderVariantSettings
    {
        [SerializeField, FormerlySerializedAs("shaderVariantLogLevel")] internal ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;
        [SerializeField, FormerlySerializedAs("supportRuntimeDebugDisplay")] internal bool m_SupportRuntimeDebugDisplay = false;

        [SerializeField] internal bool m_ExportShaderVariants = true;
        [SerializeField] internal bool m_StripDebugVariants = false;

        /// <summary>
        /// Specifies the level of the logging for shader variants
        /// </summary>
        public ShaderVariantLogLevel shaderVariantLogLevel { get => m_ShaderVariantLogLevel; set => m_ShaderVariantLogLevel = value; }

        /// <summary>
        /// Specifies if the stripping of the shaders that variants needs to be exported
        /// </summary>
        public bool exportShaderVariants { get => m_ExportShaderVariants; set => m_ExportShaderVariants = true; }

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        public bool stripDebugVariants
        {
            get => m_StripDebugVariants;
            set => m_StripDebugVariants = value;
        }
    }
}
