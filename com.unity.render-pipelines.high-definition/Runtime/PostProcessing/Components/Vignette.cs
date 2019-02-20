using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum VignetteMode
    {
        Procedural,
        Masked
    }

    [Serializable, VolumeComponentMenu("Post-processing/Vignette")]
    public sealed class Vignette : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Use the \"Procedural\" mode for parametric controls. Use the \"Masked\" mode to use your own texture mask.")]
        public VignetteModeParameter mode = new VignetteModeParameter(VignetteMode.Procedural);

        [Tooltip("Vignette color.")]
        public ColorParameter color = new ColorParameter(Color.black, false, false, true);

        [Tooltip("Sets the vignette center point (screen center is [0.5,0.5]).")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("Amount of vignetting on screen.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Smoothness of the vignette borders.")]
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

        [Tooltip("Lower values will make a square-ish vignette.")]
        public ClampedFloatParameter roundness = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Should the vignette be perfectly round or be dependent on the current aspect ratio?")]
        public BoolParameter rounded = new BoolParameter(false);

        [Tooltip("A black and white mask to use as a vignette.")]
        public TextureParameter mask = new TextureParameter(null);

        [Range(0f, 1f), Tooltip("Mask opacity.")]
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1f, 0f, 1f);

        public bool IsActive()
        {
            return (mode.value == VignetteMode.Procedural && intensity.value > 0f)
                || (mode.value == VignetteMode.Masked && opacity.value > 0f && mask.value != null);
        }
    }

    [Serializable]
    public sealed class VignetteModeParameter : VolumeParameter<VignetteMode> { public VignetteModeParameter(VignetteMode value, bool overrideState = false) : base(value, overrideState) { } }
}
