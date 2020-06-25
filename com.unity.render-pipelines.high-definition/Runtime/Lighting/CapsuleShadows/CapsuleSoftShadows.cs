using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

// Do we even need this?

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Capsule/Soft Shadows")]
    internal class CapsuleSoftShadows : VolumeComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);

        // IMPORTANT: Whenever this is changed the LUT is recomputed and that is not desirable at runtime.
        // In a proper implementation we should have the rebake happen only upon request, so TODO in case we want this in proper HDRP
        public ClampedFloatParameter coneAperture = new ClampedFloatParameter(30.0f, 15.0f, 89.0f);


        internal static bool IsCapsuleSoftShadowsEnabled(HDCamera hdCamera)
        {
            var capsuleSoftShadows = hdCamera.volumeStack.GetComponent<CapsuleSoftShadows>();
            return capsuleSoftShadows.intensity.value > 0.0f;
        }
    }
}
