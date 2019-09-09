using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum BloomResolution : int
    {
        Quarter = 4,
        Half = 2
    }

    [Serializable, VolumeComponentMenu("Post-processing/Bloom")]
    public sealed class Bloom : VolumeComponentWithQuality, IPostProcessComponent
    {
        [Tooltip("Controls the strength of the bloom filter.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls the extent of the veiling effect.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Tooltip("Specifies the tint of the bloom filter.")]
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

        [Tooltip("Specifies a Texture to add smudges or dust to the bloom effect.")]
        public TextureParameter dirtTexture = new TextureParameter(null);

        [Tooltip("Controls the strength of the lens dirt.")]
        public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

        [Tooltip("When enabled, bloom is more stable when you use high anamorphism factors or when you set the resolution to Quarter.")]
        public BoolParameter prefilter = new BoolParameter(false);

        [Tooltip("When enabled, bloom stretches horizontally depending on the current physical Camera's Anamorphism property value.")]
        public BoolParameter anamorphic = new BoolParameter(true);

        public BloomResolution resolution
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_Resolution.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().BloomRes[qualityLevel];
                }
            }
            set { m_Resolution.value = value; }
        }

        public bool highQualityFiltering
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_HighQualityFiltering.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().BloomHighQualityFiltering[qualityLevel];
                }
            }
            set { m_HighQualityFiltering.value = value; }
        }


        [Tooltip("Specifies the resolution at which HDRP processes the effect. Quarter resolution is less resource intensive.")]
        [SerializeField, FormerlySerializedAs("resolution")]
        private BloomResolutionParameter m_Resolution = new BloomResolutionParameter(BloomResolution.Half);

        [Tooltip("When enabled, bloom uses bicubic sampling instead of bilinear sampling for the upsampling passes.")]
        [SerializeField, FormerlySerializedAs("highQualityFiltering")]
        private BoolParameter m_HighQualityFiltering = new BoolParameter(true);

        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }

    [Serializable]
    public sealed class BloomResolutionParameter : VolumeParameter<BloomResolution> { public BloomResolutionParameter(BloomResolution value, bool overrideState = false) : base(value, overrideState) { } }
}
