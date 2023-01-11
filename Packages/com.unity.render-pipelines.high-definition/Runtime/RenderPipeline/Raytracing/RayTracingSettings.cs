using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds the general settings for ray traced effects.
    /// </summary>
    [HDRPHelpURLAttribute("Ray-Tracing-Settings")]
    [Serializable, VolumeComponentMenuForRenderPipeline("Ray Tracing/Ray Tracing Settings (Preview)", typeof(HDRenderPipeline))]
    public sealed class RayTracingSettings : VolumeComponent
    {
        /// <summary>
        /// Controls the bias for all real-time ray tracing effects.
        /// </summary>
        [Tooltip("Controls the bias for all real-time ray tracing effects.")]
        public ClampedFloatParameter rayBias = new ClampedFloatParameter(0.001f, 0.0f, 0.1f);

        /// <summary>
        /// Controls the Ray Bias value used when the distance between the pixel and the camera is close to the far plane. Between the near and far plane the Ray Bias and Distant Ray Bias are interpolated linearly. This does not affect Path Tracing or Recursive Rendering. This value can be increased to mitigate Ray Tracing z-fighting issues at a distance.
        /// </summary>
        [Tooltip("Controls the Ray Bias value used when the distance between the pixel and the camera is close to the far plane. Between the near and far plane the Ray Bias and Distant Ray Bias are interpolated linearly. This does not affect Path Tracing or Recursive Rendering. This value can be increased to mitigate Ray Tracing z-fighting issues at a distance.")]
        public ClampedFloatParameter distantRayBias = new ClampedFloatParameter(0.001f, 0.0f, 0.1f);

        /// <summary>
        /// When enabled, the culling region for punctual and area lights shadow maps is increased from frustum culling to extended culling. For Directional lights, cascades are not extended, but additional objects may appear in the cascades.
        /// </summary>
        [Tooltip("When enabled, the culling region for punctual and area lights shadow maps is increased from frustum culling to extended culling. For Directional lights, cascades are not extended, but additional objects may appear in the cascades.")]
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
        /// Controls the fallback directional shadow value that is used when the point to shade is outside of the cascade.
        /// </summary>
        [Tooltip("Controls the fallback directional shadow value that is used when the point to shade is outside of the cascade.")]
        public ClampedFloatParameter directionalShadowFallbackIntensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Default constructor for the ray tracing settings volume component.
        /// </summary>
        public RayTracingSettings()
        {
            displayName = "Ray Tracing Settings (Preview)";
        }
    }
}
