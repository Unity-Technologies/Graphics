using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Define the RPAsset inclusion at build time, for your pipeline.
    /// Default: only RPAsset in QualitySettings are embedded on build
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "H: RP Assets Inclusion", Order = 990), HideInInspector]
    public class IncludeAdditionalRPAssets : IRenderPipelineGraphicsSettings
    {
        enum Version
        {
            Initial,

            Count,
            Last = Count - 1
        }
        [SerializeField, HideInInspector]
        private Version m_version = Version.Last;
        int IRenderPipelineGraphicsSettings.version => (int)m_version;

        [SerializeField]
        private bool m_IncludeReferencedInScenes;

        /// <summary> Additionaly include RPAsset referenced in Scene. </summary>
        public bool includeReferencedInScenes
        {
            get => m_IncludeReferencedInScenes;
            set => this.SetValueAndNotify(ref m_IncludeReferencedInScenes, value, nameof(m_IncludeReferencedInScenes));
        }
        
        [SerializeField]
        private bool m_IncludeAssetsByLabel;
        
        /// <summary> Additionaly include RPAsset that have a specific label. </summary>
        public bool includeAssetsByLabel
        {
            get => m_IncludeAssetsByLabel;
            set => this.SetValueAndNotify(ref m_IncludeAssetsByLabel, value, nameof(m_IncludeAssetsByLabel));
        }
        
        [SerializeField]
        private string m_LabelToInclude;
        
        /// <summary> Label to use when including RPAsset by label. </summary>
        public string labelToInclude
        {
            get => m_LabelToInclude;
            set => this.SetValueAndNotify(ref m_LabelToInclude, value, nameof(m_LabelToInclude));
        }
    }
}
