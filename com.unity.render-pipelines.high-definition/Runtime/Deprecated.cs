using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Deprecated DensityVolume</summary>
    [Obsolete("DensityVolume has been deprecated (UnityUpgradable) -> LocalVolumetricFog", true)]
    public class DensityVolume : LocalVolumetricFog
    {
    }

    /// <summary>Deprecated DensityVolumeArtistParameters</summary>
    [Obsolete("DensityVolumeArtistParameters has been deprecated (UnityUpgradable) -> LocalVolumetricFogArtistParameters", true)]
    public struct DensityVolumeArtistParameters
    {
    }

    public partial struct LocalVolumetricFogArtistParameters
    {
        /// <summary>Obsolete, do not use.</summary>
        [Obsolete("Never worked correctly due to having engine working in percent. Will be removed soon.", true)]
        public bool advancedFade => true;
    }

    /// <summary>
    /// Volume debug settings.
    /// </summary>
    [Obsolete("VolumeDebugSettings has been deprecated. Use HDVolumeDebugSettings instead (UnityUpgradable) -> HDVolumeDebugSettings", true)]
    public class VolumeDebugSettings : HDVolumeDebugSettings
    {
    }
}
