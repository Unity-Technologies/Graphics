using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Chromatic Aberration")]
    public sealed class ChromaticAberration : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Amount of tangential distortion.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        public bool IsActive() => intensity.value > 0f;

        public bool IsTileCompatible() => false;
    }
}
