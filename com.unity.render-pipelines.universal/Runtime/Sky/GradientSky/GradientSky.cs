using System;

namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Sky/Gradient Sky")]
    [SkyUniqueID((int)SkyType.Gradient)]
    public class GradientSky : SkySettings
    {
        // TODO

        public override Type GetSkyRendererType() { return typeof(GradientSkyRenderer); }
    }
}
