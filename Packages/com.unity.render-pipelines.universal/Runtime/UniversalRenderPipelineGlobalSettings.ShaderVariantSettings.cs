using System;

namespace UnityEngine.Rendering.Universal
{
    partial class UniversalRenderPipelineGlobalSettings : IShaderVariantSettings
    {
        [SerializeField] private ShaderStrippingSetting m_ShaderStrippingSetting = new();
        [SerializeField] private URPShaderStrippingSetting m_URPShaderStrippingSetting = new();

#pragma warning disable 0414
        [SerializeField, Obsolete("Use ShaderStrippingSetting. #from(23.2)")] Rendering.ShaderVariantLogLevel m_ShaderVariantLogLevel = Rendering.ShaderVariantLogLevel.Disabled;
        [SerializeField, Obsolete("Use ShaderStrippingSetting. #from(23.2)")] bool m_ExportShaderVariants = true;
        [SerializeField, Obsolete("Use ShaderStrippingSetting. #from(23.2)")] bool m_StripDebugVariants = true;

        [SerializeField, Obsolete("Use ShaderStrippingSetting. #from(23.2)")] bool m_StripUnusedPostProcessingVariants = false;
        [SerializeField, Obsolete("Use ShaderStrippingSetting. #from(23.2)")] bool m_StripUnusedVariants = true;
        [SerializeField, Obsolete("Use ShaderStrippingSetting. #from(23.2)")] bool m_StripScreenCoordOverrideVariants = true;
#pragma warning restore 0414

        #region Obsolete properties
        /// <summary>
        /// If this property is true, Unity strips the LOD variants if the LOD cross-fade feature (UniversalRenderingPipelineAsset.enableLODCrossFade) is disabled.
        /// </summary>
        [Obsolete("No longer used as Shader Prefiltering automatically strips out unused LOD Crossfade variants. Please use the LOD Crossfade setting in the URP Asset to disable the feature if not used. #from(2023.1)", false)]
        public bool stripUnusedLODCrossFadeVariants { get => false; set {  } }

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        [Obsolete("Please use stripRuntimeDebugShaders instead. #from(23.1)", false)]
        public bool supportRuntimeDebugDisplay = false;
        #endregion

        /// <summary>
        /// Controls the level of logging of shader variant information outputted during the build process.
        /// Information appears in the Unity Console when the build finishes.
        /// </summary>
        public Rendering.ShaderVariantLogLevel shaderVariantLogLevel
        {
            get => m_ShaderStrippingSetting.shaderVariantLogLevel;
            set => m_ShaderStrippingSetting.shaderVariantLogLevel = value;
        }

        /// <summary>
        /// Controls whether to output shader variant information to a file.
        /// </summary>
        public bool exportShaderVariants
        {
            get => m_ShaderStrippingSetting.exportShaderVariants;
            set => m_ShaderStrippingSetting.exportShaderVariants = value;
        }

        /// <summary>
        /// When enabled, all debug display shader variants are removed when you build for the Unity Player.
        /// This decreases build time, but prevents the use of most Rendering Debugger features in Player builds.
        /// </summary>
        public bool stripDebugVariants
        {
            get => m_ShaderStrippingSetting.stripRuntimeDebugShaders;
            set => m_ShaderStrippingSetting.stripRuntimeDebugShaders = value;
        }

        /// <summary>
        /// Controls whether to automatically strip post processing shader variants based on <see cref="VolumeProfile"/> components.
        /// Stripping is done based on VolumeProfiles in project, their usage in scenes is not considered.
        /// </summary>
        public bool stripUnusedPostProcessingVariants
        {
            get => m_URPShaderStrippingSetting.stripUnusedPostProcessingVariants;
            set => m_URPShaderStrippingSetting.stripUnusedPostProcessingVariants = value;
        }

        /// <summary>
        /// Controls whether to strip variants if the feature is disabled.
        /// </summary>
        public bool stripUnusedVariants
        {
            get => m_URPShaderStrippingSetting.stripUnusedVariants;
            set => m_URPShaderStrippingSetting.stripUnusedVariants = value;
        }

        /// <summary>
        /// Controls whether Screen Coordinates Override shader variants are automatically stripped.
        /// </summary>
        public bool stripScreenCoordOverrideVariants
        {
            get => m_URPShaderStrippingSetting.stripScreenCoordOverrideVariants;
            set => m_URPShaderStrippingSetting.stripScreenCoordOverrideVariants = value;
        }
    }
}
