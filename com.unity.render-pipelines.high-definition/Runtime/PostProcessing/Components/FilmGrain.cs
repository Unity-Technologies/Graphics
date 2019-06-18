using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
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
        [Tooltip("Specifies the type of grain to use. Select a preset or select \"Custom\" to provide your own Texture.")]
        public FilmGrainLookupParameter type = new FilmGrainLookupParameter(FilmGrainLookup.Thin1);

        [Tooltip("Controls the strength of the film grain effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls the noisiness response curve. The higher you set this value, the less noise there is in brighter areas.")]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);

        [Tooltip("Specifies a tileable Texture to use for the grain. The neutral value for this Texture is 0.5 which means that HDRP does not apply grain at this value.")]
        public NoInterpTextureParameter texture = new NoInterpTextureParameter(null);

        public bool IsActive()
        {
            return intensity.value > 0f
                && (type.value != FilmGrainLookup.Custom || texture.value != null);
        }
    }

    [Serializable]
    public sealed class FilmGrainLookupParameter : VolumeParameter<FilmGrainLookup> { public FilmGrainLookupParameter(FilmGrainLookup value, bool overrideState = false) : base(value, overrideState) { } }
}
