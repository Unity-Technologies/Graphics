using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Vignette effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Vignette", typeof(UniversalRenderPipeline))]
    [URPHelpURL("post-processing-vignette")]
    public sealed class Vignette : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Specifies the color of the vignette.
        /// </summary>
        [Tooltip("Vignette color.")]
        public ColorParameter color = new ColorParameter(Color.black, false, false, true);

        /// <summary>
        /// Sets the center point for the vignette.
        /// </summary>
        [Tooltip("Sets the vignette center point (screen center is [0.5,0.5]).")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        /// <summary>
        /// Controls the strength of the vignette effect.
        /// </summary>
        [Tooltip("Amount of vignetting on screen.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls the smoothness of the vignette borders.
        /// </summary>
        [Tooltip("Smoothness of the vignette borders.")]
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

        /// <summary>
        /// Controls how round the vignette is, lower values result in a more square vignette.
        /// </summary>
        [Tooltip("Should the vignette be perfectly round or be dependent on the current aspect ratio?")]
        public BoolParameter rounded = new BoolParameter(false);

        /// <inheritdoc/>
        public bool IsActive() => intensity.value > 0f;

        /// <inheritdoc/>
        public bool IsTileCompatible() => true;
    }
}
