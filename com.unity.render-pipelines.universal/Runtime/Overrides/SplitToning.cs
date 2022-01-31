using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Split Toning effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Split Toning", typeof(UniversalRenderPipeline))]
    public sealed class SplitToning : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// The color to use for shadows.
        /// </summary>
        [Tooltip("The color to use for shadows.")]
        public ColorParameter shadows = new ColorParameter(Color.grey, false, false, true);

        /// <summary>
        /// The color to use for highlights.
        /// </summary>
        [Tooltip("The color to use for highlights.")]
        public ColorParameter highlights = new ColorParameter(Color.grey, false, false, true);

        /// <summary>
        /// Balance between the colors in the highlights and shadows.
        /// </summary>
        [Tooltip("Balance between the colors in the highlights and shadows.")]
        public ClampedFloatParameter balance = new ClampedFloatParameter(0f, -100f, 100f);

        /// <inheritdoc/>
        public bool IsActive() => shadows != Color.grey || highlights != Color.grey;

        /// <inheritdoc/>
        public bool IsTileCompatible() => true;
    }
}
