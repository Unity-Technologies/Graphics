using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Color Adjustments effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Color Adjustments")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class ColorAdjustments : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Adjusts the overall exposure of the scene in EV100.
        /// This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.
        /// </summary>
        [Tooltip("Adjusts the overall exposure of the scene in EV100. This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.")]
        public FloatParameter postExposure = new FloatParameter(0f);

        /// <summary>
        /// Controls the overall range of the tonal values.
        /// </summary>
        [Tooltip("Expands or shrinks the overall range of tonal values.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);

        /// <summary>
        /// Specifies the color that URP tints the render to.
        /// </summary>
        [Tooltip("Tint the render by multiplying a color.")]
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);

        /// <summary>
        /// Controls the hue of all colors in the render.
        /// </summary>
        [Tooltip("Shift the hue of all colors.")]
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);

        /// <summary>
        /// Controls the intensity of all colors in the render.
        /// </summary>
        [Tooltip("Pushes the intensity of all colors.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);

        /// <inheritdoc/>
        public bool IsActive()
        {
            return postExposure.value != 0f
                || contrast.value != 0f
                || colorFilter != Color.white
                || hueShift != 0f
                || saturation != 0f;
        }

        /// <inheritdoc/>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
