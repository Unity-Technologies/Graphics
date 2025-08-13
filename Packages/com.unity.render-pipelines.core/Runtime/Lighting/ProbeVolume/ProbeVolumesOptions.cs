using System;
using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="APVLeakReductionMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class APVLeakReductionModeParameter : VolumeParameter<APVLeakReductionMode>
    {
        /// <summary>
        /// Creates a new <see cref="APVLeakReductionModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public APVLeakReductionModeParameter(APVLeakReductionMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A volume component that holds settings for the Adaptive Probe Volumes System per-camera options.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Adaptive Probe Volumes Options"), SupportedOnRenderPipeline]
    [CurrentPipelineHelpURL("probevolumes")]
    [DisplayInfo(name = "Adaptive Probe Volumes Options")]
    public sealed class ProbeVolumesOptions : VolumeComponent
    {
        /// <summary>
        /// The overridden normal bias to be applied to the world position when sampling the Adaptive Probe Volumes data structure. Unit is meters.
        /// </summary>
        [Tooltip("The overridden normal bias to be applied to the world position when sampling the Adaptive Probe Volumes data structure. Unit is meters.")]
        public ClampedFloatParameter normalBias = new ClampedFloatParameter(0.05f, 0.0f, 2.0f);

        /// <summary>
        /// A bias alongside the view vector to be applied to the world position when sampling the Adaptive Probe Volumes data structure. Unit is meters.
        /// </summary>
        [Tooltip("A bias alongside the view vector to be applied to the world position when sampling the Adaptive Probe Volumes data structure. Unit is meters.")]
        public ClampedFloatParameter viewBias = new ClampedFloatParameter(0.1f, 0.0f, 2.0f);

        /// <summary>
        /// Whether to scale the bias for Adaptive Probe Volumes by the minimum distance between probes.
        /// </summary>
        [Tooltip("Whether to scale the bias for Adaptive Probe Volumes by the minimum distance between probes.")]
        public BoolParameter scaleBiasWithMinProbeDistance = new BoolParameter(false);

        /// <summary>
        /// Noise to be applied to the sampling position. It can hide seams issues between subdivision levels, but introduces noise.
        /// </summary>
        [Tooltip("Noise to be applied to the sampling position. It can hide seams issues between subdivision levels, but introduces noise.")]
        public ClampedFloatParameter samplingNoise = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);


        /// <summary>
        /// Whether to animate the noise when TAA is enabled, smoothing potentially out the noise pattern introduced.
        /// </summary>
        [Tooltip("Whether to animate the noise when TAA is enabled. It can potentially remove the visible noise patterns.")]
        public BoolParameter animateSamplingNoise = new BoolParameter(true);

        /// <summary>
        /// Method used to reduce leaks.
        /// </summary>
        [Tooltip("Method used to reduce leaks. Currently available modes are crude, but cheap methods.")]
        public APVLeakReductionModeParameter leakReductionMode = new APVLeakReductionModeParameter(APVLeakReductionMode.Quality);

        /// <summary>
        /// This parameter isn't used anymore.
        /// </summary>
        [Obsolete("This parameter isn't used anymore. #from(6000.0)")]
        public ClampedFloatParameter minValidDotProductValue = new ClampedFloatParameter(0.1f, -1.0f, 0.33f);

        /// <summary>
        /// When enabled, reflection probe normalization can only decrease the reflections intensity.
        /// </summary>
        [Tooltip("When enabled, reflection probe normalization can only decrease the reflection intensity.")]
        public BoolParameter occlusionOnlyReflectionNormalization = new BoolParameter(true);

        /// <summary>
        /// Global probe volumes weight. Allows for fading out probe volumes influence falling back to ambient probe.
        /// </summary>
        [AdditionalProperty, Tooltip("Global probe volumes weight. Allows for fading out probe volumes influence falling back to ambient probe.")]
        public ClampedFloatParameter intensityMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Multiplier applied on the sky lighting when using sky occlusion.
        /// </summary>
        [AdditionalProperty, Tooltip("Multiplier applied on the sky lighting when using sky occlusion.")]
        public ClampedFloatParameter skyOcclusionIntensityMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

        /// <summary>
        /// Offset applied at runtime to probe positions in world space.
        /// </summary>
        [AdditionalProperty, Tooltip("Offset applied at runtime to probe positions in world space.\nThis is not considered while baking.")]
        public Vector3Parameter worldOffset = new Vector3Parameter(Vector3.zero);
    }
}
