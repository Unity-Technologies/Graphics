using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Possible values for one element of the Local Volumetric Fog atlas.
    /// </summary>
    [Serializable]
    [Obsolete("DensityVolumeResolution has been deprecated (UnityUpgradable) -> LocalVolumetricFogResolution", false)]
    public enum DensityVolumeResolution
    {
        /// <summary>3D volume of 32x32x32 voxels.</summary>
        Resolution32 = 32,
        /// <summary>3D volume of 64x64x64 voxels.</summary>
        Resolution64 = 64,
        /// <summary>3D volume of 128x128x128 voxels.</summary>
        Resolution128 = 128,
        /// <summary>3D volume of 256x256x256 voxels.</summary>
        Resolution256 = 256,
    }

    public partial struct GlobalLightLoopSettings
    {
        // We keep this property for the migration code (we need to know how many cookies we could have before).
        [SerializeField, Obsolete("There is no more texture array for cookies, use cookie atlases properties instead.", false)]
        internal int cookieTexArraySize;

        /// <summary>Maximum size of one Local Volumetric Fog texture.</summary>
        [Obsolete("Use maxLocalVolumetricFogSize instead", false)]
        public DensityVolumeResolution maxDensityVolumeSize
        {
            get => (DensityVolumeResolution)maxLocalVolumetricFogSize;
            set => maxLocalVolumetricFogSize = (LocalVolumetricFogResolution)value;
        }

        /// <summary>Maximum number of Local Volumetric Fog at the same time on screen.</summary>
        [Obsolete("Use maxLocalVolumetricFogsOnScreen instead", false)]
        public int maxDensityVolumesOnScreen
        {
            get => maxLocalVolumetricFogsOnScreen;
            set => maxLocalVolumetricFogsOnScreen = value;
        }
    }

    /// <summary>Deprecated DensityVolume</summary>
    [Obsolete("LocalVolumetricFog has been deprecated (UnityUpgradable) -> Local Volumetric Fog", false)]
    public class DensityVolume : LocalVolumetricFog
    {
    }

    /// <summary></summary>
    [GenerateHLSL]
    [Obsolete("DensityVolumeFalloffMode has been deprecated (UnityUpgradable) -> LocalVolumetricFogFalloffMode", false)]
    public enum DensityVolumeFalloffMode
    {
        /// <summary></summary>
        Linear,
        /// <summary></summary>
        Exponential,
    }

    /// <summary>Deprecated DensityVolumeArtistParameters</summary>
    [Obsolete("DensityVolumeArtistParameters has been deprecated (UnityUpgradable) -> LocalVolumetricFogArtistParameters", false)]
    public struct DensityVolumeArtistParameters
    {
        LocalVolumetricFogArtistParameters m_Parameters;

        /// <summary>Single scattering albedo: [0, 1]. Alpha is ignored.</summary>
        public Color albedo
        {
            get => m_Parameters.albedo;
            set => m_Parameters.albedo = value;
        }

        /// <summary>Mean free path, in meters: [1, inf].</summary>
        public float meanFreePath
        {
            get => m_Parameters.meanFreePath;
            set => m_Parameters.meanFreePath = value;
        }

        /// <summary>Anisotropy of the phase function: [-1, 1]. Positive values result in forward scattering, and negative values - in backward scattering.</summary>
        public float anisotropy
        {
            get => m_Parameters.anisotropy;
            set => m_Parameters.anisotropy = value;
        }

        /// <summary>Texture containing density values.</summary>
        public Texture volumeMask
        {
            get => m_Parameters.volumeMask;
            set => m_Parameters.volumeMask = value;
        }

        /// <summary>Scrolling speed of the density texture.</summary>
        public Vector3 textureScrollingSpeed
        {
            get => m_Parameters.textureScrollingSpeed;
            set => m_Parameters.textureScrollingSpeed = value;
        }

        /// <summary>Tiling rate of the density texture.</summary>
        public Vector3 textureTiling
        {
            get => m_Parameters.textureTiling;
            set => m_Parameters.textureTiling = value;
        }

        /// <summary>Edge fade factor along the positive X, Y and Z axes.</summary>
        public Vector3 positiveFade
        {
            get => m_Parameters.positiveFade;
            set => m_Parameters.positiveFade = value;
        }

        /// <summary>Edge fade factor along the negative X, Y and Z axes.</summary>
        public Vector3 negativeFade
        {
            get => m_Parameters.negativeFade;
            set => m_Parameters.negativeFade = value;
        }

        /// <summary>Dimensions of the volume.</summary>
        public Vector3 size
        {
            get => m_Parameters.size;
            set => m_Parameters.size = value;
        }

        /// <summary>Inverts the fade gradient.</summary>
        public bool invertFade
        {
            get => m_Parameters.invertFade;
            set => m_Parameters.invertFade = value;
        }

        /// <summary>Distance at which density fading starts.</summary>
        public float distanceFadeStart
        {
            get => m_Parameters.distanceFadeStart;
            set => m_Parameters.distanceFadeStart = value;
        }

        /// <summary>Distance at which density fading ends.</summary>
        public float distanceFadeEnd
        {
            get => m_Parameters.distanceFadeEnd;
            set => m_Parameters.distanceFadeEnd = value;
        }

        /// <summary>Allows translation of the tiling density texture.</summary>
        [SerializeField]
        public Vector3 textureOffset
        {
            get => m_Parameters.textureOffset;
            set => m_Parameters.textureOffset = value;
        }

        /// <summary>When Blend Distance is above 0, controls which kind of falloff is applied to the transition area.</summary>
        public DensityVolumeFalloffMode falloffMode
        {
            get => (DensityVolumeFalloffMode) m_Parameters.falloffMode;
            set => m_Parameters.falloffMode = (LocalVolumetricFogFalloffMode) value;
        }

        /// <summary>Constructor.</summary>
        /// <param name="color">Single scattering albedo.</param>
        /// <param name="_meanFreePath">Mean free path.</param>
        /// <param name="_anisotropy">Anisotropy.</param>
        public DensityVolumeArtistParameters(Color color, float _meanFreePath, float _anisotropy)
        {
            m_Parameters = new LocalVolumetricFogArtistParameters(color, _meanFreePath, _anisotropy);
        }
    }

    public partial struct LocalVolumetricFogArtistParameters
    {
        /// <summary>Obsolete, do not use.</summary>
        [Obsolete("Never worked correctly due to having engine working in percent. Will be removed soon.")]
        public bool advancedFade => true;
    }

    public partial class LightingDebugSettings
    {
        /// <summary>Obsolete, please use  the lens attenuation mode in HDRP Default Settings.</summary>
        [Obsolete("Please use the lens attenuation mode in HDRP Default Settings", true)]
        public float debugLensAttenuation = 0.65f;

        /// <summary>Display the Local Volumetric Fog atlas.</summary>
        [Obsolete("Use displayLocalVolumetricFogAtlas instead", false)]
        public bool displayDensityVolumeAtlas
        {
            get => displayLocalVolumetricFogAtlas;
            set => displayLocalVolumetricFogAtlas = value;
        }

        /// <summary>Local Volumetric Fog atlas slice.</summary>
        [Obsolete("Use localVolumetricFogAtlasSlice instead", false)]
        public uint densityVolumeAtlasSlice
        {
            get => localVolumetricFogAtlasSlice;
            set => localVolumetricFogAtlasSlice = value;
        }

        /// <summary>True if Local Volumetric Fog Atlas debug mode should be displayed for the currently selected Local Volumetric Fog.</summary>
        [Obsolete("Use localVolumetricFogUseSelection instead", false)]
        public bool densityVolumeUseSelection
        {
            get => localVolumetricFogUseSelection;
            set => localVolumetricFogUseSelection = value;
        }
    }
}
