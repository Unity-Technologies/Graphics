using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// The mode HDRP uses to display the vignette effect.
    /// </summary>
    /// <seealso cref="Vignette.mode"/>
    public enum VignetteMode
    {
        /// <summary>
        /// Controls the position, shape, and intensity of the vignette.
        /// </summary>
        Procedural,

        /// <summary>
        /// Uses a texture mask to create a custom, irregular vignette effect.
        /// </summary>
        Masked
    }

    /// <summary>
    /// A volume component that holds settings for the Vignette effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Vignette")]
    public sealed class Vignette : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Specifies the mode HDRP uses to display the vignette effect.
        /// </summary>
        /// <seealso cref="VignetteMode"/>
        [Tooltip("Specifies the mode HDRP uses to display the vignette effect.")]
        public VignetteModeParameter mode = new VignetteModeParameter(VignetteMode.Procedural);

        /// <summary>
        /// Specifies the color of the vignette.
        /// </summary>
        [Tooltip("Specifies the color of the vignette.")]
        public ColorParameter color = new ColorParameter(Color.black, false, false, true);

        /// <summary>
        /// Sets the center point for the vignette.
        /// </summary>
        [Tooltip("Sets the center point for the vignette.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        /// <summary>
        /// Controls the strength of the vignette effect.
        /// </summary>
        [Tooltip("Controls the strength of the vignette effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls the smoothness of the vignette borders.
        /// </summary>
        [Tooltip("Controls the smoothness of the vignette borders.")]
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

        /// <summary>
        /// Controls how round the vignette is, lower values result in a more square vignette.
        /// </summary>
        [Tooltip("Controls how round the vignette is, lower values result in a more square vignette.")]
        public ClampedFloatParameter roundness = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// When enabled, the vignette is perfectly round. When disabled, the vignette matches shape with the current aspect ratio.
        /// </summary>
        [Tooltip("When enabled, the vignette is perfectly round. When disabled, the vignette matches shape with the current aspect ratio.")]
        public BoolParameter rounded = new BoolParameter(false);

        /// <summary>
        /// Specifies a black and white mask Texture to use as a vignette.
        /// </summary>
        [Tooltip("Specifies a black and white mask Texture to use as a vignette.")]
        public TextureParameter mask = new TextureParameter(null);

        /// <summary>
        /// Controls the opacity of the mask vignette. Lower values result in a more transparent vignette.
        /// </summary>
        [Range(0f, 1f), Tooltip("Controls the opacity of the mask vignette. Lower values result in a more transparent vignette.")]
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return (mode.value == VignetteMode.Procedural && intensity.value > 0f)
                || (mode.value == VignetteMode.Masked && opacity.value > 0f && mask.value != null);
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="VignetteMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class VignetteModeParameter : VolumeParameter<VignetteMode>
    {
        /// <summary>
        /// Creates a new <see cref="VignetteModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public VignetteModeParameter(VignetteMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
