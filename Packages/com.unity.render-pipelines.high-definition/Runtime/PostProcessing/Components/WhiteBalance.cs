using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the White Balance effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/White Balance")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Post-Processing-White-Balance")]
    public sealed class WhiteBalance : VolumeComponent, IPostProcessComponent
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
    }
}
