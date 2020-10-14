using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds the general settings for ray traced effects.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Ray Tracing Settings (Preview)")]
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
        [Tooltip("Enables the override of the shadow culling. This increases the validity area of shadow maps outside of the frustum.")]
        [FormerlySerializedAs("extendCulling")]
        public BoolParameter extendShadowCulling = new BoolParameter(false);

        /// <summary>
        /// Enables the override of the camera culling. This increases the validity area of animated skinned mesh that are outside of the frustum..
        /// </summary>
        [Tooltip("Enables the override of the camera culling. This increases the validity area of animated skinned mesh that are outside of the frustum.")]
        public BoolParameter extendCameraCulling = new BoolParameter(false);

        /// <summary>
        /// Controls the maximal ray length for ray traced shadows.
        /// </summary>
        [Tooltip("Controls the maximal ray length for ray traced directional shadows.")]
        public MinFloatParameter directionalShadowRayLength = new MinFloatParameter(1000.0f, 0.01f);

        /// <summary>
        /// Default constructor for the ray tracing settings volume component.
        /// </summary>
        public RayTracingSettings()
        {
            displayName = "Ray Tracing Settings (Preview)";
        }
    }
}

