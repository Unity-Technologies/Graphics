using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Surface Cache Global Illumination feature.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Surface Cache Global Illumination")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [DisplayInfo(name = "Surface Cache Global Illumination")]
    public class SurfaceCacheGIVolumeOverride : VolumeComponent
    {
        const PresetQuality k_defaultPresetQuality = PresetQuality.Medium;

        /// <summary>
        /// Enables or disables Surface Cache Global Illumination for the cameras this volume affects.
        /// When enabled, it calculates indirect lighting at any <see cref="intensity"/> value, which keeps
        /// the Surface Cache warm. When disabled, it does no per-frame work and discards its Surface Cache,
        /// so enabling it again costs a warm-up.
        /// </summary>
        [Tooltip("When Enabled, Surface Cache Global Illumination calculates indirect lighting at any Intensity value. When Disabled, it does not.")]
        public BoolParameter enabled = new BoolParameter(true, BoolParameter.DisplayType.EnumPopup);

        /// <summary>
        /// Scales the indirect lighting from Surface Cache Global Illumination.
        /// At 0 it adds no indirect lighting to the scene, but the feature keeps calculating, so the
        /// cache stays warm. Set <see cref="enabled"/> to false to stop the work altogether.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Scales the indirect lighting from Surface Cache Global Illumination. At 0 it adds no indirect lighting to the scene.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);

        // ==========================
        // Light Transport Parameters
        // ==========================

        /// <summary>
        /// Enable multi-bounce global illumination.
        /// </summary>
        [Tooltip("Enable multi-bounce global illumination for more accurate light propagation.")]
        public BoolParameter lightTransportMultiBounce = new BoolParameter(k_Presets[(int)k_defaultPresetQuality].lightTransportMultiBounce);

        /// <summary>
        /// When enabled, new patches are allocated at ray hit locations when multi-bounce cache lookups fail.
        /// </summary>
        [Tooltip("When enabled, new patches are allocated at ray hit locations when multi-bounce cache lookups fail.")]
        public BoolParameter lightTransportBouncePatchAllocation = new BoolParameter(false);

        /// <summary>
        /// Number of samples used for GI estimation. Higher values improve quality at performance cost.
        /// </summary>
        public ClampedIntParameter lightTransportSampleCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].lightTransportSampleCount, 1, 32);

        /// <summary>
        /// Only renderers whose Rendering Layer Mask intersects this mask contribute to Surface Cache Global Illumination.
        /// </summary>
        [AdditionalProperty]
        [FormerlySerializedAs("advancedRenderingLayerMask")] // Added in Unity 6.7b1.
        [Tooltip("Only renderers whose Rendering Layer Mask intersects this mask contribute to Surface Cache Global Illumination.")]
        public RenderingLayerMaskParameter lightTransportRenderingLayerMask = new RenderingLayerMaskParameter(0xFFFFFFFF);

        /// <summary>
        /// Number of surface cache patches to defragment per frame.
        /// </summary>
        [AdditionalProperty]
        [FormerlySerializedAs("advancedDefragCount")] // Added in Unity 6.7b1.
        public ClampedIntParameter lightTransportDefragCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].lightTransportDefragCount, 1, 32);

        /// <summary>
        /// Boosts the initial sample count on newly revealed surfaces if needed. This stabilizes the image faster when geometry or camera is moving.
        /// </summary>
        public ClampedIntParameter lightTransportWarmupSampleMultiplier = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].lightTransportWarmUpSampleMultiplier, 1, 16); // TODO(rasmusn): Add to presets?

        // ============================
        // Patch Filtering Parameters
        // ============================

        /// <summary>
        /// Temporal smoothing factor for patch data. Higher values produce more stable results but slower response to lighting changes.
        /// </summary>
        public ClampedFloatParameter patchFilteringTemporalSmoothing = new ClampedFloatParameter(k_Presets[(int)k_defaultPresetQuality].patchFilteringTemporalSmoothing, 0.0f, 1.0f);

        /// <summary>
        /// Enable spatial filtering for patch data.
        /// </summary>
        [FormerlySerializedAs("patchFilteringSpatialFilterEnabled")] // Added in Unity 6.7b1.
        public BoolParameter patchFilteringSpatialEnabled = new BoolParameter(k_Presets[(int)k_defaultPresetQuality].patchFilteringSpatialEnabled);

        /// <summary>
        /// Number of samples for spatial filtering. Higher values improve quality at performance cost.
        /// </summary>
        public ClampedIntParameter patchFilteringSpatialSampleCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].patchFilteringSpatialSampleCount, 1, 8);

        /// <summary>
        /// Radius for spatial filtering in world units.
        /// </summary>
        public ClampedFloatParameter patchFilteringSpatialRadius = new ClampedFloatParameter(k_Presets[(int)k_defaultPresetQuality].patchFilteringSpatialRadius, 0.1f, 4.0f);

        /// <summary>
        /// Enable temporal post-filtering for additional image stability.
        /// </summary>
        public BoolParameter patchFilteringPostTemporalEnabled = new BoolParameter(true);

        // ============================
        // Screen Filtering Parameters
        // ============================

        /// <summary>
        /// Number of samples for screen-space lookups.
        /// </summary>
        public ClampedIntParameter screenFilteringLookupSampleCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].screenFilteringLookupSampleCount, 0, 16);

        /// <summary>
        /// Number of denoising passes. More passes reduce noise at a performance cost.
        /// </summary>
        public ClampedIntParameter screenFilteringDenoisingPassCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].screenFilteringDenoisingPassCount, 0, 4);

        // =======================
        // Volume
        // =======================

        /// <summary>
        /// Size of the surface cache volume in world units.
        /// This can be changed per-scene without causing a performance hitch.
        /// </summary>
        public MinFloatParameter volumeSize = new MinFloatParameter(128.0f, 1.0f);

        /// <summary>
        /// Spatial resolution of the surface cache volume. Higher values improve spatial detail but use more memory.
        /// Changing this at runtime can cause a performance hitch as internal buffers are reallocated (BVH is preserved).
        /// </summary>
        public ClampedIntParameter volumeResolution = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].volumeResolution, 16, 128);

        /// <summary>
        /// Number of cascades for the surface cache volume. More cascades extend the volume's reach.
        /// Changing this at runtime can cause a performance hitch as internal buffers are reallocated (BVH is preserved).
        /// </summary>
        public ClampedIntParameter volumeCascadeCount = new ClampedIntParameter(4, 1, 8);

        /// <summary>
        /// When enabled, a realtime global probe calculated from the environment light will be applied in the far distance.
        /// </summary>
        [Tooltip("When enabled, a realtime global probe calculated from the environment light will be applied in the far distance.")]
        public BoolParameter volumeDistanceFallback = new BoolParameter(k_Presets[(int)k_defaultPresetQuality].volumeDistanceFallback);

        /// <summary>
        /// Applies a small periodic warping to sample positions before they resolve to a patch, reducing
        /// flickering on flat surfaces lined up with the voxel grid (e.g. a floor at height 0). When off,
        /// no warping is applied and the calculation is skipped.
        /// </summary>
        [AdditionalProperty]
        [FormerlySerializedAs("advancedPatchWarpingEnabled")] // Added in Unity 6.7b1.
        [Tooltip("Reduces flickering on flat surfaces lined up with the voxel grid (e.g. a floor at height 0). When off, no warping is applied.")]
        public BoolParameter volumePatchWarpingEnabled = new BoolParameter(true);

        // =======================
        // Preset Utility
        // =======================
        /// <summary>
        /// Quality presets for Surface Cache Global Illumination.
        /// </summary>
        public enum PresetQuality
        {
            /// <summary>
            /// Low quality preset - minimal samples, maximum performance.
            /// </summary>
            Low,

            /// <summary>
            /// Medium quality preset - balanced quality and performance (default).
            /// </summary>
            Medium,

            /// <summary>
            /// High quality preset - higher quality with increased cost.
            /// </summary>
            High,

            /// <summary>
            /// Ultra quality preset - maximum quality, highest cost.
            /// </summary>
            Ultra,

            /// <summary>
            /// Custom quality - user-defined parameters.
            /// </summary>
            Custom
        }

        struct Preset
        {
            public int volumeResolution;
            public bool volumeDistanceFallback;
            public bool lightTransportMultiBounce;
            public int lightTransportSampleCount;
            public int lightTransportWarmUpSampleMultiplier;
            public int lightTransportDefragCount;
            public float patchFilteringTemporalSmoothing;
            public bool patchFilteringSpatialEnabled;
            public int patchFilteringSpatialSampleCount;
            public float patchFilteringSpatialRadius;
            public int screenFilteringLookupSampleCount;
            public int screenFilteringDenoisingPassCount;
        }


        // Quality preset definitions; indexed by Quality (Low=0, Medium=1, High=2, Ultra=3).
        static readonly Preset[] k_Presets =
        {
            // Low
            new Preset
            {
                lightTransportMultiBounce = false, lightTransportSampleCount = 1,
                lightTransportWarmUpSampleMultiplier = 1, volumeDistanceFallback = false, patchFilteringTemporalSmoothing = 0.9f,
                patchFilteringSpatialEnabled = false, patchFilteringSpatialSampleCount = 4, patchFilteringSpatialRadius = 1.0f,
                volumeResolution = 16, screenFilteringLookupSampleCount = 2,
                screenFilteringDenoisingPassCount = 1, lightTransportDefragCount = 2
            },
            // Medium
            new Preset
            {
                lightTransportMultiBounce = true, lightTransportSampleCount = 2,
                lightTransportWarmUpSampleMultiplier = 2, volumeDistanceFallback = false, patchFilteringTemporalSmoothing = 0.8f,
                patchFilteringSpatialEnabled = true, patchFilteringSpatialSampleCount = 4, patchFilteringSpatialRadius = 1.0f,
                volumeResolution = 32, screenFilteringLookupSampleCount = 4,
                screenFilteringDenoisingPassCount = 2, lightTransportDefragCount = 4
            },
            // High
            new Preset
            {
                lightTransportMultiBounce = true, lightTransportSampleCount = 4,
                lightTransportWarmUpSampleMultiplier = 4, volumeDistanceFallback = true, patchFilteringTemporalSmoothing = 0.7f,
                patchFilteringSpatialEnabled = true, patchFilteringSpatialSampleCount = 6, patchFilteringSpatialRadius = 1.0f,
                volumeResolution = 48, screenFilteringLookupSampleCount = 8,
                screenFilteringDenoisingPassCount = 3, lightTransportDefragCount = 8
            },
            // Ultra
            new Preset
            {
                lightTransportMultiBounce = true, lightTransportSampleCount = 8,
                lightTransportWarmUpSampleMultiplier = 8, volumeDistanceFallback = true, patchFilteringTemporalSmoothing = 0.6f,
                patchFilteringSpatialEnabled = true, patchFilteringSpatialSampleCount = 8, patchFilteringSpatialRadius = 1.0f,
                volumeResolution = 64, screenFilteringLookupSampleCount = 16,
                screenFilteringDenoisingPassCount = 4, lightTransportDefragCount = 16
            },
        };

        /// <summary>
        /// Writes the values of the given quality preset into the parameters and enables their override states,
        /// so they can be inspected and used as a starting point when switching to <see cref="PresetQuality.Custom"/>.
        /// Has no effect when <paramref name="quality"/> is <see cref="PresetQuality.Custom"/>.
        /// </summary>
        /// <param name="quality">The quality preset to apply.</param>
        public void ApplyPreset(PresetQuality quality)
        {
            if (quality == PresetQuality.Custom) return;
            ref readonly var settings = ref k_Presets[(int)quality];

            lightTransportMultiBounce.value = settings.lightTransportMultiBounce;
            lightTransportMultiBounce.overrideState = true;
            lightTransportSampleCount.value = settings.lightTransportSampleCount;
            lightTransportSampleCount.overrideState = true;
            lightTransportWarmupSampleMultiplier.value = settings.lightTransportWarmUpSampleMultiplier;
            lightTransportWarmupSampleMultiplier.overrideState = true;
            volumeDistanceFallback.value = settings.volumeDistanceFallback;
            volumeDistanceFallback.overrideState = true;
            patchFilteringTemporalSmoothing.value = settings.patchFilteringTemporalSmoothing;
            patchFilteringTemporalSmoothing.overrideState = true;
            patchFilteringSpatialEnabled.value = settings.patchFilteringSpatialEnabled;
            patchFilteringSpatialEnabled.overrideState = true;
            patchFilteringSpatialSampleCount.value = settings.patchFilteringSpatialSampleCount;
            patchFilteringSpatialSampleCount.overrideState = true;
            patchFilteringSpatialRadius.value = settings.patchFilteringSpatialRadius;
            patchFilteringSpatialRadius.overrideState = true;
            volumeResolution.value = settings.volumeResolution;
            volumeResolution.overrideState = true;
            screenFilteringLookupSampleCount.value = settings.screenFilteringLookupSampleCount;
            screenFilteringLookupSampleCount.overrideState = true;
            screenFilteringDenoisingPassCount.value = settings.screenFilteringDenoisingPassCount;
            screenFilteringDenoisingPassCount.overrideState = true;
            lightTransportDefragCount.value = settings.lightTransportDefragCount;
            lightTransportDefragCount.overrideState = true;
        }

        /// <summary>
        /// Returns the quality preset that matches the current parameter values,
        /// or <see cref="PresetQuality.Custom"/> if no preset matches.
        /// </summary>
        /// <returns>The matching quality preset.</returns>
        public PresetQuality GetPresetQuality()
        {
            if (HasPresetOverridesEnabled())
            {
                if (HasPresetQualityValues(PresetQuality.Low)) return PresetQuality.Low;
                if (HasPresetQualityValues(PresetQuality.Medium)) return PresetQuality.Medium;
                if (HasPresetQualityValues(PresetQuality.High)) return PresetQuality.High;
                if (HasPresetQualityValues(PresetQuality.Ultra)) return PresetQuality.Ultra;
            }
            return PresetQuality.Custom;
        }

        /// <summary>
        /// Checks whether the override states of all preset-controlled parameters are enabled.
        /// </summary>
        /// <returns>True if all preset-controlled parameters have their override state enabled.</returns>
        public bool HasPresetOverridesEnabled()
        {
            return lightTransportMultiBounce.overrideState
                && lightTransportSampleCount.overrideState
                && lightTransportWarmupSampleMultiplier.overrideState
                && lightTransportDefragCount.overrideState
                && volumeDistanceFallback.overrideState
                && patchFilteringTemporalSmoothing.overrideState
                && patchFilteringSpatialEnabled.overrideState
                && patchFilteringSpatialSampleCount.overrideState
                && patchFilteringSpatialRadius.overrideState
                && volumeResolution.overrideState
                && screenFilteringLookupSampleCount.overrideState
                && screenFilteringDenoisingPassCount.overrideState;
        }

        /// <summary>
        /// Checks whether the current parameter values match the given quality preset.
        /// </summary>
        /// <param name="quality">The quality preset to compare against.</param>
        /// <returns>True if the parameter values match the preset, or if <paramref name="quality"/> is <see cref="PresetQuality.Custom"/>.</returns>
        public bool HasPresetQualityValues(PresetQuality quality)
        {
            if (quality == PresetQuality.Custom) return true;
            ref readonly var preset = ref k_Presets[(int)quality];

            return lightTransportMultiBounce.value == preset.lightTransportMultiBounce
                && lightTransportSampleCount.value == preset.lightTransportSampleCount
                && lightTransportWarmupSampleMultiplier.value == preset.lightTransportWarmUpSampleMultiplier
                && lightTransportDefragCount.value == preset.lightTransportDefragCount
                && volumeDistanceFallback.value == preset.volumeDistanceFallback
                && volumeResolution.value == preset.volumeResolution
                && patchFilteringTemporalSmoothing.value == preset.patchFilteringTemporalSmoothing
                && patchFilteringSpatialEnabled.value == preset.patchFilteringSpatialEnabled
                && patchFilteringSpatialSampleCount.value == preset.patchFilteringSpatialSampleCount
                && patchFilteringSpatialRadius.value == preset.patchFilteringSpatialRadius
                && screenFilteringLookupSampleCount.value == preset.screenFilteringLookupSampleCount
                && screenFilteringDenoisingPassCount.value == preset.screenFilteringDenoisingPassCount;
        }

#if UNITY_EDITOR
        internal event Action propertyChanged;
        private void OnValidate() => propertyChanged?.Invoke();
#endif
    }
}
