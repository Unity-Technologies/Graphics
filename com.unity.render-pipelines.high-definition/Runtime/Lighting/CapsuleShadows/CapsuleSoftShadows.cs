using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

// Do we even need this?

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Capsule/Soft Shadows")]
    internal class CapsuleSoftShadows : VolumeComponent
    {
        public BoolParameter enabled = new BoolParameter(false);

        // IMPORTANT: Whenever this is changed the LUT is recomputed and that is not desirable at runtime.
        // In a proper implementation we should have the rebake happen only upon request, so TODO in case we want this in proper HDRP
        public ClampedFloatParameter coneAperture = new ClampedFloatParameter(30.0f, 15.0f, 89.0f);


        internal static bool IsCapsuleSoftShadowsEnabled(HDCamera hdCamera)
        {
            var capsuleSoftShadows = hdCamera.volumeStack.GetComponent<CapsuleSoftShadows>();
            return capsuleSoftShadows.enabled.value;
        }
    }
}
