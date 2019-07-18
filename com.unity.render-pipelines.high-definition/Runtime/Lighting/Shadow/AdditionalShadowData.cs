using UnityEngine.Rendering.HighDefinition;
using System;

namespace UnityEngine.Rendering
{
    [RequireComponent(typeof(Light))]
    [Obsolete("This component will be removed in the future, it's content have been moved to HDAdditionalLightData.")]
    [ExecuteAlways]
    class AdditionalShadowData : MonoBehaviour
    {
// Currently m_Version is not used and produce a warning, remove these pragmas at the next version incrementation
#pragma warning disable 414
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("version")]
        private int m_Version = 1;
#pragma warning restore 414

        [Obsolete("Obsolete, use HDAdditionalLightData.customResolution instead.")]
        [UnityEngine.Serialization.FormerlySerializedAs("shadowResolution")]
        public int customResolution = HDAdditionalLightData.k_DefaultShadowResolution;

        [Range(0.0f, 1.0f)]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowDimmer instead.")]
        public float shadowDimmer = 1.0f;

        [Range(0.0f, 1.0f)]
        [Obsolete("Obsolete, use HDAdditionalLightData.volumetricShadowDimmer instead.")]
        public float volumetricShadowDimmer = 1.0f;

        [Obsolete("Obsolete, use HDAdditionalLightData.shadowFadeDistance instead.")]
        public float shadowFadeDistance = 10000.0f;

        [Obsolete("Obsolete, use HDAdditionalLightData.contactShadows instead.")]
        public bool contactShadows = false;

        [Obsolete("Obsolete, use HDAdditionalLightData.shadowTint instead.")]
        public Color shadowTint = Color.black;

        // bias control
        [Obsolete("Obsolete, use HDAdditionalLightData.normalBias instead.")]
        public float normalBias = 0.75f;

        [Obsolete("Obsolete, use HDAdditionalLightData.constantBias instead.")]
        public float constantBias = 0.15f;

        [Obsolete("Obsolete, use HDAdditionalLightData.shadowUpdateMode instead.")]
        public ShadowUpdateMode shadowUpdateMode = ShadowUpdateMode.EveryFrame;

        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowCascadeRatios instead.")]
        float[] shadowCascadeRatios = new float[3] { 0.05f, 0.2f, 0.3f };
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowCascadeBorders instead.")]
        float[] shadowCascadeBorders = new float[4] { 0.2f, 0.2f, 0.2f, 0.2f };
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowAlgorithm instead.")]
        int shadowAlgorithm = 0;
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowVariant instead.")]
        int shadowVariant = 0;
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowPrecision instead.")]
        int shadowPrecision = 0;

        void OnEnable()
        {
            var additionalData = GetComponent< HDAdditionalLightData >();

            // If the additional datas is null, then we can't upgrade
            if (additionalData != null)
            {
                additionalData.customResolution = customResolution;
                additionalData.shadowDimmer = shadowDimmer;
                additionalData.volumetricShadowDimmer = volumetricShadowDimmer;
                additionalData.shadowFadeDistance = shadowFadeDistance;
                additionalData.contactShadows = contactShadows;
                additionalData.shadowTint = shadowTint;
                additionalData.normalBias = normalBias;
                additionalData.constantBias = constantBias;
                additionalData.shadowUpdateMode = shadowUpdateMode;
                additionalData.shadowCascadeRatios = shadowCascadeRatios;
                additionalData.shadowCascadeBorders = shadowCascadeBorders;
                additionalData.shadowAlgorithm = shadowAlgorithm;
                additionalData.shadowVariant = shadowVariant;
                additionalData.shadowPrecision = shadowPrecision;

                CoreUtils.Destroy(this);
            }
        }
    }
}
