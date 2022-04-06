using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A volume component that holds settings for the Split Toning effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Split Toning")]
    [RPRedirectHelpURLAttribute("Post-Processing-Split-Toning")]
    public class SplitToning : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Specifies the color to use for shadows.
        /// </summary>
        [Tooltip("Specifies the color to use for shadows.")]
        public ColorParameter shadows = new ColorParameter(Color.grey, false, false, true);

        /// <summary>
        /// Specifies the color to use for highlights.
        /// </summary>
        [Tooltip("Specifies the color to use for highlights.")]
        public ColorParameter highlights = new ColorParameter(Color.grey, false, false, true);

        /// <summary>
        /// Controls the balance between the colors in the highlights and shadows.
        /// </summary>
        [Tooltip("Controls the balance between the colors in the highlights and shadows.")]
        public ClampedFloatParameter balance = new ClampedFloatParameter(0f, -100f, 100f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return shadows != Color.grey
                || highlights != Color.grey;
        }

        public bool IsTileCompatible() => true;

        public Type GetNewComponentType()
        {
            return typeof(SplitToning);
        }

        public void CopyToNewComponent(VolumeComponent volumeComponent)
        {
            if (volumeComponent is not SplitToning lens)
                return;

            lens.active = active;
            lens.displayName = displayName;
            lens.hideFlags = hideFlags;
            lens.shadows = shadows;
            lens.highlights = highlights;
            lens.balance = balance;
        }
    }
}
