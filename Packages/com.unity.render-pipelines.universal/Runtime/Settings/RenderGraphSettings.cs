using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Settings for Render Graph
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Render Graph", Order = 50)]
    [Categorization.ElementInfo(Order = -10)]
    public class RenderGraphSettings: IRenderPipelineGraphicsSettings
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

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region SerializeFields

        [SerializeField]
        [Tooltip("When enabled, URP does not use the Render Graph API to construct and execute the frame. Use this option only for compatibility purposes.")]
        [RecreatePipelineOnChange]
        private bool m_EnableRenderCompatibilityMode;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, Universal Rendering Pipeline will not use Render Graph API to construct and execute the frame.
        /// </summary>
        public bool enableRenderCompatibilityMode
        {
            get => m_EnableRenderCompatibilityMode && !RenderGraphGraphicsAutomatedTests.enabled;
            set
            {
                this.SetValueAndNotify(ref m_EnableRenderCompatibilityMode, value, nameof(m_EnableRenderCompatibilityMode));
            }
        }

        #endregion
    }
}
