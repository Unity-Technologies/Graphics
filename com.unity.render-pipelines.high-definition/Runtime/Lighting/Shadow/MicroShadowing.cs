using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Micro Shadows effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Shadowing/Micro Shadows", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Micro-Shadows")]
    public class MicroShadowing : VolumeComponent
    {
        /// <summary>
        /// When enabled, HDRP processes Micro Shadows for this Volume.
        /// </summary>
        [Tooltip("Enables micro shadows for directional lights.")]
        public BoolParameter enable = new BoolParameter(false);

        /// <summary>
        /// Controls the opacity of the micro shadows.
        /// </summary>
        [Tooltip("Controls the opacity of the micro shadows.")]
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        MicroShadowing()
        {
            displayName = "Micro Shadows";
        }
    }
}
