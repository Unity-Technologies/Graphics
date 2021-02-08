using System;

namespace UnityEngine.Rendering.Universal
{
    public enum DepthOfFieldMode
    {
        Off,
        Gaussian, // Non physical, fast, small radius, far blur only
        Bokeh
    }

    [Serializable, VolumeComponentMenu("Post-processing/Depth Of Field")]
    public sealed class DepthOfField : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Use \"Gaussian\" for a faster but non physical depth of field; \"Bokeh\" for a more realistic but slower depth of field.")]
        public DepthOfFieldModeParameter mode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

        [Tooltip("The distance at which the blurring will start.")]
        public MinFloatParameter gaussianStart = new MinFloatParameter(10f, 0f);

        [Tooltip("The distance at which the blurring will reach its maximum radius.")]
        public MinFloatParameter gaussianEnd = new MinFloatParameter(30f, 0f);

        [Tooltip("The maximum radius of the gaussian blur. Values above 1 may show under-sampling artifacts.")]
        public ClampedFloatParameter gaussianMaxRadius = new ClampedFloatParameter(1f, 0.5f, 1.5f);

        [Tooltip("Use higher quality sampling to reduce flickering and improve the overall blur smoothness.")]
        public BoolParameter highQualitySampling = new BoolParameter(false);

        [Tooltip("The distance to the point of focus.")]
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);

        [Tooltip("The ratio of aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is.")]
        public ClampedFloatParameter aperture = new ClampedFloatParameter(5.6f, 1f, 32f);

        [Tooltip("The distance between the lens and the film. The larger the value is, the shallower the depth of field is.")]
        public ClampedFloatParameter focalLength = new ClampedFloatParameter(50f, 1f, 300f);

        [Tooltip("The number of aperture blades.")]
        public ClampedIntParameter bladeCount = new ClampedIntParameter(5, 3, 9);

        [Tooltip("The curvature of aperture blades. The smaller the value is, the more visible aperture blades are. A value of 1 will make the bokeh perfectly circular.")]
        public ClampedFloatParameter bladeCurvature = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("The rotation of aperture blades in degrees.")]
        public ClampedFloatParameter bladeRotation = new ClampedFloatParameter(0f, -180f, 180f);

        public bool IsActive()
        {
            if (mode.value == DepthOfFieldMode.Off || SystemInfo.graphicsShaderLevel < 35)
                return false;

            return mode.value != DepthOfFieldMode.Gaussian || SystemInfo.supportedRenderTargetCount > 1;
        }

        public bool IsTileCompatible() => false;
    }

    [Serializable]
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overrideState = false) : base(value, overrideState) {} }
}
