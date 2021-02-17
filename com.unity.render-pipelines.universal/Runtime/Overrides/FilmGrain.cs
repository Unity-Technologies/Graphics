using System;

namespace UnityEngine.Rendering.Universal
{
    public enum FilmGrainLookup
    {
        Thin1,
        Thin2,
        Medium1,
        Medium2,
        Medium3,
        Medium4,
        Medium5,
        Medium6,
        Large01,
        Large02,
        Custom
    }

    [Serializable, VolumeComponentMenu("Post-processing/FilmGrain")]
    public sealed class FilmGrain : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The type of grain to use. You can select a preset or provide your own texture by selecting Custom.")]
        public FilmGrainLookupParameter type = new FilmGrainLookupParameter(FilmGrainLookup.Thin1);

        [Tooltip("Amount of vignetting on screen.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls the noisiness response curve based on scene luminance. Higher values mean less noise in light areas.")]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);

        [Tooltip("A tileable texture to use for the grain. The neutral value is 0.5 where no grain is applied.")]
        public NoInterpTextureParameter texture = new NoInterpTextureParameter(null);

        public bool IsActive() => intensity.value > 0f && (type.value != FilmGrainLookup.Custom || texture.value != null);

        public bool IsTileCompatible() => true;
    }

    [Serializable]
    public sealed class FilmGrainLookupParameter : VolumeParameter<FilmGrainLookup> { public FilmGrainLookupParameter(FilmGrainLookup value, bool overrideState = false) : base(value, overrideState) {} }
}
