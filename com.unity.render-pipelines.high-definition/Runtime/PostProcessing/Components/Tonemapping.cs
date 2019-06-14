using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum TonemappingMode
    {
        None,
        Neutral, // Neutral tonemapper
        ACES,    // ACES Filmic reference tonemapper (custom approximation)
        Custom   // Tweakable artist-friendly curve
    }

    [Serializable, VolumeComponentMenu("Post-processing/Tonemapping")]
    public sealed class Tonemapping : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Specifies the tonemapping algorithm to use for the color grading process.")]
        public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);

        [Tooltip("Controls the transition between the toe and the mid section of the curve. A value of 0 results in no transition and a value of 1 results in a very hard transition.")]
        public ClampedFloatParameter toeStrength = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls how much of the dynamic range is in the toe. Higher values result in longer toes and therefore contain more of the dynamic range.")]
        public ClampedFloatParameter toeLength = new ClampedFloatParameter(0.5f, 0f, 1f);

        [Tooltip("Controls the transition between the midsection and the shoulder of the curve. A value of 0 results in no transition and a value of 1 results in a very hard transition.")]
        public ClampedFloatParameter shoulderStrength = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Sets how many F-stops (EV) to add to the dynamic range of the curve.")]
        public MinFloatParameter shoulderLength = new MinFloatParameter(0.5f, 0f);

        [Tooltip("Controls how much overshoot to add to the shoulder.")]
        public ClampedFloatParameter shoulderAngle = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Sets a gamma correction value that HDRP applies to the whole curve.")]
        public MinFloatParameter gamma = new MinFloatParameter(1f, 0.001f);

        public bool IsActive()
        {
            return mode.value != TonemappingMode.None;
        }
    }

    [Serializable]
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { } }
}
