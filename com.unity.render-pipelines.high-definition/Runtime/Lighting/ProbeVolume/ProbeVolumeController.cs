using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    //[VolumeComponentMenu("Lighting/Experimental/Probe Volume")]
    internal class ProbeVolumeController : VolumeComponent
    {
        [Serializable]
        internal sealed class LeakMitigationModeParameter : VolumeParameter<LeakMitigationMode>
        {
            public LeakMitigationModeParameter(LeakMitigationMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        [SerializeField, Tooltip("Selects the heuristic used for mitigating light leaking and self-shadowing artifacts when sampling from the probe volumes.")]
        internal LeakMitigationModeParameter leakMitigationMode = new LeakMitigationModeParameter(LeakMitigationMode.NormalBias);

        [SerializeField, Tooltip("Controls the strength of our bilateral filter. 0.0 falls back to trilinear filtering. 1.0 is maximum cross term (geometric or validity).")]
        internal ClampedFloatParameter bilateralFilterWeight = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [SerializeField, Tooltip("Controls the minimum weight of our octahedral depth filter term. 0.0 means a probe can be fully rejected based on octahedral depth. 1.0 means a probe will never be rejected based on octahedral depth. Low, but non-zero values are reccomended.")]
        internal ClampedFloatParameter octahedralDepthWeightMin = new ClampedFloatParameter(0.05f, 0.0f, 1.0f);

        [SerializeField, Tooltip("Controls the sharpness of our octahedral depth filter. 0.0 is the least sharp transition. Can result in light leaks. 1.0 is the sharpest transition. Can result in sharp edge artifacts. High, but less than 1 values are reccomended.")]
        internal ClampedFloatParameter octahedralDepthLightBleedReductionThreshold = new ClampedFloatParameter(0.99f, 0.0f, 0.9999f);

        ProbeVolumeController()
        {
            displayName = "Probe Volume Controller (Experimental)";
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
