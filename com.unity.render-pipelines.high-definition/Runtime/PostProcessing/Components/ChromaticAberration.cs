using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Chromatic Aberration")]
    public sealed class ChromaticAberration : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Speficies a Texture which HDRP uses to shift the hue of chromatic aberrations.")]
        public TextureParameter spectralLut = new TextureParameter(null);

        [Tooltip("Controls the strength of the chromatic aberration effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls the maximum number of samples HDRP uses to render the effect. A lower sample number results in better performance.")]
        public ClampedIntParameter maxSamples = new ClampedIntParameter(8, 3, 24);

        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }
}
