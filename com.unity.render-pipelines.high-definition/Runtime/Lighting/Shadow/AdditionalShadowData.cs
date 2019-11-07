using UnityEngine.Rendering.HighDefinition;
using System;

namespace UnityEngine.Rendering
{
    [RequireComponent(typeof(Light))]
    [Obsolete("This component will be removed in the future, it's content have been moved to HDAdditionalLightData.")]
    [ExecuteAlways]
    class AdditionalShadowData : MonoBehaviour
    {
        [SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.customResolution instead.")]
        [UnityEngine.Serialization.FormerlySerializedAs("shadowResolution")]
        internal int customResolution = HDAdditionalLightData.k_DefaultShadowResolution;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowDimmer instead.")]
        internal float shadowDimmer = 1.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Obsolete("Obsolete, use HDAdditionalLightData.volumetricShadowDimmer instead.")]
        internal float volumetricShadowDimmer = 1.0f;

        [SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowFadeDistance instead.")]
        internal float shadowFadeDistance = 10000.0f;

        [SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.contactShadows instead.")]
        internal bool contactShadows = false;

        [SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowTint instead.")]
        internal Color shadowTint = Color.black;

        // bias control
        [SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.normalBias instead.")]
        internal float normalBias = 0.75f;

        [SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowUpdateMode instead.")]
        internal ShadowUpdateMode shadowUpdateMode = ShadowUpdateMode.EveryFrame;

        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowCascadeRatios instead.")]
        internal float[] shadowCascadeRatios = new float[3] { 0.05f, 0.2f, 0.3f };
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowCascadeBorders instead.")]
        internal float[] shadowCascadeBorders = new float[4] { 0.2f, 0.2f, 0.2f, 0.2f };
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowAlgorithm instead.")]
        internal int shadowAlgorithm = 0;
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowVariant instead.")]
        internal int shadowVariant = 0;
        [HideInInspector, SerializeField]
        [Obsolete("Obsolete, use HDAdditionalLightData.shadowPrecision instead.")]
        internal int shadowPrecision = 0;
    }
}
