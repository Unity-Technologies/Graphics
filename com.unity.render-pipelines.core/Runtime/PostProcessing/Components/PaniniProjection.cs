using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A volume component that holds settings for the Panini Projection effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Panini Projection")]
    [RPRedirectHelpURLAttribute("Post-Processing-Panini-Projection")]
    public class PaniniProjection : VolumeComponent, IPostProcessComponent
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
        [Indent]
        public ClampedFloatParameter cropToFit = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return distance.value > 0f;
        }

        public Type GetNewComponentType()
        {
            return typeof(PaniniProjection);
        }

        public void CopyToNewComponent(VolumeComponent volumeComponent)
        {
            if (volumeComponent is not PaniniProjection panini)
                return;

            panini.active = active;
            panini.hideFlags = hideFlags;
            panini.displayName = displayName;
            panini.distance = distance;
            panini.cropToFit = cropToFit;
        }
    }
}
