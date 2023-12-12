using System;
using System.ComponentModel;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Render Graph global settings class.
    /// </summary>
    [Serializable, SupportedOnRenderPipeline, Category("Render Graph")]
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
            set => this.SetValueAndNotify(ref m_EnableCompilationCaching, value, nameof(m_EnableCompilationCaching));
        }
    }
}
