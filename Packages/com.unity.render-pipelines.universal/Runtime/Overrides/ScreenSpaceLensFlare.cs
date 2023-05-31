using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The resolution at which URP computes the Screen Space Lens Flare effect.
    /// </summary>
    public enum ScreenSpaceLensFlareResolution : int
    {
        /// <summary>
        /// Half Resolution.
        /// </summary>
        Half = 2,

        /// <summary>
        /// Quarter Resolution.
        /// </summary>
        Quarter = 4,

        /// <summary>
        /// Eigtht Resolution.
        /// </summary>
        Eighth = 8
    }

    /// <summary>
    /// A volume component that holds settings for the Screen Space Lens Flare effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Screen Space Lens Flare")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("shared/lens-flare/lens-flare-component")]
    public class ScreenSpaceLensFlare : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Sets the global intensity of the Screen Space Lens Flare effect. When set to 0, the pass is skipped.
        /// </summary>
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);
        /// <summary>
        /// Sets the color used to tint all the flares.
        /// </summary>
        public ColorParameter tintColor = new ColorParameter(Color.white);
        /// <summary>
        /// Controls the bloom Mip used as a source for the Lens Flare effect. A high value will result in a blurrier result for all flares.
        /// </summary>
        [AdditionalProperty]
        public ClampedIntParameter bloomMip = new ClampedIntParameter(1, 0, 5);
        /// <summary>
        /// Controls the intensity of the Regular Flare sample. Those flares are sampled using scaled screen coordinates.
        /// </summary>
        [Header("Flares")]
        public MinFloatParameter firstFlareIntensity = new MinFloatParameter(1f, 0f);
        /// <summary>
        /// Controls the intensity of the Reversed Flare sample. Those flares are sampled using scaled and flipped screen coordinates.
        /// </summary>
        public MinFloatParameter secondaryFlareIntensity = new MinFloatParameter(1f, 0f);
        /// <summary>
        /// Controls the intensity of the Warped Flare sample. Those flares are sampled using polar screen coordinates.
        /// </summary>
        public MinFloatParameter warpedFlareIntensity = new MinFloatParameter(1f, 0f);
        /// <summary>
        /// Sets the scale of the warped flare. A value of 1,1 will keep this flare circular.
        /// </summary>
        [AdditionalProperty]
        public Vector2Parameter warpedFlareScale = new Vector2Parameter(new Vector2(1f, 1f));
        /// <summary>
        /// Controls the number of times the flare effect is repeated for each flare type (first, second, warped). This parameter has a strong impact on performance.
        /// </summary>
        public ClampedIntParameter samples = new ClampedIntParameter(1, 1, 3);
        /// <summary>
        /// Controls the value by which each additionnal sample is multiplied. A value of 1 keep the same intensities for all samples. A value of 0.7 multiplies the first sample by 1 (0.7 power 0), the second sample by 0.7 (0.7 power 1) and the third sample by 0.49 (0.7 power 2).
        /// </summary>
        [AdditionalProperty]
        public ClampedFloatParameter sampleDimmer = new ClampedFloatParameter(0.5f, 0.1f, 1f);
        /// <summary>
        /// Controls the intensity of the vignette effect to occlude the Lens Flare effect at the center of the screen.
        /// </summary>
        public ClampedFloatParameter vignetteEffect = new ClampedFloatParameter(1f, 0f, 1f);
        /// <summary>
        /// Controls the starting position of the flares in screen space relative to their source.
        /// </summary>
        public ClampedFloatParameter startingPosition = new ClampedFloatParameter(1.25f, 1f, 3f);
        /// <summary>
        /// Controls the scale at which the flare effect is sampled.
        /// </summary>
        public ClampedFloatParameter scale = new ClampedFloatParameter(1.5f, 1f, 4f);
        /// <summary>
        /// Controls the intensity of streaks effect. This effect has an impact on performance when above zero. When this intensity is zero, this effect is not evaluated to save costs.
        /// </summary>
        [Header("Streaks")]
        public MinFloatParameter streaksIntensity = new MinFloatParameter(0f, 0f);
        /// <summary>
        /// Controls the length of streaks effect. A value of one creates streaks about the width of the screen.
        /// </summary>
        public ClampedFloatParameter streaksLength = new ClampedFloatParameter(0.5f, 0f, 1f);
        /// <summary>
        /// Controls the orientation of streaks effect in degrees. A value of 0 produces horizontal streaks.
        /// </summary>
        public FloatParameter streaksOrientation = new FloatParameter(0f);
        /// <summary>
        /// Controls the threshold of horizontal streak effect. A high value makes the effect more localised on the high intensity areas of the screen.
        /// </summary>
        public ClampedFloatParameter streaksThreshold = new ClampedFloatParameter(0.25f, 0f, 1f);
        /// <summary>
        /// Specifies the resolution at which the streak effect is evaluated.
        /// </summary>
        [SerializeField]
        [AdditionalProperty]
        public ScreenSpaceLensFlareResolutionParameter resolution = new ScreenSpaceLensFlareResolutionParameter(ScreenSpaceLensFlareResolution.Quarter);
        /// <summary>
        /// Controls the strength of the Chromatic Aberration effect. The higher the value, the more light is dispersed on the sides of the screen
        /// </summary>
        [Header("Chromatic Abberation")]
        public ClampedFloatParameter chromaticAbberationIntensity = new ClampedFloatParameter(0.5f, 0f, 1f);
        /// <summary>
        /// Default constructor for the lens flare volume component.
        /// </summary>
        public ScreenSpaceLensFlare()
        {
            displayName = "Screen Space Lens Flare";
        }
        /// <summary>
        /// Mandatory function, cannot have an Override without it
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return intensity.value > 0;
        }
        /// <summary>
        /// Returns true is streakIntensity is above zero.
        /// </summary>
        /// <returns></returns>
        public bool IsStreaksActive()
        {
            return streaksIntensity.value > 0;
        }


        /// <inheritdoc/>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => false;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceLensFlareResolution"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceLensFlareResolutionParameter : VolumeParameter<ScreenSpaceLensFlareResolution>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceLensFlareResolutionParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceLensFlareResolutionParameter(ScreenSpaceLensFlareResolution value, bool overrideState = false) : base(value, overrideState) { }
    }

}
