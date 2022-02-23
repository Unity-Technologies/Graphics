using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Deprecated DensityVolume</summary>
    [Obsolete("DensityVolume has been deprecated, use LocalVolumetricFog", true)]
    public class DensityVolume : LocalVolumetricFog
    {
    }

    /// <summary>Deprecated DensityVolumeArtistParameters</summary>
    [Obsolete("DensityVolumeArtistParameters has been deprecated, use LocalVolumetricFogArtistParameters", true)]
    public struct DensityVolumeArtistParameters
    {
    }

    public partial struct LocalVolumetricFogArtistParameters
    {
        /// <summary>Obsolete, do not use.</summary>
        [Obsolete("Never worked correctly due to having engine working in percent. Will be removed soon.")]
        public bool advancedFade => true;
    }

    /// <summary>
    /// Volume debug settings.
    /// </summary>
    [Obsolete("VolumeDebugSettings has been deprecated. Use HDVolumeDebugSettings instead (UnityUpgradable) -> HDVolumeDebugSettings")]
    public class VolumeDebugSettings : HDVolumeDebugSettings
    {
    }

    /// <summary>
    /// AmbientOcclusion has been renamed. Use ScreenSpaceAmbientOcclusion instead
    /// </summary>
    [Obsolete("AmbientOcclusion has been renamed. Use ScreenSpaceAmbientOcclusion instead (UnityUpgradable) -> ScreenSpaceAmbientOcclusion")]
    public sealed class AmbientOcclusion : VolumeComponentWithQuality
    {
    }

    /// <summary>
    /// Class managing debug display in HDRP.
    /// </summary>
    public partial class DebugDisplaySettings
    {
        /// <summary>
        /// Debug data.
        /// </summary>
        public partial class DebugData
        {
            /// <summary>Current volume debug settings.</summary>
            [Obsolete("Moved to HDDebugDisplaySettings.Instance. Will be removed soon.")]
            public IVolumeDebugSettings volumeDebugSettings = new HDVolumeDebugSettings();
        }
    }
}
