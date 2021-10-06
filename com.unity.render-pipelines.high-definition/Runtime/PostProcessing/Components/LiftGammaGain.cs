using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Lift, Gamma, Gain effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Lift, Gamma, Gain", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Post-Processing-Lift-Gamma-Gain")]
    public sealed class LiftGammaGain : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the dark tones of the render.
        /// </summary>
        [Tooltip("Use this to control and apply a hue to the dark tones. This has a more exaggerated effect on shadows.")]
        public Vector4Parameter lift = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Controls the mid-range tones of the render with a power function.
        /// </summary>
        [Tooltip("Use this to control and apply a hue to the mid-range tones with a power function.")]
        public Vector4Parameter gamma = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Controls the highlights of the render.
        /// </summary>
        [Tooltip("Use this to increase and apply a hue to the signal and make highlights brighter.")]
        public Vector4Parameter gain = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return lift != defaultState
                || gamma != defaultState
                || gain != defaultState;
        }

        LiftGammaGain() => displayName = "Lift, Gamma, Gain";
    }
}
