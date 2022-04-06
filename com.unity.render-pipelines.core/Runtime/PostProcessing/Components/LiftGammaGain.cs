using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A volume component that holds settings for the Lift, Gamma, Gain effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Lift, Gamma, Gain")]
    [RPRedirectHelpURLAttribute("Post-Processing-Lift-Gamma-Gain")]
    public class LiftGammaGain : VolumeComponent, IPostProcessComponent
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

        protected LiftGammaGain() => displayName = "Lift, Gamma, Gain";

        public bool IsTileCompatible() => true;

        public Type GetNewComponentType()
        {
            return typeof(LiftGammaGain);
        }

        public void CopyToNewComponent(VolumeComponent volumeComponent)
        {
            if (volumeComponent is not LiftGammaGain lgg)
                return;

            lgg.active = active;
            lgg.displayName = displayName;
            lgg.hideFlags = hideFlags;
            lgg.lift = lift;
            lgg.gamma = gamma;
            lgg.gain = gain;
        }
    }
}
