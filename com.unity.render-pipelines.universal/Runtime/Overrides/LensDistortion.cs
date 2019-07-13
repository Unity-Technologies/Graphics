using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Lens Distortion")]
    public sealed class LensDistortion : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Total distortion amount.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);

        [Tooltip("Intensity multiplier on X axis. Set it to 0 to disable distortion on this axis.")]
        public ClampedFloatParameter xMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Intensity multiplier on Y axis. Set it to 0 to disable distortion on this axis.")]
        public ClampedFloatParameter yMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Distortion center point.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("Global screen scaling.")]
        public ClampedFloatParameter scale = new ClampedFloatParameter(1f, 0.01f, 5f);

        public bool IsActive()
        {
            return !Mathf.Approximately(intensity.value, 0f)
                && (xMultiplier.value > 0f || yMultiplier.value > 0f);
        }

        public bool IsTileCompatible() => false;
    }
}
