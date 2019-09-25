using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Post-processing/Chromatic Aberration")]
    public sealed class ChromaticAberration : VolumeComponentWithQuality, IPostProcessComponent
    {
        [Tooltip("Speficies a Texture which HDRP uses to shift the hue of chromatic aberrations.")]
        public TextureParameter spectralLut = new TextureParameter(null);

        [Tooltip("Controls the strength of the chromatic aberration effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

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

        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }
}
