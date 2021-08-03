using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Exposure effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Exposure")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Override-Exposure" + Documentation.endURL)]
    public sealed class Exposure : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Specifies the method that HDRP uses to process exposure.
        /// </summary>
        /// <seealso cref="ExposureMode"/>
        [Tooltip("Specifies the method that HDRP uses to process exposure.")]
        public ExposureModeParameter mode = new ExposureModeParameter(ExposureMode.Fixed);

        /// <summary>
        /// Specifies the metering method that HDRP uses the filter the luminance source.
        /// </summary>
        /// <seealso cref="MeteringMode"/>
        [Tooltip("Specifies the metering method that HDRP uses the filter the luminance source.")]
        public MeteringModeParameter meteringMode = new MeteringModeParameter(MeteringMode.CenterWeighted);

        /// <summary>
        /// Specifies the luminance source that HDRP uses to calculate the current Scene exposure.
        /// </summary>
        /// <seealso cref="LuminanceSource"/>
        [Tooltip("Specifies the luminance source that HDRP uses to calculate the current Scene exposure.")]
        public LuminanceSourceParameter luminanceSource = new LuminanceSourceParameter(LuminanceSource.ColorBuffer);

        /// <summary>
        /// Sets a static exposure value for Cameras in this Volume.
        /// This parameter is only used when <see cref="ExposureMode.Fixed"/> is set.
        /// </summary>
        [Tooltip("Sets a static exposure value for Cameras in this Volume.")]
        public FloatParameter fixedExposure = new FloatParameter(0f);

        /// <summary>
        /// Sets the compensation that the Camera applies to the calculated exposure value.
        /// This parameter is only used when any mode but <see cref="ExposureMode.Fixed"/> is set.
        /// </summary>
        [Tooltip("Sets the compensation that the Camera applies to the calculated exposure value.")]
        public FloatParameter compensation = new FloatParameter(0f);

        /// <summary>
        /// Sets the minimum value that the Scene exposure can be set to.
        /// This parameter is only used when <see cref="ExposureMode.Automatic"/> or <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Sets the minimum value that the Scene exposure can be set to.")]
        public FloatParameter limitMin = new FloatParameter(-1f);

        /// <summary>
        /// Sets the maximum value that the Scene exposure can be set to.
        /// This parameter is only used when <see cref="ExposureMode.Automatic"/> or <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Sets the maximum value that the Scene exposure can be set to.")]
        public FloatParameter limitMax = new FloatParameter(14f);

        /// <summary>
        /// Specifies a curve that remaps the Scene exposure on the x-axis to the exposure you want on the y-axis.
        /// This parameter is only used when <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Specifies a curve that remaps the Scene exposure on the x-axis to the exposure you want on the y-axis.")]
        public AnimationCurveParameter curveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -10f, 20f, 20f)); // TODO: Use TextureCurve instead?

        /// <summary>
        /// Specifies a curve that determines for each current exposure value (x-value) what minimum value is allowed to auto-adaptation (y-axis).
        /// This parameter is only used when <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Specifies a curve that determines for each current exposure value (x-value) what minimum value is allowed to auto-adaptation (y-axis).")]
        public AnimationCurveParameter limitMinCurveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -12f, 20f, 18f));

        /// <summary>
        /// Specifies a curve that determines for each current exposure value (x-value) what maximum value is allowed to auto-adaptation (y-axis).
        /// This parameter is only used when <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Specifies a curve that determines for each current exposure value (x-value) what maximum value is allowed to auto-adaptation (y-axis).")]
        public AnimationCurveParameter limitMaxCurveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -8f, 20f, 22f));

        /// <summary>
        /// Specifies the method that HDRP uses to change the exposure when the Camera moves from dark to light and vice versa.
        /// This parameter is only used when <see cref="ExposureMode.Automatic"/> or <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Specifies the method that HDRP uses to change the exposure when the Camera moves from dark to light and vice versa.")]
        public AdaptationModeParameter adaptationMode = new AdaptationModeParameter(AdaptationMode.Progressive);

        /// <summary>
        /// Sets the speed at which the exposure changes when the Camera moves from a dark area to a bright area.
        /// This parameter is only used when <see cref="ExposureMode.Automatic"/> or <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Sets the speed at which the exposure changes when the Camera moves from a dark area to a bright area.")]
        public MinFloatParameter adaptationSpeedDarkToLight = new MinFloatParameter(3f, 0.001f);

        /// <summary>
        /// Sets the speed at which the exposure changes when the Camera moves from a bright area to a dark area.
        /// This parameter is only used when <see cref="ExposureMode.Automatic"/> or <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Sets the speed at which the exposure changes when the Camera moves from a bright area to a dark area.")]
        public MinFloatParameter adaptationSpeedLightToDark = new MinFloatParameter(1f, 0.001f);

        /// <summary>
        /// Sets the texture mask used to weight the pixels in the buffer when computing exposure.
        /// </summary>
        [Tooltip("Sets the texture mask to be used to weight the pixels in the buffer for the sake of computing exposure.")]
        public NoInterpTextureParameter weightTextureMask = new NoInterpTextureParameter(null);

        /// <summary>
        /// These values are the lower and upper percentages of the histogram that will be used to
        /// find a stable average luminance. Values outside of this range will be discarded and won't
        /// contribute to the average luminance.
        /// </summary>
        [Tooltip("Sets the range of values (in terms of percentages) of the histogram that are accepted while finding a stable average exposure. Anything outside the value is discarded.")]
        public FloatRangeParameter histogramPercentages = new FloatRangeParameter(new Vector2(40.0f, 90.0f), 0.0f, 100.0f);

        /// <summary>
        /// Sets whether histogram exposure mode will remap the computed exposure with a curve remapping (akin to Curve Remapping mode)
        /// </summary>
        [Tooltip("Sets whether histogram exposure mode will remap the computed exposure with a curve remapping (akin to Curve Remapping mode).")]
        public BoolParameter histogramUseCurveRemapping = new BoolParameter(false);

        /// <summary>
        /// Sets the desired Mid gray level used by the auto exposure (i.e. to what grey value the auto exposure system maps the average scene luminance).
        /// Note that the lens model used in HDRP is not of a perfect lens, hence it will not map precisely to the selected value.
        /// </summary>
        [Tooltip("Sets the desired Mid gray level used by the auto exposure (i.e. to what grey value the auto exposure system maps the average scene luminance).")]
        public TargetMidGrayParameter targetMidGray = new TargetMidGrayParameter(TargetMidGray.Grey125);

        /// <summary>
        /// Sets whether the procedural metering mask is centered around the exposure target (to be set on the camera)
        /// </summary>
        [Tooltip("Sets whether histogram exposure mode will remap the computed exposure with a curve remapping (akin to Curve Remapping mode).")]
        public BoolParameter centerAroundExposureTarget = new BoolParameter(false);

        /// <summary>
        /// Sets the center of the procedural metering mask ([0,0] being bottom left of the screen and [1,1] top right of the screen)
        /// </summary>
        public NoInterpVector2Parameter proceduralCenter = new NoInterpVector2Parameter(new Vector2(0.5f, 0.5f));
        /// <summary>
        /// Sets the radii of the procedural mask, in terms of fraction of half the screen (i.e. 0.5 means a mask that stretch half of the screen in both directions).
        /// </summary>
        public NoInterpVector2Parameter proceduralRadii  = new NoInterpVector2Parameter(new Vector2(0.3f, 0.3f));
        /// <summary>
        /// All pixels below this threshold (in EV100 units) will be assigned a weight of 0 in the metering mask.
        /// </summary>
        [Tooltip("All pixels below this threshold (in EV100 units) will be assigned a weight of 0 in the metering mask.")]
        public FloatParameter maskMinIntensity = new FloatParameter(-30.0f);
        /// <summary>
        /// All pixels above this threshold (in EV100 units) will be assigned a weight of 0 in the metering mask.
        /// </summary>
        [Tooltip("All pixels above this threshold (in EV100 units) will be assigned a weight of 0 in the metering mask.")]
        public FloatParameter maskMaxIntensity = new FloatParameter(30.0f);

        /// <summary>
        /// Sets the softness of the mask, the higher the value the less influence is given to pixels at the edge of the mask.
        /// </summary>
        public NoInterpMinFloatParameter proceduralSoftness = new NoInterpMinFloatParameter(0.5f, 0.0f);


        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return true;
        }
    }

    /// <summary>
    /// Methods that HDRP uses to process exposure.
    /// </summary>
    /// <seealso cref="Exposure.mode"/>
    public enum ExposureMode
    {
        /// <summary>
        /// Allows you to manually sets the Scene exposure.
        /// </summary>
        Fixed = 0,

        /// <summary>
        /// Automatically sets the exposure depending on what is on screen.
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Automatically sets the exposure depending on what is on screen and can filter out outliers based on provided settings.
        /// </summary>
        AutomaticHistogram = 4,

        /// <summary>
        /// Maps the current Scene exposure to a custom curve.
        /// </summary>
        CurveMapping = 2,

        /// <summary>
        /// Uses the current physical Camera settings to set the Scene exposure.
        /// </summary>
        UsePhysicalCamera = 3
    }

    /// <summary>
    /// Metering methods that HDRP uses the filter the luminance source
    /// </summary>
    /// <seealso cref="Exposure.meteringMode"/>
    public enum MeteringMode
    {
        /// <summary>
        /// The Camera uses the entire luminance buffer to measure exposure.
        /// </summary>
        Average,

        /// <summary>
        /// The Camera only uses the center of the buffer to measure exposure. This is useful if you
        /// want to only expose light against what is in the center of your screen.
        /// </summary>
        Spot,

        /// <summary>
        /// The Camera applies a weight to every pixel in the buffer and then uses them to measure
        /// the exposure. Pixels in the center have the maximum weight, pixels at the screen borders
        /// have the minimum weight, and pixels in between have a progressively lower weight the
        /// closer they are to the screen borders.
        /// </summary>
        CenterWeighted,


        /// <summary>
        /// The Camera applies a weight to every pixel in the buffer and then uses them to measure
        /// the exposure. The weighting is specified by the texture provided by the user. Note that if
        /// no texture is provided, then this metering mode is equivalent to Average.
        /// </summary>
        MaskWeighted,

        /// <summary>
        /// Create a weight mask centered around the specified UV and with the desired parameters.
        /// </summary>
        ProceduralMask,


    }

    /// <summary>
    /// Luminance source that HDRP uses to calculate the current Scene exposure.
    /// </summary>
    /// <remarks>
    /// <see cref="LuminanceSource.LightingBuffer"/> is not implemented yet.
    /// </remarks>
    /// <seealso cref="Exposure.luminanceSource"/>
    public enum LuminanceSource
    {
        /// <summary>
        /// Uses the lighting data only.
        /// </summary>
        /// <remarks>
        /// This mode is not implemented yet.
        /// </remarks>
        LightingBuffer,

        /// <summary>
        /// Uses the final color data before post-processing has been applied.
        /// </summary>
        ColorBuffer
    }

    /// <summary>
    /// The target grey value used by the exposure system. Note this is equivalent of changing the calibration constant K on the used virtual reflected light meter.
    /// </summary>
    public enum TargetMidGray
    {
        /// <summary>
        /// Mid Grey 12.5% (reflected light meter K set as 12.5)
        /// </summary>
        Grey125,

        /// <summary>
        /// Mid Grey 14.0% (reflected light meter K set as 14.0)
        /// </summary>
        Grey14,

        /// <summary>
        /// Mid Grey 18.0% (reflected light meter K set as 18.0). Note that this value is outside of the suggested K range by the ISO standard.
        /// </summary>
        Grey18
    }

    /// <summary>
    /// Methods that HDRP uses to change the exposure when the Camera moves from dark to light and vice versa.
    /// </summary>
    /// <seealso cref="Exposure.adaptationMode"/>
    public enum AdaptationMode
    {
        /// <summary>
        /// The exposure changes instantly.
        /// </summary>
        Fixed,

        /// <summary>
        /// The exposure changes over the period of time.
        /// </summary>
        /// <seealso cref="Exposure.adaptationSpeedDarkToLight"/>
        /// <seealso cref="Exposure.adaptationSpeedLightToDark"/>
        Progressive
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ExposureMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class ExposureModeParameter : VolumeParameter<ExposureMode>
    {
        /// <summary>
        /// Creates a new <see cref="ExposureModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ExposureModeParameter(ExposureMode value, bool overrideState = false) : base(value, overrideState) {}
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="MeteringMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class MeteringModeParameter : VolumeParameter<MeteringMode>
    {
        /// <summary>
        /// Creates a new <see cref="MeteringModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public MeteringModeParameter(MeteringMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="LuminanceSource"/> value.
    /// </summary>
    [Serializable]
    public sealed class LuminanceSourceParameter : VolumeParameter<LuminanceSource>
    {
        /// <summary>
        /// Creates a new <see cref="LuminanceSourceParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public LuminanceSourceParameter(LuminanceSource value, bool overrideState = false) : base(value, overrideState) {}
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="AdaptationMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class AdaptationModeParameter : VolumeParameter<AdaptationMode>
    {
        /// <summary>
        /// Creates a new <see cref="AdaptationModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public AdaptationModeParameter(AdaptationMode value, bool overrideState = false) : base(value, overrideState) {}
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="TargetMidGray"/> value.
    /// </summary>
    [Serializable]
    public sealed class TargetMidGrayParameter : VolumeParameter<TargetMidGray>
    {
        /// <summary>
        /// Creates a new <see cref="TargetMidGrayParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public TargetMidGrayParameter(TargetMidGray value, bool overrideState = false) : base(value, overrideState) { }
    }
}
