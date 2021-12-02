using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    public interface IChromaticAberrationGlobalSettingsProvider
    {
        public int GetMaxSamples(int qualityLevel);
    }

    /// <summary>
    /// A volume component that holds settings for the Chromatic Aberration effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Chromatic Aberration")]
   // [HDRPHelpURLAttribute("Post-Processing-Chromatic-Aberration")]

    public class ChromaticAberration : VolumeComponentWithQuality, IPostProcessComponent
    {
        /// <summary>
        /// Specifies a Texture which HDRP uses to shift the hue of chromatic aberrations.
        /// </summary>
        [Tooltip("Specifies a Texture which HDRP uses to shift the hue of chromatic aberrations.")]
        public Texture2DParameter spectralLut = new(null);

        /// <summary>
        /// Controls the strength of the chromatic aberration effect.
        /// </summary>
        [Tooltip("Use the slider to set the strength of the Chromatic Aberration effect.")]
        public ClampedFloatParameter intensity = new(0f, 0f, 1f);

        /// <summary>
        /// Controls the maximum number of samples HDRP uses to render the effect. A lower sample number results in better performance.
        /// </summary>
        public int GetMaxSamples(IChromaticAberrationGlobalSettingsProvider provider = null)
        {
            if (provider == null || !UsesQualitySettings())
                return m_MaxSamples.value;

            int qualityLevel = quality.levelAndOverride.level;
            return provider.GetMaxSamples(qualityLevel);
        }

        public int maxSamples
        {
            [Obsolete("Use GetMaxSamples()")]
            get => GetMaxSamples();
            set => m_MaxSamples.value = value;
        }

        [Tooltip("Controls the maximum number of samples HDRP uses to render the effect. A lower sample number results in better performance.")]
        [SerializeField, FormerlySerializedAs("maxSamples")]
        private ClampedIntParameter m_MaxSamples = new(6, 3, 24);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return intensity.value > 0f;
        }

        public bool IsTileCompatible() => false;

        public Type GetNewComponentType()
        {
            return typeof(ChromaticAberration);
        }

        public void CopyToNewComponent(VolumeComponent volumeComponent)
        {
            if (volumeComponent is not ChromaticAberration lens)
                return;

            lens.active = active;
            lens.displayName = displayName;
            lens.hideFlags = hideFlags;
            lens.intensity = intensity;
            lens.quality = quality;
            lens.spectralLut = spectralLut;
            lens.m_MaxSamples = m_MaxSamples;
        }
    }

}
