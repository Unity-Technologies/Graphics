using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Micro Shadows effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Shadowing/Micro Shadows")]
    public class MicroShadowing : VolumeComponent
    {
        /// <summary>
        /// When enabled, HDRP processes Micro Shadows for this Volume.
        /// </summary>
        public BoolParameter enable = new BoolParameter(false);
        /// <summary>
        /// Controls the opacity of the micro shadows.
        /// </summary>
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        MicroShadowing()
        {
            displayName = "Micro Shadows";
        }
    }
}
