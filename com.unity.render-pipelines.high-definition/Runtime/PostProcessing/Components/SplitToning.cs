using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Split Toning effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Split Toning", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Post-Processing-Split-Toning")]
    public sealed class SplitToning : VolumeComponent, IPostProcessComponent
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
    }
}
