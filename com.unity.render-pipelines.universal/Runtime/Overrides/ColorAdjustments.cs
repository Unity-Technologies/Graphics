using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Color Adjustments")]
    public sealed class ColorAdjustments : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Adjusts the overall exposure of the scene in EV100. This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.")]
        public FloatParameter postExposure = new FloatParameter(0f);

        [Tooltip("Expands or shrinks the overall range of tonal values.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);

        [Tooltip("Tint the render by multiplying a color.")]
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);

        [Tooltip("Shift the hue of all colors.")]
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);

        [Tooltip("Pushes the intensity of all colors.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);

        public bool IsActive()
        {
            return postExposure.value != 0f
                || contrast.value != 0f
                || colorFilter != Color.white
                || hueShift != 0f
                || saturation != 0f;
        }

        public bool IsTileCompatible() => true;
    }
}
