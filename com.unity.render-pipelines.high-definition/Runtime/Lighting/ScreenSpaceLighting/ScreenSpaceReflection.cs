using System;
using System.Diagnostics;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Screen Space Reflection Algorithm
    /// </summary>
    public enum ScreenSpaceReflectionAlgorithm
    {
        /// <summary>Legacy SSR approximation.</summary>
        Approximation,
        /// <summary>Screen Space Reflection, Physically Based with Accumulation through multiple frame.</summary>
        PBRAccumulation
    }

    /// <summary>
    /// Screen Space Reflection Algorithm Type volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SSRAlgoParameter : VolumeParameter<ScreenSpaceReflectionAlgorithm>
    {
        /// <summary>
        /// Screen Space Reflection Algorithm Type volume parameter constructor.
        /// </summary>
        /// <param name="value">SSR Algo Type parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public SSRAlgoParameter(ScreenSpaceReflectionAlgorithm value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// A volume component that holds settings for screen space reflection and ray traced reflections.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Reflection")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Override-Screen-Space-Reflection" + Documentation.endURL)]
    public class ScreenSpaceReflection : VolumeComponentWithQuality
    {
        bool UsesRayTracingQualityMode()
        {
            // The default value is set to quality. So we should be in quality if not overriden or we have an override set to quality
            return !mode.overrideState || mode == RayTracingMode.Quality;
        }

        bool UsesRayTracing()
        {
            var hdAsset = HDRenderPipeline.currentAsset;
            return hdAsset != null && hdAsset.currentPlatformRenderPipelineSettings.supportRayTracing && rayTracing.overrideState && rayTracing.value;
        }

        /// <summary>Enable Screen Space Reflections.</summary>
        [Tooltip("Enable Screen Space Reflections.")]
        public BoolParameter enabled = new BoolParameter(true);

        /// <summary>Screen Space Reflections Algorithm used.</summary>
        public SSRAlgoParameter usedAlgorithm = new SSRAlgoParameter(ScreenSpaceReflectionAlgorithm.Approximation);

        /// <summary>
        /// Enable ray traced reflections.
        /// </summary>
        public BoolParameter rayTracing = new BoolParameter(false);

        // Shared Data
        /// <summary>
        /// Controls the smoothness value at which HDRP activates SSR and the smoothness-controlled fade out stops.
        /// </summary>
        public float minSmoothness
        {
            get
            {
                if ((UsesRayTracing() && (UsesRayTracingQualityMode() || !UsesQualitySettings())) || !UsesRayTracing())
                    return m_MinSmoothness.value;
                else
                    return GetLightingQualitySettings().RTRMinSmoothness[(int)quality.value];
            }
            set { m_MinSmoothness.value = value; }
        }

        /// <summary>
        /// Controls the smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start]
        /// </summary>
        public float smoothnessFadeStart
        {
            get
            {
                if ((UsesRayTracing() && (UsesRayTracingQualityMode() || !UsesQualitySettings())) || !UsesRayTracing())
                    return m_SmoothnessFadeStart.value;
                else
                    return GetLightingQualitySettings().RTRSmoothnessFadeStart[(int)quality.value];
            }
            set { m_SmoothnessFadeStart.value = value; }
        }

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
        /// Controls the amount of accumulation (0 no accumulation, 1 just accumulate)
        /// </summary>
        public ClampedFloatParameter accumulationFactor = new ClampedFloatParameter(0.75f, 0.0f, 1.0f);

        /// <summary>
        /// Layer mask used to include the objects for screen space reflection.
        /// </summary>
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Controls the length of reflection rays.
        /// </summary>
        public float rayLength
        {
            get
            {
                if (!UsesQualitySettings() || UsesRayTracingQualityMode())
                    return m_RayLength.value;
                else
                    return GetLightingQualitySettings().RTRRayLength[(int)quality.value];
            }
            set { m_RayLength.value = value; }
        }

        /// <summary>
        /// Clamps the exposed intensity.
        /// </summary>
        public float clampValue
        {
            get
            {
                if (!UsesQualitySettings() || UsesRayTracingQualityMode())
                    return m_ClampValue.value;
                else
                    return GetLightingQualitySettings().RTRClampValue[(int)quality.value];
            }
            set { m_ClampValue.value = value; }
        }

        /// <summary>
        /// Enable denoising on the ray traced reflections.
        /// </summary>
        public bool denoise
        {
            get
            {
                if (!UsesQualitySettings() || UsesRayTracingQualityMode())
                    return m_Denoise.value;
                else
                    return GetLightingQualitySettings().RTRDenoise[(int)quality.value];
            }
            set { m_Denoise.value = value; }
        }

        /// <summary>
        /// Controls the radius of reflection denoiser.
        /// </summary>
        public int denoiserRadius
        {
            get
            {
                if (!UsesQualitySettings() || UsesRayTracingQualityMode())
                    return m_DenoiserRadius.value;
                else
                    return GetLightingQualitySettings().RTRDenoiserRadius[(int)quality.value];
            }
            set { m_DenoiserRadius.value = value; }
        }
        /// <summary>
        /// Controls which version of the effect should be used.
        /// </summary>
        public RayTracingModeParameter mode = new RayTracingModeParameter(RayTracingMode.Quality);

        // Performance
        /// <summary>
        /// Controls the size of the upscale radius.
        /// </summary>
        public int upscaleRadius
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_UpscaleRadius.value;
                else
                    return GetLightingQualitySettings().RTRUpScaleRadius[(int)quality.value];
            }
            set { m_UpscaleRadius.value = value; }
        }

        /// <summary>
        /// Defines if the effect should be evaluated at full resolution.
        /// </summary>
        public bool fullResolution
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_FullResolution.value;
                else
                    return GetLightingQualitySettings().RTRFullResolution[(int)quality.value];
            }
            set { m_FullResolution.value = value; }
        }


        // Quality
        /// <summary>
        /// Number of samples for reflections.
        /// </summary>
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 32);
        /// <summary>
        /// Number of bounces for reflection rays.
        /// </summary>
        public ClampedIntParameter bounceCount = new ClampedIntParameter(1, 1, 8);

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

        [SerializeField, FormerlySerializedAs("minSmoothness")]
        private ClampedFloatParameter m_MinSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

        [SerializeField, FormerlySerializedAs("smoothnessFadeStart")]
        private ClampedFloatParameter m_SmoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

        [SerializeField, FormerlySerializedAs("rayMaxIterations")]
        private IntParameter m_RayMaxIterations = new IntParameter(32);

        [SerializeField, FormerlySerializedAs("rayLength")]
        private MinFloatParameter m_RayLength = new MinFloatParameter(50.0f, 0.01f);

        [SerializeField, FormerlySerializedAs("clampValue")]
        [Tooltip("Controls the clamp of intensity.")]
        private ClampedFloatParameter m_ClampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);

        [SerializeField, FormerlySerializedAs("upscaleRadius")]
        [Tooltip("Upscale Radius")]
        private ClampedIntParameter m_UpscaleRadius = new ClampedIntParameter(2, 2, 6);

        [SerializeField, FormerlySerializedAs("fullResolution")]
        [Tooltip("Full Resolution")]
        private BoolParameter m_FullResolution = new BoolParameter(false);

        [SerializeField, FormerlySerializedAs("denoise")]
        [Tooltip("Denoise the ray-traced reflection.")]
        private BoolParameter m_Denoise = new BoolParameter(true);

        [SerializeField, FormerlySerializedAs("denoiserRadius")]
        [Tooltip("Controls the radius of the ray traced reflection denoiser.")]
        private ClampedIntParameter m_DenoiserRadius = new ClampedIntParameter(8, 1, 32);
    }
}
