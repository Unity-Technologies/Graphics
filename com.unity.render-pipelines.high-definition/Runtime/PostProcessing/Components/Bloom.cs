using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum BloomResolution : int
    {
        Quarter = 4,
        Half = 2
    }

    [Serializable, VolumeComponentMenu("Post-processing/Bloom")]
    public sealed class Bloom : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Strength of the bloom filter.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Changes the extent of veiling effects.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Tooltip("Global tint of the bloom filter.")]
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

        [Tooltip("Dirtiness texture to add smudges or dust to the bloom effect.")]
        public TextureParameter dirtTexture = new TextureParameter(null);

        [Tooltip("Amount of dirtiness.")]
        public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

        [Tooltip("Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter highQualityFiltering = new BoolParameter(true);

        [Tooltip("The resolution at which the effect will be done. Quarter resolution is faster and recommended for very high screen resolution (e.g. 4K on consoles).")]
        public BloomResolutionParameter resolution = new BloomResolutionParameter(BloomResolution.Half);

        [Tooltip("Improves bloom stability with high anamorphism factors or when the resolution is set to Quarter.")]
        public BoolParameter prefilter = new BoolParameter(false);

        [Tooltip("If true, bloom will use the anamorphism parameter from the current physical camera into account.")]
        public BoolParameter anamorphic = new BoolParameter(true);

        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }

    [Serializable]
    public sealed class BloomResolutionParameter : VolumeParameter<BloomResolution> { public BloomResolutionParameter(BloomResolution value, bool overrideState = false) : base(value, overrideState) { } }
}
