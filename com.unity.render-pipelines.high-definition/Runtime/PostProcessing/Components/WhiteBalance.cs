using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/White Balance")]
    public sealed class WhiteBalance : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Controls the color temperature HDRP uses for white balancing.")]
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);

        [Tooltip("Controls the white balance color to compensate for a green or magenta tint.")]
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);

        public bool IsActive()
        {
            return !Mathf.Approximately(temperature.value, 0f)
                || !Mathf.Approximately(tint.value, 0f);
        }
    }
}
