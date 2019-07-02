using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Panini Projection")]
    public sealed class PaniniProjection : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Controls the panini projection distance. This controls the strength of the distorion.")]
        public ClampedFloatParameter distance = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls how much cropping HDRP applies to the screen with the panini projection effect. A value of 1 crops the distortion to the edge of the screen.")]
        public ClampedFloatParameter cropToFit = new ClampedFloatParameter(1f, 0f, 1f);

        public bool IsActive()
        {
            return distance.value > 0f;
        }
    }
}
