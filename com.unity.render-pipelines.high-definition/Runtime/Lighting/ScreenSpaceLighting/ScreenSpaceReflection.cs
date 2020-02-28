using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for screen space reflection and ray traced reflections.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Reflection")]
    public class ScreenSpaceReflection : VolumeComponentWithQuality
    {
        /// <summary>
        /// Enable ray traced reflections.
        /// </summary>
        public BoolParameter rayTracing = new BoolParameter(false);

        // Shared Data
        /// <summary>
        /// Controls the smoothness value at which HDRP activates SSR and the smoothness-controlled fade out stops.
        /// </summary>
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start]
        /// </summary>
        public ClampedFloatParameter smoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

        /// <summary>
        /// When enabled, SSR handles sky reflection.
        /// </summary>
        public BoolParameter reflectSky = new BoolParameter(true);

        // SSR Data
        /// <summary>
        /// Controls the distance at which HDRP fades out SSR near the edge of the screen.
        /// </summary>
        public ClampedFloatParameter depthBufferThickness = new ClampedFloatParameter(0.01f, 0, 1);

        /// <summary>
        /// Controls the typical thickness of objects the reflection rays may pass behind.
        /// </summary>
        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

        /// <summary>
        /// Layer mask used to include the objects for screen space reflection.
        /// </summary>
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Controls the length of reflection rays.
        /// </summary>
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0.001f, 50f);

        /// <summary>
        /// Clamps the exposed intensity.
        /// </summary>
        public ClampedFloatParameter clampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);

        /// <summary>
        /// Enable denoising on the ray traced reflections.
        /// </summary>
        public BoolParameter denoise = new BoolParameter(false);

        /// <summary>
        /// Controls the radius of reflection denoiser.
        /// </summary>
        public ClampedIntParameter denoiserRadius = new ClampedIntParameter(8, 1, 32);

        /// <summary>
        /// Controls which version of the effect should be used.
        /// </summary>
        public RayTracingModeParameter mode = new RayTracingModeParameter(RayTracingMode.Quality);

        // Performance
        /// <summary>
        /// Controls the size of the upscale radius.
        /// </summary>
        public IntParameter upscaleRadius = new ClampedIntParameter(2, 2, 6);
        /// <summary>
        /// Enables full resolution mode.
        /// </summary>
        public BoolParameter fullResolution = new BoolParameter(false);

        // Quality
        /// <summary>
        /// Number of samples for reflections.
        /// </summary>
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 32);
        /// <summary>
        /// Number of bounces for reflection rays.
        /// </summary>
        public ClampedIntParameter bounceCount = new ClampedIntParameter(1, 1, 31);

        /// <summary>
        /// Sets the maximum number of steps HDRP uses for raytracing. Affects both correctness and performance.
        /// </summary>
        public int rayMaxIterations
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_RayMaxIterations.value;
                else
                    return GetLightingQualitySettings().SSRMaxRaySteps[(int)quality.value];
            }
            set { m_RayMaxIterations.value = value; }
        }

        [SerializeField, FormerlySerializedAs("rayMaxIterations")]
        private IntParameter m_RayMaxIterations = new IntParameter(32);
    }
}
