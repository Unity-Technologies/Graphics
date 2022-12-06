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

    /// <summary>Light Layers.</summary>
    [Flags, Obsolete("LightLayersEnum has been renamed and can now use 16 bits. Use RenderingLayerMask instead @from(2023.1) (UnityUpgradable) -> RenderingLayerMask")]
    public enum LightLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Light Layer 0.</summary>
        LightLayerDefault = 1 << 0,
        /// <summary>Light Layer 1.</summary>
        LightLayer1 = 1 << 1,
        /// <summary>Light Layer 2.</summary>
        LightLayer2 = 1 << 2,
        /// <summary>Light Layer 3.</summary>
        LightLayer3 = 1 << 3,
        /// <summary>Light Layer 4.</summary>
        LightLayer4 = 1 << 4,
        /// <summary>Light Layer 5.</summary>
        LightLayer5 = 1 << 5,
        /// <summary>Light Layer 6.</summary>
        LightLayer6 = 1 << 6,
        /// <summary>Light Layer 7.</summary>
        LightLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    /// <summary>Decal Layers.</summary>
    [Flags, Obsolete("DecalLayerEnum has been renamed and can now use 16 bits. Use RenderingLayerMask instead @from(2023.1) (UnityUpgradable) -> RenderingLayerMask")]
    public enum DecalLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Decal Layer 0.</summary>
        DecalLayerDefault = 1 << 0,
        /// <summary>Decal Layer 1.</summary>
        DecalLayer1 = 1 << 1,
        /// <summary>Decal Layer 2.</summary>
        DecalLayer2 = 1 << 2,
        /// <summary>Decal Layer 3.</summary>
        DecalLayer3 = 1 << 3,
        /// <summary>Decal Layer 4.</summary>
        DecalLayer4 = 1 << 4,
        /// <summary>Decal Layer 5.</summary>
        DecalLayer5 = 1 << 5,
        /// <summary>Decal Layer 6.</summary>
        DecalLayer6 = 1 << 6,
        /// <summary>Decal Layer 7.</summary>
        DecalLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    /// <summary>
    /// Debug Light Layers Filtering.
    /// </summary>
    [Flags, Obsolete("DebugLightLayersMask has been renamed and can now use 16 bits. Use RenderingLayerMask instead @from(2023.1)")]
    public enum DebugLightLayersMask
    {
        /// <summary>No light layer debug.</summary>
        None = 0,
        /// <summary>Debug light layer 1.</summary>
        LightLayer1 = 1 << 0,
        /// <summary>Debug light layer 2.</summary>
        LightLayer2 = 1 << 1,
        /// <summary>Debug light layer 3.</summary>
        LightLayer3 = 1 << 2,
        /// <summary>Debug light layer 4.</summary>
        LightLayer4 = 1 << 3,
        /// <summary>Debug light layer 5.</summary>
        LightLayer5 = 1 << 4,
        /// <summary>Debug light layer 6.</summary>
        LightLayer6 = 1 << 5,
        /// <summary>Debug light layer 7.</summary>
        LightLayer7 = 1 << 6,
        /// <summary>Debug light layer 8.</summary>
        LightLayer8 = 1 << 7,
    }
}
