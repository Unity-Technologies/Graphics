using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Ray Tracing/Settings")]
    public sealed class RayTracingSettings : VolumeComponent
    {
        [Tooltip("Controls the bias for all real-time ray tracing effects.")]
        public ClampedFloatParameter rayBias = new ClampedFloatParameter(0.001f, 0.0f, 0.1f);
    }
}
