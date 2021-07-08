using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Deprecated DensityVolume</summary>
    [Obsolete("DensityVolume has been deprecated (UnityUpgradable) -> LocalVolumetricFog", false)]
    public class DensityVolume : LocalVolumetricFog
    {
    }

    /// <summary>Deprecated DensityVolumeArtistParameters</summary>
    [Obsolete("DensityVolumeArtistParameters has been deprecated (UnityUpgradable) -> LocalVolumetricFogArtistParameters", false)]
    public struct DensityVolumeArtistParameters
    {
    }

    public partial struct LocalVolumetricFogArtistParameters
    {
        /// <summary>Obsolete, do not use.</summary>
        [Obsolete("Never worked correctly due to having engine working in percent. Will be removed soon.")]
        public bool advancedFade => true;
    }
}
