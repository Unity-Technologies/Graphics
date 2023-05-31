using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Color Adjustments effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Color Curves", typeof(UniversalRenderPipeline))]
    [URPHelpURL("Post-Processing-Color-Curves")]
    public sealed class ColorCurves : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Affects the luminance across the whole image.
        /// </summary>
        [Tooltip("Affects the luminance across the whole image.")]
        public TextureCurveParameter master = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the red channel intensity across the whole image.
        /// </summary>
        [Tooltip("Affects the red channel intensity across the whole image.")]
        public TextureCurveParameter red = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the green channel intensity across the whole image.
        /// </summary>
        [Tooltip("Affects the green channel intensity across the whole image.")]
        public TextureCurveParameter green = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the blue channel intensity across the whole image.
        /// </summary>
        [Tooltip("Affects the blue channel intensity across the whole image.")]
        public TextureCurveParameter blue = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Shifts the input hue (x-axis) according to the output hue (y-axis).
        /// </summary>
        [Tooltip("Shifts the input hue (x-axis) according to the output hue (y-axis).")]
        public TextureCurveParameter hueVsHue = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, true, new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input hue (x-axis).
        /// </summary>
        [Tooltip("Adjusts saturation (y-axis) according to the input hue (x-axis).")]
        public TextureCurveParameter hueVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, true, new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input saturation (x-axis).
        /// </summary>
        [Tooltip("Adjusts saturation (y-axis) according to the input saturation (x-axis).")]
        public TextureCurveParameter satVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input luminance (x-axis).
        /// </summary>
        [Tooltip("Adjusts saturation (y-axis) according to the input luminance (x-axis).")]
        public TextureCurveParameter lumVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, false, new Vector2(0f, 1f)));

        /// <inheritdoc/>
        public bool IsActive() => true;

        /// <inheritdoc/>
        public bool IsTileCompatible() => true;
    }
}
