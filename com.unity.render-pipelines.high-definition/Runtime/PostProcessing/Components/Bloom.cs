using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// The resolution at which HDRP processes the bloom effect.
    /// </summary>
    /// <seealso cref="Bloom.resolution"/>
    public enum BloomResolution : int
    {
        /// <summary>
        /// Quarter resolution. Faster than <see cref="Half"/> but can result in aliasing artifacts.
        /// It is recommended to use this mode when targetting high-DPI displays.
        /// </summary>
        Quarter = 4,

        /// <summary>
        /// Half resolution.
        /// </summary>
        Half = 2
    }

    /// <summary>
    /// A volume component that holds settings for the Bloom effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Bloom")]
    public sealed class Bloom : VolumeComponentWithQuality, IPostProcessComponent
    {
        /// <summary>
        /// Set the level of brightness to filter out pixels under this level. This value is expressed in gamma-space. A value above 0 will disregard energy conservation rules.
        /// </summary>
        [Tooltip("Set the level of brightness to filter out pixels under this level. This value is expressed in gamma-space. A value above 0 will disregard energy conservation rules.")]
        public MinFloatParameter threshold = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// Controls the strength of the bloom filter.
        /// </summary>
        [Tooltip("Controls the strength of the bloom filter.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls the extent of the veiling effect.
        /// </summary>
        [Tooltip("Controls the extent of the veiling effect.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        /// <summary>
        /// Specifies the tint of the bloom filter.
        /// </summary>
        [Tooltip("Specifies the tint of the bloom filter.")]
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

        /// <summary>
        /// Specifies a Texture to add smudges or dust to the bloom effect.
        /// </summary>
        [Tooltip("Specifies a Texture to add smudges or dust to the bloom effect.")]
        public TextureParameter dirtTexture = new TextureParameter(null);

        /// <summary>
        /// Controls the strength of the lens dirt.
        /// </summary>
        [Tooltip("Controls the strength of the lens dirt.")]
        public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// When enabled, bloom stretches horizontally depending on the current physical Camera's Anamorphism property value.
        /// </summary>
        [Tooltip("When enabled, bloom stretches horizontally depending on the current physical Camera's Anamorphism property value.")]
        public BoolParameter anamorphic = new BoolParameter(true);

        /// <summary>
        /// Specifies the resolution at which HDRP processes the effect.
        /// </summary>
        /// <seealso cref="BloomResolution"/>
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

        /// <summary>
        /// When enabled, bloom uses bicubic sampling instead of bilinear sampling for the upsampling passes.
        /// </summary>
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

        [Tooltip("Specifies the resolution at which HDRP processes the effect. Quarter resolution is less resource intensive but can result in aliasing artifacts.")]
        [SerializeField, FormerlySerializedAs("resolution")]
        private BloomResolutionParameter m_Resolution = new BloomResolutionParameter(BloomResolution.Half);

        [Tooltip("When enabled, bloom uses bicubic sampling instead of bilinear sampling for the upsampling passes.")]
        [SerializeField, FormerlySerializedAs("highQualityFiltering")]
        private BoolParameter m_HighQualityFiltering = new BoolParameter(true);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="BloomResolution"/> value.
    /// </summary>
    [Serializable]
    public sealed class BloomResolutionParameter : VolumeParameter<BloomResolution>
    {
        /// <summary>
        /// Creates a new <see cref="BloomResolutionParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public BloomResolutionParameter(BloomResolution value, bool overrideState = false) : base(value, overrideState) { }
    }
}
