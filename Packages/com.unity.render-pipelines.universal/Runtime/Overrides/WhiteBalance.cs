using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the White Balance effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/White Balance", typeof(UniversalRenderPipeline))]
    [URPHelpURL("Post-Processing-White-Balance")]
    public sealed class WhiteBalance : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the color temperature URP uses for white balancing.
        /// </summary>
        [Tooltip("Sets the white balance to a custom color temperature.")]
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);

        /// <summary>
        /// Controls the white balance color to compensate for a green or magenta tint.
        /// </summary>
        [Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);

        /// <inheritdoc/>
        public bool IsActive() => temperature.value != 0f || tint.value != 0f;

        /// <inheritdoc/>
        public bool IsTileCompatible() => true;
    }
}
