using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Panini Projection effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Panini Projection")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Post-Processing-Panini-Projection" + Documentation.endURL)]
    public sealed class PaniniProjection : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the panini projection distance. This controls the strength of the distorion.
        /// </summary>
        [Tooltip("Controls the panini projection distance. This controls the strength of the distorion.")]
        public ClampedFloatParameter distance = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls how much cropping HDRP applies to the screen with the panini projection effect. A value of 1 crops the distortion to the edge of the screen.
        /// </summary>
        [Tooltip("Controls how much cropping HDRP applies to the screen with the panini projection effect. A value of 1 crops the distortion to the edge of the screen.")]
        public ClampedFloatParameter cropToFit = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return distance.value > 0f;
        }
    }
}
