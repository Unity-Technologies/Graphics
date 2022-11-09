using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Ambient Occlusion")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Override-Ambient-Occlusion")]
    public sealed class ScreenSpaceAmbientOcclusion : VolumeComponentWithQuality
    {
        /// <summary>
        /// Enable ray traced ambient occlusion.
        /// </summary>
        public BoolParameter rayTracing = new BoolParameter(false);

        /// <summary>
        /// Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.
        /// </summary>
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
        /// <summary>
        /// Controls how much the ambient occlusion affects direct lighting.
        /// </summary>
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);
        /// <summary>
        /// Sampling radius. Bigger the radius, wider AO will be achieved, risking to lose fine details and increasing cost of the effect due to increasing cache misses.
        /// </summary>
        public ClampedFloatParameter radius = new ClampedFloatParameter(2.0f, 0.25f, 5.0f);

        /// <summary>
        /// Moving this factor closer to 0 will increase the amount of accepted samples during temporal accumulation, increasing the ghosting, but reducing the temporal noise.
        /// </summary>
        public ClampedFloatParameter spatialBilateralAggressiveness = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);


        /// <summary>
        /// Whether the results are accumulated over time or not. This can get higher quality results at a cheaper cost, but it can lead to temporal artifacts such as ghosting.
        /// </summary>
        public BoolParameter temporalAccumulation = new BoolParameter(true);

        // Temporal only parameters
        /// <summary>
        /// Moving this factor closer to 0 will increase the amount of accepted samples during temporal accumulation, increasing the ghosting, but reducing the temporal noise.
        /// </summary>
        public ClampedFloatParameter ghostingReduction = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        // Non-temporal only parameters
        /// <summary>
        /// Modify the non-temporal blur to change how sharp features are preserved. Lower values leads to blurrier/softer results, higher values gets a sharper result, but with the risk of noise.
        /// </summary>
        public ClampedFloatParameter blurSharpness = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

        // Ray tracing parameters
        /// <summary>
        /// Defines the layers that ray traced ambient occlusion should include.
        /// </summary>
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Controls the influence of the ambient occlusion on the specular occlusion.
        /// </summary>
        [AdditionalProperty]
        public ClampedFloatParameter specularOcclusion = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the length of ray traced ambient occlusion rays.
        /// </summary>
        public float rayLength
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_RayLength.value;
                else
                    return GetLightingQualitySettings().RTAORayLength[(int)quality.value];
            }
            set { m_RayLength.value = value; }
        }

        /// <summary>
        /// Number of samples for evaluating the effect.
        /// </summary>
        public int sampleCount
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_SampleCount.value;
                else
                    return GetLightingQualitySettings().RTAOSampleCount[(int)quality.value];
            }
            set { m_SampleCount.value = value; }
        }

        /// <summary>
        /// Defines if the ray traced ambient occlusion should be denoised.
        /// </summary>
        public bool denoise
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_Denoise.value;
                else
                    return GetLightingQualitySettings().RTAODenoise[(int)quality.value];
            }
            set { m_Denoise.value = value; }
        }

        /// <summary>
        /// Controls the radius of the ray traced ambient occlusion denoiser.
        /// </summary>
        public float denoiserRadius
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_DenoiserRadius.value;
                else
                    return GetLightingQualitySettings().RTAODenoiserRadius[(int)quality.value];
            }
            set { m_DenoiserRadius.value = value; }
        }

        /// <summary>
        /// Number of steps to take along one signed direction during horizon search (this is the number of steps in positive and negative direction). Increasing the value can lead to detection
        /// of finer details, but is not a guarantee of higher quality otherwise. Also note that increasing this value will lead to higher cost.
        /// </summary>
        public int stepCount
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_StepCount.value;
                else
                    return GetLightingQualitySettings().AOStepCount[(int)quality.value];
            }
            set { m_StepCount.value = value; }
        }

        /// <summary>
        /// If this option is set to true, the effect runs at full resolution. This will increases quality, but also decreases performance significantly.
        /// </summary>
        public bool fullResolution
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_FullResolution.value;
                else
                    return GetLightingQualitySettings().AOFullRes[(int)quality.value];
            }
            set { m_FullResolution.value = value; }
        }

        /// <summary>
        /// This field imposes a maximum radius in pixels that will be considered. It is very important to keep this as tight as possible to preserve good performance.
        /// Note that the pixel value specified for this field is the value used for 1080p when *not* running the effect at full resolution, it will be scaled accordingly
        /// for other resolutions.
        /// </summary>
        public int maximumRadiusInPixels
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_MaximumRadiusInPixels.value;
                else
                    return GetLightingQualitySettings().AOMaximumRadiusPixels[(int)quality.value];
            }
            set { m_MaximumRadiusInPixels.value = value; }
        }

        /// <summary>
        /// This upsample method preserves sharp edges better, however may result in visible aliasing and it is slightly more expensive.
        /// </summary>
        public bool bilateralUpsample
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_BilateralUpsample.value;
                else
                    return GetLightingQualitySettings().AOBilateralUpsample[(int)quality.value];
            }
            set { m_BilateralUpsample.value = value; }
        }

        /// <summary>
        /// Number of directions searched for occlusion at each each pixel when temporal accumulation is disabled.
        /// </summary>
        public int directionCount
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_DirectionCount.value;
                else
                    return GetLightingQualitySettings().AODirectionCount[(int)quality.value];
            }
            set { m_DirectionCount.value = value; }
        }

        /// <summary>
        /// When enabled, ambient occlusion generated by moving objects will not be accumulated, generating less ghosting but introducing additional noise.
        /// </summary>
        [AdditionalProperty]
        public BoolParameter occluderMotionRejection = new BoolParameter(true);

        /// <summary>
        /// When enabled, ambient occlusion generated on moving objects will not be accumulated, generating less ghosting but introducing additional noise.
        /// </summary>
        [AdditionalProperty]
        public BoolParameter receiverMotionRejection = new BoolParameter(true);

        // SSAO
        [SerializeField, FormerlySerializedAs("stepCount")]
        private ClampedIntParameter m_StepCount = new ClampedIntParameter(6, 2, 32);
        [SerializeField, FormerlySerializedAs("fullResolution")]
        private BoolParameter m_FullResolution = new BoolParameter(false);
        [SerializeField, FormerlySerializedAs("maximumRadiusInPixels")]
        private ClampedIntParameter m_MaximumRadiusInPixels = new ClampedIntParameter(40, 16, 256);
        // Temporal only parameter
        [AdditionalProperty]
        [SerializeField, FormerlySerializedAs("bilateralUpsample")]
        private BoolParameter m_BilateralUpsample = new BoolParameter(true);
        // Non-temporal only parameters
        [SerializeField, FormerlySerializedAs("directionCount")]
        private ClampedIntParameter m_DirectionCount = new ClampedIntParameter(2, 1, 6);

        // RTAO
        [SerializeField, FormerlySerializedAs("rayLength")]
        private MinFloatParameter m_RayLength = new MinFloatParameter(50.0f, 0.01f);
        [SerializeField, FormerlySerializedAs("sampleCount")]
        private ClampedIntParameter m_SampleCount = new ClampedIntParameter(1, 1, 64);
        [SerializeField, FormerlySerializedAs("denoise")]
        private BoolParameter m_Denoise = new BoolParameter(true);
        [SerializeField, FormerlySerializedAs("denoiserRadius")]
        private ClampedFloatParameter m_DenoiserRadius = new ClampedFloatParameter(1.0f, 0.001f, 1.0f);
    }
}
