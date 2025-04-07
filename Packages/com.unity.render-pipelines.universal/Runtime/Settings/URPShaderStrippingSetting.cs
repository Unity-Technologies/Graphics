using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A Graphics Settings container for settings related to shader stripping for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab (URP).
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// 
    /// This container is removed from all build Players.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> Here is an example of how to determine if your project strips shader variants when building a Player with URP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    /// 
    /// public static class URPShaderStrippingHelper
    /// {
    ///     public static bool enabled
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;URPShaderStrippingSetting&gt;();
    ///             if (gs == null) //not in URP or in a Player
    ///                 return false;
    ///             return gs.stripUnusedVariants;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
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

        /// <summary>Indicates the current version of this settings container. Used exclusively for project upgrades.</summary>
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
