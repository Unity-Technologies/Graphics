using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the global illumination (screen space and ray traced).
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Lighting/Screen Space Global Illumination", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Ray-Traced-Global-Illumination")]
    public sealed class GlobalIllumination : VolumeComponentWithQuality
    {
        bool UsesQualityMode()
        {
            // The default value is set to quality. So we should be in quality if not overriden or we have an override set to quality
            return (tracing.overrideState && tracing == RayCastingMode.RayTracing && (!mode.overrideState || (mode.overrideState && mode == RayTracingMode.Quality)));
        }

        #region General
        /// <summary>
        /// Enable screen space global illumination.
        /// </summary>
        [Tooltip("Enable screen space global illumination.")]
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);

        /// <summary>
        /// </summary>
        [Tooltip("Controls the casting technique used to evaluate the effect. Ray marching uses a ray-marched screen-space solution, Ray tracing uses a hardware accelerated world-space solution. Mixed uses first Ray marching, then Ray tracing if it fails to intersect on-screen geometry.")]
        public RayCastingModeParameter tracing = new RayCastingModeParameter(RayCastingMode.RayMarching);

        /// <summary>
        /// Controls the fallback hierarchy for indirect diffuse in case the ray misses.
        /// </summary>
        [Tooltip("Controls the fallback hierarchy for indirect diffuse in case the ray misses.")]
        [FormerlySerializedAs("fallbackHierarchy")]
        [AdditionalProperty]
        public RayMarchingFallbackHierarchyParameter rayMiss = new RayMarchingFallbackHierarchyParameter(RayMarchingFallbackHierarchy.ReflectionProbesAndSky);
        #endregion

        #region RayMarching
        /// <summary>
        /// The thickness of the depth buffer value used for the ray marching step
        /// </summary>
        [Tooltip("Controls the thickness of the depth buffer used for ray marching.")]
        public ClampedFloatParameter depthBufferThickness = new ClampedFloatParameter(0.1f, 0.0f, 0.5f);

        GlobalIllumination()
        {
            displayName = "Screen Space Global Illumination";
        }

        /// <summary>
        /// Defines if the screen space global illumination should be evaluated at full resolution.
        /// </summary>
        public BoolParameter fullResolutionSS = new BoolParameter(true);

        /// <summary>
        /// The number of steps that should be used during the ray marching pass.
        /// </summary>
        public int maxRaySteps
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_MaxRaySteps.value;
                else
                    return GetLightingQualitySettings().SSGIRaySteps[(int)quality.value];
            }
            set { m_MaxRaySteps.value = value; }
        }
        [SerializeField]
        [Tooltip("Controls the number of steps used for ray marching.")]
        private MinIntParameter m_MaxRaySteps = new MinIntParameter(32, 0);

        // Filtering
        /// <summary>
        /// Defines if the screen space global illumination should be denoised.
        /// </summary>
        public bool denoiseSS
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_DenoiseSS.value;
                else
                    return GetLightingQualitySettings().SSGIDenoise[(int)quality.value];
            }
            set { m_DenoiseSS.value = value; }
        }
        [SerializeField, FormerlySerializedAs("denoise")]
        private BoolParameter m_DenoiseSS = new BoolParameter(true);

        /// <summary>
        /// Defines if the denoiser should be evaluated at half resolution.
        /// </summary>
        public bool halfResolutionDenoiserSS
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_HalfResolutionDenoiserSS.value;
                else
                    return GetLightingQualitySettings().SSGIHalfResDenoise[(int)quality.value];
            }
            set { m_HalfResolutionDenoiserSS.value = value; }
        }
        [SerializeField]
        [Tooltip("Use a half resolution denoiser.")]
        private BoolParameter m_HalfResolutionDenoiserSS = new BoolParameter(false);

        /// <summary>
        /// Controls the radius of the global illumination denoiser (First Pass).
        /// </summary>
        public float denoiserRadiusSS
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_DenoiserRadiusSS.value;
                else
                    return GetLightingQualitySettings().SSGIDenoiserRadius[(int)quality.value];
            }
            set { m_DenoiserRadiusSS.value = value; }
        }
        [SerializeField]
        [Tooltip("Controls the radius of the GI denoiser (First Pass).")]
        private ClampedFloatParameter m_DenoiserRadiusSS = new ClampedFloatParameter(0.6f, 0.001f, 1.0f);

        /// <summary>
        /// Defines if the second denoising pass should be enabled.
        /// </summary>
        public bool secondDenoiserPassSS
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_SecondDenoiserPassSS.value;
                else
                    return GetLightingQualitySettings().SSGISecondDenoise[(int)quality.value];
            }
            set { m_SecondDenoiserPassSS.value = value; }
        }
        [SerializeField]
        [Tooltip("Enable second denoising pass.")]
        private BoolParameter m_SecondDenoiserPassSS = new BoolParameter(true);

        #endregion

        #region RayTracing
        /// <summary>
        /// Controls the fallback hierarchy for lighting the last bounce.
        /// </summary>
        [Tooltip("Controls the fallback hierarchy for lighting the last bounce.")]
        [AdditionalProperty]
        public RayMarchingFallbackHierarchyParameter lastBounceFallbackHierarchy = new RayMarchingFallbackHierarchyParameter(RayMarchingFallbackHierarchy.ReflectionProbesAndSky);

        /// <summary>
        /// Controls the dimmer applied to the ambient and legacy light probes.
        /// </summary>
        [Tooltip("Controls the dimmer applied to the ambient and legacy light probes.")]
        [AdditionalProperty]
        public ClampedFloatParameter ambientProbeDimmer = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Defines the layers that GI should include.
        /// </summary>
        [Tooltip("Defines the layers that GI should include.")]
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// The LOD Bias HDRP applies to textures in the global illumination.
        /// </summary>
        [Tooltip("The LOD Bias HDRP applies to textures in the global illumination. A higher value increases performance and makes denoising easier, but it might reduce visual fidelity.")]
        public ClampedIntParameter textureLodBias = new ClampedIntParameter(7, 0, 7);

        /// <summary>
        /// Controls the length of GI rays in meters.
        /// </summary>
        public float rayLength
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_RayLength.value;
                else
                    return GetLightingQualitySettings().RTGIRayLength[(int)quality.value];
            }
            set { m_RayLength.value = value; }
        }
        [SerializeField, FormerlySerializedAs("rayLength")]
        private MinFloatParameter m_RayLength = new MinFloatParameter(50.0f, 0.01f);

        /// <summary>
        /// Controls the clamp of intensity.
        /// </summary>
        public float clampValue
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_ClampValue.value;
                else
                    return GetLightingQualitySettings().RTGIClampValue[(int)quality.value];
            }
            set { m_ClampValue.value = value; }
        }
        [SerializeField, FormerlySerializedAs("clampValue")]
        [Tooltip("Controls the clamp of intensity.")]
        private ClampedFloatParameter m_ClampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);

        /// <summary>
        /// Controls which version of the effect should be used.
        /// </summary>
        [Tooltip("Controls which version of the effect should be used.")]
        public RayTracingModeParameter mode = new RayTracingModeParameter(RayTracingMode.Quality);

        // Performance
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
                    return GetLightingQualitySettings().RTGIFullResolution[(int)quality.value];
            }
            set { m_FullResolution.value = value; }
        }
        [SerializeField, FormerlySerializedAs("fullResolution")]
        [Tooltip("Full Resolution")]
        private BoolParameter m_FullResolution = new BoolParameter(false);

        // Quality
        /// <summary>
        /// Number of samples for evaluating the effect.
        /// </summary>
        [Tooltip("Number of samples for GI.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(2, 1, 32);

        /// <summary>
        /// Number of bounces for evaluating the effect.
        /// </summary>
        [Tooltip("Number of bounces for GI.")]
        public ClampedIntParameter bounceCount = new ClampedIntParameter(1, 1, 8);

        // Filtering
        /// <summary>
        /// Defines if the ray traced global illumination should be denoised.
        /// </summary>
        public bool denoise
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_Denoise.value;
                else
                    return GetLightingQualitySettings().RTGIDenoise[(int)quality.value];
            }
            set { m_Denoise.value = value; }
        }
        [SerializeField, FormerlySerializedAs("denoise")]
        [Tooltip("Denoise the ray-traced GI.")]
        private BoolParameter m_Denoise = new BoolParameter(true);

        /// <summary>
        /// Defines if the denoiser should be evaluated at half resolution.
        /// </summary>
        public bool halfResolutionDenoiser
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_HalfResolutionDenoiser.value;
                else
                    return GetLightingQualitySettings().RTGIHalfResDenoise[(int)quality.value];
            }
            set { m_HalfResolutionDenoiser.value = value; }
        }
        [SerializeField, FormerlySerializedAs("halfResolutionDenoiser")]
        [Tooltip("Use a half resolution denoiser.")]
        private BoolParameter m_HalfResolutionDenoiser = new BoolParameter(false);

        /// <summary>
        /// Controls the radius of the global illumination denoiser (First Pass).
        /// </summary>
        public float denoiserRadius
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_DenoiserRadius.value;
                else
                    return GetLightingQualitySettings().RTGIDenoiserRadius[(int)quality.value];
            }
            set { m_DenoiserRadius.value = value; }
        }
        [SerializeField, FormerlySerializedAs("denoiserRadius")]
        [Tooltip("Controls the radius of the GI denoiser (First Pass).")]
        private ClampedFloatParameter m_DenoiserRadius = new ClampedFloatParameter(0.6f, 0.001f, 1.0f);

        /// <summary>
        /// Defines if the second denoising pass should be enabled.
        /// </summary>
        public bool secondDenoiserPass
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_SecondDenoiserPass.value;
                else
                    return GetLightingQualitySettings().RTGISecondDenoise[(int)quality.value];
            }
            set { m_SecondDenoiserPass.value = value; }
        }
        [SerializeField, FormerlySerializedAs("secondDenoiserPass")]
        [Tooltip("Enable second denoising pass.")]
        private BoolParameter m_SecondDenoiserPass = new BoolParameter(true);

        /// <summary>
        /// Controls the number of steps used for the mixed tracing
        /// </summary>
        public int maxMixedRaySteps
        {
            get
            {
                if (!UsesQualitySettings() || UsesQualityMode())
                    return m_MaxMixedRaySteps.value;
                else
                    return GetLightingQualitySettings().RTGIRaySteps[(int)quality.value];
            }
            set { m_MaxMixedRaySteps.value = value; }
        }
        [SerializeField]
        [Tooltip("Controls the number of steps HDRP uses for mixed tracing.")]
        private MinIntParameter m_MaxMixedRaySteps = new MinIntParameter(48, 0);

        /// <summary>
        /// When enabled, global illumination generated by moving objects will not be accumulated, generating less ghosting but introducing additional noise.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("When enabled, global illumination generated by moving objects will not be accumulated, generating less ghosting but introducing additional noise.")]
        public BoolParameter receiverMotionRejection = new BoolParameter(true);

        #endregion

        internal static bool RayTracingActive(GlobalIllumination volume)
        {
            return volume.tracing.value != RayCastingMode.RayMarching;
        }
    }
}
