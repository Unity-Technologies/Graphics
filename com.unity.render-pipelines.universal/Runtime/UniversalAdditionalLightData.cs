using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.LWRP
{
    [Obsolete("LWRP -> Universal (UnityUpgradable) -> UnityEngine.Rendering.Universal.UniversalAdditionalLightData", true)]
    public class LWRPAdditionalLightData
    {
    }
}


namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class UniversalAdditionalLightData : MonoBehaviour
    {
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
        public static readonly int AdditionalLightsShadowDefaultResolutionTier   = AdditionalLightsShadowResolutionTierLow;
        public static readonly int AdditionalLightsShadowDefaultCustomResolution = 128;

        [Tooltip("Controls if light shadow resolution uses pipeline settings.")]
        [SerializeField] int m_AdditionalLightsShadowResolutionTier   = AdditionalLightsShadowDefaultResolutionTier;

        public int additionalLightsShadowResolutionTier
        {
            get { return m_AdditionalLightsShadowResolutionTier; }
        }
    }
}
