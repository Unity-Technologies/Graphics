using System;

namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Sky/HDRI Sky")]
    [SkyUniqueID((int)SkyType.HDRI)]
    public class HDRISky : SkySettings
    {
        /// <summary>Cubemap used to render the HDRI sky.</summary>
        [Tooltip("Specify the cubemap HDRP uses to render the sky.")]
        public CubemapParameter hdriSky = new CubemapParameter(null);

        // TODO Other params

        /// <summary>
        /// Returns the hash code of the HDRI sky parameters.
        /// </summary>
        /// <returns>The hash code of the HDRI sky parameters.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = hdriSky.value != null ? hash * 23 + hdriSky.GetHashCode() : hash;
            }

            return hash;
        }

        /// <summary>
        /// Returns HDRISkyRenderer type.
        /// </summary>
        /// <returns>HDRISkyRenderer type.</returns>
        public override Type GetSkyRendererType() { return typeof(HDRISkyRenderer); }
    }
}
