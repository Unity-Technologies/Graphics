using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Miscellaneous", Order = 100)]
    [Categorization.ElementInfo(Order = 0)]
    class AnalyticDerivativeSettings : IRenderPipelineGraphicsSettings
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
        [InspectorName("Analytic Derivative Emulation (experimental)")]
        [Tooltip("When enabled, imported shaders will use analytic derivatives for their Forward and GBuffer pass. This is a developer-only feature for testing.")]
        private bool m_AnalyticDerivativeEmulation = false;

        [SerializeField]
        [InspectorName("Analytic Derivative Debug Output (experimental)")]
        [Tooltip("When enabled, output detailed logs of the analytic derivative parser. This is a developer-only feature for testing.")]
        private bool m_AnalyticDerivativeDebugOutput = false;

        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, imported shaders will use analytic derivatives for their Forward and GBuffer pass. This is a developer-only feature for testing.
        /// </summary>
        public bool emulation
        {
            get => m_AnalyticDerivativeEmulation;
            set => this.SetValueAndNotify(ref m_AnalyticDerivativeEmulation, value);
        }

        /// <summary>
        /// When enabled, output detailed logs of the analytic derivative parser. This is a developer-only feature for testing.
        /// </summary>
        public bool debugOutput
        {
            get => m_AnalyticDerivativeDebugOutput;
            set => this.SetValueAndNotify(ref m_AnalyticDerivativeDebugOutput, value);
        }

        #endregion
    }
}
