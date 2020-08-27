using System;

namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Sky/Gradient Sky")]
    [SkyUniqueID((int)SkyType.Gradient)]
    public class GradientSky : SkySettings
    {
        [Tooltip("Specifies the color of the upper hemisphere of the sky.")]
        public ColorParameter top = new ColorParameter(Color.blue, true, false, true);
        [Tooltip("Specifies the color at the horizon.")]
        public ColorParameter middle = new ColorParameter(new Color(0.3f, 0.7f, 1f), true, false, true);
        [Tooltip("Specifies the color of the lower hemisphere of the sky. This is below the horizon.")]
        public ColorParameter bottom = new ColorParameter(Color.white, true, false, true);
        [Tooltip("Sets the size of the horizon (Middle color).")]
        public FloatParameter gradientDiffusion = new FloatParameter(1);

        // TODO Hash

        public override Type GetSkyRendererType() { return typeof(GradientSkyRenderer); }
    }
}
