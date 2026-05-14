#if URP_SCREEN_SPACE_REFLECTION
using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Screen Space Reflections Renderer Feature.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Reflection"), SupportedOnRenderPipeline]
    [DisplayInfo(name = "Screen Space Reflection")]
    public class ScreenSpaceReflectionVolumeSettings : VolumeComponent
    {
        /// <summary>
        /// An enum specifying which resolution to render Screen Space Reflections at.
        /// </summary>
        public enum Resolution
        {
            Full = 1,
            Half = 2,
            Quarter = 4,
        }

        /// <summary>
        /// An enum specifying which technique to use for upscaling Screen Space Reflections.
        /// </summary>
        public enum UpscalingMethod
        {
            None,
            Kawase,
            Gaussian,
            Bilateral,
        }

        /// <summary>
        /// An enum specifying which quality to use for Screen Space Reflections.
        /// </summary>
        public enum RoughReflectionsQuality
        {
            Disabled,
            BoxBlur,
            GaussianBlur,
        }

        /// <summary>
        /// An enum specifying which objects to reflect using Screen Space Reflections.
        /// </summary>
        public enum ReflectionMode
        {
            Disabled,
            OpaquesOnly,
            OpaquesAndTransparents,
        }

        internal enum PerformancePreset
        {
            Fast,
            Balanced,
            HighQuality,
            BestQuality,
            Custom
        }

        internal struct PerformancePresetValues
        {
            public Resolution resolution;
            public UpscalingMethod upscalingMethod;
            public bool linearMarching;
            public int hitRefinementSteps;
            public float finalThicknessMultiplier;
            public float maxRayLength;
            public int maxRaySteps;
            public float objectThickness;
        }

        internal static ref readonly PerformancePresetValues DefaultPreset => ref k_PerformancePresets[(int)PerformancePreset.HighQuality];
        internal static readonly PerformancePresetValues[] k_PerformancePresets =
        {
            // Fastest
            new()
            {
                resolution = Resolution.Quarter,
                upscalingMethod = UpscalingMethod.Kawase,
                linearMarching = true,
                hitRefinementSteps = 3,
                finalThicknessMultiplier = 0.15f,
                maxRayLength = 20f,
                maxRaySteps = 16,
                objectThickness = 0.325f
            },
            // Balanced
            new()
            {
                resolution = Resolution.Half,
                upscalingMethod = UpscalingMethod.Gaussian,
                linearMarching = true,
                hitRefinementSteps = 5,
                finalThicknessMultiplier = 0.05f,
                maxRayLength = 30f,
                maxRaySteps = 32,
                objectThickness = 0.325f
            },
            // High Quality
            new()
            {
                resolution = Resolution.Half,
                upscalingMethod = UpscalingMethod.Gaussian,
                linearMarching = false,
                hitRefinementSteps = 5,
                finalThicknessMultiplier = 0.16f,
                maxRayLength = 30f,
                maxRaySteps = 64,
                objectThickness = 0.01f
            },
            // Best Quality
            new()
            {
                resolution = Resolution.Full,
                upscalingMethod = UpscalingMethod.Gaussian,
                linearMarching = false,
                hitRefinementSteps = 5,
                finalThicknessMultiplier = 0.16f,
                maxRayLength = 30f,
                maxRaySteps = 64,
                objectThickness = 0.01f
            }
        };

        /// <summary>The mode determining which objects to reflect using Screen Space Reflections.</summary>
        [Tooltip("The mode determining which objects to reflect using Screen Space Reflections. 'Opaques Only' will only render opaque objects in reflections, while 'Opaques And Transparents' will also render transparent objects in reflections.")]
        public EnumParameter<ReflectionMode> mode = new(ReflectionMode.OpaquesOnly);

        /// <summary>The resolution to render Screen Space Reflections at.</summary>
        [Tooltip("The resolution to render Screen Space Reflections at. Lower values will yield better performance, but lower quality.")]
        public EnumParameter<Resolution> resolution = new(DefaultPreset.resolution, true);

        /// <summary>The technique to use for upscaling Screen Space Reflections.</summary>
        [Tooltip("The method to use for upscaling the low resolution SSR texture. 'Kawase' is the most performant method, followed by 'Gaussian', and finally 'Bilateral'.")]
        public EnumParameter<UpscalingMethod> upscalingMethod = new(DefaultPreset.upscalingMethod, true);

        /// <summary>Whether to use linear marching to calculate Screen Space Reflections, rather than hierarchical depth buffer marching.</summary>
        [Tooltip("Whether to use linear marching to calculate Screen Space Reflections, rather than hierarchical depth buffer marching. With the option disabled, Unity generates a depth pyramid and uses its for hierarchical marching. This is more accurate, but may be less performant on low-end devices.")]
        public BoolParameter linearMarching = new(DefaultPreset.linearMarching, true);

        /// <summary>Amount of binary search steps applied at the end of the ray to refine hit results, reducing stair-stepping artifacts and gaps in reflections caused by linear marching, where initial steps may be imprecise and miss fine details.</summary>
        [Tooltip("Amount of binary search steps applied at the end of the ray to refine hit results, reducing stair-stepping artifacts and gaps in reflections caused by linear marching, where initial steps may be imprecise and miss fine details.")]
        public MinIntParameter hitRefinementSteps = new(DefaultPreset.hitRefinementSteps, 0, true);

        /// <summary>Multiplies the regular thickness to compute a finer value, used with additional refinement steps to achieve more precise hit detection.</summary>
        [Tooltip("Multiplies the regular thickness to compute a finer value, used with additional refinement steps to achieve more precise hit detection.")]
        public ClampedFloatParameter finalThicknessMultiplier = new(DefaultPreset.finalThicknessMultiplier, 0.0f, 1f, true);

        /// <summary>Whether to enable rough reflections by blurring the reflected color.</summary>
        [Tooltip("Whether to enable rough reflections by blurring the reflected color. Disabling will improve performance, but all reflections will be mirror-like.")]
        public EnumParameter<RoughReflectionsQuality> roughReflections = new(RoughReflectionsQuality.GaussianBlur);

        /// <summary>Controls how blurry rough reflections appear on a logarithmic scale. A value of 0 is neutral, negative values reduce blurriness, positive values increase it.</summary>
        [Tooltip("Controls how blurry rough reflections appear on a logarithmic scale. A value of 0 is neutral, negative values reduce blurriness, positive values increase it.")]
        public ClampedFloatParameter roughnessScale = new(0.0f, -2.0f, 2.0f);

        /// <summary>The minimum amount of surface smoothness at which Screen Space Reflections are used.</summary>
        [Tooltip("The minimum amount of surface smoothness at which Screen Space Reflections are used. Higher values will result in less objects receiving Screen Space Reflections.")]
        public ClampedFloatParameter minimumSmoothness = new(0.05f, 0.0f, 1.0f);

        /// <summary>The smoothness value at which the smoothness-controlled fade out starts.</summary>
        [Tooltip("The smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start].")]
        public ClampedFloatParameter smoothnessFadeStart = new(0.1f, 0.0f, 1.0f);

        /// <summary>How much to fade reflections based on the reflection normal.</summary>
        [Tooltip("How much to fade reflections based on the reflection normal.")]
        public ClampedFloatParameter normalFade = new(0.0f, 0.0f, 1.0f);

        /// <summary>The distance at which the reflection fades out near the edge of the screen.</summary>
        [Tooltip("The distance at which the reflection fades out near the edge of the screen.")]
        public ClampedFloatParameter screenEdgeFadeDistance = new(0.2f, 0.0f, 1.0f);

        /// <summary>Whether to use Screen Space Reflections to handle reflections of the sky.</summary>
        [Tooltip("Whether to use SSR to handle sky reflection. If you disable this property, pixels that reflect the sky will sample from nearby reflection probes, or the skybox.")]
        public BoolParameter reflectSky = new(false);

        /// <summary>The maximum distance in world space units a ray can travel. Only has an effect when linearMarching is enabled.</summary>
        [Tooltip("The maximum distance in world space units a ray can travel.")]
        public MinFloatParameter maxRayLength = new(DefaultPreset.maxRayLength, 0f, true);

        /// <summary>The fade distance in world space units before the maximum ray length. Only has an effect when linearMarching is enabled.</summary>
        [Tooltip("The fade distance in world space units before the maximum ray length. Only has an effect when Linear Marching is enabled.")]
        public MinFloatParameter rayLengthFade = new(1f, 0f);

        /// <summary>The maximum amount of steps to take when tracing rays.</summary>
        [Tooltip("The maximum amount of steps to take when tracing rays.")]
        public MinIntParameter maxRaySteps = new(DefaultPreset.maxRaySteps, 1, true);

        /// <summary>How close to the depth buffer a ray must be to be considered a hit.</summary>
        [Tooltip("How close to the depth buffer a ray must be to be considered a hit. Higher values will result in less accurate reflections, but may help mitigate shimmering artifacts.")]
        public ClampedFloatParameter objectThickness = new(DefaultPreset.objectThickness, 0f, 1f, true);

        // Helpers
        internal bool ShouldRenderTransparents() => mode.value == ReflectionMode.OpaquesAndTransparents;
        internal bool ShouldUseGaussianBlurRoughness() => roughReflections.value == RoughReflectionsQuality.GaussianBlur;
        internal bool ShouldUseLinearMarching() => linearMarching.value || !SystemInfo.supportsComputeShaders;

        // Allow listening for property changes, to support presets in the presence of undo and changing values from script etc.
#if UNITY_EDITOR
        internal event Action propertyChanged;
        private void OnValidate() => propertyChanged?.Invoke();
#endif
    }
}
#endif
