using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds the general settings for ray traced effects.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Settings (Preview)")]
    public sealed class RayTracingSettings : VolumeComponent
    {
        /// <summary>
        /// Controls the bias for all real-time ray tracing effects.
        /// </summary>
        [Tooltip("Controls the bias for all real-time ray tracing effects.")]
        public ClampedFloatParameter rayBias = new ClampedFloatParameter(0.001f, 0.0f, 0.1f);

        /// <summary>
        /// Enables the override of the shadow culling. This increases the validity area of shadow maps outside of the frustum.
        /// </summary>
        [Tooltip("Enables the override of the shadow culling.")]
        public BoolParameter extendCulling = new BoolParameter(false);
    }
}
