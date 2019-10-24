using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Sky/HDRI Sky")]
    [SkyUniqueID((int)SkyType.HDRI)]
    public class HDRISky : SkySettings
    {
        [Tooltip("Specify the cubemap HDRP uses to render the sky.")]
        public CubemapParameter hdriSky = new CubemapParameter(null);

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = hdriSky.value != null ? hash * 23 + hdriSky.GetHashCode() : hash;
            }

            return hash;
        }

        public override Type GetSkyRendererType() { return typeof(HDRISkyRenderer); }
    }
}
