using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Chromatic Aberration effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Chromatic Aberration")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Post-Processing-Chromatic-Aberration" + Documentation.endURL)]
    public sealed class ChromaticAberration : VolumeComponentWithQuality, IPostProcessComponent
    {
        /// <summary>
        /// Specifies a Texture which HDRP uses to shift the hue of chromatic aberrations.
        /// </summary>
        [Tooltip("Specifies a Texture which HDRP uses to shift the hue of chromatic aberrations.")]
        public TextureParameter spectralLut = new TextureParameter(null);

        /// <summary>
        /// Controls the strength of the chromatic aberration effect.
        /// </summary>
        [Tooltip("Controls the strength of the chromatic aberration effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls the maximum number of samples HDRP uses to render the effect. A lower sample number results in better performance.
        /// </summary>
        public int maxSamples
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_MaxSamples.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().ChromaticAberrationMaxSamples[qualityLevel];
                }
            }
            set { m_MaxSamples.value = value; }
        }

        [Tooltip("Controls the maximum number of samples HDRP uses to render the effect. A lower sample number results in better performance.")]
        [SerializeField, FormerlySerializedAs("maxSamples")]
        private ClampedIntParameter m_MaxSamples = new ClampedIntParameter(6, 3, 24);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }
}
