using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ray traced global illumination.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Global Illumination (Preview)")]
    public sealed class GlobalIllumination : VolumeComponent
    {
        /// <summary>
        /// Enable ray traced global illumination.
        /// </summary>
        [Tooltip("Enable ray traced global illumination.")]
        public BoolParameter rayTracing = new BoolParameter(false);

        /// <summary>
        /// Defines the layers that GI should include.
        /// </summary>
        [Tooltip("Defines the layers that GI should include.")]
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Controls the length of GI rays.
        /// </summary>
        [Tooltip("Controls the length of GI rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0.001f, 50f);

        /// <summary>
        /// Controls the clamp of intensity.
        /// </summary>
        [Tooltip("Controls the clamp of intensity.")]
        public ClampedFloatParameter clampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);

        /// <summary>
        /// Controls which version of the effect should be used.
        /// </summary>
        [Tooltip("Controls which version of the effect should be used.")]
        public RayTracingModeParameter mode = new RayTracingModeParameter(RayTracingMode.Quality);

        // Performance
        /// <summary>
        /// Defines if the effect should be evaluated at full resolution.
        /// </summary>
        [Tooltip("Full Resolution")]
        public BoolParameter fullResolution = new BoolParameter(false);

        /// <summary>
        /// Defines what radius value should be used to pre-filter the signal.
        /// </summary>
        [Tooltip("Upscale Radius")]
        public ClampedIntParameter upscaleRadius = new ClampedIntParameter(2, 2, 4);

        // Quality
        /// <summary>
        /// Number of samples for evaluating the effect.
        /// </summary>
        [Tooltip("Number of samples for GI.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 32);

        /// <summary>
        /// Number of bounces for evaluating the effect.
        /// </summary>
        [Tooltip("Number of bounces for GI.")]
        public ClampedIntParameter bounceCount = new ClampedIntParameter(1, 1, 31);

        // Filtering
        /// <summary>
        /// Defines if the ray traced global illumination should be denoised.
        /// </summary>
        [Tooltip("Denoise the ray-traced GI.")]
        public BoolParameter denoise = new BoolParameter(false);

        /// <summary>
        /// Defines if the denoiser should be evaluated at half resolution.
        /// </summary>
        [Tooltip("Use a half resolution denoiser.")]
        public BoolParameter halfResolutionDenoiser = new BoolParameter(false);

        /// <summary>
        /// Controls the radius of the global illumination denoiser (First Pass).
        /// </summary>
        [Tooltip("Controls the radius of the GI denoiser (First Pass).")]
        public ClampedFloatParameter denoiserRadius = new ClampedFloatParameter(0.6f, 0.001f, 1.0f);

        /// <summary>
        /// Defines if the second denoising pass should be enabled.
        /// </summary>
        [Tooltip("Enable second denoising pass.")]
        public BoolParameter secondDenoiserPass = new BoolParameter(false);

        /// <summary>
        /// Controls the radius of the global illumination denoiser (Second Pass).
        /// </summary>
        [Tooltip("Controls the radius of the GI denoiser (Second Pass).")]
        public ClampedFloatParameter secondDenoiserRadius = new ClampedFloatParameter(0.3f, 0.001f, 0.5f);
    }
}
