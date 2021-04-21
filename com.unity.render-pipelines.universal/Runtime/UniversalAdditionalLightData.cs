using System;

namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class UniversalAdditionalLightData : MonoBehaviour
    {
        // Version 0 means serialized data before the version field.
        [SerializeField] int m_Version = 1;
        public int version
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

        public static readonly int AdditionalLightsShadowResolutionTierCustom    = -1;
        public static readonly int AdditionalLightsShadowResolutionTierLow       =  0;
        public static readonly int AdditionalLightsShadowResolutionTierMedium    =  1;
        public static readonly int AdditionalLightsShadowResolutionTierHigh      =  2;
        public static readonly int AdditionalLightsShadowDefaultResolutionTier   = AdditionalLightsShadowResolutionTierHigh;
        public static readonly int AdditionalLightsShadowDefaultCustomResolution = 128;
        public static readonly int AdditionalLightsShadowMinimumResolution       = 128;

        [Tooltip("Controls if light shadow resolution uses pipeline settings.")]
        [SerializeField] int m_AdditionalLightsShadowResolutionTier   = AdditionalLightsShadowDefaultResolutionTier;

        public int additionalLightsShadowResolutionTier
        {
            get { return m_AdditionalLightsShadowResolutionTier; }
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

        // TODO: check priority ordering, so that it's consistent with rest URP/Unity
        [Tooltip("Light priority. Higher priority number is more important.")]
        [SerializeField] int m_Priority = 0;
        public int priority
        {
            get => m_Priority;
            set => m_Priority = value;
        }
    }
}
