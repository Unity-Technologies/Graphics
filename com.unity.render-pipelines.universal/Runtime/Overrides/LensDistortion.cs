using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Lens Distortion", typeof(UniversalRenderPipeline))]
    public sealed class LensDistortion : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Total distortion amount.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);

        [Tooltip("Intensity multiplier on X axis. Set it to 0 to disable distortion on this axis.")]
        public ClampedFloatParameter xMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Intensity multiplier on Y axis. Set it to 0 to disable distortion on this axis.")]
        public ClampedFloatParameter yMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Distortion center point. 0.5,0.5 is center of the screen")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("Controls global screen scaling for the distortion effect. Use this to hide screen borders when using high \"Intensity.\"")]
        public ClampedFloatParameter scale = new ClampedFloatParameter(1f, 0.01f, 5f);

        public bool IsActive()
        {
            return Mathf.Abs(intensity.value) > 0
                && (xMultiplier.value > 0f || yMultiplier.value > 0f);
        }

        public bool IsTileCompatible() => false;
    }
}
