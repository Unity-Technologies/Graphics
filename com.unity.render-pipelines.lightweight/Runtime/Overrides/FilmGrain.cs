using System;

namespace UnityEngine.Rendering.LWRP
{
    // TODO: Add presets like HDRP once the resource loading PR goes in
    [Serializable, VolumeComponentMenu("Post-processing/FilmGrain")]
    public sealed class FilmGrain : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("A tileable texture to use for the grain. The neutral value is 0.5 where no grain is applied.")]
        public NoInterpTextureParameter texture = new NoInterpTextureParameter(null);

        [Tooltip("Amount of vignetting on screen.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls the noisiness response curve based on scene luminance. Higher values mean less noise in light areas.")]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);

        public bool IsActive() => intensity.value > 0f && texture.value != null;

        public bool IsTileCompatible() => true;
    }
}
