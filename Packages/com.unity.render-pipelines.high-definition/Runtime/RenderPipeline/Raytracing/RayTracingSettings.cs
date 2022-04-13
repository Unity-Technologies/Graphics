using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Control the ray tracing acceleration structure build mode
    /// </summary>
    public enum RTASBuildMode
    {
        /// <summary>HDRP automatically collects mesh renderers and builds the ray tracing acceleration structure every frame</summary>
        Automatic,
        /// <summary>Uses a ray tracing acceleration structure handeled by the user.</summary>
        Manual
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="RTASMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class RTASBuildModeParameter : VolumeParameter<RTASBuildMode>
    {
        /// <summary>
        /// Creates a new <see cref="RTASBuildModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RTASBuildModeParameter(RTASBuildMode value, bool overrideState = false) : base(value, overrideState) { }
    }

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
        /// Controls the fallback directional shadow value that is used when the point to shade is outside of the cascade.
        /// </summary>
        [Tooltip("Controls the fallback directional shadow value that is used when the point to shade is outside of the cascade.")]
        public ClampedFloatParameter directionalShadowFallbackIntensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Controls how the ray tracing acceleration structure is build.
        /// </summary>
        [AdditionalProperty]
        public RTASBuildModeParameter buildMode = new RTASBuildModeParameter(RTASBuildMode.Automatic);

        /// <summary>
        /// Default constructor for the ray tracing settings volume component.
        /// </summary>
        public RayTracingSettings()
        {
            displayName = "Ray Tracing Settings (Preview)";
        }
    }
}
