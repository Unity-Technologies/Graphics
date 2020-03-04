using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Exposure effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Exposure")]
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
        public FloatParameter limitMin = new FloatParameter(-10f);

        /// <summary>
        /// Sets the maximum value that the Scene exposure can be set to.
        /// This parameter is only used when <see cref="ExposureMode.Automatic"/> or <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Sets the maximum value that the Scene exposure can be set to.")]
        public FloatParameter limitMax = new FloatParameter(20f);

        /// <summary>
        /// Specifies a curve that remaps the Scene exposure on the x-axis to the exposure you want on the y-axis.
        /// This parameter is only used when <see cref="ExposureMode.CurveMapping"/> is set.
        /// </summary>
        [Tooltip("Specifies a curve that remaps the Scene exposure on the x-axis to the exposure you want on the y-axis.")]
        public AnimationCurveParameter curveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -10f, 20f, 20f)); // TODO: Use TextureCurve instead?

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
        Fixed,

        /// <summary>
        /// Automatically sets the exposure depending on what is on screen.
        /// </summary>
        Automatic,

        /// <summary>
        /// Maps the current Scene exposure to a custom curve.
        /// </summary>
        CurveMapping,

        /// <summary>
        /// Uses the current physical Camera settings to set the Scene exposure.
        /// </summary>
        UsePhysicalCamera
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
        CenterWeighted
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
}
