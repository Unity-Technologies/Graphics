using System;

namespace UnityEngine.Rendering.Universal
{
    partial class UniversalRenderPipelineGlobalSettings : IShaderVariantSettings
    {
        [SerializeField] Rendering.ShaderVariantLogLevel m_ShaderVariantLogLevel = Rendering.ShaderVariantLogLevel.Disabled;
        [SerializeField] bool m_ExportShaderVariants = true;
        [SerializeField] bool m_StripDebugVariants = true;
        [SerializeField] bool m_StripUnusedPostProcessingVariants = false;
        [SerializeField] bool m_StripUnusedVariants = true;
        [SerializeField] bool m_StripUnusedLODCrossFadeVariants = true;
        [SerializeField] bool m_StripScreenCoordOverrideVariants = true;

        /// <summary>
        /// Specifies the level of the logging for shader variants
        /// </summary>
        public Rendering.ShaderVariantLogLevel shaderVariantLogLevel { get => m_ShaderVariantLogLevel; set => m_ShaderVariantLogLevel = value; }

        /// <summary>
        /// Specifies if the stripping of the shaders that variants needs to be exported
        /// </summary>
        public bool exportShaderVariants { get => m_ExportShaderVariants; set => m_ExportShaderVariants = true; }

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        [Obsolete("Please use stripRuntimeDebugShaders instead. #from(23.1)", false)]
        public bool supportRuntimeDebugDisplay = false;

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        public bool stripDebugVariants { get => m_StripDebugVariants; set { m_StripDebugVariants = value; } }

        /// <summary>
        /// Controls whether strips automatically post processing shader variants based on <see cref="VolumeProfile"/> components.
        /// It strips based on VolumeProfiles in project and not scenes that actually uses it.
        /// </summary>
        public bool stripUnusedPostProcessingVariants { get => m_StripUnusedPostProcessingVariants; set { m_StripUnusedPostProcessingVariants = value; } }

        /// <summary>
        /// Controls whether strip off variants if the feature is enabled.
        /// </summary>
        public bool stripUnusedVariants { get => m_StripUnusedVariants; set { m_StripUnusedVariants = value; } }

        /// If this property is true, Unity strips the LOD variants if the LOD cross-fade feature (UniversalRenderingPipelineAsset.enableLODCrossFade) is disabled.
        /// </summary>
        public bool stripUnusedLODCrossFadeVariants { get => m_StripUnusedLODCrossFadeVariants; set { m_StripUnusedLODCrossFadeVariants = value; } }

        /// <summary>
        /// Controls whether Screen Coordinates Override shader variants are automatically stripped.
        /// </summary>
        public bool stripScreenCoordOverrideVariants { get => m_StripScreenCoordOverrideVariants; set => m_StripScreenCoordOverrideVariants = value; }
    }
}
