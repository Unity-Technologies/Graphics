using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Settings for Render Graph
    /// </summary>
    [Serializable]
    [Category("Miscellaneous")]
    [HideInInspector] // TODO Remove when UI has fully being migrated
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
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
        [InspectorName("Use Render Graph")]
        [Tooltip("When enabled, Universal Rendering Pipeline will use Render Graph API to construct and execute the frame")]
        private bool m_UseRenderGraph;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, Universal Rendering Pipeline will use Render Graph API to construct and execute the frame
        /// </summary>
        public bool useRenderGraph
        {
            get => m_UseRenderGraph || RenderGraphGraphicsAutomatedTests.enabled;
            set => this.SetValueAndNotify(ref m_UseRenderGraph, value, nameof(m_UseRenderGraph));
        }

        #endregion
    }
}
