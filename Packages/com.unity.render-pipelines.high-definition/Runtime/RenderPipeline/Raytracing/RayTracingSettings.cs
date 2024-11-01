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
    /// A <see cref="VolumeParameter"/> that holds a <see cref="RTASBuildMode"/> value.
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
    /// Controls the culling mode for the ray tracing acceleration structure.
    /// </summary>
    public enum RTASCullingMode
    {
        /// <summary>HDRP automatically extends the camera's frustum when culling for the ray tracing acceleration structure.</summary>
        ExtendedFrustum,
        /// <summary>The user provides the radius of the sphere used to cull objects out of the ray tracing acceleration structure.</summary>
        Sphere,
        /// <summary>HDRP does not perform any culling step on the ray tracing acceleration structure.</summary>
        None,
        /// <summary>The user provides the minimum solid angle relative to the camera position to accept object to the ray tracing acceleration structure.</summary>
        SolidAngle
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="RTASCullingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class RTASCullingModeParameter : VolumeParameter<RTASCullingMode>
    {
        /// <summary>
        /// Creates a new <see cref="RTASCullingModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RTASCullingModeParameter(RTASCullingMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A volume component that holds the general settings for ray traced effects.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Ray Tracing Settings")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Ray-Tracing-Settings")]
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
        public BoolParameter extendShadowCulling = new BoolParameter(true);

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
        [Tooltip("Controls how the ray tracing acceleration structure is build.")]
        [AdditionalProperty]
        public RTASBuildModeParameter buildMode = new RTASBuildModeParameter(RTASBuildMode.Automatic);

        /// <summary>
        /// Specifies the method used for the ray tracing culling.
        /// </summary>
        [Tooltip("Specifies the method used for the ray tracing culling.")]
        [AdditionalProperty]
        public RTASCullingModeParameter cullingMode = new RTASCullingModeParameter(RTASCullingMode.ExtendedFrustum);

        /// <summary>
        /// Specifies the radius of the sphere used to cull objects out of the ray tracing acceleration structure when the culling mode is set to Sphere.
        /// </summary>
        [Tooltip("Specifies the radius of the sphere used to cull objects out of the ray tracing acceleration structure when the culling mode is set to Sphere.")]
        public MinFloatParameter cullingDistance = new MinFloatParameter(1000.0f, 0.01f);

        /// <summary>
        /// Specifies the minimum object solid angle in degrees relative to the camera position to accept objects to the ray tracing acceleration structure when the culling mode is set to Solid Angle.
        /// </summary>
        [Tooltip("Specifies the minimum object solid angle in degrees relative to the camera position to accept objects to the ray tracing acceleration structure when the culling mode is set to Solid Angle.")]
        public ClampedFloatParameter minSolidAngle = new ClampedFloatParameter(4.0f, 0.01f, 180f);

        /// <summary>
        /// Default constructor for the ray tracing settings volume component.
        /// </summary>
        public RayTracingSettings()
        {
            displayName = "Ray Tracing Settings";
        }
    }
}
