using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Color Adjustments effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Color Curves")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Post-Processing-Color-Curves" + Documentation.endURL)]
    public sealed class ColorCurves : VolumeComponent, IPostProcessComponent
    {
        // Note: we don't need tooltips as this component uses a custom editor with no use for tooltips

        /// <summary>
        /// Affects the luminance across the whole image.
        /// </summary>
        public TextureCurveParameter master = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the red channel intensity across the whole image.
        /// </summary>
        public TextureCurveParameter red = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the green channel intensity across the whole image.
        /// </summary>
        public TextureCurveParameter green = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the blue channel intensity across the whole image.
        /// </summary>
        public TextureCurveParameter blue = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Shifts the input hue (x-axis) according to the output hue (y-axis).
        /// </summary>
        public TextureCurveParameter hueVsHue = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, true,  new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input hue (x-axis).
        /// </summary>
        public TextureCurveParameter hueVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, true,  new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input saturation (x-axis).
        /// </summary>
        public TextureCurveParameter satVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input luminance (x-axis).
        /// </summary>
        public TextureCurveParameter lumVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] {}, 0.5f, false, new Vector2(0f, 1f)));

#pragma warning disable 414
        [SerializeField]
        int m_SelectedCurve = 0; // Only used to track the currently selected curve in the UI
#pragma warning restore 414

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return true;
        }
    }
}
