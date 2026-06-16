#if SURFACE_CACHE

using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Surface Cache Global Illumination feature.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Surface Cache Global Illumination")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class SurfaceCacheGIVolumeOverride : VolumeComponent
    {
        const PresetQuality k_defaultPresetQuality = PresetQuality.Medium;

        /// <summary>
        /// Enables or disables Surface Cache Global Illumination. When disabled, the feature
        /// performs no work and contributes no indirect lighting.
        /// </summary>
        [Tooltip("Enables Surface Cache Global Illumination. When disabled, the feature performs no work.")]
        public BoolParameter enabled = new BoolParameter(true);

        // ====================
        // Sampling Parameters
        // ====================

        /// <summary>
        /// Enable multi-bounce global illumination.
        /// </summary>
        [Tooltip("Enable multi-bounce global illumination for more accurate light propagation.")]
        public BoolParameter multiBounce = new BoolParameter(k_Presets[(int)k_defaultPresetQuality].multiBounce);

        /// <summary>
        /// Bounce Patch Allocation. When enabled, new patches are allocated at ray hit locations when multi-bounce cache lookups fail.
        /// </summary>
        [Tooltip("When enabled, new patches are allocated at ray hit locations when multi-bounce cache lookups fail.")]
        public BoolParameter bouncePatchAllocation = new BoolParameter(k_Presets[(int)k_defaultPresetQuality].bouncePatchAllocation);

        /// <summary>
        /// Number of samples used for GI estimation. Higher values improve quality at performance cost.
        /// </summary>
        public ClampedIntParameter sampleCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].sampleCount, 1, 32);

        // ============================
        // Patch Filtering Parameters
        // ============================

        /// <summary>
        /// Temporal smoothing factor for patch data. Higher values produce more stable results but slower response to lighting changes.
        /// </summary>
        public ClampedFloatParameter temporalSmoothing = new ClampedFloatParameter(k_Presets[(int)k_defaultPresetQuality].temporalSmoothing, 0.0f, 1.0f);

        /// <summary>
        /// Enable spatial filtering for patch data.
        /// </summary>
        public BoolParameter spatialFilterEnabled = new BoolParameter(k_Presets[(int)k_defaultPresetQuality].spatialFilterEnabled);

        /// <summary>
        /// Number of samples for spatial filtering. Higher values improve quality at performance cost.
        /// </summary>
        public ClampedIntParameter spatialSampleCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].spatialSampleCount, 1, 8);

        /// <summary>
        /// Radius for spatial filtering in world units.
        /// </summary>
        public ClampedFloatParameter spatialRadius = new ClampedFloatParameter(k_Presets[(int)k_defaultPresetQuality].spatialRadius, 0.1f, 4.0f);

        /// <summary>
        /// Enable temporal post-filtering for additional image stability.
        /// </summary>
        public BoolParameter temporalPostFilter = new BoolParameter(k_Presets[(int)k_defaultPresetQuality].temporalPostFilter);

        // ============================
        // Screen Filtering Parameters
        // ============================

        /// <summary>
        /// Number of samples for screen-space lookups.
        /// </summary>
        public ClampedIntParameter lookupSampleCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].lookupSampleCount, 0, 8);

        /// <summary>
        /// Kernel size for upsampling filtering.
        /// </summary>
        public ClampedFloatParameter upsamplingKernelSize = new ClampedFloatParameter(k_Presets[(int)k_defaultPresetQuality].upsamplingKernelSize, 0.0f, 8.0f);

        /// <summary>
        /// Number of samples for upsampling. Higher values improve quality at performance cost.
        /// </summary>
        public ClampedIntParameter upsamplingSampleCount = new ClampedIntParameter(k_Presets[(int)k_defaultPresetQuality].upsamplingSampleCount, 1, 16);

        // =======================
        // Volume Configuration
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
        public ClampedIntParameter volumeResolution = new ClampedIntParameter(32, 16, 128);

        /// <summary>
        /// Number of cascades for the surface cache volume. More cascades extend the volume's reach.
        /// Changing this at runtime can cause a performance hitch as internal buffers are reallocated (BVH is preserved).
        /// </summary>
        public ClampedIntParameter volumeCascadeCount = new ClampedIntParameter(4, 1, 8);

        // =======================
        // Volume Behavior
        // =======================

        /// <summary>
        /// Enable volume cascades to follow the camera.
        /// </summary>
        public BoolParameter cascadeMovement = new BoolParameter(true);

        // =======================
        // Advanced Properties
        // =======================

        /// <summary>
        /// Number of surface cache patches to defragment per frame.
        /// </summary>
        [AdditionalProperty]
        public ClampedIntParameter defragCount = new ClampedIntParameter(2, 1, 32);

        /// <summary>
        /// Only renderers whose Rendering Layer Mask intersects this mask contribute to Surface Cache Global Illumination.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Only renderers whose Rendering Layer Mask intersects this mask contribute to Surface Cache Global Illumination.")]
        public RenderingLayerMaskParameter renderingLayerMask = new RenderingLayerMaskParameter(0xFFFFFFFF);

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
            public bool multiBounce;
            public bool bouncePatchAllocation;
            public int sampleCount;
            public float temporalSmoothing;
            public bool spatialFilterEnabled;
            public int spatialSampleCount;
            public float spatialRadius;
            public bool temporalPostFilter;
            public int lookupSampleCount;
            public float upsamplingKernelSize;
            public int upsamplingSampleCount;
        }


        // Quality preset definitions; indexed by Quality (Low=0, Medium=1, High=2, Ultra=3).
        static readonly Preset[] k_Presets =
        {
            // Low
            new Preset
            {
                multiBounce = false, bouncePatchAllocation = false, sampleCount = 1,
                temporalSmoothing = 0.9f, spatialFilterEnabled = false, spatialSampleCount = 4, spatialRadius = 1.0f, temporalPostFilter = false,
                lookupSampleCount = 4, upsamplingKernelSize = 2.0f, upsamplingSampleCount = 1,
            },
            // Medium
            new Preset
            {
                multiBounce = true, bouncePatchAllocation = true, sampleCount = 2,
                temporalSmoothing = 0.8f, spatialFilterEnabled = true, spatialSampleCount = 4, spatialRadius = 1.0f, temporalPostFilter = true,
                lookupSampleCount = 6, upsamplingKernelSize = 4.0f, upsamplingSampleCount = 2,
            },
            // High
            new Preset
            {
                multiBounce = true, bouncePatchAllocation = true, sampleCount = 4,
                temporalSmoothing = 0.7f, spatialFilterEnabled = true, spatialSampleCount = 6, spatialRadius = 1.5f, temporalPostFilter = true,
                lookupSampleCount = 8, upsamplingKernelSize = 5.0f, upsamplingSampleCount = 4,
            },
            // Ultra
            new Preset
            {
                multiBounce = true, bouncePatchAllocation = true, sampleCount = 8,
                temporalSmoothing = 0.6f, spatialFilterEnabled = true, spatialSampleCount = 8, spatialRadius = 2.0f, temporalPostFilter = true,
                lookupSampleCount = 8, upsamplingKernelSize = 7.0f, upsamplingSampleCount = 8,
            },
        };

        // Writes preset values into the backing serialized fields so users can inspect them
        // and use them as a starting point when switching to Custom.
        public void ApplyPreset(PresetQuality quality)
        {
            if (quality == PresetQuality.Custom) return;
            ref readonly var settings = ref k_Presets[(int)quality];

            multiBounce.value = settings.multiBounce;
            multiBounce.overrideState = true;
            bouncePatchAllocation.value = settings.bouncePatchAllocation;
            bouncePatchAllocation.overrideState = true;
            sampleCount.value = settings.sampleCount;
            sampleCount.overrideState = true;
            temporalSmoothing.value = settings.temporalSmoothing;
            temporalSmoothing.overrideState = true;
            spatialFilterEnabled.value = settings.spatialFilterEnabled;
            spatialFilterEnabled.overrideState = true;
            spatialSampleCount.value = settings.spatialSampleCount;
            spatialSampleCount.overrideState = true;
            spatialRadius.value = settings.spatialRadius;
            spatialRadius.overrideState = true;
            temporalPostFilter.value = settings.temporalPostFilter;
            temporalPostFilter.overrideState = true;
            lookupSampleCount.value = settings.lookupSampleCount;
            lookupSampleCount.overrideState = true;
            upsamplingKernelSize.value = settings.upsamplingKernelSize;
            upsamplingKernelSize.overrideState = true;
            upsamplingSampleCount.value = settings.upsamplingSampleCount;
            upsamplingSampleCount.overrideState = true;
        }

        public PresetQuality GetPresetQuality()
        {
            if(HasPresetOverridesEnabled())
            {
                if (HasPresetQualityValues (PresetQuality.Low)) return PresetQuality.Low;
                if (HasPresetQualityValues (PresetQuality.Medium)) return PresetQuality.Medium;
                if (HasPresetQualityValues (PresetQuality.High)) return PresetQuality.High;
                if (HasPresetQualityValues(PresetQuality.Ultra)) return PresetQuality.Ultra;
            }
            return PresetQuality.Custom;
        }

        public bool HasPresetOverridesEnabled()
        {
            return multiBounce.overrideState == true
                && bouncePatchAllocation.overrideState == true
                && sampleCount.overrideState == true
                && temporalSmoothing.overrideState == true
                && spatialFilterEnabled.overrideState == true
                && spatialSampleCount.overrideState == true
                && spatialRadius.overrideState == true
                && temporalPostFilter.overrideState == true
                && lookupSampleCount.overrideState == true
                && upsamplingKernelSize.overrideState == true
                && upsamplingSampleCount.overrideState == true;
        }

        public bool HasPresetQualityValues(PresetQuality quality)
        {
            if (quality == PresetQuality.Custom) return true;
            ref readonly var preset = ref k_Presets[(int)quality];

            return multiBounce.value == preset.multiBounce
                && bouncePatchAllocation.value == preset.bouncePatchAllocation
                && sampleCount.value == preset.sampleCount
                && temporalSmoothing.value == preset.temporalSmoothing
                && spatialFilterEnabled.value == preset.spatialFilterEnabled
                && spatialSampleCount.value == preset.spatialSampleCount
                && spatialRadius.value == preset.spatialRadius
                && temporalPostFilter.value == preset.temporalPostFilter
                && lookupSampleCount.value == preset.lookupSampleCount
                && upsamplingKernelSize.value == preset.upsamplingKernelSize
                && upsamplingSampleCount.value == preset.upsamplingSampleCount;
        }

#if UNITY_EDITOR
        internal event Action propertyChanged;
        private void OnValidate() => propertyChanged?.Invoke();
#endif
    }
}

#endif
