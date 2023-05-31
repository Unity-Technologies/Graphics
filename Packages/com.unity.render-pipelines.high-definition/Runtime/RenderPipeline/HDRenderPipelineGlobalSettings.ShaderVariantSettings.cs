using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IShaderVariantSettings
    {
        [SerializeField] private ShaderStrippingSetting m_ShaderStrippingSetting = new ();

#pragma warning disable 0414
        [SerializeField, FormerlySerializedAs("shaderVariantLogLevel"), Obsolete("Use ShaderStrippingSetting #from(23.2)")] internal ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;
        [SerializeField, FormerlySerializedAs("supportRuntimeDebugDisplay"), Obsolete("Use ShaderStrippingSetting #from(23.2)")] internal bool m_SupportRuntimeDebugDisplay = false;

        [SerializeField, Obsolete("Use ShaderStrippingSetting #from(23.2)")] internal bool m_ExportShaderVariants = true;
        [SerializeField, Obsolete("Use ShaderStrippingSetting #from(23.2)")] internal bool m_StripDebugVariants = false;
#pragma warning restore 0414

        /// <summary>
        /// Specifies the level of the logging for shader variants
        /// </summary>
        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get => m_ShaderStrippingSetting.shaderVariantLogLevel;
            set => m_ShaderStrippingSetting.shaderVariantLogLevel  = value;
        }

        /// <summary>
        /// Specifies if the stripping of the shaders that variants needs to be exported
        /// </summary>
        public bool exportShaderVariants
        {
            get => m_ShaderStrippingSetting.exportShaderVariants;
            set => m_ShaderStrippingSetting.exportShaderVariants = value;
        }

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        public bool stripDebugVariants
        {
            get => m_ShaderStrippingSetting.stripRuntimeDebugShaders;
            set => m_ShaderStrippingSetting.stripRuntimeDebugShaders = value;
        }
    }
}
