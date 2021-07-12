using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Lift, Gamma, Gain", typeof(UniversalRenderPipeline))]
    public sealed class LiftGammaGain : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Use this to control and apply a hue to the dark tones. This has a more exaggerated effect on shadows.")]
        public Vector4Parameter lift = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        [Tooltip("Use this to control and apply a hue to the mid-range tones with a power function.")]
        public Vector4Parameter gamma = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        [Tooltip("Use this to increase and apply a hue to the signal and make highlights brighter.")]
        public Vector4Parameter gain = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return lift != defaultState
                || gamma != defaultState
                || gain != defaultState;
        }

        public bool IsTileCompatible() => true;
    }
}
