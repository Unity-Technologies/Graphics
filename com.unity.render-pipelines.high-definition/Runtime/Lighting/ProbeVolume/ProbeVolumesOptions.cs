using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering
{

    public enum LeakingPreventionMethod
    {
        None = 0,
        Validity = 1,
        Geometric = 2,
        Octahedral = 3
    }


    [Serializable]
    public sealed class LeakingPreventionMethodParameter : VolumeParameter<LeakingPreventionMethod>
    {
        /// <summary>
        /// Creates a new <see cref="LeakingPreventionMethod"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public LeakingPreventionMethodParameter(LeakingPreventionMethod value, bool overrideState = false) : base(value, overrideState) { }
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


        //
        public LeakingPreventionMethodParameter antiLeakMode = new LeakingPreventionMethodParameter(LeakingPreventionMethod.None);
        public ClampedFloatParameter leakWeightContrib = new ClampedFloatParameter(1, 0, 1);
        public ClampedIntParameter neighbourIndices = new ClampedIntParameter(0, 0, 7);
        public BoolParameter debug = new BoolParameter(false);
    }
}
