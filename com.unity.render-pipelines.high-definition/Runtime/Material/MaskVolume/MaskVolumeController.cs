using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    // TODO: Probe Volume leak mitigation modes are not used in Mask Volumes. Delete if this isn't needed.
    //[VolumeComponentMenu("Lighting/Experimental/Mask Volume")]
    /* internal class MaskVolumeController : VolumeComponent
    {
        [Serializable]
        internal sealed class LeakMitigationModeParameter : VolumeParameter<LeakMitigationMode>
        {
            public LeakMitigationModeParameter(LeakMitigationMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        [SerializeField, Tooltip("Selects the heuristic used for mitigating light leaking and self-shadowing artifacts when sampling from the mask volumes.")]
        internal LeakMitigationModeParameter leakMitigationMode = new LeakMitigationModeParameter(LeakMitigationMode.NormalBias);

        [SerializeField, Tooltip("Controls the strength of our bilateral filter. 0.0 falls back to trilinear filtering. 1.0 is maximum cross term (geometric or validity).")]
        internal ClampedFloatParameter bilateralFilterWeight = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        MaskVolumeController()
        {
            displayName = "Mask Volume Controller (Experimental)";
        }
    } */
} // UnityEngine.Experimental.Rendering.HDPipeline
