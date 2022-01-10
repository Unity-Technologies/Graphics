using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Post-processing/AutoLensFlare")]
    public sealed class AutoLensFlare : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the effectiveness of The Director
        /// </summary>
        [Tooltip("Intensity")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 1f);
		[Tooltip("Scale")]
        public ClampedFloatParameter blurSize = new ClampedFloatParameter(1f, 0f, 16f);
        public ClampedIntParameter blurSampleCount = new ClampedIntParameter(8, 2, 64);
        public ClampedFloatParameter thingy = new ClampedFloatParameter(1, 0, 1);

        /// <summary>
        /// Mandatory function, cannot have an Override without it
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }

}