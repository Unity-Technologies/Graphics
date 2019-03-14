using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Chromatic Aberration")]
    public sealed class ChromaticAberration : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Shifts the hue of chromatic aberrations.")]
        public TextureParameter spectralLut = new TextureParameter(null);

        [Tooltip("Amount of tangential distortion.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Maximum amount of samples used to render the effect. Lower count means better performance.")]
        public ClampedIntParameter maxSamples = new ClampedIntParameter(8, 3, 24);

        public bool IsActive()
        {
            return intensity > 0f;
        }
    }
}
