using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Exposure global settings class.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Lighting", Order = 21)]
    public class URPExposureSettings : IRenderPipelineGraphicsSettings
    {
        [SerializeField, HideInInspector] private int version = 1;

        int IRenderPipelineGraphicsSettings.version => version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, Tooltip("Applies exposure to support physical light units and higher dynamic range. Recommended when working with physically based lighting. May impact performance on low-end platforms.")]
        private bool m_UseExposure = true;

        /// <summary>
        /// Whether to take exposure into account when rendering.
        /// </summary>
        public bool UseExposure
        {
            get => m_UseExposure;
#if UNITY_EDITOR
            internal set => this.SetValueAndNotify(ref m_UseExposure, value, nameof(m_UseExposure));
#endif
        }
    }
}
