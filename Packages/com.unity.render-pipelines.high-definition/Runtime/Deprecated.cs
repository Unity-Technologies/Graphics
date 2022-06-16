using System;
using System.Linq;

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

        /// <summary>List of Full Screen Lighting RTAS Debug view names.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static GUIContent[] lightingFullScreenRTASDebugViewStrings => Enum.GetNames(typeof(RTASDebugView)).Select(t => new GUIContent(t)).ToArray();

        /// <summary>List of Full Screen Lighting RTAS Debug view values.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static int[] lightingFullScreenRTASDebugViewValues => (int[])Enum.GetValues(typeof(RTASDebugView));

        /// <summary>List of Full Screen Lighting RTAS Debug mode names.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static GUIContent[] lightingFullScreenRTASDebugModeStrings => Enum.GetNames(typeof(RTASDebugMode)).Select(t => new GUIContent(t)).ToArray();

        /// <summary>List of Full Screen Lighting RTAS Debug mode values.</summary>
        [Obsolete("Use autoenum instead @from(2022.2)")]
        public static int[] lightingFullScreenRTASDebugModeValues => (int[])Enum.GetValues(typeof(RTASDebugMode));
    }

    /// <summary>
    /// AmbientOcclusion has been renamed. Use ScreenSpaceAmbientOcclusion instead
    /// </summary>
    [Obsolete("AmbientOcclusion has been renamed. Use ScreenSpaceAmbientOcclusion instead @from(2022.2) (UnityUpgradable) -> ScreenSpaceAmbientOcclusion")]
    public sealed class AmbientOcclusion
    {
    }

    /// <summary>Deprecated DiffusionProfileOverride</summary>
    [Obsolete("DiffusionProfileOverride has been deprecated @from(2022.2) (UnityUpgradable) -> DiffusionProfileList", false)]
    public sealed class DiffusionProfileOverride
    {
    }
}
