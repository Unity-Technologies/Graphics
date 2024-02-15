using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Render Graph global settings class.
    /// </summary>
    [Serializable] 
    [SupportedOnRenderPipeline] 
    [Categorization.CategoryInfo(Name = "Render Graph", Order = 50)]
    [Categorization.ElementInfo(Order = 0)]
    public class RenderGraphGlobalSettings : IRenderPipelineGraphicsSettings
    {
        enum Version
        {
            Initial,
            Count,
            Last = Count - 1
        }

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, HideInInspector]
        private Version m_version = Version.Last;
        int IRenderPipelineGraphicsSettings.version => (int)m_version;

        [RecreatePipelineOnChange , SerializeField, Tooltip("Enable caching of render graph compilation from one frame to another.")]
        private bool m_EnableCompilationCaching = true;

        /// <summary>Enable Compilation caching for render graph.</summary>
        public bool enableCompilationCaching
        {
            get => m_EnableCompilationCaching;
            set => this.SetValueAndNotify(ref m_EnableCompilationCaching, value);
        }

        [RecreatePipelineOnChange , SerializeField, Tooltip("Enable validity checks of render graph in Editor and Development mode. Always disabled in Release build.")]
        private bool m_EnableValidityChecks = true;

        /// <summary>Enable validity checks for render graph. Always disabled in Release mode.</summary>
        public bool enableValidityChecks
        {
            get => m_EnableValidityChecks;
            set => this.SetValueAndNotify(ref m_EnableValidityChecks, value);
        }
    }
}
