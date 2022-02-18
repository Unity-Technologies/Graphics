using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Presets for the <see cref="FilmGrain"/> effect.
    /// </summary>
    /// <seealso cref="FilmGrain.type"/>
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
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Film Grain", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Post-Processing-Film-Grain")]
    public sealed class FilmGrain : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Specifies the type of grain to use. Use <see cref="FilmGrainLookup.Custom"/> to provide your own <see cref="texture"/>.
        /// </summary>
        /// <seealso cref="FilmGrainLookup"/>
        [Tooltip("Specifies the type of grain to use. Select a preset or select \"Custom\" to provide your own Texture.")]
        public FilmGrainLookupParameter type = new FilmGrainLookupParameter(FilmGrainLookup.Thin1);

        /// <summary>
        /// Controls the strength of the film grain effect.
        /// </summary>
        [Tooltip("Use the slider to set the strength of the Film Grain effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls the noisiness response curve. The higher you set this value, the less noise there is in brighter areas.
        /// </summary>
        [Tooltip("Controls the noisiness response curve. The higher you set this value, the less noise there is in brighter areas.")]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);

        /// <summary>
        /// Specifies a tileable Texture to use for the grain. The neutral value for this Texture is 0.5 which means that HDRP does not apply grain at this value.
        /// </summary>
        [Tooltip("Specifies a tileable Texture to use for the grain. The neutral value for this Texture is 0.5 which means that HDRP does not apply grain at this value.")]
        public Texture2DParameter texture = new Texture2DParameter(null);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return intensity.value > 0f
                && (type.value != FilmGrainLookup.Custom || texture.value != null);
        }
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
