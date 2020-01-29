using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Probe Volume Global Illumination")]
    public class ProbeVolumeController : VolumeComponent
    {
        [Serializable]
        public sealed class LeakMitigationModeParameter : VolumeParameter<LeakMitigationMode>
        {
            public LeakMitigationModeParameter(LeakMitigationMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        [Tooltip("Selects the heuristic used for mitigating light leaking and self-shadowing artifacts when sampling from the probe volumes.")]
        public LeakMitigationModeParameter leakMitigationMode = new LeakMitigationModeParameter(LeakMitigationMode.NormalBias);

        [Tooltip("Controls the distance in world space to bias along the surface normal to mitigate light leaking self-shadow artifacts.")]
        public MinFloatParameter normalBiasWS = new MinFloatParameter(0.0f, 0.0f);

        [Tooltip("Controls the strength of our bilateral filter. 0.0 falls back to trilinear filtering. 1.0 is maximum cross term (geometric or validity).")]
        public ClampedFloatParameter bilateralFilterWeight = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        ProbeVolumeController()
        {
            displayName = "Probe Volume Controller";
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
