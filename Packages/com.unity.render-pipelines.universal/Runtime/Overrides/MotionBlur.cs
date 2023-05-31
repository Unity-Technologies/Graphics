using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Option to control motion blur Mode.
    /// </summary>
    public enum MotionBlurMode
    {
        /// <summary>
        /// Use this if you don't need object motion blur.
        /// </summary>
        CameraOnly,

        /// <summary>
        /// Use this if you need object motion blur.
        /// </summary>
        CameraAndObjects
    }

    /// <summary>
    /// Options to control the quality the motion blur effect.
    /// </summary>
    public enum MotionBlurQuality
    {
        /// <summary>
        /// Use this to select low motion blur quality.
        /// </summary>
        Low,

        /// <summary>
        /// Use this to select medium motion blur quality.
        /// </summary>
        Medium,

        /// <summary>
        /// Use this to select high motion blur quality.
        /// </summary>
        High
    }

    /// <summary>
    /// A volume component that holds settings for the motion blur effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Motion Blur")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Motion-Blur")]
    public sealed class MotionBlur : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// The motion blur technique to use. If you don't need object motion blur, CameraOnly will result in better performance.
        /// </summary>
        [Tooltip("The motion blur technique to use. If you don't need object motion blur, CameraOnly will result in better performance.")]
        public MotionBlurModeParameter mode = new MotionBlurModeParameter(MotionBlurMode.CameraOnly);

        /// <summary>
        /// The quality of the effect. Lower presets will result in better performance at the expense of visual quality.
        /// </summary>
        [Tooltip("The quality of the effect. Lower presets will result in better performance at the expense of visual quality.")]
        public MotionBlurQualityParameter quality = new MotionBlurQualityParameter(MotionBlurQuality.Low);

        /// <summary>
        /// Sets the intensity of the motion blur effect. Acts as a multiplier for velocities.
        /// </summary>
        [Tooltip("The strength of the motion blur filter. Acts as a multiplier for velocities.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Sets the maximum length, as a fraction of the screen's full resolution, that the velocity resulting from Camera rotation can have.
        /// Lower values will improve performance.
        /// </summary>
        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the velocity resulting from Camera rotation can have. Lower values will improve performance.")]
        public ClampedFloatParameter clamp = new ClampedFloatParameter(0.05f, 0f, 0.2f);

        /// <inheritdoc/>
        public bool IsActive() => intensity.value > 0f;

        /// <inheritdoc/>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => false;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="MotionBlurMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class MotionBlurModeParameter : VolumeParameter<MotionBlurMode>
    {
        /// <summary>
        /// Creates a new <see cref="MotionBlurModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public MotionBlurModeParameter(MotionBlurMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="MotionBlurQuality"/> value.
    /// </summary>
    [Serializable]
    public sealed class MotionBlurQualityParameter : VolumeParameter<MotionBlurQuality>
    {
        /// <summary>
        /// Creates a new <see cref="MotionBlurQualityParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public MotionBlurQualityParameter(MotionBlurQuality value, bool overrideState = false) : base(value, overrideState) { }
    }
}
