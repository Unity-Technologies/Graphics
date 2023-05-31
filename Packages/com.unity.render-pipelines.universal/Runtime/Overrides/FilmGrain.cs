using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Presets for the <see cref="FilmGrain"/> effect.
    /// </summary>
    public enum FilmGrainLookup
    {
        /// <summary>
        /// Thin grain preset.
        /// </summary>
        Thin1,

        /// <summary>
        /// Thin grain preset.
        /// </summary>
        Thin2,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium1,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium2,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium3,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium4,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium5,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium6,

        /// <summary>
        /// Large grain preset.
        /// </summary>
        Large01,

        /// <summary>
        /// Large grain preset.
        /// </summary>
        Large02,

        /// <summary>
        /// Custom grain preset.
        /// </summary>
        /// <seealso cref="FilmGrain.texture"/>
        Custom
    }

    /// <summary>
    /// A volume component that holds settings for the Film Grain effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Film Grain", typeof(UniversalRenderPipeline))]
    [URPHelpURL("Post-Processing-Film-Grain")]
    public sealed class FilmGrain : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// The type of grain to use. You can select a preset or provide your own texture by selecting Custom.
        /// </summary>
        [Tooltip("The type of grain to use. You can select a preset or provide your own texture by selecting Custom.")]
        public FilmGrainLookupParameter type = new FilmGrainLookupParameter(FilmGrainLookup.Thin1);

        /// <summary>
        /// Use this to set the strength of the Film Grain effect.
        /// </summary>
        [Tooltip("Use the slider to set the strength of the Film Grain effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls the noisiness response curve based on scene luminance. Higher values mean less noise in light areas.
        /// </summary>
        [Tooltip("Controls the noisiness response curve based on scene luminance. Higher values mean less noise in light areas.")]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);

        /// <summary>
        /// A tileable texture to use for the grain. The neutral value is 0.5 where no grain is applied
        /// </summary>
        [Tooltip("A tileable texture to use for the grain. The neutral value is 0.5 where no grain is applied.")]
        public NoInterpTextureParameter texture = new NoInterpTextureParameter(null);

        /// <inheritdoc/>
        public bool IsActive() => intensity.value > 0f && (type.value != FilmGrainLookup.Custom || texture.value != null);

        /// <inheritdoc/>
        public bool IsTileCompatible() => true;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="FilmGrainLookup"/> value.
    /// </summary>
    [Serializable]
    public sealed class FilmGrainLookupParameter : VolumeParameter<FilmGrainLookup>
    {
        /// <summary>
        /// Creates a new <see cref="FilmGrainLookupParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FilmGrainLookupParameter(FilmGrainLookup value, bool overrideState = false) : base(value, overrideState) { }
    }
}
