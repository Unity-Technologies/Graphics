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
    }
}
