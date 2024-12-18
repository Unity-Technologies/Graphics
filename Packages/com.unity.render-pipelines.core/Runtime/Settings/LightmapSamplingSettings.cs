using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Lightmap Sampling global settings class.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline()]
    [Categorization.CategoryInfo(Name = "Lightmap Sampling Settings", Order = 20)]
    public class LightmapSamplingSettings : IRenderPipelineGraphicsSettings
    {
        [SerializeField, HideInInspector]
        int m_Version = 1;

        int IRenderPipelineGraphicsSettings.version { get => m_Version; }

        [SerializeField, Tooltip("Use Bicubic Lightmap Sampling. Enabling this will improve the appearance of lightmaps, but may worsen performance on lower end platforms.")]
        bool m_UseBicubicLightmapSampling;

        /// <summary>
        /// Whether to use bicubic sampling for lightmaps.
        /// </summary>
        public bool useBicubicLightmapSampling
        {
            get => m_UseBicubicLightmapSampling;
            set => this.SetValueAndNotify(ref m_UseBicubicLightmapSampling, value, nameof(m_UseBicubicLightmapSampling));
        }
    }
}
