using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>Light Layers.</summary>
    [Flags]
    public enum LightLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Light Layer 0.</summary>
        LightLayerDefault = 1 << 0,
        /// <summary>Light Layer 1.</summary>
        LightLayer1 = 1 << 1,
        /// <summary>Light Layer 2.</summary>
        LightLayer2 = 1 << 2,
        /// <summary>Light Layer 3.</summary>
        LightLayer3 = 1 << 3,
        /// <summary>Light Layer 4.</summary>
        LightLayer4 = 1 << 4,
        /// <summary>Light Layer 5.</summary>
        LightLayer5 = 1 << 5,
        /// <summary>Light Layer 6.</summary>
        LightLayer6 = 1 << 6,
        /// <summary>Light Layer 7.</summary>
        LightLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    /// <summary>
    /// Contains extension methods for Light class.
    /// </summary>
    public static class LightExtensions
    {
        /// <summary>
        /// Universal Render Pipeline exposes additional light data in a separate component.
        /// This method returns the additional data component for the given light or create one if it doesn't exist yet.
        /// </summary>
        /// <param name="light"></param>
        /// <returns>The <c>UniversalAdditionalLightData</c> for this light.</returns>
        /// <see cref="UniversalAdditionalLightData"/>
        public static UniversalAdditionalLightData GetUniversalAdditionalLightData(this Light light)
        {
            var gameObject = light.gameObject;
            bool componentExists = gameObject.TryGetComponent<UniversalAdditionalLightData>(out var lightData);
            if (!componentExists)
                lightData = gameObject.AddComponent<UniversalAdditionalLightData>();

            return lightData;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [URPHelpURL("universal-additional-light-data")]
    public class UniversalAdditionalLightData : MonoBehaviour, IAdditionalData
    {
        // Version 0 means serialized data before the version field.
        [SerializeField] int m_Version = 1;
        internal int version
        {
            get => m_Version;
        }

        [Tooltip("Controls if light Shadow Bias parameters use pipeline settings.")]
        [SerializeField] bool m_UsePipelineSettings = true;

        public bool usePipelineSettings
        {
            get { return m_UsePipelineSettings; }
            set { m_UsePipelineSettings = value; }
        }

        public static readonly int AdditionalLightsShadowResolutionTierCustom = -1;
        public static readonly int AdditionalLightsShadowResolutionTierLow = 0;
        public static readonly int AdditionalLightsShadowResolutionTierMedium = 1;
        public static readonly int AdditionalLightsShadowResolutionTierHigh = 2;
        public static readonly int AdditionalLightsShadowDefaultResolutionTier = AdditionalLightsShadowResolutionTierHigh;
        public static readonly int AdditionalLightsShadowDefaultCustomResolution = 128;
        public static readonly int AdditionalLightsShadowMinimumResolution = 128;

        [Tooltip("Controls if light shadow resolution uses pipeline settings.")]
        [SerializeField] int m_AdditionalLightsShadowResolutionTier = AdditionalLightsShadowDefaultResolutionTier;

        public int additionalLightsShadowResolutionTier
        {
            get { return m_AdditionalLightsShadowResolutionTier; }
        }

        // The layer(s) this light belongs too.
        [SerializeField] LightLayerEnum m_LightLayerMask = LightLayerEnum.LightLayerDefault;

        public LightLayerEnum lightLayerMask
        {
            get { return m_LightLayerMask; }
            set { m_LightLayerMask = value; }
        }

        [SerializeField] bool m_CustomShadowLayers = false;

        // if enabled, shadowLayerMask use the same settings as lightLayerMask.
        public bool customShadowLayers
        {
            get { return m_CustomShadowLayers; }
            set { m_CustomShadowLayers = value; }
        }

        // The layer(s) used for shadow casting.
        [SerializeField] LightLayerEnum m_ShadowLayerMask = LightLayerEnum.LightLayerDefault;

        public LightLayerEnum shadowLayerMask
        {
            get { return m_ShadowLayerMask; }
            set { m_ShadowLayerMask = value; }
        }

        [Tooltip("Controls the size of the cookie mask currently assigned to the light.")]
        [SerializeField] Vector2 m_LightCookieSize = Vector2.one;
        public Vector2 lightCookieSize
        {
            get => m_LightCookieSize;
            set => m_LightCookieSize = value;
        }

        [Tooltip("Controls the offset of the cookie mask currently assigned to the light.")]
        [SerializeField] Vector2 m_LightCookieOffset = Vector2.zero;
        public Vector2 lightCookieOffset
        {
            get => m_LightCookieOffset;
            set => m_LightCookieOffset = value;
        }
    }
}
