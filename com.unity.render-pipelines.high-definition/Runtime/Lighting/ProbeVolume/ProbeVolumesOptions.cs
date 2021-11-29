using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering
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
    /// A volume component that holds settings for the Probe Volumes System per-camera options.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Lighting/Probe Volumes Options (Experimental)", typeof(HDRenderPipeline))]
    public sealed class ProbeVolumesOptions : VolumeComponent
    {
        /// <summary>
        /// The overridden normal bias to be applied to the world position when sampling the Probe Volumes data structure. Unit is meters.
        /// </summary>
        [Tooltip("The overridden normal bias to be applied to the world position when sampling the Probe Volumes data structure. Unit is meters.")]
        public ClampedFloatParameter normalBias = new ClampedFloatParameter(0.33f, 0.0f, 2.0f);

        /// <summary>
        /// A bias alongside the view vector to be applied to the world position when sampling the Probe Volumes data structure. Unit is meters.
        /// </summary>
        [AdditionalProperty, Tooltip("A bias alongside the view vector to be applied to the world position when sampling the Probe Volumes data structure. Unit is meters.")]
        public ClampedFloatParameter viewBias = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Whether to scale the bias for Probe Volumes by the minimum distance between probes.
        /// </summary>
        [AdditionalProperty, Tooltip("Whether to scale the bias for Probe Volumes by the minimum distance between probes.")]
        public BoolParameter scaleBiasWithMinProbeDistance = new BoolParameter(false);

        /// <summary>
        /// Noise to be applied to the sampling position. It can hide seams issues between subdivision levels, but introduces noise.
        /// </summary>
        [AdditionalProperty, Tooltip("Noise to be applied to the sampling position. It can hide seams issues between subdivision levels, but introduces noise.")]
        public ClampedFloatParameter samplingNoise = new ClampedFloatParameter(0.125f, 0.0f, 1.0f);

        /// <summary>
        /// Method used to reduce leaks. Validity mode will only find occlusions when a probe is inside an object. Will provide a crude reduction only in some cases.
        /// </summary>
        [AdditionalProperty, Tooltip("Method used to reduce leaks. Validity mode will only find occlusions when a probe is inside an object. Will provide a crude reduction only in some cases.")]
        public APVLeakReductionModeParameter leakReductionMode = new APVLeakReductionModeParameter(APVLeakReductionMode.None);
    }
}
