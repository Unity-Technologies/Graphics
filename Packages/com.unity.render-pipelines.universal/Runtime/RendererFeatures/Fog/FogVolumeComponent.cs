#if VOLUMETRIC_FOG

using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for volumetric fog.
    /// </summary>
    [Serializable, VolumeComponentMenu("Fog")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class FogVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// The color of the fog.
        /// </summary>
        [Tooltip("The color of the fog.")]
        public ColorParameter color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);

        /// <summary>
        /// Average distance (in meters) light travels before scattering. Lower values produce denser fog.
        /// Extinction coefficient = 1 / meanFreePath.
        /// </summary>
        [Tooltip("Average distance (in meters) light travels before scattering. Lower values produce denser fog. Extinction coefficient = 1 / meanFreePath.")]
        public MinFloatParameter meanFreePath = new MinFloatParameter(400.0f, 1.0f);

        /// <summary>
        /// Controls the angular distribution of scattered light via the Cornette-Shanks phase function. 0 means that
        /// light is equally scattered forwards and backwards, positive values favor forward scattering, negative values
        /// favor backward scattering.
        /// </summary>
        // The default 0.65 is chosen because it makes "godrays" visible out of the box.
        [Tooltip("Controls the angular distribution of scattered light via the Cornette-Shanks phase function. 0 means that light is equally scattered forwards and backwards, positive values favor forward scattering, negative values favor backward scattering.")]
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.65f, -1.0f, 1.0f);

        /// <summary>
        /// Controls which lights contribute to the per-voxel fog lighting.
        /// <see cref="VolumetricFogLightFilter.DirectionalOnly"/> strips the additional-lights (point + spot) from
        /// the lighting compute via a shader keyword, making fog cheaper to compute.
        /// </summary>
        [Tooltip("Controls which lights contribute to the per-voxel fog lighting. Directional Only strips the additional-lights (point + spot) from the lighting compute via a shader keyword, making fog cheaper to compute.")]
        public VolumetricFogLightFilterParameter lightFilter = new VolumetricFogLightFilterParameter(VolumetricFogLightFilter.AllLights);

        /// <summary>
        /// Screen-space approximation of multiple scattering: where fog opacity is high, the post-fog scene color is
        /// sampled from a Gaussian color pyramid at a mip level proportional to opacity × intensity. Gives a soft
        /// "scattered" look on objects seen through dense fog. 0 disables the pass entirely.
        /// </summary>
        [Tooltip("Screen-space approximation of multiple scattering: where fog opacity is high, the post-fog scene color is sampled from a Gaussian color pyramid at a mip level proportional to opacity × intensity. Gives a soft \"scattered\" look on objects seen through dense fog. 0 disables the pass entirely.")]
        public ClampedFloatParameter multipleScatteringIntensity = new ClampedFloatParameter(0.0f, 0.0f, 2.0f);

        /// <summary>
        /// Selects the denoise technique applied to volumetric fog lighting.
        /// </summary>
        [Tooltip("Selects the denoise technique applied to volumetric fog lighting.")]
        public VolumetricFogDenoisingModeParameter denoisingMode = new VolumetricFogDenoisingModeParameter(VolumetricFogDenoisingMode.Reprojection);

        /// <summary>
        /// Skips the lighting loop for voxels whose extinction is at or below this threshold. Cheap
        /// fast-path for sparse fog, where many voxels carry negligible density.
        /// </summary>
        [Tooltip("Skips the lighting loop for voxels whose extinction is at or below this threshold. Cheap fast-path for sparse fog, where many voxels carry negligible density.")]
        public MinFloatParameter volumetricLightingDensityCutoff = new MinFloatParameter(0.0f, 0.0f);

        /// <summary>
        /// Controls whether light cookies are sampled when evaluating per-voxel fog lighting. When off, fog ignores
        /// cookies even if the lights themselves still project them on surfaces. Costs an extra cookie-atlas sample per
        /// lit voxel; free when no visible light has a cookie.
        /// </summary>
        [Tooltip("Controls whether light cookies are sampled when evaluating per-voxel fog lighting. When off, fog ignores cookies even if the lights themselves still project them on surfaces. Costs an extra cookie-atlas sample per lit voxel; free when no visible light has a cookie.")]
        public BoolParameter enableLightCookies = new BoolParameter(true);

        /// <summary>
        /// V-buffer XY resolution as a percentage of the screen. Lower values are cheaper but produce blockier fog.
        /// </summary>
        [Tooltip("V-buffer XY resolution as a percentage of the screen. Lower values are cheaper but produce blockier fog.")]
        public ClampedFloatParameter screenResolutionPercentage = new ClampedFloatParameter(12.5f, 6.25f, 50.0f);

        /// <summary>
        /// Number of v-buffer depth slices. Higher values reduce slice-banding at the cost of memory, bandwidth, and compute.
        /// </summary>
        [Tooltip("Number of v-buffer depth slices. Higher values reduce slice-banding at the cost of memory, bandwidth, and compute.")]
        public ClampedIntParameter volumeSliceCount = new ClampedIntParameter(64, 1, 512);

        /// <summary>
        /// How far (in meters) the high-quality volumetric fog reaches in front of the camera; past
        /// this distance fog falls back to a simpler approximation with no shadowed lights.
        /// Increasing it stretches the same <see cref="volumeSliceCount"/> slices over a longer range, so each slice covers more depth.
        /// </summary>
        [Tooltip("How far (in meters) the high-quality volumetric fog reaches in front of the camera; past this distance fog falls back to a simpler approximation with no shadowed lights. Increasing it stretches the same Volume Slice Count slices over a longer range, so each slice covers more depth.")]
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);

        /// <summary>
        /// How the fog's depth slices are spread out between the camera and the far end of <see cref="depthExtent"/>.
        /// 0 packs more slices close to the camera for sharper near-field fog (far slices get coarse);
        /// 1 spaces them evenly so every slice covers the same depth.
        /// </summary>
        [Tooltip("How the fog's depth slices are spread out between the camera and the far end of Depth Extent. 0 packs more slices close to the camera for sharper near-field fog (far slices get coarse); 1 spaces them evenly so every slice covers the same depth.")]
        public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0.0f, 1.0f);

        /// <summary>
        /// The height (in meters) up to which the fog stays at full density. Above it, the fog
        /// gradually thins out, fading away by <see cref="maximumHeight"/>.
        /// </summary>
        [Tooltip("The height (in meters) up to which the fog stays at full density. Above it, the fog gradually thins out, fading away by Maximum Height.")]
        public FloatParameter baseHeight = new FloatParameter(0.0f);

        /// <summary>
        /// The height (in meters) at which the fog has essentially faded to nothing. Together with
        /// <see cref="baseHeight"/> it sets how quickly fog thins with height, which also dims
        /// directional light passing through it.
        /// </summary>
        [Tooltip("The height (in meters) at which the fog has essentially faded to nothing. Together with Base Height it sets how quickly fog thins with height, which also dims directional light passing through it.")]
        public FloatParameter maximumHeight = new FloatParameter(50.0f);

        /// <summary>
        /// The distance (in meters) at which fog reaches its maximum. Objects get progressively foggier
        /// up to this distance; past it the fog stops increasing, so the sky and anything farther away
        /// all show the same maximum amount of fog.
        /// </summary>
        [Tooltip("The distance (in meters) at which fog reaches its maximum. Objects get progressively foggier up to this distance; past it the fog stops increasing, so the sky and anything farther away all show the same maximum amount of fog.")]
        public MinFloatParameter maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);

        /// <inheritdoc/>
        // The volume stack always hands back a default Fog component, so returning true would fog
        // every URP scene even when no one asked for it. AnyPropertiesIsOverridden() is true only
        // when a scene Volume actually overrode a Fog parameter, which is the real on/off signal.
        public bool IsActive() => AnyPropertiesIsOverridden();
    }

    /// <summary>
    /// Denoise techniques applied to the volumetric fog lighting.
    /// </summary>
    public enum VolumetricFogDenoisingMode
    {
        /// <summary>No denoising. Each frame is rendered independently — cheapest, but per-froxel jitter and shadowed fog read as visible noise in motion.</summary>
        None = 0,
        /// <summary>Blend each frame with a reprojected history buffer. Effective for static and slow-moving lighting; mild ghosting on fast-moving shadows.</summary>
        Reprojection = 1 << 0,
        /// <summary>Run a Gaussian filter across each v-buffer slice after lighting.</summary>
        Gaussian = 1 << 1,
        /// <summary>Both Reprojection and Gaussian. Highest visual quality; pays the cost of both techniques.</summary>
        Both = Reprojection | Gaussian,
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> wrapper for <see cref="VolumetricFogDenoisingMode"/>.
    /// </summary>
    [Serializable]
    public sealed class VolumetricFogDenoisingModeParameter : VolumeParameter<VolumetricFogDenoisingMode>
    {
        /// <summary>
        /// Creates a new <see cref="VolumetricFogDenoisingModeParameter"/>.
        /// </summary>
        /// <param name="value">The initial denoise mode.</param>
        /// <param name="overrideState">Whether the parameter is overridden in the volume profile.</param>
        public VolumetricFogDenoisingModeParameter(VolumetricFogDenoisingMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// Which lights contribute to volumetric fog lighting evaluation.
    /// </summary>
    public enum VolumetricFogLightFilter
    {
        /// <summary>Evaluate the main directional light plus all additional punctual lights per voxel.</summary>
        AllLights = 0,
        /// <summary>Evaluate only the main directional light per voxel; skip point and spot lights entirely.</summary>
        DirectionalOnly = 1,
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> wrapper for <see cref="VolumetricFogLightFilter"/>.
    /// </summary>
    [Serializable]
    public sealed class VolumetricFogLightFilterParameter : VolumeParameter<VolumetricFogLightFilter>
    {
        /// <summary>
        /// Creates a new <see cref="VolumetricFogLightFilterParameter"/>.
        /// </summary>
        /// <param name="value">The initial light filter.</param>
        /// <param name="overrideState">Whether the parameter is overridden in the volume profile.</param>
        public VolumetricFogLightFilterParameter(VolumetricFogLightFilter value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}

#endif
