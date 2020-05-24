using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Available tonemapping modes.
    /// </summary>
    /// <seealso cref="Tonemapping.mode"/>
    public enum TonemappingMode
    {
        /// <summary>
        /// No tonemapping.
        /// </summary>
        None,

        /// <summary>
        /// Tonemapping mode with minimal impact on color hue and saturation.
        /// </summary>
        Neutral,

        /// <summary>
        /// Close approximation of the reference ACES tonemapper for a more filmic look.
        /// </summary>
        ACES,

        /// <summary>
        /// A tweakable, artist-friendly tonemapping curve.
        /// </summary>
        Custom,

        /// <summary>
        /// Specifies a custom lookup table.
        /// </summary>
        /// <seealso cref="Tonemapping.lutTexture"/>
        /// <seealso cref="Tonemapping.lutContribution"/>
        External
    }

    /// <summary>
    /// A volume component that holds settings for the Tonemapping effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Tonemapping")]
    public sealed class Tonemapping : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Specifies the tonemapping algorithm to use for the color grading process.
        /// </summary>
        /// <seealso cref="TonemappingMode"/>
        [Tooltip("Specifies the tonemapping algorithm to use for the color grading process.")]
        public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);

        /// <summary>
        /// Controls the transition between the toe and the mid section of the curve. A value of 0
        /// results in no transition and a value of 1 results in a very hard transition.
        /// This parameter is only used when <see cref="TonemappingMode.Custom"/> is set.
        /// </summary>
        [Tooltip("Controls the transition between the toe and the mid section of the curve. A value of 0 results in no transition and a value of 1 results in a very hard transition.")]
        public ClampedFloatParameter toeStrength = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls how much of the dynamic range is in the toe. Higher values result in longer
        /// toes and therefore contain more of the dynamic range.
        /// This parameter is only used when <see cref="TonemappingMode.Custom"/> is set.
        /// </summary>
        [Tooltip("Controls how much of the dynamic range is in the toe. Higher values result in longer toes and therefore contain more of the dynamic range.")]
        public ClampedFloatParameter toeLength = new ClampedFloatParameter(0.5f, 0f, 1f);

        /// <summary>
        /// Controls the transition between the midsection and the shoulder of the curve. A value of 0
        /// results in no transition and a value of 1 results in a very hard transition.
        /// This parameter is only used when <see cref="TonemappingMode.Custom"/> is set.
        /// </summary>
        [Tooltip("Controls the transition between the midsection and the shoulder of the curve. A value of 0 results in no transition and a value of 1 results in a very hard transition.")]
        public ClampedFloatParameter shoulderStrength = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Sets how many F-stops (EV) to add to the dynamic range of the curve.
        /// This parameter is only used when <see cref="TonemappingMode.Custom"/> is set.
        /// </summary>
        [Tooltip("Sets how many F-stops (EV) to add to the dynamic range of the curve.")]
        public MinFloatParameter shoulderLength = new MinFloatParameter(0.5f, 0f);

        /// <summary>
        /// Controls how much overshoot to add to the shoulder.
        /// This parameter is only used when <see cref="TonemappingMode.Custom"/> is set.
        /// </summary>
        [Tooltip("Controls how much overshoot to add to the shoulder.")]
        public ClampedFloatParameter shoulderAngle = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Sets a gamma correction value that HDRP applies to the whole curve.
        /// This parameter is only used when <see cref="TonemappingMode.Custom"/> is set.
        /// </summary>
        [Tooltip("Sets a gamma correction value that HDRP applies to the whole curve.")]
        public MinFloatParameter gamma = new MinFloatParameter(1f, 0.001f);

        /// <summary>
        /// A custom 3D texture lookup table to apply.
        /// This parameter is only used when <see cref="TonemappingMode.External"/> is set.
        /// </summary>
        [Tooltip("A custom 3D texture lookup table to apply.")]
        public TextureParameter lutTexture = new TextureParameter(null);

        /// <summary>
        /// How much of the lookup texture will contribute to the color grading effect.
        /// This parameter is only used when <see cref="TonemappingMode.External"/> is set.
        /// </summary>
        [Tooltip("How much of the lookup texture will contribute to the color grading effect.")]
        public ClampedFloatParameter lutContribution = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            if (mode.value == TonemappingMode.External)
                return ValidateLUT() && lutContribution.value > 0f;

            return mode.value != TonemappingMode.None;
        }

        /// <summary>
        /// Validates the format and size of the LUT texture set in <see cref="lutTexture"/>.
        /// </summary>
        /// <returns><c>true</c> if the LUT is valid, <c>false</c> otherwise.</returns>
        public bool ValidateLUT()
        {
            var hdAsset = HDRenderPipeline.currentAsset;
            if (hdAsset == null || lutTexture.value == null)
                return false;

            if (lutTexture.value.width != hdAsset.currentPlatformRenderPipelineSettings.postProcessSettings.lutSize)
                return false;

            bool valid = false;

            switch (lutTexture.value)
            {
                case Texture3D t:
                    valid |= t.width == t.height
                          && t.height == t.depth;
                    break;
                case RenderTexture rt:
                    valid |= rt.dimension == TextureDimension.Tex3D
                          && rt.width == rt.height
                          && rt.height == rt.volumeDepth;
                    break;
            }

            return valid;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="TonemappingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode>
    {
        /// <summary>
        /// Creates a new <see cref="VignetteModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
