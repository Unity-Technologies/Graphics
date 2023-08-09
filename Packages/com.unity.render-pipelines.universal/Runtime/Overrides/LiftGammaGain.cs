using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Split Toning effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Lift, Gamma, Gain", typeof(UniversalRenderPipeline))]
    [URPHelpURL("Post-Processing-Lift-Gamma-Gain")]
    public sealed class LiftGammaGain : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Use this to control and apply a hue to the dark tones. This has a more exaggerated effect on shadows.
        /// </summary>
        [Tooltip("Use this to control and apply a hue to the dark tones. This has a more exaggerated effect on shadows.")]
        public Vector4Parameter lift = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to control and apply a hue to the mid-range tones with a power function.
        /// </summary>
        [Tooltip("Use this to control and apply a hue to the mid-range tones with a power function.")]
        public Vector4Parameter gamma = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to increase and apply a hue to the signal and make highlights brighter.
        /// </summary>
        [Tooltip("Use this to increase and apply a hue to the signal and make highlights brighter.")]
        public Vector4Parameter gain = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <inheritdoc/>
        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return lift != defaultState
                || gamma != defaultState
                || gain != defaultState;
        }

        /// <inheritdoc/>
        public bool IsTileCompatible() => true;
    }
}
