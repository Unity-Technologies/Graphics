using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : RenderPipelineGlobalSettings
    {
        [SerializeField]
        [FormerlySerializedAs("shaderVariantLogLevel"), Obsolete("Use logShaderVariants")]
        internal ShaderVariantLogLevel m_ObsoleteShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;
    }

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

    /// <summary>
    /// Volume debug settings.
    /// </summary>
    [Obsolete("VolumeDebugSettings has been deprecated. Use HDVolumeDebugSettings instead (UnityUpgradable) -> HDVolumeDebugSettings")]
    public class VolumeDebugSettings : HDVolumeDebugSettings
    {
    }
}
