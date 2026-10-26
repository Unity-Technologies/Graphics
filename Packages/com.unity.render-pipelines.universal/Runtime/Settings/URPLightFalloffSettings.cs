using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The distance attenuation formula used by URP lights.
    /// </summary>
    public enum LightFalloffMode
    {
        /// <summary>
        /// Physically-based inverse squared falloff. URP default.
        /// </summary>
        [InspectorName("Inverse Squared")]
        InverseSquared = 0,

        /// <summary>
        /// Falloff matching the Built-in Render Pipeline. Use for Built-in to URP migration.
        /// </summary>
        [InspectorName("Quadratic")]
        Quadratic = 1,
    }

    /// <summary>
    /// Project-wide light falloff settings for URP.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Lighting", Order = 21)]
    public class URPLightFalloffSettings : IRenderPipelineGraphicsSettings
    {
        [SerializeField, HideInInspector] private int m_Version = 1;

        int IRenderPipelineGraphicsSettings.version => m_Version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, Tooltip("How light intensity falls off with distance. Use Quadratic to match the Built-in Render Pipeline.")]
        private LightFalloffMode m_LightFalloffMode = LightFalloffMode.InverseSquared;

        /// <summary>
        /// Defines the light attenuation formula used by URP lights.
        /// </summary>
        public LightFalloffMode lightFalloffMode
        {
            get => m_LightFalloffMode;
#if UNITY_EDITOR
            set => this.SetValueAndNotify(ref m_LightFalloffMode, value, nameof(m_LightFalloffMode));
#endif
        }
    }
}
