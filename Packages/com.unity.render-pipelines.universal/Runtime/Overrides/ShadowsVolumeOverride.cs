using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds the maximum shadow distance and the main light shadow cascade settings.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Shadows")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [DisplayInfo(name = "Shadows")]
    public sealed class ShadowsVolumeOverride : VolumeComponent
    {
        /// <summary>
        /// Maximum distance from the camera at which URP renders shadows, in meters.
        /// </summary>
        [Tooltip("Maximum distance from the camera at which URP renders shadows, in meters.")]
        public NoInterpMinFloatParameter maxShadowDistance = new NoInterpMinFloatParameter(50f, 0f);

        /// <summary>
        /// Number of cascades URP uses for main light shadows.
        /// </summary>
        [Tooltip("Number of cascades URP uses for main light shadows.")]
        public NoInterpClampedIntParameter cascadeCount = new NoInterpClampedIntParameter(1, UniversalRenderPipelineAsset.k_ShadowCascadeMinCount, UniversalRenderPipelineAsset.k_ShadowCascadeMaxCount);

        /// <summary>
        /// End of the first cascade as a fraction of the maximum shadow distance.
        /// </summary>
        [Tooltip("Where the first cascade ends and the second cascade starts.")]
        public NoInterpClampedFloatParameter cascadeSplit0 = new NoInterpClampedFloatParameter(0.067f, 0f, 1f);

        /// <summary>
        /// End of the second cascade as a fraction of the maximum shadow distance.
        /// </summary>
        [Tooltip("Where the second cascade ends and the third cascade starts.")]
        public NoInterpClampedFloatParameter cascadeSplit1 = new NoInterpClampedFloatParameter(0.2f, 0f, 1f);

        /// <summary>
        /// End of the third cascade as a fraction of the maximum shadow distance.
        /// </summary>
        [Tooltip("Where the third cascade ends and the fourth cascade starts.")]
        public NoInterpClampedFloatParameter cascadeSplit2 = new NoInterpClampedFloatParameter(0.467f, 0f, 1f);

        /// <summary>
        /// Fraction of the last cascade over which shadows fade out.
        /// </summary>
        [Tooltip("Fraction of the last cascade over which shadows fade out.")]
        public NoInterpClampedFloatParameter lastCascadeBorder = new NoInterpClampedFloatParameter(0.2f, 0f, 1f);

        internal Vector3 GetCascadeSplit()
        {
            float s0 = cascadeSplit0.value;
            float s1 = Mathf.Max(cascadeSplit1.value, s0);
            float s2 = Mathf.Max(cascadeSplit2.value, s1);

            switch (cascadeCount.value)
            {
                case 1: return new Vector3(1f, 0f, 0f);
                case 2: return new Vector3(s0, 1f, 0f);
                case 3: return new Vector3(s0, s1, 0f);
                default: return new Vector3(s0, s1, s2);
            }
        }
    }
}
