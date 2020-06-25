using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

// Do we even need this?

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Capsule/Soft Shadows")]
    internal class CapsuleSoftShadows : VolumeComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public BoolParameter directShadow = new BoolParameter(true);

        // IMPORTANT: Whenever this is changed the LUT is recomputed and that is not desirable at runtime.
        // In a proper implementation we should have the rebake happen only upon request, so TODO in case we want this in proper HDRP
        public ClampedFloatParameter coneAperture = new ClampedFloatParameter(30.0f, 15.0f, 89.0f);
    }
}
