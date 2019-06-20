using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Lens Distortion")]
    public sealed class LensDistortion : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Controls the overall strength of the distortion effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);

        [Tooltip("Controls the distortion intensity on the x-axis. Acts as a multiplier.")]
        public ClampedFloatParameter xMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Controls the distortion intensity on the x-axis. Acts as a multiplier.")]
        public ClampedFloatParameter yMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Sets the center point for the distortion.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("Controls global screen scaling for the distortion effect. Use this to hide the screen borders when using a high \"Intensity\".")]
        public ClampedFloatParameter scale = new ClampedFloatParameter(1f, 0.01f, 5f);

        public bool IsActive()
        {
            return !Mathf.Approximately(intensity.value, 0f)
                && (xMultiplier.value > 0f || yMultiplier.value > 0f);
        }
    }
}
