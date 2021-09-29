using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    // TODO: Probe Volume leak mitigation modes are not used in Mask Volumes. Delete if this isn't needed.
    //[VolumeComponentMenu("Lighting/Experimental/Mask Volume")]
    internal class MaskVolumeController : VolumeComponent
    {
        [SerializeField, Tooltip("The global distance fade start, applied on top of per Mask Volume distance fade start.")]
        internal FloatParameter distanceFadeStart = new FloatParameter(200.0f);

        [SerializeField, Tooltip("The global distance fade end, applied on top of per Mask Volume distance fade end.")]
        internal FloatParameter distanceFadeEnd = new FloatParameter(300.0f);

        MaskVolumeController()
        {
            displayName = "Mask Volume Controller (Experimental)";
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
