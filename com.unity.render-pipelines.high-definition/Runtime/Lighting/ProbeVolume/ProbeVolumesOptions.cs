using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// A volume component that holds settings for the Probe Volumes System per-camera options.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Probe Volumes Options (Experimental)")]
    public sealed class ProbeVolumesOptions : VolumeComponent
    {
        /// <summary>
        /// The overridden normal bias to be applied to the world position when sampling the Probe Volumes data structure. Units is meters.
        /// </summary>
        [Tooltip("The overridden normal bias to be applied to the world position when sampling the Probe Volumes data structure. Units is meters.")]
        public ClampedFloatParameter normalBias = new ClampedFloatParameter(0.33f, 0.0f, 2.0f);

        /// <summary>
        /// A bias alongside the view vector to be applied to the world position when sampling the Probe Volumes data structure. Units is meters
        /// </summary>
        [AdditionalProperty, Tooltip("A bias alongside the view vector to be applied to the world position when sampling the Probe Volumes data structure. Units is meters.")]
        public ClampedFloatParameter viewBias = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Whether the scale the bias for Probe Volumes by the minimum distance between probes.
        /// </summary>
        [AdditionalProperty, Tooltip("Whether the scale the bias for Probe Volumes by the minimum distance between probes.")]
        public BoolParameter scaleBiasWithMinProbeDistance = new BoolParameter(false);

        /// <summary>
        /// Noise to be applied to the sampling position. It can hide seams issues between subdivision levels, but introduces noise.
        /// </summary>
        [AdditionalProperty, Tooltip("Noise to be applied to the sampling position. It can hide seams issues between subdivision levels, but introduces noise.")]
        public ClampedFloatParameter samplingNoise = new ClampedFloatParameter(0.125f, 0.0f, 1.0f);
    }
}
