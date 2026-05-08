#if MODERN_SSAO
using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Ambient occlusion rendering modes.
    /// </summary>
    public enum ScreenSpaceAmbientOcclusionMode
    {
        /// <summary>
        /// Disables Screen Space Ambient Occlusion.
        /// </summary>
        [InspectorName("None")]
        None = 0,

        /// <summary>
        /// Standard SSAO algorithm - Alchemy method with configurable noise and blur.
        /// </summary>
        [InspectorName("Standard")]
        Standard = 1,

        /// <summary>
        /// Ground Truth Ambient Occlusion.
        /// </summary>
        [InspectorName("GTAO")]
        GTAO = 2,

    }

    /// <summary>
    /// Noise method for ambient occlusion sampling.
    /// </summary>
    public enum ScreenSpaceAmbientOcclusionNoiseMethod
    {
        /// <summary>
        /// Blue noise based sampling - reduces visible patterns.
        /// </summary>
        [InspectorName("Blue Noise")]
        BlueNoise = 0,

        /// <summary>
        /// Interleaved gradient noise based sampling - better performance.
        /// </summary>
        [InspectorName("Interleaved Gradient")]
        InterleavedGradient = 1
    }

    /// <summary>
    /// Quality presets for ambient occlusion sampling.
    /// </summary>
    public enum ScreenSpaceAmbientOcclusionSampleCount
    {
        /// <summary>
        /// Low quality preset - 4 samples in Standard mode. 2 iterations in fragment GTAO.
        /// </summary>
        [InspectorName("Low")]
        Low = 0,

        /// <summary>
        /// Medium quality - 8 samples in Standard mode, 8 iterations in fragment GTAO.
        /// </summary>
        [InspectorName("Medium")]
        Medium = 1,

        /// <summary>
        /// High quality - 12 samples in Standard mode, 16 iterations in fragment GTAO.
        /// </summary>
        [InspectorName("High")]
        High = 2
    }

    /// <summary>
    /// Depth and normal data source for ambient occlusion.
    /// </summary>
    public enum ScreenSpaceAmbientOcclusionDepthSource
    {
        /// <summary>
        /// Depth buffer only - normals reconstructed from depth.
        /// </summary>
        [InspectorName("Depth")]
        Depth = 0,

        /// <summary>
        /// Depth and normals from the depth-normals prepass - more accurate.
        /// </summary>
        [InspectorName("Depth Normals")]
        DepthNormals = 1
    }

    /// <summary>
    /// Normal reconstruction quality when computing normals from depth.
    /// </summary>
    public enum ScreenSpaceAmbientOcclusionNormalQuality
    {
        /// <summary>
        /// Low quality - fewer samples, faster reconstruction.
        /// </summary>
        [InspectorName("Low")]
        Low = 0,

        /// <summary>
        /// Medium quality - balanced reconstruction (default).
        /// </summary>
        [InspectorName("Medium")]
        Medium = 1,

        /// <summary>
        /// High quality - more samples, better accuracy.
        /// </summary>
        [InspectorName("High")]
        High = 2
    }

    /// <summary>
    /// Blur quality for the ambient occlusion texture.
    /// </summary>
    public enum ScreenSpaceAmbientOcclusionBlurQuality
    {
        /// <summary>
        /// Low quality - Kawase blur, best performance.
        /// </summary>
        [InspectorName("Low (Kawase)")]
        Low = 0,

        /// <summary>
        /// Medium quality - Gaussian blur, balanced performance.
        /// </summary>
        [InspectorName("Medium (Gaussian)")]
        Medium = 1,

        /// <summary>
        /// High quality - bilateral blur for edge-preserving smoothing.
        /// </summary>
        [InspectorName("High (Bilateral)")]
        High = 2
    }

    /// <summary>
    /// Quality presets for ambient occlusion settings.
    /// </summary>
    public enum ScreenSpaceAmbientOcclusionQuality
    {
        /// <summary>
        /// Low quality preset - better performance, lower accuracy.
        /// </summary>
        [InspectorName("Low")]
        Low = 0,

        /// <summary>
        /// Medium quality preset - balanced quality and performance (default).
        /// </summary>
        [InspectorName("Medium")]
        Medium = 1,

        /// <summary>
        /// High quality preset - best visual quality with increased cost.
        /// </summary>
        [InspectorName("High")]
        High = 2,

        /// <summary>
        /// Custom quality - user-defined parameters.
        /// </summary>
        [InspectorName("Custom")]
        Custom = 3
    }

    /// <summary>
    /// A volume component that holds settings for the Screen Space Ambient Occlusion effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Ambient Occlusion")]
    [DisplayInfo(name = "Screen Space Ambient Occlusion")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [VolumeRequiresRendererFeatures(typeof(ScreenSpaceAmbientOcclusion))]
    [URPHelpURL("post-processing-ssao")]
    public sealed class ScreenSpaceAmbientOcclusionVolumeOverride : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Ambient occlusion rendering mode.
        /// </summary>
        public ScreenSpaceAmbientOcclusionMode mode
        {
            get => m_Mode.value;
            set => m_Mode.value = value;
        }
        [Tooltip("The ambient occlusion algorithm to use. Standard uses the Alchemy SSAO method. GTAO (Ground Truth Ambient Occlusion) provides higher quality.")]
        [SerializeField]
        private ScreenSpaceAmbientOcclusionModeParameter m_Mode = new ScreenSpaceAmbientOcclusionModeParameter(ScreenSpaceAmbientOcclusionMode.None);

        /// <summary>
        /// Quality preset for ambient occlusion. Selecting a preset automatically configures all quality-related parameters.
        /// Select Custom to manually adjust individual parameters.
        /// </summary>
        public ScreenSpaceAmbientOcclusionQuality quality
        {
            get => m_Quality.value;
            set => m_Quality.value = value;
        }
        [Tooltip("Quality preset for ambient occlusion. Custom allows manual adjustment of all parameters.")]
        [SerializeField]
        private ScreenSpaceAmbientOcclusionQualityParameter m_Quality = new ScreenSpaceAmbientOcclusionQualityParameter(ScreenSpaceAmbientOcclusionQuality.Medium);

        // ====================
        // Common Parameters
        // ====================

        /// <summary>
        /// Intensity of the ambient occlusion effect. Higher values produce darker occlusion.
        /// </summary>
        public float intensity
        {
            get => m_Intensity.value;
            set => m_Intensity.value = value;
        }
        [Tooltip("Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.")]
        [SerializeField]
        private MinFloatParameter m_Intensity = new MinFloatParameter(1f, 0f);

        /// <summary>
        /// Radius around each point for ambient occlusion calculation.
        /// </summary>
        public float radius
        {
            get => m_Radius.value;
            set => m_Radius.value = value;
        }
        [Tooltip("The radius around a given point where Unity calculates and applies the effect. Larger values cover more area but may reduce performance due to increased texture sampling.")]
        [SerializeField]
        private MinFloatParameter m_Radius = new MinFloatParameter(0.3f, 0.001f);

        /// <summary>
        /// Controls how much the ambient occlusion affects direct lighting.
        /// </summary>
        public float directLightingStrength
        {
            get => m_DirectLightingStrength.value;
            set => m_DirectLightingStrength.value = value;
        }
        [Tooltip("Controls how visible the ambient occlusion effect is on surfaces lit by direct light sources. Higher values apply occlusion more uniformly across all lighting, not just in shadowed areas.")]
        [SerializeField]
        private ClampedFloatParameter m_DirectLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Distance from the camera beyond which ambient occlusion fades out.
        /// </summary>
        public float falloffDistance
        {
            get => m_FalloffDistance.value;
            set => m_FalloffDistance.value = value;
        }
        [Tooltip("The distance from the camera where Ambient Occlusion should be visible. Beyond this distance, AO will fade out.")]
        [SerializeField]
        private MinFloatParameter m_FalloffDistance = new MinFloatParameter(100f, 0f);

        /// <summary>
        /// Sample count for ambient occlusion. Higher values improve quality at performance cost.
        /// </summary>
        public ScreenSpaceAmbientOcclusionSampleCount sampleCount
        {
            get
            {
                var q = m_Quality.value;
                return q == ScreenSpaceAmbientOcclusionQuality.Custom ? m_SampleCount.value : GetPresetSampleCount(q);
            }
            set => m_SampleCount.value = value;
        }
        [Tooltip("The quality of the ambient occlusion sampling. Higher quality uses more samples/steps but provides better results.")]
        [SerializeField]
        private ScreenSpaceAmbientOcclusionSampleCountParameter m_SampleCount = new ScreenSpaceAmbientOcclusionSampleCountParameter(ScreenSpaceAmbientOcclusionSampleCount.Medium);

        /// <summary>
        /// Enable downsampling of the SSAO texture to improve performance.
        /// </summary>
        public bool downsample
        {
            get
            {
                var q = m_Quality.value;
                return q == ScreenSpaceAmbientOcclusionQuality.Custom ? m_Downsample.value : GetPresetDownsample(q);
            }
            set => m_Downsample.value = value;
        }
        [Tooltip("With this option enabled, Unity downsamples the SSAO effect texture to improve performance.")]
        [SerializeField]
        private BoolParameter m_Downsample = new BoolParameter(false);

        /// <summary>
        /// Apply SSAO after the opaque pass. Can improve performance on mobile and tiled GPUs.
        /// </summary>
        public bool afterOpaque
        {
            get => m_AfterOpaque.value;
            set => m_AfterOpaque.value = value;
        }
        [Tooltip("When enabled, SSAO is applied as a multiply on the final opaque image after rendering. This can improve performance on tiled GPUs but may produce slightly different visual results compared to applying SSAO during lighting.")]
        [SerializeField]
        private BoolParameter m_AfterOpaque = new BoolParameter(false);

        /// <summary>
        /// Blur quality for the ambient occlusion texture. Higher quality reduces noise at performance cost.
        /// </summary>
        public ScreenSpaceAmbientOcclusionBlurQuality blurQuality
        {
            get
            {
                var q = m_Quality.value;
                return q == ScreenSpaceAmbientOcclusionQuality.Custom ? m_BlurQuality.value : GetPresetBlurQuality(q);
            }
            set => m_BlurQuality.value = value;
        }
        [Tooltip("The blur quality to apply to the ambient occlusion texture. Higher quality reduces noise but is more expensive.")]
        [SerializeField]
        private ScreenSpaceAmbientOcclusionBlurQualityParameter m_BlurQuality = new ScreenSpaceAmbientOcclusionBlurQualityParameter(ScreenSpaceAmbientOcclusionBlurQuality.High);

        // ====================
        // Default Mode Parameters
        // ====================

        /// <summary>
        /// Noise method for ambient occlusion sampling.
        /// </summary>
        public ScreenSpaceAmbientOcclusionNoiseMethod method
        {
            get => m_Method.value;
            set => m_Method.value = value;
        }
        [Tooltip("'Interleaved Gradient Noise' generates static SSAO and is more performant. 'Blue Noise' generates dynamic SSAO at a slightly higher cost, producing a more subtle effect when the camera is in motion.")]
        [SerializeField]
        private ScreenSpaceAmbientOcclusionNoiseMethodParameter m_Method = new ScreenSpaceAmbientOcclusionNoiseMethodParameter(ScreenSpaceAmbientOcclusionNoiseMethod.InterleavedGradient);

        /// <summary>
        /// Source of depth and normal data for ambient occlusion.
        /// </summary>
        public ScreenSpaceAmbientOcclusionDepthSource depthSource
        {
            get
            {
                var q = m_Quality.value;
                return q == ScreenSpaceAmbientOcclusionQuality.Custom ? m_DepthSource.value : GetPresetDepthSource(q);
            }
            set => m_DepthSource.value = value;
        }
        [Tooltip("The source of the depth and normal data. Depth Normals is more accurate but requires the depth-normals prepass.")]
        [SerializeField]
        private ScreenSpaceAmbientOcclusionDepthSourceParameter m_DepthSource = new ScreenSpaceAmbientOcclusionDepthSourceParameter(ScreenSpaceAmbientOcclusionDepthSource.DepthNormals);

        /// <summary>
        /// Normal reconstruction quality when computing normals from depth.
        /// Only used when depth source is set to Depth.
        /// </summary>
        public ScreenSpaceAmbientOcclusionNormalQuality normalQuality
        {
            get
            {
                var q = m_Quality.value;
                return q == ScreenSpaceAmbientOcclusionQuality.Custom ? m_NormalQuality.value : GetPresetNormalQuality(q);
            }
            set => m_NormalQuality.value = value;
        }
        [Tooltip("The number of depth texture samples that Unity takes when computing normals from depth. Only used when Depth Source is set to Depth.")]
        [SerializeField]
        private ScreenSpaceAmbientOcclusionNormalQualityParameter m_NormalQuality = new ScreenSpaceAmbientOcclusionNormalQualityParameter(ScreenSpaceAmbientOcclusionNormalQuality.Medium);

        // ====================
        // GTAO Mode Parameters
        // ====================

        /// <summary>
        /// Maximum radius in pixels for GTAO sampling.
        /// </summary>
        public int maximumRadiusInPixels
        {
            get => m_MaximumRadiusInPixels.value;
            set => m_MaximumRadiusInPixels.value = value;
        }
        [Tooltip("Maximum screen-space extent in pixels for the ambient occlusion sampling area. Works together with Radius to control the visible range of the effect.")]
        [SerializeField]
        private ClampedIntParameter m_MaximumRadiusInPixels = new ClampedIntParameter(40, 16, 256);

        /// <summary>
        /// When enabled, uses compute shaders for GTAO calculation.
        /// Provides temporal filtering support and configurable direction/step counts.
        /// </summary>
        public bool useComputeShader
        {
            get => m_UseComputeShader.value;
            set => m_UseComputeShader.value = value;
        }
        [Tooltip("When enabled, uses compute shaders for GTAO calculation. Provides temporal filtering support and configurable direction/step counts.")]
        [SerializeField]
        private BoolParameter m_UseComputeShader = new BoolParameter(false);

        // ============================
        // Temporal Filter Parameters
        // ============================

        /// <summary>
        /// Enable temporal filtering for noise reduction and temporal stability.
        /// When enabled, direction count is fixed to 1 (rotates over 6 frames).
        /// </summary>
        public bool temporalFilter
        {
            get => m_TemporalFilter.value;
            set => m_TemporalFilter.value = value;
        }
        [Tooltip("Enable temporal filtering to reduce noise and improve stability over time. Requires Motion Vectors. When enabled, Direction Count is ignored and fixed to 1.")]
        [SerializeField]
        private BoolParameter m_TemporalFilter = new BoolParameter(false);

        /// <summary>
        /// Variance scale for temporal AABB clamping. Higher values allow more ghosting but smoother results.
        /// </summary>
        public float temporalScale
        {
            get => m_TemporalScale.value;
            set => m_TemporalScale.value = value;
        }
        [Tooltip("Controls how much variation is allowed between frames. Higher values produce smoother results but may cause ghosting artifacts.")]
        [SerializeField]
        private ClampedFloatParameter m_TemporalScale = new ClampedFloatParameter(1.25f, 0.5f, 2.0f);

        /// <summary>
        /// Blend weight for previous frame's result. Higher values allow more ghosting but smoother results.
        /// </summary>
        public float temporalResponse
        {
            get => m_TemporalResponse.value;
            set => m_TemporalResponse.value = value;
        }
        [Tooltip("Controls how much of the previous frame's result is kept. Higher values produce smoother, more stable results but may cause ghosting.")]
        [SerializeField]
        private ClampedFloatParameter m_TemporalResponse = new ClampedFloatParameter(0.9f, 0.0f, 0.98f);

        // ============================
        // GTAO Compute Parameters
        // ============================

        /// <summary>
        /// Number of directions to sample for GTAO. More directions improve quality but increase cost.
        /// </summary>
        public int directionCount
        {
            get
            {
                var q = m_Quality.value;
                return q == ScreenSpaceAmbientOcclusionQuality.Custom ? m_DirectionCount.value : GetPresetDirectionCount(q);
            }
            set => m_DirectionCount.value = value;
        }
        [Tooltip("Number of directions to sample for GTAO. More directions improve quality but increase cost.")]
        [SerializeField]
        private ClampedIntParameter m_DirectionCount = new ClampedIntParameter(2, 1, 8);

        /// <summary>
        /// Number of steps per direction for GTAO. More steps improve quality but increase cost.
        /// </summary>
        public int stepCount
        {
            get
            {
                var q = m_Quality.value;
                return q == ScreenSpaceAmbientOcclusionQuality.Custom ? m_StepCount.value : GetPresetStepCount(q);
            }
            set => m_StepCount.value = value;
        }
        [Tooltip("Number of steps per direction for GTAO. More steps improve quality but increase cost.")]
        [SerializeField]
        private ClampedIntParameter m_StepCount = new ClampedIntParameter(4, 1, 16);

        // ============================
        // Quality Preset Lookups
        // ============================

        internal static bool GetPresetDownsample(ScreenSpaceAmbientOcclusionQuality q) => q switch
        {
            ScreenSpaceAmbientOcclusionQuality.Low      => true,
            ScreenSpaceAmbientOcclusionQuality.Medium   => true,
            _                                           => false, // High
        };

        internal static ScreenSpaceAmbientOcclusionSampleCount GetPresetSampleCount(ScreenSpaceAmbientOcclusionQuality q) => q switch
        {
            ScreenSpaceAmbientOcclusionQuality.Low      => ScreenSpaceAmbientOcclusionSampleCount.Low,
            ScreenSpaceAmbientOcclusionQuality.Medium   => ScreenSpaceAmbientOcclusionSampleCount.Medium,
            _                                           => ScreenSpaceAmbientOcclusionSampleCount.High,
        };

        internal static ScreenSpaceAmbientOcclusionBlurQuality GetPresetBlurQuality(ScreenSpaceAmbientOcclusionQuality q) => q switch
        {
            ScreenSpaceAmbientOcclusionQuality.Low      => ScreenSpaceAmbientOcclusionBlurQuality.Low,
            ScreenSpaceAmbientOcclusionQuality.Medium   => ScreenSpaceAmbientOcclusionBlurQuality.Medium,
            _                                           => ScreenSpaceAmbientOcclusionBlurQuality.High,
        };

        internal static ScreenSpaceAmbientOcclusionDepthSource GetPresetDepthSource(ScreenSpaceAmbientOcclusionQuality q) => q switch
        {
            ScreenSpaceAmbientOcclusionQuality.Low      => ScreenSpaceAmbientOcclusionDepthSource.Depth,
            ScreenSpaceAmbientOcclusionQuality.Medium   => ScreenSpaceAmbientOcclusionDepthSource.DepthNormals,
            _                                           => ScreenSpaceAmbientOcclusionDepthSource.DepthNormals, // High
        };

        internal static ScreenSpaceAmbientOcclusionNormalQuality GetPresetNormalQuality(ScreenSpaceAmbientOcclusionQuality q) => q switch
        {
            ScreenSpaceAmbientOcclusionQuality.Low      => ScreenSpaceAmbientOcclusionNormalQuality.Low,
            ScreenSpaceAmbientOcclusionQuality.Medium   => ScreenSpaceAmbientOcclusionNormalQuality.Medium,
            _                                           => ScreenSpaceAmbientOcclusionNormalQuality.High,
        };

        internal static int GetPresetDirectionCount(ScreenSpaceAmbientOcclusionQuality q) => q switch
        {
            ScreenSpaceAmbientOcclusionQuality.Low      => 1,
            ScreenSpaceAmbientOcclusionQuality.Medium   => 2,
            _                                           => 4, // High
        };

        internal static int GetPresetStepCount(ScreenSpaceAmbientOcclusionQuality q) => q switch
        {
            ScreenSpaceAmbientOcclusionQuality.Low      => 2,
            ScreenSpaceAmbientOcclusionQuality.Medium   => 4,
            _                                           => 4, // High
        };

        /// <summary>
        /// Query if the effect is active and should be rendered.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive() => m_Mode.value != ScreenSpaceAmbientOcclusionMode.None && m_Intensity.value > 0f && m_Radius.value > 0f && m_FalloffDistance.value > 0f;

        /// <summary>
        /// Query if the effect is using the Standard SSAO mode.
        /// </summary>
        /// <returns><c>true</c> if using Standard mode, <c>false</c> otherwise.</returns>
        public bool IsStandardMode() => m_Mode.value == ScreenSpaceAmbientOcclusionMode.Standard;

        /// <summary>
        /// Query if blue noise sampling is enabled.
        /// </summary>
        /// <returns><c>true</c> if blue noise sampling is enabled, <c>false</c> otherwise.</returns>
        public bool IsBlueNoiseEnabled() => m_Method.value == ScreenSpaceAmbientOcclusionNoiseMethod.BlueNoise;
    }

    // ============================
    // Volume Parameter Types
    // ============================

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceAmbientOcclusionMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceAmbientOcclusionModeParameter : VolumeParameter<ScreenSpaceAmbientOcclusionMode>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceAmbientOcclusionModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceAmbientOcclusionModeParameter(ScreenSpaceAmbientOcclusionMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceAmbientOcclusionNoiseMethod"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceAmbientOcclusionNoiseMethodParameter : VolumeParameter<ScreenSpaceAmbientOcclusionNoiseMethod>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceAmbientOcclusionNoiseMethodParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceAmbientOcclusionNoiseMethodParameter(ScreenSpaceAmbientOcclusionNoiseMethod value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceAmbientOcclusionSampleCount"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceAmbientOcclusionSampleCountParameter : VolumeParameter<ScreenSpaceAmbientOcclusionSampleCount>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceAmbientOcclusionSampleCountParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceAmbientOcclusionSampleCountParameter(ScreenSpaceAmbientOcclusionSampleCount value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceAmbientOcclusionDepthSource"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceAmbientOcclusionDepthSourceParameter : VolumeParameter<ScreenSpaceAmbientOcclusionDepthSource>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceAmbientOcclusionDepthSourceParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceAmbientOcclusionDepthSourceParameter(ScreenSpaceAmbientOcclusionDepthSource value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceAmbientOcclusionNormalQuality"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceAmbientOcclusionNormalQualityParameter : VolumeParameter<ScreenSpaceAmbientOcclusionNormalQuality>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceAmbientOcclusionNormalQualityParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceAmbientOcclusionNormalQualityParameter(ScreenSpaceAmbientOcclusionNormalQuality value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceAmbientOcclusionBlurQuality"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceAmbientOcclusionBlurQualityParameter : VolumeParameter<ScreenSpaceAmbientOcclusionBlurQuality>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceAmbientOcclusionBlurQualityParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceAmbientOcclusionBlurQualityParameter(ScreenSpaceAmbientOcclusionBlurQuality value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ScreenSpaceAmbientOcclusionQuality"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceAmbientOcclusionQualityParameter : VolumeParameter<ScreenSpaceAmbientOcclusionQuality>
    {
        /// <summary>
        /// Creates a new <see cref="ScreenSpaceAmbientOcclusionQualityParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ScreenSpaceAmbientOcclusionQualityParameter(ScreenSpaceAmbientOcclusionQuality value, bool overrideState = false) : base(value, overrideState) { }
    }

}
#endif
