using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Color Adjustments")]
    public sealed class ColorAdjustments : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Sets the value that HDRP uses to adjust the overall exposure of the Scene, in EV.")]
        public FloatParameter postExposure = new FloatParameter(0f);
        
        [Tooltip("Controls the overall range of the tonal values.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);

        [Tooltip("Specifies the color that HDRP tints the render to.")]
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);

        [Tooltip("Controls the hue of all colors in the render.")]
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);

        [Tooltip("Controls the intensity of all colors in the render.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);

        public bool IsActive()
        {
            return postExposure.value != 0f
                || contrast.value != 0f
                || colorFilter != Color.white
                || hueShift != 0f
                || saturation != 0f;
        }
    }
}
