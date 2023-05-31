using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Options to select a tonemapping algorithm to use for color grading.
    /// </summary>
    public enum TonemappingMode
    {
        /// <summary>
        /// Use this option if you do not want to apply tonemapping
        /// </summary>
        None,

        /// <summary>
        /// Use this option if you only want range-remapping with minimal impact on color hue and saturation.
        /// It is generally a great starting point for extensive color grading.
        /// </summary>
        Neutral, // Neutral tonemapper

        /// <summary>
        /// Use this option to apply a close approximation of the reference ACES tonemapper for a more filmic look.
        /// It is more contrasted than Neutral and has an effect on actual color hue and saturation.
        /// Note that if you use this tonemapper all the grading operations will be done in the ACES color spaces for optimal precision and results.
        /// </summary>
        ACES, // ACES Filmic reference tonemapper (custom approximation)
    }

    /// <summary>
    /// Available options for when HDR Output is enabled and Tonemap is set to Neutral.
    /// </summary>
    public enum NeutralRangeReductionMode
    {
        /// <summary>
        /// Simple Reinhard tonemapping curve.
        /// </summary>
        Reinhard = HDRRangeReduction.Reinhard,
        /// <summary>
        /// Range reduction curve as specified in the BT.2390 standard.
        /// </summary>
        BT2390 = HDRRangeReduction.BT2390
    }

    /// <summary>
    /// Preset used when selecting ACES tonemapping for HDR displays.
    /// </summary>
    public enum HDRACESPreset
    {
        /// <summary>
        /// Preset for a display with a maximum range of 1000 nits.
        /// </summary>
        ACES1000Nits = HDRRangeReduction.ACES1000Nits,
        /// <summary>
        /// Preset for a display with a maximum range of 2000 nits.
        /// </summary>
        ACES2000Nits = HDRRangeReduction.ACES2000Nits,
        /// <summary>
        /// Preset for a display with a maximum range of 4000 nits.
        /// </summary>
        ACES4000Nits = HDRRangeReduction.ACES4000Nits,
    }

    /// <summary>
    /// A volume component that holds settings for the tonemapping effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Tonemapping")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("post-processing-tonemapping")]
    public sealed class Tonemapping : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Use this to select a tonemapping algorithm to use for color grading.
        /// </summary>
        [Tooltip("Select a tonemapping algorithm to use for the color grading process.")]
        public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);

        // -- HDR Output options --

        /// <summary>
        /// Specifies the range reduction mode used when HDR output is enabled and Neutral tonemapping is enabled.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Specifies the range reduction mode used when HDR output is enabled and Neutral tonemapping is enabled.")]
        public NeutralRangeReductionModeParameter neutralHDRRangeReductionMode = new NeutralRangeReductionModeParameter(NeutralRangeReductionMode.BT2390);

        /// <summary>
        /// Specifies the preset for HDR displays.
        /// </summary>
        [Tooltip("Use the ACES preset for HDR displays.")]
        public HDRACESPresetParameter acesPreset = new HDRACESPresetParameter(HDRACESPreset.ACES1000Nits);

        /// <summary>
        /// Specify how much hue to preserve. Values closer to 0 are likely to preserve hue. As values get closer to 1, Unity doesn't correct hue shifts.
        /// </summary>
        [Tooltip("Specify how much hue to preserve. Values closer to 0 are likely to preserve hue. As values get closer to 1, Unity doesn't correct hue shifts.")]
        public ClampedFloatParameter hueShiftAmount = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Enable to use values detected from the output device as paper white. When enabled, output images might differ between SDR and HDR. For best accuracy, set this value manually.
        /// </summary>
        [Tooltip("Enable to use values detected from the output device as paper white. When enabled, output images might differ between SDR and HDR. For best accuracy, set this value manually.")]
        public BoolParameter detectPaperWhite = new BoolParameter(false);

        /// <summary>
        /// The reference brightness of a paper white surface. This property determines the maximum brightness of UI. The brightness of the scene is scaled relative to this value. The value is in nits.
        /// </summary>
        [Tooltip("The reference brightness of a paper white surface. This property determines the maximum brightness of UI. The brightness of the scene is scaled relative to this value. The value is in nits.")]
        public ClampedFloatParameter paperWhite = new ClampedFloatParameter(300.0f, 0.0f, 400.0f);

        /// <summary>
        /// Enable to use the minimum and maximum brightness values detected from the output device. For best accuracy, considering calibrating these values manually.
        /// </summary>
        [Tooltip("Enable to use the minimum and maximum brightness values detected from the output device. For best accuracy, considering calibrating these values manually.")]
        public BoolParameter detectBrightnessLimits = new BoolParameter(true);

        /// <summary>
        /// The minimum brightness of the screen (in nits). This value is assumed to be 0.005f with ACES Tonemap.
        /// </summary>
        [Tooltip("The minimum brightness of the screen (in nits). This value is assumed to be 0.005f with ACES Tonemap.")]
        public ClampedFloatParameter minNits = new ClampedFloatParameter(0.005f, 0.0f, 50.0f);

        /// <summary>
        /// The maximum brightness of the screen (in nits). This value is defined by the preset when using ACES Tonemap.
        /// </summary>
        [Tooltip("The maximum brightness of the screen (in nits). This value is defined by the preset when using ACES Tonemap.")]
        public ClampedFloatParameter maxNits = new ClampedFloatParameter(1000.0f, 0.0f, 5000.0f);

        /// <inheritdoc/>
        public bool IsActive() => mode.value != TonemappingMode.None;

        /// <inheritdoc/>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="TonemappingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode>
    {
        /// <summary>
        /// Creates a new <see cref="TonemappingModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that contains a <see cref="NeutralRangeReductionMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class NeutralRangeReductionModeParameter : VolumeParameter<NeutralRangeReductionMode>
    {
        /// <summary>
        /// Creates a new <see cref="NeutralRangeReductionModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NeutralRangeReductionModeParameter(NeutralRangeReductionMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that contains a <see cref="HDRACESPreset"/> value.
    /// </summary>
    [Serializable]
    public sealed class HDRACESPresetParameter : VolumeParameter<HDRACESPreset>
    {
        /// <summary>
        /// Creates a new <see cref="HDRACESPresetParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public HDRACESPresetParameter(HDRACESPreset value, bool overrideState = false) : base(value, overrideState) { }
    }
}
