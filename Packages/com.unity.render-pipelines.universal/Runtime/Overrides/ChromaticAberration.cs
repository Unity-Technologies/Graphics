using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Chromatic Aberration effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Chromatic Aberration", typeof(UniversalRenderPipeline))]
    [URPHelpURL("post-processing-chromatic-aberration")]
    public sealed class ChromaticAberration : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the strength of the chromatic aberration effect.
        /// </summary>
        [Tooltip("Use the slider to set the strength of the Chromatic Aberration effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <inheritdoc/>
        public bool IsActive() => intensity.value > 0f;

        /// <inheritdoc/>
        public bool IsTileCompatible() => false;
    }
}
