using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Lens Distortion effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Lens Distortion")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Post-Processing-Lens-Distortion")]
    public sealed class LensDistortion : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the overall strength of the distortion effect.
        /// </summary>
        [Tooltip("Controls the overall strength of the distortion effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);

        /// <summary>
        /// Controls the distortion intensity on the x-axis. Acts as a multiplier.
        /// </summary>
        [Tooltip("Controls the distortion intensity on the x-axis. Acts as a multiplier.")]
        public ClampedFloatParameter xMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Controls the distortion intensity on the x-axis. Acts as a multiplier.
        /// </summary>
        [Tooltip("Controls the distortion intensity on the x-axis. Acts as a multiplier.")]
        public ClampedFloatParameter yMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Sets the center point for the distortion.
        /// </summary>
        [Tooltip("Distortion center point. 0.5,0.5 is center of the screen.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        /// <summary>
        /// Controls global screen scaling for the distortion effect. Use this to hide the screen borders when using high <see cref="intensity"/>.
        /// </summary>
        [Tooltip("Controls global screen scaling for the distortion effect. Use this to hide the screen borders when using high \"Intensity\".")]
        public ClampedFloatParameter scale = new ClampedFloatParameter(1f, 0.01f, 5f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return Mathf.Abs(intensity.value) > 0
                && (xMultiplier.value > 0f || yMultiplier.value > 0f);
        }
    }
}
