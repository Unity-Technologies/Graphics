using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Lift, Gamma, Gain")]
    public sealed class LiftGammaGain : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Controls the darkest portions of the render.")]
        public Vector4Parameter lift = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        [Tooltip("Power function that controls mid-range tones.")]
        public Vector4Parameter gamma = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        [Tooltip("Controls the lightest portions of the render.")]
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
