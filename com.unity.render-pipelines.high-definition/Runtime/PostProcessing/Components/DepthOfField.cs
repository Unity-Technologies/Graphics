using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum DepthOfFieldMode
    {
        Off,
        UsePhysicalCamera,
        Manual
    }

    public enum DepthOfFieldResolution : int
    {
        Quarter = 4,
        Half = 2,
        Full = 1
    }

    // TODO: Tooltips
    [Serializable, VolumeComponentMenu("Post-processing/Depth Of Field")]
    public sealed class DepthOfField : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Specifies the mode that HDRP uses to set the focus for the depth of field effect.")]
        public DepthOfFieldModeParameter focusMode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

        // Physical settings
        [Tooltip("Sets the distance to the focus point from the Camera.")]
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);

        // Manual settings
        // Note: because they can't mathematically be mapped to physical settings, interpolating
        // between manual & physical is not supported
        [Tooltip("Sets the distance from the Camera at which the near field blur begins to decrease in intensity.")]
        public MinFloatParameter nearFocusStart = new MinFloatParameter(0f, 0f);
        [Tooltip("Sets the distance from the Camera at which the near field does not blur anymore.")]
        public MinFloatParameter nearFocusEnd = new MinFloatParameter(4f, 0f);

        [Tooltip("Sets the distance from the Camera at which the far field starts blurring.")]
        public MinFloatParameter farFocusStart = new MinFloatParameter(10f, 0f);
        [Tooltip("Sets the distance from the Camera at which the far field blur reaches its maximum blur radius.")]
        public MinFloatParameter farFocusEnd = new MinFloatParameter(20f, 0f);

        // Shared settings
        [Tooltip("Sets the number of samples to use for the near field.")]
        public ClampedIntParameter nearSampleCount = new ClampedIntParameter(5, 3, 8);
        [Tooltip("Sets the maximum radius the near blur can reach.")]
        public ClampedFloatParameter nearMaxBlur = new ClampedFloatParameter(4f, 0f, 8f);

        [Tooltip("Sets the number of samples to use for the far field.")]
        public ClampedIntParameter farSampleCount = new ClampedIntParameter(7, 3, 16);
        [Tooltip("Sets the maximum radius the far blur can reach.")]
        public ClampedFloatParameter farMaxBlur = new ClampedFloatParameter(8f, 0f, 16f);

        // Advanced settings
        [Tooltip("When enabled, HDRP uses bicubic filtering instead of bilinear filtering for the depth of field effect.")]
        public BoolParameter highQualityFiltering = new BoolParameter(true);
        [Tooltip("Specifies the resolution at which HDRP processes the depth of field effect.")]
        public DepthOfFieldResolutionParameter resolution = new DepthOfFieldResolutionParameter(DepthOfFieldResolution.Half);

        public bool IsActive()
        {
            return focusMode.value != DepthOfFieldMode.Off && (IsNearLayerActive() || IsFarLayerActive());
        }

        public bool IsNearLayerActive() => nearMaxBlur.value > 0f && nearFocusEnd.value > 0f;

        public bool IsFarLayerActive() => farMaxBlur.value > 0f;
    }

    [Serializable]
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overrideState = false) : base(value, overrideState) { } }

    [Serializable]
    public sealed class DepthOfFieldResolutionParameter : VolumeParameter<DepthOfFieldResolution> { public DepthOfFieldResolutionParameter(DepthOfFieldResolution value, bool overrideState = false) : base(value, overrideState) { } }
}
