using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Color Adjustments effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Color Adjustments")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Post-Processing-Color-Adjustments" + Documentation.endURL)]
    public sealed class ColorAdjustments : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Adjusts the brightness of the image just before color grading, in EV.
        /// </summary>
        [Tooltip("Adjusts the brightness of the image just before color grading, in EV.")]
        public FloatParameter postExposure = new FloatParameter(0f);

        /// <summary>
        /// Controls the overall range of the tonal values.
        /// </summary>
        [Tooltip("Controls the overall range of the tonal values.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);

        /// <summary>
        /// Specifies the color that HDRP tints the render to.
        /// </summary>
        [Tooltip("Specifies the color that HDRP tints the render to.")]
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);

        /// <summary>
        /// Controls the hue of all colors in the render.
        /// </summary>
        [Tooltip("Controls the hue of all colors in the render.")]
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);

        /// <summary>
        /// Controls the intensity of all colors in the render.
        /// </summary>
        [Tooltip("Controls the intensity of all colors in the render.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return postExposure.value != 0f
                || contrast.value != 0f
                || colorFilter != Color.white
                || hueShift != 0f
                || saturation != 0f;
        }
    }
}
