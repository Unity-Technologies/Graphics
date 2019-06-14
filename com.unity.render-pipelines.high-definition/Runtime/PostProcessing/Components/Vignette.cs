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
        [Tooltip("Specifies the mode HDRP uses to display the vignette effect.")]
        public VignetteModeParameter mode = new VignetteModeParameter(VignetteMode.Procedural);

        [Tooltip("Specifies the color of the vignette.")]
        public ColorParameter color = new ColorParameter(Color.black, false, false, true);

        [Tooltip("Sets the center point for the vignette.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("Controls the strength of the vignette effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Controls the smoothness of the vignette borders.")]
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

        [Tooltip("Controls how round the vignette is, lower values result in a more square vignette.")]
        public ClampedFloatParameter roundness = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("When enabled, the vignette is perfectly round. When disabled, the vignette matches shape with the current aspect ratio.")]
        public BoolParameter rounded = new BoolParameter(false);

        [Tooltip("Specifies a black and white mask Texture to use as a vignette.")]
        public TextureParameter mask = new TextureParameter(null);

        [Range(0f, 1f), Tooltip("Controls the opacity of the mask vignette. Lower values result in a more transparent vignette.")]
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
