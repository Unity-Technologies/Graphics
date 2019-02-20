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
        public DepthOfFieldModeParameter focusMode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

        // Physical settings
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);

        // Manual settings
        // Note: because they can't mathematically be mapped to physical settings, interpolating
        // between manual & physical is not supported
        public MinFloatParameter nearFocusStart = new MinFloatParameter(0f, 0f);
        public MinFloatParameter nearFocusEnd = new MinFloatParameter(4f, 0f);

        public MinFloatParameter farFocusStart = new MinFloatParameter(10f, 0f);
        public MinFloatParameter farFocusEnd = new MinFloatParameter(20f, 0f);

        // Shared settings
        public ClampedIntParameter nearSampleCount = new ClampedIntParameter(5, 3, 8);
        public ClampedFloatParameter nearMaxBlur = new ClampedFloatParameter(4f, 0f, 8f);

        public ClampedIntParameter farSampleCount = new ClampedIntParameter(7, 3, 16);
        public ClampedFloatParameter farMaxBlur = new ClampedFloatParameter(8f, 0f, 16f);

        // Advanced settings
        public BoolParameter highQualityFiltering = new BoolParameter(true);
        public DepthOfFieldResolutionParameter resolution = new DepthOfFieldResolutionParameter(DepthOfFieldResolution.Half);

        public bool IsActive()
        {
            bool dofActive = focusMode.value != DepthOfFieldMode.Off && (IsNearLayerActive() || IsFarLayerActive());

            if (dofActive && XRGraphics.enabled)
            {
                Debug.LogWarning("DepthOfField is not supported with VR.");
                dofActive = false;
            }

            return dofActive;
        }

        public bool IsNearLayerActive() => nearMaxBlur > 0f && nearFocusEnd > 0f;

        public bool IsFarLayerActive() => farMaxBlur > 0f;
    }

    [Serializable]
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overrideState = false) : base(value, overrideState) { } }

    [Serializable]
    public sealed class DepthOfFieldResolutionParameter : VolumeParameter<DepthOfFieldResolution> { public DepthOfFieldResolutionParameter(DepthOfFieldResolution value, bool overrideState = false) : base(value, overrideState) { } }
}
