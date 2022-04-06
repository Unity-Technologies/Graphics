using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A volume component that holds settings for the White Balance effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/White Balance")]
    [RPRedirectHelpURLAttribute("Post-Processing-White-Balance")]
    public class WhiteBalance : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the color temperature HDRP uses for white balancing.
        /// </summary>
        [Tooltip("Controls the color temperature HDRP uses for white balancing.")]
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);

        /// <summary>
        /// Controls the white balance color to compensate for a green or magenta tint.
        /// </summary>
        [Tooltip("Controls the white balance color to compensate for a green or magenta tint.")]
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return !Mathf.Approximately(temperature.value, 0f)
                || !Mathf.Approximately(tint.value, 0f);
        }

        public bool IsTileCompatible() => true;

        public Type GetNewComponentType()
        {
            return typeof(WhiteBalance);
        }

        public void CopyToNewComponent(VolumeComponent volumeComponent)
        {
            if (volumeComponent is not WhiteBalance wb)
                return;

            wb.active = active;
            wb.hideFlags = hideFlags;
            wb.displayName = displayName;
            wb.temperature = temperature;
            wb.tint = tint;
        }
    }
}
