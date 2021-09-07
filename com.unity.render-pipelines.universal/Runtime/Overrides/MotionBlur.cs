using System;

namespace UnityEngine.Rendering.Universal
{
    public enum MotionBlurMode
    {
        CameraOnly,
        CameraAndObjects
    }

    public enum MotionBlurQuality
    {
        Low,
        Medium,
        High
    }

    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Motion Blur", typeof(UniversalRenderPipeline))]
    public sealed class MotionBlur : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The motion blur technique to use. If you don't need object motion blur, CameraOnly will result in better performance.")]
        public MotionBlurModeParameter mode = new MotionBlurModeParameter(MotionBlurMode.CameraOnly);

        [Tooltip("The quality of the effect. Lower presets will result in better performance at the expense of visual quality.")]
        public MotionBlurQualityParameter quality = new MotionBlurQualityParameter(MotionBlurQuality.Low);

        [Tooltip("The strength of the motion blur filter. Acts as a multiplier for velocities.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the velocity resulting from Camera rotation can have. Lower values will improve performance.")]
        public ClampedFloatParameter clamp = new ClampedFloatParameter(0.05f, 0f, 0.2f);

        public bool IsActive() => intensity.value > 0f && mode.value == MotionBlurMode.CameraOnly;

        public bool IsTileCompatible() => false;
    }

    [Serializable]
    public sealed class MotionBlurModeParameter : VolumeParameter<MotionBlurMode> { public MotionBlurModeParameter(MotionBlurMode value, bool overrideState = false) : base(value, overrideState) { } }

    [Serializable]
    public sealed class MotionBlurQualityParameter : VolumeParameter<MotionBlurQuality> { public MotionBlurQualityParameter(MotionBlurQuality value, bool overrideState = false) : base(value, overrideState) { } }
}
