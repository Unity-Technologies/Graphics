using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class that stores the shader stripping settings that are specific for <see cref="UniversalRenderPipeline"/>
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Additional Shader Stripping Settings", Order = 40)]
    [Categorization.ElementInfo(Order = 10)]
    public class URPShaderStrippingSetting : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        private Version m_Version;

        /// <summary>Current version.</summary>
        public int version => (int)m_Version;
        #endregion

        #region SerializeFields
        [SerializeField]
        [Tooltip("Controls whether to automatically strip post processing shader variants based on VolumeProfile components. Stripping is done based on VolumeProfiles in project, their usage in scenes is not considered.")]
        bool m_StripUnusedPostProcessingVariants = false;

        [SerializeField]
        [Tooltip("Controls whether to strip variants if the feature is disabled.")]
        bool m_StripUnusedVariants = true;

        [SerializeField]
        [Tooltip("Controls whether Screen Coordinates Override shader variants are automatically stripped.")]
        bool m_StripScreenCoordOverrideVariants = true;
        #endregion

        #region Data Accessors

        /// <summary>
        /// Controls whether to automatically strip post processing shader variants based on <see cref="VolumeProfile"/> components.
        /// Stripping is done based on VolumeProfiles in project, their usage in scenes is not considered.
        /// </summary>
        public bool stripUnusedPostProcessingVariants
        {
            get => m_StripUnusedPostProcessingVariants;
            set => this.SetValueAndNotify(ref m_StripUnusedPostProcessingVariants, value);
        }

        /// <summary>
        /// Controls whether to strip variants if the feature is disabled.
        /// </summary>
        public bool stripUnusedVariants
        {
            get => m_StripUnusedVariants;
            set => this.SetValueAndNotify(ref m_StripUnusedVariants, value);
        }

        /// <summary>
        /// Controls whether Screen Coordinates Override shader variants are automatically stripped.
        /// </summary>
        public bool stripScreenCoordOverrideVariants
        {
            get => m_StripScreenCoordOverrideVariants;
            set => this.SetValueAndNotify(ref m_StripScreenCoordOverrideVariants, value);
        }
        #endregion
    }
}
