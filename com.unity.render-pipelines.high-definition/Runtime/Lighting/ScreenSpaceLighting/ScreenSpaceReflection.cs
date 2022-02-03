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
    [Serializable, VolumeComponentMenuForRenderPipeline("Lighting/Screen Space Reflection", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Screen-Space-Reflection")]
    public class ScreenSpaceReflection : VolumeComponentWithQuality
    {
        bool UsesRayTracingQualityMode()
        {
            // The default value is set to quality. So we should be in quality if not overriden or we have an override set to quality
            return (tracing.overrideState && tracing == RayCastingMode.RayTracing && (!mode.overrideState || (mode.overrideState && mode == RayTracingMode.Quality)));
        }

        bool UsesRayTracing()
        {
            var hdAsset = HDRenderPipeline.currentAsset;
            return hdAsset != null && hdAsset.currentPlatformRenderPipelineSettings.supportRayTracing
                && tracing.overrideState && tracing.value != RayCastingMode.RayMarching;
        }

        #region General
        /// <summary>Enable Screen Space Reflections.</summary>
        [Tooltip("Enable Screen Space Reflections.")]
        public BoolParameter enabled = new BoolParameter(true);

        /// <summary>Enable Transparent Screen Space Reflections.</summary>
        [Tooltip("Enable Transparent Screen Space Reflections.")]
        public BoolParameter enabledTransparent = new BoolParameter(true);

        /// <summary>
        /// </summary>
        [Tooltip("Controls the casting technique used to evaluate the effect.")]
        public RayCastingModeParameter tracing = new RayCastingModeParameter(RayCastingMode.RayMarching);

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
        [SerializeField, FormerlySerializedAs("minSmoothness")]
        private ClampedFloatParameter m_MinSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

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
        [SerializeField, FormerlySerializedAs("smoothnessFadeStart")]
        private ClampedFloatParameter m_SmoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        #endregion

        #region Ray Marching
        /// <summary>
        /// When enabled, SSR handles sky reflection.
        /// </summary>
        public BoolParameter reflectSky = new BoolParameter(true);

        /// <summary>Screen Space Reflections Algorithm used.</summary>
        public SSRAlgoParameter usedAlgorithm = new SSRAlgoParameter(ScreenSpaceReflectionAlgorithm.Approximation);

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
        private MinIntParameter m_RayMaxIterations = new MinIntParameter(64, 0);
        #endregion

        #region Ray Tracing
        /// <summary>
        /// Controls which sources are used to fallback on when the traced ray misses.
        /// </summary>
        [FormerlySerializedAs("fallbackHierachy")]
        [AdditionalProperty]
        public RayTracingFallbackHierachyParameter rayMiss = new RayTracingFallbackHierachyParameter(RayTracingFallbackHierachy.ReflectionProbesAndSky);

        /// <summary>
        /// Controls the fallback hierarchy for lighting the last bounce.
        /// </summary>
        [AdditionalProperty]
        public RayTracingFallbackHierachyParameter lastBounceFallbackHierarchy = new RayTracingFallbackHierachyParameter(RayTracingFallbackHierachy.ReflectionProbesAndSky);

        /// <summary>
        /// Layer mask used to include the objects for screen space reflection.
        /// </summary>
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Defines the LOD Bias for sampling all the textures.
        /// </summary>
        public ClampedIntParameter textureLodBias = new ClampedIntParameter(1, 0, 7);

        /// <summary>
        /// Controls the length of reflection rays in meters.
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
        [SerializeField, FormerlySerializedAs("rayLength")]
        private MinFloatParameter m_RayLength = new MinFloatParameter(50.0f, 0.01f);

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
        [SerializeField, FormerlySerializedAs("clampValue")]
        [Tooltip("Controls the clamp of intensity.")]
        private ClampedFloatParameter m_ClampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);

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
        [SerializeField, FormerlySerializedAs("denoise")]
        [Tooltip("Denoise the ray-traced reflection.")]
        private BoolParameter m_Denoise = new BoolParameter(true);

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
        [SerializeField, FormerlySerializedAs("denoiserRadius")]
        [Tooltip("Controls the radius of the ray traced reflection denoiser.")]
        private ClampedIntParameter m_DenoiserRadius = new ClampedIntParameter(8, 1, 32);

        /// <summary>
        /// Controls if the denoising should affect pefectly smooth surfaces
        /// </summary>
        public bool affectSmoothSurfaces
        {
            get
            {
                if (!UsesQualitySettings() || UsesRayTracingQualityMode())
                    return m_AffectSmoothSurfaces.value;
                else
                    return GetLightingQualitySettings().RTRSmoothDenoising[(int)quality.value];
            }
            set { m_AffectSmoothSurfaces.value = value; }
        }
        [SerializeField]
        [Tooltip("Denoiser affects smooth surfaces.")]
        private BoolParameter m_AffectSmoothSurfaces = new BoolParameter(false);

        /// <summary>
        /// Controls which version of the effect should be used.
        /// </summary>
        public RayTracingModeParameter mode = new RayTracingModeParameter(RayTracingMode.Quality);

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
        [SerializeField, FormerlySerializedAs("fullResolution")]
        [Tooltip("Full Resolution")]
        private BoolParameter m_FullResolution = new BoolParameter(false);

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
        /// Sets the maximum number of steps HDRP uses for mixed tracing. Affects both correctness and performance.
        /// </summary>
        public int rayMaxIterationsRT
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_RayMaxIterationsRT.value;
                else
                    return GetLightingQualitySettings().RTRRayMaxIterations[(int)quality.value];
            }
            set { m_RayMaxIterationsRT.value = value; }
        }

        [SerializeField, FormerlySerializedAs("rayMaxIterations")]
        private MinIntParameter m_RayMaxIterationsRT = new MinIntParameter(48, 0);
        #endregion

        internal static bool RayTracingActive(ScreenSpaceReflection volume)
        {
            return volume.tracing.value != RayCastingMode.RayMarching;
        }
    }
}
