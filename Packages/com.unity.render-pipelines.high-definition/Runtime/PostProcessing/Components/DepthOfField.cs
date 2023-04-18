using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Focusing modes for the depth of field effect.
    /// </summary>
    /// <seealso cref="DepthOfField.focusMode"/>
    public enum DepthOfFieldMode
    {
        /// <summary>
        /// Disables depth of field.
        /// </summary>
        Off,

        /// <summary>
        /// Uses the physical Camera to set focusing properties.
        /// </summary>
        [InspectorName("Physical Camera")]
        UsePhysicalCamera,

        /// <summary>
        /// Uses custom distance values to set the focus.
        /// </summary>
        [InspectorName("Manual Ranges")]
        Manual
    }

    /// <summary>
    /// The resolution at which HDRP processes the depth of field effect.
    /// </summary>
    /// <seealso cref="DepthOfField.resolution"/>
    public enum DepthOfFieldResolution : int
    {
        /// <summary>
        /// Quarter resolution.
        /// </summary>
        Quarter = 4,

        /// <summary>
        /// Half resolution.
        /// </summary>
        Half = 2,

        /// <summary>
        /// Full resolution. Should only be set for beauty shots or film uses.
        /// </summary>
        Full = 1
    }

    /// <summary>
    /// Options for the source of the focus distance HDRP uses in the depth of field calculations.
    /// </summary>
    public enum FocusDistanceMode
    {
        /// <summary>
        /// Uses the focus distance from the Volume component.
        /// </summary>
        Volume,

        /// <summary>
        /// Uses the focus distance from the physical camera.
        /// </summary>
        Camera
    }

    /// <summary>
    /// A volume component that holds settings for the Depth Of Field effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Depth Of Field", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Post-Processing-Depth-of-Field")]
    public sealed class DepthOfField : VolumeComponentWithQuality, IPostProcessComponent
    {
        // Sampling ratios for adaptive sampling.
        // X: ratio of the sharp part tiles of PBR dof that have high variance of CoC.
        // Y: ratio of the blurry / sharp tiles that have low variance of CoC.
        internal static Vector2 s_HighQualityAdaptiveSamplingWeights = new Vector2(4.0f, 1.0f);
        internal static Vector2 s_LowQualityAdaptiveSamplingWeights  = new Vector2(1.0f, 0.75f);

        /// <summary>
        /// Specifies the mode that HDRP uses to set the focus for the depth of field effect.
        /// </summary>
        /// <seealso cref="DepthOfFieldMode"/>
        [Tooltip("Specifies the mode that HDRP uses to set the focus for the depth of field effect.")]
        public DepthOfFieldModeParameter focusMode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

        // -------------------------------------------
        // Physical settings
        //

        /// <summary>
        /// The distance to the focus plane from the Camera.
        /// </summary>
        [Tooltip("The distance to the focus plane from the Camera.")]
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);

        /// <summary>
        /// Specifies where to read the focus distance from.
        /// </summary>
        [Tooltip("Specifies where to read the focus distance from..")]
        public FocusDistanceModeParameter focusDistanceMode = new FocusDistanceModeParameter(FocusDistanceMode.Volume);

        // -------------------------------------------
        // Manual settings
        // Note: because they can't mathematically be mapped to physical settings, interpolating
        // between manual & physical is not supported
        //

        /// <summary>
        /// Sets the distance from the Camera at which the near field blur begins to decrease in intensity.
        /// </summary>
        [Header("Near Range")]
        [Tooltip("Sets the distance from the Camera at which the near field blur begins to decrease in intensity.")]
        public MinFloatParameter nearFocusStart = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// Sets the distance from the Camera at which the near field does not blur anymore.
        /// </summary>
        [Tooltip("Sets the distance from the Camera at which the near field does not blur anymore.")]
        public MinFloatParameter nearFocusEnd = new MinFloatParameter(4f, 0f);

        /// <summary>
        /// Sets the distance from the Camera at which the far field starts blurring.
        /// </summary>
        [Header("Far Range")]
        [Tooltip("Sets the distance from the Camera at which the far field starts blurring.")]
        public MinFloatParameter farFocusStart = new MinFloatParameter(10f, 0f);

        /// <summary>
        /// Sets the distance from the Camera at which the far field blur reaches its maximum blur radius.
        /// </summary>
        [Tooltip("Sets the distance from the Camera at which the far field blur reaches its maximum blur radius.")]
        public MinFloatParameter farFocusEnd = new MinFloatParameter(20f, 0f);

        // -------------------------------------------
        // Shared settings
        //

        /// <summary>
        /// Sets the number of samples to use for the near field.
        /// </summary>
        public int nearSampleCount
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_NearSampleCount.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().NearBlurSampleCount[qualityLevel];
                }
            }
            set { m_NearSampleCount.value = value; }
        }

        /// <summary>
        /// Sets the maximum radius the near blur can reach.
        /// </summary>
        public float nearMaxBlur
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_NearMaxBlur.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().NearBlurMaxRadius[qualityLevel];
                }
            }
            set { m_NearMaxBlur.value = value; }
        }

        /// <summary>
        /// Sets the number of samples to use for the far field.
        /// </summary>
        public int farSampleCount
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_FarSampleCount.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().FarBlurSampleCount[qualityLevel];
                }
            }
            set { m_FarSampleCount.value = value; }
        }

        /// <summary>
        /// Sets the maximum radius the far blur can reach.
        /// </summary>
        public float farMaxBlur
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_FarMaxBlur.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().FarBlurMaxRadius[qualityLevel];
                }
            }
            set { m_FarMaxBlur.value = value; }
        }

        /// <summary>
        /// When enabled, HDRP uses bicubic filtering instead of bilinear filtering for the depth of field effect.
        /// </summary>
        public bool highQualityFiltering
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_HighQualityFiltering.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().DoFHighQualityFiltering[qualityLevel];
                }
            }
            set { m_HighQualityFiltering.value = value; }
        }
    
        /// <summary>
        /// When enabled, HDRP uses a more accurate but slower physically based method to compute the depth of field effect.
        /// </summary>
        public bool physicallyBased
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_PhysicallyBased.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().DoFPhysicallyBased[qualityLevel];
                }
            }
            set { m_PhysicallyBased.value = value; }
        }

        /// <summary>
        /// Adjust near blur CoC based on depth distance when manual, non-physical mode is used.
        /// </summary>
        public bool limitManualRangeNearBlur
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_LimitManualRangeNearBlur.value;

                return GetPostProcessingQualitySettings().LimitManualRangeNearBlur[quality.levelAndOverride.level];
            }
            set => m_LimitManualRangeNearBlur.value = value;
        }

        /// <summary>
        /// Specifies the resolution at which HDRP processes the depth of field effect.
        /// </summary>
        /// <seealso cref="DepthOfFieldResolution"/>
        public DepthOfFieldResolution resolution
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_Resolution.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().DoFResolution[qualityLevel];
                }
            }
            set
            {
                m_Resolution.value = value;
            }
        }


        [Header("Near Blur")]
        [Tooltip("Sets the number of samples to use for the near field.")]
        [SerializeField, FormerlySerializedAs("nearSampleCount")]
        ClampedIntParameter m_NearSampleCount = new ClampedIntParameter(5, 3, 8);

        [SerializeField, FormerlySerializedAs("nearMaxBlur")]
        [Tooltip("Sets the maximum radius the near blur can reach.")]
        ClampedFloatParameter m_NearMaxBlur = new ClampedFloatParameter(4f, 0f, 8f);

        [Header("Far Blur")]
        [Tooltip("Sets the number of samples to use for the far field.")]
        [SerializeField, FormerlySerializedAs("farSampleCount")]
        ClampedIntParameter m_FarSampleCount = new ClampedIntParameter(7, 3, 16);

        [Tooltip("Sets the maximum radius the far blur can reach.")]
        [SerializeField, FormerlySerializedAs("farMaxBlur")]
        ClampedFloatParameter m_FarMaxBlur = new ClampedFloatParameter(8f, 0f, 16f);

        // -------------------------------------------
        // Advanced settings
        //
        [Header("Advanced Tweaks")]
        [AdditionalProperty]
        [Tooltip("Specifies the resolution at which HDRP processes the depth of field effect.")]
        [SerializeField, FormerlySerializedAs("resolution")]
        DepthOfFieldResolutionParameter m_Resolution = new DepthOfFieldResolutionParameter(DepthOfFieldResolution.Half);

        [AdditionalProperty]
        [Tooltip("When enabled, HDRP uses bicubic instead of bilinear filtering for the depth of field effect. Also conceals tiling artifacts in the physically-based mode.")]
        [SerializeField, FormerlySerializedAs("highQualityFiltering")]
        BoolParameter m_HighQualityFiltering = new BoolParameter(true);

        [AdditionalProperty]
        [Tooltip("When enabled, HDRP uses a more accurate but slower physically based algorithm to compute the depth of field effect.")]
        [SerializeField]
        BoolParameter m_PhysicallyBased = new BoolParameter(false);

        [AdditionalProperty]
        [Tooltip("Adjust near blur CoC based on depth distance when manual, non-physical mode is used.")]
        [SerializeField]
        BoolParameter m_LimitManualRangeNearBlur = new BoolParameter(false);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return focusMode.value != DepthOfFieldMode.Off && (IsNearLayerActive() || IsFarLayerActive());
        }

        /// <summary>
        /// Returns the state of the near field blur.
        /// This is only relevant when <see cref="DepthOfFieldMode.Manual"/> is set.
        /// </summary>
        /// <returns><c>true</c> if the near field blur is active and visible.</returns>
        public bool IsNearLayerActive() => nearMaxBlur > 0f && nearFocusEnd.value > 0f;

        /// <summary>
        /// Returns the state of the far field blur.
        /// This is only relevant when <see cref="DepthOfFieldMode.Manual"/> is set.
        /// </summary>
        /// <returns><c>true</c> if the far field blur is active and visible.</returns>
        public bool IsFarLayerActive() => farMaxBlur > 0f;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="DepthOfFieldMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode>
    {
        /// <summary>
        /// Creates a new <see cref="DepthOfFieldModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="DepthOfFieldResolution"/> value.
    /// </summary>
    [Serializable]
    public sealed class DepthOfFieldResolutionParameter : VolumeParameter<DepthOfFieldResolution>
    {
        /// <summary>
        /// Creates a new <see cref="DepthOfFieldResolutionParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public DepthOfFieldResolutionParameter(DepthOfFieldResolution value, bool overrideState = false) : base(value, overrideState) { }
    }


    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="FocusDistanceModeParameter"/> value.
    /// </summary>
    [Serializable]
    public sealed class FocusDistanceModeParameter : VolumeParameter<FocusDistanceMode>
    {
        /// <summary>
        /// Creates a new <see cref="FocusDistanceModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FocusDistanceModeParameter(FocusDistanceMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
